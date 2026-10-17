#!/usr/bin/env python3
"""Accept the complete Fate setup and eight fronts against reviewed sources and TRX."""
import task_registry
import argparse
from pathlib import Path

from card_coverage import verify_results
from fate_card_coverage import read, verified_cards as event_cards
from fate_character_coverage import verified_cards as character_cards

SETUP_TESTS = [
    "FateSetupTests.CompleteRegionDiffMatchesPrintedRulesAndPreservesTheGraph",
    "FateSetupTests.EveryPhysicalFigureMatchesFinalSetupTotalsAndRemovedFiguresReturnToReserve",
    "FateSetupTests.BaseSetupRemainsByteIdenticalAndNewCitiesWithoutFateFailsBeforeRandomness",
    "FateSetupTests.SetupAndBothPublicViewsAreDeterministicDetachedAndStableAcrossReload",
    "FateSetupTests.KingsSetupExplicitlyOmitsBothAbsentSovereignsWithoutChangingOrdinaryFateFigures",
    "FateBoardRuleTests.OnlyEnemyConquestScoresAndRecaptureRemovesThosePoints",
    "FateBoardRuleTests.RecruitmentUsesTheNewNationAndSettlementEvenWhenControlledByTheRecruitingSide",
    "FateBoardRuleTests.NeutralDaleDoesNotBlockPeacefulNationalMovementAndDoesNotChangeControl",
    "FateBoardRuleTests.SauronMayEnterItsNewHomelandAtPeaceAndTheMovementCopyPreservesFate",
    "FateBoardRuleTests.CityAndFortificationDefenceAppliesOnlyToTheFirstFieldRound",
    "FateBoardRuleTests.EreborUsesShadowHuntsAndEntriesWhileIronHillsIsNowASanctuary",
    "FateBoardRuleTests.WhiteHandAndFateOverlaysComposeWithoutChangingConquestPoints",
    "FateBoardRuleTests.RecruitmentPersistsMapOverridesAndPoliticalStateForBothPlayersAcrossRestartAndRetry",
    "FateCardRegionTests.PreRegisteredPitsProgramRecruitsInFateEreborAndStillRejectsItForBase",
    "FateCardRegionTests.MadnessIncludesTheNewSauronStrongholdNeighborhoodOnlyInFate",
    "FateCardRegionTests.RangersDoNotTreatNeutralDaleAsNorthTerritory",
    "FateCardRegionTests.NaryaUsesTheConfiguredNationAndDoesNotActivateDwarvesInCapturedErebor",
]


def verify_setup(source, setup):
    assert setup["version"] == "fate-setup-1" and setup["baseVersion"] == "base-setup-1"
    assert source["setup"]["source"] in setup["source"]
    assert source["setup"]["operation"] == "replace-region-nation-totals"
    totals = [{key.lower(): value for key, value in row.items()} for row in source["setup"]["totals"]]
    assert setup["units"] == totals, "Runtime Fate figure totals differ from the reviewed source"
    assert setup["activeNations"] == [nation["id"] for nation in source["setup"]["nations"] if nation["active"]]
    # Source notation uses an empty nation; runtime represents neutral Dale with null.
    assert setup["regions"] == [{**region, "nation": region["nation"] or None} for region in source["regionOverrides"]]
    assert setup["newCities"] == source["newCities"]["regionOverrides"]


def verify_complete():
    tasks = {task["id"]: task for task in task_registry.records()}
    assert all(tasks[task_id]["status"] == "done" for task_id in ("T32-SETUP", "T32-CARDS", "T32-GIMLI"))
    gap = next(gap for gap in read("docs/sources/gaps.json")["gaps"] if gap["id"] == "GAP-FATE")
    assert gap["status"] == "resolved"
    verify_setup(read("docs/sources/fate-index.json"), read("content/fate-setup.json"))
    events, _ = event_cards()
    characters, _ = character_cards()
    return {**events, **characters, "setup": {"tests": SETUP_TESTS}}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--results", type=Path)
    args = parser.parse_args()
    cards = verify_complete()
    if args.results:
        verify_results(args.results, cards)
    print("PASS: three completed Fate components, exact source/runtime setup and New Cities, six events and two Gimli fronts")


if __name__ == "__main__":
    main()
