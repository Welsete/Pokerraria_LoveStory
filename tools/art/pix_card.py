"""Cartão do Pix do README (.github/assets/pix.png): céu com aurora, moldura do diário, QR Code no centro.
Rodar com o QR original do banco salvo como qr_original.png na pasta atual."""
import math, random
import numpy as np
from PIL import Image
S=3                      # art pixel size
W,H=190,250              # art pixels
img=np.zeros((H,W,3),np.uint8)
YY,XX=np.mgrid[0:H,0:W]
# night sky
t=YY/H
for i,(a,b) in enumerate(zip((10,10,34),(40,34,80))): img[...,i]=np.round((a*(1-t)+b*t)/8)*8
rnd=random.Random(3)
for _ in range(140):
    x,y=rnd.randrange(W),rnd.randrange(H); img[y,x]=(240,240,255) if rnd.random()<.6 else (170,170,220)
def aurora(y0,amp,ph,cols,h):
    for x in range(W):
        yb=y0+amp*math.sin(x*0.06+ph)+2*math.sin(x*0.21+ph*2)
        for k in range(h):
            y=int(yb-k)
            if not 0<=y<H or ((x+k)%3==0 and k>h*.5): continue
            c=cols[0] if k<2 else cols[1] if k<h*.55 else cols[2]
            img[y,x]=(np.array(img[y,x])*0.25+np.array(c)*0.75).astype(np.uint8) if k>=2 else c
aurora(52,5,0.3,[(200,255,220),(90,230,160),(60,150,130)],22)
aurora(40,4,2.0,[(230,200,255),(170,110,230),(100,70,160)],14)
aurora(232,4,1.1,[(200,255,220),(90,230,160),(60,150,130)],14)
# frame: green leather border with light inner line, gold corner studs
def rect(x0,y0,x1,y1,c): img[y0:y1,x0:x1]=c
G1,G2,G3=(18,46,28),(40,98,58),(70,140,88)
for d,c in ((0,G1),(1,G2),(2,G2),(3,G2),(4,G3),(5,G1)):
    img[d,d:W-d]=c; img[H-1-d,d:W-d]=c; img[d:H-d,d]=c; img[d:H-d,W-1-d]=c
gold=[(255,222,96),(232,176,40),(170,112,20)]
for cx,cy in ((0,0),(W-12,0),(0,H-12),(W-12,H-12)):
    rect(cx,cy,cx+12,cy+12,gold[1]); rect(cx,cy,cx+12,cy+1,gold[0]); rect(cx,cy,cx+1,cy+12,gold[0])
    rect(cx,cy+11,cx+12,cy+12,gold[2]); rect(cx+11,cy,cx+12,cy+12,gold[2]); rect(cx+4,cy+4,cx+8,cy+8,gold[0])
    img[cy+5:cy+7,cx+5:cx+7]=(255,250,210)
# pixel font 5x7
F={
'A':["01110","10001","10001","11111","10001","10001","10001"],'P':["11110","10001","10001","11110","10000","10000","10000"],
'O':["01110","10001","10001","10001","10001","10001","01110"],'I':["11111","00100","00100","00100","00100","00100","11111"],
'E':["11111","10000","10000","11110","10000","10000","11111"],'C':["01111","10000","10000","10000","10000","10000","01111"],
'M':["10001","11011","10101","10101","10001","10001","10001"],'X':["10001","10001","01010","00100","01010","10001","10001"],
'B':["11110","10001","10001","11110","10001","10001","11110"],'R':["11110","10001","10001","11110","10100","10010","10001"],
'G':["01111","10000","10000","10111","10001","10001","01111"],'D':["11110","10001","10001","10001","10001","10001","11110"],
'!':["00100","00100","00100","00100","00100","00000","00100"],' ':["00000"]*7,'U':["10001","10001","10001","10001","10001","10001","01110"],
'S':["01111","10000","10000","01110","00001","00001","11110"],'K':["10001","10010","10100","11000","10100","10010","10001"],
'N':["10001","11001","10101","10011","10001","10001","10001"],'L':["10000","10000","10000","10000","10000","10000","11111"],
'T':["11111","00100","00100","00100","00100","00100","00100"],'V':["10001","10001","10001","10001","10001","01010","00100"],
'Ç':["01111","10000","10000","10000","10000","01111","00100"],'Ã':["01010","01110","10001","11111","10001","10001","10001"],
}
def text(s,cy,scale,fill,shade,outline=(20,14,30)):
    w=len(s)*6*scale-scale; x0=(W-w)//2
    mask=np.zeros((H,W),bool)
    for i,ch in enumerate(s):
        g=F[ch]
        for r,row in enumerate(g):
            for c,v in enumerate(row):
                if v=='1':
                    y,x=cy+r*scale,x0+i*6*scale+c*scale; mask[y:y+scale,x:x+scale]=True
    o=np.zeros_like(mask)
    for dy in (-1,0,1):
        for dx in (-1,0,1): o|=np.roll(np.roll(mask,dy,0),dx,1)
    img[o&~mask]=outline; img[mask]=fill
    lower=mask&(YY>=cy+4*scale); img[lower]=shade
text("APOIE COM PIX",16,2,(255,215,90),(232,160,40))
# QR panel: cream paper with a white field for the code
px0,py0,ps=21,40,148
rect(px0-3,py0-3,px0+ps+3,py0+ps+3,(120,92,60)); rect(px0-2,py0-2,px0+ps+2,py0+ps+2,(241,232,206)); rect(px0,py0,px0+ps,py0+ps,(255,255,255))
# footer ribbon with coffee cup and hearts
text("OBRIGADO!",py0+ps+14,2,(255,240,250),(240,160,200))
cup=["..o.o....","...o.o...","OOOOOOO..","OwwwwwOOO","OwwwwwO.O","OwwwwwOOO","OwwwwwO..",".OOOOO...","OOOOOOOO."]
hrt=[".hh.hh.","hHhhhhh","hhhhhhh",".hhhhh.","..hhh..","...h..."]
def stamp(b,x,y,k=2):
    pal={'O':(60,30,20),'w':(250,245,235),'o':(220,220,230),'h':(232,90,140),'H':(255,200,220)}
    for r,row in enumerate(b):
        for c,v in enumerate(row):
            if v in pal: img[y+r*k:y+r*k+k,x+c*k:x+c*k+k]=pal[v]
fy=py0+ps+30
stamp(cup,W//2-9,fy); stamp(hrt,W//2-34,fy+3); stamp(hrt,W//2+20,fy+3)
out=Image.fromarray(img).resize((W*S,H*S),Image.NEAREST)
qr=Image.open('qr_original.png').convert('RGB')
q=(ps-8)*S; qr=qr.resize((q,q),Image.LANCZOS)
out.paste(qr,((px0+4)*S,(py0+4)*S))
out.save('pix.png',optimize=True)
import cv2; t,_,_=cv2.QRCodeDetector().detectAndDecode(cv2.imread('pix.png')); print('decode ok' if '2db8d9ed' in t else 'DECODE FAIL', out.size)
