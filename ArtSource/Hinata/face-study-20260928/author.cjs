// 生成済みC案の顔パーツだけを、編集可能なストロークで描き直す試験。
// 人間の手描きではない。髪・体・衣装の線は生成原画をそのまま残す。
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const sharp = require('C:/Users/issak/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const engine = require('C:/Users/issak/.codex/skills/ink-workshop/assets/engine.js');
const root = __dirname, source = path.join(root, '../line-proposals-20260928/C-compact-sd.png');
const ink = '#322A35';
const patches = [
  [[292,514],[330,505],[391,511],[435,541],[451,590],[445,628],[378,637],[331,620],[308,580]],
  [[531,458],[552,427],[602,435],[639,444],[676,463],[674,506],[649,558],[626,581],[565,578],[533,541]],
  [[469,613],[526,600],[553,610],[554,667],[495,673],[469,651]],
  [[356,494],[393,499],[420,519],[416,530],[358,514]],
  [[529,426],[564,410],[585,417],[584,428],[533,451]]
];
function cubic(start, cs) {
  const pts=[start]; let a=start;
  for(const c of cs) {
    if(c.length===2) { pts.push(c); a=c; continue; }
    for(let i=1;i<=18;i++) { const t=i/18,q=1-t; pts.push([q*q*q*a[0]+3*q*q*t*c[0]+3*q*t*t*c[2]+t*t*t*c[4],q*q*q*a[1]+3*q*q*t*c[1]+3*q*t*t*c[3]+t*t*t*c[5]]); }
    a=c.slice(4);
  } return pts;
}
function makeScene(expression) {
  const s={version:1,title:'ひなた / 顔の省略設計 '+expression,width:1024,height:1536,
    layers:[{id:'erase',name:'旧パーツを消す局所領域'},{id:'face',name:'目・眉・口の再設計'}],strokes:[]};
  let id=0;
  const fill=(name,pts,color=ink,layer='face')=>s.strokes.push({id:name+'-'+id++,layer,kind:'fill',color,duration:.1,points:pts});
  const line=(name,pts,size=6)=>s.strokes.push({id:name+'-'+id++,layer:'face',kind:'brush',color:ink,size,thinning:.6,streamline:0,smoothing:.1,duration:.2,
    points:pts.map((p,i)=>[...p,i===0||i===pts.length-1?.15:.55])});
  patches.forEach((pts,i)=>fill('erase'+i,pts,'#FFFFFF','erase'));
  if(expression==='normal') {
    // 左右を鏡像にせず、頭の傾きに合わせて瞼の角度と開きを分ける。
    fill('lid-left',cubic([316,553],[[346,519,395,512,427,546],[400,532,365,534,339,556],[322,560]]));
    line('eye-left',cubic([329,559],[[321,595,352,625,395,618],[420,612,435,586,426,553]]),4);
    fill('iris-left',cubic([374,544],[[395,539,408,560,403,581],[398,603,381,606,370,590],[359,574,360,552,374,544]]));
    fill('eye-light-left',cubic([372,552],[[380,548,388,552,386,563],[379,569,371,563,372,552]]),'#FFFFFF');
    fill('lid-right',cubic([546,493],[[570,456,625,442,657,478],[633,463,586,470,562,501],[546,505]]));
    line('eye-right',cubic([558,501],[[551,540,573,567,612,562],[642,558,653,531,651,492]]),4);
    fill('iris-right',cubic([603,474],[[625,468,634,489,629,515],[624,541,609,552,596,535],[583,519,583,488,603,474]]));
    fill('eye-light-right',cubic([595,484],[[604,477,612,481,610,493],[604,501,595,497,595,484]]),'#FFFFFF');
    line('brow-left',cubic([361,504],[[378,503,392,509,404,520]]),4);
    line('brow-right',cubic([539,443],[[551,429,565,423,578,422]]),4);
    line('mouth',cubic([480,636],[[494,650,520,645,540,622]]),5);
  } else if(expression==='cheer') {
    // 勝利時は目の輝きではなく、瞼・口・眉の形で反応を分ける。
    line('smile-left',cubic([321,570],[[345,541,389,535,420,553]]),10);
    line('smile-left-lash',[[329,564],[316,552]],5);
    line('smile-right',cubic([548,507],[[576,473,613,464,646,484]]),10);
    line('smile-right-lash',[[635,479],[652,463]],5);
    line('brow-left',cubic([359,507],[[375,501,393,506,405,513]]),4);
    line('brow-right',cubic([540,443],[[548,433,567,425,579,426]]),4);
    fill('mouth',cubic([478,631],[[498,633,523,626,542,616],[542,643,519,665,502,657],[490,652,482,639,478,631]]));
    fill('tongue',cubic([502,650],[[512,641,526,640,529,647],[519,658,508,659,502,650]]),'#C88591');
  } else {
    line('eye-left',cubic([330,571],[[351,549,384,543,416,558]]),8);
    line('eye-left-lower',cubic([336,578],[[340,601,361,614,386,609],[405,605,414,586,412,569]]),4);
    fill('pupil-left',cubic([374,556],[[388,552,394,568,388,588],[377,604,366,591,367,574],[368,565,370,559,374,556]]));
    line('eye-right',cubic([550,507],[[574,486,613,479,645,492]]),8);
    line('eye-right-lower',cubic([557,514],[[558,541,580,558,606,551],[630,546,640,523,639,502]]),4);
    fill('pupil-right',cubic([600,491],[[614,486,620,503,614,524],[601,541,590,528,592,508],[594,500,596,495,600,491]]));
    line('brow-left',cubic([361,498],[[377,500,397,514,409,525]]),6);
    line('brow-right',cubic([539,451],[[551,435,568,422,581,418]]),6);
    line('mouth',cubic([490,640],[[502,630,520,626,534,627]]),5);
  }
  return engine.validate(s);
}
async function main() {
  const raw=fs.readFileSync(source), before=await sharp(raw).ensureAlpha().raw().toBuffer();
  const maskSVG='<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1536">'+patches.map(p=>'<polygon fill="white" points="'+p.map(x=>x.join(',')).join(' ')+'"/>').join('')+'</svg>';
  const mask=await sharp(Buffer.from(maskSVG)).ensureAlpha().raw().toBuffer();
  const records=[];
  for(const kind of ['normal','cheer','focus']) {
    const scene=makeScene(kind), svg=engine.svg(scene);
    fs.writeFileSync(path.join(root,kind+'.scene.json'),JSON.stringify(scene,null,2)+'\n');
    fs.writeFileSync(path.join(root,kind+'.svg'),svg);
    const overlay=await sharp(Buffer.from(svg)).png().toBuffer();
    const png=await sharp(raw).composite([{input:overlay}]).png().toBuffer();
    fs.writeFileSync(path.join(root,kind+'.png'),png);
    await sharp(png).resize({height:220}).png().toFile(path.join(root,kind+'-220.png'));
    const after=await sharp(png).ensureAlpha().raw().toBuffer(); let outsideChanges=0;
    // 線のアンチエイリアス周辺を含め、局所パーツ外が実際に一致するか検査。
    for(let i=0;i<before.length;i+=4) if(mask[i+3]===0 && !before.subarray(i,i+4).equals(after.subarray(i,i+4))) outsideChanges++;
    records.push({kind,outsideChanges});
  }
  const tiles=[]; const kinds=['original','normal','cheer','focus'];
  for(let i=0;i<kinds.length;i++) {
    const png=kinds[i]==='original'?raw:fs.readFileSync(path.join(root,kinds[i]+'.png'));
    tiles.push({input:await sharp(png).resize({height:440}).png().toBuffer(),left:i*305+5,top:35});
  }
  const labels='<svg xmlns="http://www.w3.org/2000/svg" width="1220" height="500"><style>text{font:18px sans-serif;fill:#322a35}</style>'+kinds.map((x,i)=>'<text x="'+(i*305+30)+'" y="25">'+x+'</text>').join('')+'</svg>';
  await sharp({create:{width:1220,height:500,channels:4,background:'#FFFFFF'}}).composite([...tiles,{input:Buffer.from(labels)}]).png().toFile(path.join(root,'comparison.png'));
  fs.writeFileSync(path.join(root,'manifest.json'),JSON.stringify({source:path.relative(root,source),sha256:crypto.createHash('sha256').update(raw).digest('hex'),status:'検討用・未採用',imageGenerationCalls:0,humanDrawing:false,records},null,2)+'\n');
  console.log(JSON.stringify(records));
}
main().catch(e=>{console.error(e);process.exitCode=1;});
