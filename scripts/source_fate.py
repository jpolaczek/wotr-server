#!/usr/bin/env python3
"""Verify the reviewed Fate sources, replacements and setup; optionally fetch images."""
import task_registry
import argparse
from collections import Counter
import hashlib
from itertools import product
import json
from pathlib import Path
from urllib.request import urlopen

from source_lords_cards import image_dimensions


ROOT = Path(__file__).resolve().parents[1]
INDEX = "docs/sources/fate-index.json"


def read(path):
    return json.loads((ROOT / path).read_text(encoding="utf-8"))


def verify_sources(index, fetch):
    sources = {source["id"]: source for source in index["sourceDocuments"]}
    assert len(sources) == 6
    assert {source["printedPage"] for source in sources.values()} == {44, 45, 46, None}
    for source in sources.values():
        path = ROOT / source["artifact"]
        assert path.is_relative_to(ROOT / ".artifacts/source-audit/fate")
        if fetch:
            data = urlopen(source["url"], timeout=45).read()
            assert hashlib.sha256(data).hexdigest() == source["sha256"], source["id"]
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        data = path.read_bytes()
        assert hashlib.sha256(data).hexdigest() == source["sha256"], source["id"]
        assert len(data) == source["bytes"]
        assert image_dimensions(data) == (source["width"], source["height"])
        assert source["status"] == "reviewed_reproduction"
    return sources


def verify_editions(index, sources):
    comparison = index["editionComparison"]
    assert comparison["standaloneRules"] == "FATE-WOTR018-RULES"
    assert comparison["standaloneIdentity"] == "FATE-WOTR018-PACKAGE"
    for key in ["standaloneRules", "standaloneIdentity", "cardFronts"]:
        assert comparison[key] in sources
    assert comparison["standaloneProduct"] == "WOTR018"
    assert comparison["standaloneCopyright"] == "2011, 2021"
    assert comparison["standalonePhotoDate"] == "2022-04-28"
    assert len(comparison["equalScopes"]) == 6
    assert comparison["differences"] and comparison["limitation"]
    assert comparison["kingsIntegration"]["pages"] == [18, 19]


def verify_fronts(index, sources):
    cards = index["cards"]
    characters = index["characters"]
    all_fronts = cards + characters
    assert len(cards) == 6 and len(characters) == 2
    assert len({card["id"] for card in all_fronts}) == 8
    assert len({tuple(card["source"]["bounds"]) for card in all_fronts}) == 8
    for card in all_fronts:
        assert card["verification"] == "second_edition_front_reviewed"
        assert card["effectsVerified"] and card["verificationCases"]
        assert card["reviewedAt"] and card["edition"]
        source = sources[card["source"]["sheet"]]
        left, top, right, bottom = card["source"]["bounds"]
        assert 0 <= left < right <= source["width"]
        assert 0 <= top < bottom <= source["height"]
        assert (right - left, bottom - top) == (415, 750)
        assert card["setup"]["operation"] == "replace"
    for card in cards:
        assert card["eventRules"] and card["combatRules"]
        assert card["eventType"] in {"Character", "Muster"}
        assert card["printedNumber"] == f"{card['number']}/24 FE"
        assert len(card["initiative"]) == 1 and 0 <= card["initiative"][0] <= 7
    assert Counter(card["side"] for card in cards) == {"FreePeoples": 4, "Shadow": 2}
    expected = {"base.fp.character.17", "base.fp.strategy.04", "base.fp.strategy.19",
                "base.fp.strategy.22", "base.sh.strategy.20", "base.sh.strategy.22"}
    assert {card["setup"]["replaces"] for card in cards} == expected
    return cards, characters


def verify_characters(index, characters):
    assert {card["identity"] for card in characters} == {"gimli"}
    by_id = {card["id"]: card for card in characters}
    ordinary = by_id["fate.character.gimli"]
    alternative = by_id["fate.character.gimli-dwarven-lord"]
    assert (ordinary["level"], ordinary["leadership"], ordinary["nationIcon"]) == (2, 1, "dwarves")
    assert ordinary["setup"]["replaces"] == "base.character.gimli"
    assert alternative["setup"]["replaces"] == "lords.character.gimli"
    assert (alternative["level"], alternative["leadership"], alternative["nationIcon"]) == (1, 1, None)
    assert alternative["movementLevel"] == 2 and alternative["requiredVariant"] == "CouncilOfRivendell"
    assert set(alternative["councilStart"]) == {"iron-hills", "ered-luin"}
    kings = index["integration"]["kings"]
    assert kings["source"] == "KINGS" and kings["pages"] == [18, 19]
    assert set(kings["omitSovereignIdentities"]) == {"dain", "brand"}
    kings_index = read("docs/sources/kings-character-index.json")
    kings_ids = {card["id"] for card in kings_index["cards"]}
    assert set(kings["omitSovereignCards"]) <= kings_ids


def apply_cards(decks, cards, excluded_slots=()):
    for card in cards:
        setup = card["setup"]
        if setup["operation"] == "set-aside" or setup["replaces"] in excluded_slots:
            continue
        deck = decks[card["side"], card["deck"]]
        if setup["operation"] == "replace":
            assert setup["replaces"] in deck, card["id"]
            deck.remove(setup["replaces"])
        else:
            assert setup["operation"] == "add"
        assert card["id"] not in deck
        deck.add(card["id"])


def verify_compositions(index):
    base = read("docs/sources/base-card-index.json")["cards"]
    lords = read("docs/sources/lords-card-index.json")["cards"]
    kings = read("docs/sources/kings-card-index.json")["cards"]
    fate = index["cards"]
    skipped = set(index["integration"]["kings"]["skipKingsReplacementSlots"])
    assert skipped == {"base.fp.strategy.19", "base.fp.strategy.22"}
    assert {card["setup"]["replaces"] for card in kings
            if "FateOfErebor" in card["setup"].get("excludedWith", [])} == skipped
    for with_lords, with_kings, with_fate in product([False, True], repeat=3):
        decks = {(side, deck): {card["id"] for card in base if card["side"] == side and card["deck"] == deck}
                 for side in ["FreePeoples", "Shadow"] for deck in ["Character", "Strategy"]}
        if with_lords:
            apply_cards(decks, lords)
        if with_kings:
            apply_cards(decks, kings, skipped if with_fate else ())
        if with_fate:
            apply_cards(decks, fate)
        all_ids = set().union(*decks.values())
        assert len(all_ids) == sum(map(len, decks.values())) == 96 + 12 * with_lords + 8 * with_kings
        for card in fate:
            assert (card["id"] in all_ids) == with_fate
        if with_fate:
            assert "kings.fp.strategy.19" not in all_ids and "kings.fp.strategy.22" not in all_ids


def verify_setup(index):
    board = read("content/base-board.json")
    regions = {region["id"]: region for region in board["regions"]}
    expected = {("erebor", "dwarves"): (0, 0, 0), ("erebor", "sauron"): (2, 0, 0),
                ("dale", "north"): (0, 0, 0), ("carrock", "north"): (2, 0, 1),
                ("ered-luin", "dwarves"): (2, 0, 0), ("iron-hills", "dwarves"): (2, 0, 1)}
    totals = {(row["region"], row["nation"]): tuple(row[kind] for kind in ["Regular", "Elite", "Leader"])
              for row in index["setup"]["totals"]}
    assert totals == expected
    assert index["setup"]["nations"] == [{"id": "dwarves", "active": True, "stepsToWar": "unchanged"}]
    overrides = {row["id"]: row for row in index["regionOverrides"]}
    assert set(overrides) == {"erebor", "dale", "iron-hills"}
    assert overrides["erebor"]["nation"] == "sauron" and overrides["erebor"]["originalOwner"] == "Shadow"
    assert overrides["dale"]["settlement"] == "Fortification" and overrides["dale"]["nation"] == ""
    assert overrides["iron-hills"]["settlement"] == "City"
    assert index["mapRules"]["initialShadowVictoryPointsFromErebor"] == 0
    assert index["mapRules"]["freePeoplesVictoryPointsForTakingErebor"] == 2
    assert index["mapRules"]["adjacencyUnchanged"] and index["mapRules"]["impassableBordersUnchanged"]
    for row in index["newCities"]["regionOverrides"]:
        original = regions[row["id"]]
        assert original["settlement"] == "Town" and row["settlement"] == "City"
        assert row["nation"] == original["nation"] and row["originalOwner"] == original["originalOwner"]
    assert {row["id"] for row in index["newCities"]["regionOverrides"]} == {"ered-luin", "south-rhun"}
    assert index["newCities"]["requiresFate"]


def verify_registration(index, registered):
    expected = {"T32-CARDS": [card["id"] for card in index["cards"]],
                "T32-GIMLI": [card["id"] for card in index["characters"]]}
    assert {pack["task"]: pack["content_ids"] for pack in index["packs"]} == expected
    if not registered:
        return
    tasks = {task["id"]: task for task in task_registry.records()}
    for task_id, ids in expected.items():
        assert tasks[task_id]["content_ids"] == ids and tasks[task_id]["content_count"] == len(ids)
    sources = {source["id"]: source for source in read("docs/sources/manifest.json")["sources"]}
    for source in index["sourceDocuments"]:
        assert sources[source["id"]]["sha256"] == source["sha256"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fetch", action="store_true", help="Refetch the six pinned images before verifying hashes")
    parser.add_argument("--registered", action="store_true", help="Also require shared manifest and task ID registration")
    args = parser.parse_args()
    index = read(INDEX)
    sources = verify_sources(index, args.fetch)
    verify_editions(index, sources)
    _, characters = verify_fronts(index, sources)
    verify_characters(index, characters)
    verify_compositions(index)
    verify_setup(index)
    verify_registration(index, args.registered)
    print("PASS: 6 pinned sources; Collector/standalone edition evidence; 6 event fronts/combat effects; "
          "2 Gimli versions; 6 setup totals; 8 deck configurations; New Cities.")
    print("Shared registration: " + ("verified" if args.registered else "not checked (use --registered after coordinator integration)"))


if __name__ == "__main__":
    main()
