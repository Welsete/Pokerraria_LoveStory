"""Lê arquivos de mundo do Terraria (.wld, versão 319): cabeçalho, blocos/paredes/líquidos e placas.
Uso: import wld; w = wld.load(caminho); T, WL, LQ, _ = wld.tiles(w); wld.signs(w)
Serve para estudar mundos e recortar áreas (por exemplo, para virar design do Blueprint Builder)."""
import struct, sys
class R:
    def __init__(s,b,p=0): s.b=b; s.p=p
    def u8(s): v=s.b[s.p]; s.p+=1; return v
    def i16(s): v=struct.unpack_from('<h',s.b,s.p)[0]; s.p+=2; return v
    def u16(s): v=struct.unpack_from('<H',s.b,s.p)[0]; s.p+=2; return v
    def i32(s): v=struct.unpack_from('<i',s.b,s.p)[0]; s.p+=4; return v
    def u32(s): v=struct.unpack_from('<I',s.b,s.p)[0]; s.p+=4; return v
    def u64(s): v=struct.unpack_from('<Q',s.b,s.p)[0]; s.p+=8; return v
    def s7(s):
        n=0; sh=0
        while True:
            c=s.u8(); n|=(c&0x7f)<<sh; sh+=7
            if not c&0x80: break
        v=s.b[s.p:s.p+n].decode('utf-8','replace'); s.p+=n; return v
def load(path):
    b=open(path,'rb').read(); r=R(b)
    ver=r.i32(); magic=r.b[r.p:r.p+7]; r.p+=7; ft=r.u8(); rev=r.u32(); fav=r.u64()
    ns=r.i16(); ptr=[r.i32() for _ in range(ns)]
    nt=r.i16(); fi=[]; byte=0
    for i in range(nt):
        if i%8==0: byte=r.u8()
        fi.append(bool(byte&(1<<(i%8))))
    r.p=ptr[0]; name=r.s7(); seed=r.s7(); gv=r.u64(); r.p+=16; wid=r.i32()
    l,ri,t,bo=r.i32(),r.i32(),r.i32(),r.i32(); H=r.i32(); W=r.i32()
    return dict(b=b,ver=ver,magic=magic,ptr=ptr,fi=fi,name=name,seed=seed,W=W,H=H)
def tiles(w):
    import numpy as np
    r=R(w['b'],w['ptr'][1]); W,H,fi=w['W'],w['H'],w['fi']
    T=np.full((H,W),-1,np.int32); WL=np.zeros((H,W),np.int32); LQ=np.zeros((H,W),np.uint8)
    for x in range(W):
        y=0
        while y<H:
            h1=r.u8(); h2=h3=h4=0
            if h1&1:
                h2=r.u8()
                if h2&1:
                    h3=r.u8()
                    if h3&1: h4=r.u8()
            typ=-1
            if h1&2:
                typ = r.u16() if h1&0x20 else r.u8()
                if typ < len(fi) and fi[typ]: r.p+=4
                if h3&8: r.p+=1
            wall=0
            if h1&4:
                wall=r.u8()
                if h3&0x10: r.p+=1
            lq=(h1&0x18)>>3
            if lq: r.p+=1
            if h3&0x40: wall|=r.u8()<<8
            k=(h1&0xC0)>>6
            rep = r.u8() if k==1 else (r.i16() if k==2 else 0)
            for yy in range(y, min(H,y+rep+1)):
                T[yy,x]=typ; WL[yy,x]=wall; LQ[yy,x]=lq if lq else 0
            y+=rep+1
    return T,WL,LQ,r.p
def signs(w):
    r=R(w['b'],w['ptr'][3]); n=r.i16(); out=[]
    for _ in range(n):
        t=r.s7(); x=r.i32(); y=r.i32(); out.append((x,y,t))
    return out
