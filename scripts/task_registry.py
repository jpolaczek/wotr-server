"""Read the local task registry and the tracked content coverage ledger.

The task registry (docs/tasks: tasks.json, completions.json, task documents) is local-only and ignored by Git
(user decision 2026-10-17). The coverage checks in CI read the tracked ledger docs/rules/content-ledger.json: the
completed card and character packs with their source-to-content assignments. Locally both are read, the local
completion ledger also keeping the dependency identity of every completed task.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTENT_LEDGER = ROOT / 'docs/rules/content-ledger.json'
LOCAL_LEDGER = ROOT / 'docs/tasks/completions.json'
REGISTRY = ROOT / 'docs/tasks/tasks.json'


def completed_records():
    records = {}
    for path in (LOCAL_LEDGER, CONTENT_LEDGER):
        if path.exists():
            for entry in json.loads(path.read_text(encoding='utf-8'))['completions']:
                records.setdefault(entry['id'], entry)
    return list(records.values())


def records(data=None):
    if data is None:
        data = json.loads(REGISTRY.read_text(encoding='utf-8')) if REGISTRY.exists() else {'tasks': []}
    completed = [dict(entry, status='done', depends_on=[]) for entry in completed_records()]
    return data['tasks'] + completed
