#!/usr/bin/env bash
# The whole backend suite in six test hosts, with disjoint filters that cover every test (docs/TESTING.md, EXP-10). In one host the suite
# outgrows 16 GB: the kernel kills the test host, and on CI the runner with it. Every part runs even after a failure, and their results
# are merged into one <results>/backend.trx for the coverage scripts.
# Usage: scripts/test_parts.sh [results folder]. Needs WOTR_TEST_DB, and LC_ALL=en_US.UTF-8 on a Mac.
set -uo pipefail
RESULTS="${1:-.artifacts/test-results}"
NOT_MODULES='FullyQualifiedName!~Expansion&FullyQualifiedName!~Kings&FullyQualifiedName!~Lords'
NOT_GROUP='FullyQualifiedName!~Fate&FullyQualifiedName!~Council&FullyQualifiedName!~Release&FullyQualifiedName!~ModuleIndependence'
PARTS=(
  "FullyQualifiedName~Expansion"
  "FullyQualifiedName~Kings&FullyQualifiedName!~Expansion"
  "FullyQualifiedName~Lords&FullyQualifiedName!~Expansion&FullyQualifiedName!~Kings"
  "(FullyQualifiedName~Fate|FullyQualifiedName~Council|FullyQualifiedName~Release|FullyQualifiedName~ModuleIndependence)&$NOT_MODULES"
  "FullyQualifiedName~Persistence&$NOT_MODULES&$NOT_GROUP"
  "FullyQualifiedName!~Persistence&$NOT_MODULES&$NOT_GROUP"
)
dotnet build --no-restore --configuration Release --nologo -v q || exit 1
rm -f "$RESULTS"/backend*.trx
status=0
for index in "${!PARTS[@]}"; do
  echo "== part $((index + 1)) of ${#PARTS[@]}: ${PARTS[index]}"
  dotnet test tests/Wotr.Tests --no-build --configuration Release --filter "${PARTS[index]}" \
    --logger "trx;LogFileName=backend-$index.trx" --results-directory "$RESULTS" || status=1
done
python3 scripts/merge_trx.py "$RESULTS/backend.trx" "${#PARTS[@]}" "$RESULTS"/backend-*.trx || status=1
exit "$status"
