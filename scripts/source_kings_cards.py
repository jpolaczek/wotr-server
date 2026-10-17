#!/usr/bin/env python3
"""Check reviewed Kings event fronts, deck composition and source integration."""
import task_registry
import argparse
import hashlib
import json
from pathlib import Path
from urllib.request import urlopen

from source_lords_cards import image_dimensions

ROOT = Path(__file__).resolve().parents[1]
INDEX = "docs/sources/kings-card-index.json"
SHEET = "KINGS-CARD-SHEET-C906DF19"
REPLACEMENTS = {
    "fp.character": {23},
    "fp.strategy": {8, 16, 19, 22, 24},
    "sh.character": set(),
    "sh.strategy": {1, 3, 5, 6, 18},
}


def read(path):
    return json.loads((ROOT / path).read_text(encoding="utf-8"))


def deck_key(card):
    side = "fp" if card["side"] == "FreePeoples" else "sh"
    return f"{side}.{card['deck'].lower()}"


def validate_card(card, source):
    assert card["kind"] == "Event"
    assert card["id"] == f"kings.{deck_key(card)}.{card['number']:02}"
    assert card["verification"] == "second_edition_front_reviewed"
    assert card["effectsVerified"] and card["reviewedAt"] and card["edition"]
    for field in ("title", "eventRules", "combatTitle", "combatRules", "verificationCases"):
        assert card[field], f"Missing {field}: {card['id']}"
    assert card["eventType"] in {"Character", "Army", "Muster"}
    assert len(card["initiative"]) == 1 and 0 <= card["initiative"][0] <= 7
    assert card["source"]["sheet"] == source["id"] == SHEET
    left, top, right, bottom = card["source"]["bounds"]
    assert 0 <= left < right <= source["width"]
    assert 0 <= top < bottom <= source["height"]
    assert (right - left, bottom - top) == (332, 600)
    assert left % 332 == top % 600 == 0
    replacement = card["number"] in REPLACEMENTS[deck_key(card)]
    expected = f"{card['number']}*/24" if replacement else f"KM {card['number']}/24"
    assert card["printedNumber"] == expected
    assert card["setup"]["operation"] == ("replace" if replacement else "add")
    target = card["id"].replace("kings.", "base.") if replacement else None
    assert card["setup"]["replaces"] == target
    if not replacement:
        assert card["number"] in {25, 26}


def validate_cards(index, sources):
    cards = index["cards"]
    assert len(cards) == len({card["id"] for card in cards}) == 19
    assert sum(card["side"] == "FreePeoples" for card in cards) == 10
    assert sum(card["side"] == "Shadow" for card in cards) == 9
    ordering = index["ordering"]
    order = lambda card: (
        ordering["side"].index(card["side"]), ordering["deck"].index(card["deck"]),
        card["number"], card["id"],
    )
    assert cards == sorted(cards, key=order)
    source = sources[SHEET]
    assert source["sha256"] == "e4139bd403a61853ad0c50a6bf3aa16aef7171d26143c6e8df315632e2e718c0"
    assert (source["width"], source["height"], source["bytes"]) == (3320, 2400, 3632090)
    for card in cards:
        validate_card(card, source)
    by_id = {card["id"]: card for card in cards}
    assert by_id["kings.sh.strategy.06"]["initiative"] == [7], "Stormcrow locator has an incorrect initiative"
    excluded = index["excludedFronts"]
    assert {card["printedNumber"] for card in excluded} == {"KM PROMO 1", "KM PROMO 2"}
    bounds = [tuple(card["source"]["bounds"]) for card in cards + excluded]
    assert len(set(bounds)) == 21, "A front was included twice or a promo entered a deck"
    fate_excluded = [card for card in cards if card["setup"].get("excludedWith")]
    assert [card["id"] for card in fate_excluded] == ["kings.fp.strategy.19", "kings.fp.strategy.22"]
    for card in fate_excluded:
        assert card["setup"]["excludedWith"] == ["FateOfErebor"]
        assert card["setup"]["exceptionSource"] == {"source": "KINGS", "page": 18}
    return cards


def add_expansion(decks, cards):
    for card in cards:
        operation = card["setup"]["operation"]
        if operation == "set-aside":
            continue
        deck = decks[deck_key(card)]
        if operation == "replace":
            target = card["setup"]["replaces"]
            assert target in deck, f"Replacement target missing: {target}"
            deck.remove(target)
        else:
            assert operation == "add"
        assert card["id"] not in deck
        deck.add(card["id"])


def validate_composition(index, cards):
    base = read("docs/sources/base-card-index.json")["cards"]
    decks = {key: {card["id"] for card in base if deck_key(card) == key}
             for key in REPLACEMENTS}
    assert all(len(deck) == 24 for deck in decks.values())
    replacement_ids = {card["id"] for card in cards if card["setup"]["operation"] == "replace"}
    expected = {f"kings.{key}.{number:02}" for key, numbers in REPLACEMENTS.items() for number in numbers}
    assert replacement_ids == expected and len(replacement_ids) == 11
    composition = index["composition"]
    assert composition["regularEvents"] == 19
    assert composition["freePeoples"] == 10 and composition["shadow"] == 9
    assert composition["replacedBaseCards"] == 11 and composition["addedToDecks"] == 8
    add_expansion(decks, cards)
    assert {key: len(deck) for key, deck in decks.items()} == composition["resultDeckSizes"]
    assert all(len(deck) == 26 for deck in decks.values())
    assert len(set().union(*decks.values())) == composition["normalDeckTotal"] == 104
    add_expansion(decks, read("docs/sources/lords-card-index.json")["cards"])
    expected_sizes = {"fp.character": 30, "fp.strategy": 28, "sh.character": 30, "sh.strategy": 28}
    assert {key: len(deck) for key, deck in decks.items()} == composition["withLordsDeckSizes"] == expected_sizes
    assert len(set().union(*decks.values())) == composition["withLordsNormalDeckTotal"] == 116


def validate_packs(index, cards, index_only):
    tasks = {task["id"]: task for task in task_registry.records()}
    ids = [card["id"] for card in cards]
    assigned = []
    assert len(index["packs"]) == 4
    for position, pack in enumerate(index["packs"]):
        expected = ids[position * 6:(position + 1) * 6]
        assert pack["task"] == f"T30-CARDS-{position + 1:02}"
        assert pack["content_ids"] == expected
        task = tasks[pack["task"]]
        assert task["content_count"] == len(expected) and task["content_group"] == "kings"
        if not index_only:
            assert task["content_ids"] == expected, f"Task assignment not integrated: {pack['task']}"
            assert INDEX in task["read_before"] and index["audit"] in task["read_before"]
        assigned.extend(expected)
    assert assigned == ids and len(set(assigned)) == 19


def validate_integration(index):
    gap = next(gap for gap in read("docs/sources/gaps.json")["gaps"] if gap["id"] == "GAP-KINGS-CARDS")
    assert gap["status"] == "resolved" and gap["resolution"]["evidence"] == index["audit"]
    rules = {rule["id"]: rule for rule in read("docs/rules/catalog.json")["rules"]}
    for rule_id in ("K02", "K03"):
        review = rules[rule_id]["card_source_review"]
        assert review["index"] == INDEX and review["document"] == index["audit"]
        assert "GAP-KINGS-CARDS" not in rules[rule_id]["unresolved"]


def fetch(source):
    cache = ROOT / ".artifacts/kings-card-sources"
    cache.mkdir(parents=True, exist_ok=True)
    path = cache / f"{SHEET}.jpg"
    if path.exists():
        data = path.read_bytes()
    else:
        with urlopen(source["url"], timeout=45) as response:
            data = response.read()
    assert len(data) == source["bytes"]
    assert hashlib.sha256(data).hexdigest() == source["sha256"], "Kings source bytes changed"
    assert image_dimensions(data) == (source["width"], source["height"])
    if not path.exists():
        path.write_bytes(data)
    print(f"PASS: {SHEET}, {len(data)} bytes, SHA-256 and image dimensions")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fetch", action="store_true", help="Fetch and verify the pinned Kings sheet")
    parser.add_argument("--index-only", action="store_true", help="Check the index before shared task/gap/catalog integration")
    args = parser.parse_args()
    index = read(INDEX)
    sources = {source["id"]: source for source in read("docs/sources/manifest.json")["sources"]}
    cards = validate_cards(index, sources)
    validate_composition(index, cards)
    validate_packs(index, cards, args.index_only)
    if args.index_only:
        print("NOTE: task assignments and resolved source gate not checked (--index-only).")
    else:
        validate_integration(index)
    print("PASS: 19 event/combat fronts, 11 replacements + 8 additions, 104/116 deck cards, packs 6+6+6+1, 2 excluded promos.")
    if args.fetch:
        fetch(sources[SHEET])


if __name__ == "__main__":
    main()
