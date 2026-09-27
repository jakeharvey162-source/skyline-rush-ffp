# Android release status

No playable APK exists yet. This native PC game currently uses desktop SDL/OpenGL and has no Android game project, touch controls or tested mobile renderer.

A real Android port requires arm64 native dependencies, Android lifecycle/storage handling, OpenGL ES-compatible rendering and shaders, touch input, mobile asset delivery and device testing. Wrapping the download website in a WebView does not port the game.

Public distribution requires a persistent private signing key, signed APK verification, device install/update tests, and compliance with current Android developer verification requirements. Never commit signing secrets. Warning-free installation and store acceptance cannot be guaranteed.

The website exposes only published Windows release assets, with archive integrity checks and checksums. Android download links must remain unavailable until a playable build is tested.

## First-class development target

Windows and Android are both required targets. See [the staged cross-platform development plan](docs/ANDROID-DEVELOPMENT-PLAN.md) for native engine work, touch UI, shared Supabase rooms, Tailscale alpha hosting, compatibility checks, APK/AAB CI and physical-device release gates. These are requirements, not completed Android features.
