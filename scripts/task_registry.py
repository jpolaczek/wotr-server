"""Read active tasks and the compact completion/coverage ledger.

The ledger has no task descriptions or history. It preserves dependency identity
and source-to-content assignments after completed task documents are removed.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def completed_records():
    path = ROOT / 'docs/tasks/completions.json'
    if not path.exists():
        return []
    return json.loads(path.read_text(encoding='utf-8'))['completions']


def records(data=None):
    if data is None:
        data = json.loads((ROOT / 'docs/tasks/tasks.json').read_text(encoding='utf-8'))
    completed = [dict(entry, status='done', depends_on=[]) for entry in completed_records()]
    return data['tasks'] + completed
