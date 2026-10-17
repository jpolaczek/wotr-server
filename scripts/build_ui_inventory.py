"""Synchronize canonical decisions without recreating superseded UI wireframes."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
inventory_path = root / 'docs/ui/interaction-inventory.json'
existing = json.loads(inventory_path.read_text())
if existing.get('interaction_revision', 1) < 2:
    raise SystemExit('Current interaction inventory required; historical wireframes are retired.')
existing['decisions'] = json.loads((root / 'docs/rules/decisions.json').read_text())['decisions']
inventory_path.write_text(json.dumps(existing, ensure_ascii=False, indent=2) + '\n')
print('PASS: current UI design preserved; canonical decisions synchronized.')
