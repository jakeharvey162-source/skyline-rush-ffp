# Multiplayer delivery status

Continue the existing C++ engine, Supabase tables and Linux server. No replacement architecture.

## Verified foundation increment

- Removed broad authenticated table grants, including TRUNCATE. A client can create its own profile with a display name and change that name; it cannot insert or update wins, losses, kills or deaths. Existing RLS is retained.
- Added skyline_lobby_action: member-only roster, ready state, leave and host close. Host departure closes the room and clears membership. Unauthorized control, expired rooms and nonmembers are rejected.
- Existing create and join RPCs remain available. They are legacy endpoint-based calls, not a finished automatic cloud allocation flow.
- Linux runner disables shared LAN and HTTP listeners, protects its config with umask 077, and validates the query-port boundary.
- Added a trusted-local MatchPool component with unique passwords, per-room directories, process groups, a single-supervisor lock, limits, idle/max-lifetime cleanup and crash detection. It reserves UDP pairs: 29801/29802, 29803/29804, and so on. Adjacent game ports collide with this engine's query socket.
- Seven process supervisor tests passed. Two actual locally available native Linux dedicated servers started concurrently on 41201/41202 and 41203/41204 and shut down cleanly. This does not test players on separate networks.
- Database role tests and transactional integration tests passed for create, join, full room, roster, ready, nonmember rejection, nonhost close rejection and host-leave cleanup. Test identities and rows were rolled back.

## Next required implementation

1. Native player launcher with sign-up/sign-in, encrypted refresh-token persistence, session refresh/sign-out, profile and lobby UI. The new bridge methods are not a shipped UI.
2. Authenticated allocator that invokes MatchPool on a public machine, verifies host/readiness and updates existing room status. Never expose MatchPool directly to clients. It currently has no HTTP endpoint and is not deployed.
3. Trusted occupancy/heartbeat adapter and a loop calling reap; systemd KillMode=control-group for restart cleanup. A crash must return the room to an understandable failed/retry state; automatic recovery is not implemented by the component alone.
4. Server-authorized match results and progression. Clients must not regain stat-write grants.
5. Public-server two-network tests, password rejection, reconnect, packet loss and latency; then player release.

The full 16-priority alpha roadmap remains open: menu/HUD redesign, maps, model conversion and animation, gunplay, AI, progression, accessibility, performance and release QA. Uploaded GLB assets are still not integrated. No public-server deployment or complete online room flow is claimed by this increment.

## Windows and Android requirement

[The Android development plan](ANDROID-DEVELOPMENT-PLAN.md) extends this roadmap. Both native clients must share accounts, rooms and compatibility checks. The immediate no-cost alpha hosting path is a participant PC dedicated server over Tailscale, with Supabase room discovery; public cloud allocation remains optional. Android is not yet playable. The Linux MatchPool still needs a tested Windows host adapter for that alpha hosting path.
