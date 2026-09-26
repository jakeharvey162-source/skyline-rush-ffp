"""Generate original geometric game branding and tile textures (Pillow required)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'data/skyline'
OUT.mkdir(parents=True,exist_ok=True)
fontpath='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
def font(n):
 try: return ImageFont.truetype(fontpath,n)
 except OSError: return ImageFont.load_default(size=n)
def emblem(n):
 im=Image.new('RGBA',(n,n),(10,18,30,255)); d=ImageDraw.Draw(im); u=n/128
 pts=[(22,95),(22,55),(39,55),(39,29),(56,29),(56,70),(72,70),(72,42),(89,42),(89,18),(106,18),(106,95)]
 d.polygon([(x*u,y*u) for x,y in pts],fill='#48dec3')
 d.line([(20*u,110*u),(108*u,110*u)],fill='#f4ba6b',width=max(1,int(6*u)))
 return im
icon=emblem(256)
for name in ['emblem','icon']: icon.save(OUT/(name+'.png'))
icon.save(ROOT/'src/skyline.ico',sizes=[(16,16),(32,32),(48,48),(256,256)])
logo=Image.new('RGBA',(1024,512),(0,0,0,0)); logo.alpha_composite(emblem(192),(416,36)); d=ImageDraw.Draw(logo)
d.text((512,282),'SKYLINE RUSH',font=font(80),fill='#f5f3eb',anchor='mm')
d.text((512,366),'OWN THE HIGH GROUND',font=font(26),fill='#48dec3',anchor='mm')
for name in ['logo','logocrop']: logo.save(OUT/(name+'.png'))
for name,bg,line in [('street','#202932','#303b46'),('concrete','#596674','#79848e'),('roof','#aeb5b8','#929ba2'),('bridge','#1d706d','#49dbc2'),('cover','#bd803e','#e8b368')]:
 im=Image.new('RGB',(256,256),bg);d=ImageDraw.Draw(im)
 for v in range(0,257,64): d.line([(v,0),(v,256)],fill=line,width=2);d.line([(0,v),(256,v)],fill=line,width=2)
 if name=='bridge': d.rectangle((4,4,251,251),outline='#e9b762',width=8)
 im.save(OUT/(name+'.png'))
print('Brand and texture assets generated')
