"""モックと実画面を同じ1600×900に揃え、比較用に並べる（ゲーム素材は変更しない）。"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[1]
mock=Image.open(ROOT/'Artifacts/planning-mock-approved.png').convert('RGB')
# IABの全ページ保存は1600×900のCSS面を1280×720で描画し余白を付ける。
# ステージ左端282px=352CSSpx×0.8を確認したため、描画領域だけを正規化する。
mock=mock.crop((0,0,1280,720)).resize((1600,900),Image.Resampling.LANCZOS)
actual=Image.open(ROOT/'Artifacts/CompanyOps/46-approved-planning.png').convert('RGB')
actual=actual.resize((1600,900),Image.Resampling.LANCZOS)
result=Image.new('RGB',(3200,958),'#1d2a44')
font=ImageFont.truetype('C:/Windows/Fonts/meiryo.ttc',24)
draw=ImageDraw.Draw(result)
draw.text((24,12),'承認済みモック',font=font,fill='white')
draw.text((1624,12),'Unity実画面（数値・相談・進捗はゲーム状態）',font=font,fill='white')
result.paste(mock,(0,58));result.paste(actual,(1600,58))
result.save(ROOT/'Artifacts/planning-comparison.png')
mock.save(ROOT/'Artifacts/planning-mock-normalized.png')
print(ROOT/'Artifacts/planning-comparison.png')
