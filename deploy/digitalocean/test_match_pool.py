import importlib.util
from pathlib import Path
import socket
import subprocess
import sys
import tempfile
import unittest
import uuid

spec = importlib.util.spec_from_file_location('pool', Path(__file__).with_name('match_pool.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

def sleeper(argv, **kwargs):
    return subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(60)'], **kwargs)

class PoolTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.now = 0
        self.pool = module.MatchPool('.', self.tmp.name, first_port=41001, capacity=2,
                                     idle_seconds=10, max_seconds=20, spawn=sleeper, clock=lambda:self.now)

    def tearDown(self):
        self.pool.close()
        self.tmp.cleanup()

    def test_separate_ports_passwords_and_capacity(self):
        a = self.pool.start(str(uuid.uuid4()), 'duel', 'heights')
        b = self.pool.start(str(uuid.uuid4()), '4v4', 'rift')
        self.assertEqual(b['port'] - a['port'], 2)
        self.assertNotEqual(a['password'], b['password'])
        with self.assertRaises(RuntimeError): self.pool.start(str(uuid.uuid4()), 'duel', 'heights')

    def test_reject_invalid_input(self):
        for room,mode,district in [('../bad','duel','heights'),(str(uuid.uuid4()),'duel;exit','heights'),(str(uuid.uuid4()),'duel','../bad')]:
            with self.assertRaises(ValueError): self.pool.start(room,mode,district)

    def test_idle_cleanup_and_reuse(self):
        room = str(uuid.uuid4())
        first = self.pool.start(room,'duel','heights')
        config = Path(self.tmp.name)/room/'servinit.cfg'
        config.write_text('private password')
        self.now = 11
        self.assertEqual(self.pool.reap()[0]['reason'],'idle')
        self.assertFalse(config.exists())
        self.assertEqual(self.pool.start(room,'duel','heights')['port'],first['port'])

    def test_crash_is_reported_and_reclaimed(self):
        room = str(uuid.uuid4())
        self.pool.start(room,'duel','heights')
        proc = self.pool.matches[room]['process']
        proc.kill(); proc.wait()
        self.assertEqual(self.pool.status(room),'failed')
        self.assertEqual(self.pool.reap()[0]['reason'],'crashed')

    def test_single_supervisor_lock(self):
        with self.assertRaises(RuntimeError): module.MatchPool('.',self.tmp.name)

    def test_query_port_conflict_is_skipped(self):
        with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as sock:
            sock.bind(('0.0.0.0',41002))
            self.assertEqual(self.pool.start(str(uuid.uuid4()),'duel','heights')['port'],41003)

    def test_heartbeat_does_not_bypass_max_lifetime(self):
        room=str(uuid.uuid4()); self.pool.start(room,'duel','heights')
        self.now=19; self.pool.heartbeat(room); self.now=21
        self.assertEqual(self.pool.reap()[0]['reason'],'expired')

if __name__ == '__main__': unittest.main()
