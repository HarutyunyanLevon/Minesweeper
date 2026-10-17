import random, sys
W,H=24,16
def nbrs(x,y,r):
    for dx in range(-r,r+1):
        for dy in range(-r,r+1):
            if dx==0 and dy==0: continue
            a,b=x+dx,y+dy
            if 0<=a<W and 0<=b<H: yield a,b
def solve(mines,start,r):
    st=[[0]*H for _ in range(W)]; num={}
    stack=[start]; st[start[0]][start[1]]=1
    while True:
        while stack:
            c=stack.pop()
            if st[c[0]][c[1]]==3: continue
            st[c[0]][c[1]]=3
            n=sum(1 for a,b in nbrs(c[0],c[1],r) if mines[a][b]); num[c]=n
            if n==0:
                for a,b in nbrs(c[0],c[1],r):
                    if st[a][b]==0: st[a][b]=1; stack.append((a,b))
        progress=False; cons=[]
        for c,n in num.items():
            U=[(a,b) for a,b in nbrs(c[0],c[1],r) if st[a][b]==0]
            if not U: continue
            k=sum(1 for a,b in nbrs(c[0],c[1],r) if st[a][b]==2); rem=n-k
            if rem==0:
                for a,b in U: st[a][b]=1; stack.append((a,b))
                progress=True
            elif rem==len(U):
                for a,b in U: st[a][b]=2
                progress=True
            else: cons.append((frozenset(U),rem))
        if progress: continue
        for i in range(len(cons)):
            A,ra=cons[i]
            for j in range(len(cons)):
                if i==j: continue
                B,rb=cons[j]
                if len(A)<len(B) and A<=B:
                    D=B-A; rd=rb-ra
                    if rd==0:
                        for a,b in D:
                            if st[a][b]==0: st[a][b]=1; stack.append((a,b)); progress=True
                    elif rd==len(D):
                        for a,b in D:
                            if st[a][b]==0: st[a][b]=2; progress=True
        if not progress: break
    return [(x,y) for x in range(W) for y in range(H) if not mines[x][y] and st[x][y] in (0,)]
def gen(seed,density=0.14,r=1,safe_rows=2):
    rng=random.Random(seed)
    start=(W//2,rng.randint(0,H-1)) if False else (W//2,0)
    m=[[False]*H for _ in range(W)]
    for x in range(W):
        for y in range(safe_rows,H):
            if abs(x-start[0])<=2 and abs(y-start[1])<=2: continue
            m[x][y]=rng.random()<density
    p=start  # first probe at the start cell
    return rng,m,p
def repair(rng,m,p,r=1,maxit=200):
    removed=0; budget=max(3,sum(map(sum,m))//3)
    for i in range(maxit):
        un=solve(m,p,r)
        if not un: return removed,0,i
        u=rng.choice(un); cand=[c for rr in (r,r+1) for c in nbrs(u[0],u[1],rr) if m[c[0]][c[1]]]
        if not cand: continue
        c=rng.choice(cand); m[c[0]][c[1]]=False; removed+=1
        if removed>=budget: break
    return removed,len(solve(m,p,r)),maxit
if __name__=='__main__':
    tot=0;fails=0;its=0;N=200
    for s in range(N):
        rng,m,p=gen(s); n0=sum(map(sum,m))
        rem,left,it=repair(rng,m,p); tot+=rem; fails+=left>0; its+=it
    print('avg mines',n0,'avg removed',tot/N,'fail',fails,'avg iters',its/N)
