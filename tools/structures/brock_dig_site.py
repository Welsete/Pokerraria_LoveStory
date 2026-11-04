"""Desenha a entrada do Ginásio do Brock (BrockDigSite.cs): colina em camadas com o fóssil e o túnel de mina.
Rodar: python3 tools/structures/brock_dig_site.py  (grava brock_dig_site.png e brock_dig_site.txt na pasta atual).
"""
import numpy as np, os, math, random
from PIL import Image
W=64; H=24; G=H-1
g=[[' ']*W for _ in range(H)]
def put(x,y,v):
    x,y=int(round(x)),int(round(y))
    if 0<=x<W and 0<=y<H: g[y][x]=v
def get(x,y): return g[y][x] if 0<=x<W and 0<=y<H else 'X'
rnd=random.Random(11)
# --- the hill: smooth, layered
cx=34
tops={}
for x in range(3,W-2):
    d=(x-cx)/28
    top=G-int(round(18*max(0,1-d*d)**0.8))+ (1 if rnd.random()<0.15 else 0)
    top=min(top,G-1)
    tops[x]=top
    for y in range(top,H):
        wave=math.sin(x*0.25)*0.8
        band=int((y-wave))%6
        v={0:'R',1:'R',2:'D',3:'D',4:'Y',5:'N'}[band]
        if y==top: v='R'
        put(x,y,v)
# --- the fossil dig in the hill's face: a recess with the skeleton lying in it
def recess(x,y):
    return ((x-24)/13)**2+((y-(G-11))/4.2)**2<=1
for x in range(W):
    for y in range(H):
        if recess(x,y) and get(x,y) not in (' ','X'): g[y][x]='.'
sp=G-12
for x in range(13,34):
    yy=sp+(0 if x>16 else (17-x)*0.5); put(x,yy,'b')
    if 17<=x<=32: put(x,yy-1,'b')       # spine, two blocks thick; tail dips at the left
for x in range(18,31,2):                                                    # ribs
    for t in range(1,5): put(x - (t>2), sp+t,'b')
for t in range(1,4): put(16,sp+t,'b'); put(31,sp+t,'b')                    # legs
skull=["  bbbbb   ","bbbbbbbbb ","bb.bbbbbbb","bbbbbbbbbb"," b b b b b","  bbbbbbb "]
for j,r in enumerate(skull):
    for i,ch in enumerate(r):
        if ch=='b': put(33+i,sp-3+j,'b')
        elif ch=='.': put(33+i,sp-3+j,'e')
# platform under the dig to stand and look, and a ladder rope up from the ground
for x in range(14,36): 
    if get(x,G-6)=='.': put(x,G-6,'-')
# --- the mine tunnel at ground level into the shaft
shaft=44
for x in range(0,shaft+3):
    for y in range(G-4,G):
        if x<4: put(x,y,'_')
        else: put(x,y,'t')
    if x>=4: put(x,G,'R' if get(x,G)!=' ' else 'R')
for x in range(5,shaft,5):       # timber frames
    put(x,G-4,'w'); put(x,G-3,'w'); put(x,G-2,'w'); put(x,G-1,'w')
    put(x,G-5,'W')
for x in range(5,shaft-1): put(x,G-1,'=')   # track along the floor (marker)
for y in range(G-4,H): put(shaft,y,'|')
for x in (shaft-1,shaft+1):
    put(x,G,'v')                   # shaft opening in the floor
put(shaft,G,'|')
for x in (12,22,32,41): put(x,G-4,'L')
# entrance frame and props outside
for y in range(G-4,G): put(4,y,'w')
put(4,G-5,'W')
put(3,G-5,'W'); put(5,G-5,'W')
put(1,G-1,'s'); put(7,G-1,'C')
C={' ':(140,180,230),'R':(128,128,128),'D':(150,85,65),'N':(85,85,98),'Y':(190,160,110),'.':(80,60,45),'b':(235,228,205),'e':(40,30,30),'-':(170,120,70),'w':(150,105,60),'W':(120,80,45),'t':(55,45,40),'_':(140,180,230),'=':(150,150,160),'|':(220,190,90),'v':(55,45,40),'L':(255,210,80),'s':(160,110,50),'C':(170,120,60)}
img=np.array([[C[ch] for ch in r] for r in g],np.uint8)
Image.fromarray(img).resize((W*11,H*11),Image.NEAREST).save('brock_dig_site.png')
open('brock_dig_site.txt','w').write("\n".join("".join(r) for r in g))
