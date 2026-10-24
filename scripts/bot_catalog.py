#!/usr/bin/env python3
"""Public mechanic inventory only. No private strategy implementation or acceptance evidence."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = Path("docs/bots/runtime-catalog.json")
LEDGER = Path("docs/bots/coverage.json")
MANIFESTS = sorted(p.relative_to(ROOT) for p in (ROOT / "content").glob("*implementation-manifest.json"))
MODULES = {
    "base": ("BOT-08", ["B06", "B11"]),
    "lords": ("BOT-10", ["L01", "L02", "L03"]),
    "kings": ("BOT-11", ["K01", "K02", "K03", "X01", "X02"]),
    "fate": ("BOT-09", ["F01", "X03"]),
}


def read(path, root=ROOT):
    return json.loads((root / path).read_text())


def runtime_digest(root=ROOT):
    """Changing compiled rule/contract/content inputs requires re-export through the engine."""
    paths = []
    for folder in ("src/Wotr.Domain", "src/Wotr.Contracts"):
        paths.extend(p for p in (root / folder).rglob("*") if p.suffix in (".cs", ".csproj")
                     and not {"bin", "obj"}.intersection(p.parts))
    paths.extend(p for p in (root / "content").glob("*.json") if p.name != "rules-package.json")
    paths.extend((root / "tools/Wotr.BotCatalog").glob("*.cs"))
    digest = hashlib.sha256()
    for path in sorted(set(paths)):
        digest.update(str(path.relative_to(root)).encode() + b"\0" + path.read_bytes() + b"\0")
    return digest.hexdigest()


def source_coverage(runtime, root=ROOT):
    """Manifests cannot quietly omit newly compiled cards, characters or abilities."""
    cards, characters, tokens = {}, {}, set()
    errors = []
    for path in MANIFESTS:
        manifest = read(path, root)
        tokens.update(token["id"] for token in manifest.get("tokens", []))
        for entry in manifest["cards"]:
            target = cards if "eventImplemented" in entry else characters
            if entry["id"] in target:
                errors.append("Duplicate content manifest entry: " + entry["id"])
            target[entry["id"]] = entry
    for name, entries in (("cards", cards), ("characters", characters)):
        compiled = {entry["id"]: entry for entry in runtime[name]}
        if set(compiled) != set(entries):
            errors.append("Compiled content/manifest mismatch: " + name)
        if name == "characters":
            for identity in compiled.keys() & entries.keys():
                declared = {ability["id"] for ability in entries[identity]["abilities"]}
                if set(compiled[identity]["abilities"]) != declared:
                    errors.append("Compiled ability/manifest mismatch: " + identity)
    return errors, set(cards) | set(characters) | tokens


def write_catalog(path, document):
    """One record per line keeps generated inventories diffable without thousands of wrapper lines."""
    lines = ["{"]
    for index, (key, value) in enumerate(document.items()):
        prefix = "  " + json.dumps(key) + ": "
        suffix = "," if index + 1 < len(document) else ""
        if isinstance(value, list):
            lines.append(prefix + "[")
            lines.extend("    " + json.dumps(row, ensure_ascii=False, sort_keys=True) +
                         ("," if i + 1 < len(value) else "") for i, row in enumerate(value))
            lines.append("  ]" + suffix)
        else:
            lines.append(prefix + json.dumps(value, ensure_ascii=False, sort_keys=True) + suffix)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(lines + ["}"]) + "\n")


def inventory(runtime, root=ROOT):
    rows = []
    scenarios = []
    rules = read("docs/rules/catalog.json", root)["rules"]
    decision_spec = read("docs/rules/decisions.json", root)

    def add(identity, kind, source, *, rule_ids, owner="BOT-03", faction="Both", content=None,
            predicate="Legal configuration; advertised owner decision/options", mode="choice", tests=()):
        positive = identity + ":useful"
        negative = identity + ":retain-or-reject"
        rows.append({
            "id": identity, "kind": kind, "source": source, "rules": rule_ids,
            "content": content, "faction": faction, "configuration": predicate, "owner": owner,
            "generator": "none:automatic" if mode == "automatic" else "planned:" + owner + ":" + identity,
            "evaluator": "planned:" + owner + ":" + identity,
            "planHook": "planned:BOT-05:" + identity, "mode": mode,
            "positive": positive, "negative": negative, "ruleTests": sorted(set(tests)),
            "evidence": {"legal": "pending", "purposeful": "pending", "privateReport": None},
        })
        scenarios.extend([
            {"id": positive, "mechanic": identity, "intent": "consequence" if mode == "automatic" else "useful-use",
             "status": "planned"},
            {"id": negative, "mechanic": identity, "intent": "no-effect-outside-conditions" if mode == "automatic" else "retain-decline-or-illegal",
             "status": "planned"},
        ])

    for path in MANIFESTS:
        manifest = read(path, root)
        for index, card in enumerate(manifest["cards"]):
            cid = card["id"]
            module = cid.split(".")[0]
            owner, rule_ids = MODULES[module]
            metadata = next(c for c in runtime["cards"] + runtime["characters"] if c["id"] == cid)
            is_event = "eventImplemented" in card
            predicate = (f"CardCatalog.Enabled(configuration, {cid}); ordinary player options" if is_event else
                         f"Enabled module/version and player-visible roster/options for {cid}; evaluate passive consequences")
            common = dict(rule_ids=rule_ids if is_event or module != "base" else ["B10", "B16"],
                          owner=owner, faction=metadata["side"], content=cid, predicate=predicate)
            source = f"{path}#/cards/{index}"
            if is_event:
                for use in ("event", "combat"):
                    if card.get(use + "Implemented"):
                        add(f"card:{cid}:{use}", "card-" + use, source + "/" + use + "Implemented",
                            tests=card.get("tests", []), **common)
            else:
                add(f"character:{cid}:entry", "character-entry", source, tests=card.get("entryTests", []), **common)
                if "exitTests" in card:
                    add(f"character:{cid}:exit", "character-exit", source + "/exitTests", mode="automatic",
                        tests=card["exitTests"], **common)
                for ai, ability in enumerate(card["abilities"]):
                    add(f"character:{cid}:ability:{ability['id']}", "character-ability", source + f"/abilities/{ai}",
                        mode="choice-and-consequence", tests=ability.get("tests", []), **common)
            # Source clauses include passive restrictions, timing, costs and configuration exceptions.
            for field in ("eventRules", "combatRules", "sourceAbilities", "verificationCases"):
                for ci, clause in enumerate(card.get(field, [])):
                    add(f"clause:{cid}:{field}:{ci}", "source-clause", source + f"/{field}/{ci}",
                        mode="choice-and-consequence", tests=clause.get("tests", []), **common)
        for ti, token in enumerate(manifest.get("tokens", [])):
            add("token:" + token["id"], "token", f"{path}#/tokens/{ti}", content=token["id"],
                rule_ids=["K01"], owner="BOT-11", tests=token.get("tests", []), predicate="Kings; ordinary token options")

    for rule in rules:
        rid = rule["id"]
        owner = {"L": "BOT-10", "K": "BOT-11", "X": "BOT-11", "F": "BOT-09"}.get(rid[0], "BOT-03")
        for category in ("rule", "cost"):
            add(f"{category}:{rid}", category, f"docs/rules/catalog.json#rules/{rid}", rule_ids=[rid],
                owner=owner, predicate="Modules: " + ", ".join(rule["required_modules"]), tests=rule.get("tests", []))
    for category in ("decisions", "automatic_steps"):
        for item in decision_spec[category]:
            if isinstance(item, str):
                item = {"id": item, "rules": ["B20"] if "Victory" in item else ["B02"]}
            add("window:" + item["id"], "documented-window", f"docs/rules/decisions.json#{category}/{item['id']}",
                rule_ids=item["rules"], mode="automatic" if category == "automatic_steps" else "choice")
    # The typed runtime catalog closes the gap between rule-document families and actual API enums.
    for category, rule_ids in (("commands", ["B02", "B04"]), ("decisions", ["B02"]),
                               ("phases", ["B02"]), ("victories", ["B20"]), ("costs", ["B03", "B04", "B05"])):
        for value in runtime[category]:
            add(f"{category}:{value}", category, f"{RUNTIME}#/{category}/{value}", rule_ids=rule_ids,
                owner="BOT-05" if category == "victories" else "BOT-03",
                mode="automatic" if category in ("phases", "victories") else "choice")
    for name, shape in runtime["shapes"].items():
        for field in shape:
            add(f"payload:{name}.{field}", "payload-field", f"{RUNTIME}#/shapes/{name}/{field}", rule_ids=["B04"])
    for config in runtime["configurations"]:
        add("configuration:" + config["id"], "configuration", f"{RUNTIME}#/configurations/{config['id']}",
            rule_ids=["B01", "L01", "X01", "X02", "F01", "X03"], owner="BOT-14", mode="automatic",
            predicate="Exact normalized flags: " + json.dumps(config["flags"], sort_keys=True))
    return {
        "schemaVersion": 1,
        "scope": "Inventory and planned evidence. No implemented strategic behavior or acceptance is claimed.",
        "scenarioIntents": {
            "useful-use": "A visible opportunity makes this legal option preferable to an available alternative; assert choice, cost and resulting state.",
            "retain-decline-or-illegal": "A paired state makes retaining/declining preferable; also reject unavailable/wrong-owner/cost/target options. Mandatory choices use a legal alternative.",
            "consequence": "Account for the automatic effect in candidate value and the resulting hypothetical state; no fabricated command.",
            "no-effect-outside-conditions": "Absent trigger/module/participant does not apply the effect or distort candidate value.",
        },
        "mechanics": sorted(rows, key=lambda row: row["id"]),
        "scenarios": sorted(scenarios, key=lambda row: row["id"]),
    }
