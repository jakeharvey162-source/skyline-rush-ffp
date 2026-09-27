# DigitalOcean dedicated server

This folder prepares Skyline Rush for a real internet-facing Linux dedicated server.

## What it does

- Builds the native Linux `redeclipse_server_linux` binary from this repository.
- Runs the game under a dedicated unprivileged `skyline` system user.
- Creates a persistent systemd service with automatic restart.
- Opens the selected UDP game port in UFW while preserving SSH.
- Supports the existing Skyline Rush presets: `duel`, `2v2`, `4v4`, `coop2`, and `coop4`.
- Supports the three current districts: `heights`, `lagoon`, and `rift`.

## Install on a fresh Ubuntu Droplet

Run as root:

```bash
git clone --depth=1 https://github.com/jakeharvey162-source/skyline-rush-ffp.git
cd skyline-rush-ffp
sudo bash deploy/digitalocean/install.sh
```

The installer creates `/etc/skyline-rush/server.env` with a generated server password. Keep that file private.

## Change a match preset

Edit `/etc/skyline-rush/server.env`, then restart:

```bash
sudo systemctl restart skyline-rush
```

Example:

```text
SKYLINE_MODE=2v2
SKYLINE_DISTRICT=lagoon
SKYLINE_PORT=29801
SKYLINE_PASSWORD=replace_with_private_room_password
```

## Verify

```bash
systemctl status skyline-rush
journalctl -u skyline-rush -n 100 --no-pager
ss -lunp | grep 29801
```

Players use the same Skyline Rush build and connect to the Droplet's public IPv4 address on the configured UDP port. The Supabase lobby layer can publish the address and room credentials only to authenticated room members, so the finished launcher does not need to ask players to type an IP address.

## Security notes

Never commit a Supabase secret/service-role key or a room password. The existing publishable Supabase key may be shipped in the client only because access is constrained with RLS.
