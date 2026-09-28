"""承認済みHTMLの図形を再現する、計画画面専用の再生成可能なUI素材。"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance
import math

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Art/UI'
OUT.mkdir(parents=True, exist_ok=True)

def save(name, im):
    im.save(OUT / (name + '.png'))

def rounded(name, radius, top_only=False):
    s = 4
    im = Image.new('RGBA', (128*s, 128*s))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, 128*s-1, 128*s-1), radius=radius*s, fill='white')
    if top_only: d.rectangle((0, 64*s, 128*s-1, 128*s-1), fill='white')
    save(name, im.resize((128,128), Image.Resampling.LANCZOS))

for r in (12,16,20,24,28): rounded('round-'+str(r),r)
rounded('stage-top',28,True)
bubble=Image.new('RGBA',(512,512));bd=ImageDraw.Draw(bubble)
bd.rounded_rectangle((0,0,511,511),radius=256,fill='white')
bd.rectangle((0,256,256,456),fill='white');bd.rectangle((56,456,256,511),fill='white');bd.ellipse((0,400,112,512),fill='white')
save('marker-bubble',bubble.resize((128,128),Image.Resampling.LANCZOS))
shadow = Image.new('RGBA',(192,192))
ImageDraw.Draw(shadow).rounded_rectangle((28,24,164,160),24,fill=(27,35,64,65))
save('soft-shadow',shadow.filter(ImageFilter.GaussianBlur(12)))
office = Image.open(ROOT/'Assets/Art/Office/office-topdown.png').convert('RGBA')
save('office-blur',ImageEnhance.Color(office.resize((900,900))).enhance(1.2).filter(ImageFilter.GaussianBlur(7)))

def gradient(name, w, h, fn):
    im=Image.new('RGBA',(w,h)); im.putdata([fn(x,y,w,h) for y in range(h) for x in range(w)]); save(name,im)
gradient('planning-gradient',400,225,lambda x,y,w,h: tuple(round(a+(b-a)*(.25*x/(w-1)+.75*y/(h-1))) for a,b in zip((207,233,255,255),(255,227,236,255))))
gradient('stage-shade',4,300,lambda x,y,w,h:(27,35,64,round(140*y/(h-1))))
gradient('button-shine',128,128,lambda x,y,w,h:(255,255,255,round(140*max(0,1-abs((x+y*.18)/w-.55)*3))))
ribbon=Image.new('RGBA',(300,48)); ImageDraw.Draw(ribbon).polygon([(0,0),(300,0),(280,48),(0,48)],fill='white'); save('ribbon',ribbon)
tail=Image.new('RGBA',(56,80)); ImageDraw.Draw(tail).polygon([(56,0),(0,40),(56,80)],fill='white'); save('speech-tail',tail)
petal=Image.new('RGBA',(56,40)); ImageDraw.Draw(petal).ellipse((0,0,55,39),fill='#ffc4d6'); save('petal',petal)
snow=Image.new('RGBA',(32,32)); ImageDraw.Draw(snow).ellipse((4,4,27,27),fill='white');save('snow',snow)
face=Image.open(ROOT/'Assets/Sprites/Hinata/hinata_normal.png').convert('RGBA')
cast=Image.new('RGBA',face.size,(27,35,64,0)); cast.putalpha(face.getchannel('A').point(lambda a: round(a*.35)))
save('hinata-shadow',cast.filter(ImageFilter.GaussianBlur(face.width*18/470)))

# モックのSVGと同じ24単位の線・形。4倍解像度から縮小して輪郭を整える。
def icon(name,color,kind):
    s=8; im=Image.new('RGBA',(24*s,24*s)); d=ImageDraw.Draw(im)
    def line(points,width=2.4):
        p=[(round(x*s),round(y*s)) for x,y in points]; d.line(p,fill=color,width=round(width*s),joint='curve')
        for x,y in p: d.ellipse((x-width*s/2,y-width*s/2,x+width*s/2,y+width*s/2),fill=color)
    def ellipse(box,width=2.4,fill=None):d.ellipse(tuple(round(v*s) for v in box),fill=fill,outline=color,width=round(width*s))
    if kind=='audit': ellipse((4,4,17,17));line([(15.5,15.5),(21,21)])
    elif kind=='listen':line([(4,5),(20,5),(20,15),(9,15),(4,19),(4,5)])
    elif kind=='map':
        for y in (5,12,19):line([(9,y),(20,y)]);line([(3,y),(4.5,y+1.5),(7,y-1)])
    elif kind=='rest':
        line([(5,10),(16,10),(16,15),(15,18),(12,20),(9,20),(6,18),(5,15),(5,10)])
        line([(16,11),(18,11),(20,12),(20,15),(18,16),(16,16)]);line([(9,3),(9,6)]);line([(12,3),(12,6)])
    elif kind=='upgrade':line([(3,8),(12,3),(21,8),(21,16),(12,21),(3,16),(3,8),(12,13),(21,8)]);line([(12,13),(12,21)])
    elif kind=='menu':
        for y in (6,12,18):line([(4,y),(20,y)],2.2)
    elif kind=='tool':line([(14.7,6.3),(11,6),(8.5,8),(9.3,11.7),(3,18),(6,21),(12.3,14.7),(16,15.5),(19,12),(17.7,9.3),(15.2,11.8),(12.7,11.3),(12.2,8.8),(14.7,6.3)])
    elif kind=='star':
        p=[(12,2.5),(14.9,8.5),(21.5,9.4),(16.7,14),(17.9,20.5),(12,17.4),(6.1,20.5),(7.3,14),(2.5,9.4),(9.1,8.5)]
        vertices=[(int(x*s),int(y*s)) for x,y in p]; d.polygon(vertices,fill=color)
        d.line(vertices+[vertices[0]],fill='#b8c1d3' if name=='star-muted' else '#e0a800',width=round(1.5*s),joint='curve')
    elif kind=='morale':
        ellipse((2,2,22,22),2)
        for x in (8,16):d.ellipse(((x-1)*s,8*s,(x+1)*s,10*s),fill=color)
        d.arc((6*s,9*s,18*s,18*s),0,180,fill=color,width=2*s)
    save('icon-'+name,im.resize((96,96),Image.Resampling.LANCZOS))
for name,col in [('audit','#3fa9f5'),('listen','#ff6f91'),('map','#8e7cc3'),('rest','#2ec4a0'),('upgrade','#4a3200'),('menu','#1d2a44'),('tool','#ffffff'),('star','#ffd23f'),('morale','#2ec4a0')]:icon(name,col,name)
icon('star-muted','#dfe5f0','star')
print('Planning UI assets:',OUT)
