# Skyline Rush: Windows and Android development plan

Status: accepted requirements; implementation and device validation remain open.
This extends the existing 16-priority alpha roadmap. It does not replace the
C++/SDL engine, repository, Windows infrastructure or Supabase architecture.

## Platform contract

- Windows PC: native client, dedicated server, itch.io Windows package.
- Android: real native C++ FPS, SDL Android integration, arm64-v8a first.
  No WebView, website wrapper or desktop HUD merely scaled down.
- Android outputs: debug APK, securely signed release APK, and AAB when needed.
  itch.io APK is the initial distribution target; storefront AAB comes later.
- Shared accounts, multiplayer identity, content and compatible network protocol.
  Android phones are clients, not dedicated hosts in the initial alpha.
- Keep PC visual quality and existing passing build/package/smoke checks.
- Android is not currently playable. A packaged APK alone does not establish support.

## Milestone A — native feasibility and renderer audit

Before implementation inspect the current source, build system, SDL version,
native dependencies, renderer/shaders, file access and network serialization.
Record concrete desktop-only APIs and required adaptations.

- Add an isolated Android application/build target using JDK, Android SDK and NDK.
  Pin compatible toolchain/dependency versions after verification.
- Compile the actual client and required libraries for arm64-v8a.
- Integrate SDL Android entry points, activity lifecycle and native library loading.
- Port desktop OpenGL calls and shaders to an explicitly selected OpenGL ES baseline;
  audit framebuffer formats, extensions, precision and texture support.
- Handle landscape orientation, rotation/surface recreation, high DPI and changing
  screen sizes. Keep application state separate from graphics-context lifetime.
- Use app-private writable storage, Android-compatible packaged asset reads,
  versioned content installation and integrity checks. Avoid broad storage access.
- Handle permissions, audio focus, pause/resume, backgrounding and interruptions.
  Release held inputs on focus loss; suspend audio/render work appropriately.
- Verify native audio and networking, including network loss and VPN changes.

Exit gate: install a debug APK on a real Android device, launch the actual engine,
load a Skyline district, render correctly, move and fire. Record logs and real
screenshots. Until then label artifacts experimental and not player-ready.

## Milestone B — mobile interaction and presentation

Implement an input abstraction that preserves keyboard/mouse behaviour and supports
simultaneous touch contacts identified independently.

Default controls:
- Left: virtual movement joystick.
- Right: swipe/look camera.
- Fire, aim/ADS, reload, jump, supported crouch/slide, weapon switch, interact,
  scoreboard and pause buttons.
- Configurable size, position and opacity; look and aim sensitivity; reset layout.
- Optional gyro only after correct calibration, lifecycle and device testing.
- Safe-area insets for display cutouts and navigation/gesture areas.

Create deliberate phone/tablet HUD layouts for 16:9, 18:9, 19.5:9 and 20:9.
All login, registration, play, multiplayer, create/join, lobby, loadout, settings,
profile and post-match screens must work with large touch targets and no hover
or right-click requirements. Include keyboard/text-entry behaviour for room codes
and credentials. Preserve health, ammo, objectives, scores and connection readability.

Exit gate: test simultaneous movement/look/fire, edge gestures, interrupted touches,
layout persistence, text entry and every menu on multiple aspect ratios.

## Milestone C — one multiplayer system, no-cost alpha hosting

Use the existing Supabase accounts, skyline_profiles, skyline_rooms,
skyline_room_members, skyline_create_room and skyline_join_room.
Extend existing RPCs safely; do not create a second Android database.

Alpha topology:
- A participant-controlled PC runs the existing dedicated server.
- PC and Android players join the same authorized Tailscale private network.
- Supabase stores protected room membership/state and provides authorized connection
  information. Players sign in and enter six-character room codes, not IP addresses.
- The game connects automatically after readiness, compatibility and server checks.
- Tailscale enrollment/installation is a separate one-time prerequisite; explain
  unavailable VPN/server states clearly. Do not promise automatic VPN provisioning.
- No paid cloud provisioning is required for this alpha path. Verify applicable
  service free-tier eligibility/limits before publishing setup instructions.
- Retain existing Linux/cloud multi-match preparation as an optional later deployment;
  Vercel is not a game server.

Implement sign-up/in/out, secure session persistence/refresh, display name, mode/map
selection, roster/teams, ready state, host controls, expiry, leave, match starting,
in-progress and lobby/rematch flow across both clients. Host-PC sleep/shutdown and
server loss must produce actionable errors, not silent connection attempts.
A participant hosting a match may need guided setup; ordinary joining players must
not use router settings, developer consoles or manual connect commands.

Keep secrets and service-role keys out of both clients. Preserve RLS and trusted
stat writes. Validate allocation/host actions and occupancy on trusted services.
Do not assume the Linux-only MatchPool already manages Windows server processes:
implement and test a compatible host adapter without replacing the working server.

Exit gate: PC and Android on separate networks join by room code through Tailscale,
play together, leave/reconnect, and return to lobby. Test expired/full/wrong-code
rooms, unavailable VPN/server, wrong password, host departure and timeouts.

## Milestone D — compatibility and input policy

Before connection compare:
- Client release/version and supported compatibility range.
- Network protocol version.
- Selected map version/hash.
- Required content version/manifest.

Enforce compatibility again on the authoritative server; a client-only preflight is
insufficient. Reject mismatches with clear update instructions before loading a
broken match. Test arm64/PC serialization rather than assuming identical layouts.
Retain existing engine handshake checks and add missing checks incrementally.

Prepare explicit room input/platform policy: cross-play, PC-only, mobile-only.
Show the policy in room creation and lobby, validate joining eligibility, and avoid
silently changing existing room behaviour. Platform self-reporting is not anti-cheat.
No hidden mobile gameplay advantages. Any aim assist must be visible, configurable,
balanced and subject to a documented room policy.

## Milestone E — mobile quality and performance

Separate Android quality settings from PC defaults:
- Low: reduced shadows/particles/dynamic lights, lower texture budget, simpler
  post-processing and shorter draw distance where necessary.
- Medium and High: progressively larger measured budgets.
- Selectable 30 FPS cap and 60 FPS where sustainable.

Implement measured LODs, supported texture compression/fallbacks, mobile shaders,
optimized geometry and content packaging. Preserve African district identity and
combat readability. Do not bundle unlicensed assets or blindly copy the full PC
asset payload into an APK.

Profile frame time/FPS, CPU/GPU where available, RAM, startup/map-load time,
network traffic, sustained thermals and battery impact. Record device and settings.
Prefer stable frame pacing over effects. Define minimum supported hardware only
after measurement; do not invent performance claims.

## Milestone F — Android CI and signing

Add GitHub Actions only alongside a buildable native target:
1. Set up pinned compatible JDK.
2. Install Android SDK/NDK and required build tools.
3. Build arm64 native client and dependencies.
4. Build Android application and debug APK.
5. Run available unit/static/package/native checks.
6. Upload debug APK, symbols and useful test/log artifacts.

Add release APK/AAB workflow after debug runtime gates:
- Keystore, alias and passwords supplied through GitHub secrets.
- No committed keystore/passwords or credentials in logs/artifacts.
- Restrict signing to trusted release jobs; never expose secrets to untrusted PRs.
- Verify signing, package identity, version codes and upgrade compatibility.
- Keep the signing identity backed up securely for future updates.
- Distinguish installable APK from store-upload AAB; test delivered content paths.
- Publish checksums, version, content compatibility and honest known issues.

Maintain Windows client/server, Linux server, district validators, graphical and
multiplayer smoke tests. Do not mark a workflow green through placeholder builds
or waived native compilation failures.

## Milestone G — physical-device release gates

Required matrix: low/mid-range Android, modern Android, different screen ratios,
plus tablet/responsive coverage. Emulators supplement, not replace, physical devices.

For each tested device record model, Android/GPU versions, RAM, build commit,
content versions, quality/FPS cap, duration, measurements, logs and screenshots.
Verify:
- Install, startup, update and clean first launch.
- Movement/look/actions, orientation and safe areas.
- Audio, map loading, memory and sustained FPS/temperature.
- Sign-in, room flow, PC/Android multiplayer and reconnect.
- Background/resume, notification interruption and network changes.
- Timeout, high latency and packet loss where practical.
- Match ending, results, return-to-lobby and rematch.

No playable-Android claim until install, actual gameplay and the relevant device
checks pass. Access to physical test devices and a host PC/Tailscale network is an
external prerequisite; never fabricate those results.

## Release acceptance

Android: download itch.io APK → install → launch → sign in → enter room code →
join a native multiplayer match (with documented Tailscale alpha prerequisite).

Windows: download itch.io package → launch → sign in → create/join room → play.

Both use the same accounts, rooms and identity. Preserve upstream attribution,
asset redistribution rules and the original 16-priority polish/security/QA plan.
