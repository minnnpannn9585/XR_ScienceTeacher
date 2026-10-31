import zipfile, xml.etree.ElementTree as ET, re, json
from pathlib import Path
base = Path(__file__).resolve().parent
def inspect(path):
    with zipfile.ZipFile(path) as z:
        names = sorted([n for n in z.namelist() if re.fullmatch(r'ppt/slides/slide\d+.xml', n)], key=lambda n: int(re.search(r'(\d+)\.xml', n).group(1)))
        roots = [ET.fromstring(z.read(n)) for n in names]
    count = sum(len(re.findall(r'[\u4e00-\u9fffA-Za-z0-9]', ''.join(s.itertext()))) for s in roots)
    pics = [len(s.findall('.//{http://schemas.openxmlformats.org/presentationml/2006/main}pic')) for s in roots]
    return {'slides':len(roots),'visible_content_characters':count,'average_characters':round(count/len(roots),1),'images_per_page':pics}
old = inspect(base.parent/'output'/'XR_ScienceTeacher_项目介绍.pptx')
new = inspect(base/'reading'/'candidate.pptx')
result={'old':old,'reading':new,'average_density_ratio':round((new['visible_content_characters']/new['slides'])/(old['visible_content_characters']/old['slides']),2)}
assert new['slides'] == 8
assert min(new['images_per_page']) >= 1
assert result['average_density_ratio'] >= 3
(base/'reading'/'density-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
