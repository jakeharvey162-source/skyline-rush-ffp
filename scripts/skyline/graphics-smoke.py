"""Real OpenGL client smoke tests; screenshots are evidence, not human playtesting."""
from pathlib import Path
import os, subprocess, time
ROOT=Path(__file__).resolve().parents[2]
os.chdir(ROOT)
OUT=ROOT/'smoke-evidence'
OUT.mkdir(exist_ok=True)
CLIENT=ROOT/'src/redeclipse_linux'
SERVER=ROOT/'src/redeclipse_server_linux'
def launch(name, script):
    home=OUT/name
    home.mkdir(exist_ok=True)
    log=open(home/'stdout.log','w')
    proc=subprocess.Popen([str(CLIENT),f'-h{home}','-gclient.log','-df0','-dw640','-dh360','-sm',f'-x{script}'],stdout=log,stderr=subprocess.STDOUT)
    return proc,home,log
def finish(proc,home,log,marker):
    try:
        proc.wait(timeout=240)
        text=(home/'client.log').read_text(errors='replace')
        if proc.returncode or marker not in text:
            raise AssertionError(f'{home.name}: exit={proc.returncode}, marker missing? {marker not in text}')
        if home.name.startswith('player'):
            line=next(line for line in text.splitlines() if marker in line)
            if line.split()[-1] == '-1': raise AssertionError('Second player was not present')
        if not list(home.rglob('*.png')):
            raise AssertionError(f'{home.name}: no rendered screenshot')
    finally:
        if proc.poll() is None: proc.kill();proc.wait()
        log.close()
for district in ('heights','lagoon','rift'):
    # Choose a valid loadout before joining; an empty loadout prevents spawning.
    script=f'name SkylineTester; playerloadweap [2 3]; gameui_close_all; botbalance 4; start maps/skyline/{district} 2 0; sleep 12000 [gameui_close_all; spectate 0; sleep 1000 [screenshot {district}; echo SKYLINE_RENDER_{district} $mapname; quit]]'
    finish(*launch(district,script),f'SKYLINE_RENDER_{district} maps/skyline/{district}')
serverhome=OUT/'server';serverhome.mkdir(exist_ok=True)
(serverhome/'servinit.cfg').write_text('servermaster ""\nserverpass "SmokeOnly93"\nsv_serverclients 2\nsv_serverspectators 0\nsv_defaultmap "maps/skyline/lagoon"\nsv_defaultmode 2\nsv_defaultmuts 1\nsv_botbalance 0\nsv_rotatemode 0\nsv_rotatemuts 0\nsv_resetvarsonend 0\n')
with open(serverhome/'stdout.log','w') as log:
    server=subprocess.Popen([str(SERVER),f'-h{serverhome}','-gserver.log','-ss1','-si127.0.0.1','-sm','-xskyline_startroom'],stdout=log,stderr=subprocess.STDOUT)
    clients=[]
    try:
        time.sleep(3)
        for i in (1,2):
            script=f'connectguidelines 1; name SmokePlayer{i}; playerloadweap [2 3]; connect 127.0.0.1 29801 SmokeOnly93; sleep 20000 [if (=s (connectedip) "127.0.0.1") [gameui_close_all; spectate 0; sleep 1000 [screenshot player{i}; echo SKYLINE_CONNECTED_{i} $mapname (getclientnum SmokePlayer{3-i}); quit]] [echo SKYLINE_CONNECTION_FAILED; quit]]'
            clients.append((*launch(f'player{i}',script),f'SKYLINE_CONNECTED_{i} maps/skyline/lagoon'))
        for job in clients: finish(*job)
    finally:
        for proc,home,clog,marker in clients:
            if proc.poll() is None: proc.kill();proc.wait()
            clog.close()
        server.terminate()
        try: server.wait(timeout=5)
        except subprocess.TimeoutExpired: server.kill();server.wait()
print('PASS: three rendered districts and two connected clients. Human gameplay still required.')
