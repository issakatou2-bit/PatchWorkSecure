# モック（HTML）とUnityの撮影を左右に並べた比較画像を作る。細部の監査に使う。
# 使い方: py -3 Tools/Compare-Mock.py <モックHTML> <Unityの撮影PNG> <出力PNG> [切り出し x,y,w,h（1600x900基準）]
# 例: py -3 Tools/Compare-Mock.py Docs/Mockups/planning-bubbles.html Artifacts/CompanyOps/133-bubbles-appear.png Artifacts/compare-bubbles.png 350,100,900,560
import argparse, hashlib, json, os, subprocess, tempfile
from PIL import Image, ImageDraw, ImageFont

EDGE = r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'

def render(html, out, profile, shot=None, browser=EDGE):
    url = 'file:///' + os.path.abspath(html).replace('\\', '/')
    if shot:
        from urllib.parse import quote
        url += '#shot=' + quote(shot)
    # 開いているEdgeや前回の固定PNGを再利用しない。独立したプロファイルで必ず新規撮影する。
    subprocess.run([browser, '--headless=new', '--no-first-run', '--user-data-dir=' + profile,
                    '--screenshot=' + out, '--window-size=1600,900', '--hide-scrollbars',
                    '--virtual-time-budget=4000', url], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                   timeout=45, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    if not os.path.isfile(out):
        raise RuntimeError('今回のHTMLの撮影PNGが作られていません。以前の画像では代用しません。')

def main():
    parser = argparse.ArgumentParser(description='モックとUnityの変更前後を並べて確認する')
    parser.add_argument('mock_html')
    parser.add_argument('unity_png')
    parser.add_argument('out')
    parser.add_argument('crop', nargs='?')
    parser.add_argument('--mock-image', help='書き出し済みの1600×900モックPNG。指定時はブラウザを起動しない')
    parser.add_argument('--mock-shot', help='HTMLの #shot= に渡す撮影状態。--mock-image と同時には使えない')
    parser.add_argument('--browser-exe', default=EDGE, help='ローカルHTMLの撮影に使うブラウザ実行ファイル。既定はEdge')
    args = parser.parse_args()
    if args.mock_image and args.mock_shot:
        parser.error('--mock-image と --mock-shot は同時に使えません')
    mock_html, unity_png, out = args.mock_html, args.unity_png, args.out
    crop = tuple(int(v) for v in args.crop.split(',')) if args.crop else None
    if crop and len(crop) != 4:
        parser.error('切り出しは x,y,w,h の4個を指定してください')
    scratch = tempfile.TemporaryDirectory(prefix='patchwork-compare-')
    tmp = os.path.join(scratch.name, 'mock.png')
    if args.mock_image:
        mock_path = args.mock_image
        with Image.open(mock_path) as source:
            if source.size != (1600, 900):
                parser.error('書き出し済みのモックPNGは1600×900にしてください')
    else:
        render(mock_html, tmp, os.path.join(scratch.name, 'browser-profile'), args.mock_shot, args.browser_exe)
        mock_path = tmp
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    if not args.mock_image:
        # 比較PNGの左半分だけでなく、今回描画した手本そのものも監査用に残す。
        mock_path = os.path.abspath(out) + '.mock.png'
        with Image.open(tmp) as source:
            source.save(mock_path)
    a = Image.open(mock_path).convert('RGB').resize((1600, 900))
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
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    sheet.save(out)
    # HTMLと描画画像を区別して記録。外部で書き出した画像を再描画済みとは扱わない。
    def digest(path):
        with open(path, 'rb') as source:
            return hashlib.sha256(source.read()).hexdigest()
    with open(out + '.json', 'w', encoding='utf-8') as record:
        json.dump({'mock_html': os.path.abspath(mock_html), 'mock_html_sha256': digest(mock_html),
                   'mock_image': os.path.abspath(mock_path), 'mock_image_sha256': digest(mock_path),
                   'unity_image': os.path.abspath(unity_png), 'unity_image_sha256': digest(unity_png),
                   'rendered_here': not bool(args.mock_image), 'browser': None if args.mock_image else args.browser_exe,
                   'mock_shot': args.mock_shot, 'crop': crop}, record, ensure_ascii=False, indent=2)
    print('saved', out, sheet.size)

if __name__ == '__main__':
    main()
