"""ブラウザ確認用。公開範囲はモック1枚と参照画像2枚だけ。ループバック限定。"""
from pathlib import Path
from http.server import BaseHTTPRequestHandler, HTTPServer
ROOT = Path(__file__).resolve().parents[1]
FILES = {
    '/Docs/Mockups/planning-screen.html': ('Docs/Mockups/planning-screen.html', 'text/html; charset=utf-8'),
    '/Assets/Art/Office/office-topdown.png': ('Assets/Art/Office/office-topdown.png', 'image/png'),
    '/Assets/Sprites/Hinata/hinata_normal.png': ('Assets/Sprites/Hinata/hinata_normal.png', 'image/png'),
}
class Preview(BaseHTTPRequestHandler):
    def do_GET(self):
        item = FILES.get(self.path)
        if not item: self.send_error(404); return
        data = (ROOT/item[0]).read_bytes()
        self.send_response(200); self.send_header('Content-Type',item[1]); self.send_header('Content-Length',str(len(data)))
        self.end_headers(); self.wfile.write(data)
HTTPServer(('127.0.0.1',8769),Preview).serve_forever()
