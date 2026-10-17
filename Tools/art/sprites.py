import numpy as np, os
from PIL import Image
R='/mnt/user-data/uploads/Minesweeper/RawDownloads/'
OUT='/mnt/user-data/outputs/Art/final/'
pal=np.array([[int(l[i:i+2],16) for i in (0,2,4)] for l in open('/mnt/user-data/outputs/Art/palette.hex').read().split()],float)

def snap_rgba(im,thresh=110):
    a=np.asarray(im.convert('RGBA')).copy()
    a[...,3]=np.where(a[...,3]>=thresh,255,0)
    a[a[...,3]==0]=0
    return Image.fromarray(a,'RGBA')

def prep(name,scale,body_center_rows=None,snap=True):
    im=Image.open(R+name).convert('RGBA'); im=im.crop(im.getbbox())
    w,h=im.size; nw,nh=max(1,round(w*scale)),max(1,round(h*scale))
    im=im.resize((nw,nh),Image.LANCZOS)
    return snap_rgba(im) if snap else im

def center_body(im,frac=0.5):
    # pad horizontally so the body (upper part) is centered
    a=np.asarray(im)[...,3]>0; h,w=a.shape
    cols=np.where(a[:int(h*frac)].any(0))[0]; cx=(cols.min()+cols.max()+1)/2
    left=int(round(w-2*cx)) if cx<w/2 else 0   # add to left if body is left of center
    right=int(round(2*cx-w)) if cx>w/2 else 0
    out=Image.new('RGBA',(w+max(left,0)+max(right,0),h),(0,0,0,0)); out.paste(im,(max(left,0),0)); return out

os.makedirs(OUT+'Characters',exist_ok=True); os.makedirs(OUT+'Objects/Test',exist_ok=True); os.makedirs(OUT+'Props',exist_ok=True); os.makedirs(OUT+'Tiles/Test',exist_ok=True)
front=center_body(prep('deminer_a.png',0.5)); front.save(OUT+'Characters/deminer_front.png')
back=center_body(prep('deminer_back.png',0.5)); back.save(OUT+'Characters/deminer_back.png')
prep('farmhouse_raw.png',1.0).save(OUT+'Props/farmhouse.png')
prep('tank_raw.png',0.45).save(OUT+'Props/tank.png')
mine=prep('mine_raw.png',0.30); mine.save(OUT+'Objects/Test/mine_debug.png')
f=Image.open(R+'flag_raw.png').convert('RGBA'); f=f.crop(f.getbbox()); sc=20/f.height
flag=snap_rgba(f.resize((max(1,round(f.width*sc)),20),Image.LANCZOS)); flag.save(OUT+'Objects/Test/flag.png')
for n in ['deminer_front','deminer_back']:
    pass
print(front.size,back.size,mine.size,flag.size,Image.open(OUT+'Props/farmhouse.png').size,Image.open(OUT+'Props/tank.png').size)
# preview
items=[front,back,mine,flag,Image.open(OUT+'Props/farmhouse.png'),Image.open(OUT+'Props/tank.png')]
W=sum(i.width*5+20 for i in items); H=max(i.height*5 for i in items)
sh=Image.new('RGBA',(W,H),(100,90,60,255)); x=0
for i in items:
    b=i.resize((i.width*5,i.height*5),Image.NEAREST); sh.paste(b,(x,0),b); x+=b.width+20
sh.convert('RGB').save('sheet3.png')
