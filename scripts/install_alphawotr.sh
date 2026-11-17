#!/usr/bin/env bash
# BOT-220: install and verify the pinned AlphaWOTR v0.2.0 (MIT, https://github.com/Weyoun5/AlphaWOTR) for the protocol
# seat, in one command, on a Linux x86_64 host:
#
#     bash scripts/install_alphawotr.sh [DIR]          # DIR defaults to /srv/alphawotr
#
# It clones the release at its commit, checks every file of the clone against the release's RELEASE_MANIFEST.tsv, makes
# a Python 3.12 environment with the package versions this seat was measured with (CPU torch), and proves the bot
# answers the protocol's hello directly and through scripts/protocol_sidecar.py. Running it again re-verifies and
# repairs. It needs git and either uv (preferred; it fetches Python 3.12 itself) or python3 >= 3.10 with venv and pip.
# Nothing outside DIR is changed.
set -euo pipefail

DIR=${1:-/srv/alphawotr}
REPO=https://github.com/Weyoun5/AlphaWOTR
TAG=v0.2.0
COMMIT=3dd687463336304a472ec32edab643d720d64d19
TORCH_INDEX=https://download.pytorch.org/whl/cpu
HERE=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
SRC=$DIR/AlphaWOTR
VENV=$DIR/venv
PY=$VENV/bin/python

say() { printf '%s\n' "$*"; }
fail() { printf 'FAIL: %s\n' "$*" >&2; exit 1; }

command -v git >/dev/null || fail "git is required"
mkdir -p "$DIR" "$DIR/logs"

# 1. The release at its commit.
if [ ! -d "$SRC/.git" ]; then
  say "cloning $REPO $TAG into $SRC"
  git -c advice.detachedHead=false clone --quiet --branch "$TAG" --depth 1 "$REPO" "$SRC"
fi
if [ "$(git -C "$SRC" rev-parse HEAD)" != "$COMMIT" ]; then
  git -C "$SRC" fetch --quiet --depth 1 origin "refs/tags/$TAG:refs/tags/$TAG"
  git -C "$SRC" -c advice.detachedHead=false checkout --quiet --force "$COMMIT"
fi
[ "$(git -C "$SRC" rev-parse HEAD)" = "$COMMIT" ] || fail "AlphaWOTR is not at $TAG ($COMMIT)"
[ -z "$(git -C "$SRC" status --porcelain --untracked-files=no)" ] || fail "the clone has local changes: git -C $SRC status"

# 2. Every file of the clone against the release manifest (path, sha256). Files the manifest lists that the git tree lacks
# (the release zip's bundled Python) are counted, not failed; the bundled WOTR-Gandalf client is not used by the protocol.
python3 - "$SRC" <<'PY' || fail "the clone differs from RELEASE_MANIFEST.tsv"
import hashlib, os, sys
root = sys.argv[1]
bad = missing = ok = 0
for line in open(os.path.join(root, "RELEASE_MANIFEST.tsv"), encoding="utf-8"):
    parts = line.rstrip("\n").split("\t")
    if len(parts) < 2 or parts[0] == "RELEASE_MANIFEST.tsv" or parts[0].startswith("wotr_bridge/play_client/"):
        continue
    path = os.path.join(root, parts[0])
    if not os.path.isfile(path):
        missing += 1
        continue
    with open(path, "rb") as f:
        digest = hashlib.sha256(f.read()).hexdigest()
    if digest == parts[1]:
        ok += 1
    else:
        bad += 1
        print("MISMATCH", parts[0])
print(f"manifest: {ok} files match, {bad} differ, {missing} listed but not in the clone")
sys.exit(1 if bad or ok < 1000 else 0)
PY

# 3. The environment, pinned to the versions measured (BOT-220, 2026-11-17).
PINS=(numpy==2.5.4 shapely==2.2.0 pillow==13.0.0 matplotlib==3.11.2 psutil==7.2.2 contourpy==1.4.0 cycler==0.12.1
  fonttools==4.68.0 kiwisolver==1.5.1 packaging==26.3 pyparsing==3.3.3 python-dateutil==4.0.0 tzdata==2026.6)
if command -v uv >/dev/null; then
  [ -x "$PY" ] || uv venv --quiet --python 3.12 "$VENV"
  uv pip install --quiet --python "$PY" --index-url "$TORCH_INDEX" "torch==2.15.0+cpu"
  uv pip install --quiet --python "$PY" "${PINS[@]}"
else
  python3 -c 'import sys; sys.exit(sys.version_info < (3, 10))' || fail "python3 >= 3.10 (or uv) is required"
  [ -x "$PY" ] || python3 -m venv "$VENV"
  "$PY" -m pip install --quiet --upgrade pip
  "$PY" -m pip install --quiet --index-url "$TORCH_INDEX" "torch==2.15.0+cpu"
  "$PY" -m pip install --quiet "${PINS[@]}"
fi
"$PY" -I - <<'PY' || fail "the environment's packages are not the pinned ones"
import matplotlib, numpy, PIL, psutil, shapely, torch
found = {"torch": torch.__version__, "numpy": numpy.__version__, "shapely": shapely.__version__,
         "pillow": PIL.__version__, "matplotlib": matplotlib.__version__, "psutil": psutil.__version__}
want = {"torch": "2.15.0+cpu", "numpy": "2.5.4", "shapely": "2.2.0", "pillow": "13.0.0", "matplotlib": "3.11.2",
        "psutil": "7.2.2"}
print("packages:", ", ".join(f"{k} {v}" for k, v in found.items()))
raise SystemExit(0 if found == want else 1)
PY

# 4. The bot answers hello on its standard streams, then through the sidecar on a free loopback port.
BOT=(env OMP_NUM_THREADS=2 MKL_NUM_THREADS=2 MPLBACKEND=Agg "$PY" -I protocol/wotr_protocol_bot.py)
reply=$(cd "$SRC" && printf '%s\n' '{"type":"hello","id":"install","protocol":"0.2"}' '{"type":"quit"}' |
  timeout 300 "${BOT[@]}" 2>"$DIR/logs/install-hello.log" | head -1) || true
case "$reply" in *'"type": "hello"'*|*'"type":"hello"'*) say "bot: $reply" | cut -c1-200 ;;
  *) fail "the bot did not answer hello (see $DIR/logs/install-hello.log): $reply" ;; esac

port=$(python3 -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1", 0)); print(s.getsockname()[1]); s.close()')
python3 "$HERE/protocol_sidecar.py" --listen "127.0.0.1:$port" --cwd "$SRC" --logs "$DIR/logs" --max-games 1 -- "${BOT[@]}" \
  >"$DIR/logs/install-sidecar.log" 2>&1 &
sidecar=$!
trap 'kill $sidecar 2>/dev/null || true' EXIT
python3 - "$port" <<'PY' || fail "the sidecar did not pass the bot's hello (see $DIR/logs)"
import json, socket, sys, time
port = int(sys.argv[1])
for _ in range(100):
    try:
        conn = socket.create_connection(("127.0.0.1", port), timeout=300)
        break
    except OSError:
        time.sleep(0.1)
else:
    raise SystemExit(1)
conn.sendall(b'{"type":"hello","id":"install","protocol":"0.2"}\n')
line = conn.makefile("r", encoding="utf-8").readline()
conn.sendall(b'{"type":"quit"}\n')
reply = json.loads(line)
print("sidecar:", reply.get("name"), reply.get("version"), "protocol", reply.get("protocol"))
raise SystemExit(0 if reply.get("type") == "hello" else 1)
PY

say "OK: AlphaWOTR $TAG at $SRC, environment $VENV"
say "Run the sidecar (one bot process per CPU game; the server connects to it):"
say "  python3 $HERE/protocol_sidecar.py --listen 127.0.0.1:7311 --cwd $SRC --logs $DIR/logs --max-games 2 -- \\"
say "    env OMP_NUM_THREADS=2 MKL_NUM_THREADS=2 $PY -I protocol/wotr_protocol_bot.py"
say "and set Wotr:Bots:Protocol:Enabled=true (Port 7311, Sims, DecisionSeconds) on the game server."
