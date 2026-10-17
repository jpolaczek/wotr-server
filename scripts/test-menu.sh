#!/usr/bin/env bash
# Dedicated local PostgreSQL instance; never uses the player's database.
set -euo pipefail
wotr_menu_root="$(cd "$(dirname "$0")/.." && pwd)"
export WOTR_TEST_DB="${WOTR_MENU_TEST_DB:-Host=127.0.0.1;Port=55448;Username=wotr;Database=wotr_menu_test}"
exec bash "$wotr_menu_root/scripts/test.sh" "$@"
