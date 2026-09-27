# Poki release-readiness matrix

This matrix separates requirements we can verify automatically/in-browser from the Poki-hosted gates that require Poki for Developers.

## Local / automated gates

| Requirement | Status | Evidence |
| --- | --- | --- |
| Desktop support | PASS | Desktop browser user-flow test: load, deploy, aim, fire, kill, pause/resume |
| Mobile support | PASS | Landscape touch layout, fire input, responsive canvas |
| Tablet mobile controls | PASS | Coarse/touch input forces mobile controls |
| 16:9 Poki frame | PASS | Native canvas is 1280×720 and scales at required desktop frame sizes |
| Incognito/restricted storage | PASS | localStorage reads/writes are guarded and restricted-storage simulation remains playable |
| External resources | PASS | Only Poki's official SDK URL is referenced at runtime |
| No external account collection | PASS | No email/social login or personal-data form |
| No chat | PASS | No text-chat system |
| No third-party ads | PASS | Only Poki SDK ad calls are implemented |
| Ad-block resilience | PASS | Game remains playable when the SDK is unavailable |
| SDK loading event | PASS | gameLoadingFinished fires after boot |
| Gameplay events | PASS | gameplayStart on player deploy/resume; gameplayStop on pause/result |
| Midroll placement | PASS | commercialBreak only at return-to-play natural breaks |
| Rewarded placement | PASS | Optional one-revive offer, never required for core play |
| Reward button hierarchy | PASS | Normal restart remains primary and visible; rewarded option is separate, blue, and carries a video icon |
| Audio during ads | PASS | Procedural audio is muted/suspended through ad playback |
| Input during ads | PASS | Gameplay state/input guards remain inactive during breaks |
| Save behavior | PASS | Best score persists when storage is available; game remains playable if not |
| Fast loading / lean build | PASS | Core build is tens of KB, with no remote game assets |
| Clear onboarding | PASS | Immediate deploy + short control guide + in-game hint |
| Adaptive controls | PASS | Keyboard/mouse on desktop; joystick/look/fire/reload on touch |
| Page-scroll prevention | PASS | Fixed full-screen shell, overflow hidden, touch-action none |
| Keyboard pause/resume | PASS | ESC and Space supported |
| Content safety baseline | PASS | Stylized robot targets, no gore, no chat, no personal-data collection |

## Retention and monetization design

- Three escalating waves form a roughly three-minute core run.
- Rush multiplier rewards quick consecutive kills.
- Best score gives a replay target.
- Wave healing and escalating enemy behavior create progression without a grind economy.
- Commercial breaks are only signaled at natural replay/resume moments.
- Rewarded revive is optional, one-per-run, and core gameplay remains available without an ad.
- No internal ad timer is used; Poki controls actual ad frequency.
- Custom measure events track session start, first kill, wave milestones, result, and granted revive for iteration.

## Poki-hosted gates that cannot be claimed locally

| Poki gate | Status |
| --- | --- |
| Poki Inspector QA modules | BLOCKED — upload the final ZIP in Poki for Developers / Inspector |
| Poki Event Log on production SDK | BLOCKED — requires Poki Inspector |
| Content moderation | BLOCKED — Poki decision |
| 10-player recorded playtest | BLOCKED — Poki testing stage |
| Player fit test (500 players) | BLOCKED — Poki testing stage |
| Web fit test / CTR / C2P | BLOCKED — Poki testing stage |
| Final review / Global Release | BLOCKED — Poki decision |
| Live ad fill and revenue | BLOCKED — starts only under Poki's released/testing monetization conditions |

Do not describe the game as globally released or guaranteed to trend until these platform-side stages pass.