"""Desenha a escavação de fóssil da entrada do Ginásio do Brock (BrockDigSite.cs) e gera o molde de texto do C#.
Rodar: python3 tools/structures/brock_dig_site.py  (grava brock_dig_site.png e brock_dig_site.txt na pasta atual).
"""
import numpy as np, os, math, random
from PIL import Image
W=75; H=38; c=36; G=H-1
g=[[' ']*W for _ in range(H)]
def put(x,y,v,only_air=False):
    x,y=int(round(x)),int(round(y))
    if 0<=x<W and 0<=y<H and (not only_air or g[y][x]==' '): g[y][x]=v
def get(x,y):
    x,y=int(round(x)),int(round(y))
    return g[y][x] if 0<=x<W and 0<=y<H else 'X'
rnd=random.Random(7)
# --- crag: low plateau on the left, a cliff on the right, layered rock
def top_of(x):
    d=x-c
    if d < -30 or d > 36: return None
    base = G-6 + (0 if d<-26 else 0)
    if d < -26: base = G-6 + (-26-d)        # left edge slopes down
    if d > 14: base = G-6 - min(8, (d-14)*2)  # cliff rising on the right
    if d > 32: base = G-14 + (d-32)*2         # and falling off at the far right
    return base + rnd.choice([0,0,0,1])
tops={}
for x in range(W):
    t=top_of(x)
    if t is None: continue
    tops[x]=t
    for y in range(t,H):
        band=(y+(x//9)%2)%6
        v={0:'R',1:'R',2:'N',3:'R',4:'D',5:'D'}[band]
        if y==t: v='R'
        put(x,y,v)
# --- the dig: a shored pit in the plateau, rope down the middle
for x in range(c-3,c+4):
    for y in range(0,H): 
        if get(x,y)!=' ': g[y][x]='.'
for y in range(G-7,H): put(c-4,y,'w'); put(c+4,y,'w')
for y in range(G-7,H): put(c,y,'|')
for x in (c-3,c-2,c+2,c+3): put(x,G,'W')
for x in (c-1,c+1): put(x,G,'=')
put(c-4,G-8,'w'); put(c+4,G-8,'w')
for x in range(c-4,c+5): put(x,G-9,'w')   # a wooden frame over the pit
for y in range(G-9,G-7): put(c,y,'|')
# --- the fossil
def spine_y(x): return G-21 + 10*((x-c)/21)**2
xs=list(range(c-21,c+17))
for x in xs: put(x,spine_y(x),'b')
# tail: on down into the plateau on the left
tx,ty=c-21,spine_y(c-21)
for k in range(1,11):
    put(tx-k, ty+k*0.85,'b')
# ribs hang from the spine, curving back, stop at the rock
for x in range(c-13,c+14,3):
    if abs(x-c)<=1: continue
    y0=spine_y(x)
    for t in range(1,40):
        bend = 1.8*math.sin(min(t,10)/10*math.pi/2)*(1 if x<c else -1)
        xx,yy=x+bend,y0+t
        if get(xx,yy)!=' ' or yy>G-10: break
        put(xx,yy,'b')
# hip and leg bones
for k in range(1,9): put(c-17, spine_y(c-17)+k,'b')
for k in range(1,6): put(c+13, spine_y(c+13)+k,'b')
# neck up and over to the skull lying on the cliff top
nx,ny=c+16,spine_y(c+16)
for k in range(1,7): put(nx+k, ny-k*0.6,'b')
skull=[
"   bbbbbbb        ",
" bbbbbbbbbbbbb    ",
"bbb.bbbbbbbbbbbbb ",
"bbbbbbbbbbbbbbbbbb",
"bb b b b b b bbbbb",
" b             bbb",
" bb b b b b bbbbb ",
"  bbbbbbbbbbbbbb  ",
]
sx=c+19; sy=tops.get(c+26,G-14)-len(skull)+1
for j,row in enumerate(skull):
    for i,ch in enumerate(row):
        if ch=='b': put(sx+i,sy+j,'b')
        elif ch=='.': put(sx+i,sy+j,'e')
# --- scaffold on the plateau, left of the pit, with a crane over the pit
sxl=c-12; base=tops[sxl]-1
for y in range(base-13,base+1): put(sxl,y,'w'); put(sxl+4,y,'w')
for y in (base-13,base-8,base-3):
    for x in range(sxl-1,sxl+6): put(x,y,'-')
for y in range(base-13,base+1): put(sxl+2,y,'|',True)
for x in range(sxl+4,c+2): put(x,base-15,'w')
put(sxl+4,base-14,'w')
for y in range(base-14,G-11): put(c+1,y,'~')
put(c+1,G-11,'L')
put(sxl-1,base-12,'L'); put(sxl+5,base-7,'L')
# crates and barrels on the plateau, a sign by the pit
for x in (c-8,c-7,c+7):
    put(x,tops[x]-1,'C')
put(c-6,tops[c-6]-1,'s')
C={' ':(140,180,230),'R':(128,128,128),'D':(140,95,60),'N':(85,85,95),'.':(60,50,45),'w':(150,105,60),'W':(120,80,45),'|':(220,190,90),'~':(220,190,90),'b':(235,228,205),'e':(40,30,30),'-':(170,120,70),'L':(255,210,80),'C':(170,120,60),'s':(160,110,50),'=':(60,60,66)}
img=np.array([[C[ch] for ch in r] for r in g],np.uint8)
Image.fromarray(img).resize((W*10,H*10),Image.NEAREST).save('brock_dig_site.png')
open('brock_dig_site.txt','w').write("\n".join("".join(r) for r in g))
