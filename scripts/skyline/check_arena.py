"""Validate the generated map format and physical path constraints."""
from pathlib import Path
import gzip,struct,json,collections,sys
district=sys.argv[1] if len(sys.argv)>1 else "heights"
root=Path(__file__).resolve().parents[2];d=root/'maps/skyline'
b=gzip.decompress((d/(district+'.mpz')).read_bytes());off=0
def get(fmt):
 global off
 v=struct.unpack_from(fmt,b,off);off+=struct.calcsize(fmt);return v
magic,ver,size,world,nents,pvs,blend,slots,game,rev,gid=get('<4s9i4s')
assert (magic,ver,size,world,game,gid)==(b'MAPZ',56,44,1024,282,b'fps\0')
for _ in range(get('<i')[0]):
 n=get('<i')[0]; off+=n+1; kind=get('<i')[0];assert kind==2
 n=get('<i')[0];off+=n+1
n=get('<H')[0];off+=n*2
for _ in range(nents):
 x,y,z,t=get('<3fB3x');assert t in (3,16)
 n=get('<i')[0];assert n==(7 if t==3 else 11);off+=n*4;assert get('<i')[0]==0
assert get('<i')[0]==-slots
leaves=0
def cube(depth=0):
 global leaves
 assert depth<=7
 kind=get('<B')[0]
 if kind==0:
  for _ in range(8):cube(depth+1)
 else:
  assert kind in (1,2);assert all(0<=t<slots for t in get('<6H'));leaves+=1
for _ in range(8):cube()
assert off==len(b),(off,len(b))
layout=json.loads((d/('layout.json' if district=='heights' else district+'-layout.json')).read_text());boxes=layout['boxes']
def solid(x,y,z):return any(a<=x<a+w and c<=y<c+h and e<=z<e+l for a,c,e,w,h,l,t in boxes)
for x,y,z,yaw in layout['spawns']:
 assert not solid(x,y,z) and solid(x,y,255)
for x,y,z in layout.get('actors',[]):
 assert not solid(x,y,z) and solid(x,y,255), (district,'actor inside geometry')
points=[tuple(p) for p in layout['waypoints']]; pointset=set(points)
for x,y,z in points:
 assert solid(x,y,255)
 for dx,dy in [(0,0),(4,0),(-4,0),(0,4),(0,-4)]:
  assert not solid(x+dx,y+dy,264),(x,y,'route intersects cover')
seen={points[0]};q=collections.deque(seen)
while q:
 x,y,z=q.popleft()
 for p in [(x-16,y,z),(x+16,y,z),(x,y-16,z),(x,y+16,z)]:
  if p in pointset and p not in seen:seen.add(p);q.append(p)
assert len(seen)==len(points),'Disconnected navigation grid'
print(f'PASS MAPZ: {leaves} leaves, {len(layout['spawns'])} clear spawns, exact byte consumption')
print(f'PASS navigation: {len(points)} connected points with floor and cover clearance')
print('Rendering, movement and bot behavior still require client playtesting.')
