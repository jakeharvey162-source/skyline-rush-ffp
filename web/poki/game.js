(() => {
'use strict';

const canvas=document.getElementById('game');
const ctx=canvas.getContext('2d',{alpha:false});
const W=canvas.width,H=canvas.height,FOV=Math.PI/3,RAYS=320,MAX=24;
const map=[
"1111111111111111","1000000000000001","1000011000110001","1000010000010001",
"1000000000000001","1011000110001101","1000000000000001","1000110001100001",
"1000000000000001","1000011110000001","1000000000000001","1011000000011001",
"1000000000000001","1000010001000001","1000000000000001","1111111111111111"];
const keys={},depth=new Float32Array(RAYS);
const ui={
 loading:document.getElementById('loading'),menu:document.getElementById('menu'),
 hud:document.getElementById('hud'),pause:document.getElementById('pause'),result:document.getElementById('result'),
 mobile:document.getElementById('mobileControls'),timer:document.getElementById('timer'),
 health:document.getElementById('healthText'),healthFill:document.getElementById('healthFill'),
 kills:document.getElementById('kills'),ammo:document.getElementById('ammo'),hit:document.getElementById('hitmarker'),
 damage:document.getElementById('damageFlash'),resultTitle:document.getElementById('resultTitle'),
 resultEyebrow:document.getElementById('resultEyebrow'),resultKills:document.getElementById('resultKills'),
 resultScore:document.getElementById('resultScore'),best:document.getElementById('bestScore'),
 loadFill:document.getElementById('loadFill')
};
let state='loading',player,enemies,last=performance.now(),elapsed=0,shotCd=0;
let stickX=0,stickY=0,lookDelta=0,best=loadBest();

function loadBest(){try{return Number(localStorage.getItem('skylinePokiBest')||0)||0}catch(_){return 0}}
function saveBest(v){try{localStorage.setItem('skylinePokiBest',String(v))}catch(_){}}
function poki(name,...args){try{if(window.PokiSDK&&typeof PokiSDK[name]==='function')return PokiSDK[name](...args)}catch(_){}return Promise.resolve()}
function show(el){[ui.loading,ui.menu,ui.pause,ui.result].forEach(x=>x.classList.remove('active'));if(el)el.classList.add('active')}
function wall(x,y){const gx=Math.floor(x),gy=Math.floor(y);return gx<0||gy<0||gy>=map.length||gx>=map[0].length||map[gy][gx]!=='0'}
function norm(a){while(a>Math.PI)a-=Math.PI*2;while(a<-Math.PI)a+=Math.PI*2;return a}
function cast(a){const s=Math.sin(a),c=Math.cos(a);for(let d=.03;d<MAX;d+=.035){if(wall(player.x+c*d,player.y+s*d))return d}return MAX}
function los(e){const dx=e.x-player.x,dy=e.y-player.y,d=Math.hypot(dx,dy);return cast(Math.atan2(dy,dx))>=d-.15}

function reset(){
 player={x:2.5,y:2.5,a:.08,hp:100,ammo:30,kills:0};
 const pts=[[13.4,2.6],[9.5,3.5],[4.4,4.4],[12.3,6.4],[3.2,8.4],[11.8,9.3],[5.5,12.5],[13.1,13.2]];
 enemies=pts.map((p,i)=>({x:p[0],y:p[1],hp:i===7?70:45,alive:true,phase:i*.6,fire:.5+i*.17}));
 elapsed=0;shotCd=0;updateHud();
}
function start(){
 reset();state='playing';show(null);ui.hud.classList.add('active');
 if(matchMedia('(pointer:coarse)').matches)ui.mobile.classList.add('active');
 poki('gameplayStart');
 if(!matchMedia('(pointer:coarse)').matches)canvas.requestPointerLock?.();
}
function finish(win){
 if(state!=='playing')return;
 state='result';poki('gameplayStop');ui.hud.classList.remove('active');ui.mobile.classList.remove('active');
 const score=player.kills*120+Math.max(0,Math.round(900-elapsed*6))+Math.max(0,Math.round(player.hp*4));
 if(score>best){best=score;saveBest(best)}
 ui.resultTitle.textContent=win?'VICTORY':'MISSION FAILED';
 ui.resultEyebrow.textContent=win?'DISTRICT CLEARED':'EXTRACTION LOST';
 ui.resultKills.textContent=player.kills;ui.resultScore.textContent=score;ui.best.textContent=best;
 show(ui.result);document.exitPointerLock?.();
}
function pause(){
 if(state!=='playing')return;state='paused';poki('gameplayStop');show(ui.pause);document.exitPointerLock?.();
}
function resume(){
 if(state!=='paused')return;
 state='playing';show(null);
 poki('commercialBreak').finally(()=>{poki('gameplayStart');if(!matchMedia('(pointer:coarse)').matches)canvas.requestPointerLock?.()});
}
function updateHud(){
 ui.health.textContent=Math.max(0,Math.ceil(player.hp));ui.healthFill.style.width=Math.max(0,player.hp)+'%';
 ui.kills.textContent=player.kills;ui.ammo.textContent=player.ammo;
 const rem=Math.max(0,90-elapsed),m=Math.floor(rem/60),s=Math.floor(rem%60);
 ui.timer.textContent=String(m).padStart(2,'0')+':'+String(s).padStart(2,'0');
}
function damage(n){
 if(state!=='playing')return;player.hp-=n;ui.damage.classList.add('show');
 setTimeout(()=>ui.damage.classList.remove('show'),100);if(player.hp<=0)finish(false);
}
function move(dt){
 let f=(keys.KeyW||keys.ArrowUp?1:0)-(keys.KeyS||keys.ArrowDown?1:0)-stickY;
 let s=(keys.KeyD?1:0)-(keys.KeyA?1:0)+stickX;
 if(keys.ArrowLeft)player.a-=2.2*dt;if(keys.ArrowRight)player.a+=2.2*dt;
 player.a+=lookDelta;lookDelta=0;
 const l=Math.hypot(f,s);if(l>1){f/=l;s/=l}
 const dx=(Math.cos(player.a)*f+Math.cos(player.a+Math.PI/2)*s)*2.7*dt;
 const dy=(Math.sin(player.a)*f+Math.sin(player.a+Math.PI/2)*s)*2.7*dt,r=.23;
 if(!wall(player.x+dx+Math.sign(dx)*r,player.y))player.x+=dx;
 if(!wall(player.x,player.y+dy+Math.sign(dy)*r))player.y+=dy;
}
function updateEnemies(dt){
 for(const e of enemies){
  if(!e.alive)continue;e.phase+=dt*2.2;e.fire-=dt;
  const dx=player.x-e.x,dy=player.y-e.y,d=Math.hypot(dx,dy),visible=los(e);
  if(visible&&d>2.4){
   const sp=.55*dt,nx=e.x+dx/d*sp,ny=e.y+dy/d*sp;
   if(!wall(nx,e.y))e.x=nx;if(!wall(e.x,ny))e.y=ny;
  }
  if(visible&&d<8&&e.fire<=0){
   e.fire=1.35+Math.random()*.6;
   if(Math.random()<Math.max(.18,.72-d*.055))damage(d<3?12:7);
  }
 }
}
function fire(){
 if(state!=='playing'||shotCd>0)return;shotCd=.16;
 if(player.ammo<=0){player.ammo=30;updateHud();return}
 player.ammo--;
 let target=null,td=999;
 for(const e of enemies){
  if(!e.alive)continue;
  const dx=e.x-player.x,dy=e.y-player.y,d=Math.hypot(dx,dy),da=Math.abs(norm(Math.atan2(dy,dx)-player.a));
  if(da<Math.min(.13,.42/d)&&d<td&&los(e)){target=e;td=d}
 }
 if(target){
  target.hp-=26;ui.hit.classList.add('show');setTimeout(()=>ui.hit.classList.remove('show'),90);
  if(target.hp<=0){target.alive=false;player.kills++;if(player.kills===enemies.length)setTimeout(()=>finish(true),350)}
 }
 updateHud();
}
function drawWeapon(){
 ctx.save();ctx.translate(W*.5,H*(.84+(shotCd>0?.025:0)));
 ctx.fillStyle='#11191e';ctx.beginPath();ctx.moveTo(-135,90);ctx.lineTo(-70,-12);ctx.lineTo(75,-8);ctx.lineTo(152,92);ctx.closePath();ctx.fill();
 ctx.fillStyle='#39464c';ctx.fillRect(-54,-34,122,42);ctx.fillStyle='#f2a33a';ctx.fillRect(15,-28,42,6);
 ctx.fillStyle='#0a0f12';ctx.fillRect(49,-24,122,14);ctx.fillStyle='#202a30';ctx.fillRect(-18,7,44,78);ctx.restore();
}
function draw(){
 const sky=ctx.createLinearGradient(0,0,0,H*.56);sky.addColorStop(0,'#0b2635');sky.addColorStop(.65,'#5d7885');sky.addColorStop(1,'#d09055');
 ctx.fillStyle=sky;ctx.fillRect(0,0,W,H*.56);ctx.fillStyle='#172028';ctx.fillRect(0,H*.56,W,H*.44);
 ctx.fillStyle='rgba(3,8,11,.22)';
 for(let i=0;i<22;i++){const bw=28+(i%5)*14,bh=40+(i*37)%150,x=(i*71)%W;ctx.fillRect(x,H*.56-bh,bw,bh)}
 const sw=W/RAYS;
 for(let i=0;i<RAYS;i++){
  const rel=(i/RAYS-.5)*FOV,a=player.a+rel,raw=cast(a),d=raw*Math.cos(rel);depth[i]=raw;
  const wh=Math.min(H*1.5,H/(d*.82)),top=H*.56-wh/2,shade=Math.max(24,165-d*8);
  const ax=player.x+Math.cos(a)*raw,ay=player.y+Math.sin(a)*raw,accent=(Math.floor(ax*2)+Math.floor(ay*2))%9===0;
  ctx.fillStyle=accent?'rgb('+Math.min(190,shade+30)+','+Math.min(130,shade*.72)+','+Math.min(70,shade*.42)+')':'rgb('+(shade*.55)+','+(shade*.72)+','+(shade*.78)+')';
  ctx.fillRect(i*sw,top,sw+1,wh);
 }
 const sprites=[];
 for(const e of enemies){if(!e.alive)continue;const dx=e.x-player.x,dy=e.y-player.y,d=Math.hypot(dx,dy),a=norm(Math.atan2(dy,dx)-player.a);if(Math.abs(a)<FOV*.72)sprites.push({e,d,a})}
 sprites.sort((a,b)=>b.d-a.d);
 for(const s of sprites){
  const sx=W/2+(s.a/(FOV/2))*W/2,size=Math.min(H*.8,620/s.d),ri=Math.max(0,Math.min(RAYS-1,Math.floor(sx/W*RAYS)));
  if(depth[ri]<s.d-.35)continue;
  const x=sx-size*.32,y=H*.56-size*.62+Math.sin(s.e.phase)*size*.02;
  ctx.fillStyle='rgba(0,0,0,.22)';ctx.beginPath();ctx.ellipse(sx,H*.56+size*.11,size*.3,size*.09,0,0,Math.PI*2);ctx.fill();
  ctx.fillStyle=s.e.hp<25?'#bd514c':'#c9d6d9';ctx.fillRect(x+size*.12,y+size*.2,size*.4,size*.52);
  ctx.fillStyle='#222b31';ctx.fillRect(x+size*.18,y,size*.28,size*.23);ctx.fillStyle='#f2a33a';ctx.fillRect(x+size*.39,y+size*.07,size*.055,size*.035);
  ctx.fillStyle='#29353b';ctx.fillRect(x,y+size*.26,size*.16,size*.12);ctx.fillRect(x+size*.48,y+size*.26,size*.16,size*.12);
  ctx.fillStyle='#192126';ctx.fillRect(x+size*.16,y+size*.68,size*.13,size*.25);ctx.fillRect(x+size*.36,y+size*.68,size*.13,size*.25);
 }
 drawWeapon();
}
function loop(now){
 const dt=Math.min(.04,(now-last)/1000);last=now;
 if(state==='playing'){elapsed+=dt;shotCd=Math.max(0,shotCd-dt);move(dt);updateEnemies(dt);updateHud();if(elapsed>=90)finish(player.kills===enemies.length)}
 if(state!=='loading')draw();requestAnimationFrame(loop);
}

document.addEventListener('keydown',e=>{keys[e.code]=true;if(e.code==='Escape')pause();if(e.code==='KeyR'&&state==='playing'){player.ammo=30;updateHud()}});
document.addEventListener('keyup',e=>keys[e.code]=false);
canvas.addEventListener('mousedown',e=>{if(e.button===0)fire()});
document.addEventListener('mousemove',e=>{if(state==='playing'&&document.pointerLockElement===canvas)lookDelta+=e.movementX*.0023});
canvas.addEventListener('click',()=>{if(state==='playing'&&!matchMedia('(pointer:coarse)').matches)canvas.requestPointerLock?.()});
document.getElementById('startBtn').onclick=start;
document.getElementById('pauseBtn').onclick=pause;
document.getElementById('resumeBtn').onclick=resume;
document.getElementById('quitBtn').onclick=()=>{state='menu';poki('gameplayStop');ui.hud.classList.remove('active');ui.mobile.classList.remove('active');show(ui.menu)};
document.getElementById('resultMenuBtn').onclick=()=>{state='menu';show(ui.menu)};
document.getElementById('retryBtn').onclick=()=>{poki('commercialBreak').finally(start)};
document.getElementById('fireBtn').addEventListener('pointerdown',e=>{e.preventDefault();fire()});

const stickZone=document.getElementById('stickZone'),knob=document.querySelector('#stick i');let stickId=null;
function updateStick(e){const r=stickZone.getBoundingClientRect(),cx=r.left+r.width/2,cy=r.top+r.height/2;let dx=(e.clientX-cx)/(r.width*.34),dy=(e.clientY-cy)/(r.height*.34),l=Math.hypot(dx,dy);if(l>1){dx/=l;dy/=l}stickX=dx;stickY=dy;knob.style.transform='translate('+(dx*28)+'px,'+(dy*28)+'px)'}
stickZone.addEventListener('pointerdown',e=>{stickId=e.pointerId;stickZone.setPointerCapture(e.pointerId);updateStick(e)});
stickZone.addEventListener('pointermove',e=>{if(e.pointerId===stickId)updateStick(e)});
function clearStick(e){if(e.pointerId===stickId){stickId=null;stickX=stickY=0;knob.style.transform='translate(0,0)'}}
stickZone.addEventListener('pointerup',clearStick);stickZone.addEventListener('pointercancel',clearStick);

const lookZone=document.getElementById('lookZone');let lookId=null,lastLookX=0;
lookZone.addEventListener('pointerdown',e=>{lookId=e.pointerId;lastLookX=e.clientX;lookZone.setPointerCapture(e.pointerId)});
lookZone.addEventListener('pointermove',e=>{if(e.pointerId===lookId){lookDelta+=(e.clientX-lastLookX)*.0042;lastLookX=e.clientX}});
function clearLook(e){if(e.pointerId===lookId)lookId=null}
lookZone.addEventListener('pointerup',clearLook);lookZone.addEventListener('pointercancel',clearLook);

async function boot(){
 ui.loadFill.style.width='28%';
 try{await(window.PokiSDK?.init?.()||Promise.resolve())}catch(_){}
 ui.loadFill.style.width='72%';await new Promise(r=>setTimeout(r,180));ui.loadFill.style.width='100%';
 poki('gameLoadingFinished');reset();state='menu';setTimeout(()=>show(ui.menu),160);
}
reset();requestAnimationFrame(loop);boot();
})();