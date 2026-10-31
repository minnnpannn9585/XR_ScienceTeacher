import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {createRequire} from 'node:module';
import {Presentation,PresentationFile} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
import {pages} from './reading-data.mjs';
const dir=path.dirname(fileURLToPath(import.meta.url));
const build=path.join(dir,'reading');await fs.mkdir(build,{recursive:true});
const runtime='C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies';
process.env.RUNTIME_NODE_MODULES=runtime+'/node/node_modules';
const require=createRequire(import.meta.url);
const {createCanvas}=require(runtime+'/node/node_modules/@napi-rs/canvas');
const ctx=createCanvas(10,10).getContext('2d');
const SKILL='C:/Users/minpa/.codex/plugins/cache/openai-primary-runtime/presentations/26.1026.10807/skills/presentations';
const C={paper:'#F6F4EE',ink:'#172832',body:'#34464F',accent:'#8A6636',dark:'#101B24',white:'#F5F0E6',dim:'#BBC7CD',gold:'#D5B57C'};
const p=Presentation.create({slideSize:{width:1280,height:720}});
const stats=[];
function t(s,str,x,y,w,h,size,color,bold=false){
 const o=s.shapes.add({geometry:'textbox',name:str.slice(0,42),position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 o.text=str;o.text.style={typeface:'Microsoft YaHei',fontSize:size,color,bold,insets:0,wrap:'none',autoFit:'none'};
 return o;
}
function wrap(str,size,w,bold=false){
 ctx.font=`${bold?'bold ':''}${size}px "Microsoft YaHei"`;
 const measure=v=>ctx.measureText(v).width;
 const lines=[];
 for(const para of str.split('\n')){
  const units=para.match(/[A-Za-z0-9][A-Za-z0-9._/−=+]*|[^\r\n]/gu)||[];
  let line='';
  for(const u of units){
   if(line&&measure(line+u)>w-9&&!/[，。；：、！？）】]/.test(u)){lines.push(line.trim());line=u.trimStart();}else line+=u;
  }
  if(line)lines.push(line.trim());
 }
 return lines;
}
function para(s,str,x,y,w,size=19,color=C.body,bold=false,lineHeight=25){
 const lines=wrap(str,size,w,bold);
 lines.forEach((v,i)=>t(s,v,x,y+i*lineHeight,w,lineHeight+2,size,color,bold));
 return y+lines.length*lineHeight;
}
function sections(s,data,x,y,w,dark){
 const font=19,step=25,gap=13;
 for(const [h,b] of data){
  t(s,h,x,y,w,30,23,dark?C.gold:C.accent,true);y+=33;
  y=para(s,b,x,y,w,font,dark?C.dim:C.body,false,step)+gap;
 }
 return y-gap;
}
async function img(s,file,x,y,w,h,crop){
 const data=await fs.readFile(path.join(dir,'screenshots',file));
 s.images.add({blob:data.buffer.slice(data.byteOffset,data.byteOffset+data.byteLength),contentType:'image/png',fit:'contain',alt:file+' Unity PC运行时画面',position:{left:x,top:y,width:w,height:h},...(crop?{crop}:{} )});
}
const R={x:780,w:458};
for(const [i,d] of pages.entries()){
 const s=p.slides.add(),dark=!!d.dark;s.background.fill=dark?C.dark:C.paper;
 t(s,d.title,42,26,1196,55,36,dark?C.white:C.ink,true);
 const leadEnd=para(s,d.lead,42,86,1196,19,dark?C.dim:C.body,false,25);
 const y=153;
 const end=sections(s,d.sections,42,y,696,dark);
 let rightEnd=0;
 const captionColor=dark?C.dim:C.body;
 if(d.images.length===1){
  await img(s,d.images[0][0],R.x,y,R.w,R.w*9/16);
  let z=para(s,d.images[0][1],R.x,y+R.w*9/16+7,R.w,15.5,captionColor,false,20)+9;
  t(s,d.asideTitle,R.x,z,R.w,29,22,dark?C.gold:C.accent,true);z+=33;
  z=para(s,d.aside,R.x,z,R.w,18,dark?C.dim:C.body,false,23);
  if(d.asideBottom)z=para(s,d.asideBottom,R.x,z+12,R.w,16.5,dark?C.dim:C.body,false,21);
  rightEnd=z;
 }else if(d.images.length===4){
  const w=220,h=w*9/16;
  for(let k=0;k<4;k++){
   const x=R.x+(k%2)*238,z=y+Math.floor(k/2)*174;
   await img(s,d.images[k][0],x,z,w,h,{left:0.22,top:0.2,right:0.21,bottom:0.14});
   para(s,d.images[k][1],x,z+h+6,w,15.5,captionColor,false,20);
  }
  let z=y+358;t(s,d.asideTitle,R.x,z,R.w,30,22,C.accent,true);z+=34;
  rightEnd=para(s,d.aside,R.x,z,R.w,18,C.body,false,24);
 }else{
  await img(s,d.images[0][0],R.x,y,R.w,R.w*9/16);
  let z=para(s,d.images[0][1],R.x,y+R.w*9/16+7,R.w,15.5,captionColor,false,20)+17;
  const w=220,h=w*9/16;
  for(let k=1;k<3;k++){
   const x=R.x+(k-1)*238;
   await img(s,d.images[k][0],x,z,w,h);
   rightEnd=Math.max(rightEnd,para(s,d.images[k][1],x,z+h+7,w,15.5,captionColor,false,21));
  }
 }
 t(s,'XR ScienceTeacher　项目阅读版',42,692,500,20,13,dark?C.dim:C.body);
 t(s,'图像：Unity PC 运行时渲染',780,692,360,20,13,dark?C.dim:C.body);
 t(s,String(i+1).padStart(2,'0')+' / 08',1171,690,80,23,15,dark?C.dim:C.body);
 s.speakerNotes.text='项目依据：'+d.source+'\n图像来源：本地 Unity 2022.3.62f3 工程 Play Mode 的 PC 摄像机渲染，1920×1080。截图为实际场景状态，非头显实拍，未使用AI生成画面。纯场景图仅隐藏UI。';
 stats.push({page:i+1,title:d.title,leftEnd:end,rightEnd,leadEnd,images:d.images.length,characters:JSON.stringify(d.sections).replace(/[\[\]",\s]/g,'').length});
 if(Math.max(end,rightEnd)>674)console.warn('OVERFLOW',stats.at(-1));
}
await fs.mkdir(path.join(build,'renders'),{recursive:true});
for(const [i,s]of p.slides.items.entries())await fs.writeFile(path.join(build,'renders',`slide-${String(i+1).padStart(2,'0')}.png`),new Uint8Array(await(await p.export({slide:s,format:'png',scale:1})).arrayBuffer()));
await fs.writeFile(path.join(build,'montage.webp'),new Uint8Array(await(await p.export({format:'webp',montage:true})).arrayBuffer()));
await fs.writeFile(path.join(build,'stats.json'),JSON.stringify(stats,null,2));
const candidatePath=path.join(build,'candidate.pptx');await(await PresentationFile.exportPptx(p)).save(candidatePath);
console.log(JSON.stringify(stats));
if(process.argv.includes('--final')){
 const {finalizePresentation}=await import(pathToFileURL(SKILL+'/container_tools/artifact_tool_utils.mjs').href);
 const finalPath=path.join(path.dirname(dir),'output','XR_ScienceTeacher_项目介绍_阅读版_v2.pptx');
 const result=await finalizePresentation({workspaceDir:path.dirname(dir),candidatePath,finalPath,
  pythonExecutable:runtime+'/python/python.exe',integrityValidatorPath:SKILL+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:SKILL+'/container_tools/inspect_presentation_layout_geometry.py',
  layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],explicitTotalSlideCount:8,
  requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],fontPolicy:{basis:'design',families:['Microsoft YaHei']},verifyArtifactToolImport:true,
  receiptPath:path.join(build,'validation-v2.json')});console.log(JSON.stringify({finalPath:result.finalPath,slideCount:result.firstPartyImport.slideCount,layout:result.presentationLayout.findingCount}));
}

