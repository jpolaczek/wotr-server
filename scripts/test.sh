#!/usr/bin/env bash
set -euo pipefail
wotr_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$wotr_root"
# The task registry is local-only (docs/tasks, ignored by Git); validate it where it exists.
if [[ -f docs/tasks/tasks.json ]]; then
  python3 scripts/tasks.py validate
fi
python3 scripts/check_catalog.py
python3 scripts/source_cards.py
if [[ -z "${WOTR_TEST_DB:-}" ]]; then
  bash scripts/postgres.sh start
  export WOTR_TEST_DB='Host=127.0.0.1;Port=55432;Username=wotr;Database=wotr_test'
fi
bash scripts/dotnet.sh test --configuration Release "$@"
