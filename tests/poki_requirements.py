from pathlib import Path
import re
import sys

root = Path(sys.argv[1] if len(sys.argv) > 1 else "web/poki")
html = (root / "index.html").read_text(encoding="utf-8")
css = (root / "style.css").read_text(encoding="utf-8")
js = (root / "game.js").read_text(encoding="utf-8")
errors = []

def require(condition, message):
    if not condition:
        errors.append(message)

require((root / "index.html").exists(), "index.html missing")
require((root / "style.css").exists(), "style.css missing")
require((root / "game.js").exists(), "game.js missing")
require('width="1280" height="720"' in html, "default canvas is not 16:9")
require("viewport-fit=cover" in html, "safe-area viewport support missing")
require("poki-sdk.js" in html, "Poki SDK script missing")

urls = []
for text in (html, css, js):
    urls += re.findall(r'https?://[^"\'\s)>]+', text)
allowed = {"https://game-cdn.poki.com/scripts/v2/poki-sdk.js"}
require(all(url in allowed for url in urls), "unapproved external runtime URL found")

require('type="email"' not in html.lower(), "external email/account collection found")
require("window.open(" not in js and "location.href=" not in js, "direct external navigation found")
for event in ("gameLoadingFinished", "gameplayStart", "gameplayStop", "commercialBreak", "rewardedBreak"):
    require(event in js, f"required SDK integration missing: {event}")

require("try{return Number(localStorage.getItem" in js, "localStorage read is not protected")
require("try{localStorage.setItem" in js, "localStorage write is not protected")
require("adActive" in js and "state==='playing'" in js, "ad/gameplay input guard missing")
require("return kind!=='rewardedBreak'" in js, "rewarded ad fallback must never grant a reward when Poki SDK/ad is unavailable")
require("state!=='menu'||adActive||rewardBusy" in js, "duplicate first-start guard missing")
require("setAudioMuted(true)" in js and "setAudioMuted(false)" in js, "ad audio mute/unmute handling missing")
require("🎬" in html, "rewarded-video option lacks a video affordance")
require("background:#24556b" in css, "rewarded button must remain distinct and non-green")
require("rotateHint" in html and "mobileControls" in html, "mobile controls/orientation guidance missing")
require("classList.toggle('touch'" in js, "tablet/mobile input detection missing")
require("resizeCanvas" in js, "responsive render resizing missing")
require("visibilitychange" in js, "background/pause interruption handling missing")
require("measure(" in js, "gameplay analytics hooks missing")
require("overflow:hidden" in css and "touch-action:none" in css, "viewport scrolling prevention missing")
require("window.addEventListener('wheel'" in js and "ArrowDown','ArrowUp','Space" in js, "Poki parent-page key/wheel scroll prevention missing")
require("object-fit:cover" in css and "Math.round(w*9/16)" in js, "16:9 render preservation missing")
require(".ad-active .screen button" in css, "UI input is not disabled during ads")
require("measure('reward','revive','visible')" in js and "measure('reward','revive','interact')" in js, "rewarded placement analytics missing")
require("measure('reward','revive','granted')" not in js, "do not duplicate rewarded-ad completion analytics")
require("e.code==='Space'" in js and "e.code==='Escape'" in js, "keyboard pause/resume support missing")

size = sum(path.stat().st_size for path in root.iterdir() if path.is_file())
require(size < 1_000_000, f"build unexpectedly large: {size} bytes")

if errors:
    print("FAIL: Poki static QA")
    for error in errors:
        print("-", error)
    raise SystemExit(1)

print(f"PASS: Poki static QA ({size} bytes, {len(urls)} allowed external URL reference(s))")