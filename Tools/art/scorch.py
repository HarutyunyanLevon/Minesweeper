import numpy as np, math, os
from PIL import Image
def in_d(x,y,W=32,H=16):
    if x<0 or y<0 or x>=W or y>=H: return False
    k=y if y<H//2 else H-1-y
    rw=4*(k+1); c=W//2
    return c-rw//2<=x<c+rw//2
rng=np.random.RandomState(5)
cx,cy=15.5,7.5
ph=rng.rand(4)*6.28
def rad(a,b,amp): return b*(1+amp*(math.sin(2*a+ph[0])*.5+math.sin(3*a+ph[1])*.35+math.sin(5*a+ph[2])*.2))
pts=np.array([[cx+rng.uniform(-10,10),cy+rng.uniform(-5,5)] for _ in range(12)])
im=np.zeros((16,32,4),np.uint8)
for y in range(16):
    for x in range(32):
        if not in_d(x,y): continue
        dx,dy=x-cx,(y-cy)*2.0
        ang=math.atan2(dy,dx); dist=math.hypot(dx,dy)
        Rh=rad(ang+1,6.4,.12); Rr=rad(ang,10.4,.07); Rs=rad(ang+2,13.5,.15)
        if dist<Rh:
            n=rng.rand(); col=(14,10,9)
            if n>.9: col=(60,30,18)      # dying embers
            elif n>.75: col=(30,22,18)
            if y<cy-1: col=(8,6,5)
            im[y,x]=(*col,252)
        elif dist<Rr:
            d=np.hypot(pts[:,0]-x,(pts[:,1]-y)*2.0); o=np.argsort(d)
            crack=(d[o[1]]-d[o[0]])<1.4
            base=np.array([58,46,40])+rng.uniform(-6,6)       # charred earth
            if dx+dy<0: base+=8
            if dist<Rh+1.6: base-=18
            if crack: base=np.array([32,25,22])
            im[y,x]=(*np.clip(base,0,255).astype(np.uint8),245)
        elif dist<Rs:
            t=(dist-Rr)/(Rs-Rr)
            if rng.rand()<0.75*(1-t):                           # soot spray fading out
                v=rng.uniform(26,48)
                im[y,x]=(int(v),int(v*.82),int(v*.72),int(rng.uniform(120,210)*(1-t*.6)))
os.makedirs('fx/Tiles',exist_ok=True)
Image.fromarray(im).save('fx/Tiles/crater.png')
bg=Image.new('RGBA',(100,24),(140,120,50,255))
for i,f in enumerate(['dug_1','crater']): bg.alpha_composite(Image.open(f'fx/Tiles/{f}.png'),(2+i*40,4))
bg.resize((800,192),Image.NEAREST).save('scorch_prev.png')
