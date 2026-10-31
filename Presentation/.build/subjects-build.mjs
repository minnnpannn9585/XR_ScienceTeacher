import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {FileBlob,PresentationFile} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const dir=path.dirname(fileURLToPath(import.meta.url)),workspace=path.dirname(dir),out=path.join(dir,'subjects');
const runtime='C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies';
const SKILL='C:/Users/minpa/.codex/plugins/cache/openai-primary-runtime/presentations/26.1026.10807/skills/presentations';
process.env.RUNTIME_NODE_MODULES=runtime+'/node/node_modules';
await fs.mkdir(path.join(out,'renders'),{recursive:true});
const source=path.join(workspace,'output','XR_ScienceTeacher_项目介绍_版式优化版.pptx'),backup=path.join(out,'source.pptx');
try{await fs.access(backup);}catch{await fs.copyFile(source,backup);}
const p=await PresentationFile.importPptx(await FileBlob.load(backup));
if(p.slides.items.length!==8)throw new Error('Expected eight slides');
const C={ink:'#173344',body:'#4A646C',accent:'#008C95',line:'#77B7BB'};
function txt(s,str,x,y,w,h,size=24,color=C.ink,bold=false){
 const t=s.shapes.add({geometry:'textbox',name:str,position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 t.text=str;t.text.style={typeface:'Microsoft YaHei',fontSize:size,color,bold,insets:0,wrap:'none',autoFit:'none'};return t;
}
const edits=[];
function change(s,old,value){
 const found=s.shapes.items.filter(x=>x.text.toString()===old);
 if(found.length!==1)throw new Error('Expected one text: '+old+'; got '+found.length);
 found[0].text=value;edits.push({from:old,to:value});return found[0];
}
const s=p.slides.items;
// Product positioning on the existing cover.
change(s[0],'桌前科学实验室','交互式理科教具');
change(s[0],'展开一个几何体，观察一场扩散，','面向数学、物理、化学，');
change(s[0],'追踪一束光。','把知识点变成可操作的关卡。');
change(s[0],'在可交互的三维空间中，让抽象知','在观察、实验与挑战中，');
change(s[0],'识拥有可见的形态。','让抽象概念更容易理解。');
change(s[0],'3 个实验主题','3 大学科，首批 3 关');
txt(s[0],'持续扩展更多关卡',42,549,398,50,26,C.ink,true);
change(s[0],'光路与成像','物理：光路与成像');
change(s[0],'微观与扩散','化学：微观与扩散');
s[0].speakerNotes.text+='\n产品定位由项目方明确为数学、物理、化学的交互式理科教具。现有三模块为三个学科各一个首批示范关卡，未来继续扩充关卡。数学对应立体几何，物理对应凸透镜成像，化学对应分子运动与扩散。';
// Page 2: subject / current level / extension structure, preserving real menu image.
const overview=s[1];
const page2Footer=overview.shapes.items.filter(sh=>sh.position.top>=690);
for(const sh of [...overview.shapes.items])if(!page2Footer.includes(sh))overview.shapes.deleteById(sh.id);
for(const im of [...overview.images.items]){
 const r=im.frame;
 if(r.left>870&&r.top>400&&r.top<500)overview.images.deleteById(im.id);
 else if(r.left>40&&r.left<44&&r.top>150&&r.top<156)im.position={left:42,top:177,width:540,height:304};
}
txt(overview,'三大学科，持续扩展的关卡',42,28,1060,61,43,C.ink,true);
txt(overview,'以数学、物理、化学为主线，每个关卡聚焦一个知识主题。',42,96,1190,33,23,C.body);
txt(overview,'首批 3 个示范关卡',42,506,540,40,29,C.accent,true);
txt(overview,'从一个知识点出发，',42,556,540,34,24,C.body);
txt(overview,'形成可观察、可操作、可验证的体验。',42,592,540,34,24,C.body);
txt(overview,'学科',640,170,100,30,20,C.body);
txt(overview,'当前关卡',775,170,228,30,20,C.body);
txt(overview,'后续扩展',1060,170,178,30,20,C.body);
const rows=[['数学','立体几何'],['物理','凸透镜成像'],['化学','分子运动与扩散']];
for(const [i,[subject,level]] of rows.entries()){
 const y=230+i*132;
 const a=txt(overview,subject,640,y,92,43,29,C.accent,true);
 const b=txt(overview,level,775,y+1,235,42,26,C.ink,true);
 const c=txt(overview,'更多关卡',1060,y+2,178,40,24,C.body);
 overview.shapes.connect(a,b,{kind:'straight',fromSide:'right',toSide:'left',line:{style:'solid',fill:C.line,width:2},head:{type:'none'},tail:{type:'none'}}).bringToFront();
 overview.shapes.connect(b,c,{kind:'straight',fromSide:'right',toSide:'left',line:{style:'solid',fill:C.line,width:2},head:{type:'none'},tail:{type:'none'}}).bringToFront();
}
txt(overview,'现有内容是起点，学科关卡可持续增加。',640,635,598,35,23,C.accent,true);
overview.speakerNotes.text='产品架构依据项目方定位：以数学、物理、化学为一级结构，各学科通过不同知识主题的关卡持续扩展。当前已实现的三个内容模块作为首批示范关卡：立体几何、凸透镜成像、分子运动与扩散。更多关卡为未来产品扩展方向，本页未声称这些新关卡已实现。关卡为产品内容组织单位，不等同于关卡中的挑战题，也不意味着当前主菜单强制顺序解锁。主菜单画面来自真实Unity PC运行状态。';
// Current subject levels. The chemistry page describes microscopic understanding, not a reaction simulator.
change(s[2],'几何在眼前展开','数学关卡：立体几何');
change(s[2],'旋转、展开、投影与切割，让同一个立体呈现多种观察方式。','在旋转、展开与切割中，理解立体与平面之间的关系。');
s[2].speakerNotes.text+='\n本页为数学学科当前的一个示范关卡。几何对象、展开、投影与截面属于同一关卡的不同观察方式。未来数学学科可继续增加其他知识主题关卡。';
change(s[3],'看见微观世界的运动','化学关卡：分子运动与扩散');
change(s[3],'在宏观现象与微观粒子之间切换，观察温度改变后的运动与扩散。','从颜色的扩散现象出发，建立物质微观运动的直观认识。');
change(s[3],'一次温度调节、一滴品红，让看得见的颜色变化与微观过程联系起来。','比较温度与扩散变化，认识分子在持续运动。');
s[3].speakerNotes.text+='\n本页按产品内容架构归入化学的微观认知示范关卡，分子运动与扩散也是物理、化学共有的基础概念。此关卡表现分子运动和扩散，不宣称已实现化学反应模拟。';
change(s[4],'光路与成像同步呈现','物理关卡：凸透镜成像');
change(s[4],'移动蜡烛和光屏、调节焦距，观察光线如何交会、图像如何变化。','移动蜡烛和光屏，观察光路与成像的同步变化。');
s[4].speakerNotes.text+='\n本页为物理学科当前的一个示范关卡，后续可围绕其他物理知识主题扩充关卡。';
// One reusable level design, with counts explicitly scoped to the current first batch.
change(s[5],'每一次尝试，都有反馈','一个关卡，多种学习方式');
change(s[5],'讲解帮助入门，自由实验鼓励尝试，挑战连接观察与判断。','讲解建立认知，自由实验验证想法，挑战巩固理解。');
change(s[5],'逐步认识三个科学主题','首批 3 个关卡的引导内容');
change(s[5],'9 道挑战','9 道挑战题');
change(s[5],'在任务中应用所学','每关 3 题，练习理解与判断');
s[5].speakerNotes.text+='\n17步讲解和9道挑战题仅统计当前三个示范关卡。每个关卡内有3道挑战题；关卡与挑战题是两级内容结构。';
change(s[6],'同一套科学内容，连接电脑操作与桌前空间交互。','同一套学科关卡，连接电脑操作与桌前空间交互。');
change(s[6],'共享实验内容','共享学科关卡');
change(s[6],'几何　分子　光学','数学　物理　化学');
// Classroom vision with the licensed Chinese photograph and editable application diagram preserved.
change(s[7],'面向课堂，也面向自主发现','走进课堂，扩展更多关卡');
change(s[7],'让科学概念进入课堂讨论，也进入每个人的自主探索。','从首批三关出发，面向数学、物理、化学持续丰富教学内容。');
change(s[7],'一次直观观察，打开下一次科学提问。','让更多理科知识，成为可以动手探索的关卡。').text.fontSize=25;
change(s[7],'共享','学科');change(s[7],'内容','关卡');
s[7].speakerNotes.text+='\n未来关卡扩展为产品愿景，当前覆盖三个示范关卡。学科关卡可以用于课堂演示、小组探究和自主练习。';
// Present the detailed subject pages in the requested mathematics / physics / chemistry order.
const anchors=(await p.inspect({kind:'slide',maxChars:100000})).ndjson.split('\n').filter(Boolean).map(v=>JSON.parse(v)).filter(v=>v.kind==='slide');
const order=[0,1,2,4,3,5,6,7];
p.slides.reorder(order.map(i=>anchors.find(a=>a.slideIndex===i).id));
for(const[i,slide]of p.slides.items.entries()){
 const page=slide.shapes.items.find(sh=>/^\d{2} \/ 08$/.test(sh.text.toString()));
 if(page)page.text=String(i+1).padStart(2,'0')+' / 08';
 await fs.writeFile(path.join(out,'renders',`slide-0${i+1}.png`),new Uint8Array(await(await p.export({slide,format:'png',scale:1})).arrayBuffer()));
}
await fs.writeFile(path.join(out,'montage.webp'),new Uint8Array(await(await p.export({format:'webp',montage:true})).arrayBuffer()));
await fs.writeFile(path.join(out,'edits.json'),JSON.stringify(edits,null,2));
const candidatePath=path.join(out,'candidate.pptx');await(await PresentationFile.exportPptx(p)).save(candidatePath);
console.log(JSON.stringify({slides:p.slides.items.length,edits:edits.length,order}));
if(process.argv.includes('--final')){
 const{finalizePresentation}=await import(pathToFileURL(SKILL+'/container_tools/artifact_tool_utils.mjs').href);
 const finalPath=path.join(workspace,'output','XR_ScienceTeacher_理科教具项目介绍.pptx');
 const r=await finalizePresentation({workspaceDir:workspace,candidatePath,finalPath,pythonExecutable:runtime+'/python/python.exe',integrityValidatorPath:SKILL+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:SKILL+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],explicitTotalSlideCount:8,requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],fontPolicy:{basis:'design',families:['Microsoft YaHei']},verifyArtifactToolImport:true,receiptPath:path.join(out,'validation.json')});
 console.log(JSON.stringify({finalPath:r.finalPath,slides:r.firstPartyImport.slideCount,layout:r.presentationLayout.findingCount}));
}
