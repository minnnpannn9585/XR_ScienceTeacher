import fs from 'node:fs/promises';
import {PresentationFile,FileBlob} from 'file:///C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const p=await PresentationFile.importPptx(await FileBlob.load('Presentation/output/XR_ScienceTeacher_项目介绍_图文概览版.pptx'));
console.log(p.help('*',{search:'remove delete',include:['index','notes'],maxChars:6000}));
const s=p.slides.items[0];
console.log('proto',Object.getOwnPropertyNames(Object.getPrototypeOf(s.shapes)),Object.getOwnPropertyNames(Object.getPrototypeOf(s.images)));
console.log('shapes',JSON.stringify(s.shapes.items.map(a=>({id:a.id,text:a.text.toString(),position:a.position,style:a.text.style,proto:a.toProto?.()})),null,2));
console.log('imgs',JSON.stringify(s.images.items.map(a=>({id:a.id,frame:a.frame,alt:a.alt})),null,2));
