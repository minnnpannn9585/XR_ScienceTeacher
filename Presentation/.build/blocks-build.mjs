import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {FileBlob,PresentationFile} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const dir=path.dirname(fileURLToPath(import.meta.url)),workspace=path.dirname(dir),out=path.join(dir,'blocks');
const runtime='C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies';
const SKILL='C:/Users/minpa/.codex/plugins/cache/openai-primary-runtime/presentations/26.1026.10807/skills/presentations';
process.env.RUNTIME_NODE_MODULES=runtime+'/node/node_modules';
await fs.mkdir(path.join(out,'renders'),{recursive:true});
const source=path.join(workspace,'output','XR_ScienceTeacher_项目介绍_精修版.pptx'),backup=path.join(out,'source.pptx');
try{await fs.access(backup);}catch{await fs.copyFile(source,backup);}
const p=await PresentationFile.importPptx(await FileBlob.load(backup));
if(p.slides.items.length!==8)throw new Error('Expected eight slides');
const bg=await fs.readFile(path.join(workspace,'assets','block-decoration.png'));
let removed=0;
for(const [i,s] of p.slides.items.entries()){
 for(const im of [...s.images.items]){
  const r=im.frame;
  if(r.left>1120&&r.top<100&&r.width>80&&r.width<110&&r.height>50&&r.height<80){s.images.deleteById(im.id);removed++;}
 }
 const backdrop=s.images.add({blob:bg.buffer.slice(bg.byteOffset,bg.byteOffset+bg.byteLength),contentType:'image/png',alt:'深蓝与青色的大块面页边装饰',fit:'cover',position:{left:0,top:0,width:1280,height:720}});
 backdrop.sendToBack();
}
if(removed!==7)throw new Error('Unexpected removed accent count: '+removed);
const chosenPath=path.join(workspace,'assets','context','china','selected.json');
let chosen=null;
try{chosen=JSON.parse(await fs.readFile(chosenPath,'utf8'));}catch{}
if(chosen){
 const s=p.slides.items[7];
 const photos=s.images.items.filter(im=>Math.abs(im.frame.left-42)<2&&Math.abs(im.frame.top-155)<2&&Math.abs(im.frame.width-650)<2);
 if(photos.length!==1)throw new Error('Expected one classroom image');
 const target=photos[0],frame=target.frame;
 s.images.deleteById(target.id);
 const f=path.resolve(path.dirname(chosenPath),chosen.file),b=await fs.readFile(f);
 s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:/\.png$/i.test(f)?'image/png':'image/jpeg',alt:chosen.alt,fit:'cover',position:frame,...(chosen.crop?{crop:chosen.crop}:{})});
 const footer=s.shapes.items.find(sh=>sh.text.toString()==='场景参考，非项目使用照片');
 if(footer){footer.text=chosen.creditShort??'中国课堂场景参考，非项目使用照片';footer.text.fontSize=13;}
 s.speakerNotes.text='课堂演示、小组探究和自主练习为依据现有讲解、自由实验与挑战能力归纳的目标场景，不代表已开展课堂部署或验证教学成效。原型不含教师后台或云端班级。\n真实中国课堂照片：'+chosen.title+'。摄影者：'+chosen.author+'。来源：'+chosen.source+'。许可：'+chosen.license+'，'+chosen.licenseUrl+'。本页按版式裁切；人物照片仅作为课堂场景参考，非本项目使用记录。\n本页关系图为原生可编辑PPT形状与连接线。';
}
for(const[i,s]of p.slides.items.entries())await fs.writeFile(path.join(out,'renders',`slide-0${i+1}.png`),new Uint8Array(await(await p.export({slide:s,format:'png',scale:1})).arrayBuffer()));
await fs.writeFile(path.join(out,'montage.webp'),new Uint8Array(await(await p.export({format:'webp',montage:true})).arrayBuffer()));
const candidatePath=path.join(out,'candidate.pptx');await(await PresentationFile.exportPptx(p)).save(candidatePath);
console.log(JSON.stringify({slides:8,removed,photo:chosen?.file??'pending'}));
if(process.argv.includes('--final')){
 if(!chosen)throw new Error('Chinese classroom source is required');
 const{finalizePresentation}=await import(pathToFileURL(SKILL+'/container_tools/artifact_tool_utils.mjs').href);
 const finalPath=path.join(workspace,'output','XR_ScienceTeacher_项目介绍_版式优化版.pptx');
 const r=await finalizePresentation({workspaceDir:workspace,candidatePath,finalPath,pythonExecutable:runtime+'/python/python.exe',integrityValidatorPath:SKILL+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:SKILL+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],explicitTotalSlideCount:8,requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],fontPolicy:{basis:'design',families:['Microsoft YaHei']},verifyArtifactToolImport:true,receiptPath:path.join(out,'validation.json')});
 console.log(JSON.stringify({finalPath:r.finalPath,slides:r.firstPartyImport.slideCount,layout:r.presentationLayout.findingCount}));
}
