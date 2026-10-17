import numpy as np, math, random, os
from PIL import Image

OUT='fx'
os.makedirs(OUT+'/Radar',exist_ok=True); os.makedirs(OUT+'/Tiles',exist_ok=True)

def hashn(x,y,seed):
    h=(x*374761393+y*668265263+seed*144665)&0xffffffff
    h=((h^(h>>13))*1274126177)&0xffffffff
    return ((h^(h>>16))&0xffff)/65535.0

def vnoise(x,y,seed,scale):
    # blocky value noise
    return hashn(int(x//scale),int(y//scale),seed)

def in_diamond(x,y,W=32,H=16):
    if x<0 or y<0 or x>=W or y>=H: return False
    half=H//2
    k=y if y<half else H-1-y
    rw=4*(k+1)
    c=W//2
    return c-rw//2<=x<c+rw//2

# ---------------- digits
G=[
 ["01110","10001","10011","10101","11001","10001","01110"],
 ["00100","01100","00100","00100","00100","00100","01110"],
 ["01110","10001","00001","00010","00100","01000","11111"],
 ["11110","00001","00001","01110","00001","00001","11110"],
 ["00010","00110","01010","10010","11111","00010","00010"],
 ["11111","10000","11110","00001","00001","10001","01110"],
 ["00110","01000","10000","11110","10001","10001","01110"],
 ["11111","00001","00010","00100","01000","01000","01000"],
 ["01110","10001","10001","01110","10001","10001","01110"],
]
def gl(d,gx,gy):  # gy from bottom
    if gx<0 or gx>4 or gy<0 or gy>6: return False
    return G[d][6-gy][gx]=='1'
for d in range(9):
    im=np.zeros((10,8,4),np.uint8)
    for r in range(10):
        y=9-r
        for x in range(8):
            if gl(d,x-1,y-1):
                # light top, slightly grey bottom so tint keeps depth
                gy=y-1
                v=255 if gy>=4 else (236 if gy>=2 else 214)
                im[r,x]=(v,v,v,255)
            else:
                outline=any((dx or dy) and gl(d,x-1+dx,y-1+dy) for dx in (-1,0,1) for dy in (-1,0,1))
                shadow=gl(d,x-2,y)  # glyph pixel up-left -> shadow down-right
                if outline: im[r,x]=(24,17,12,255)
                elif shadow: im[r,x]=(24,17,12,150)
    Image.fromarray(im).save(f'{OUT}/Radar/digit_{d}.png')

# ---------------- cursor 32x16
cur=np.zeros((16,32,4),np.uint8)
for y in range(16):
    for x in range(32):
        if not in_diamond(x,y): continue
        edge=not(in_diamond(x-1,y) and in_diamond(x+1,y) and in_diamond(x,y-1) and in_diamond(x,y+1))
        if edge:
            # dashes: leave a gap in the middle of each side
            t=(x+y*2)%8
            a=235 if t<6 else 0
            cur[y,x]=(255,238,190,a) if a else (0,0,0,0)
        else:
            cur[y,x]=(255,238,190,26)
# corners brighter
for (x,y) in [(15,0),(16,0),(0,7),(0,8),(31,7),(31,8),(15,15),(16,15)]:
    cur[y,x]=(255,250,225,255)
Image.fromarray(cur).save(f'{OUT}/Tiles/cursor.png')

# ---------------- probed overlay 32x16: trodden earth shade
pr=np.zeros((16,32,4),np.uint8)
for y in range(16):
    for x in range(32):
        if not in_diamond(x,y): continue
        n=vnoise(x,y,5,2)
        a=62+int((n-0.5)*34)
        edge=not(in_diamond(x-1,y) and in_diamond(x+1,y) and in_diamond(x,y-1) and in_diamond(x,y+1))
        if edge: a=34
        col=(34,24,12)
        if hashn(x,y,9)>0.965: col=(150,120,76); a=70   # pebble
        pr[y,x]=(*col,a)
Image.fromarray(pr).save(f'{OUT}/Tiles/probed.png')

# ---------------- crater 32x16
cr=np.zeros((16,32,4),np.uint8)
for y in range(16):
    for x in range(32):
        if not in_diamond(x,y): continue
        dx=(x+0.5-16)/16.5; dy=(y+0.5-8)/8.2
        d=abs(dx)+abs(dy)
        d=0.5*d+0.5*math.hypot(dx,dy)   # blend diamond/ellipse for a rounder hole
        n=(hashn(x,y,41)-0.5)*14
        lit=(-(dx)+(-dy))   # lighter rim on upper-left
        if d<=0.45:   col=(26+n,18+n,13+n,255)
        elif d<=0.66: col=(48+n,33+n,23+n,255)
        elif d<=0.80:
            base=122+int(lit*14)
            col=(base+n,int(base*0.76)+n,int(base*0.45)+n,255)    # dirt rim
        elif d<=0.92: col=(60+n,44+n,28+n,170)
        else: col=(40,30,20,70)
        if 0.8<d<1.0 and hashn(x,y,77)>0.9: col=(150,118,72,230)   # flung dirt
        cr[y,x]=tuple(int(max(0,min(255,c))) for c in col)
Image.fromarray(cr).save(f'{OUT}/Tiles/crater.png')

# ---------------- blast frames 48x48 (pivot center = cell center)
S=48
rng=random.Random(12)
clods=[]
for i in range(11):
    ang=rng.uniform(math.radians(25),math.radians(155))
    sp=rng.uniform(3.2,6.0)
    clods.append((math.cos(ang)*sp*rng.choice([-1,1])*0.9, -math.sin(ang)*sp, rng.choice([2,2,3])))
R  =[6,10,13,14,12,10]
CY =[21,19,16,13,11,9]
FIRE=[1.0,0.95,0.7,0.45,0.22,0.0]
SMOKE=[0.0,0.15,0.35,0.6,0.85,1.0]
for t in range(6):
    im=np.zeros((S,S,4),np.uint8)
    cx=24; cy=CY[t]
    mask=np.zeros((S,S),bool); colr=np.zeros((S,S,3),np.uint8)
    for y in range(S):
        for x in range(S):
            n=vnoise(x,y,100+t,2)*0.6+vnoise(x,y,200+t,4)*0.4
            d=math.hypot(x+0.5-cx,(y+0.5-cy)*1.1)
            rr=R[t]*(0.72+0.55*n)
            if d>rr: continue
            heat=(1-d/rr)*FIRE[t]+(n-0.5)*0.25
            if heat>0.72: c=(255,246,200)
            elif heat>0.5: c=(255,205,90)
            elif heat>0.3: c=(244,140,50)
            elif heat>0.14: c=(206,82,36)
            else:
                s=SMOKE[t]
                g=int(58+s*60+(n-0.5)*26)
                c=(g,int(g*0.93),int(g*0.86))
            mask[y,x]=True; colr[y,x]=c
    # ground dust ring (flat ellipse) frames 1-4
    if 1<=t<=4:
        rx=6+t*4; ry=2+t*1.4
        for y in range(S):
            for x in range(S):
                dd=((x+0.5-24)/rx)**2+((y+0.5-27)/ry)**2
                if 0.55<dd<1.0 and hashn(x,y,300+t)>0.35 and not mask[y,x]:
                    mask[y,x]=True; colr[y,x]=(168,136,92) if hashn(x,y,3)>0.5 else (140,110,72)
    # clods
    for (vx,vy,sz) in clods:
        if t==0: continue
        px=24+vx*t*1.1; py=26+vy*t*1.5+0.9*t*t
        for yy in range(sz):
            for xx in range(sz):
                X=int(px)+xx; Y=int(py)+yy
                if 0<=X<S and 0<=Y<S and not (t==5 and (X+Y)%2):
                    mask[Y,X]=True; colr[Y,X]=(134,100,62) if (xx==0 and yy==0) else (76,54,34)
    # dissipate last frame with dither
    if t==5:
        for y in range(S):
            for x in range(S):
                if (x+y)%2==0: mask[y,x]=False
    # outline
    for y in range(S):
        for x in range(S):
            if mask[y,x]:
                im[y,x]=(*colr[y,x],255)
    for y in range(S):
        for x in range(S):
            if not mask[y,x] and t<5:
                for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    X,Y=x+dx,y+dy
                    if 0<=X<S and 0<=Y<S and mask[Y,X] and tuple(colr[Y,X])[0]>0:
                        im[y,x]=(34,24,18,255); break
    Image.fromarray(im).save(f'{OUT}/Tiles/blast_{t+1}.png')

# ---------------- preview montage
bg=(92,76,40,255)
tiles=['cursor','probed','crater']
W=32*3+10*(len(tiles)+1)+48*6+10*7+8*9+10*10
sheet=Image.new('RGBA',(900,140),bg)
x=10
for n in tiles:
    sheet.alpha_composite(Image.open(f'{OUT}/Tiles/{n}.png'),(x,10)); x+=42
x+=10
for i in range(1,7):
    sheet.alpha_composite(Image.open(f'{OUT}/Tiles/blast_{i}.png'),(x,10)); x+=54
x=10
for d in range(9):
    im=Image.open(f'{OUT}/Radar/digit_{d}.png')
    # tint preview
    sheet.alpha_composite(im,(x,80)); x+=14
sheet=sheet.resize((sheet.width*3,sheet.height*3),Image.NEAREST)
sheet.save('fx_preview.png'); print(sheet.size)
