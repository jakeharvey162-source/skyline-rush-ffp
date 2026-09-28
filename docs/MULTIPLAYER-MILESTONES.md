# Multiplayer delivery status

Skyline Rush keeps the existing C++ engine, Supabase backend and dedicated-server architecture. The Poki browser target is separate.

## Verified foundation

- Supabase RLS protects Skyline tables; clients cannot write trusted lifetime stats.
- Existing room RPCs provide authenticated six-character room codes and protected connection details.
- `skyline_lobby_action` provides member-only roster, ready, leave, close and host-only start state.
- Host start is rejected until every current lobby member is ready; late joins are rejected after the room enters `starting`.
- The host-start transition was tested transactionally against the live Skyline Supabase project and test rows were rolled back.
- Linux MatchPool isolates matches, passwords, ports and process groups.
- Windows `WindowsMatchHost` uses private state directories and Job Objects so owned server processes die with the launcher.
- Windows host tests cover real dedicated-server startup, unique credentials, port-pair conflicts, capacity, crash reaping and stale-credential cleanup.
- Tailscale discovery accepts only a connected IPv4 in 100.64.0.0/10 and returns player-facing setup errors otherwise.
- Joiners are preflighted before lobby admission: local Tailscale must be connected and the host must answer a Tailscale ping. Failed reachability checks remove the attempted membership instead of leaving a dead lobby slot.

## Native Windows player flow implemented

The Windows package now contains a native `SkylineRush.exe` launcher.

Player flow:

1. Launch Skyline Rush.
2. Sign in or create a Skyline account through Supabase Auth.
3. Play offline immediately, or use private multiplayer.
4. Host chooses mode + district and creates a room.
5. Launcher verifies Tailscale, starts the real local dedicated server, waits for readiness, then creates a protected Supabase room.
6. Host shares only the six-character room code.
7. Joiner signs in and enters the code; Skyline verifies the joiner's Tailscale connection and host reachability before keeping the membership.
8. Joiner sees the lobby roster/ready state.
9. Host selects **Start Match** after everyone is ready.
10. The backend moves the room to `starting`.
11. Host and joiner launchers automatically connect to the protected server endpoint; normal players do not type IPs, passwords or console commands.

The refresh token is persisted with Windows DPAPI for the current Windows user. The Supabase service-role key is never shipped.

## Alpha networking model

Current zero-cost Alpha hosting is:

**participant Windows PC dedicated server + Tailscale + Supabase room discovery**

The host PC must remain online for the match. Every remote participant must already have access to the same Tailscale network. Tailscale enrollment is an external prerequisite; Skyline does not silently provision or administer a player's VPN account.

The optional Linux/DigitalOcean deployment work remains in the repository for a future public-server phase and is not required for the R0 Alpha path.

## Remaining release gates

1. Complete the latest packaged Windows CI run with `SkylineRush.exe` included.
2. Perform a real two-device test on different internet connections through Tailscale:
   - sign in
   - create room
   - join by code
   - ready
   - host start
   - automatic connection
   - movement/shooting/death/respawn
   - disconnect/reconnect
   - host departure cleanup
3. Add trusted match-complete/result reporting before progression stats are enabled.
4. Add a clearer Tailscale enrollment/help surface for first-time testers.
5. Continue menu/HUD, maps, models, gunplay, AI, accessibility and performance polish.
6. Continue the real native Android port; Android is still not playable and no APK should be advertised yet.

Do not describe separate-network multiplayer as proven until the physical two-device test passes.
