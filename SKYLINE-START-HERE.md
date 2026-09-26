# Latest: 0.2 district and private-room expansion

See [SKYLINE-PLAYTEST-02.md](SKYLINE-PLAYTEST-02.md) for current modes, hosting and limitations. The notes below describe the original 0.1 milestone.

# Skyline Rush — rooftop playtest 0.1

An independent modification of Red Eclipse for Jake Harvey. This is a development prototype, not a finished commercial game.

## Added

- Separate Skyline Rush title, save folder and ports (29799–29801).
- Original teal/amber geometric logo, engine/window icon and map textures.
- Braamfontein Heights: a fictional six-rooftop blockout inspired by Johannesburg, with wide crossings, cover, distant towers, six spawns and a connected bot waypoint grid.
- Rooftop Practice menu shortcut: four-player bot balance, moderate bot skill and five-minute deathmatch.
- Direct/LAN multiplayer foundation inherited from Red Eclipse. Upstream public master registration is disabled. No public matchmaking service has been created.
- Windows build-and-package workflow and launchers. Original credits/licenses remain.

## Windows package

Builds run automatically when game or workflow files change on master. Open Actions → Skyline Rush Windows playtest and select a successful run. You can also use Run workflow for a manual build. Download the skyline-rush-windows-playtest artifact from a successful run, extract the whole folder, and double-click skyline-rush.bat. Do not move the EXE out of the folder; config, assets and DLLs are required.

The Windows client and server have compiled successfully on GitHub. Use the latest successful run; the workflow also checks packaged server startup. The graphical game must still be tested on a real Windows PC. Report any failing build step before treating it as ready.

## Developer setup

Clone this repository with `git clone --recurse-submodules YOUR-FORK-URL`. Assets are large. If already cloned, run `git submodule update --init --recursive`.

On Ubuntu, install build-essential, pkg-config, libsdl2-dev, libsdl2-image-dev, libopenal-dev, libsndfile1-dev, libgl-dev, libx11-dev and zlib1g-dev. Run `make -C src -j2 client server`, then `make -C src install-client install-server` and `./skyline-rush.sh`.

Map generation: `python3 scripts/skyline/generate_arena.py`. Branding regeneration requires Pillow: `python3 scripts/skyline/generate_brand.py`. Generated files are committed; players do not need Python.

## Controls and testing

Use the in-game Settings → controls for the exact current bindings. Start with WASD, mouse look/fire and Space to jump. Click Rooftop Practice to load the arena. The greybox uses inherited weapons and movement. Test spawn clearance, cross-bridge travel, bots, collision, frame rate and restarts before adding detail art.

The bot grid deliberately avoids jump gaps. Bot behavior has not yet been play-tested. The level is a blockout, not a polished Johannesburg recreation. There is no verified graphical preview yet.

For two-player testing, both PCs need the same build and assets. Start skyline-server.bat and use the in-game console command `/connect SERVER-LAN-IP 29801`. LAN/server connections need practical testing; do not expose a public server until access, moderation, updates and operating costs are planned.

## Next release gates

1. Successful Windows client build and startup.
2. Verify textures, lighting, bot navigation and every spawn in a live match.
3. Test two clients against the dedicated server.
4. Iterate on weapon feedback and map balance from player feedback.
5. Replace remaining prototype art as needed and audit all distributed licenses.
6. Consider monetisation only after repeat playtesting shows people enjoy it.

## Source ZIP

The source ZIP excludes the multi-gigabyte upstream assets and contains no playable Windows EXE. Run `powershell -File scripts/skyline/setup-assets.ps1` from a clean extracted copy to download pinned assets. Prefer the GitHub fork and Actions workflow for the first Windows build. Do not upload this entire ZIP as one file to GitHub; the repository needs the extracted source files.

## Verification completed here

The original and modified Linux dedicated servers compile. The modified server starts on port 29801, reports Skyline Rush, reads its separate settings folder and shuts down cleanly on interruption. Structural map validation and connected waypoint/clearance checks pass. The Windows client and server also compiled successfully in GitHub Actions, and a downloadable Windows package was produced. The updated workflow adds a packaged server startup check. Graphical rendering, movement, bot behavior and multiplayer playtesting remain unverified.
