"""Trusted local match supervisor. Not a public API or Supabase allocator yet.

Reserve game+query UDP pairs; systemd must use KillMode=control-group.
Only a trusted lobby service should call this module after checking authorization.
"""
import fcntl
import json
import os
from pathlib import Path
import secrets
import signal
import socket
import subprocess
import time
import uuid

MODES = {'duel', '2v2', '4v4', 'coop2', 'coop4'}
MAPS = {'heights', 'lagoon', 'rift'}


class MatchPool:
    def __init__(self, root, state, first_port=29801, capacity=8, idle_seconds=300, max_seconds=7200, spawn=subprocess.Popen, clock=time.monotonic):
        if capacity < 1 or capacity > 64 or first_port < 1024 or first_port + capacity * 2 - 1 > 65535:
            raise ValueError('Invalid port range or match capacity')
        if idle_seconds <= 0 or max_seconds < idle_seconds:
            raise ValueError('Invalid match time limits')
        self.root = Path(root).resolve()
        self.state = Path(state).resolve()
        self.state.mkdir(parents=True, exist_ok=True, mode=0o700)
        self.lock = open(self.state / 'pool.lock', 'a')
        try:
            fcntl.flock(self.lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except Exception:
            self.lock.close()
            raise RuntimeError('Another match pool owns this state directory')
        self.ports = list(range(first_port, first_port + capacity * 2, 2))
        self.idle_seconds, self.max_seconds = idle_seconds, max_seconds
        self.spawn, self.clock = spawn, clock
        self.matches = {}

    @staticmethod
    def _free_pair(port):
        held = []
        try:
            for candidate in (port, port + 1):
                sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
                held.append(sock)
                sock.bind(('0.0.0.0', candidate))
            return True
        except OSError:
            return False
        finally:
            for sock in held:
                sock.close()

    def start(self, room_id, mode, district):
        room_id = str(uuid.UUID(room_id))
        if mode not in MODES or district not in MAPS:
            raise ValueError('Unsupported mode or district')
        self.reap()
        if room_id in self.matches:
            raise ValueError('Room already has a match')
        used = {m['port'] for m in self.matches.values()}
        port = next((p for p in self.ports if p not in used and self._free_pair(p)), None)
        if port is None:
            raise RuntimeError('Server is full; try again shortly')
        home = self.state / room_id
        home.mkdir(mode=0o700, exist_ok=True)
        (home / 'server.log').unlink(missing_ok=True)
        password = secrets.token_urlsafe(18)
        env = dict(os.environ, SKYLINE_ROOT=str(self.root), SKYLINE_HOME=str(home), SKYLINE_MODE=mode,
                   SKYLINE_DISTRICT=district, SKYLINE_PORT=str(port), SKYLINE_PASSWORD=password)
        # argv list only: no shell command interpolation and no user-controlled executable.
        log = open(home / 'supervisor.log', 'ab')
        try:
            proc = self.spawn(['bash', str(self.root / 'deploy/digitalocean/run-server.sh')], env=env,
                              stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
        finally:
            log.close()
        now = self.clock()
        self.matches[room_id] = dict(process=proc, port=port, started=now, heartbeat=now, password=password)
        return {'room_id': room_id, 'port': port, 'query_port': port + 1, 'password': password, 'state': 'starting'}

    def heartbeat(self, room_id):
        # A trusted server adapter must provide occupancy/liveness, not arbitrary clients.
        self.matches[str(uuid.UUID(room_id))]['heartbeat'] = self.clock()

    def status(self, room_id):
        match = self.matches.get(str(uuid.UUID(room_id)))
        if not match:
            return 'stopped'
        if match['process'].poll() is not None:
            return 'failed'
        log = self.state / str(uuid.UUID(room_id)) / 'server.log'
        if log.exists() and 'Dedicated server started' in log.read_text(errors='replace')[-32768:]:
            return 'running'
        return 'starting'

    def stop(self, room_id):
        room_id = str(uuid.UUID(room_id))
        match = self.matches.pop(room_id, None)
        if not match:
            return
        proc = match['process']
        if proc.poll() is None:
            os.killpg(proc.pid, signal.SIGTERM)
            try:
                proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                os.killpg(proc.pid, signal.SIGKILL)
                proc.wait(timeout=5)
        # Preserve diagnostic logs but remove connection credentials after shutdown.
        (self.state / room_id / 'servinit.cfg').unlink(missing_ok=True)

    def reap(self):
        now = self.clock()
        ended = []
        for room_id, match in list(self.matches.items()):
            reason = ('crashed' if match['process'].poll() is not None else
                      'idle' if now - match['heartbeat'] >= self.idle_seconds else
                      'expired' if now - match['started'] >= self.max_seconds else None)
            if reason:
                self.stop(room_id)
                ended.append({'room_id': room_id, 'reason': reason})
        return ended

    def close(self):
        for room_id in list(self.matches):
            self.stop(room_id)
        self.lock.close()
