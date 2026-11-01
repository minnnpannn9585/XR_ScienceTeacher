import fs from 'node:fs/promises';
import {constants} from 'node:fs';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
const dir=path.dirname(fileURLToPath(import.meta.url));
const workspace=path.dirname(dir),out=path.join(dir,'ar-classroom-v2');
const runtime='C:/Users/minpa/.cache/codex-runtimes/codex-primary-runtime/dependencies';
const skill='C:/Users/minpa/.codex/plugins/cache/openai-primary-runtime/presentations/26.1026.10807/skills/presentations';
process.env.RUNTIME_NODE_MODULES=runtime+'/node/node_modules';
const finalPath=path.join(workspace,'output','XR_ScienceTeacher_Rokid参赛版_AR课堂效果.pptx');
const record=JSON.parse(await fs.readFile(path.join(out,'build-record.json'),'utf8'));
// This Windows workspace disallows hard-link publication. Preserve all validator
// checks and no-overwrite behavior, with an exclusive byte-for-byte copy only
// for this finalizer's validated staging file and this exact output path.
const originalLink=fs.link.bind(fs);
let copied=false;
fs.link=async(source,destination)=>{
 try{return await originalLink(source,destination);}
 catch(error){
  const expectedPrefix='.'+path.basename(finalPath)+'.';
  if(error.code!=='EPERM'||destination!==finalPath||path.dirname(source)!==path.dirname(finalPath)||!path.basename(source).startsWith(expectedPrefix)||!source.endsWith('.validated'))throw error;
  await fs.copyFile(source,destination,constants.COPYFILE_EXCL);copied=true;
 }
};
try{
 const{finalizePresentation}=await import(pathToFileURL(skill+'/container_tools/artifact_tool_utils.mjs').href);
 const result=await finalizePresentation({workspaceDir:workspace,candidatePath:path.join(out,'candidate.pptx'),finalPath,pythonExecutable:runtime+'/python/python.exe',integrityValidatorPath:skill+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:skill+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],explicitTotalSlideCount:9,requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],fontPolicy:{basis:'reference',families:['Microsoft YaHei'],referencePath:record.source,referenceSha256:record.sourceHash},verifyArtifactToolImport:true,receiptPath:path.join(out,'validation.json')});
 console.log(JSON.stringify({finalPath:result.finalPath,slides:result.firstPartyImport.slideCount,layout:result.presentationLayout.findingCount,integrity:result.packageIntegrity.exitCode,publication:copied?'exclusive-copy':'hard-link',sha256:result.finalSha256}));
}finally{fs.link=originalLink;}

