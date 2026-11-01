import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {FileBlob,PresentationFile} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const dir=path.dirname(fileURLToPath(import.meta.url)),workspace=path.dirname(dir),out=path.join(dir,'ar-classroom-v2');
const source=path.join(workspace,'output','XR_ScienceTeacher_Rokid参赛版_六关更新.pptx');
const A=path.join(workspace,'assets','ar-classroom-v2');
await fs.mkdir(path.join(out,'renders'),{recursive:true});
const sourceHash=createHash('sha256').update(await fs.readFile(source)).digest('hex');
await fs.copyFile(source,path.join(out,'source.pptx'));
const p=await PresentationFile.importPptx(await FileBlob.load(source));
if(p.slides.items.length!==9)throw Error('User-edited source should contain 9 slides');
const changes=[];
function txt(s,str,x,y,w,h,size=24,color='#173344',bold=false){const t=s.shapes.add({geometry:'textbox',name:str,position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});t.text=str;t.text.style={typeface:'Microsoft YaHei',fontSize:size,color,bold,insets:0,wrap:'none',autoFit:'none'};return t;}
function findText(s,t){return s.shapes.items.find(x=>x.text.toString()===t);}
function foot(s,str){const t=s.shapes.items.find(x=>x.position.top>=692&&!/^\d{2} \/ \d{2}$/.test(x.text.toString()));if(t){t.text=str;t.position={...t.position,width:1060};t.text.fontSize=13;}}
async function replaceAt(index,x,y,name,alt){
 const s=p.slides.items[index],im=s.images.items.find(i=>Math.abs(i.frame.left-x)<3&&Math.abs(i.frame.top-y)<3);
 if(!im)throw Error(`Missing image ${index} ${x},${y}`);
 const b=await fs.readFile(path.join(A,name)),frame=im.frame;
 im.replace({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt,fit:'cover'});
 im.frame=frame;im.crop={left:0,top:0,right:0,bottom:0};
 changes.push({slide:index+1,file:name,alt});
}
const s=p.slides.items;
// Preserve user edits to text, slide deletion, branding and the video-link field.
// Recompose the cover around a single classroom hero instead of dark thumbnails.
for(const im of [...s[0].images.items]){const r=im.frame;if(r.width>300&&r.left>400)s[0].images.deleteById(im.id);}
for(const t of ['物理：光路与成像','化学：微观与扩散']){const sh=findText(s[0],t);if(sh)s[0].shapes.deleteById(sh.id);}
const hero=await fs.readFile(path.join(A,'hero-ar.png'));
s[0].images.add({blob:hero.buffer.slice(hero.byteOffset,hero.byteOffset+hero.byteLength),contentType:'image/png',alt:'真实教室中的几何、光学与微观虚拟教具，AR合成概念图',fit:'cover',position:{left:500,top:151,width:738,height:415}});
changes.push({slide:1,file:'hero-ar.png',alt:'三学科AR课堂合成主视觉'});
txt(s[0],'真实课堂中的虚拟实验',500,586,738,43,29,'#008C95',true);
txt(s[0],'让实验对象与现实课桌出现在同一视野。',500,636,738,36,24,'#4A646C');
foot(s[0],'Rokid AR 应用参赛方案　画面为 AR 课堂合成示意，非设备实拍');

await replaceAt(1,42,155,'spatial-observation-ar.png','立体与正投影在真实课桌上的AR合成示意');
await replaceAt(1,449,157,'lens-observation-ar.png','光学实验叠加在真实课堂中的AR合成示意');
await replaceAt(1,856,157,'micro-detail-ar.png','微观粒子在真实教室中的AR合成示意');
foot(s[1],'AR 课堂合成示意，非设备实拍；教学效果待试用验证');

await replaceAt(2,42,160,'six-labs-ar.png','六个实验主题在真实课堂中的概念总览');
foot(s[2],'课堂应用概念合成图；六关内容结构依据当前 Unity 原型');

await replaceAt(3,42,209,'netfold-ar.png','正方体六面展开，真实课桌上的透明AR教具合成示意');
await replaceAt(3,656,209,'triangle-ar.png','无黑色背板的三角形内角关系，AR课堂合成示意');
foot(s[3],'AR 课堂合成示意，非设备实拍；数学功能依据现有原型');
await replaceAt(4,42,209,'lens-ar.png','真实课堂中的透镜、蜡烛与光屏合成示意');
await replaceAt(4,656,209,'lever-ar.png','真实课桌上的虚拟杠杆，AR合成示意');
foot(s[4],'AR 课堂合成示意，非设备实拍；光路与杠杆效果为示意');
await replaceAt(5,42,209,'particles-ar.png','真实课桌上的透明烧杯与微观粒子合成示意');
await replaceAt(5,656,209,'indicator-ar.png','真实教室中的紫甘蓝指示剂虚拟实验合成示意');
foot(s[5],'AR 课堂合成示意，非设备实拍；粒子和指示剂颜色为教学示意');

await replaceAt(6,42,161,'lens-task-ar.png','凸透镜操作任务的AR课堂概念效果');
findText(s[6],'现有：讲解与实验相结合').text='讲解与实验的课堂体验';
foot(s[6],'左侧为 AR 课堂合成示意；右侧操作达标任务为下一阶段开发计划');

await replaceAt(7,42,215,'pc-classroom.png','教室中用笔记本备课的场景合成示意');
foot(s[7],'PC 与 AR 画面均为场景合成示意，非实机实拍；Rokid 体验待验证');

const oldSource='Nguyen Phan Nam Anh / Unsplash，https://unsplash.com/photos/an-empty-classroom-with-desks-and-windows-fKAUB1uYFcc，Unsplash License https://unsplash.com/license';
const newSource='Barry Zhou / Unsplash，https://unsplash.com/photos/empty-classroom-khjwIW9HH5s，Unsplash License https://unsplash.com/license';
const pcSource='Mikhail Nilov / Pexels，https://www.pexels.com/photo/an-empty-classroom-6981007/，Pexels License https://www.pexels.com/license/';
for(const[i,sl]of s.entries()){
 const pg=sl.shapes.items.find(x=>/^\d{2} \/ \d{2}$/.test(x.text.toString()));if(pg)pg.text=String(i+1).padStart(2,'0')+' / 09';
 const imgs=changes.filter(c=>c.slide===i+1);
 if(imgs.length){
  const bg=(i>=3&&i<=5)?oldSource:(i===7?pcSource:newSource);
  const historic=sl.speakerNotes.text;
  sl.speakerNotes.text='本页配图为基于真实教室摄影与本项目Unity截图制作的AR/使用场景合成概念图，使用内置imagegen。展示设计愿景与教学主题，不是Rokid设备实拍，不作为功能、性能、课堂使用或教学效果验证证据。光路、粒子、投影及材质仅作概念示意，具体科学显示以实际软件与教学审校为准。\n当前配图：'+imgs.map(c=>c.file+'：'+c.alt).join('；')+'。\n背景摄影来源与许可：'+bg+'。全部引用和实际提示词保存在Presentation/assets/ar-classroom-v2/中的*-prompts.json及sources/sources.json。\n原型功能与原始素材记录（仅说明内容依据，不表示当前合成图为截图）：\n'+historic;
 }
}
for(const[i,sl]of s.entries())await fs.writeFile(path.join(out,'renders',`slide-${String(i+1).padStart(2,'0')}.png`),new Uint8Array(await(await p.export({slide:sl,format:'png',scale:1})).arrayBuffer()));
await fs.writeFile(path.join(out,'montage.webp'),new Uint8Array(await(await p.export({format:'webp',montage:true})).arrayBuffer()));
const candidatePath=path.join(out,'candidate.pptx');await(await PresentationFile.exportPptx(p)).save(candidatePath);
await fs.writeFile(path.join(out,'build-record.json'),JSON.stringify({source,sourceHash,slides:9,changes},null,2));
console.log(JSON.stringify({candidatePath,slides:9,replaced:changes.length}));
