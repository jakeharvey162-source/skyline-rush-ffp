"""Original Skyline Rush blockout, MAPZ 56 / game protocol 282.
The layout and encoder are original. Format reference: engine/worldio.cpp.
Run from repository root. No third-party Python packages required.
"""
from pathlib import Path
import struct, gzip, json, argparse
parser=argparse.ArgumentParser()
parser.add_argument('--district', choices=['heights','lagoon','rift'], default='heights')
district=parser.parse_args().district
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'maps/skyline'
OUT.mkdir(parents=True,exist_ok=True)
S=1024
# Each box is x,y,z,width,depth,height,texture. All geometry is on an 8-unit grid.
boxes=[]
def box(x,y,z,w,d,h,t=2): boxes.append((x,y,z,w,d,h,t))
# Low street plane, six connected rooftops and wide crossings.
box(0,0,0,1024,1024,16,1)
for x,y in [(256,256),(448,256),(640,256),(256,544),(448,544),(640,544)]:
 box(x,y,16,128,160,240,2)
 box(x,y,248,128,160,8,3)
for y in (304,592):
 box(384,y,240,64,32,16,4); box(576,y,240,64,32,16,4)
for x in (304,496,688): box(x,416,240,32,128,16,4)
# Cover and two-sided wall-running lanes. Leave spawn pads and routes unobstructed.
for x in (272,464,656):
 for y in (272,640): box(x,y,256,32,32,24,5)
for y in (352,568):
 box(456,y,256,8,32,48,2); box(560,y,256,8,32,48,2)
# Distinctive distant skyline, deliberately outside the combat loop.
for x,y,w,d,h in [(80,80,80,80,352),(160,800,80,80,304),(824,152,96,96,400),(816,792,112,64,328),(464,80,64,64,424)]:
 box(x,y,16,w,d,h,2)
 box(x+16,y+16,16+h,32,32,32,5)
# Districts share modular materials but use different combat topology.
if district != 'heights':
 boxes.clear()
 box(0,0,0,1024,1024,16,1)
 box(224,224,16,576,576,240,3)
 if district == 'lagoon':
  for x,y in [(352,352),(544,352),(352,544),(544,544)]:
   box(x,y,256,96,48,48,4)
   box(x,y,304,96,48,8,5)
  for x in (112,864):
   box(x,240,16,32,544,384,2)
   box(x,240,400,48,544,16,5)
 else:
  for x,y in [(336,336),(576,336),(336,576),(576,576)]:
   box(x,y,256,80,80,64,2)
   box(x-8,y-8,320,96,96,8,5)
  for x,y in [(64,96),(848,80),(64,848),(848,848)]:
   box(x,y,16,96,96,448,2)
# Octree serialisation: recurse only where a box boundary intersects a node.
def node(x,y,z,n):
 hits=[b for b in boxes if x<b[0]+b[3] and x+n>b[0] and y<b[1]+b[4] and y+n>b[1] and z<b[2]+b[5] and z+n>b[2]]
 if not hits: return struct.pack('<B6H',1,*([0]*6))
 last=hits[-1]
 full=all((a>=b and a+n<=b+s) for a,b,s in zip((x,y,z),last[:3],last[3:6]))
 if full or n==8: return struct.pack('<B6H',2,*([last[6]]*6))
 half=n//2
 return b'\0'+b''.join(node(x+(i&1)*half,y+((i>>1)&1)*half,z+((i>>2)&1)*half,half) for i in range(8))
def svar(k,v):
 k=k.encode();v=v.encode();return struct.pack('<i',len(k))+k+b'\0'+struct.pack('<ii',2,len(v))+v+b'\0'
spawns=[(320,336,264,0),(512,336,264,0),(704,336,264,0),(320,624,264,180),(512,624,264,180),(704,624,264,180)]
if district != 'heights':
 spawns=[(272,272,264,45),(432,272,264,90),(592,272,264,90),(752,272,264,135),
         (272,752,264,315),(432,752,264,270),(592,752,264,270),(752,752,264,225)]
else:
 spawns.extend([(320,384,264,0),(704,672,264,180)])
entities=[]
for x,y,z,yaw in spawns:
 attrs=[0,yaw,0,0,0,0,0]
 entities.append(struct.pack('<3fB3x',x,y,z,3)+struct.pack('<i7ii',7,*attrs,0))
titles={'heights':'Braamfontein Heights','lagoon':'Lagos Lagoon Terminal','rift':'Rift Valley Relay'}
# Onslaught-only actors, using upstream models. The elite is not a scripted boss.
actor_positions = [(320,320,272),(512,320,272),(704,320,280),(320,608,272),(512,608,272),(704,608,272)] if district=='heights' else [(304,448,272),(512,304,272),(720,448,280),(304,640,272),(512,704,272),(720,640,272)]
for i,(x,y,z) in enumerate(actor_positions):
 attrs=[i%4,180,0,1,256,0,0,600 if i==5 else 100,80 if i==5 else 100,150 if i==5 else 100,0]
 if i==5: attrs[0]=0
 entities.append(struct.pack('<3fB3x',x,y,z,16)+struct.pack('<i11ii',11,*attrs,0))
variables=[svar('maptitle','Skyline Rush / '+titles[district]),svar('mapauthor','Jake Harvey / Skyline Rush'),svar('mapdesc','Fictional African-inspired district. PvP arena and Onslaught enemy encounters. Development blockout.')]
raw=struct.pack('<4s9i4s',b'MAPZ',56,44,S,len(entities),0,0,6,282,1,b'fps\0')
raw+=struct.pack('<i',len(variables))+b''.join(variables)+struct.pack('<H',0)+b''.join(entities)
raw+=struct.pack('<i',-6) # six unchanged texture slots
raw+=b''.join(node((i&1)*512,((i>>1)&1)*512,((i>>2)&1)*512,512) for i in range(8))
(OUT/(district+'.mpz')).write_bytes(gzip.compress(raw,mtime=0))
# Conservative bot routes stay on rooftops and crossings. No links across a gap.
points=[]
for y in (320,608):
 for x in range(288,737,16): points.append((x,y,257))
for x in (320,512,704):
 for y in range(336,593,16): points.append((x,y,257))
if district != 'heights':
 def clear(x,y):
  return not any(a-8<=x<a+w+8 and b-8<=y<b+d+8 and c<=264<c+h for a,b,c,w,d,h,t in boxes)
 points=[(x,y,257) for y in range(256,769,16) for x in range(256,769,16) if clear(x,y)]
points=list(dict.fromkeys(points)); ids={p:i+1 for i,p in enumerate(points)}
wp=b'RWPT'+struct.pack('<iH',1,len(points))
for x,y,z in points:
 links=[ids[p] for p in [(x-16,y,z),(x+16,y,z),(x,y-16,z),(x,y+16,z)] if p in ids]
 wp+=struct.pack('<3fiB',x,y,z,1,len(links))+struct.pack('<'+'H'*len(links),*links)
(OUT/(district+'.wpt')).write_bytes(gzip.compress(wp,mtime=0))
(OUT/('layout.json' if district=='heights' else district+'-layout.json')).write_text(json.dumps({'worldsize':S,'boxes':boxes,'spawns':spawns,'waypoints':points,'actors':actor_positions,'title':titles[district]},indent=2)+'\n')
print(f'Wrote arena: {len(boxes)} boxes, {len(spawns)} spawns, {len(points)} bot waypoints')
