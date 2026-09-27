# Skyline Rush distribution handoff

## Play the current Windows build

Open https://github.com/jakeharvey162-source/skyline-rush-ffp/releases . Download BOTH numbered archive parts from the same release. Put them in one folder and extract .7z.001 with 7-Zip. Open skyline-rush.bat. Choose practice first; skyline-room.bat hosts private multiplayer.

This is a development playtest. The currently released build does not contain the newly uploaded GLB models. AAB is an Android format, not a Microsoft Store upload.

## itch.io

1. Create an itch.io creator account and a downloadable game project. Keep the page draft until installation has been checked.
2. Install official butler from https://itch.io/docs/butler/ and run `butler login` on your own computer. Do not paste your login token into chat.
3. Extract the full Windows release, then run:

```powershell
./packaging/itch/publish.ps1 -GameDirectory C:/Games/skyline-rush -Project YOUR-ITCH-USERNAME/skyline-rush -Version 0.2-playtest
```

The script adds Play and Host Room launch actions and uploads the complete folder to the Windows channel. Test download, launch, uninstall and reinstall through the itch desktop app. Do not upload just one part of the split archive to itch; use the extracted folder and butler.

## Microsoft Store

Register/sign in to Partner Center, create a Windows game listing and reserve its name. Copy these exact, non-secret fields from Product management > Product identity:

- Package/Identity/Name
- Package/Identity/Publisher
- Package/Properties/PublisherDisplayName
- Reserved display name

Install Windows SDK and run:

```powershell
./packaging/windows/build-msix.ps1 -GameDirectory C:/Games/skyline-rush -IdentityName 'EXACT-IDENTITY' -Publisher 'CN=EXACT-PUBLISHER' -PublisherDisplayName 'EXACT-PUBLISHER-DISPLAY-NAME' -DisplayName 'Skyline Rush' -Version 1.0.0.0
```

This creates an unsigned MSIX intended for Store submission. It is NOT a signed installer for direct sideloading. Microsoft re-signs accepted MSIX packages; certification is still required. Never submit a package with the CI test identity.

Before submission: test the installed package and updates on Windows, run Windows App Certification Kit, complete the content/age-rating questionnaire truthfully, provide real gameplay screenshots, support contact and a privacy policy matching the shipped data collection. Explain runFullTrust: this is a native desktop game using local files, graphics, audio and UDP networking. The launcher writes game settings under LOCALAPPDATA/SkylineRush/player instead of the read-only installation folder.

Store packaging and successful certification are different gates. The project is not yet certified or published in either store.

## Draft listing copy

Title: Skyline Rush

Short description: Challenge friends across African-inspired arenas in a fast-paced PC shooter playtest.

Description: Explore Braamfontein Heights, Lagoon Terminal and Rift Valley Relay. Practise against bots, host a private 1v1 duel, form 2v2 or 4v4 teams, or try experimental co-op against machines. Private matches require a reachable host through LAN, VPN or configured forwarding. This early playtest uses keyboard and mouse and includes evolving environments and balance. It is not an open-world RPG and does not yet include a campaign or Android version.

Do not claim the new uploaded characters, vehicles or weapons until they have been converted, animated where necessary and tested in a release.

## Uploaded models

See model-inventory.json for attribution, source links, embedded licences and inspection results. All six inspected files contain zero animation clips. Winter Soldier has one skin; Combat Robot has no skin. GLB is not a supported model loader in this engine (OBJ/IQM/MD5/SMD/MD2/MD3 are supported).

Winter Soldier: retarget a complete movement/combat animation set, optimise and export to a skeletal format before player/enemy replacement.
Russian Soldier and UTSM: uploads failed; require re-upload and inspection.
Combat Robot: rig and animate for an enemy, or convert as static scenery first.
M4A1 and DC-17m: convert geometry/materials, align grips and muzzle attachment points, then add first/third-person and reload animations.
Yamaha: optimise for garage scenery; a rideable bike additionally requires physics and input work.
Rainier AK: embedded CC-BY-NC-4.0 licence restricts commercial use. Keep out of the monetized build unless permission is obtained or an independently created replacement model is used. A new weapon type also requires game balance, effects, sound, animation and network-compatible implementation.

Embedded licence metadata is an inventory, not proof that every depicted character/brand right is cleared. Keep source attribution and confirm distribution rights before commercial release.

## Official references

- https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements
- https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion
- https://itch.io/docs/butler/
- https://itch.io/docs/itch/integrating/manifest.html
- https://creativecommons.org/licenses/by-nc/4.0/
