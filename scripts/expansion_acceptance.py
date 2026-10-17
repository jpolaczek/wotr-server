#!/usr/bin/env python3
"""Check expansion composition sources, all card manifests and the executed 93-case matrix."""
import argparse
from collections import Counter
from pathlib import Path
import xml.etree.ElementTree as ET

from card_coverage import read, test_link, verified_cards as base_cards, verify_results
from fate_acceptance import verify_complete as fate_cards
from kings_card_coverage import verified_cards as kings_cards
from lords_card_coverage import verified_cards as lords_cards
from source_fate import verify_compositions

MATRIX_METHODS = (
    "ExpansionCompositionTests.EveryLegalConfigurationHasExactSourceSlotsFiguresDiceTilesAndFirstTurn",
    "ExpansionPrivacyTests.EveryConfigurationHidesPrivateSetupHandsDeckOrderAndEntropyAcrossReload",
)
TESTS = [
    *MATRIX_METHODS,
    "ExpansionConfigurationTests.All512ExplicitFlagsMatchTheSourceTableAndInvalidInputsConsumeNoRandomness",
    "ExpansionConfigurationTests.OmittedDismayHasTheSameLegalityAndSetupAsItsEffectiveDefaultForAll256Inputs",
    "ExpansionConfigurationTests.ConflictingFateKingsSlotsFailWithPublicSourceDiagnosticsIndependentOfOrder",
    "ExpansionConfigurationTests.MissingAddedCardsSpecialCardsAndDuplicateSlotsCannotPassComposition",
    "ExpansionPersistenceTests.All93ConfigurationsRestartTheirFirstSetupDecisionWithPrivateViewsAndIdempotentRetry",
    "ExpansionPersistenceTests.All93ConfigurationsRestartTheirFirstPaidActionWithoutResettingSetupOrShufflingAgain",
    "ExpansionPersistenceTests.FateCouncilBookAndChieftainPersistEachDecisionAndBothSeparateCosts",
    "ExpansionPersistenceTests.FullRosterDiceChoicesRulerDiscardsAndDismayPersistWithEitherTokenVariant",
    "ExpansionPersistenceTests.FullCompositionPrivateCorruptionDrawAndAttachmentRestartInAllFourTileVariants",
    "ExpansionInteractionTests.FateCouncilGimliEnablesBookAndItsGrantedTokenPaysAKingsChieftainWithoutDainOrBrand",
    "ExpansionInteractionTests.FullCompositionCorruptionSkipsSmeagolExcludesCaptainAndPreservesSpecialEyeCost",
    "ExpansionDiceIntegrationTests.CompleteFateRosterUsesLordsChoicesRulerDiscardsThenTheodensDismay",
    "KingsVariantMatrixTests.EveryLegalKingsConfigurationPreservesVariantBehaviorAndBothViewsAcrossSave",
    "KingsLordsIntegrationTests.SpecialEyesPayCorruptionAndRemainOwnedButArePlacedWithFreePeoplesUsedDice",
    "KingsIntegrationPersistenceTests.LoreAndDismayOrderPersistsBeforeAndAfterEitherEffect",
    "BaseCorpusTests.FrozenSaveReadsWithoutLosingDecisionCostOrContinuation",
    "BaseCorpusTests.SavedDecisionResumesToTheReviewedResultWithoutMutatingItsInput",
    "CharacterManifestTests.EveryReviewedCharacterEntryAndPrintedAbilityHasAnExistingBehaviorTest",
    "LordsCharacterManifestTests.AllEightCardsHaveEntryExitAbilitiesAndEveryReviewedSourceCaseMappedToBehaviorTests",
    "LordsCharacterManifestTests.AllSixCouncilCardsHaveAbilitiesAndEveryReviewedSourceCaseMappedToBehaviorTests",
    "KingsCharacterManifestTests.TenSovereignFrontsMatchReviewedStatsAndMapAllAbilitiesAndCasesToActiveTests",
    "KingsCharacterManifestTests.ThreeChieftainFrontsAndFiveTokensMatchReviewedStatsAndMapEveryCaseToActiveTests",
]


def verify_metadata():
    for module, source_file in (("lords", "lords-card-index"), ("kings", "kings-card-index"), ("fate", "fate-index")):
        metadata = read(f"content/{module}-cards.json")
        # The client receives the catalog through the pinned public content package.
        assert f"{module}-cards.json" in read("content/unity-public-content.json")["files"]
        sources = {card["id"]: card for card in read(f"docs/sources/{source_file}.json")["cards"]}
        assert len(metadata) == len(sources) == len({card["id"] for card in metadata})
        for card in metadata:
            source = sources[card["id"]]
            assert card.get("replaces", "") == (source["setup"].get("replaces") or ""), card["id"]
            assert card["side"] == source["side"]
            assert card["deck"] == (source["deck"] or "special").lower()
    verify_compositions(read("docs/sources/fate-index.json"))


def character_tests(value):
    if isinstance(value, dict):
        for key, child in value.items():
            if key in ("tests", "entryTests", "exitTests", "integrationTests"):
                yield from child
            else:
                yield from character_tests(child)
    elif isinstance(value, list):
        for child in value:
            yield from character_tests(child)


def requirements():
    verify_metadata()
    cards = {**base_cards()[1], **lords_cards()[1], **kings_cards()[1], **fate_cards()}
    for module in ("character", "lords-character", "council-character", "kings-sovereign", "kings-chieftain"):
        tests = sorted(set(character_tests(read(f"content/{module}-implementation-manifest.json"))))
        assert tests, module
        cards[module] = {"tests": tests}
    cards["integration"] = {"tests": TESTS}
    for name in {name for card in cards.values() for name in card["tests"]}:
        test_link(name)
    return cards


def verify_matrix_results(path):
    root = ET.parse(path).getroot()
    ns = {"t": root.tag.split("}")[0][1:]}
    counts = Counter()
    for test in root.findall("t:TestDefinitions/t:UnitTest", ns):
        method = test.find("t:TestMethod", ns)
        counts[f"{method.attrib['className'].split('.')[-1]}.{method.attrib['name']}"] += 1
    for method in MATRIX_METHODS:
        assert counts[method] == 93, f"Incomplete configuration matrix: {method} ({counts[method]}/93)"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--results", type=Path)
    args = parser.parse_args()
    cards = requirements()
    if args.results:
        verify_results(args.results, cards)
        verify_matrix_results(args.results)
    print("PASS: eight source compositions, explicit runtime replacements, all event/character manifests and integration requirements")


if __name__ == "__main__":
    main()
