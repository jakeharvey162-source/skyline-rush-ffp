# Skyline Rush: Rooftop Strike — Poki Edition

This is a separate browser-first Skyline Rush target. It does not replace the native Windows/Android project.

## Current playable prototype

- Self-contained HTML/CSS/JavaScript FPS prototype.
- Braamfontein Heights-inspired rooftop arena.
- Eight Rogue Machine targets.
- 90-second mission, health, ammo, scoring and best score.
- Desktop controls: WASD, mouse look, click to fire, R to reload.
- Touch controls: movement stick, drag-to-look and fire button.
- Landscape mobile presentation and safe-area aware controls.
- Poki SDK loading/gameplay/commercial-break events.
- localStorage is optional and wrapped so private/incognito restrictions do not stop play.
- No third-party runtime libraries or remote assets. The only remote script is Poki's own SDK.

## What this build is

A real playable Poki prototype suitable for Poki Inspector and early playtest submission. It is intentionally lightweight and browser-native.

It is NOT yet the complete native Skyline Rush port and does not yet include online multiplayer, all native maps, native models, progression or the full desktop feature set.

## Test locally

Serve this directory over HTTP and open it in a browser. Do not rely on file:// for final testing.

Example:

    python3 -m http.server 8080 --directory web/poki

Then open localhost:8080.

For Poki submission, upload the contents of web/poki (index.html must stay at the build root) or use the skyline-rush-poki artifact produced by the Poki web workflow.

## Release gates before asking for a Poki global release

- Run through Poki Inspector.
- Test desktop, phone and tablet browsers.
- Verify Poki SDK event order.
- Test incognito/private browsing.
- Measure loading and sustained performance.
- Replace prototype visuals only with bundled, redistribution-safe assets.
- Add multiplayer only through a Poki-compatible/approved approach; do not add external email login.
