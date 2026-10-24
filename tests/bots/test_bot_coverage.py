import copy
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "scripts"))
from bot_catalog import LEDGER, RUNTIME, inventory, read
from bot_coverage import validate
from ci_scope import needs_code


class CoverageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.runtime = read(RUNTIME)
        cls.ledger = read(LEDGER)

    def altered(self):
        return copy.deepcopy(self.ledger)

    def test_complete_inventory_is_not_behavioral_acceptance(self):
        self.assertEqual([], validate(self.ledger, self.runtime))
        self.assertTrue(all(row["evidence"]["purposeful"] == "pending" for row in self.ledger["mechanics"]))

    def test_missing_mechanic(self):
        document = self.altered()
        document["mechanics"].pop()
        self.assertTrue(any("Missing mechanics" in error for error in validate(document, self.runtime)))

    def test_unknown_rule_and_content(self):
        for field, value in (("rules", ["B999"]), ("content", "base.nonexistent")):
            with self.subTest(field=field):
                document = self.altered()
                document["mechanics"][0][field] = value
                self.assertTrue(any("." + field in error for error in validate(document, self.runtime)))

    def test_orphan_scenario(self):
        document = self.altered()
        document["scenarios"].pop()
        self.assertTrue(any("Orphan scenario" in error for error in validate(document, self.runtime)))

    def test_pending_evidence_cannot_be_called_passed(self):
        document = self.altered()
        document["mechanics"][0]["evidence"]["purposeful"] = "passed"
        document["scenarios"][0]["status"] = "passed"
        errors = validate(document, self.runtime)
        self.assertTrue(any(".evidence" in error for error in errors))
        self.assertTrue(any(".status" in error for error in errors))

    def test_duplicate_mechanic(self):
        document = self.altered()
        document["mechanics"].append(document["mechanics"][0])
        self.assertTrue(any("Duplicate mechanics" in error for error in validate(document, self.runtime)))

    def test_new_runtime_decision_and_payload_need_coverage(self):
        runtime = copy.deepcopy(self.runtime)
        runtime["decisions"].append("NewDecision")
        runtime["shapes"]["CommandPayload"]["NewTarget"] = "String"
        errors = validate(self.ledger, runtime)
        self.assertTrue(any("decisions:NewDecision" in error for error in errors))
        self.assertTrue(any("payload:CommandPayload.NewTarget" in error for error in errors))

    def test_engine_inputs_include_defaults_and_rejected_combinations(self):
        inputs = self.runtime["configurationInputs"]
        self.assertEqual(768, len(inputs))
        self.assertTrue(any(row["flags"]["Dismay"] is None for row in inputs))
        self.assertTrue(any(row["conflict"] is not None for row in inputs))
        legal = {row["configuration"] for row in inputs if row["conflict"] is None}
        self.assertEqual(93, len(legal))
        self.assertEqual(legal, {row["id"] for row in self.runtime["configurations"]})

    def test_stale_export_fails(self):
        runtime = dict(self.runtime, sourceDigest="old")
        self.assertTrue(any("stale" in error for error in validate(self.ledger, runtime)))

    def test_new_compiled_ability_requires_a_source_manifest_entry(self):
        runtime = copy.deepcopy(self.runtime)
        runtime["characters"][0]["abilities"].append("new-ability")
        self.assertTrue(any("ability/manifest mismatch" in error for error in validate(self.ledger, runtime)))

    def test_missing_compiled_content_reports_a_source_mismatch(self):
        runtime = copy.deepcopy(self.runtime)
        runtime["cards"].pop()
        self.assertTrue(any("content/manifest mismatch" in error for error in validate(self.ledger, runtime)))

    def test_export_changes_force_compiled_verification(self):
        self.assertTrue(needs_code([str(RUNTIME)]))

    def test_regeneration_is_stable(self):
        self.assertEqual(self.ledger, inventory(self.runtime))


if __name__ == "__main__":
    unittest.main()
