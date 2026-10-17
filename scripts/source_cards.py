#!/usr/bin/env python3
"""Validate reviewed base card sources; optionally fetch pinned reference sheets."""
import task_registry
import argparse
import hashlib
import json
from pathlib import Path
from urllib.request import urlopen

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return json.loads((ROOT / path).read_text(encoding="utf-8"))


def validate():
    cards = read("docs/sources/base-card-index.json")["cards"]
    sources = {s["id"]: s for s in read("docs/sources/manifest.json")["sources"]}
    production = set(read("content/base-board.json")["cards"])
    ids = [c["id"] for c in cards]
    assert len(ids) == len(set(ids)) == 96
    assert set(ids) == production, "Source IDs and production decks differ"
    metadata = {card['id']: card for card in read('content/base-cards.json')}
    assert set(metadata) == production
    descriptions = read('content/base-card-text.json')
    assert len(descriptions) == 96
    descriptions = {card['id']: card for card in descriptions}
    assert set(descriptions) == production
    for side, prefix in [("FreePeoples", "fp"), ("Shadow", "sh")]:
        for deck in ["Character", "Strategy"]:
            group = [c for c in cards if c["side"] == side and c["deck"] == deck]
            assert sorted(c["number"] for c in group) == list(range(1, 25))
            for card in group:
                assert card["id"] == f"base.{prefix}.{deck.lower()}.{card['number']:02}"
    for card in cards:
        description = descriptions[card['id']]
        assert description['event'] and description['combat']
        assert description['combatTitle'] == card['combatTitle']
        assert description['initiative'] == card['initiative']
        assert metadata[card['id']] == {
            'id': card['id'], 'title': card['title'], 'side': card['side'],
            'deck': card['deck'].lower(), 'eventType': card['eventType'], 'source': card['source']['sheet']
        }, f"Runtime metadata differs from reviewed front: {card['id']}"
        assert card["verification"] == "second_edition_front_reviewed"
        assert card["effectsVerified"] and card["title"] and card["combatTitle"]
        assert card["eventType"] in {"Army", "Muster", "Character"}
        assert card["initiative"] and all(0 <= i <= 7 for i in card["initiative"])
        source = sources[card["source"]["sheet"]]
        left, top, right, bottom = card["source"]["bounds"]
        assert 0 <= left < right <= source["width"]
        assert 0 <= top < bottom <= source["height"]
    tasks = [t for t in task_registry.records() if t["id"].startswith("T21-")]
    assigned = [c for t in tasks for c in t["content_ids"]]
    assert len(assigned) == len(set(assigned)) == 96 and set(assigned) == production
    print("PASS: 96 reviewed card fronts, four complete decks, production IDs, 16 disjoint packs and source bounds.")
    referenced = {c["source"]["sheet"]: sources[c["source"]["sheet"]] for c in cards}
    referenced.update(validate_characters(sources))
    return referenced


def validate_characters(sources):
    cards = read("docs/sources/base-character-index.json")["cards"]
    metadata = {c['id']: c for c in read('content/base-characters.json')}
    assert len(cards) == len({c["id"] for c in cards}) == 14
    assert set(metadata) == {c['id'] for c in cards}
    for side, count in [("FreePeoples", 11), ("Shadow", 3)]:
        assert sorted(c["number"] for c in cards if c["side"] == side) == list(range(1, count + 1))
    initial = [c for c in cards if c["role"] == "Companion" and c["entry"].get("mode") == "setup-fellowship"]
    assert len(initial) == 7 and sum(c["extraActionDice"] for c in cards) == 5
    assert sum(c["identity"] == "gandalf" for c in cards) == 2
    assert sum(c["identity"] == "aragorn" for c in cards) == 2
    for card in cards:
        assert metadata[card['id']] == {
            'id': card['id'], 'title': card['title'], 'identity': card['identity'],
            'side': card['side'], 'role': card['role'], 'level': card['level'], 'leadership': card['leadership'],
            'nationIcon': 'free-peoples' if card['nationIcon'] == 'any' else card['nationIcon'],
            'initial': card['role'] == 'Companion' and card['entry'].get('mode') == 'setup-fellowship',
            'extraActionDice': card['extraActionDice'], 'unlimitedMovement': card['unlimitedMovement'],
            'abilities': card['abilities']
        }, f"Runtime statistics differ from reviewed character: {card['id']}"
        assert card["verification"] == "second_edition_front_reviewed" and card["abilities"]
        if card["role"] in {"Guide", "Fellowship"}:
            assert card["level"] is None and card["leadership"] is None
        source = sources[card["source"]["sheet"]]
        left, top, right, bottom = card["source"]["bounds"]
        assert 0 <= left < right <= source["width"] and 0 <= top < bottom <= source["height"]
    print("PASS: 14 character fronts, seven starting companions, version identities, roles and source bounds.")
    return {c["source"]["sheet"]: sources[c["source"]["sheet"]] for c in cards}


def fetch(sources):
    directory = ROOT / ".artifacts/card-sources"
    directory.mkdir(parents=True, exist_ok=True)
    for source_id, source in sorted(sources.items()):
        destination = directory / (source_id + ".png")
        if destination.exists():
            data = destination.read_bytes()
        else:
            with urlopen(source["url"], timeout=45) as response:
                data = response.read()
        if hashlib.sha256(data).hexdigest() != source["sha256"]:
            raise ValueError(f"Reference changed: {source_id}; requires a new source review")
        if not destination.exists():
            destination.write_bytes(data)
        print(f"PASS: {source_id} SHA-256 matches")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fetch", action="store_true", help="Fetch and verify pinned sheets in ignored local cache")
    args = parser.parse_args()
    sources = validate()
    if args.fetch:
        fetch(sources)


if __name__ == "__main__":
    main()
