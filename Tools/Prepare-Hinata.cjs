// ユーザー承認済みの背景切り抜きと、暫定仕事着へのレタッチ。原本は変更しない。
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('C:/Users/issak/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
async function main() {
  const source = process.argv[2];
  if (!source) throw new Error('元画像を指定してください');
  const {data, info} = await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const {width:w, height:h} = info;
  if (w !== 1254 || h !== 1254) throw new Error('この衣装レタッチは1254×1254の暫定原画専用です');
  const seen = new Uint8Array(w*h), queue = new Int32Array(w*h);
  let head=0, tail=0;
  function add(i) {
    if (seen[i]) return;
    const r=data[i*4], g=data[i*4+1], b=data[i*4+2];
    if (Math.max(r,g,b)-Math.min(r,g,b)>25 || Math.min(r,g,b)<55) return;
    seen[i]=1; queue[tail++]=i;
  }
  for (let x=0;x<w;x++){add(x);add((h-1)*w+x);}
  for (let y=0;y<h;y++){add(y*w);add(y*w+w-1);}
  while(head<tail){
    const i=queue[head++], x=i%w, y=Math.floor(i/w);
    if(x>0)add(i-1);if(x<w-1)add(i+1);if(y>0)add(i-w);if(y<h-1)add(i+w);
  }
  for(let i=0;i<w*h;i++){
    const x=i%w, y=Math.floor(i/w);
    const rgb=[data[i*4],data[i*4+1],data[i*4+2]];
    const trappedChecker = Math.max(...rgb)-Math.min(...rgb)<18 && Math.min(...rgb)>80 && Math.max(...rgb)<220;
    if(seen[i] || trappedChecker || (y>=698 && x>=430 && x<=820) || y>=800) data[i*4+3]=0;
  }
  const headImage=await sharp(data,{raw:{width:w,height:h,channels:4}}).png().toBuffer();
  const outfit=fs.readFileSync(path.join(__dirname,'Hinata-workwear.svg'));
  const output=path.resolve(__dirname,'../Assets/Sprites/Hinata/hinata_normal.png');
  await sharp({create:{width:w,height:h,channels:4,background:{r:0,g:0,b:0,alpha:0}}})
    .composite([{input:outfit},{input:headImage}]).png().toFile(output);
  const stats=await sharp(output).stats();
  if(stats.channels[3].min!==0 || stats.channels[3].max!==255) throw new Error('透過検査失敗');
  console.log(JSON.stringify({output,transparentBackground:true,removedBackgroundPixels:tail}));
}
main().catch(error=>{console.error(error);process.exit(1);});
