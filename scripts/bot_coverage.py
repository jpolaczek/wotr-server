#!/usr/bin/env python3
"""Validate public bot inventory without private code, credentials or claimed behavioral passes."""
import argparse
import collections
import subprocess
import tempfile
from pathlib import Path

from bot_catalog import LEDGER, ROOT, RUNTIME, inventory, read, runtime_digest, source_coverage, write_catalog


def validate(document, runtime, root=ROOT):
    errors, content_ids = source_coverage(runtime, root)
    if errors:
        return errors
    rule_ids = {rule["id"] for rule in read("docs/rules/catalog.json", root)["rules"]}
    if runtime.get("sourceDigest") != runtime_digest(root):
        errors.append("Runtime catalog is stale: regenerate using ConfigurationRules.")
    expected = inventory(runtime, root)
    if document.get("schemaVersion") != expected["schemaVersion"]:
        errors.append("Unknown ledger schema.")
    for section in ("mechanics", "scenarios"):
        actual_rows = document.get(section, [])
        ids = [row["id"] for row in actual_rows]
        for identity, count in collections.Counter(ids).items():
            if count != 1:
                errors.append(f"Duplicate {section}: {identity}")
        wanted = {row["id"]: row for row in expected[section]}
        actual = {row["id"]: row for row in actual_rows}
        for identity in sorted(wanted.keys() - actual.keys()):
            errors.append(f"Missing {section}: {identity}")
        for identity in sorted(actual.keys() - wanted.keys()):
            errors.append(f"Unknown {section}: {identity}")
        for identity in sorted(wanted.keys() & actual.keys()):
            for field, value in wanted[identity].items():
                if actual[identity].get(field) != value:
                    errors.append(f"Invalid {section} {identity}.{field}; expected source-derived value (behavior evidence remains pending).")
    scenarios = {row["id"] for row in document.get("scenarios", [])}
    for row in document.get("mechanics", []):
        if not set(row.get("rules", [])).issubset(rule_ids):
            errors.append("Unknown rule ID: " + row["id"])
        if row.get("content") is not None and row["content"] not in content_ids:
            errors.append("Unknown content ID: " + row["id"])
        for field in ("positive", "negative"):
            if row.get(field) not in scenarios:
                errors.append(f"Orphan scenario: {row['id']}.{field}")
    if document.get("scenarioIntents") != expected["scenarioIntents"]:
        errors.append("Scenario intent definitions differ from the reviewed contract.")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--generate", action="store_true", help="Re-export engine inputs and reset all evidence to pending.")
    args = parser.parse_args()
    if args.generate:
        with tempfile.TemporaryDirectory() as temporary:
            exported = Path(temporary) / "runtime.json"
            subprocess.run(["bash", "scripts/dotnet.sh", "run", "--project", "tools/Wotr.BotCatalog", "--", str(exported)],
                           cwd=ROOT, check=True)
            runtime = read(exported)
        runtime["sourceDigest"] = runtime_digest()
        write_catalog(ROOT / RUNTIME, runtime)
        write_catalog(ROOT / LEDGER, inventory(runtime))
    runtime = read(RUNTIME)
    ledger = read(LEDGER)
    errors = validate(ledger, runtime)
    if errors:
        for error in errors:
            print(error)
        return 1
    invalid = sum(item["conflict"] is not None for item in runtime["configurationInputs"])
    print(f"PASS: {len(ledger['mechanics'])} mechanics; {len(ledger['scenarios'])} planned scenarios; "
          f"{len(runtime['configurations'])} legal normalized configurations; "
          f"{len(runtime['configurationInputs'])} input combinations ({invalid} rejected). "
          "Strategic behavior accepted: 0 (all pending).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
