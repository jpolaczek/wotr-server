#!/usr/bin/env python3
"""Validate the reviewed Lords event manifest and optionally fetch pinned fronts."""
import task_registry
import argparse
import hashlib
import json
from pathlib import Path
import struct
from urllib.request import urlopen

ROOT = Path(__file__).resolve().parents[1]
INDEX_PATH = "docs/sources/lords-card-index.json"


def read(path):
    return json.loads((ROOT / path).read_text(encoding="utf-8"))


def validate_cards(index, sources):
    cards = index["cards"]
    assert len(cards) == len({card["id"] for card in cards}) == 14
    side_order = index["ordering"]["side"]
    deck_order = index["ordering"]["deck"]
    order = lambda card: (
        side_order.index(card["side"]), deck_order.index(card["deck"]),
        card["number"], card["id"],
    )
    assert cards == sorted(cards, key=order), "Pack ordering differs from manifest"
    for card in cards:
        validate_card(card, sources)
    events = [card for card in cards if card["kind"] == "Event"]
    assert len(events) == 13
    assert sum(card["side"] == "FreePeoples" for card in events) == 6
    assert sum(card["side"] == "Shadow" for card in events) == 7
    special = [card for card in cards if card["kind"] == "SpecialEvent"]
    assert len(special) == 1 and special[0]["id"] == "lords.sh.special.05"
    assert special[0]["printedNumber"] == "LM 5/5"
    assert special[0]["deck"] is None and special[0]["eventType"] == "Character"
    assert special[0]["setup"]["operation"] == "set-aside"
    assert special[0]["combatTitle"] is None and special[0]["combatRules"] == []
    assert special[0]["initiative"] == []
    return cards


def validate_card(card, sources):
    assert card["verification"] == "second_edition_front_reviewed"
    assert card["effectsVerified"] and card["reviewedAt"] and card["edition"]
    assert card["title"] and card["eventRules"] and card["verificationCases"]
    assert card["eventType"] in {"Character", "Army", "Muster"}
    source = sources[card["source"]["sheet"]]
    left, top, right, bottom = card["source"]["bounds"]
    assert 0 <= left < right <= source["width"]
    assert 0 <= top < bottom <= source["height"]
    assert len(source["sha256"]) == 64 and source["url"].startswith("https://")
    prefix = "fp" if card["side"] == "FreePeoples" else "sh"
    deck = card["deck"].lower() if card["deck"] else "special"
    assert card["id"] == f"lords.{prefix}.{deck}.{card['number']:02}"
    if card["kind"] == "Event":
        assert card["combatTitle"] and card["combatRules"]
        assert card["initiative"] and all(0 <= n <= 7 for n in card["initiative"])
        printed = "17*/24" if card["number"] == 17 else f"LM {card['number']}/24"
        assert card["printedNumber"] == printed


def validate_composition(index, cards):
    base = read("docs/sources/base-card-index.json")["cards"]
    decks = {(side, deck): {card["id"] for card in base
             if card["side"] == side and card["deck"] == deck}
             for side in ["FreePeoples", "Shadow"] for deck in ["Character", "Strategy"]}
    added, replaced = [], []
    for card in cards:
        setup = card["setup"]
        if setup["operation"] == "set-aside":
            continue
        deck = decks[(card["side"], card["deck"])]
        if setup["operation"] == "replace":
            assert setup["replaces"] in deck
            deck.remove(setup["replaces"])
            replaced.append((card["id"], setup["replaces"]))
        else:
            assert setup["operation"] == "add" and setup["replaces"] is None
            added.append(card)
        assert card["id"] not in deck
        deck.add(card["id"])
    assert replaced == [("lords.sh.character.17", "base.sh.character.17")]
    assert len(added) == index["composition"]["addedToDecks"] == 12
    sizes = {f"{'fp' if side == 'FreePeoples' else 'sh'}.{deck.lower()}": len(ids)
             for (side, deck), ids in decks.items()}
    assert sizes == index["composition"]["resultDeckSizes"]
    assert sizes == {"fp.character": 28, "fp.strategy": 26, "sh.character": 28, "sh.strategy": 26}
    combined = set().union(*decks.values())
    assert len(combined) == sum(sizes.values()) == index["composition"]["normalDeckTotal"] == 108
    assert "lords.sh.special.05" not in combined
    mumakil = next(card for card in cards if card["id"] == "lords.sh.strategy.25")
    assert mumakil["initiative"] == [3, 5]


def validate_packs(index, cards):
    tasks = {task["id"]: task for task in task_registry.records()}
    ids = [card["id"] for card in cards]
    assigned = []
    for position, pack in enumerate(index["packs"]):
        task = tasks[pack["task"]]
        expected = ids[position * 6:(position + 1) * 6]
        assert pack["task"] == f"T27-CARDS-{position + 1:02}"
        assert task["content_group"] == "lords"
        assert task["content_ids"] == pack["content_ids"] == expected
        assert task["content_count"] == len(expected)
        assigned.extend(expected)
    assert len(index["packs"]) == 3
    assert len(assigned) == len(set(assigned)) == 14 and assigned == ids


def image_dimensions(data):
    if data[:8] == b"\x89PNG\r\n\x1a\n":
        return struct.unpack(">II", data[16:24])
    assert data[:2] == b"\xff\xd8", "Expected JPEG or PNG source image"
    offset = 2
    while offset < len(data):
        assert data[offset] == 0xFF, "Malformed JPEG marker"
        while data[offset] == 0xFF:
            offset += 1
        marker = data[offset]
        offset += 1
        length = int.from_bytes(data[offset:offset + 2], "big")
        if 0xC0 <= marker <= 0xCF and marker not in {0xC4, 0xC8, 0xCC}:
            height, width = struct.unpack(">HH", data[offset + 3:offset + 7])
            return width, height
        assert length >= 2, "Malformed JPEG segment"
        offset += length
    raise ValueError("JPEG source has no frame dimensions")


def fetch(cards, sources):
    cache = ROOT / ".artifacts/lords-card-sources"
    cache.mkdir(parents=True, exist_ok=True)
    for source_id in sorted({card["source"]["sheet"] for card in cards}):
        source = sources[source_id]
        path = cache / (source_id + ".jpg")
        if path.exists():
            data = path.read_bytes()
        else:
            with urlopen(source["url"], timeout=45) as response:
                data = response.read()
        assert hashlib.sha256(data).hexdigest() == source["sha256"], f"Changed source: {source_id}"
        assert image_dimensions(data) == (source["width"], source["height"])
        if not path.exists():
            path.write_bytes(data)
        print(f"PASS: {source_id}, {len(data)} bytes, SHA-256 and image dimensions")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fetch", action="store_true", help="Fetch five pinned front sheets into ignored cache")
    args = parser.parse_args()
    index = read(INDEX_PATH)
    sources = {source["id"]: source for source in read("docs/sources/manifest.json")["sources"]}
    cards = validate_cards(index, sources)
    validate_composition(index, cards)
    validate_packs(index, cards)
    print("PASS: 13 event fronts + 1 special, both uses, 108 deck cards, one replacement, disjoint packs 6+6+2.")
    if args.fetch:
        fetch(cards, sources)


if __name__ == "__main__":
    main()
