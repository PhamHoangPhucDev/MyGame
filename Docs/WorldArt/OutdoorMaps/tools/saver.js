// Tiny local helper: the art generator page POSTs PNG data URLs here and they are written under OUT.
const http=require('http'),fs=require('fs'),path=require('path');
const OUT=process.argv[2];const PORT=+process.argv[3]||8767;
http.createServer((req,res)=>{
 res.setHeader('Access-Control-Allow-Origin','*');res.setHeader('Access-Control-Allow-Headers','*');
 if(req.method==='OPTIONS'){res.end();return}
 const u=new URL(req.url,'http://x');const rel=u.searchParams.get('p')||'';
 if(req.method!=='POST'||!rel||rel.includes('..')||path.isAbsolute(rel)){res.statusCode=400;res.end('bad');return}
 let body='';req.on('data',d=>body+=d);req.on('end',()=>{
  const f=path.join(OUT,rel);fs.mkdirSync(path.dirname(f),{recursive:true});
  if(body.startsWith('data:'))fs.writeFileSync(f,Buffer.from(body.split(',')[1],'base64'));else fs.writeFileSync(f,body);
  res.end('ok '+rel)})
}).listen(PORT,'127.0.0.1',()=>console.log('saver on',PORT,'->',OUT));
