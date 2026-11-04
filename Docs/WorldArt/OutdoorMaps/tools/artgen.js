// Art kit exporter. Runs inside outdoor2.html?map=0 (shares its globals: P, T, IMG, make* helpers, hash, ri...).
(function(){
const SAVE='http://127.0.0.1:8767/save?p=';
const MAN=[],BOARD=[];
async function save(rel,c,o){o=o||{};await fetch(SAVE+encodeURIComponent(rel),{method:'POST',body:c.toDataURL('image/png')});
 MAN.push([rel,c.width,c.height,o.frames||1,o.fw||c.width,o.fh||c.height,o.pivot||'0.5,0',(o.note||'').replace(/,/g,';')]);BOARD.push({rel,c,o})}
async function saveText(rel,txt){await fetch(SAVE+encodeURIComponent(rel),{method:'POST',body:txt})}
function strip(frs){const w=frs[0].width,h=frs[0].height,c=mk(w*frs.length,h),g=c.getContext('2d');frs.forEach((f,i)=>g.drawImage(f,i*w,0));return c}
function grid(frs,cols){const w=frs[0].width,h=frs[0].height,rows=Math.ceil(frs.length/cols),c=mk(w*cols,h*rows),g=c.getContext('2d');frs.forEach((f,i)=>g.drawImage(f,(i%cols)*w,Math.floor(i/cols)*h));return c}
function pix(c){return c.getContext('2d')}
function px(g,x,y,col){g.fillStyle=col;g.fillRect(x,y,1,1)}

// ---------- periodic noise (period 16) so tiles join seamlessly ----------
function pn(x,y,s){function lat(cell){const n=16/cell,xi=Math.floor(x/cell),yi=Math.floor(y/cell),xf=x/cell-xi,yf=y/cell-yi,u=xf*xf*(3-2*xf),v=yf*yf*(3-2*yf);
  const h=(a,b)=>hash(((a%n)+n)%n,((b%n)+n)%n,s+cell*13);const a=h(xi,yi),b=h(xi+1,yi),c=h(xi,yi+1),d=h(xi+1,yi+1);return a+(b-a)*u+(c-a)*v+(a-b-c+d)*u*v}
 return lat(8)*0.5+lat(4)*0.32+lat(2)*0.18}
// material painters: return colour at tile pixel (x,y), variant v
const MAT={
 grass:(x,y,v)=>{const h=hash(x&15,y&15,40+v);if(h<0.03)return P.T1;if(h>0.97)return P.T3;if(h>0.93)return P.G3;return P.G2},
 dark:(x,y,v)=>{const h=hash(x&15,y&15,60+v);if(h<0.03)return P.G2;if(h>0.97)return P.T1;if(h>0.93)return P.G0;return P.G1},
 forest:(x,y,v)=>{const h=hash(x&15,y&15,80+v);if(h<0.03)return P.T1;if(h>0.95)return P.G1;return P.G0},
 dirt:(x,y,v)=>{const h=hash(x&15,y&15,100+v);return h<0.05?P.D2:h>0.965?P.D3:P.D1},
 water:(x,y,v)=>{const h=hash(x&15,y&15,120+v);return h>0.96?P.W2:h<0.03?P.W0:P.W1},
 pave:(x,y,v)=>{const row=Math.floor((y&15)/4),xx=((x&15)+(row%2)*4)&15,tx=xx%8,ty=(y&15)%4;const id=Math.floor(xx/8)+row*2;const h=hash(id,v,130);
  if(tx===0||ty===0)return hash(x&15,y&15,131+v)<0.2?'#4e7458':P.pave[0];if(ty===1)return P.pave[3];return h<0.25?P.pave[3]:h<0.6?P.pave[2]:P.pave[1]}};
function matTile(m,v){const c=mk(16,16),g=pix(c);for(let y=0;y<16;y++)for(let x=0;x<16;x++)px(g,x,y,MAT[m](x,y,v));
 if(m==='grass'||m==='dark')for(let k=0;k<3;k++){const x=Math.floor(hash(k,v,141+m.length)*13)+1,y=Math.floor(hash(k,v,142)*14)+1;const col=m==='grass'?(k%2?P.T1:P.T3):P.G2;px(g,x,y,col);px(g,x+2,y,col);px(g,x+1,y+1,col)}
 return c}
// dual-grid tile: corner bits (TL,TR,BL,BR) choose material B; jagged organic edge from periodic noise
function field(x,y,bits,s){const u=(x+0.5)/16,v=(y+0.5)/16,[tl,tr,bl,br]=bits;const f=tl*(1-u)*(1-v)+tr*u*(1-v)+bl*(1-u)*v+br*u*v;return f+(pn(x,y,s)-0.5)*0.62}
const PAIRS=[
 {name:'GrassToDark',A:'grass',B:'dark',edgeB:()=>P.G1o},
 {name:'DarkToForest',A:'dark',B:'forest',edgeB:()=>'#24403d'},
 {name:'GrassToDirt',A:'grass',B:'dirt',edgeB:()=>P.De,innerB:()=>P.Ds,lipA:true},
 {name:'GrassToWater',A:'grass',B:'water',edgeB:()=>P.Wb,innerB:()=>P.W3,inner2:()=>P.W2},
 {name:'DirtToPave',A:'dirt',B:'pave',edgeB:()=>P.pave[0]}];
function dualTile(pair,mask,s,overlay){const bits=[mask&1,(mask>>1)&1,(mask>>2)&1,(mask>>3)&1].map(Number);const c=mk(16,16),g=pix(c);
 const isB=(x,y)=>field(x,y,bits,s)>0.5;
 for(let y=0;y<16;y++)for(let x=0;x<16;x++){let col;
  if(isB(x,y)){col=MAT[pair.B](x,y,0);const nA=!isB(x-1,y)||!isB(x+1,y)||!isB(x,y-1)||!isB(x,y+1);
   if(nA)col=pair.edgeB();else if(pair.innerB&&(!isB(x,y-2)||!isB(x-2,y)||!isB(x+2,y)||!isB(x,y+2)))col=pair.innerB();else if(pair.inner2&&(!isB(x,y-3)||!isB(x-3,y)||!isB(x+3,y)||!isB(x,y+3)))col=pair.inner2()}
  else{col=overlay?null:MAT[pair.A](x,y,0);if(pair.lipA&&isB(x,y+1)&&hash(x,y,77)<0.4)col=P.G2}
  if(col)px(g,x,y,col)}
 return c}

// ---------- cliff ----------
function cliffFace(seed){const c=mk(16,56),g=pix(c);const top=6,bot=52;let x=0,k=0;const widths=[[7,9],[6,10],[8,8],[5,11],[9,7],[10,6]][seed%6];
 for(const bw of widths){const mid=top+Math.floor((bot-top)*(0.38+hash(seed,k,5)*0.17));
  for(let xx=x;xx<x+bw;xx++)for(let y=top;y<bot;y++){let col=P.C2;const lx=xx-x;if(lx===0)col=P.C0;else if(lx===1)col=P.C3;else if(lx===bw-1)col=P.C1;
   if(y===mid)col=P.C4;else if(y===mid+1||y===mid+2)col=P.C1;if(y>=bot-3)col=y===bot-1?P.C0:P.C1;if(y<top+3)col=P.C1;
   if(col===P.C2&&hash(xx,y,51+seed)<0.05)col=P.C3;else if(col===P.C2&&hash(xx,y,52+seed)<0.04)col=P.C1;px(g,xx,y,col)}
  for(let j=0;j<2;j++){const cy=top+5+Math.floor(hash(seed,j+k*3,53)*(bot-top-12)),cx=x+2+Math.floor(hash(seed,j,54+k)*Math.max(1,bw-4));g.fillStyle=P.C1;g.fillRect(cx,cy,2+Math.floor(hash(j,k,55)*2),1)}
  x+=bw;k++}
 for(let xx=0;xx<16;xx++){const lip=2+Math.floor(hash(xx>>1,seed,61)*3);g.fillStyle=P.lip[0];g.fillRect(xx,top-1,1,lip);px(g,xx,top-1,P.lip[1]);px(g,xx,top-1+lip,'#22383a');g.fillStyle=P.PL[2];g.fillRect(xx,0,1,top-1);if(hash(xx,seed,62)<0.3)px(g,xx,Math.floor(hash(xx,1,63)*4),P.PL[1])}
 g.fillStyle='rgba(16,28,28,0.42)';g.fillRect(0,bot,16,2);g.fillStyle='rgba(16,28,28,0.2)';g.fillRect(0,bot+2,16,2);return c}
function vine(len,s){const c=mk(3,len+1),g=pix(c);for(let y=0;y<len;y++)px(g,(y>>2)%2,y,y%3?'#3c6a52':'#4f8060');g.fillStyle='#6a9a6e';g.fillRect(0,len,2,1);if(s%2)px(g,2,len-3,'#6a9a6e');return c}
function waterfallFrame(f){const c=mk(20,48),g=pix(c);for(let x=0;x<20;x++)for(let y=0;y<48;y++){const v=((y*0.9-f*1.5+hash(x,0,3)*30)%9+9)%9;px(g,x,y,v<1.4?'#d8f0ea':v<4?'#79b2ac':'#4f8c8c')}
 for(const x of [0,19])for(let y=0;y<48;y++)px(g,x,y,'#3a6e70');return c}
function foamFrame(f){const c=mk(28,6),g=pix(c);for(let x=0;x<28;x++){const y=2+Math.round(Math.sin(f*1.57+x*0.9));px(g,x,y,((x+f)%3)?'#cfe8e2':'#ffffff');px(g,x,y+1,'#cfe8e2');if(hash(x,f,9)<0.25)px(g,x,y-1,'#ffffff')}return c}
function caveMouth(){const c=mk(40,36),g=pix(c);const cx=20,b=34;
 for(let y=-30;y<=0;y++){const hw=y<-18?Math.round(15*Math.sqrt(Math.max(0,1-Math.pow((y+18)/12,2)))):15;for(let x=-hw;x<=hw;x++)px(g,cx+x,b+y,Math.abs(x)>=hw-1?'#3a2a22':y>-4?'#120c0a':'#1a1210')}
 for(let k=0;k<9;k++){const t=Math.PI*(k/8),x=Math.round(cx-Math.cos(t)*17),y=Math.round(b-18-Math.sin(t)*14);g.fillStyle=P.C3;g.fillRect(x-2,y-1,5,3);g.fillStyle=P.C0;g.fillRect(x-2,y+1,5,1)}return c}

// ---------- fire, particles, glows ----------
function fireFrame(big,fr){const w=big?11:7,h=big?9:7,c=mk(w,h),g=pix(c);const span=big?5:3,cols=['#c43a24','#ee7a2c','#ffb43c','#ffe89a'];
 for(let k=-span;k<=span;k++){const base=(big?6.5:4.6)-Math.abs(k)*1.05;if(base<=0)continue;const hh=Math.max(1,Math.round(base*(0.7+0.6*hash(k+9,fr,5))));
  for(let y=0;y<hh;y++){const li=Math.min(3,Math.floor((1-y/hh)*3.2+(Math.abs(k)<2?0.9:0)));px(g,k+span,h-1-y,cols[li])}}return c}
function fireRing(big){const s=big?1:0.6,w=Math.round(40*s)+4,hh=Math.round(24*s)+4,c=mk(w,hh),g=pix(c);const cx=w/2,cy=hh/2;
 g.fillStyle='#5b4632';g.beginPath();g.ellipse(cx,cy,17*s,10*s,0,0,7);g.fill();g.fillStyle='#3e3026';g.beginPath();g.ellipse(cx,cy,11*s,6*s,0,0,7);g.fill();
 const n=big?12:8;for(let a=0;a<n;a++){const t=a/n*Math.PI*2,x=Math.round(cx+Math.cos(t)*12*s)-2,y=Math.round(cy+Math.sin(t)*7*s)-1;g.fillStyle='#7d7a72';g.fillRect(x,y,5,3);g.fillStyle='#a19d92';g.fillRect(x,y,5,1);g.fillStyle='#4f4c46';g.fillRect(x,y+2,5,1)}
 g.fillStyle='#5a3424';g.fillRect(Math.round(cx-8*s),Math.round(cy-2),Math.round(16*s),3);g.fillStyle='#7c4a32';g.fillRect(Math.round(cx-8*s),Math.round(cy-2),Math.round(16*s),1);
 // crisp the anti-aliased ellipse edges
 const d=g.getImageData(0,0,w,hh);for(let i=3;i<d.data.length;i+=4)d.data[i]=d.data[i]>110?255:0;g.putImageData(d,0,0);return c}
function soft(col,w,h){const c=mk(w,h),g=pix(c);const gr=g.createRadialGradient(w/2,h/2,1,w/2,h/2,Math.min(w,h)/2);gr.addColorStop(0,col+'1)');gr.addColorStop(1,col+'0)');g.fillStyle=gr;g.save();g.translate(w/2,h/2);g.scale(w/Math.min(w,h),h/Math.min(w,h));g.translate(-w/2,-h/2);g.fillRect(0,0,w,h);g.restore();return c}
function smoke(s){const c=mk(s,s),g=pix(c);g.fillStyle='#c8d0cc';g.fillRect(0,0,s,s);if(s>3){g.clearRect(0,0,1,1);g.clearRect(s-1,0,1,1);g.clearRect(0,s-1,1,1);g.clearRect(s-1,s-1,1,1)}return c}
function ripple(f){const c=mk(17,9),g=pix(c);const r=1+f*2;for(let k=0;k<16;k++){const t=k/16*Math.PI*2;px(g,Math.round(8+Math.cos(t)*r),Math.round(4+Math.sin(t)*r*0.45),'#b6d8cd')}g.globalCompositeOperation='destination-in';g.globalAlpha=1-f*0.22;g.fillRect(0,0,17,9);return c}
function streak(){const c=mk(32,9),g=pix(c);for(let k=0;k<32;k++){g.globalAlpha=Math.sin(k/31*Math.PI)*0.9;px(g,k,Math.floor(k*0.25),'#e6f4ee')}return c}
function firefly(){const c=mk(3,3),g=pix(c);g.globalAlpha=0.35;g.fillStyle='#e8ff9a';g.fillRect(0,0,3,3);g.globalAlpha=1;px(g,1,1,'#f6ffd0');return c}
function swirl(f){const c=mk(24,30),g=pix(c);for(let i=0;i<18;i++){const a=f/6*Math.PI*2/3+i/18*Math.PI*2,r=9+Math.sin(f+i)*1.5;px(g,Math.round(12+Math.cos(a)*r*0.8),Math.round(15+Math.sin(a)*r),i%3?'#6fd6cc':'#b8a0ff')}
 g.globalCompositeOperation='destination-over';g.fillStyle='rgba(122,224,214,0.18)';g.fillRect(0,1,24,28);return c}

// ---------- nodes (full + depleted) ----------
function woodPile(full){const c=mk(26,18),g=pix(c);const ends=full?[[2,10],[9,10],[16,10],[5,4],[12,4],[19,10],[8.5,-2]]:[[4,10],[12,10]];
 for(const [x,y] of ends){if(y<0)continue;g.fillStyle='#6e4430';g.fillRect(x,y+1,7,6);g.fillStyle='#c09060';g.fillRect(x+1,y+1,5,5);g.fillStyle='#d8aa74';g.fillRect(x+2,y+2,3,3);g.fillStyle='#9a6a44';px(g,x+3,y+3,'#9a6a44');g.fillStyle='#4a2c20';g.fillRect(x,y+6,7,1)}
 if(full){g.fillStyle='#8a9098';g.fillRect(20,1,4,3);g.fillStyle='#6a4a32';g.fillRect(21,4,1,6)}else for(let k=0;k<6;k++){g.fillStyle=k%2?'#c09060':'#8e5e3e';g.fillRect(ri(2,22),ri(14,16),2,1)}return c}
function hideRack(full){const c=mk(26,22),g=pix(c);g.fillStyle='#5e3a26';g.fillRect(2,4,2,18);g.fillRect(22,4,2,18);g.fillStyle='#7a4c30';g.fillRect(1,6,24,2);g.fillStyle='#9a6640';g.fillRect(1,6,24,1);
 if(full){g.fillStyle='#b98e62';g.fillRect(5,8,7,9);g.fillStyle='#cfa676';g.fillRect(6,9,5,7);g.fillStyle='#8f6a4a';g.fillRect(14,8,6,11);g.fillStyle='#a98058';g.fillRect(15,9,4,9)}else{g.fillStyle='#6a4a32';g.fillRect(7,8,1,3);g.fillRect(16,8,1,3)}return c}
function sapTree(full){const c=mk(18,30),g=pix(c);const bk=['#3e2a20','#5e402c','#7a5638'];g.fillStyle=bk[1];g.fillRect(5,0,8,28);g.fillStyle=bk[2];g.fillRect(5,0,2,28);g.fillStyle=bk[0];g.fillRect(11,0,2,28);for(let y=2;y<28;y+=5)px(g,8+(y%3),y,bk[0]);
 g.fillStyle='#3e2a20';g.fillRect(3,26,12,3);g.fillStyle='#8a6a48';g.fillRect(12,20,6,6);g.fillStyle='#5a4430';g.fillRect(12,20,6,1);
 if(full){g.fillStyle='#e0902a';g.fillRect(9,10,2,9);g.fillStyle='#ffc060';px(g,9,11,'#ffc060');px(g,9,14,'#ffc060');g.fillStyle='#e0902a';g.fillRect(13,21,4,2);px(g,12,17,'#f0a840');px(g,12,18,'#f0a840')}else{g.fillStyle='#7a5a30';g.fillRect(9,14,2,3)}return c}
function ironScrap(full){const c=mk(24,15),g=pix(c);const items=full?[[2,8,10,4],[8,5,12,3],[13,9,9,4],[4,3,6,3]]:[[6,9,8,3]];
 for(const [x,y,w,h] of items){g.fillStyle='#585d64';g.fillRect(x,y,w,h);g.fillStyle='#959ba0';g.fillRect(x,y,w,1);g.fillStyle='#3c4046';g.fillRect(x,y+h-1,w,1);g.fillStyle='#b8652a';px(g,x+w-2,y+1,'#b8652a')}return c}
function spoils(full){const c=mk(26,18),g=pix(c);
 if(full){g.fillStyle='#8a2a22';g.fillRect(2,8,9,9);g.fillStyle='#e6dcc6';g.fillRect(5,10,3,3);g.fillStyle='#4a2c1a';g.fillRect(2,8,9,1);
  g.fillStyle='#a8aeb0';g.fillRect(13,2,1,12);g.fillStyle='#e0e6e8';px(g,13,1,'#e0e6e8');g.fillStyle='#6a4a32';g.fillRect(11,11,5,1);
  g.fillStyle='#7a8088';g.fillRect(16,9,8,6);g.fillStyle='#9aa0a6';g.fillRect(17,9,6,2);g.fillStyle='#c9a24a';g.fillRect(19,12,2,2);
  g.fillStyle='#5a5f66';g.fillRect(7,4,5,4);g.fillStyle='#e6dcc6';px(g,6,4,'#e6dcc6');px(g,12,4,'#e6dcc6')}
 else{g.fillStyle='#a8aeb0';g.fillRect(8,12,8,1);g.fillStyle='#6a4a32';g.fillRect(6,12,2,1)}return c}
function rubble(){const c=mk(14,6),g=pix(c);for(const [x,y] of [[1,3],[5,2],[9,3],[4,4]]){g.fillStyle='#7d847c';g.fillRect(x,y,3,2);g.fillStyle='#9aa096';g.fillRect(x,y,3,1)}return c}
function stub(col){const c=mk(10,6),g=pix(c);g.fillStyle='#3f6a52';g.fillRect(4,2,1,4);g.fillRect(2,4,6,1);g.fillStyle=col;px(g,4,1,col);return c}
function oreVein(iron){const c=makeOre(),g=pix(c);if(iron){g.globalCompositeOperation='source-atop';for(const [x,y] of [[5,4],[11,6],[14,4]]){g.fillStyle='#c8d0d8';g.fillRect(x,y,2,1)}}return c}

// ---------- name lookup for layout export ----------
const NAMES=new Map();
async function run(){
 const R0=performance.now();
 // ===== Shared =====
 const SH='Shared/';
 const cf=[];for(let i=0;i<6;i++)cf.push(cliffFace(i));await save(SH+'Cliff/Cliff_Face.png',strip(cf),{frames:6,fw:16,fh:56,pivot:'0,0',note:'6 variants; rows 0-5 plateau; 6-8 grass lip; 52-55 shadow cast on grass; offset tiles 1-3px vertically to jag the rim'});
 const vs=[vine(6,0),vine(10,1),vine(13,2),vine(16,3)];for(let i=0;i<4;i++)await save(SH+'Cliff/Vine_'+i+'.png',vs[i],{pivot:'0,1',note:'hang from the lip'});
 const wf=[];for(let f=0;f<6;f++)wf.push(waterfallFrame(f));await save(SH+'Cliff/Waterfall.png',strip(wf),{frames:6,fw:20,fh:48,pivot:'0.5,1',note:'loop 12 fps; tile vertically for taller cliffs'});
 const fo=[];for(let f=0;f<4;f++)fo.push(foamFrame(f));await save(SH+'Cliff/Waterfall_Foam.png',strip(fo),{frames:4,fw:28,fh:6,note:'loop 10 fps at the waterfall base'});
 await save(SH+'Cliff/Cave_Mouth.png',caveMouth(),{note:'sits on the cliff base'});
 const fb=[],fs=[];for(let f=0;f<6;f++){fb.push(fireFrame(true,f));fs.push(fireFrame(false,f))}
 await save(SH+'Fire/Fire_Big.png',strip(fb),{frames:6,fw:11,fh:9,note:'10 fps; pivot at fire base'});await save(SH+'Fire/Fire_Small.png',strip(fs),{frames:6,fw:7,fh:7,note:'10 fps; braziers and rest-point fires'});
 await save(SH+'Fire/FireRing_Big.png',fireRing(true),{pivot:'0.5,0.5',note:'ground decal under Fire_Big'});await save(SH+'Fire/FireRing_Small.png',fireRing(false),{pivot:'0.5,0.5'});
 await save(SH+'Fire/Brazier.png',makeBrazier(),{note:'Fire_Small sits 14px above the pivot'});
 await save(SH+'Fire/Glow_Warm.png',soft('rgba(255,150,60,',64,64),{pivot:'0.5,0.5',note:'additive; flicker 0.85-1.0'});await save(SH+'Fire/Glow_Violet.png',soft('rgba(190,140,255,',64,64),{pivot:'0.5,0.5',note:'additive; sacred tree'});
 const P_='Shared/Particles/';
 const one=mk(1,1);pix(one).fillStyle='#fff';pix(one).fillRect(0,0,1,1);await save(P_+'Px_White.png',one,{pivot:'0.5,0.5',note:'tint per particle: sparks #ffd36a #ff9a3c #fff0b0; twinkle; motes #f4f2d0; dust'});
 await save(P_+'Smoke.png',strip([smoke(2),smoke(3),smoke(5),smoke(7)].map(s=>{const c=mk(7,7);pix(c).drawImage(s,(7-s.width)>>1,(7-s.height)>>1);return c})),{frames:4,fw:7,fh:7,pivot:'0.5,0.5',note:'grow over 3s; alpha 0.35 to 0'});
 const lv=['#7aa884','#5f8f74','#9cc28a','#c6b25e'].map(col=>{const c=mk(2,1);pix(c).fillStyle=col;pix(c).fillRect(0,0,2,1);return c});await save(P_+'Leaf.png',strip(lv),{frames:4,fw:2,fh:1,pivot:'0.5,0.5',note:'fall 6-12px/s with sine sway'});
 const rp=[];for(let f=0;f<4;f++)rp.push(ripple(f));await save(P_+'Ripple.png',strip(rp),{frames:4,fw:17,fh:9,pivot:'0.5,0.5',note:'1.6s per ripple'});
 await save(P_+'Firefly.png',firefly(),{pivot:'0.5,0.5',note:'additive; blink with sin'});await save(P_+'Wind_Streak.png',streak(),{pivot:'0,0',note:'moves 150,38 px/s; alpha max 0.22'});
 await save(P_+'Mist_Blob.png',soft('rgba(220,232,236,',64,40),{pivot:'0.5,0.5',note:'scale x3-5; alpha 0.16'});await save(P_+'Cloud_Shadow.png',soft('rgba(8,20,24,',64,40),{pivot:'0.5,0.5',note:'scale x3-5; alpha 0.14; drift 9,3 px/s'});
 const sw=[];for(let f=0;f<6;f++)sw.push(swirl(f));await save(SH+'Portal/Portal_Frame.png',makePortal(),{note:'colliders on both pillars'});await save(SH+'Portal/Portal_Swirl.png',strip(sw),{frames:6,fw:24,fh:30,pivot:'0.5,0.5',note:'additive; 8 fps; centre 22px above the frame pivot'});
 await save(SH+'Props/Chest_Closed.png',makeChest(false));await save(SH+'Props/Chest_Open.png',makeChest(true));
 await save(SH+'Props/Bench.png',T.Bench);await save(SH+'Props/Signpost.png',T.Signpost_Up);await save(SH+'Props/Barrel.png',T.Barrel);await save(SH+'Props/Crate.png',T.Crate);await save(SH+'Props/Crate_Herbs.png',T.Crate_Herbs);await save(SH+'Props/Sacks.png',T.Sacks);await save(SH+'Props/Hay.png',T.Hay);
 await save(SH+'Props/Log_Fallen.png',makeLog(),{note:'sprout on top; collider 36x8'});await save(SH+'Props/Log_Seat.png',makeSeat());
 for(let i=0;i<3;i++)await save(SH+'Props/Boulder_'+'SML'[[1,0,2][i]]+'.png',T.boulders[i]);
 // bridge + stepping stone
 {const c=mk(16,19),g=pix(c);for(let x=0;x<16;x++)for(let y=0;y<19;y++){let o=x%4===3?P.wood[0]:x%4===0?P.wood[3]:P.wood[2];if(((y+Math.floor(x/4)*7)%17)===0)o=P.wood[0];if(y===0||y===18)o=P.wood[0];px(g,x,y,o)}await save(SH+'Bridge/Bridge_Deck_H.png',c,{pivot:'0,0',note:'tile horizontally across the water'})}
 {const c=mk(16,5),g=pix(c);g.fillStyle=P.wood[0];g.fillRect(0,1,16,2);g.fillStyle=P.wood[3];g.fillRect(0,1,16,1);g.fillStyle=P.wood[0];g.fillRect(0,0,2,5);await save(SH+'Bridge/Bridge_Rail_H.png',c,{pivot:'0,0',note:'top edge of the deck; posts every 16px'})}
 {const c=mk(19,16),g=pix(c);for(let x=0;x<19;x++)for(let y=0;y<16;y++){let o=y%4===3?P.wood[0]:y%4===0?P.wood[3]:P.wood[2];if(((x+Math.floor(y/4)*7)%17)===0)o=P.wood[0];if(x===0||x===18)o=P.wood[0];px(g,x,y,o)}await save(SH+'Bridge/Bridge_Deck_V.png',c,{pivot:'0,0'})}
 {const c=mk(13,10),g=pix(c);for(let y=0;y<9;y++)for(let x=0;x<13;x++){if(Math.pow((x-6)/6.5,2)+Math.pow((y-4)/4.5,2)>1)continue;px(g,x,y,y===8?'#5d625c':hash(x>>2,y>>2,73)<0.5?'#7e847c':'#9aa096')}g.fillStyle='rgba(10,20,24,0.35)';g.fillRect(1,9,11,1);await save(SH+'Bridge/Stepping_Stone.png',c,{pivot:'0.5,0.5'})}
 // resource nodes
 const N='Shared/Nodes/';const nodesOut=[['Wood',woodPile(true),woodPile(false)],['Hide',hideRack(true),hideRack(false)],['Herb',makeHerb(),stub('#9fd8e8')],['Mushroom',makeMush(),stub('#3ab0a0')],['Sap',sapTree(true),sapTree(false)],['SpiritFlower',makeSpirit(),stub('#b48ae0')],['Stone',makeSpire(),rubble()],['IronOre',oreVein(false),rubble()],['IronScrap',ironScrap(true),ironScrap(false)],['Spoils',spoils(true),spoils(false)]];
 for(const [n,f,d] of nodesOut){await save(N+n+'_Full.png',f,{note:'gatherable'});await save(N+n+'_Depleted.png',d,{note:'shown while respawning'})}
 // ===== Camp (palette-specific) =====
 const C='Camp/';
 const mats=['grass','dark','forest','dirt','water','pave'];for(const m of mats){const v=[];for(let i=0;i<4;i++)v.push(matTile(m,i));await save(C+'Ground/Fill_'+m[0].toUpperCase()+m.slice(1)+'.png',strip(v),{frames:4,fw:16,fh:16,pivot:'0,0',note:'4 seamless fill variants'})}
 for(const pr of PAIRS){const t=[];for(let m=0;m<16;m++)t.push(dualTile(pr,m,300+PAIRS.indexOf(pr)));await save(C+'Ground/Dual_'+pr.name+'.png',grid(t,4),{frames:16,fw:16,fh:16,pivot:'0,0',note:'dual-grid; index = TL + 2*TR + 4*BL + 8*BR (1 = '+pr.B+'); laid out 4 per row'})}
 // tall grass, decals
 const tg=[];for(let p=0;p<2;p++)for(let f=0;f<3;f++)tg.push(TG[f][p]);await save(C+'Ground/TallGrass.png',grid(tg,3),{frames:6,fw:10,fh:9,note:'row 0 light / row 1 dark; frames lean L, up, R; drive with a wind wave'});
 {const dec=[];const tuft=(cols)=>{const c=mk(4,3),g=pix(c);px(g,0,0,cols);px(g,2,0,cols);px(g,1,1,cols);return c};dec.push(tuft(P.T1),tuft(P.T3),tuft(P.G2));
  for(const cs of [['#c9a6e0','#ffffff'],['#f2e6a0','#ffffff'],['#e9a0b0','#fff3f3'],['#a6c8f0','#e8f4ff']]){const c=mk(4,4),g=pix(c);px(g,1,2,'#2f564c');px(g,1,3,'#2f564c');g.fillStyle=cs[0];g.fillRect(0,1,3,1);g.fillRect(1,0,1,3);px(g,1,1,cs[1]);dec.push(c)}
  {const c=mk(4,4),g=pix(c);g.fillStyle='#e7dcc4';g.fillRect(1,2,1,2);g.fillStyle='#b0483c';g.fillRect(0,0,3,2);px(g,1,0,'#f3e6d0');dec.push(c)}
  {const c=mk(4,4),g=pix(c);g.fillStyle='#8d8a7e';g.fillRect(0,1,2,1);g.fillStyle='#6b675e';g.fillRect(0,2,2,1);px(g,0,1,'#b4b0a2');dec.push(c)}
  {const c=mk(4,4),g=pix(c);g.fillStyle='#e6dcc6';g.fillRect(0,1,4,1);g.fillStyle='#a89c86';g.fillRect(0,2,4,1);dec.push(c)}
  const big=dec.map(d=>{const c=mk(4,4);pix(c).drawImage(d,0,0);return c});await save(C+'Ground/Decals.png',strip(big),{frames:big.length,fw:4,fh:4,pivot:'0.5,0',note:'0-2 grass tufts; 3-6 flowers (cluster 4-9); 7 mushroom; 8 pebble; 9 bone'})}
 for(let i=0;i<9;i++)await save(C+'Ground/Deco_'+i+'.png',DECO_TINT[i],{note:'village deco recoloured for Camp'});
 // trees: canopy and trunk on separate layers of the same size and pivot
 const TN=['Teal','Leaf','TealDark','LeafDark','TealCool'];
 for(let i=0;i<5;i++){const img=T.trees[i];const can=mk(80,72),tr=mk(80,72);pix(can).drawImage(img,0,0,80,50,0,0,80,50);pix(tr).drawImage(img,0,50,80,22,0,50,80,22);
  await save(C+'Trees/Tree_'+TN[i]+'_Canopy.png',can,{pivot:'0.5,0.028',note:'sways <=0.55px; fades to 42% when the hero stands behind'});await save(C+'Trees/Tree_'+TN[i]+'_Trunk.png',tr,{pivot:'0.5,0.028',note:'static; collider r7 at the base'})}
 await save(C+'Trees/Tree_Shadow.png',T.Shadow_TreeV3,{pivot:'0.5,0.55'});
 for(let i=0;i<3;i++)await save(C+'Trees/Bush_'+['Teal','Leaf','Dark'][i]+'.png',T.bushes[i],{note:'top part sways 0.35px; bottom 8 rows static'});
 await save(C+'Trees/Stump.png',T.Stump);
 // camp landmarks
 await save(C+'Props/Tent_Horn.png',T.Tent,{pivot:'0.5,0.025',note:'collider 44x20'});await save(C+'Props/Flag_Tattered.png',T.Flag,{note:'wave per column: sin(t*4.2-col*0.45)*1.3, pole cols 0-3 fixed'});
 await save(C+'Props/Rack_Spears.png',makeSpears());await save(C+'Props/Archer_Platform.png',makeWatch(P.wood),{note:'archer stands 30px above the pivot'});
 await save(C+'Props/Tent_Ruined.png',makeRuinTent(),{pivot:'0.5,0.025',note:'Camp secret: abandoned tent in the NE bush ring'});
 const R1=performance.now();return {files:MAN.length,ms:Math.round(R1-R0)}}
// ---------- per-map export helpers ----------
const TN=['Teal','Leaf','TealDark','LeafDark','TealCool'];
const NODE_FILE={stone:'Stone_Full',herb:'Herb_Full',mush:'Mushroom_Full',spirit:'SpiritFlower_Full',ore:'IronOre_Full'};
function spriteName(e){const NM=new Map();
 T.trees.forEach((c,i)=>NM.set(c,'Tree_'+TN[i]));T.bushes.forEach((c,i)=>NM.set(c,'Bush_'+['Teal','Leaf','Dark'][i]));T.boulders.forEach((c,i)=>NM.set(c,'Boulder_'+'SML'[[1,0,2][i]]));
 for(const n of ['Stump','Barrel','Crate','Crate_Herbs','Sacks','Hay','Bench','Signpost_Up'])NM.set(T[n],n==='Signpost_Up'?'Signpost':n);NM.set(T.Tent,'Tent_Horn');NM.set(T.Flag,'Flag_Tattered');
 NM.set(PROPS.LOG,'Log_Fallen');NM.set(PROPS.SEAT,'Log_Seat');NM.set(PROPS.RACK,'Hide_Full');NM.set(PROPS.SPEARS,'Rack_Spears');NM.set(PROPS.BRAZ,'Brazier');NM.set(portal.img,'Portal_Frame');NM.set(T.chestC,'Chest_Closed');NM.set(T.chestO,'Chest_Closed');
 spriteName.NM=NM}
function nameOf(e){const NM=spriteName.NM;if(e.k==='node')return NODE_FILE[e.type];if(e.rustle)return 'Bush_Rustle';const n=NM.get(e.img);if(n)return n;
 if(e.img&&e.img.width===8)return 'Palisade_Post_'+e.img.height;return null}
async function exportMap(dir,title,labels,notes){spriteName();const items=[],unk=new Set();
 for(const e of ents){if(e.k==='mob')continue;const name=nameOf(e);if(!name){unk.add(e.k+':'+(e.img&&e.img.width+'x'+e.img.height));continue}
  items.push({sprite:name,x:Math.round(e.x),y:Math.round(e.y),layer:e.plateau?'plateau':e.k==='tree'?'tree':e.k==='node'?'node':'prop'})}
 const mobsOut=mobs.map(m=>({mob:m.name,sheet:Object.keys(SHT).find(k=>SHT[k]===m.sh),x:Math.round(m.spawn[0]),y:Math.round(m.spawn[1]),lv:m.lv,boss:!!m.boss,home:[Math.round(m.hx),Math.round(m.hy)],radius:m.hr,patrol:m.patrol||null,orbit:m.orb||null,lift:m.lift||0,caged:!!m.caged}));
 const layout={map:MAP.name,units:'pixels at PPU 16; origin top-left, y grows downward (Unity: worldY = -y/16)',size:[W,H],
  terrain:{branches:MAP.branches||null,clears:MAP.clears,patches:MAP.patches,paths:MAP.paths,stream:MAP.stream,ponds:MAP.ponds,waterfallX:MAP.fall,pave:MAP.pave||null,steps:MAP.steps||null,cave:MAP.cave||null,dark:MAP.dark||null,cliffRimY:'24-38 (noise)',cliffBaseY:'70-93 (noise)',bridges:BRIDGES},
  portal:MAP.portal,rest:MAP.rest,fires:fires.map(f=>({x:f.x,y:f.y,big:!!f.big,lift:f.lift})),secretChest:[chest.x,chest.y],items,mobs:mobsOut,notes};
 await saveText(dir+title+'_Layout.json',JSON.stringify(layout,null,1));
 // full-map render without HUD/weather
 const keep={dh:drawHUD,dl:drawLabels,vig:T.vig,S,SW,SH,VWW,VWH};drawHUD=()=>{};drawLabels=()=>{};T.vig=mk(1,1);parts.length=0;streaks.length=0;const cl=clouds.splice(0);bannerT=99;
 hero.x=MAP.spawn[0];hero.y=MAP.spawn[1];S=1;SW=W;SH=H;cv.width=W;cv.height=H;VWW=W;VWH=H;cam.x=0;cam.y=0;for(const e of ents)if(e.k==='tree')e.a=1;render();
 const plain=mk(W,H);plain.getContext('2d').drawImage(cv,0,0);await save(dir+title+'_Overview.png',plain,{pivot:'0,1',note:'whole map at 1x from the approved demo; layout reference only'});
 const an=mk(W,H),g=an.getContext('2d');g.drawImage(plain,0,0);g.font='600 14px "Pixelify Sans"';g.lineJoin='round';g.textAlign='center';
 const lab=(t,x,y,c)=>{g.lineWidth=4;g.strokeStyle='rgba(10,16,16,0.9)';g.strokeText(t,x,y);g.fillStyle=c||'#f6d860';g.fillText(t,x,y)};
 for(const l of labels)lab(l[0],l[1],l[2],l[3]);
 for(const n of nodes){g.fillStyle=NODE[n.type].c;g.fillRect(n.x-3,n.y-3,6,6);g.strokeStyle='#000';g.strokeRect(n.x-3.5,n.y-3.5,7,7)}
 for(const m of mobs){g.fillStyle=m.boss?'#ff5a3a':m.caged?'#b8a0ff':'#e08a6a';g.beginPath();g.arc(m.spawn[0],m.spawn[1],m.boss?6:3.5,0,7);g.fill()}
 g.textAlign='left';g.fillStyle='rgba(10,16,16,0.75)';g.fillRect(10,H-70,560,60);g.font='600 13px "Pixelify Sans"';
 lab('● quái (đỏ: boss)   ■ điểm tài nguyên (màu theo loại)',20,H-46,'#f2efe2');lab('1 px = 1/16 unit · toạ độ khớp '+title+'_Layout.json',20,H-24,'#c8c4b4');
 await save(dir+title+'_Overview_Annotated.png',an,{pivot:'0,1',note:'annotated layout'});
 drawHUD=keep.dh;drawLabels=keep.dl;T.vig=keep.vig;clouds.push(...cl);resize();
 return {items:items.length,mobs:mobsOut.length,unknown:[...unk]}}
async function writeBoard(file,title,filter){const items=BOARD.filter(it=>!filter||filter(it));const sc=3;const groups={};for(const it of items){const k=it.rel.split('/').slice(0,2).join('/');(groups[k]=groups[k]||[]).push(it)}
 const BW=1920;let layout=[],y=70;
 for(const [k,list] of Object.entries(groups)){layout.push({head:k,y});y+=34;let x=20,rowH=0;for(const it of list){const lim=it.c.width>400?1:sc;const w=it.c.width*lim,h=it.c.height*lim;if(x+Math.max(w,160)>BW-20){x=20;y+=rowH+30;rowH=0}layout.push({it,x,y,w,h});x+=Math.max(w,110)+24;rowH=Math.max(rowH,h)}y+=rowH+46}
 const B=mk(BW,y+20),g=B.getContext('2d');g.fillStyle='#10191a';g.fillRect(0,0,BW,B.height);g.imageSmoothingEnabled=false;
 g.font='700 30px "Pixelify Sans"';g.fillStyle='#f6d860';g.fillText(title,20,46);
 for(const L of layout){if(L.head){g.font='700 20px "Pixelify Sans"';g.fillStyle='#9fe6dc';g.fillText(L.head,20,L.y+20);g.fillStyle='#2a3a38';g.fillRect(20,L.y+26,BW-40,2);continue}
  g.fillStyle='#1b2826';g.fillRect(L.x-4,L.y-4,L.w+8,L.h+8);g.drawImage(L.it.c,L.x,L.y,L.w,L.h);g.font='600 13px "Pixelify Sans"';g.fillStyle='#d8d4c4';g.fillText(L.it.rel.split('/').pop().replace('.png',''),L.x,L.y+L.h+18)}
 await fetch(SAVE+encodeURIComponent(file),{method:'POST',body:B.toDataURL('image/png')});return [B.width,B.height]}
async function writeManifest(file){await saveText(file,'file,width,height,frames,frame_w,frame_h,pivot_xy_normalized,note\n'+MAN.map(m=>m.join(',')).join('\n')+'\n')}
// ---------- Trail ----------
function rustleFrames(){const b=T.bushes[0],w=b.width,h=b.height;return [-1,0,1].map(o=>{const c=mk(w+2,h),g=pix(c);g.drawImage(b,0,0,w,h-8,1+o,0,w,h-8);g.drawImage(b,0,h-8,w,8,1,h-8,w,8);return c})}
function hedge(n,s){// a pre-composed run of overlapping bushes for the trail walls
 const c=mk(28+n*14,30),g=pix(c);for(let i=0;i<n+1;i++){const b=T.bushes[(i+s)%2===0?0:(i%3===2?2:1)];g.drawImage(shadow(28),i*14+4,22);g.drawImage(b,i*14+Math.round(hash(i,s,5)*3),2+Math.round(hash(i,s,6)*3))}return c}
async function runTrail(){const D='Trail/';
 for(let h=22;h<=29;h++)await save('Shared/Fortify/Palisade_Post_'+h+'.png',makePost(h,P.wood,h),{note:'wood post; rows of posts every 7px; collider r4'});
 await save(D+'Props/Bush_Rustle.png',strip(rustleFrames()),{frames:3,fw:T.bushes[0].width+2,fh:T.bushes[0].height,note:'secret entrance; no collider; shake 3 frames at 20 fps when the hero is within 14px and every ~2s idle'});
 for(let i=0;i<3;i++)await save(D+'Props/Hedge_'+(i+2)+'.png',hedge(i+2,i),{note:'run of '+(i+3)+' overlapping bushes; colliders every 14px'});
 const r=await exportMap(D,'Trail',[
  ['⛩ Cổng về Làng',MAP.portal[0]+40,MAP.portal[1]-56,'#9fe6dc'],['1 Trại Goblin Lv5',430,262,'#ffc8a0'],['2 Đồng hoa Pixie Lv6',660,975,'#d8c0ff'],['3 Ổ cung thủ Lv7',930,335,'#ffc8a0'],['4 Bãi Satyr Lv8',1270,945,'#d8c0ff'],['5 Chuồng Gobrider Lv9',1650,282,'#ffc8a0'],['6 Rừng nấm Lv10',1850,995,'#9ff0e0'],
  ['Gobrider tuần tra đường chính',500,540],['Gobrider tuần tra',1500,520],['Cầu + suối → hồ',1100,610,'#b6d8cd'],['Trạm nghỉ',1420,385,'#9fe6a0'],['Bí mật: bụi rung → rương',770,440,'#fff2a0'],['Sân boss: Gobrider Captain',2060,462,'#ff9a6a']],
  ['Trail uses the base (Camp) palette: reuse Camp/Ground and Camp/Trees unchanged.','Depth darkness: world-space gradient from x=800 (0) to the east edge (0.42 black-teal #060e16); spawn fireflies more as it darkens.',
   'Layout: one main trail (paths[0]) with six side branches (paths[1..6]) to training grounds 1-6, a rest spur (paths[7]) and the secret spur (paths[8]); branch forks are listed in branches[] with a Signpost at each.','Hedges: Bush_* along both main-trail sides every 9px at 24-32px offset, opened where a branch leaves; Hedge_2..4 are pre-composed runs to place faster.','Boss arena posts: Palisade_Post_* ring open to the west.']);
 return r}
window.ARTGEN={run,runTrail,exportMap,writeBoard,writeManifest,MAN,BOARD,saveText,save,strip,grid,matTile,dualTile,PAIRS,NAMES};
})();
