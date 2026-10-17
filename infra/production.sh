#!/usr/bin/env bash
# DIST-04: Compose of the production stack on Omarchy — compose.yaml with compose.production.yaml and infra/.env.
# Usage: infra/production.sh config --quiet | up -d --wait | ps | logs <service> | down …
set -euo pipefail
infra="$(cd "$(dirname "$0")" && pwd)"
exec docker compose --env-file "$infra/.env" -f "$infra/compose.yaml" -f "$infra/compose.production.yaml" "$@"
