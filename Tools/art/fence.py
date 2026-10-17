from PIL import Image, ImageDraw
OUT='/mnt/user-data/outputs/Art/final/Props/'
D=(58,42,30,255); M=(104,78,52,255); L=(152,120,80,255); G=(120,104,70,255)
def seg(broken=False):
    im=Image.new('RGBA',(20,24),(0,0,0,0)); d=ImageDraw.Draw(im)
    def post(x,yb):
        d.rectangle([x,yb-12,x+1,yb],fill=M); d.line([x,yb-12,x,yb],fill=L); d.line([x+1,yb-12,x+1,yb],fill=D)
        d.point((x,yb-13),fill=D); d.point((x+1,yb-13),fill=D)
    # rails (behind posts)
    def rail(y0,y1,skip=None):
        for i in range(0,17):
            x=2+i; y=round(y0+(y1-y0)*i/16)
            if skip and skip[0]<=i<=skip[1]: continue
            d.point((x,y),fill=M); d.point((x,y+1),fill=D)
    rail(11,3)
    rail(17,9,skip=(5,10) if broken else None)
    post(2,22); post(17,14)
    if broken:
        # snapped rail end hanging down
        d.line([8,19,9,22],fill=D); d.point((9,13),fill=L)
    return im
a=seg(False); b=seg(True)
a.save(OUT+'fence_x.png'); b.save(OUT+'fence_x_broken.png')
a.transpose(Image.FLIP_LEFT_RIGHT).save(OUT+'fence_y.png'); b.transpose(Image.FLIP_LEFT_RIGHT).save(OUT+'fence_y_broken.png')
sh=Image.new('RGBA',(20*4*4+30,24*6),(120,110,70,255))
for k,i in enumerate([a,b,a.transpose(Image.FLIP_LEFT_RIGHT),b.transpose(Image.FLIP_LEFT_RIGHT)]):
    bb=i.resize((80,96),Image.NEAREST); sh.paste(bb,(k*90,0),bb)
sh.convert('RGB').save('sheet_fence.png')
