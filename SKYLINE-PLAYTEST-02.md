# Skyline Rush 0.2 — African district playtest

## What is implemented
Three separate fictional arenas: Braamfontein Heights (Johannesburg-inspired rooftop crossings), Lagos Lagoon Terminal (freight lanes), and Rift Valley Relay (research courtyard).
Each has eight player starts, connected bot waypoints and six Onslaught-only actor spawns.
The encounters use Red Eclipse's turret, grunt, drone and roller assets. One turret per map has 600 health and 150% scale: an elite enemy, not a fully scripted original boss.
The game menu cycles districts and launches eight-player bot practice or Rogue Machines PvE.
Teams are Jozi Falcons and Lagos Comets in practice and hosted presets.

## Create or join a private Windows room

Download the latest successful Windows build and extract the whole folder.

For normal play, launch `SkylineRush.exe` or double-click `skyline-rush.bat` with no command-line arguments. The native launcher provides:

- Supabase sign-in / account creation.
- Encrypted refresh-token persistence using Windows DPAPI.
- Offline play.
- Tailscale connection status.
- Create Room with mode and district selection.
- Six-character room codes.
- Join Room by code.
- Lobby roster and ready state.
- Host-only Start Match.
- Automatic connection to the protected dedicated server after the host starts.

Modes: duel (2 max), 2v2 (4 max), 4v4 (8 max), coop2 or coop4.
Districts: heights, lagoon or rift.

For the zero-cost internet Alpha, every remote player must already be connected to the same Tailscale network. The host PC runs the real Skyline dedicated server and must stay online during the match. Normal players do not type the host IP, server password or `/connect` command.

`skyline-room.bat` remains as a developer/legacy manual-room tool. It is not the intended public player flow.

Match passwords and local server configuration are generated/stored under the current Windows user's Skyline Rush application data and are not committed to GitHub.

## Distribution and hosting
This is a native PC application, not a browser/WebGL build. Vercel can host a landing page but cannot run this persistent UDP game server.
itch.io can distribute free downloadable builds; use its official butler uploader for large packages.
For private games start with LAN or a private VPN. Tailscale's Personal plan currently allows six users, so do not assume it covers eight distinct accounts for 4v4.
Oracle Cloud Always Free is a potential dedicated-server option, subject to account verification, eligible resources and regional capacity. No cloud resources have been provisioned.
No itch.io, Oracle or Tailscale deployment connection is currently available in this session. GitHub is connected.

## Verification and limits
Map structure, spawn/actor clearance and waypoint connectivity are checked by scripts/skyline/check_arena.py.
The Windows workflow tests all 15 combinations of room mode and district by starting real dedicated servers and reading their selected configuration. A separate Windows host workflow compiles the native launcher and exercises the real dedicated-server host supervisor.
The graphics workflow builds a Linux client, renders each district using software OpenGL, saves screenshots, and attempts two simultaneous localhost clients. A passing run must be inspected before claiming rendering/network smoke success.
These are smoke tests, not full human gameplay, internet latency, combat balance or performance testing.
Not implemented: seamless world streaming, story quests, NPC dialogue, inventory, trusted progression results, original animated monsters, scripted multi-stage bosses, public matchmaking, industrial anti-cheat or native-game monetisation. Android is still a planned native port and no playable APK exists yet.
These district blockouts are a foundation for that expansion, not a finished commercial open-world game.
