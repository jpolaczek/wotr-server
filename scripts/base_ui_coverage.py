#!/usr/bin/env python3
"""Generate the base-game UI coverage matrix (UI-244) into docs/ui/interaction-inventory.json.

Usage: python3 scripts/base_ui_coverage.py [--check]

Reads the base implementation manifests (content/implementation-manifest.json and
content/character-implementation-manifest.json) and the card programs in src/Wotr.Domain: each program's steps, and
for each choice step its presentation. Every Event and Combat use of the 96 base cards and every base character
entry/ability maps to a decision family; FAMILIES records the family's current host, target host, choice objects,
cost, commit gesture, exception reason and implementation owner (the UI-244 plan). The result replaces the
`base_coverage` section of the inventory. --check fails when the section is stale or a use has no family.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOMAIN = ROOT / "src/Wotr.Domain"
INVENTORY = ROOT / "docs/ui/interaction-inventory.json"
CARDS = ROOT / "content/implementation-manifest.json"
CHARACTERS = ROOT / "content/character-implementation-manifest.json"

# host: where the choice is made today; target: where UI-244..251 put it; objects: what the player clicks;
# commit: the gesture that sends it; owner: the task that implements the target (baseline = already there).
FAMILIES = {
    "Automatic": dict(host="none", target="none", objects="none", cost="the card's own cost",
                      commit="none: the server resolves it", owner="baseline", exception=""),
    "TableEffect": dict(host="Effects panel", target="Effects panel", objects="table card",
                        commit="none: the card stays on the table", owner="baseline", exception=""),
    "Region": dict(host="map highlights + console", target="map regions", objects="highlighted region",
                   cost="shown in the console before the click", commit="final region click", owner="baseline (UI-211)",
                   exception=""),
    "RegionArmy": dict(host="region panel", target="region panel field/garrison rows", objects="army row or figure",
                       cost="none", commit="final row click", owner="UI-251", exception=""),
    "ArmyFigures": dict(host="region panel", target="region panel figures", objects="figures",
                        cost="none", commit="one confirmation for a set", owner="baseline (UI-215)", exception=""),
    "EventLosses": dict(host="casualty tray", target="region panel figures", objects="figures to remove",
                        cost="the losses", commit="one confirmation", owner="UI-251", exception=""),
    "ArmyEvent": dict(host="map + console", target="map", objects="armies and regions", cost="none",
                      commit="final move click, Skip maneuver", owner="baseline (UI-216)",
                      exception="Skip maneuver ends an optional army event"),
    "Reserve": dict(host="Reserve panel", target="Reserve panel", objects="reserve figures and settlements",
                    cost="recruitment points", commit="final settlement click", owner="baseline (UI-213)", exception=""),
    "TableCards": dict(host="Effects panel", target="Effects panel", objects="table cards", cost="none",
                       commit="card click", owner="baseline (UI-214)", exception=""),
    "Deck": dict(host="card decks", target="card decks", objects="deck", cost="none", commit="deck click",
                 owner="baseline", exception=""),
    "Nation": dict(host="politics panel", target="politics panel", objects="nation", cost="none",
                   commit="nation click", owner="baseline", exception=""),
    "DiceChoice": dict(host="dice pool + console", target="dice pool", objects="action dice", cost="the dice",
                       commit="die click; a set has one confirmation", owner="UI-248", exception=""),
    "RingChoice": dict(host="Ring sockets + picker", target="Ring sockets", objects="Elven Ring, die, face",
                       cost="the Ring", commit="face click", owner="baseline (UI-161, UI-238)",
                       exception="the die-face picker stays"),
    "HandChoice": dict(host="decision window list", target="hand", objects="hand cards", cost="the cards",
                       commit="card click; a set has one confirmation", owner="UI-248", exception=""),
    "CombatTarget": dict(host="Battle window buttons", target="Battle window dice and figures",
                         objects="battle dice, force portraits", cost="shown before the final click",
                         commit="final target click; an adjustable set has one confirmation", owner="UI-245",
                         exception="Keep misses stays a paid outcome"),
    "CombatLeadership": dict(host="Battle window buttons", target="Battle window figures and hit indicators",
                             objects="Leadership sources, sacrificed figure, hits", cost="exact points",
                             commit="one confirmation", owner="UI-246", exception="Keep all stays explicit"),
    "Reaction": dict(host="console options / decision window", target="source card or portrait",
                     objects="reaction source", cost="the source card", commit="source click; Decline stays",
                     owner="UI-247", exception="Decline stays explicit"),
    "FellowshipChoice": dict(host="decision window list", target="Fellowship panel portraits and tracks",
                             objects="companions, Guide, corruption", cost="none", commit="portrait click",
                             owner="UI-249", exception=""),
    "CompanionJourney": dict(host="decision window groups", target="map groups + shared console",
                             objects="companion groups and regions", cost="the die", commit="one confirmation",
                             owner="UI-250", exception=""),
    "Alternatives": dict(host="console options", target="console options", objects="named outcomes",
                         cost="per outcome", commit="option click", owner="baseline (UI-214)",
                         exception="distinct outcomes without one object (Lure of the Ring, Black Captain, Balrog)"),
    "HiddenHand": dict(host="console options", target="console options", objects="random discard", cost="none",
                       commit="option click", owner="baseline (UI-214)",
                       exception="an enemy hand stays hidden; the discard is random"),
    "CharacterEntry": dict(host="Characters list", target="Characters list + map regions",
                           objects="character row, region", cost="the die", commit="final region click",
                           owner="baseline (UI-239, UI-240, UI-243)", exception=""),
    "CharacterAbility": dict(host="region panel / Characters list", target="character figure or portrait",
                             objects="character", cost="per ability", commit="final click",
                             owner="UI-247", exception=""),
    "Generic": dict(host="decision window list", target="decision window list", objects="listed choices",
                    cost="per choice", commit="choice click", owner="UI-244 fallback",
                    exception="no object owns the choice yet"),
}

PRESENTATIONS = {
    "Region": "Region", "RegionOrShelter": "Region", "ArmyFigures": "ArmyFigures", "ArmyFigure": "RegionArmy",
    "ReserveFigures": "Reserve", "RecruitmentPlan": "Reserve", "TableCards": "TableCards", "Deck": "Deck",
    "Nation": "Nation", "ActionDie": "DiceChoice", "DieAndResult": "DiceChoice", "ElvenRings": "RingChoice",
    "CombatReplacement": "CombatTarget", "CombatEffect": "CombatTarget", "ConvertMisses": "CombatTarget",
    "LeadershipAndMiss": "CombatTarget", "NazgulOrMinion": "CombatTarget", "LeaderOrCompanion": "CombatTarget",
    "Minion": "CombatTarget", "NazgulStrike": "Alternatives", "CharacterLeadership": "CombatLeadership",
    "NazgulLeadership": "CombatLeadership", "MinionLeadership": "CombatLeadership",
    "SacrificeAndHits": "CombatLeadership", "FellowshipCompanions": "FellowshipChoice",
    "FellowshipAction": "Alternatives", "Companion": "FellowshipChoice", "CompanionDestination": "CompanionJourney",
    "CorruptionRemoval": "FellowshipChoice", "SmeagolCorruption": "FellowshipChoice",
    "InspectedHand": "HiddenHand", "InspectedCorruption": "FellowshipChoice", "CharacterCardDiscard": "HandChoice",
    "LureCompanion": "Alternatives", "CaptainOrder": "Alternatives", "BalrogOrder": "Alternatives",
    "EffectTargets": "Generic",
}

# Steps without a presentation that still open a decision of their own, by name.
STEP_PATTERNS = [
    (re.compile(r"Loss|Casualt|Eliminat"), "EventLosses"),
    (re.compile(r"Recruit|Muster"), "Reserve"),
    (re.compile(r"Journey|CompanionMove|Separate"), "CompanionJourney"),
    (re.compile(r"Army|March|Attack|Maneuver"), "ArmyEvent"),
    (re.compile(r"Discard.*Hand|Hand.*Discard"), "HandChoice"),
    (re.compile(r"Lasting|Table"), "TableEffect"),
]


def domain_sources():
    return {path: path.read_text(encoding="utf-8") for path in sorted(DOMAIN.rglob("*.cs"))}


def step_classes(sources):
    declaration = re.compile(r"(?:record|class)\s+([A-Za-z]+Step)\b[^{;]*?:\s*([A-Za-z]+)")
    classes = {}
    for text in sources.values():
        for match in declaration.finditer(text):
            start = text.find("{", match.end())
            body = text[start:start + 4000] if start >= 0 else ""
            presentation = re.search(r'Presentation\s*=>\s*"(\w+)"', body)
            classes[match.group(1)] = dict(base=match.group(2), presentation=presentation.group(1) if presentation else None)
    return classes


def programs(sources):
    constants = {}
    for text in sources.values():
        for match in re.finditer(r'const string (\w+)\s*=\s*"([^"]+)"', text):
            constants.setdefault(match.group(1), match.group(2))
    definition = re.compile(r'new\(\s*("([^"]+)"|[A-Za-z_.]+)\s*,\s*\[(.*?)\]\s*\)', re.S)
    found = {}
    for text in sources.values():
        if "EffectDefinition" not in text:
            continue
        for match in definition.finditer(text):
            program = match.group(2) or constants.get(match.group(1).split(".")[-1])
            if not program:
                continue
            body = match.group(3)
            steps = re.findall(r"new\s+([A-Za-z]+Step)\b", body)
            helpers = re.findall(r"\b([A-Z][A-Za-z]+)\(", body)
            found.setdefault(program, dict(steps=steps, helpers=[h for h in helpers if not h.endswith("Step")]))
    return found


# A base character's ability: a passive one needs no choice; the others keep their object (UI-247 owns reactions).
ABILITIES = [
    (re.compile(r"^combat-|leadership$|^cannot-|^ignore-|^isengard-|^entry-activates|^draw-after|^add-gollum"), "Automatic"),
    (re.compile(r"^may-transform"), "CharacterEntry"),
    (re.compile(r"^guide-separate|^voluntary-reveal"), "Reaction"),
    (re.compile(r"^guide-hide|^guide-draw"), "Automatic"),
    (re.compile(r"^move-"), "CompanionJourney"),
    (re.compile(r"^politics-"), "Nation"),
    (re.compile(r"^muster-"), "Reserve"),
    (re.compile(r"^forfeit-leadership"), "CombatLeadership"),
    (re.compile(r"^return-after|^enter-and-guide"), "FellowshipChoice"),
]


def ability_family(ability):
    for pattern, family in ABILITIES:
        if pattern.search(ability):
            return family
    return "CharacterAbility"


def step_family(step, classes):
    info = classes.get(step, {})
    if info.get("presentation"):
        return PRESENTATIONS.get(info["presentation"], "Generic")
    for pattern, family in STEP_PATTERNS:
        if pattern.search(step):
            return family
    return "Automatic"


def families_of(program, catalog, classes):
    entry = catalog.get(program)
    if entry is None:
        return ["Automatic"]
    found = [step_family(step, classes) for step in entry["steps"]]
    if not found and entry["helpers"]:
        found = ["TableEffect"]
    distinct = []
    for family in found or ["Automatic"]:
        if family not in distinct:
            distinct.append(family)
    if len(distinct) > 1 and "Automatic" in distinct:
        distinct.remove("Automatic")
    return distinct


# Base card reactions: new(<reaction>, "<window>", <program>, <condition>, <source card>, ReactionCost...) in the base
# card files (expansion folders excluded).
def reactions(sources, constants):
    pattern = re.compile(r'new\(\s*([A-Za-z_.]+)(?:\s*\+\s*"[^"]*")?\s*,\s*"([a-z.]+)"\s*,\s*([A-Za-z_.]+)\s*,\s*new [A-Za-z]+\([^)]*\)\s*,\s*([A-Za-z_.]+)')
    found = []
    for path, text in sources.items():
        if "/Cards/" not in str(path) or "Lords" in str(path) or "Kings" in str(path) or "Fate" in str(path):
            continue
        for match in pattern.finditer(text):
            card = constants.get(match.group(4).split(".")[-1], "")
            if card.startswith("base."):
                found.append(dict(source=card, use="Reaction", program=constants.get(match.group(1), match.group(1)) + " @ " + match.group(2),
                                  families=["Reaction"]))
    return found


def constants_of(sources):
    found = {}
    for text in sources.values():
        for match in re.finditer(r'const string (\w+)\s*=\s*"([^"]+)"', text):
            found.setdefault(match.group(1), match.group(2))
    return found


def coverage():
    sources = domain_sources()
    classes = step_classes(sources)
    catalog = programs(sources)
    uses = []
    for card in json.loads(CARDS.read_text())["cards"]:
        uses.append(dict(source=card["id"], use="Event", program=card["programId"],
                         families=families_of(card["programId"], catalog, classes)))
        for program in card.get("combatPrograms", []):
            uses.append(dict(source=card["id"], use="Combat", program=program,
                             families=families_of(program, catalog, classes)))
    uses.extend(reactions(sources, constants_of(sources)))
    for character in json.loads(CHARACTERS.read_text())["cards"]:
        identifier = character["id"]
        uses.append(dict(source=identifier, use="Entry", program="", families=["CharacterEntry"]))
        for ability in character.get("abilities", []):
            uses.append(dict(source=identifier, use="Ability", program=ability["id"],
                             families=[ability_family(ability["id"])]))
    owners = {}
    for use in uses:
        for family in use["families"]:
            owners.setdefault(FAMILIES[family]["owner"], set()).add(family)
    return dict(
        task="UI-244",
        generator="scripts/base_ui_coverage.py",
        scope="Second Edition base game: both uses of the 96 base cards and the base characters' entries and abilities",
        families={name: FAMILIES[name] for name in sorted({f for u in uses for f in u["families"]})},
        uses=uses,
        owners={owner: sorted(names) for owner, names in sorted(owners.items())})


def main():
    inventory = json.loads(INVENTORY.read_text(encoding="utf-8"))
    generated = coverage()
    unknown = [u for u in generated["uses"] if any(f not in FAMILIES for f in u["families"])]
    if unknown:
        sys.exit("Uses without a family: " + ", ".join(u["source"] for u in unknown))
    if "--check" in sys.argv:
        if inventory.get("base_coverage") != generated:
            sys.exit("docs/ui/interaction-inventory.json base_coverage is stale; run scripts/base_ui_coverage.py")
        print(f"PASS: {len(generated['uses'])} base uses in {len(generated['families'])} families")
        return
    inventory["base_coverage"] = generated
    INVENTORY.write_text(json.dumps(inventory, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    counts = {}
    for use in generated["uses"]:
        for family in use["families"]:
            counts[family] = counts.get(family, 0) + 1
    print(f"{len(generated['uses'])} base uses: " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())))


if __name__ == "__main__":
    main()
