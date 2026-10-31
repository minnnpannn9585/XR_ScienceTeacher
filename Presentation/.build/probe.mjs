import fs from 'node:fs/promises';
import {Presentation, PresentationFile} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const p=Presentation.create({slideSize:{width:1280,height:720}});
const s=p.slides.add(); s.background.fill='#10151B';
const t=s.shapes.add({geometry:'textbox',position:{left:70,top:100,width:1100,height:300},fill:'none',line:{fill:'none',width:0}});
t.text='XR 科学实验室\n几何、分子与光学';t.text.style={typeface:'Microsoft YaHei',fontSize:60,bold:true,color:'#F3EFE4',insets:0};
await fs.writeFile(new URL('./probe.png',import.meta.url),new Uint8Array(await (await p.export({slide:s,format:'png',scale:1})).arrayBuffer()));
await (await PresentationFile.exportPptx(p)).save(new URL('./probe.pptx',import.meta.url).pathname.replace(/^\/([A-Za-z]:)/,'$1'));
console.log('probe ok');

