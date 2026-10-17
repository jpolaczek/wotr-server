<a id="t21--odbiór-kart-bazowych"></a>
# T21 — acceptance cards base

Scope: all 96 cards Second Edition, both halves of each card, after integration 16 separate packages. [Report of each card](T21-CARD-COVERAGE.md) is generated from a confirmed index, implementation manifesto and parcel allocation. acceptance all game remains in T23, and the completeness of the interface in UI-10/T22.

<a id="kompletność-i-wykonanie"></a>
## Completeness and execution

`python3 scripts/card_coverage.py --check` requires exactly four decks after 24 card, 16 completed packages without repetitions, both implemented halves and initiatives compatible with the read obverses. `EffectManifestTests` compares the manifesto to the real `EffectCatalog.Base`, requires all 96 ID, test methods without `Skip` and non-empty appeals for tests. No command entry can pass through conditional override.

Control of `--results` reads real TRX and requires a result for each definition and execution position recorded in TRX , a consistent number of results and a credit of all the manifesto methods present. TRX does not prove that the call did not filter theory cases; the full regression also confirms the saved command without a filter. Does not consider the presence of the test code itself to be a credit. Call for `shadow-strategy-full-final.trx` confirmed 470 methods and 980 cases; full course included 1638 domain testing, HTTP and PostgreSQL The new control of the manifesto itself passed separately 1/1 In `t21-manifest.trx` .

<a id="przecięcia-paczek"></a>
## Cuts of parcels

The following matrix indicates the behavior checked, not only the similarity of the program names. The sources of the individual rules and interpretations are in the specifications of the packages available by the 96 cards report.

| Intersection | Expected behaviour | Existing evidence |
| --- | --- | --- |
| Swarm / Daring / White Rider | 0 draws the defender; cancellation of the cost; played but ineffective card is still not missing card. | `ShadowStrategy02CombatTests.SwarmAndDaringResolveTheDefendersInitiativeZeroFirst`, `WhiteRiderMakesDaringIneffectiveButItStillBlocksTheSwarmRerollBonus` |
| Mûmakil / Charge | The condition of the elite when playing; the later loss of the elite does not remove the bonus. Comparison of hits takes into account previous special hits. | `ShadowStrategy02CombatTests.LosingTheSouthronEliteToChargeDoesNotRemoveCommittedMumakilBonus`, `MumakilComparisonIncludesFreePeoplesChargeHitsAlreadyTaken` |
| Words / Andúril / Heroic Death | Disablement of capabilities and Leadership blocks the cost of Andúril, but not the voluntary victim of the current character. | `Shadow03InteractionTests.WordsStopsAndurilPaymentButNotHeroicDeathOfThatCompanion` |
| Words / Sudden Strike / Brave Stand | Disabled Leadership reduces the right throw; the presence of Companion itself remains. | `Shadow03InteractionTests.WordsUpdatesSuddenStrikeButBraveStandStillCountsTheCompanion` |
| Durin’s Bane / Sudden Strike | The defender performs a first-initiation draw; the destroyed army does not carry out a later attack. | `Shadow03InteractionTests.DefenderDurinsBaneCanDestroyArmyBeforeTiedSuddenStrike` |
| Balrog au stole / Sorcerer | The table game starts the right selection; unused Balrog remains on the table; excess hand awaits at the end of battle. | `Shadow03InteractionTests.TableBalrogEnablesSorcererAndExcessCardsAreDiscardedAtBattleEnd`, `UnusedBalrogRemainsOnTableAfterBattleWithoutBecomingAHandDiscardCost` |
| Grond / Sorcerer / Jerkír / Politics | Restart saves the round, cost and queue; the next selection occurs in its window, without repeating the previous decision. | `Shadow04PersistenceTests.GrondRestoresEachRoundPoliticalQueueSorcererAndFinalPalantirDraw` |
| Corsairs / Scouts | The withdrawal ends the fight before the throw; the landing party remains in the aim, without being returned to Umbar. | `CorsairBattleTests.ScoutsCanWithdrawBeforeRollWithoutSendingShadowBackToUmbar` |
| Four Thing / Axe / new Guide | Protection precedes forced random victim and choice of Guide; removal of injuries does not draw the victim. | `Shadow02HuntTests.TableProtectionPrecedesForcedRandomCasualtyAndNewGuide`, `ZeroDamageAfterProtectionNeverDrawsACompanion` |
| Foul Thing / Staff / Mithril | Prevention of draw, replacement after selection, then the injury rule. | `Shadow02HuntTests.StaffCanPreventTheDrawAndMithrilCanReplaceItBeforeForcedCasualty` |
| Hobbit / Gollum / Additional Stronghold | The separation, the victim and the new Guide have separate windows; the additional tile does not inherit the forced victim of the event. | `Shadow02HuntTests.HobbitCanSeparateBeforeTheRandomPickAndGollumCanReduceRemainder`, `RevelationStartsIndependentStrongholdHuntWithoutForcedCasualtyRule` |
| Different sources of protection for the region | Paying for the removal of one source does not remove the other or reveal the aliens cards. | `RegionProtectionCardTests.RemovingOneSourceLeavesOtherProtectionAndNeverRevealsPrivateOptions` |

<a id="uzupełnienia-testów-rzeczywistych-kart"></a>
## Real test supplements cards

`BaseCardBattleInteractionTests` replaces missing proof of integration, which was not given by the earlier test of the synthetic steps anulowania/ataku:

- Both real card Swarm cancel Scouts before selecting a retreat; they do not get a bonus for the
  absence of card opponent.
- Scouts finish Onslaught and Relentless before the window of voluntary losses and before RNG,
  retaining a later choice of entry and one cost.
- Corsairs → Scouts → Sorcerer → Jerkír passes seven commands HTTP with PostgreSQL reboots and
  receipts. The Desant remains in Pelargir; the Character of Sorcerer is selected before choosing
  the Jerkíra deck; the original die Event remains paid exactly once.

`bash scripts/test.sh --no-restore --filter FullyQualifiedName~BaseCardBattleInteractionTests --logger 'trx;LogFileName=t21-battle-interactions.trx' --results-directory .artifacts/test-results` — **3/3 PASS**, without omission; `.artifacts/T21-battle-interactions.log`. Early test compilation revealed a wrong call `Copy`, the next course 2/3 incomplete payload entry. The tests were corrected and the whole was re-made. Production did not require any change for these cuts.

[Cuts Hunt](T21-HUNT-INTERACTIONS.md) add four methods and seven cases: Jerkír waits for Foul Thing, Worm, change Guide and deploy Hobbit; rejected Staff does not return for an additional Stronghold; victory by Corruption closes the pending selection. Four actual card selection tiles check that Staff prevents the tile but does not cancel the entire event paid. Focus `BaseCardHuntInteractionTests`: **7/7 PASS**, zero omissions, `.artifacts/test-results/t21-hunt-interactions.trx`. One test includes three decisions HTTP/PostgreSQL with reboot and reboot. No production error was detected.

Independent review of the reporting script detected too strong description of the completeness of Theory. The warranty was precisiond and added the compatibility of Definitions/Entries/Results and the TRX meters. The experiment with removing one result in memory was correctly rejected; the full historical TRX passes. Saved command without the filter remains necessary proof of full regression.

<a id="struktura-i-granice"></a>
## Structure and boundaries

Rules remain in pure C#: `Cards` includes programs and special steps, `Effects` stack and costs, `Battles`, `Hunt`, `Fellowship` and `Armies` shared procedures. Transport and durability are separate from the domain; Unity uses options and projections. Production sources do not include `test.*`, atrap rules or `NotImplementedException`. The clear blockage of unimplemented start expansions remains valid behavior of the lobby.

The generation of the report is a documentation tool outside the gameplay path. CI controls its currentity and compatibility of the obverses. Infrastructure tests can continue to use open testing programs; we do not count them as proof of performance of a specific card. acceptance T21 does not declare manual user tests, new standalone builds, or measuring FPS.

Overview of common modules: `EffectRuntime` has 206 line, `BattleRuntime` 258, `HuntRuntime` 244, `ShadowMarchRules` 165. Methods separate individual windows and sequels; none of these modules performs the whole turn as one command. A large list of definitions is loaded once. The most expensive new march generator has a separate measurement and parity test in the specification [Strategy Shadow 7–12](T21-SH-STR-02.md); the documentation report does not load the server or UI.

<a id="końcowa-regresja-t21"></a>
## Final T21 regression

`bash scripts/test.sh --no-restore --logger 'trx;LogFileName=t21-full-final.trx' --results-directory .artifacts/test-results` — **1648/1648 PASS**, no omissions, after integration of all packages and the new interactions. Log: `.artifacts/T21-full-final.log`; report: `.artifacts/test-results/t21-full-final.trx`.

`python3 scripts/card_coverage.py --check --results .artifacts/test-results/t21-full-final.trx` — **PASS : 477 methods, 990 reported cases related to 96 card**, four decks after 24 , complete 16 separate packages and source-compatible initiatives. Full TRX is compatible 1648 Definitions/Entries/Results and execution counters without errors/ omissions. Other full regression tests include the basic rules, server and infrastructure.

T21 range received. Further UI-10 checks the entire interaction contract and immunity draft, T22 set of screens, T23 full batch and acceptance with the base. The previous Unity Packing results remain in TESTING; T21 did not perform a new Unity run or acceptance hand-held.
