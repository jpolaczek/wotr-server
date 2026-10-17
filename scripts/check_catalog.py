import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
catalog = json.loads((root / 'docs/rules/catalog.json').read_text())['rules']
decisions = json.loads((root / 'docs/rules/decisions.json').read_text())['decisions']
inventory = json.loads((root / 'docs/ui/interaction-inventory.json').read_text())
gaps = {gap['id']: gap for gap in json.loads((root / 'docs/sources/gaps.json').read_text())['gaps']}
ids = {r['id'] for r in catalog}
assert len(ids) == len(catalog) == 30
for rule in catalog:
    assert all(rule['specification'].get(k) for k in ('trigger','conditions','choices','cost','order','next_window','duration','visibility','normal_case','edge_case'))
    assert rule['source'] and rule['locator'] and rule['required_modules']
for decision in decisions:
    assert set(decision['rules']) <= ids
    assert all(decision[k] for k in ('owner','visibility','checkpoint','presentation','next_window','scenarios'))
assert len({d['id'] for d in decisions}) == len(decisions)
assert inventory['decisions'] == decisions, 'UI decision inventory is stale; run scripts/build_ui_inventory.py'
presentations = {family['id'] for family in inventory['families']}
assert len(presentations) == len(inventory['families']), 'Duplicate UI presentation family'
for decision in decisions:
    assert decision['presentation'] in presentations, f"{decision['id']}: missing UI family"
for family in inventory['families']:
    assert all(family.get(key) for key in ('normal', 'difficult', 'click_path', 'preview'))
for rule in catalog:
    for gap_id in rule['unresolved']:
        assert gap_id in gaps, f"{rule['id']}: unknown source gap {gap_id}"
        assert gaps[gap_id]['status'] == 'open', f"{rule['id']}: resolved source gap {gap_id} still listed"
required_sources = {'F01': 'GAP-FATE', 'X03': 'GAP-FATE', 'B10': 'GAP-BASE-CHAR', 'K03': 'GAP-KINGS-CARDS'}
for rule in catalog:
    if rule['id'] in required_sources:
        gap_id = required_sources[rule['id']]
        assert (gap_id in rule['unresolved']) == (gaps[gap_id]['status'] == 'open'), f"{rule['id']}: missing source gate {gap_id}"
print(f'PASS: {len(catalog)} rule families and {len(decisions)} decision windows; source/visibility/continuation coverage.')
