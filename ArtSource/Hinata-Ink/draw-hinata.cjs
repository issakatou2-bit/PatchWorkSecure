// ひなたの新規作画。既存PNGの読込・輪郭抽出・画像生成APIは使わない。
// 各色面と筆圧線を編集して再実行すると、Ink Workshopの制作データを更新する。
const fs = require('node:fs');
const path = require('node:path');
const engine = require('C:/Users/issak/.codex/skills/ink-workshop/assets/engine.js');
const sharp = require('C:/Users/issak/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const C = {ink:'#49353f',hair:'#ed9c98',shade:'#cb747f',light:'#ffd2b6',skin:'#fff0dc',skinShade:'#efc3b1',rose:'#e99e9e',cream:'#fff8e8',teal:'#55857f',tealDark:'#39635f',tealLight:'#82a59a',pants:'#424a5a',pantsDark:'#323c4b',shoe:'#5a4850',gold:'#e2b568'};
const scene={version:1,title:'ひなた / 情シスの頼れるとなりの席',width:960,height:1100,layers:[
  {id:'hair-back',name:'01 後ろ髪・ツインテール'},
  {id:'legs',name:'02 パンツ・靴'},
  {id:'body',name:'03 仕事着・社員証'},
  {id:'face',name:'04 顔・耳'},
  {id:'expression',name:'05 目・眉・口'},
  {id:'fringe',name:'06 前髪・おくれ毛'},
  {id:'hair-ink',name:'07 髪の線・髪留め'},
  {id:'hands',name:'08 手・ノート'},
  {id:'finish',name:'09 小さな仕上げ'}],strokes:[]};
let serial=0;
function points(d){
  const t=d.match(/[MLCQZ]|-?\d*\.?\d+/g);let i=0,x=0,y=0,sx=0,sy=0,out=[];
  const n=()=>Number(t[i++]);
  while(i<t.length){const op=t[i++];
    if(op==='M'||op==='L'){x=n();y=n();out.push([x,y]);if(op==='M'){sx=x;sy=y;}}
    else if(op==='C'){const ax=x,ay=y,bx=n(),by=n(),cx=n(),cy=n(),dx=n(),dy=n();
      const steps=Math.max(8,Math.ceil((Math.hypot(bx-ax,by-ay)+Math.hypot(cx-bx,cy-by)+Math.hypot(dx-cx,dy-cy))/8));
      for(let j=1;j<=steps;j++){const u=j/steps,v=1-u;out.push([v*v*v*ax+3*v*v*u*bx+3*v*u*u*cx+u*u*u*dx,v*v*v*ay+3*v*v*u*by+3*v*u*u*cy+u*u*u*dy]);}x=dx;y=dy;
    }else if(op==='Q'){const ax=x,ay=y,bx=n(),by=n(),cx=n(),cy=n();for(let j=1;j<=16;j++){const u=j/16,v=1-u;out.push([v*v*ax+2*v*u*bx+u*u*cx,v*v*ay+2*v*u*by+u*u*cy]);}x=cx;y=cy;}
    else if(op==='Z'){out.push([sx,sy]);x=sx;y=sy;}
    else throw Error('未対応の作画命令 '+op);
  }return out;
}
function fill(layer,d,color){scene.strokes.push({id:'fill-'+(++serial),layer,kind:'fill',color,duration:.16,points:points(d)});}
function line(layer,d,color=C.ink,size=6){const p=points(d),closed=d.trim().endsWith('Z');scene.strokes.push({id:'line-'+(++serial),layer,kind:'brush',color,size,thinning:.55,smoothing:.55,streamline:0,duration:.55,points:p.map(([x,y],i)=>[x,y,closed?.57+.06*Math.sin(i/p.length*6.28):.16+.5*Math.pow(Math.max(0,Math.sin(Math.PI*(i/(p.length-1)))),.4)])});}
function shape(layer,d,color,width=7){fill(layer,d,color);if(width)line(layer,d,C.ink,width);}
function ellipse(layer,x,y,rx,ry,color,width=0){shape(layer,`M ${x-rx} ${y} C ${x-rx} ${y-ry*.552} ${x-rx*.552} ${y-ry} ${x} ${y-ry} C ${x+rx*.552} ${y-ry} ${x+rx} ${y-ry*.552} ${x+rx} ${y} C ${x+rx} ${y+ry*.552} ${x+rx*.552} ${y+ry} ${x} ${y+ry} C ${x-rx*.552} ${y+ry} ${x-rx} ${y+ry*.552} ${x-rx} ${y} Z`,color,width);}

// 少し左右の動きが異なるツインテール。輪郭のギザギザやノイズで手描き感を偽装しない。
shape('hair-back','M 309 274 C 230 259 183 317 180 409 C 180 474 155 539 160 597 C 162 640 182 684 214 702 C 201 666 210 649 217 630 C 222 671 250 698 282 704 C 263 677 266 648 278 619 C 303 568 337 491 343 412 C 348 350 331 297 309 274 Z',C.hair,8);
fill('hair-back','M 228 334 C 198 419 231 473 196 572 C 183 610 189 651 211 679 C 201 644 220 613 233 582 C 262 511 239 431 270 345 Z',C.shade);
fill('hair-back','M 284 373 C 314 442 283 527 269 558 C 252 600 253 658 281 702 C 249 685 234 654 241 609 C 251 535 282 477 284 373 Z',C.shade);
line('hair-back','M 221 377 C 207 447 227 485 205 548 C 192 588 194 615 203 637',C.ink,3.5);
line('hair-back','M 308 407 C 312 488 270 558 268 602',C.ink,3.6);
shape('hair-back','M 686 278 C 752 258 798 308 802 376 C 805 454 794 492 816 548 C 836 600 827 646 798 674 C 807 644 798 620 791 605 C 785 654 753 690 715 690 C 737 659 731 634 720 608 C 690 547 658 467 659 394 C 659 340 666 303 686 278 Z',C.hair,8);
fill('hair-back','M 754 321 C 786 377 758 443 781 513 C 797 563 819 600 801 646 C 805 609 774 588 763 549 C 744 484 761 403 733 358 Z',C.shade);
fill('hair-back','M 682 375 C 675 469 713 521 736 586 C 750 622 749 660 716 689 C 730 655 713 621 701 590 C 675 535 656 470 662 412 Z',C.shade);
line('hair-back','M 761 397 C 750 471 787 533 793 568',C.ink,3.5);
line('hair-back','M 703 422 C 705 508 748 562 751 611',C.ink,3.5);
shape('hair-back','M 287 410 C 247 281 277 180 382 147 C 472 111 579 130 649 175 C 735 229 756 316 716 455 L 660 585 C 578 650 372 647 307 556 Z',C.shade,8);

// 仕事用の細身パンツとフラットシューズ。
shape('legs','M 410 851 C 466 841 547 843 593 850 L 581 966 C 566 979 535 978 521 969 L 503 911 L 484 974 C 466 987 432 985 417 972 Z',C.pants,7);
fill('legs','M 501 883 L 518 904 L 538 967 L 522 970 L 503 911 L 484 974 L 469 978 L 487 901 Z',C.pantsDark);
line('legs','M 422 948 C 445 953 468 953 486 948',C.ink,3.5);
line('legs','M 527 947 C 544 952 562 952 581 947',C.ink,3.5);
shape('legs','M 417 969 C 439 978 465 978 481 970 L 485 1001 C 474 1017 421 1024 401 1012 C 392 1004 403 983 417 969 Z',C.shoe,7);
shape('legs','M 523 968 C 543 976 564 975 580 966 C 594 981 606 996 597 1006 C 580 1020 536 1012 522 1001 Z',C.shoe,7);
line('legs','M 407 1004 C 430 1012 458 1009 477 1001','#ae918b',4);
line('legs','M 530 997 C 550 1005 576 1008 591 1000','#ae918b',4);

// アイボリーのブラウス＋セージ色のカーディガン。
shape('body','M 450 599 L 448 638 L 414 657 L 420 850 C 462 873 545 874 593 848 L 588 657 L 551 636 L 549 597 Z',C.cream,7);
shape('body','M 451 597 L 549 598 L 550 639 L 502 672 L 449 638 Z',C.skin,5);
fill('body','M 451 603 L 548 602 L 549 619 C 520 644 477 639 451 624 Z',C.skinShade);
shape('body','M 447 628 L 499 664 L 471 690 L 430 650 Z',C.cream,5);
shape('body','M 550 629 L 505 664 L 530 690 L 573 649 Z',C.cream,5);
shape('body','M 431 642 C 400 644 373 668 352 713 L 393 772 L 399 867 C 419 880 441 883 460 877 L 475 685 Z',C.teal,7);
shape('body','M 568 643 C 609 648 623 677 644 718 L 610 773 L 608 868 C 586 880 560 883 543 875 L 530 686 Z',C.teal,7);
fill('body','M 400 715 L 416 748 L 417 861 L 447 872 L 405 868 Z',C.tealDark);
fill('body','M 590 710 L 573 762 L 581 870 L 606 863 L 608 770 Z',C.tealDark);
line('body','M 433 660 C 451 716 455 817 448 862',C.tealLight,5);
line('body','M 564 660 C 550 724 551 811 555 858',C.tealLight,5);
line('body','M 411 813 Q 433 822 451 816',C.ink,4);
line('body','M 559 815 Q 581 823 597 814',C.ink,4);
ellipse('body',459,737,3.5,3.5,C.gold);ellipse('body',455,784,3.5,3.5,C.gold);
line('body','M 478 681 L 501 751 L 526 681',C.pants,7);
shape('body','M 481 747 L 520 747 Q 525 747 525 753 L 525 800 Q 525 806 519 806 L 482 806 Q 476 806 476 800 L 476 754 Q 476 748 481 747 Z',C.gold,4);
fill('body','M 483 755 L 518 755 L 518 798 L 483 798 Z',C.cream);
ellipse('body',500,768,6,6,C.teal);
line('body','M 488 784 L 513 784',C.teal,3.5);line('body','M 491 792 L 507 792',C.teal,3);

// 顔は前の生成画像を使わず、耳・頬・顎の形から新規に構成。
shape('face','M 320 452 C 277 436 269 477 286 508 C 295 524 310 529 324 520 Z',C.skin,6);
line('face','M 303 470 Q 287 471 300 493',C.skinShade,5);
shape('face','M 670 451 C 710 437 722 475 707 505 C 698 523 684 527 669 518 Z',C.skin,6);
line('face','M 690 470 Q 704 468 694 493',C.skinShade,5);
shape('face','M 315 356 C 329 282 418 246 507 256 C 609 248 677 311 679 402 L 672 511 C 664 565 633 600 580 621 C 529 642 462 640 410 619 C 352 596 326 557 316 509 Z',C.skin,8);
fill('face','M 319 394 C 315 461 327 527 350 554 C 374 585 409 599 431 602 C 369 592 330 559 318 511 Z',C.skinShade);
ellipse('expression',364,542,31,14,C.rose);ellipse('expression',626,539,31,14,C.rose);
line('expression','M 342 540 L 347 548',C.shade,2.5);line('expression','M 356 538 L 361 546',C.shade,2.5);line('expression','M 619 536 L 624 544',C.shade,2.5);line('expression','M 633 537 L 638 545',C.shade,2.5);

// 眼球・虹彩・まつ毛を分け、表情差分で顔を丸ごと描き直さずに済むようにする。
shape('expression','M 352 465 C 356 417 404 405 435 430 C 450 444 452 492 440 519 C 425 545 383 546 365 525 C 354 511 350 486 352 465 Z',C.cream,3);
shape('expression','M 549 459 C 554 415 599 405 627 430 C 643 446 644 488 632 515 C 618 543 577 544 560 524 C 548 509 546 482 549 459 Z',C.cream,3);
ellipse('expression',405,481,30,54,'#a76455',4);ellipse('expression',596,479,29,53,'#a76455',4);
ellipse('expression',408,468,18,35,'#63454a');ellipse('expression',599,466,17,34,'#63454a');
fill('expression','M 382 505 Q 407 523 430 502 Q 423 532 404 532 Q 389 530 382 505 Z',C.gold);
fill('expression','M 574 503 Q 599 520 620 500 Q 615 530 596 530 Q 581 527 574 503 Z',C.gold);
ellipse('expression',393,447,12,16,C.cream);ellipse('expression',584,444,12,16,C.cream);
ellipse('expression',421,494,5,6,C.cream);ellipse('expression',612,492,5,6,C.cream);
line('expression','M 344 448 C 365 417 398 408 425 423 C 439 431 446 443 450 456',C.ink,11);
line('expression','M 543 453 C 549 427 571 413 595 415 C 615 415 632 427 643 441',C.ink,11);
line('expression','M 351 442 Q 336 436 331 424',C.ink,6);
line('expression','M 641 438 Q 654 430 657 419',C.ink,6);
line('expression','M 368 532 Q 401 546 434 533',C.ink,3.5);
line('expression','M 562 531 Q 594 544 626 530',C.ink,3.5);
line('expression','M 356 396 Q 391 379 423 395',C.ink,5);
line('expression','M 562 392 Q 592 377 621 391',C.ink,5);
line('expression','M 493 523 Q 485 535 497 535',C.skinShade,4);
shape('expression','M 470 557 Q 494 568 522 553 C 526 576 512 592 497 591 C 481 590 472 578 470 557 Z','#985661',4.5);
fill('expression','M 481 578 Q 497 565 515 577 Q 508 589 498 589 Q 487 588 481 578 Z','#e7a29c');

// 前髪は大きな束を優先し、細い線を入れすぎない。
shape('fringe','M 299 450 C 275 405 272 343 284 286 C 296 222 339 169 405 151 C 496 123 597 149 652 194 C 709 237 733 327 706 413 C 697 447 682 475 666 490 L 654 412 C 625 392 609 365 598 337 C 568 378 532 401 493 410 C 513 388 526 360 528 333 C 493 385 444 414 394 415 C 421 397 441 374 451 351 C 407 389 369 412 326 422 L 315 491 C 307 485 300 470 299 450 Z',C.hair,8);
fill('fringe','M 599 337 C 616 368 635 394 654 411 L 666 490 Q 682 471 690 453 L 686 390 C 652 365 632 335 620 295 Z',C.shade);
fill('fringe','M 306 291 C 297 336 302 383 326 422 L 315 491 Q 301 476 296 438 C 282 384 284 329 306 291 Z',C.shade);
fill('fringe','M 525 303 C 502 354 459 391 395 414 C 428 392 448 367 452 350 C 480 324 504 294 516 261 Z','#e78b8f');
fill('fringe','M 583 302 C 566 346 535 382 494 409 Q 521 379 528 333 L 551 291 Z',C.shade);
// 控えめなハイライトは色面で描き、均一なテカリを避ける。
fill('fringe','M 328 264 C 355 218 398 194 446 184 C 418 198 398 214 376 239 L 366 265 L 353 255 L 343 274 Z',C.light);
fill('fringe','M 479 177 C 522 176 566 187 594 209 L 585 220 C 554 202 527 194 495 195 Z',C.light);
fill('fringe','M 618 220 Q 653 246 665 282 L 647 274 Q 637 248 613 234 Z',C.light);
line('hair-ink','M 461 169 C 403 193 367 253 354 308',C.ink,3.6);
line('hair-ink','M 509 190 C 500 247 477 291 452 321',C.ink,3.8);
line('hair-ink','M 579 220 C 579 258 564 286 545 307',C.ink,3.5);
line('hair-ink','M 624 263 C 637 305 654 334 676 350',C.ink,3.4);
line('hair-ink','M 315 349 Q 313 384 321 405',C.ink,3.3);
// 髪留めは制服リボンではなく、小さな布の結び目。
shape('hair-ink','M 288 301 C 266 285 252 259 260 249 C 274 241 297 257 310 277 C 315 256 335 242 345 251 C 354 265 337 294 316 305 Z',C.pants,5.5);
shape('hair-ink','M 300 285 Q 313 277 321 289 Q 326 304 313 311 Q 300 312 296 300 Z',C.gold,4);
line('hair-ink','M 267 262 L 292 287',C.tealLight,3);
shape('hair-ink','M 692 288 C 681 266 688 242 700 243 C 714 244 721 263 720 279 C 736 264 759 260 762 272 C 765 286 742 304 723 305 Z',C.pants,5.5);
shape('hair-ink','M 705 284 Q 716 275 724 286 Q 730 300 719 308 Q 707 310 701 299 Z',C.gold,4);
line('hair-ink','M 731 287 L 750 275',C.tealLight,3);
// 小さな跳ね毛。
shape('hair-ink','M 454 144 C 452 114 473 94 499 96 C 479 108 475 124 481 140 Z',C.hair,5);

// 片手は挨拶、もう片方は作業ノート。指は重ねず、親指の位置も明示。
shape('hands','M 390 666 C 371 637 363 618 352 600 L 319 621 C 324 670 340 716 365 731 C 378 727 398 706 402 691 Z',C.teal,6.5);
shape('hands','M 322 613 L 353 591 L 369 611 L 335 637 Z',C.cream,4.5);
shape('hands','M 323 614 C 306 605 300 591 298 573 L 294 536 C 293 526 300 522 306 528 L 316 550 L 310 514 C 309 503 319 501 324 510 L 337 546 L 336 509 C 336 498 347 498 350 509 L 356 546 L 363 522 C 366 513 376 517 374 526 L 369 566 C 380 552 389 554 390 562 C 388 576 377 588 365 599 L 349 621 Z',C.skin,6);
line('hands','M 317 558 Q 335 555 348 567 Q 353 575 350 584',C.skinShade,4);
line('hands','M 353 566 L 361 551',C.ink,3);
line('hands','M 333 549 L 335 563',C.ink,2.6);
// ノートは左脇。傾きは色面の座標に直接反映。
shape('hands','M 558 727 L 648 711 Q 659 710 661 723 L 678 852 Q 680 863 668 866 L 577 881 Z',C.pants,6);
shape('hands','M 546 721 L 635 706 Q 645 705 647 717 L 664 847 Q 666 857 655 859 L 566 874 Z',C.cream,5);
fill('hands','M 546 722 L 560 719 L 580 871 L 566 874 Z',C.teal);
line('hands','M 577 750 L 627 742',C.tealLight,4);
line('hands','M 580 764 L 622 757',C.tealLight,4);
line('hands','M 582 778 L 611 773',C.tealLight,4);
shape('hands','M 618 791 C 631 781 650 780 661 787 C 670 793 676 801 671 807 C 667 810 660 805 653 804 C 666 810 672 817 666 823 C 661 827 653 821 646 819 C 658 827 659 833 651 837 C 641 837 625 828 617 820 L 607 811 Z',C.skin,5.5);
line('hands','M 639 801 L 653 808',C.skinShade,3);
line('hands','M 636 814 L 648 821',C.skinShade,3);
// 少量の縫い目と頬の線。背景の飾りは付けず、ゲームに置ける透過素材にする。
line('finish','M 349 676 Q 354 695 367 704',C.tealLight,4);
line('finish','M 604 674 Q 616 693 619 708',C.tealLight,4);

async function main(){
  const out=__dirname;const valid=engine.validate(scene);
  fs.writeFileSync(path.join(out,'scene.json'),JSON.stringify(valid,null,2)+'\n');
  const svg=engine.svg(valid,1,new Set(),false);
  fs.writeFileSync(path.join(out,'hinata.svg'),svg);
  await sharp(Buffer.from(svg)).png().toFile(path.join(out,'hinata.png'));
  await sharp(Buffer.from(svg)).resize({height:220}).png().toFile(path.join(out,'hinata-220px.png'));
  for(const [name,hidden] of [['01-silhouette',['expression','fringe','hair-ink','hands','finish']],['02-face',['hair-ink','hands','finish']]])
    await sharp(Buffer.from(engine.svg(valid,1,new Set(hidden),false))).png().toFile(path.join(out,name+'.png'));
  const {data,info}=await sharp(path.join(out,'hinata.png')).raw().toBuffer({resolveWithObject:true});
  let empty=0,solid=0,edge=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){const a=data[(y*info.width+x)*4+3];if(a===0)empty++;if(a===255)solid++;if((x===0||y===0||x===info.width-1||y===info.height-1)&&a!==0)edge++;}
  const report={width:info.width,height:info.height,channels:info.channels,strokes:valid.strokes.length,layers:valid.layers.length,transparentPixels:empty,opaquePixels:solid,nonTransparentBorderPixels:edge,source:'新規のコード作画。既存ビットマップ不使用。画像生成API不使用。'};
  if(info.channels!==4||empty<info.width*info.height*.25||solid===0||edge!==0)throw Error('透過・余白の検査に失敗');
  fs.writeFileSync(path.join(out,'checks.json'),JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify(report));
}
main().catch(e=>{console.error(e);process.exitCode=1;});
