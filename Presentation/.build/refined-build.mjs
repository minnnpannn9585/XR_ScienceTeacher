import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {createRequire} from 'node:module';
import {FileBlob,PresentationFile} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const dir=path.dirname(fileURLToPath(import.meta.url));
const workspace=path.dirname(dir),out=path.join(dir,'refined');
const runtime='C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies';
const SKILL='C:/Users/minpa/.codex/plugins/cache/openai-primary-runtime/presentations/26.1026.10807/skills/presentations';
process.env.RUNTIME_NODE_MODULES=runtime+'/node/node_modules';
const req=createRequire(import.meta.url),{createCanvas}=req(runtime+'/node/node_modules/@napi-rs/canvas');
const ctx=createCanvas(10,10).getContext('2d');
const C={bg:'#F5F8F8',ink:'#173344',body:'#4A646C',accent:'#008C95',line:'#B5D5D9',pale:'#E4F1F1'};
await fs.mkdir(path.join(out,'renders'),{recursive:true});
const original=path.join(workspace,'output','XR_ScienceTeacher_项目介绍_图文概览版.pptx');
const backup=path.join(out,'source.pptx');
try{await fs.access(backup);}catch{await fs.copyFile(original,backup);}
// Import and edit the original presentation; slides 1–6 retain original objects.
const p=await PresentationFile.importPptx(await FileBlob.load(backup));
if(p.slides.items.length!==8)throw new Error('Expected the existing 8-slide source');
const oldNotes=p.slides.items.map(s=>s.speakerNotes.text);

function text(s,str,x,y,w,h,size,color=C.ink,bold=false){
 const sh=s.shapes.add({geometry:'textbox',name:str.slice(0,55),position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 sh.text=str;sh.text.style={typeface:'Microsoft YaHei',fontSize:size,color,bold,insets:0,wrap:'none',autoFit:'none'};return sh;
}
function para(s,str,x,y,w,size,color=C.body,bold=false,lineHeight=size*1.35){
 ctx.font=`${bold?'bold ':''}${size}px "Microsoft YaHei"`;
 const lines=[];
 for(const par of str.split('\n')){
  let line='';for(const u of par.match(/[A-Za-z0-9][A-Za-z0-9._=/-]*|[^\r\n]/gu)||[]){
   if(line&&ctx.measureText(line+u).width>w-10&&!/[，。；：、！？）】]/.test(u)){lines.push(line.trim());line=u.trimStart();}else line+=u;
  }if(line)lines.push(line.trim());
 }
 lines.forEach((v,j)=>text(s,v,x,y+j*lineHeight,w,lineHeight+3,size,color,bold));return y+lines.length*lineHeight;
}
async function image(s,f,x,y,w,h,alt,fit='cover',crop){
 const b=await fs.readFile(f);return s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:/\.jpe?g$/i.test(f)?'image/jpeg':'image/png',alt,fit,position:{left:x,top:y,width:w,height:h},...(crop?{crop}:{})});
}
function foot(s,i,str){text(s,str,42,695,1050,20,i===6?14:13,C.body);text(s,String(i+1).padStart(2,'0')+' / 08',1160,692,90,24,16,C.body);}
function title(s,a,b){text(s,a,42,28,1060,61,43,C.ink,true);para(s,b,42,96,1190,23,C.body,false,31);}
function cleanNote(v){return String(v).replace(/科技纹理仅作装饰，由内置image_gen生成；提示词和本地资产记录见Presentation\/assets\/tech-background-provenance\.txt。/g,'').trim();}
const captionText=new Set(['光路与成像','微观与扩散','讲解','自由实验','挑战','展开与折叠','三视图对照','动态截面','宏观与微观联动','冷热对比','扩散过程','可操作的光学台','放大实像','虚像探索','把观察变成判断','用星级记录完成情况']);
for(const [i,s]of p.slides.items.entries()){
 s.background.fill=C.bg;
 for(const im of [...s.images.items]){
  const f=im.frame;if(f.width>=1279&&f.height>=719)s.images.deleteById(im.id);
 }
 for(const sh of s.shapes.items){
  const str=sh.text.toString();
  if(!str)continue;
  const pr=sh.toProto(),size=(pr.textStyle?.fontSize??1800)/75;
  const color=captionText.has(str)?C.accent:(size>=28||pr.textStyle?.bold?C.ink:C.body);
  sh.text.color=color;
 }
 s.speakerNotes.text=cleanNote(oldNotes[i]);
}

const cover=p.slides.items[0];
for(const sh of [...cover.shapes.items]){
 const str=sh.text.toString();
 if(str==='中英双语')cover.shapes.deleteById(sh.id);
 if(str==='PC 与 XR')sh.text='PC / AR 双端设计';
 if(str==='XR ScienceTeacher'){
  sh.position={left:114,top:66,width:370,height:60};
  sh.text.style={typeface:'Microsoft YaHei',fontSize:31,color:C.ink,bold:true,insets:0,wrap:'none',autoFit:'none'};
 }
}
cover.speakerNotes.text+='\n封面PC / AR双端设计为产品设计方向，其中现有工程为PC与OpenXR头显交互，真实空间融合见第7页设计示意。';

// Rework slides 7–8 in place; retain the imported slide identities and order.
for(const i of [6,7]){
 const s=p.slides.items[i];s.shapes.deleteAll();
 for(const im of [...s.images.items])s.images.deleteById(im.id);
}
{
 const s=p.slides.items[6];
 title(s,'PC / AR 双端设计','同一套科学内容，连接电脑操作与桌前空间交互。');
 text(s,'PC 桌面端',42,160,318,43,30,C.ink,true);
 await image(s,path.join(dir,'screenshots','02_netfold_cube.png'),42,215,318,179,'PC桌面端：几何实验真实运行截图');
 para(s,'课堂演示与自主练习，直接使用鼠标探索。',42,416,318,23,C.body,false,33);
 text(s,'共享实验内容',42,536,318,38,27,C.accent,true);
 text(s,'几何　分子　光学',42,585,318,36,25,C.ink);
 await image(s,path.join(workspace,'assets','AR课堂_概念效果图.png'),400,154,838,472,'AR课堂概念效果图，AI合成的产品设计示意，非设备实拍');
 text(s,'AR：虚拟实验叠加到真实课桌',400,637,838,40,26,C.accent,true);
 foot(s,6,'AR 设计示意，非设备实拍；现有原型采用 OpenXR，空间融合为拓展方向。');
 s.speakerNotes.text='PC侧来自项目Unity 2022.3.62f3运行画面。AR侧为AI合成的交互设计示意，非项目运行或头显实拍。现有工程包含PC与OpenXR输入、手柄/手部交互和桌边双面板。未发现真实环境透视、平面检测或空间融合实现，不宣称AR实测完成。\nAR概念图参考课堂照片：Nguyen Phan Nam Anh / Unsplash，https://unsplash.com/photos/an-empty-classroom-with-desks-and-windows-fKAUB1uYFcc 。素材及生成记录见Presentation/assets/ar-concept-provenance.json。';
}
{
 const s=p.slides.items[7];
 title(s,'面向课堂，也面向自主发现','让科学概念进入课堂讨论，也进入每个人的自主探索。');
 await image(s,path.join(workspace,'assets','context','students-computer.jpg'),42,155,650,433,'学生共用电脑的课堂场景参考，非项目使用照片');
 text(s,'一次直观观察，打开下一次科学提问。',42,613,650,44,26,C.ink,true);
 const center=s.shapes.add({geometry:'ellipse',name:'共享科学内容',position:{left:728,top:341,width:112,height:112},fill:C.pale,line:{fill:C.accent,width:2}});
 text(s,'共享',759,370,70,31,23,C.ink,true);text(s,'内容',759,402,70,31,23,C.ink,true);
 for(const [j,[label,sub]]of [['课堂演示','共同观察'],['小组探究','比较现象'],['自主练习','反复尝试']].entries()){
  const y=190+j*180;
  const target=text(s,label,900,y,322,42,28,C.ink,true);
  text(s,sub,900,y+49,322,36,23,C.body);
  s.shapes.connect(center,target,{kind:'elbow',fromSide:'right',toSide:'left',line:{style:'solid',fill:C.line,width:2},head:{type:'none'},tail:{type:'none'}});
 }
 foot(s,7,'场景参考，非项目使用照片');
 s.speakerNotes.text='课堂演示、小组探究和自主练习为依据现有讲解、自由实验与挑战能力归纳的目标场景，不代表已开展课堂部署或验证教学成效。原型不含教师后台或云端班级。\n场景照片：Bright Kwame Ayisi，Students using a computer，Wikimedia Commons，CC0，https://commons.wikimedia.org/wiki/File:Students_using_a_computer.jpg 。本页关系图为原生可编辑PPT形状与连接线。';
}
const logo=path.join(workspace,'assets','logos','XR_ScienceTeacher_logo.png');
let hasLogo=true;try{await fs.access(logo);}catch{hasLogo=false;}
const accent=path.join(workspace,'assets','science-accent.png');
for(const[i,s]of p.slides.items.entries()){
 if(i>0)await image(s,accent,1138,18,100,65,'局部科学线条装饰','contain');
 if(hasLogo){
  if(i===0)await image(s,logo,42,55,62,62,'XR ScienceTeacher 标志','contain',{left:.15,right:.15,top:.06,bottom:.06});
  await image(s,logo,1124,688,26,26,'XR ScienceTeacher 页脚标志','contain',{left:.15,right:.15,top:.06,bottom:.06});
 }
}
for(const[i,s]of p.slides.items.entries()){
 await fs.writeFile(path.join(out,'renders',`slide-0${i+1}.png`),new Uint8Array(await(await p.export({slide:s,format:'png',scale:1})).arrayBuffer()));
}
await fs.writeFile(path.join(out,'montage.webp'),new Uint8Array(await(await p.export({format:'webp',montage:true})).arrayBuffer()));
const candidatePath=path.join(out,'candidate.pptx');
await(await PresentationFile.exportPptx(p)).save(candidatePath);
await fs.writeFile(path.join(out,'edit-summary.json'),JSON.stringify({slideCount:p.slides.items.length,importedFrom:backup,firstSixRetained:true,logoIncluded:hasLogo,fullSlideImages:p.slides.items.flatMap((s,i)=>s.images.items.filter(im=>im.frame.width>=1279&&im.frame.height>=719).map(im=>({slide:i+1,id:im.id})))},null,2));
console.log(JSON.stringify({candidatePath,slides:p.slides.items.length,logoIncluded:hasLogo}));
if(process.argv.includes('--final')){
 if(!hasLogo)throw new Error('Final logo is not yet available');
 const{finalizePresentation}=await import(pathToFileURL(SKILL+'/container_tools/artifact_tool_utils.mjs').href);
 const finalPath=path.join(workspace,'output','XR_ScienceTeacher_项目介绍_精修版.pptx');
 const r=await finalizePresentation({workspaceDir:workspace,candidatePath,finalPath,pythonExecutable:runtime+'/python/python.exe',integrityValidatorPath:SKILL+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:SKILL+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],explicitTotalSlideCount:8,requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],fontPolicy:{basis:'design',families:['Microsoft YaHei']},verifyArtifactToolImport:true,receiptPath:path.join(out,'validation.json')});
 console.log(JSON.stringify({finalPath:r.finalPath,slides:r.firstPartyImport.slideCount,layout:r.presentationLayout.findingCount}));
}
