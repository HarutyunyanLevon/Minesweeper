import numpy as np, random
from PIL import Image, ImageFilter
RAW='/mnt/user-data/uploads/Minesweeper/RawDownloads/'
pal=np.array([[int(l[i:i+2],16) for i in (0,2,4)] for l in open('/mnt/user-data/outputs/Art/palette.hex').read().split()],float)
def snap(rgb):
    d=((rgb[...,None,:]-pal[None,None,:,:])**2).sum(-1)
    return pal[d.argmin(-1)].astype(np.uint8)

def seamless(img):
    a=np.asarray(img.convert('RGB'),float); h,w,_=a.shape
    # offset blend: cross-fade image with its half-shifted copy using a smooth window
    s=np.roll(np.roll(a,h//2,0),w//2,1)
    y=np.abs(np.linspace(-1,1,h))[:,None]; x=np.abs(np.linspace(-1,1,w))[None,:]
    wgt=(1-y)*(1-x); wgt=wgt[...,None]  # center weight of original, edges from shifted
    wgt=np.clip(wgt*1.6,0,1)
    return a*wgt+s*(1-wgt)

def flatten(a,sigma=9):
    im=Image.fromarray(a.astype(np.uint8))
    # wrap-aware blur: tile 3x3, blur, crop
    h,w,_=a.shape
    big=Image.new('RGB',(w*3,h*3))
    for i in range(3):
        for j in range(3): big.paste(im,(j*w,i*h))
    bl=np.asarray(big.filter(ImageFilter.GaussianBlur(sigma)),float)[h:2*h,w:2*w]
    m=a.reshape(-1,3).mean(0)
    return a-0.65*(bl-m)-(m-np.array([118,100,44.]))*0.0

def sample_tile(tex,ox,oy,s=22.63):
    h,w,_=tex.shape
    out=np.zeros((16,32,4),np.uint8)
    for py in range(16):
        for px in range(32):
            dx=px+0.5-16; dy=py+0.5-8
            if abs(dx)/16+abs(dy)/8>1.0: continue
            xp=(dx/16+dy/8)/2*s; yp=(dx/16-dy/8)/2*s
            u=(ox+xp)%w; v=(oy+yp)%h
            x0=int(u)%w; y0=int(v)%h; x1=(x0+1)%w; y1=(y0+1)%h; fx=u-int(u); fy=v-int(v)
            c=(tex[y0,x0]*(1-fx)*(1-fy)+tex[y0,x1]*fx*(1-fy)+tex[y1,x0]*(1-fx)*fy+tex[y1,x1]*fx*fy)
            out[py,px,:3]=np.clip(c,0,255); out[py,px,3]=255
    out[...,:3]=snap(out[...,:3].astype(float))
    return out

if __name__=='__main__':
    g=Image.open(RAW+'grass_raw.png').convert('RGB')
    g=g.crop((60,50,162,152)); gt=flatten(seamless(g),8)
    d=Image.open(RAW+'dirt_raw.png').convert('RGBA')
    dirt_base=np.array([100,66,38],float)
    da=np.asarray(d,float); dt=da[...,:3]*(da[...,3:4]/255)+dirt_base*(1-da[...,3:4]/255)
    dtex=seamless(Image.fromarray(dt.astype(np.uint8)))
    tiles={'grass_a':sample_tile(gt,10,20),'grass_b':sample_tile(gt,60,60),'grass_c':sample_tile(gt,30,80),'grass_d':sample_tile(gt,80,30),
           'dirt':sample_tile(dtex,51,101),'dirt_b':sample_tile(dtex,101,31),'border':sample_tile(dtex*0.8,121,111)}
    import os; os.makedirs('out',exist_ok=True)
    for k,v in tiles.items(): Image.fromarray(v).save('out/%s.png'%k)
    # preview: iso field
    rnd=random.Random(3); names=['grass_a','grass_b','grass_c','grass_d']
    W,H=12,10
    canvas=Image.new('RGBA',(32*(W+H)//2+32,16*(W+H)//2+16),(20,14,8,255))
    for x in range(W):
        for y in range(H):
            n=rnd.choice(names) if rnd.random()>0.2 else rnd.choice(['dirt','dirt_b'])
            t=Image.fromarray(tiles[n])
            cx=(x-y)*16+ (H*16); cy=(x+y)*8
            canvas.paste(t,(cx,cy),t)
    canvas.resize((canvas.width*4,canvas.height*4),Image.NEAREST).save('preview_tiles.png')
