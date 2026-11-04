"""Desenha o templo da entrada do Ginásio do Brock (BrockTemple.cs) e gera o molde de texto que vai no C#.
Rodar: python3 tools/structures/brock_temple.py  (grava brock_temple.png e brock_temple.txt na pasta atual).
"""
import numpy as np, os
from PIL import Image
W=57; H=40; c=W//2
g=[[' ']*W for _ in range(H)]
def put(x,y,v):
    if 0<=x<W and 0<=y<H: g[y][x]=v
def h(y,x0,x1,v):
    for x in range(x0,x1+1): put(x,y,v)
def sym(y,dx0,dx1,v):  # both sides, dx from center
    for dx in range(dx0,dx1+1): put(c-dx,y,v); put(c+dx,y,v)
def roof(y,half,depth):
    # eave: dark line with upturned tips, brick band, slab underside
    sym(y,0,half,'K'); put(c-half-1,y-1,'K'); put(c+half+1,y-1,'K'); put(c-half,y-1,'K'); put(c+half,y-1,'K')
    sym(y+1,0,half-1,'B'); put(c-half,y+1,'K'); put(c+half,y+1,'K')
    sym(y+2,0,half-2,'S'); put(c-half+1,y+2,'K'); put(c+half-1,y+2,'K')
# ground: mound
G=H-1  # bottom row index = ground row
for x in range(W):
    d=abs(x-c)
    top=G-6+max(0,(d-12)//3)  # flat top 25 wide then slopes
    for y in range(max(top,0),H): put(x,y,'R')
# steps (stairs) on both sides of the mound: brick
for i in range(6):
    for s in (-1,1):
        x=c+s*(13+i*3); y=G-6+i
        for k in range(3): put(x+s*k,y,'B')
# platform top
h(G-6,c-13,c+13,'B'); h(G-7,c-12,c+12,'S')
# shrine body: pillars and dark back
body_top=G-17
for y in range(body_top,G-7):
    sym(y,0,7,'#')
    for s in (-1,1):
        put(c+s*8,y,'S'); put(c+s*9,y,'P'); put(c+s*10,y,'S')
    put(c-8,body_top,'K'); put(c+8,body_top,'K'); put(c-10,body_top,'K'); put(c+10,body_top,'K')
for s in (-1,1):
    put(c+s*8,G-8,'K'); put(c+s*10,G-8,'K')
# lanterns hanging and torches
put(c-6,body_top+2,'L'); put(c+6,body_top+2,'L')
put(c-4,G-10,'T'); put(c+4,G-10,'T')
# lower roof
roof(body_top-3,15,3)
# upper tier: small room with badge window
t2=body_top-9
for y in range(t2,body_top-3):
    sym(y,0,6,'S'); put(c-7,y,'K'); put(c+7,y,'K')
# boulder badge (octagon) in the upper tier
badge=[" OOO ","OoooO","OoOoO","OoooO"," OOO "]
for j,row in enumerate(badge):
    for i,ch in enumerate(row):
        if ch!=' ': put(c-2+i,t2+j,ch)
roof(t2-3,10,3)
# gable / spire
for k in range(5):
    y=t2-4-k; sym(y,0,5-k,'S'); put(c-(5-k)-1,y,'K'); put(c+(5-k)+1,y,'K')
put(c,t2-9,'K'); put(c,t2-10,'K')
# rope shaft down the center through mound
for y in range(G-9,H): put(c,y,'|')
for y in range(G-5,H): put(c-1,y,'='); put(c+1,y,'=')
# stone lanterns (toro) on the platform edges
for s in (-1,1):
    x=c+s*15
    put(x,G-7,'K'); put(x-1,G-7,'K'); put(x+1,G-7,'K'); put(x,G-8,'S'); put(x,G-9,'t'); put(x-1,G-9,'K'); put(x+1,G-9,'K'); put(x,G-10,'K'); put(x-1,G-10,'K'); put(x+1,G-10,'K'); put(x-2,G-10,'K'); put(x+2,G-10,'K'); put(x,G-11,'K')
# scattered boulders on the mound slopes
for (dx,dy) in ((-22,2),(21,3),(-17,1),(25,4)):
    x=c+dx; y=G-6+dy
    for (a,b) in ((0,0),(1,0),(0,-1),(1,-1),(-1,0)):
        put(x+a,y+b-1,'r')
C={'t':(255,230,90),'K':(70,70,78),'S':(185,185,178),'B':(115,115,122),'R':(128,128,128),'P':(210,210,200),'#':(45,45,52),'L':(255,210,80),'T':(255,240,120),'|':(220,190,90),'=':(60,60,66),' ':(140,180,230),'O':(105,105,112),'o':(190,190,182),'r':(150,140,130)}
img=np.array([[C[ch] for ch in r] for r in g],np.uint8)
Image.fromarray(img).resize((W*12,H*12),Image.NEAREST).save('brock_temple.png')
open('brock_temple.txt','w').write("\n".join("".join(r) for r in g))
