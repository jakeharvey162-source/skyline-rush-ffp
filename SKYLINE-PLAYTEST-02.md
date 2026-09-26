# Skyline Rush 0.2 — African district playtest

## What is implemented
Three separate fictional arenas: Braamfontein Heights (Johannesburg-inspired rooftop crossings), Lagos Lagoon Terminal (freight lanes), and Rift Valley Relay (research courtyard).
Each has eight player starts, connected bot waypoints and six Onslaught-only actor spawns.
The encounters use Red Eclipse's turret, grunt, drone and roller assets. One turret per map has 600 health and 150% scale: an elite enemy, not a fully scripted original boss.
The game menu cycles districts and launches eight-player bot practice or Rogue Machines PvE.
Teams are Jozi Falcons and Lagos Comets in practice and hosted presets.

## Create a private Windows room
Download the latest successful Windows build and extract everything.
Open skyline-room.bat, enter a mode and district, then a private password.
Modes: duel (2 humans), 2v2 (4 humans), 4v4 (8 humans), coop2 or coop4.
The launcher runs a dedicated server and connects your game to localhost.
Your friend opens the game console and enters:
    /connect HOST-ADDRESS 29801 YOUR-PASSWORD
Use the host's LAN address on the same network. Across the internet, both PCs can use a private VPN such as Tailscale, or the host must configure UDP 29801 forwarding/firewall access.
No room-code matchmaking or NAT relay is included. The host PC must stay running.
Only one room can use the default port on one PC at a time.
The room window's Enter key stops the server. Teams can be chosen through Change team; automatic balancing enforces even teams.
Passwords/configuration are stored under the host's LOCALAPPDATA/SkylineRush/rooms, not committed to GitHub.
PowerShell must permit running this downloaded local script; inspect/unblock the downloaded package according to your Windows policy.

## Distribution and hosting
This is a native PC application, not a browser/WebGL build. Vercel can host a landing page but cannot run this persistent UDP game server.
itch.io can distribute free downloadable builds; use its official butler uploader for large packages.
For private games start with LAN or a private VPN. Tailscale's Personal plan currently allows six users, so do not assume it covers eight distinct accounts for 4v4.
Oracle Cloud Always Free is a potential dedicated-server option, subject to account verification, eligible resources and regional capacity. No cloud resources have been provisioned.
No itch.io, Oracle or Tailscale deployment connection is currently available in this session. GitHub is connected.

## Verification and limits
Map structure, spawn/actor clearance and waypoint connectivity are checked by scripts/skyline/check_arena.py.
The Windows workflow tests all 15 combinations of room mode and district by starting real dedicated servers and reading their selected configuration.
The graphics workflow builds a Linux client, renders each district using software OpenGL, saves screenshots, and attempts two simultaneous localhost clients. A passing run must be inspected before claiming rendering/network smoke success.
These are smoke tests, not full human gameplay, internet latency, combat balance or performance testing.
Not implemented: seamless world streaming, story quests, NPC dialogue, inventory, progression saves, original animated monsters, scripted multi-stage bosses, public matchmaking, anti-cheat or monetisation.
These district blockouts are a foundation for that expansion, not a finished commercial open-world game.
