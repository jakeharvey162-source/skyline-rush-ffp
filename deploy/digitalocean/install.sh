#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
  echo "Run this installer as root." >&2
  exit 1
fi

REPO_URL="${SKYLINE_REPO_URL:-https://github.com/jakeharvey162-source/skyline-rush-ffp.git}"
INSTALL_ROOT="${SKYLINE_INSTALL_ROOT:-/opt/skyline-rush}"
SOURCE_DIR="$INSTALL_ROOT/source"
STATE_DIR="/var/lib/skyline-rush"
ENV_DIR="/etc/skyline-rush"
PORT="${SKYLINE_PORT:-29801}"

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends build-essential ca-certificates git pkg-config ufw zlib1g-dev

if ! id skyline >/dev/null 2>&1; then
  useradd --system --home "$STATE_DIR" --create-home --shell /usr/sbin/nologin skyline
fi

mkdir -p "$INSTALL_ROOT"
if [[ -d "$SOURCE_DIR/.git" ]]; then
  git -C "$SOURCE_DIR" fetch --depth=1 origin main
  git -C "$SOURCE_DIR" reset --hard origin/main
  git -C "$SOURCE_DIR" submodule update --init --recursive
else
  rm -rf "$SOURCE_DIR"
  git clone --depth=1 --recurse-submodules "$REPO_URL" "$SOURCE_DIR"
fi

make -C "$SOURCE_DIR/src" -j"$(nproc)" server CXXFLAGS=-O2
test -x "$SOURCE_DIR/src/redeclipse_server_linux"

install -m 0755 "$SOURCE_DIR/deploy/digitalocean/run-server.sh" /usr/local/bin/skyline-rush-server
install -d -m 0750 "$ENV_DIR" "$STATE_DIR"
chown -R skyline:skyline "$STATE_DIR"

if [[ ! -f "$ENV_DIR/server.env" ]]; then
  GENERATED_PASSWORD="$(od -An -N8 -tx1 /dev/urandom | tr -d ' \n')"
  cat > "$ENV_DIR/server.env" <<EOF
SKYLINE_ROOT=$SOURCE_DIR
SKYLINE_HOME=$STATE_DIR
SKYLINE_MODE=duel
SKYLINE_DISTRICT=heights
SKYLINE_PORT=$PORT
SKYLINE_PASSWORD=$GENERATED_PASSWORD
EOF
  chmod 0640 "$ENV_DIR/server.env"
  chown root:skyline "$ENV_DIR/server.env"
fi

cat > /etc/systemd/system/skyline-rush.service <<'EOF'
[Unit]
Description=Skyline Rush dedicated multiplayer server
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=skyline
Group=skyline
EnvironmentFile=/etc/skyline-rush/server.env
ExecStart=/usr/local/bin/skyline-rush-server
Restart=always
RestartSec=3
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/lib/skyline-rush

[Install]
WantedBy=multi-user.target
EOF

ufw allow OpenSSH
ufw allow "$PORT/udp"
ufw --force enable

systemctl daemon-reload
systemctl enable --now skyline-rush.service
sleep 2
systemctl --no-pager --full status skyline-rush.service || true

echo
echo "Skyline Rush server installed."
echo "UDP port: $PORT"
echo "Configuration: $ENV_DIR/server.env"
echo "Server log: $STATE_DIR/server.log"
echo "Use: journalctl -u skyline-rush -f"
