import numpy as np, os, math
from PIL import Image, ImageEnhance
def in_d(x,y,W=32,H=16):
    if x<0 or y<0 or x>=W or y>=H: return False
    k=y if y<H//2 else H-1-y
    rw=4*(k+1); c=W//2
    return c-rw//2<=x<c+rw//2
def make(seed):
    rng=np.random.RandomState(seed)
    cx,cy=15.5,7.5
    # irregular radius via angular noise
    ph=rng.rand(4)*6.28
    def rad(ang,base,amp): return base*(1+amp*(math.sin(2*ang+ph[0])*0.5+math.sin(3*ang+ph[1])*0.35+math.sin(5*ang+ph[2])*0.2))
    pts=np.array([[cx+rng.uniform(-8.5,8.5),cy+rng.uniform(-4.2,4.2)] for _ in range(11)])
    shade=rng.uniform(-14,14,len(pts))
    im=np.zeros((16,32,4),np.uint8)
    for y in range(16):
        for x in range(32):
            if not in_d(x,y): continue
            dx,dy=(x-cx),(y-cy)*2.0   # undo 2:1 squash
            ang=math.atan2(dy,dx); dist=math.hypot(dx,dy)
            Ro=rad(ang,9.6,0.07); Rh=rad(ang+1.0,5.6,0.12)
            if dist<Rh:
                n=rng.rand()
                col=(20,14,11)
                if n>0.86: col=(58,44,36)
                elif n>0.7: col=(34,25,20)
                im[y,x]=(*col,250)
                if y<cy-1: im[y,x]=(10,7,6,252)                       # far wall in shadow
            elif dist<Ro:
                d=np.hypot(pts[:,0]-x,(pts[:,1]-y)*2.0); o=np.argsort(d)
                crack=(d[o[1]]-d[o[0]])<1.5
                base=np.array([104,82,68])+shade[o[0]]
                if dx+dy<0: base=base+10                                 # lit from upper left
                lower = y>cy+1 and dist<Rh+3
                if lower: base=base+12
                if dist<Rh+1.6: base=base-24                              # inner rim shadow
                if crack: base=np.array([66,51,43])
                base=base+rng.uniform(-6,6)
                im[y,x]=(*np.clip(base,0,255).astype(np.uint8),240)
            elif dist<Ro+2.6 and rng.rand()<0.30:
                c=np.array([128,106,92])+rng.uniform(-14,14)
                im[y,x]=(*np.clip(c,0,255).astype(np.uint8),int(rng.uniform(110,190)))
    return Image.fromarray(im)
os.makedirs('fx/Tiles',exist_ok=True)
for i,sd in enumerate((3,11,27),1): make(sd).save(f'fx/Tiles/dug_{i}.png')
bg=Image.new('RGBA',(130,24),(140,120,50,255))
for i in range(3): bg.alpha_composite(Image.open(f'fx/Tiles/dug_{i+1}.png'),(2+i*42,4))
bg.resize((1040,192),Image.NEAREST).save('dug_prev.png')
