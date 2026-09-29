# モック（HTML）とUnityの撮影を左右に並べた比較画像を作る。細部の監査に使う。
# 使い方: py -3 Tools/Compare-Mock.py <モックHTML> <Unityの撮影PNG> <出力PNG> [切り出し x,y,w,h（1600x900基準）]
# 例: py -3 Tools/Compare-Mock.py Docs/Mockups/planning-bubbles.html Artifacts/CompanyOps/133-bubbles-appear.png Artifacts/compare-bubbles.png 350,100,900,560
import os, subprocess, sys, tempfile
from PIL import Image, ImageDraw, ImageFont

EDGE = r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'

def render(html, out):
    url = 'file:///' + os.path.abspath(html).replace('\\', '/')
    subprocess.run([EDGE, '--headless=new', '--screenshot=' + out, '--window-size=1600,900', '--hide-scrollbars',
                    '--virtual-time-budget=4000', url], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

def main():
    mock_html, unity_png, out = sys.argv[1:4]
    crop = tuple(int(v) for v in sys.argv[4].split(',')) if len(sys.argv) > 4 else None
    tmp = os.path.join(tempfile.gettempdir(), 'compare-mock.png')
    render(mock_html, tmp)
    a = Image.open(tmp).convert('RGB').resize((1600, 900))
    b = Image.open(unity_png).convert('RGB').resize((1600, 900))
    if crop:
        x, y, w, h = crop
        a, b = a.crop((x, y, x + w, y + h)), b.crop((x, y, x + w, y + h))
    w, h = a.size
    sheet = Image.new('RGB', (w * 2 + 30, h + 50), (255, 255, 255))
    sheet.paste(a, (0, 50)); sheet.paste(b, (w + 30, 50))
    d = ImageDraw.Draw(sheet)
    try:
        f = ImageFont.truetype('meiryo.ttc', 26)
    except OSError:
        f = ImageFont.load_default()
    d.text((10, 10), 'モック（手本）', fill=(217, 74, 112), font=f)
    d.text((w + 40, 10), 'Unity（実装）', fill=(29, 42, 68), font=f)
    sheet.save(out)
    print('saved', out, sheet.size)

if __name__ == '__main__':
    main()
