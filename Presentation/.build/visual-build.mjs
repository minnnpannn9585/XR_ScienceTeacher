import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {createRequire} from 'node:module';
import {Presentation,PresentationFile} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
import {slides} from './visual-data.mjs';
const dir=path.dirname(fileURLToPath(import.meta.url)),out=path.join(dir,'visual');
await fs.mkdir(path.join(out,'renders'),{recursive:true});
const runtime='C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies';
process.env.RUNTIME_NODE_MODULES=runtime+'/node/node_modules';
const req=createRequire(import.meta.url),{createCanvas}=req(runtime+'/node/node_modules/@napi-rs/canvas');
const ctx=createCanvas(10,10).getContext('2d');
const SKILL='C:/Users/minpa/.codex/plugins/cache/openai-primary-runtime/presentations/26.1026.10807/skills/presentations';
const p=Presentation.create({slideSize:{width:1280,height:720}});
const C={bg:'#071321',paper:'#071321',ink:'#ECF7FF',body:'#BDD1E1',white:'#ECF7FF',dim:'#BDD1E1',gold:'#6FE6FF',brown:'#6FE6FF'};
const techBytes=await fs.readFile(path.join(path.dirname(dir),'assets','tech-background.png'));
let extra={};try{extra=JSON.parse(await fs.readFile(path.join(dir,'visual-extra','image-map.json'),'utf8'));}catch{}
const fallback={settings:'01_main_menu.png',challenge_result:'16_challenge.png',particle_hero:'08_drift_lab.png',cone_hero:'07_netfold_cylinder.png',lens_lesson:'12_lens_bench.png'};
const missing=new Set();
function text(s,str,x,y,w,h,size,color,bold=false){
 const sh=s.shapes.add({geometry:'textbox',name:str.slice(0,45),position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 sh.text=str;sh.text.style={typeface:'Microsoft YaHei',fontSize:size,color,bold,insets:0,wrap:'none',autoFit:'none'};return sh;
}
function para(s,str,x,y,w,size,color,bold=false,lineHeight=size*1.35){
 ctx.font=`${bold?'bold ':''}${size}px "Microsoft YaHei"`;
 let lines=[];
 for(const par of str.split('\n')){
  let line='';for(const u of par.match(/[A-Za-z0-9][A-Za-z0-9._=/-]*|[^\r\n]/gu)||[]){
   if(line&&ctx.measureText(line+u).width>w-12&&!/[，。；：、！？）】]/.test(u)){lines.push(line.trim());line=u.trimStart();}else line+=u;
  }if(line)lines.push(line.trim());
 }
 lines.forEach((v,j)=>text(s,v,x,y+j*lineHeight,w,lineHeight+2,size,color,bold));return y+lines.length*lineHeight;
}
async function img(s,key,x,y,w,h,crop,fit='cover'){
 let f;
 if(key.startsWith('extra:')){const k=key.slice(6);if(extra[k])f=path.resolve(dir,'visual-extra',extra[k]);else{missing.add(k);f=path.join(dir,'screenshots',fallback[k]);}}
 else f=path.join(dir,'screenshots',key);
 const b=await fs.readFile(f);s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt:key+' Unity PC运行时画面',fit,position:{left:x,top:y,width:w,height:h},...(crop?{crop}:{})});
}
function title(s,d,dark){text(s,d.title,42,28,1190,61,43,dark?C.white:C.ink,true);if(d.lead)para(s,d.lead,42,96,1190,23,dark?C.dim:C.body,false,31);}
function foot(s,i,dark){text(s,'Unity PC 运行时画面',42,695,600,20,13,dark?C.dim:C.body);text(s,String(i+1).padStart(2,'0')+' / 08',1160,692,90,24,16,dark?C.dim:C.body);}
const geoCrop={left:.22,top:.2,right:.21,bottom:.14};
for(const [i,d]of slides.entries()){
 const s=p.slides.add(),dark=true;s.background.fill=C.bg;
 s.images.add({blob:techBytes.buffer.slice(techBytes.byteOffset,techBytes.byteOffset+techBytes.byteLength),contentType:'image/png',fit:'cover',alt:'科技装饰背景：青蓝电路、网格与微光',position:{left:0,top:0,width:1280,height:720}});
 if(d.kind==='cover'){
  await img(s,d.images[0],500,38,738,404,{left:.16,right:.08,top:.08,bottom:0});
  text(s,d.title,42,66,420,60,38,C.gold,true);
  text(s,d.headline,42,144,420,75,49,C.white,true);
  para(s,d.body,42,254,398,25,C.dim,false,37);
  para(s,d.points.replaceAll('　　','\n'),42,453,398,26,C.white,true,48);
  await img(s,d.images[1],514,462,340,192);
  await img(s,d.images[2],890,462,340,192);
  text(s,'光路与成像',514,662,340,28,20,C.white);text(s,'微观与扩散',890,662,340,28,20,C.white);
 }else if(d.kind==='menu'){
  title(s,d,dark);await img(s,d.image,42,153,800,450);
  d.points.forEach((a,j)=>{const y=153+j*81;text(s,a[0],884,y,350,38,28,C.brown,true);text(s,a[1],884,y+39,350,31,22,C.body);});
  await img(s,d.detail,884,418,350,197);
  text(s,d.bottom,42,634,1140,40,29,C.ink,true);
 }else if(d.kind==='feature'){
  title(s,d,dark);const crop=i===2?geoCrop:undefined;
  await img(s,d.images[0],42,153,760,428,crop);
  await img(s,d.images[1],832,153,406,218,crop);
  await img(s,d.images[2],832,424,406,218,crop);
  text(s,d.captions[0],42,592,760,39,28,C.gold,true);
  text(s,d.captions[1],832,379,406,35,24,C.white,true);
  text(s,d.captions[2],832,650,406,35,24,C.white,true);
  para(s,d.body,42,636,760,23,C.dim,false,30);
  if(d.note)text(s,d.note,610,697,210,20,13,C.dim);
 }else if(d.kind==='challenge'){
  title(s,d,dark);await img(s,d.images[0],42,153,686,386);
  await img(s,d.images[1],758,153,480,386,{left:.28,right:.28,top:.22,bottom:.24},'contain');
  text(s,d.captions[0],42,548,686,36,24,C.gold,true);text(s,d.captions[1],758,548,480,36,24,C.gold,true);
  d.points.forEach((a,j)=>{const x=42+j*405;text(s,a[0],x,606,380,40,30,C.white,true);text(s,a[1],x,651,380,30,21,C.dim);});
 }else if(d.kind==='highlights'){
  title(s,d,dark);
  for(let j=0;j<3;j++){
   const x=42+j*405;
   await img(s,d.images[j],x,161,386,330,j<2?geoCrop:{left:.28,right:.28,top:.25,bottom:.25},j<2?'cover':'contain');
   text(s,d.captions[j],x,501,386,35,23,C.brown,true);
   text(s,d.points[j][0],x,568,386,42,31,C.ink,true);
   para(s,d.points[j][1],x,618,386,24,C.body,false,31);
  }
 }else if(d.kind==='closing'){
  title(s,d,dark);
  for(let j=0;j<2;j++){
   const x=42+j*610;await img(s,d.images[j],x,161,586,330);
   text(s,d.captions[j],x,505,586,45,31,C.brown,true);
  }
  para(s,d.body,42,580,1190,29,C.ink,true,39);text(s,d.note,42,650,1190,31,22,C.body);
 }
 foot(s,i,dark);
 s.speakerNotes.text=d.notes+'\n项目图像来自本地Unity 2022.3.62f3 Play Mode的PC摄像机渲染。纯场景图片仅隐藏UI，部分图片在PPT中裁切以放大对象。项目画面均来自工程，非头显实拍。科技纹理仅作装饰，由内置image_gen生成；提示词和本地资产记录见Presentation/assets/tech-background-provenance.txt。';
}
if(process.argv.includes('--final')&&missing.size)throw new Error('Missing final screenshots: '+[...missing]);
for(const[i,s]of p.slides.items.entries())await fs.writeFile(path.join(out,'renders',`slide-0${i+1}.png`),new Uint8Array(await(await p.export({slide:s,format:'png',scale:1})).arrayBuffer()));
await fs.writeFile(path.join(out,'montage.webp'),new Uint8Array(await(await p.export({format:'webp',montage:true})).arrayBuffer()));
const candidatePath=path.join(out,'candidate.pptx');await(await PresentationFile.exportPptx(p)).save(candidatePath);
console.log(JSON.stringify({slides:p.slides.items.length,missing:[...missing]}));
if(process.argv.includes('--final')){
 const{finalizePresentation}=await import(pathToFileURL(SKILL+'/container_tools/artifact_tool_utils.mjs').href);
 const finalPath=path.join(path.dirname(dir),'output','XR_ScienceTeacher_项目介绍_图文概览版.pptx');
 const r=await finalizePresentation({workspaceDir:path.dirname(dir),candidatePath,finalPath,pythonExecutable:runtime+'/python/python.exe',integrityValidatorPath:SKILL+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:SKILL+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],explicitTotalSlideCount:8,requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],fontPolicy:{basis:'design',families:['Microsoft YaHei']},verifyArtifactToolImport:true,receiptPath:path.join(out,'validation.json')});
 console.log(JSON.stringify({finalPath:r.finalPath,slides:r.firstPartyImport.slideCount,layout:r.presentationLayout.findingCount}));
}
