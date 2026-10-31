import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {Presentation,PresentationFile} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
import {slides} from './deck-data.mjs';
const dir=path.dirname(fileURLToPath(import.meta.url));
const workspaceDir=path.dirname(dir);
const SKILL_DIR='C:/Users/minpa/.codex/plugins/cache/openai-primary-runtime/presentations/26.1026.10807/skills/presentations';
const PY='C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe';
process.env.RUNTIME_NODE_MODULES='C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules';
const C={bg:'#10151B',paper:'#F3F0E8',ink:'#14202B',dim:'#A9B2BA',muted:'#56616A',white:'#F3EFE5',gold:'#C4A76D',blue:'#9ABCD6'};
const p=Presentation.create({slideSize:{width:1280,height:720}});
let images={};try{images=JSON.parse(await fs.readFile(path.join(dir,'image-map.json'),'utf8'));}catch{}
function text(s,str,x,y,w,h,size=24,color=C.ink,bold=false,align='left'){
 const t=s.shapes.add({geometry:'textbox',name:str.slice(0,35),position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 t.text=str;t.text.style={typeface:'Microsoft YaHei',fontSize:size,color,bold,alignment:align,verticalAlignment:'top',insets:0,autoFit:'none',wrap:'square'};return t;
}
async function image(s,key,x,y,w,h,fit='contain'){
 const file=images[key];if(!file){text(s,'截图待接入：'+key,x,y,w,h,28,C.dim);return;}
 const b=await fs.readFile(path.isAbsolute(file)?file:path.join(dir,file));
 s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt:key+' Unity真实运行时截图',fit,position:{left:x,top:y,width:w,height:h}});
}
function base(d,i,dark=false){const s=p.slides.add();s.background.fill=dark?C.bg:C.paper;
 text(s,d.title,64,42,1152,80,42,dark?C.white:C.ink,true);
 text(s,String(i+1).padStart(2,'0'),1160,675,56,24,16,dark?C.dim:C.muted,false,'right');
 s.speakerNotes.text=d.notes||'';return s;}
function bodyItems(s,items,x,y,w,size=24,color=C.ink,gap=82){items.forEach((a,j)=>text(s,a.replace(/。$/,''),x,y+j*gap,w,gap-12,size,color));}
function cols(s,d){const n=d.columns.length;const gap=44;const w=(1152-gap*(n-1))/n;
 d.columns.forEach((c,j)=>{const x=64+j*(w+gap);text(s,c.head,x,170,w,70,28,C.ink,true);bodyItems(s,c.items,x,255,w,23,C.muted,n===2?79:87);});
 if(d.bottom)text(s,d.bottom,64,613,1120,42,25,C.ink,true);
 if(d.note)text(s,d.note,64,633,1080,34,18,C.muted);
}
for(const [i,d]of slides.entries()){
 const dark=['cover','fullImage','imageText','dualImage','closing'].includes(d.type);const s=base(d,i,dark);
 if(d.type==='cover'){
  // Remove the standard title by covering the whole slide with the image first in a separate background layer.
  s.shapes.items[0].text='';
  await image(s,d.image,530,0,750,720,'cover');
  // A flat solid text field preserves readable editable type beside the photograph.
  s.shapes.add({geometry:'rect',position:{left:0,top:0,width:575,height:720},fill:C.bg,line:{fill:'none',width:0}});
  text(s,'XR\nScienceTeacher',62,151,520,185,61,C.white,true);
  text(s,d.subtitle,64,372,490,62,38,C.white,true);
  text(s,d.tag,64,517,460,40,22,C.gold);
  text(s,'Unity 项目介绍',64,557,460,32,20,C.dim);
 }else if(d.type==='intro'){
  text(s,d.lead,64,165,1100,144,46,C.ink,true);
  text(s,d.body,64,346,1080,94,27,C.muted);
  d.stats.forEach((a,j)=>{text(s,a[0],64+j*380,494,120,90,72,C.ink,true);text(s,a[1],190+j*380,533,230,45,28,C.muted);});
 }else if(d.type==='rows'){
  d.rows.forEach((r,j)=>{const y=173+j*153;text(s,String(j+1).padStart(2,'0'),64,y,70,60,38,C.muted);text(s,r[0],155,y,990,44,30,C.ink,true);text(s,r[1],155,y+52,1010,42,24,C.muted);text(s,r[2],155,y+96,1010,42,24,C.ink);});
 }else if(d.type==='columns'){cols(s,d);
 }else if(d.type==='journey'){
  text(s,d.lead,64,163,1120,102,31,C.ink,true);
  d.steps.forEach((a,j)=>{let x=64+j*295;text(s,'0'+(j+1),x,316,240,68,52,C.muted);text(s,a[0],x,407,250,48,30,C.ink,true);text(s,a[1],x,479,248,110,24,C.muted);});
  text(s,d.note,64,635,1080,34,18,C.muted);
 }else if(d.type==='fullImage'){
  await image(s,d.image,72,141,1136,496);text(s,d.caption,72,642,1080,34,22,C.dim);
 }else if(d.type==='imageText'){
  await image(s,d.image,38,174,815,459);
  text(s,d.head,881,171,330,105,28,C.white,true);
  bodyItems(s,d.items,881,307,330,23,C.dim,d.items.length>3?72:91);
  text(s,d.note,52,639,800,36,18,C.dim);
 }else if(d.type==='dualImage'){
  await image(s,d.images[0],52,189,576,324);await image(s,d.images[1],652,189,576,324);
  d.captions.forEach((a,j)=>text(s,a,58+j*600,552,560,88,26,C.white));
 }else if(d.type==='lensRules'){
  text(s,d.formula,64,186,520,95,52,C.ink,true);
  text(s,'理想薄透镜成像\n依据物距判断像的性质',64,309,480,114,30,C.muted);
  d.rules.forEach((r,j)=>{text(s,r[0],603,177+j*79,205,54,27,C.ink,true);text(s,r[1],821,177+j*79,395,65,24,C.muted);});
  text(s,d.note,64,632,1115,40,19,C.muted);
 }else if(d.type==='architecture'){
  d.rows.forEach((r,j)=>{let y=166+j*113;text(s,r[0],64,y,175,50,30,C.ink,true);text(s,r[1],261,y,951,50,26,C.ink,true);text(s,r[2],261,y+50,951,43,23,C.muted);});
 }else if(d.type==='highlights'){
  d.items.forEach((a,j)=>{const x=64+(j%2)*603,y=174+Math.floor(j/2)*230;text(s,'0'+(j+1),x,y,65,55,34,C.muted);text(s,a[0],x+82,y,455,62,29,C.ink,true);text(s,a[1],x+82,y+77,455,94,24,C.muted);text(s,a[2],x+82,y+169,455,53,19,C.muted);});
 }else if(d.type==='closing'){
  s.shapes.items[0].text='';text(s,d.title,64,120,1140,150,52,C.white,true);
  text(s,d.lead,64,321,1120,57,30,C.gold);
  text(s,d.body,64,419,1095,100,28,C.dim);
  text(s,d.demo,64,603,1100,64,20,C.dim);
 }
 if(['fullImage','imageText','dualImage'].includes(d.type))text(s,'Unity PC 运行时渲染',64,678,420,24,15,C.dim);
 if(d.image||d.images)s.speakerNotes.text=(d.notes||'')+'\n图像来源：本地工程 Unity 2022.3.62f3 Play Mode 的 PC 摄像机渲染，1920×1080。采集时将界面临时转换为摄像机画布。封面为隐藏UI的纯场景取景。非头显实拍，未使用AI生成场景。';
}
await fs.mkdir(path.join(dir,'renders'),{recursive:true});
for(const [i,s]of p.slides.items.entries()){
 const out=path.join(dir,'renders',`slide-${String(i+1).padStart(2,'0')}.png`);
 await fs.writeFile(out,new Uint8Array(await (await p.export({slide:s,format:'png',scale:1})).arrayBuffer()));
}
await fs.writeFile(path.join(dir,'montage.webp'),new Uint8Array(await(await p.export({format:'webp',montage:true})).arrayBuffer()));
const candidatePath=path.join(dir,'candidate.pptx');
await(await PresentationFile.exportPptx(p)).save(candidatePath);
await fs.writeFile(path.join(dir,'deck.json'),JSON.stringify(p.toProto()));
console.log(JSON.stringify({slides:p.slides.items.length,candidatePath,images:Object.keys(images)}));
if(process.argv.includes('--final')){
 const {finalizePresentation}=await import(pathToFileURL(path.join(SKILL_DIR,'container_tools/artifact_tool_utils.mjs')).href);
 const finalPath=path.join(workspaceDir,'output','XR_ScienceTeacher_项目介绍.pptx');
 const result=await finalizePresentation({workspaceDir,candidatePath,finalPath,pythonExecutable:PY,
  integrityValidatorPath:path.join(SKILL_DIR,'container_tools/inspect_presentation_package_integrity.py'),
  layoutValidatorPath:path.join(SKILL_DIR,'container_tools/inspect_presentation_layout_geometry.py'),
  layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-bullet-geometry','--validate-heading-fit'],
  requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],fontPolicy:{basis:'design',families:['Microsoft YaHei']},
  verifyArtifactToolImport:true,receiptPath:path.join(dir,'validation-final.json')});
 console.log(JSON.stringify(result));
}


