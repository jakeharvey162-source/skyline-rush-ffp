#!/usr/bin/env bash
set -euo pipefail

ROOT="${SKYLINE_ROOT:-/opt/skyline-rush/source}"
STATE_DIR="${SKYLINE_HOME:-/var/lib/skyline-rush}"
MODE="${SKYLINE_MODE:-duel}"
DISTRICT="${SKYLINE_DISTRICT:-heights}"
PORT="${SKYLINE_PORT:-29801}"
PASSWORD="${SKYLINE_PASSWORD:-}"

case "$MODE" in
  duel)  SLOTS=2; MUTATORS=1; ENEMIES=0 ;;
  2v2)   SLOTS=4; MUTATORS=0; ENEMIES=0 ;;
  4v4)   SLOTS=8; MUTATORS=0; ENEMIES=0 ;;
  coop2) SLOTS=2; MUTATORS=258; ENEMIES=12 ;;
  coop4) SLOTS=4; MUTATORS=258; ENEMIES=16 ;;
  *) echo "Unsupported SKYLINE_MODE: $MODE" >&2; exit 2 ;;
esac

case "$DISTRICT" in
  heights|lagoon|rift) ;;
  *) echo "Unsupported SKYLINE_DISTRICT: $DISTRICT" >&2; exit 2 ;;
esac

if ! [[ "$PORT" =~ ^[0-9]+$ ]] || (( PORT < 1024 || PORT > 65535 )); then
  echo "SKYLINE_PORT must be 1024-65535" >&2
  exit 2
fi

if ! [[ "$PASSWORD" =~ ^[A-Za-z0-9_-]{6,32}$ ]]; then
  echo "SKYLINE_PASSWORD must be 6-32 letters, digits, underscores or hyphens" >&2
  exit 2
fi

SERVER="$ROOT/src/redeclipse_server_linux"
if [[ ! -x "$SERVER" ]]; then
  echo "Missing Skyline Rush Linux server binary: $SERVER" >&2
  exit 3
fi

install -d -m 0750 "$STATE_DIR"
cat > "$STATE_DIR/servinit.cfg" <<EOF
serverport $PORT
servermaster ""
serverpass "$PASSWORD"
sv_serverdesc "Skyline Rush | Cloud | $MODE"
sv_serverclients $SLOTS
sv_serverspectators 0
sv_resetvarsonend 0
sv_defaultmode 2
sv_defaultmuts $MUTATORS
sv_defaultmap "maps/skyline/$DISTRICT"
sv_rotatemode 0
sv_rotatemuts 0
sv_rotatemaps 0
sv_botbalance 0
sv_botbalanceduel 0
sv_botbalancesurvivor 0
sv_botlimit 0
sv_enemylimit $ENEMIES
sv_enemyspawntime 20000
sv_enemyspawndelay 3000
sv_coopskillmin 35
sv_coopskillmax 55
sv_enemyskillmin 35
sv_enemyskillmax 55
sv_timelimit 10
sv_teambalance 4
sv_teamalphaname "Jozi Falcons"
sv_teamomeganame "Lagos Comets"
sv_teamenemyname "Rogue Machines"
EOF

cd "$ROOT"
exec "$SERVER" -h"$STATE_DIR" -gserver.log -ss1 -sm -xskyline_startroom
