import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {createHash} from 'node:crypto';
import {FileBlob,PresentationFile} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const dir=path.dirname(fileURLToPath(import.meta.url)), workspace=path.dirname(dir), repo=path.dirname(workspace);
const out=path.join(dir,'contest-six');
const runtime='C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies';
const skill='C:/Users/minpa/.codex/plugins/cache/openai-primary-runtime/presentations/26.1026.10807/skills/presentations';
process.env.RUNTIME_NODE_MODULES=runtime+'/node/node_modules';
await fs.mkdir(path.join(out,'renders'),{recursive:true});
const source=path.join(workspace,'output','XR_ScienceTeacher_理科教具项目介绍.pptx');
const sourceBytes=await fs.readFile(source), sourceHash=createHash('sha256').update(sourceBytes).digest('hex');
await fs.copyFile(source,path.join(out,'source.pptx'));
const p=await PresentationFile.importPptx(await FileBlob.load(source));
if(p.slides.items.length!==8)throw Error('Expected 8-slide source');
const original=[...p.slides.items];
const ids=(await p.inspect({kind:'slide',maxChars:100000})).ndjson.split('\n').filter(Boolean).map(x=>JSON.parse(x)).filter(x=>x.kind==='slide').map(x=>x.id);
const C={bg:'#F5F8F8',ink:'#173344',body:'#4A646C',accent:'#008C95',line:'#77B7BB',pale:'#E4F1F1'};
const assets=path.join(workspace,'assets'), shots=path.join(dir,'screenshots'), mini=path.join(repo,'Logs','MiniLabs');
const logo=path.join(assets,'logos','XR_ScienceTeacher_logo.png');
const used=[];
function txt(s,str,x,y,w,h,size=24,color=C.ink,bold=false){
 const t=s.shapes.add({geometry:'textbox',name:str,position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 t.text=str;t.text.style={typeface:'Microsoft YaHei',fontSize:size,color,bold,insets:0,wrap:'none',autoFit:'none'};return t;
}
function lines(s,arr,x,y,w,size=23,color=C.body,step=34,bold=false){return arr.map((t,i)=>txt(s,t,x,y+i*step,w,step+4,size,color,bold));}
async function img(s,file,x,y,w,h,alt,fit='cover',crop){
 const b=await fs.readFile(file);used.push({file,alt});
 return s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:/\.jpe?g$/i.test(file)?'image/jpeg':'image/png',alt,fit,position:{left:x,top:y,width:w,height:h},...(crop?{crop}:{})});
}
function clearContent(s){
 for(const sh of [...s.shapes.items])s.shapes.deleteById(sh.id);
 for(const im of [...s.images.items]){
  const r=im.frame;
  if(!(r.left===0&&r.top===0&&r.width===1280&&r.height===720)&&!(r.left===1124&&r.top===688))s.images.deleteById(im.id);
 }
}
function heading(s,a,b){txt(s,a,42,28,1090,61,43,C.ink,true);txt(s,b,42,96,1195,36,23,C.body);}
function foot(s,num,str='Unity PC 运行画面'){txt(s,str,42,695,1060,20,13,C.body);txt(s,String(num).padStart(2,'0')+' / 10',1160,692,90,24,16,C.body);}
function connect(s,a,b){s.shapes.connect(a,b,{kind:'straight',fromSide:'right',toSide:'left',line:{style:'solid',fill:C.line,width:2},head:{type:'none'},tail:{type:'none'}}).bringToFront();}
async function newSlide(){const s=p.slides.add();s.background.fill=C.bg;await img(s,path.join(assets,'block-decoration.png'),0,0,1280,720,'沿用原稿深蓝与青色块面装饰');await img(s,logo,1124,688,26,26,'XR ScienceTeacher 已确认 Logo','cover',{left:0.15,top:0.06,right:0.15,bottom:0.06});return s;}
function update(s,old,value){const el=s.shapes.items.find(x=>x.text.toString()===old);if(!el)throw Error(old);el.text=value;return el;}

// 1. Preserve the approved cover composition and original logo artwork.
const cover=original[0];
update(cover,'面向数学、物理、化学，','让抽象理科知识，');
update(cover,'把知识点变成可操作的关卡。','成为眼前可操作的实验。');
update(cover,'在观察、实验与挑战中，','面向课堂演示与自主探索，');
update(cover,'让抽象概念更容易理解。','在尝试中观察，在反馈中理解。');
update(cover,'3 大学科，首批 3 关','3 大学科，现有 6 关');
update(cover,'PC / AR 双端设计','PC 原型与 AR 体验设计');
update(cover,'持续扩展更多关卡','新增 3 个操作型轻实验');
update(cover,'Unity PC 运行时画面','Rokid AR 应用参赛方案　画面为 Unity PC 原型');
update(cover,'01 / 08','01 / 10');
cover.speakerNotes.text='定位：数学、物理、化学的交互式理科教具。现有6个内容关卡，分为3个主题关卡和3个新增轻实验。新增关卡来自Assets/Scripts/MiniLabs：TriangleAnglesLab、LeverBalanceLab、IndicatorLab；主菜单和场景记录见Logs/MiniLabs/menu.png及results.txt。PC运行证据不代表Rokid实机已验证。图像均来自本项目Unity运行截图。Logo为用户已确认版本，保持原始图像。';

// 2. Teaching needs and interaction value; distinct evidence images, editable labels.
const value=await newSlide();
heading(value,'教学难点与交互价值','每个关卡聚焦一个知识点，让学生通过操作观察关系与变化。');
const valueCols=[
 {x:42,img:'05_netfold_projection.png',title:'空间关系难想象',body:['旋转与投影同步观察，','把立体结构与平面图联系起来。'],take:'数学：看清空间关系'},
 {x:449,img:'15_lens_virtual.png',title:'实验规律难建立',body:['改变条件，立刻比较现象，','把操作、参数与结果联系起来。'],take:'物理：验证条件与结果'},
 {x:856,img:'11_drift_diffusion.png',title:'微观过程难看见',body:['将宏观变化与微观模型并置，','给不可见的过程直观参照。'],take:'化学：连接现象与解释'}
];
for(const c of valueCols){await img(value,path.join(shots,c.img),c.x,157,382,215,c.title+'，项目Unity截图');txt(value,c.title,c.x,393,382,41,29,C.ink,true);lines(value,c.body,c.x,449,382,23,C.body,35);txt(value,c.take,c.x,545,382,37,25,C.accent,true);}
txt(value,'交互价值',42,617,160,37,25,C.accent,true);
txt(value,'亲手改变一个条件，立即看到它带来的变化。',210,617,1028,38,26,C.ink,true);
foot(value,2,'项目设计目标与现有交互示例，教学效果待试用验证');
value.speakerNotes.text='本页为项目设计推理，未引用或宣称学习成绩改善、效率提升等已验证效果。图像来源：Presentation/.build/screenshots/05_netfold_projection.png、15_lens_virtual.png、11_drift_diffusion.png。所有图片为项目真实Unity画面。适用学段与课程知识点仍需教学审校。';

// 3. Six-level content architecture, retaining overview layout.
const map=original[1];clearContent(map);
heading(map,'三大学科，六个可操作关卡','每个学科已有两个主题，用完整关卡与轻实验覆盖不同教学节奏。');
await img(map,path.join(mini,'menu.png'),42,160,565,318,'新版学科菜单：数学、物理、化学，每科两个关卡');
txt(map,'3 个主题关卡 + 3 个轻实验',42,503,565,44,29,C.accent,true);
lines(map,['主题关卡提供讲解、自由实验和挑战。','轻实验围绕一个目标，操作后即时反馈。'],42,560,565,23,C.body,36);
txt(map,'学科',645,165,90,32,20,C.body);
txt(map,'主题关卡',775,165,220,32,20,C.body);
txt(map,'新增轻实验',1030,165,210,32,20,C.body);
const rowData=[['数学','立体几何','三角形内角和'],['物理','凸透镜成像','杠杆平衡'],['化学','分子运动与扩散','酸碱探色']];
rowData.forEach(([a,b,c],i)=>{const y=236+i*132;const left=txt(map,a,645,y,86,42,29,C.accent,true);const middle=txt(map,b,775,y+1,235,40,b.length>6?25:27,C.ink,true);connect(map,left,middle);txt(map,c,1030,y+1,210,42,26,C.ink,true);});
txt(map,'同一学科下，继续增加新的知识主题。',645,635,592,36,24,C.accent,true);
foot(map,3);
map.speakerNotes.text='现有内容结构据Assets/Scripts/UI/MainMenuUI.cs、Assets/Scripts/Core/SceneLoader.cs和Logs/MiniLabs/menu.png核对。6个关卡是内容层级，13个Unity构建场景不是13个关卡。旧3个主题关卡含讲解、自由实验、挑战模式；新增3个轻实验具有操作目标、实时读数、完成反馈、重置与返回。新增轻实验不声称已经具有旧关卡全部模式或计入原9道选择题。';

// 4–6. One subject per page, two substantial screenshots and compact descriptions.
async function subjectPage(s,num,head,sub,left,right,footnote){
 clearContent(s);heading(s,head,sub);
 txt(s,left.title,42,153,582,43,30,C.accent,true);txt(s,right.title,656,153,582,43,30,C.accent,true);
 await img(s,left.file,42,209,582,328,left.alt);
 await img(s,right.file,656,209,582,328,right.alt,'cover',right.crop);
 lines(s,left.body,42,556,582,23,C.body,35);lines(s,right.body,656,556,582,23,C.body,35);
 txt(s,left.goal,42,642,582,36,24,C.ink,true);txt(s,right.goal,656,642,582,36,24,C.ink,true);
 foot(s,num,footnote||'Unity 编辑器运行画面与实验局部；新增轻实验已完成编辑器交互检查');
}
await subjectPage(original[2],4,'数学：空间结构与角度关系','旋转立体、拖动顶点，在形状变化中寻找不变的关系。',
 {title:'立体几何',file:path.join(shots,'04_netfold_unfold.png'),alt:'立体几何关卡，正方体展开为平面图',body:['旋转、展开、投影与切割，','从不同角度理解同一个立体。'],goal:'理解立体与平面的对应关系'},
 {title:'新增：三角形内角和',file:path.join(mini,'triangle.png'),alt:'三角形内角和轻实验真实运行画面局部',crop:{left:0.304,top:0.26,right:0.302,bottom:0.35},body:['拖动顶点或滑块，实时观察三个内角。','分别探索左右位置后获得完成反馈。'],goal:'形状改变，内角和保持 180°'});
original[2].speakerNotes.text='数学主题关卡素材：Presentation/.build/screenshots/04_netfold_unfold.png。新增三角形内角和来自Assets/Scripts/MiniLabs/TriangleAnglesLab.cs与TriangleVertexHandle.cs，截图Logs/MiniLabs/triangle.png。仅为程序约束范围内的非退化平面三角形。完成条件为分别探索顶点左右位置；不是全自由三维三角形编辑器。自动检查包含角和不变量、拖动、完成和重置，见Logs/MiniLabs/results.txt。';

await subjectPage(original[3],5,'物理：光学成像与杠杆平衡','移动实验对象，观察成像与平衡如何随条件改变。',
 {title:'凸透镜成像',file:path.join(shots,'12_lens_bench.png'),alt:'凸透镜自由实验，实时光路与成像参数',body:['移动蜡烛和光屏，比较成像性质。','光路、参数和图像同步呈现。'],goal:'建立物距、像距与成像的联系'},
 {title:'新增：杠杆平衡',file:path.join(mini,'lever.png'),alt:'杠杆平衡轻实验局部，右侧砝码40厘米处达到平衡',crop:{left:0.304,top:0.29,right:0.302,bottom:0.31},body:['调节右侧砝码的位置，让杠杆恢复水平。','实时比较左右力矩，获得平衡解释。'],goal:'较轻的砝码需要更长的力臂'},
 'Unity PC 运行画面；杠杆模型忽略杆重与摩擦，倾斜为示意');
original[3].speakerNotes.text='光学来源：Assets/Scripts/LensCraft和Presentation/.build/screenshots/12_lens_bench.png。杠杆来源：Assets/Scripts/MiniLabs/LeverBalanceLab.cs及Logs/MiniLabs/lever.png。模型固定左侧2kg、距支点20cm，右侧1kg，移动至40cm平衡。采用重力矩计算与示意倾斜，忽略杆重和摩擦。Logs/MiniLabs/results.txt验证力矩、边界、重置与水平状态。此处不宣称真实力反馈或已完成Rokid设备验证。';

await subjectPage(original[4],6,'化学：微观运动与酸碱探色','通过粒子模型和指示剂颜色，建立对物质性质的直观认识。',
 {title:'分子运动与扩散',file:path.join(shots,'09_drift_micro.png'),alt:'分子运动与扩散关卡，宏观微观对照',body:['切换宏观与微观视角，比较温度影响。','观察颜色扩散与粒子运动。'],goal:'连接可见现象与微观解释'},
 {title:'新增：酸碱探色',file:path.join(mini,'indicator.png'),alt:'酸碱探色轻实验局部，紫甘蓝指示剂和三种样品',crop:{left:0.304,top:0.35,right:0.302,bottom:0.25},body:['选择白醋、纯水与肥皂水，比较颜色。','观察三种样品后获得总结反馈。'],goal:'通过颜色区分酸性、中性与碱性'},
 '教学可视化模型；指示剂颜色为示意，实际颜色受浓度与成分影响');
original[4].speakerNotes.text='分子运动与扩散是物理化学共用基础概念，本产品将其归入化学微观认知关卡。原截图Presentation/.build/screenshots/09_drift_micro.png。新增酸碱探色来自Assets/Scripts/MiniLabs/IndicatorLab.cs与Logs/MiniLabs/indicator.png。选择样品会切换独立新样品，不是连续混合或中和模拟；观察三种样品触发完成反馈。使用紫甘蓝指示剂颜色示意，不提供精确pH测量，不宣称精确化学反应仿真。';

// 7. Existing learning structure and clearly labelled planned operation challenge.
const play=original[5];clearContent(play);
heading(play,'学习体验与玩法升级','已有讲解、实验与反馈，下一步强化通过操作达成目标的挑战。');
await img(play,path.join(dir,'visual-extra','22_lens_lesson.png'),42,161,680,383,'现有凸透镜讲解模式真实运行画面');
txt(play,'现有：讲解与实验相结合',42,560,680,41,29,C.accent,true);
lines(play,['主题关卡支持讲解、自由实验和挑战。','新增轻实验已加入操作目标与完成反馈。'],42,612,680,23,C.body,34);
txt(play,'计划新增：光学操作挑战',770,160,468,45,29,C.accent,true);
const stages=[['实验目标','找到清晰的实像'],['玩家操作','移动光屏，观察成像变化'],['达标反馈','判断清晰度，解释成像条件']];
stages.forEach(([a,b],i)=>{const y=237+i*113;txt(play,String(i+1).padStart(2,'0'),770,y,56,48,30,C.accent,true);txt(play,a,842,y,396,35,25,C.ink,true);txt(play,b,842,y+43,396,36,23,C.body);});
txt(play,'支持重试，逐步增加物距调节任务。',770,608,468,70,23,C.ink,true);
foot(play,7,'左侧为现有讲解界面；右侧光学操作达标任务为下一阶段开发计划');
play.speakerNotes.text='已有主题关卡包含讲解、自由实验和选择题挑战，见Assets/Scripts/LensCraft/LensQuizController.cs。图片Presentation/.build/visual-extra/22_lens_lesson.png。当前新增MiniLabs已有操作完成反馈。右侧为明确标注的后续计划：固定蜡烛与焦距，移动光屏满足清晰度条件；后续再加入物距调节。可复用LensMath的ImageResult与Sharpness，但任务状态、锁定逻辑、自动光屏冲突和重试流程仍需开发。该任务不宣称已经完成或已经在Rokid上运行。';

// 8. Preserve the requested AR hero image, distinguish design from tested device output.
const spatial=original[6];
update(spatial,'PC / AR 双端设计','PC / AR 的课堂分工');
update(spatial,'同一套学科关卡，连接电脑操作与桌前空间交互。','PC 支持备课与演示，AR 面向桌前观察与实验操作。');
update(spatial,'课堂演示与自主练习，直接使','备课、投屏演示与自主练习，');
update(spatial,'用鼠标探索。','通过鼠标操作实验。');
update(spatial,'共享学科关卡','AR 体验目标');
update(spatial,'数学　物理　化学','保留真实课堂视野。');
txt(spatial,'近距离观察空间关系。',42,623,318,35,23,C.body);
update(spatial,'AR：虚拟实验叠加到真实课桌','AR 目标：在真实课堂中观察和操作虚拟实验').text.fontSize=25;
update(spatial,'AR 设计示意，非设备实拍；现有原型采用 OpenXR，空间融合为拓展方向。','AR 概念合成图，非设备实拍；Rokid 显示、输入与空间能力待指定设备验证');
update(spatial,'07 / 08','08 / 10');
spatial.speakerNotes.text='AR大图为此前用户要求的概念合成效果，保留其设计表达，不作为Rokid实机运行或桌面识别证据。出处与生成记录Presentation/assets/ar-concept-provenance.json，课堂底图Nguyen Phan Nam Anh / Unsplash：https://unsplash.com/photos/an-empty-classroom-with-desks-and-windows-fKAUB1uYFcc。PC截图来自项目。双端分工为产品定位，不含已实现的教师后台、多端实时控制或云端同步。当前有OpenXR输入适配及Rokid控制器profile；Quest透视实现不等于Rokid适配完成。具体设备显示方式与空间能力需要实测确认。';

// 9. Current completion and evidence plan, without invented metrics.
const ready=await newSlide();
heading(ready,'当前完成度与验证重点','六关内容已具备运行证据，下一步把 Rokid 实机体验与教学试用补齐。');
await img(ready,path.join(dir,'visual-extra','21_challenge_result.png'),42,161,494,325,'现有主题关卡结果反馈局部，展示完成后的知识总结','cover',{left:0.30,top:0.25,right:0.30,bottom:0.25});
txt(ready,'可运行的内容与反馈',42,506,494,41,29,C.accent,true);
lines(ready,['新增三关通过编辑器交互检查，','覆盖完成判定、重置与菜单返回。','现有结果页可总结知识并支持重试。'],42,559,494,23,C.body,35);
const rx=586;
txt(ready,'已具备',rx,160,180,39,29,C.accent,true);
lines(ready,['三大学科菜单与六关 PC 运行画面','新增关卡共用操作、反馈和重置结构','XR 桌前界面与输入接入基础'],rx,211,652,24,C.ink,39);
txt(ready,'Rokid 待验证',rx,351,300,39,29,C.accent,true);
lines(ready,['安装启动、显示和输入方式','一关完整体验、重试及返回','运行稳定性、实测性能与设备录屏'],rx,402,652,24,C.ink,39);
txt(ready,'教学试用计划',rx,542,300,39,29,C.accent,true);
lines(ready,['邀请教师审校，记录小规模试用表现。','关注独立完成、操作卡点与真实反馈。'],rx,593,652,23,C.ink,35);
foot(ready,9,'状态依据当前工程与本地检查记录；尚未形成 Rokid 实机验证及教学效果数据');
ready.speakerNotes.text='来源：Logs/MiniLabs/results.txt明确PASS本地检查，且Hardware headset verification remains manual。该记录覆盖13个构建场景、菜单、三角形角和与拖动、杠杆力矩与边界、指示剂样品/重置、XR Canvas与输入绑定。不能外推为头显实机体验已验证。当前RokidControllerProfile映射有代码基础，QuestPassthroughFeature使用XR_FB_passthrough，仅对应Quest方向。结果页截图Presentation/.build/visual-extra/21_challenge_result.png为现有主题关卡演示例，不是新增三关星级记录。教学试用为计划，可邀请约2位教师和5至8位体验者，采集完成、时间、重试与卡点；没有既有学习增益或用户规模数据。';

// 10. Classroom use and concrete delivery milestones; preserve licensed China image.
const roadmap=original[7];clearContent(roadmap);
heading(roadmap,'课堂应用与下一阶段计划','围绕真实课堂需求，优先完成设备体验、玩法验证和内容扩展。');
await img(roadmap,path.join(assets,'context','china','tianjin-classroom-pexels.jpg'),42,158,580,387,'中国天津课堂真实照片，仅作课堂应用场景参考');
txt(roadmap,'课堂演示　小组探究　自主练习',42,569,580,43,27,C.accent,true);
lines(roadmap,['教师展示现象，学生尝试改变条件。','让讨论建立在同一次可观察的实验上。'],42,622,580,23,C.body,32);
const tasks=[
 ['01','设备体验','完成 Rokid 安装、操作与完整关卡实录。'],
 ['02','玩法升级','加入光学操作挑战，统一提示和重试。'],
 ['03','课堂验证','教师审校与小规模试用，修复操作卡点。'],
 ['04','关卡扩展','复用轻实验结构，继续增加学科主题。']
];
tasks.forEach(([n,t,b],i)=>{const y=159+i*126;txt(roadmap,n,668,y,60,45,31,C.accent,true);txt(roadmap,t,749,y,489,41,29,C.ink,true);txt(roadmap,b,749,y+53,489,66,23,C.body);});
foot(roadmap,10,'中国天津课堂场景参考，非项目使用照片；摄影：TonyNojmanSK / Pexels');
roadmap.speakerNotes.text='路线图为后续开发与验证计划，不表示四项已完成。设备计划需先确认赛道与指定Rokid型号。可验收交付：一关完整实机流程、真实录屏、稳定性记录、操作挑战、小规模试用记录以及扩展候选主题。课堂照片TonyNojmanSK，Smiling Boys and Girls in Classroom，https://www.pexels.com/photo/smiling-boys-and-girls-in-classroom-13320300/，Pexels License https://www.pexels.com/license/，项目既有来源记录Presentation/assets/context/china/sources.json，照片按版式裁切，仅作应用情境参考，不暗示照片人物已试用或认可本产品。赛事背景来源https://spatialjoy.rokid.com/2026；本页没有宣称已获参赛资格或已符合特定赛道。';

const allSlides=(await p.inspect({kind:'slide',maxChars:100000})).ndjson.split('\n').filter(Boolean).map(x=>JSON.parse(x)).filter(x=>x.kind==='slide');
const addedIds=allSlides.filter(x=>!ids.includes(x.id)).map(x=>x.id);
if(addedIds.length!==2)throw Error('Expected two added slides');
p.slides.reorder([ids[0],addedIds[0],ids[1],ids[2],ids[3],ids[4],ids[5],ids[6],addedIds[1],ids[7]]);
for(const[i,s]of p.slides.items.entries()){
 await fs.writeFile(path.join(out,'renders',`slide-${String(i+1).padStart(2,'0')}.png`),new Uint8Array(await(await p.export({slide:s,format:'png',scale:1})).arrayBuffer()));
}
await fs.writeFile(path.join(out,'montage.webp'),new Uint8Array(await(await p.export({format:'webp',montage:true})).arrayBuffer()));
await fs.writeFile(path.join(out,'inspect.ndjson'),(await p.inspect({kind:'slide,textbox,notes',maxChars:300000})).ndjson);
await fs.writeFile(path.join(out,'build-record.json'),JSON.stringify({source,sourceHash,slides:10,used,createdAt:new Date().toISOString()},null,2));
const candidatePath=path.join(out,'candidate.pptx');await(await PresentationFile.exportPptx(p)).save(candidatePath);
console.log(JSON.stringify({slides:p.slides.items.length,candidatePath}));
if(process.argv.includes('--final')){
 const{finalizePresentation}=await import(pathToFileURL(skill+'/container_tools/artifact_tool_utils.mjs').href);
 const finalPath=path.join(workspace,'output','XR_ScienceTeacher_Rokid参赛版_六关更新.pptx');
 const result=await finalizePresentation({workspaceDir:workspace,candidatePath,finalPath,pythonExecutable:runtime+'/python/python.exe',integrityValidatorPath:skill+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:skill+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],explicitTotalSlideCount:10,requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],fontPolicy:{basis:'reference',families:['Microsoft YaHei'],referencePath:source,referenceSha256:sourceHash},verifyArtifactToolImport:true,receiptPath:path.join(out,'validation.json')});
 console.log(JSON.stringify({finalPath:result.finalPath,slides:result.firstPartyImport.slideCount,layout:result.presentationLayout.findingCount}));
}
