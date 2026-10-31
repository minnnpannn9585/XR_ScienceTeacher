import fs from 'node:fs/promises';
import {Presentation,PresentationFile,FileBlob} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const p=await PresentationFile.importPptx(await FileBlob.load('Presentation/output/XR_ScienceTeacher_项目介绍_图文概览版.pptx'));
await fs.mkdir('Presentation/.build/refined',{recursive:true});
await fs.writeFile('Presentation/.build/refined/source-inspect.ndjson',(await p.inspect({kind:'slide,layout,textbox,shape,image',maxChars:1000000})).ndjson);
console.log(JSON.stringify({slides:p.slides.items.map((s,i)=>({i,id:s.id,images:s.images.items.map(im=>({id:im.id,alt:im.alt,position:im.position})),shapes:s.shapes.items.map(sh=>({id:sh.id,name:sh.name,text:sh.text.toString(),position:sh.position}))})),imageMethods:Object.getOwnPropertyNames(Object.getPrototypeOf(p.slides.items[0].images)),shapeMethods:Object.getOwnPropertyNames(Object.getPrototypeOf(p.slides.items[0].shapes))}));
