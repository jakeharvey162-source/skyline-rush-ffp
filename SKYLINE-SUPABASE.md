# Skyline Rush + Supabase

Skyline Rush is now connected to Supabase for the multiplayer-lobby foundation.

## Connected project

- Project URL: `https://ftsomveafuskrutqzsvs.supabase.co`
- Client key type: Supabase **publishable** key
- The service-role/secret key is **not** stored in this repository.

## Database objects

All Skyline Rush data is isolated from the other apps in the Supabase project by using a `skyline_` prefix:

- `skyline_profiles` — player profile and basic lifetime stats.
- `skyline_rooms` — room code, mode, district, host endpoint and expiry.
- `skyline_room_members` — authenticated room membership and ready state.
- `skyline_create_room(...)` — authenticated RPC that creates a room and returns its six-character code.
- `skyline_join_room(code)` — authenticated RPC that joins an exact room code and returns the connection details.

## Security

Row Level Security is enabled on every Skyline table. Anonymous users have no table access and cannot execute the room RPCs. Signed-in players can only read a room after they are the host or have joined it. A room's host address and server password are therefore not globally enumerable through the public client key.

## Game bridge

`scripts/skyline/supabase.ps1` is the native launcher bridge. Once the launcher has a Supabase Auth access token it can call:

```powershell
. .\scripts\skyline\supabase.ps1

$room = New-SkylineCloudRoom `
  -AccessToken $token `
  -Mode duel `
  -District heights `
  -HostAddress "100.x.x.x" `
  -ServerPassword "Example_93"

$join = Join-SkylineCloudRoom -AccessToken $token -RoomCode $room.room_code
```

Supabase handles identity and lobby metadata. The C++ dedicated server still carries the actual UDP match traffic; Supabase is not being misused as the real-time FPS game server.

## Next integration gate

Add the sign-in/create-account screen to the Skyline launcher, persist the user's Supabase session locally, and call the two RPCs from the Create Room / Join Room UI so players never need to paste IP addresses manually.
