import os
os.chdir(r'C:\Projects\PatchWorkSecure\Docs\Mockups')
src = open('planning-screen.html', encoding='utf-8').read()
lines = src.split('\n')
# 上部の帯の直前にある閉じタグがマップの閉じタグ
top = next(i for i, l in enumerate(lines) if 'left: 24px; top: 16px; width: 1552px' in l)
end = max(i for i in range(top) if lines[i].rstrip() == '</div>')
ic = {
    'printer': '<path d="M6 9V3h12v6"/><rect x="3" y="9" width="18" height="8" rx="2"/><path d="M7 14h10v7H7z"/>',
    'pc': '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4M9 10a3 3 0 1 1 3 3"/>',
    'key': '<circle cx="8" cy="12" r="4"/><path d="M12 12h9M18 12v3M21 12v2"/>',
    'wifi': '<path d="M2 9a15 15 0 0 1 20 0M5 12.5a10 10 0 0 1 14 0M8.5 16a5 5 0 0 1 7 0"/><circle cx="12" cy="19.5" r="1"/>',
    'mail': '<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 7l9 6 9-6"/>',
    'star': '<path d="M12 3l2.6 5.6 6 .7-4.5 4.1 1.2 6-5.3-3-5.3 3 1.2-6L3.3 9.3l6-.7z"/>',
}
# 種類, x, y, アイコン, 見出し, 報酬, 出現の遅れ
B = [('n', 95, 110, 'printer', '紙づまり', '信頼 +1', 0.2),
     ('n', 280, 185, 'pc', 'PCが重い', '疲労 -1', 0.9),
     ('n', 300, 330, 'key', 'パスワード', '相談文化 +1', 1.6),
     ('c', 600, 235, 'mail', 'このメール…？', '手がかり', 2.3),
     ('n', 505, 300, 'wifi', '来客Wi-Fi', '信頼 +1', 3.0),
     ('r', 760, 420, 'star', '？？？', 'お礼カード', 4.2)]
col = {'n': '#3fa9f5', 'c': '#ff6f91', 'r': '#e0a100'}
html = ['''  <!-- 困りごとの泡（案）。押すと弾けて小さな報酬。ピンクは相談の泡（兆候＝事件の手がかり）、金はレア -->
  <div class="r" style="position: absolute; left: 16px; top: 16px; z-index: 6; background: rgba(255,255,255,.94); border-radius: 16px; padding: 6px 14px 6px 10px; font-size: 16px; display: flex; align-items: center; gap: 8px; box-shadow: 0 4px 0 rgba(27,35,64,.15)"><span style="width: 22px; height: 22px; border-radius: 11px; background: radial-gradient(circle at 35% 30%, #ffffff, #bfe4ff 60%, #3fa9f5); display: inline-block"></span>困りごと <span id="bubbleDone" style="color: #ff6f91">0</span> / 6</div>''']
for k, x, y, icon, label, rew, d in B:
    html.append(f'''  <button class="bubble b-{k}" data-kind="{k}" data-reward="{rew}" style="left: {x}px; top: {y}px; animation-delay: {d}s, {d + 0.6:.1f}s" aria-label="{label}">
    <svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="{col[k]}" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round">{ic[icon]}</svg>
    <span class="tag">{label}</span>
  </button>''')
html.append('''  <div id="clue" style="position: absolute; left: 186px; top: 10px; width: 420px; z-index: 6; border-radius: 18px; background: #ffffff; padding: 12px 16px 12px 62px; box-sizing: border-box; box-shadow: 0 0 0 3px #ffd3de, 0 10px 26px rgba(27,35,64,.3); transform: translateY(-30px); opacity: 0; transition: all .35s cubic-bezier(.2,1.6,.4,1)">
    <span style="position: absolute; left: 14px; top: 14px; width: 36px; height: 36px; border-radius: 12px; background: #ff6f91; display: flex; align-items: center; justify-content: center"><svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#ffffff" stroke-width="2.6" stroke-linecap="round"><circle cx="11" cy="11" r="6"/><path d="M20 20l-4.5-4.5"/></svg></span>
    <div class="r" style="font-size: 13px; color: #ff6f91">今月の手がかり（事件のときに効く）</div>
    <div style="font-size: 16px; font-weight: 700; margin-top: 2px">営業部に、社長を名乗る<br>支払い依頼のメールが届いていた</div>
  </div>''')
lines[end:end] = html
out = '\n'.join(lines)
css = '''
/* 困りごとの泡（色は変数で持つ。インラインに色を書くと質感レイヤーの規則に拾われる） */
.b-n{--c:#3fa9f5}.b-c{--c:#ff6f91}.b-r{--c:#e0a100}
@keyframes bIn{0%{transform:scale(0);opacity:0}70%{transform:scale(1.12);opacity:1}100%{transform:scale(1)}}
@keyframes bFloat{0%,100%{translate:0 0}33%{translate:3px -7px}66%{translate:-3px -3px}}
@keyframes bPop{0%{transform:scale(1)}30%{transform:scale(.82)}60%{transform:scale(1.35);opacity:.9}100%{transform:scale(1.6);opacity:0}}
@keyframes drop{0%{transform:translate(0,0) scale(1);opacity:1}100%{transform:translate(var(--dx),var(--dy)) scale(.3);opacity:0}}
@keyframes rise{0%{transform:translate(-50%,0) scale(.6);opacity:0}20%{transform:translate(-50%,-14px) scale(1.15);opacity:1}100%{transform:translate(-50%,-70px) scale(1);opacity:0}}
@keyframes ring{to{transform:rotate(360deg)}}
.bubble{position:absolute;z-index:5;width:70px;height:70px;border-radius:50%;padding:0;cursor:pointer;display:flex;align-items:center;justify-content:center;
 background:radial-gradient(circle at 32% 28%,rgba(255,255,255,.98) 0 14%,rgba(255,255,255,.75) 30%,rgba(210,236,255,.55) 70%,rgba(255,255,255,.9) 100%);
 border:3px solid rgba(255,255,255,.95);box-shadow:0 0 0 2px var(--c),0 8px 18px rgba(27,35,64,.28),inset 0 -6px 12px rgba(63,169,245,.18);
 animation:bIn .5s cubic-bezier(.2,1.5,.4,1) both,bFloat 3.2s ease-in-out infinite}
.bubble::before{display:none}
.bubble:hover{filter:brightness(1.06)}
.bubble .tag{position:absolute;top:74px;left:50%;transform:translateX(-50%);white-space:nowrap;font-family:'M PLUS Rounded 1c',sans-serif;font-weight:800;font-size:12px;color:#ffffff;background:var(--c);border-radius:8px;padding:1px 8px;box-shadow:0 2px 0 rgba(0,0,0,.15)}
.b-c{background:radial-gradient(circle at 32% 28%,#ffffff 0 14%,rgba(255,255,255,.8) 30%,rgba(255,214,226,.7) 70%,#ffffff 100%);box-shadow:0 0 0 2px var(--c),0 0 0 7px rgba(255,111,145,.25),0 8px 18px rgba(27,35,64,.28)}
.b-r{background:radial-gradient(circle at 32% 28%,#ffffff 0 14%,#fff4c4 35%,#ffd65a 80%,#fff2b0 100%);box-shadow:0 0 0 2px var(--c),0 0 18px 4px rgba(255,200,40,.7),0 8px 18px rgba(27,35,64,.28)}
.b-r::after{content:'';position:absolute;inset:-9px;border-radius:50%;border:3px dotted #ffc02e;animation:ring 6s linear infinite}
.bubble.popped{animation:bPop .32s ease-out forwards;pointer-events:none}
.fx-drop{position:absolute;z-index:7;width:10px;height:10px;border-radius:50%;background:var(--c);pointer-events:none;animation:drop .5s ease-out forwards}
.fx-num{position:absolute;z-index:8;font-family:'M PLUS Rounded 1c',sans-serif;font-weight:800;font-size:22px;color:#ffffff;white-space:nowrap;pointer-events:none;text-shadow:2px 2px 0 var(--c),-2px -2px 0 var(--c),2px -2px 0 var(--c),-2px 2px 0 var(--c);animation:rise 1s ease-out forwards}
'''
out = out.replace('</style>', css + '</style>', 1)
js = '''<script>
// 泡を押すと弾ける。連続で弾くと音程が上がる（3個目以降）。ピンクは手がかり、金はレア
(function () {
  var ctx = null, done = 0, streak = 0, last = 0;
  function tone(f, t, type, vol) {
    try {
      ctx = ctx || new (window.AudioContext || window.webkitAudioContext)();
      var o = ctx.createOscillator(), g = ctx.createGain(), s = ctx.currentTime + t;
      o.type = type || 'sine';
      o.frequency.setValueAtTime(f, s); o.frequency.exponentialRampToValueAtTime(f * 1.9, s + .07);
      g.gain.setValueAtTime(vol || .18, s); g.gain.exponentialRampToValueAtTime(.001, s + .16);
      o.connect(g); g.connect(ctx.destination); o.start(s); o.stop(s + .18);
    } catch (e) {}
  }
  document.querySelectorAll('.bubble').forEach(function (b) {
    b.addEventListener('click', function () {
      var now = Date.now(); streak = (now - last < 2500) ? streak + 1 : 0; last = now;
      var base = 520 * Math.pow(1.122, Math.max(0, streak - 1)), kind = b.dataset.kind;
      tone(base, 0, 'sine');
      if (kind === 'r') { tone(base * 1.5, .09, 'triangle', .14); tone(base * 2, .18, 'triangle', .12); }
      var map = b.parentElement, cx = b.offsetLeft + 35, cy = b.offsetTop + 35, c = getComputedStyle(b).getPropertyValue('--c');
      var count = kind === 'r' ? 14 : 8;
      for (var i = 0; i < count; i++) {
        var d = document.createElement('span'), a = i / count * Math.PI * 2;
        d.className = 'fx-drop';
        d.style.cssText = 'left:' + (cx - 5) + 'px;top:' + (cy - 5) + 'px;--c:' + c + ';--dx:' + Math.cos(a) * 54 + 'px;--dy:' + Math.sin(a) * 54 + 'px';
        map.appendChild(d); setTimeout(d.remove.bind(d), 600);
      }
      var n = document.createElement('span');
      n.className = 'fx-num'; n.textContent = b.dataset.reward;
      n.style.cssText = 'left:' + cx + 'px;top:' + (cy - 40) + 'px;--c:' + c;
      map.appendChild(n); setTimeout(n.remove.bind(n), 1100);
      b.classList.add('popped');
      document.getElementById('bubbleDone').textContent = ++done;
      if (kind === 'c') {
        var cl = document.getElementById('clue');
        setTimeout(function () { cl.style.opacity = 1; cl.style.transform = 'translateY(0)'; }, 250);
      }
    });
  });
  // 確認用：#demo を付けて開くと、紙づまりと相談の泡を自動で弾く
  if (location.hash === '#demo') {
    var bs = document.querySelectorAll('.bubble');
    setTimeout(function () { bs[0].click(); }, 5000);
    setTimeout(function () { bs[3].click(); }, 5300);
  }
})();
</script>
'''
out = out.replace('</body>', js + '</body>', 1)
out = out.replace('<title>', '<title>計画画面（泡）', 1).replace('<title>計画画面（泡）計画画面', '<title>計画画面（泡）', 1)
open('planning-bubbles.html', 'w', encoding='utf-8').write(out)
print('ok')
