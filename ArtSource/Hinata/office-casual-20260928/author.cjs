// 衣装案のみをコードで作画。顔・髪は暫定素材を保持し、ゲームの原本は変更しない。
const fs = require('node:fs'), path = require('node:path');
const sharp = require('C:/Users/issak/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const engine = require('C:/Users/issak/.codex/skills/ink-workshop/assets/engine.js');
const root = __dirname;
const scene = {version:1,title:'ひなた：衣装・ポーズ検討案（顔と髪は別の暫定原画）',width:1254,height:1254,
  layers:[{id:'outfit',name:'衣装・手・小物（編集可能）'}],strokes:[]};
const ink='#32232B', skin='#FFDABF', white='#F4F6FB', rose='#C98296', navy='#3D455E';
let serial=0;
// 意図した曲線をBezierで標本化。揺らぎや自動陰影は付けない。
function curve(start, commands) {
  let p=start, points=[p];
  for(const c of commands) {
    if(c.length===2) {p=c;points.push(p);continue;}
    const a=p,b=c.slice(0,2),d=c.slice(2,4),e=c.slice(4,6);
    const steps=Math.max(8,Math.ceil((Math.hypot(b[0]-a[0],b[1]-a[1])+Math.hypot(e[0]-d[0],e[1]-d[1]))/8));
    for(let i=1;i<=steps;i++) {const t=i/steps,q=1-t;points.push([q*q*q*a[0]+3*q*q*t*b[0]+3*q*t*t*d[0]+t*t*t*e[0],q*q*q*a[1]+3*q*q*t*b[1]+3*q*t*t*d[1]+t*t*t*e[1]]);}
    p=e;
  }
  return points;
}
function line(name,color,points,size=7) {
  scene.strokes.push({id:name+'-'+(++serial),layer:'outfit',kind:'brush',color,size,thinning:0,smoothing:.15,streamline:0,duration:.3,points:points.map(p=>[...p,.5])});
}
function shape(name,color,start,commands,size=11) {
  const points=curve(start,commands);points.push(start);
  scene.strokes.push({id:name+'-fill',layer:'outfit',kind:'fill',color,duration:.2,points});line(name+'-edge',ink,points,size);
}
shape('neck',skin,[574,665],[[681,665],[679,755],[628,786],[577,747]]);
shape('leg-left',skin,[546,1005],[[601,1004],[593,1113],[542,1111]]);
shape('leg-right',skin,[642,1004],[[699,1005],[704,1109],[650,1112]]);
shape('shoe-left','#584450',[543,1098],[[563,1110,581,1110,593,1101],[603,1146],[602,1170,527,1179,518,1156],[522,1135,533,1114,543,1098]]);
shape('shoe-right','#584450',[650,1101],[[666,1112,688,1110,704,1098],[724,1143],[738,1171,656,1181,644,1158],[643,1138,647,1116,650,1101]]);
line('shoe-left-strap','#A891A0',curve([535,1135],[[553,1140,573,1143,591,1140]]),6);
line('shoe-right-strap','#A891A0',curve([653,1140],[[672,1142,693,1140,712,1135]]),6);
shape('skirt',navy,[530,915],[[584,908,683,908,729,917],[737,961,747,1010,750,1038],[690,1060,576,1063,510,1036],[519,990,527,949,530,915]]);
line('skirt-fold-left','#66728D',curve([565,958],[[558,985,558,1016,556,1039]]),6);
line('skirt-fold-right','#66728D',curve([696,958],[[704,985,704,1015,707,1039]]),6);
shape('blouse',white,[551,719],[[579,712],[606,737,649,737,676,713],[706,721],[723,846,732,921,718,948],[661,963,590,963,534,947],[527,861,535,779,551,719]]);
shape('cardigan-left',rose,[551,719],[[572,736,585,751,594,770],[587,837,584,907,590,951],[548,969,505,956,495,939],[501,856,506,776,551,719]]);
shape('cardigan-right',rose,[706,719],[[729,742,746,788,751,837],[758,886,763,921,764,939],[739,957,697,969,669,952],[671,885,659,817,656,770],[674,750,690,734,706,719]]);
// 小さな丸襟。制服の大きなリボン・黒い襟線は使わない。
shape('collar-left',white,[580,706],[[594,722,611,729,628,737],[614,756,594,759,579,751],[570,741,570,721,580,706]],8);
shape('collar-right',white,[676,708],[[684,725,686,741,675,751],[658,760,640,753,628,737],[647,728,662,719,676,708]],8);
line('blouse-placket','#BCC4D4',[[627,775],[627,838]],5);
line('cardigan-left-pocket','#995D76',curve([516,910],[[532,916,550,917,566,916]]),6);
line('cardigan-right-pocket','#995D76',curve([693,916],[[710,918,731,914,745,909]]),6);
// 片手で端末、片手で案内。両手の拳ポーズから職場の動作へ。
shape('sleeve-left',rose,[513,742],[[479,743,457,778,460,812],[463,844,480,869,505,886],[533,875,549,851,541,829],[519,815],[516,788,513,764,513,742]]);
shape('sleeve-right',rose,[739,741],[[759,735,773,760,788,785],[800,808,799,832,785,845],[768,857,747,845,733,828],[718,806],[729,782,739,761,739,741]]);
shape('cuff-left',white,[493,850],[[521,833],[549,859],[523,884]],8);
shape('cuff-right',white,[743,795],[[768,776],[792,806],[770,827]],8);
shape('hand-right',skin,[751,799],[[730,788,720,769,731,755],[738,746,747,746,752,739],[757,728,765,727,771,735],[779,747,778,761,786,773],[798,792,780,811,767,814],[756,809,755,803,751,799]],9);
line('right-thumb','#AF796C',curve([743,766],[[755,759,763,765,763,778]]),5);
shape('tablet','#56627A',[554,824],[[694,824],[706,837],[706,971],[548,971],[546,840]],9);
shape('tablet-screen','#DDE9F3',[568,840],[[690,840],[690,948],[565,948]],5);
line('tablet-lines-one','#91AABF',[[584,869],[666,869]],7);
line('tablet-lines-two','#91AABF',[[584,889],[646,889]],7);
shape('hand-left',skin,[512,848],[[521,838,537,836,547,843],[551,836,561,840,562,848],[572,841,581,848,578,856],[568,875,545,881,525,873],[513,869,507,859,512,848]],8);
line('left-fingers','#AF796C',curve([536,858],[[547,860,557,859,566,856]]),5);
// 目立ちすぎないクリップ式社員証。黄色・金縁を外す。
shape('badge','#F4F6FB',[691,789],[[729,789],[730,842],[691,842]],5);
line('badge-clip','#56627A',[[700,785],[717,785]],6);
line('badge-mark','#83A8BC',[[699,815],[722,815]],7);
line('badge-text','#A8B1C1',[[699,829],[715,829]],4);
async function main() {
  if(fs.existsSync(path.join(root,'candidate.png'))&&!process.argv.includes('--force')) throw Error('既存案を保持してください。再出力は --force');
  const source=path.resolve(root,'../../../Assets/Sprites/Hinata/hinata_normal.png'), original=fs.readFileSync(source);
  const {data,info}=await sharp(original).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  if(info.width!==1254||info.height!==1254) throw Error('原画サイズが変わりました。マスクを再確認してください');
  const head=Buffer.from(data);
  for(let y=0;y<1254;y++) for(let x=0;x<1254;x++) if((y>=698&&x>=430&&x<=820)||y>=800) head[(y*1254+x)*4+3]=0;
  const headPNG=await sharp(head,{raw:{width:1254,height:1254,channels:4}}).png().toBuffer(), svg=engine.svg(engine.validate(scene));
  fs.writeFileSync(path.join(root,'scene.json'),JSON.stringify(scene,null,2)+'\n');
  fs.writeFileSync(path.join(root,'outfit.svg'),svg);
  fs.writeFileSync(path.join(root,'source-original.png'),original);
  fs.writeFileSync(path.join(root,'head-preserved.png'),headPNG);
  await sharp(Buffer.from(svg)).composite([{input:headPNG}]).png().toFile(path.join(root,'candidate.png'));
  const {data:after}=await sharp(path.join(root,'candidate.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let unchanged=true,transparent=0;
  for(let y=0;y<698;y++) for(let x=0;x<1254;x++) {const i=(y*1254+x)*4;if(data[i+3]===255&&!data.subarray(i,i+4).equals(after.subarray(i,i+4))) unchanged=false;}
  for(let i=3;i<after.length;i+=4) if(after[i]===0) transparent++;
  if(!unchanged||!transparent) throw Error('顔・髪の保護または透過検査に失敗');
  await sharp(path.join(root,'candidate.png')).resize(220,220).png().toFile(path.join(root,'candidate-220.png'));
  fs.writeFileSync(path.join(root,'manifest.json'),JSON.stringify({headOrigin:'既存の生成由来の暫定顔・髪',outfitOrigin:'コード定義の筆跡',newImageGenerationCalls:0,gameAssetReplaced:false,width:1254,height:1254,opaqueHeadUnchangedAboveY698:unchanged,transparentPixels:transparent},null,2)+'\n');
  console.log(JSON.stringify({output:path.join(root,'candidate.png'),unchangedHead:unchanged,transparentPixels:transparent}));
}
main().catch(e=>{console.error(e);process.exit(1);});
