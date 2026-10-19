<a id="weryfikacja-i-odbiór"></a>
# Verification and acceptance

This document states what every change must prove and how the tests run. Results of individual checks are recorded
in the local task registry and its testing log (`docs/tasks/`, ignored by Git since 2026-10-17); earlier per-task
evidence remains in Git history. Environment setup is in [DEVELOPMENT](DEVELOPMENT.md).

## Running the tests

- Server: `bash scripts/test.sh` starts the private PostgreSQL 17 cluster (`127.0.0.1:55432`) and runs every .NET
  test; it validates the local task registry first when it exists.
- Content coverage: `python3 scripts/source_cards.py`, `scripts/card_coverage.py --check` and the Lords, Kings and
  Fate checks (see `.github/workflows/ci.yml`) read the tracked ledger `docs/rules/content-ledger.json`.
- Python suites: `python3 -m unittest discover -s tests/ci`, `tests/packages` and `tests/release`.
- Client: the client's `scripts/test-client.sh` runs Unity EditMode and PlayMode against a real Kestrel and
  PostgreSQL; the complete PlayMode inventory runs in isolated batches through `scripts/discover-tests.py` and
  `scripts/test-client-batches.py` (the client's DEVELOPMENT document). Network PlayMode classes run in small packs
  because of the test server's login limit.
- A check that was not run is reported as not run; a generated report or a build is not runtime acceptance.

<a id="poziomy"></a>
## Levels

- Engine: xUnit, examples of sources, controlled RNG, decision sequences, non-converters and
  round-trip writing. The test cannot only repeat the code design without the expected result as a
  rule.
- Server: actual transaction PostgreSQL, two parallel requests, idempotence, private projections,
  reboot and migration.
- Unity: EditMode for adapters and presentations, PlayMode for interaction, visual tests and
  measurements on desktop. Data array is marked as a prototype.
- acceptance games: two independent Windows/macOS clients; full party scenarios and return to many
  types of pending decision.

<a id="obowiązkowa-weryfikacja-ui-80--informacja-i-interaktywne-okna"></a>
## Mandatory verification of UI-80 — information and interactive windows

This is the acceptance plan, without a declaration of performance. Each new result requires XML/log from the actual start and version of sources. Historical tests of “no text on hover/fokus” must be replaced by a separation of text and interactive panel, keeping all cost, projection, draft and geometry ratings.

| Zone | Required proof |
| --- | --- |
| RuntimeTooltip | Hover waits 200 ms, focus of the keyboard gives the text immediately; departure/change source/detach/focus fate removes the description. At most one layer, text in four layouts, `PickingMode.Ignore`. |
| A gesture limit | Any PointerDown, drag/pan/wheel/cancel extinguishs the description, no opening after release itself. The first Esc simultaneously removes the text and goes to the active panel/draft; the hld-key does not close another window. |
| Rich sources and terrain | The own metadata only give text; the region on the hiver only stands out. Cards/inspector, Politics, Characters, sovereign, Effects, battle, Hunt, Fellowship, Victory, Help and Log do not open the panel via hover/fokus. |
| Privacy and accessibility | The own disabled control can explain the condition by remaining disabled. The foreign hand, private versions of the Council, covered tiles and TextField values do not enter the text; the full public description is not cut off. |
| Actual actions | Click/Enter/Space continues to open the panel or performs the legal action. HandDiscard sends exactly one recoil on the double, without additional Confirm; drag and terrain retain the exact cost and receive. |
| Network and return | hover/fokus itself does not create GET/POST or read history. After reconnecting an outdated or invalid source loses the description without changing the camera, choosing or playing the gesture; still the correct information may remain visible. |

The primary families are `RuntimeTooltipTests`, `ClickInspectionTests`, `HandDiscardNetworkTests` and the gestures cards/maps and the other panel representatives. The coordinator records the exact full name of the methods from the class declaration, not the file names; missing/skip is not PASS. The tests of both roles with the real server are separate from the local layout fixture. Four layouts mean 1366×768 and 1920×1080 at 100% and 125%. We do not count the physical smooth, Windows runtime or manual acceptance user based on these attempts. The final editor, profile, builds and views belong to UI-77.

<a id="obowiązkowe-rodziny-przypadków"></a>
## Mandatory families of cases

| ID | Case and expected result point | Related rules |
| --- | --- | --- |
| V01 | pass with a smaller number of dice legal, with an equal of no; the Council chip does not change the comparison. | B03, L03 |
| V02 | The ring changes die, action uses another; the change in the Eye moves the right die without ending normal action. | B05 |
| V03 | The same figure does not move again after the connection; the stack limit enforced at the end of the correct action. | B09 |
| V04 | Regular recruitment and card vary by source. | B06, B08 |
| V05 | Natural 1/6 despite modifiers; limit of five dice/transmit; cancelled Leadership cannot be sacrificed. | B11, B12 |
| V06 | The attacker chooses card first, the defender sees only allowed reverses; a draw of initiative for the defender. | B11 |
| V07 | One/two hits, elite, lack of regular; last unit dies in battle and since the event — different appropriate effects on the character. | B06, B12 |
| V08 | A garrison separate from the besieging; control of Stronghold, rescue, out, extension and no normal retreat. | B13, B14 |
| V09 | Progress of Fellowship without a recorded route; declaration and disclosure with different legal goals and consequences. | B15, B16 |
| V10 | Guide or random Companion; additional tile and the appropriate ability window of the new Guide. | B18 |
| V11 | The final Mordor with Corruption 11/12; military thresholds 9/10 and 3/4, rated in the correct phase and priority. | B19, B20 |
| V12 | Keeper/Lesser with Eye and Remove, Ruler disccard, recovery after loss of character; origin of Captain of Despair. | L01, K01, X01 |
| V13 | Council: secret version, authorized disclosure, start outside Fellowship and token Shadow. | L03 |
| V14 | Kings: Awakened immunity, secret tiles, automatic interruption Kings tile, sequential last tile and nation icon. | K02, X01, X02 |
| V15 | Fate: original owner of Erebor, no free points, no Dáin/Brand, correct card and New Cities. | F01, X03 |
| V16 | Each card: legal and illegal play, both effects, proper windows, mandatory partiality, end of effect. | B06, L02, K03 |

T32-SETUP cover the map part of V15 by `FateSetupTests` , `FateBoardRuleTests` and `FateCardRegionTests` : complete final figures figures , all regions/graf , reserves, deterministic record, legal flags and rejection of New Cities without Fate, no initials PZ , acquisition/Reflection , none Branda/D áina, traffic with preview, recruitment, city defence, additional Hunt and conditions cards Depending on the nation. PostgreSQL includes reboot, both projections, authorisation, preview without mutations and idmpotent retry. `BoardOverrideTests` Unity check the overlay and restore the base without rebuilding geometry/s The hunter. T32-CARDS Covers the replacement cards ; Gimli and full configuration require T32-GIMLI and T33. Command results: [task history](tasks/tasks.json) , description of the rules: [T32-SETUP](rules/T32-FATE-SETUP.md) .

## Niezmienniki

T05: `KernelTests` compares the full score of before/after with serialization of two nested reactions, cost remaining paid, illegal actor/revision/decision/type command, deterministic shuffling and rejection of unknown version of the recording. This is a follow-up test, not a test of unimplemented effects of cards.

Each component is included in exactly one condition/site; the number of reserves and losses are non-negative. Maintaining the number of figures takes into account the apparent changes and removals. No approved expenditure occurs twice. The expected decision belongs to the eligible player and has at least a legal response or a public automatic ending. Exceptions of temporary excess stack are marked by a state of resolution, not a false global violation.

Fuzz/property tests generate only source-compatible input states or consciously malignant command. We do not assume that the randomly created figures system is a legal party.

<a id="macierz-sieci-i-zapisu"></a>
## Network and write Matrix

UI-10: [transport](https://github.com/jpolaczek/wotr-client/blob/ba0e6ad14bbea67b9382aab8cab330d1d8491db2/docs/ui/UI-10-TRANSPORT.md), [reconciliation](https://github.com/jpolaczek/wotr-client/blob/ba0e6ad14bbea67b9382aab8cab330d1d8491db2/docs/ui/UI-10-DRAFTS.md), [raw server payodays](server/UI-10-ACCEPTANCE.md) and [real Unity sequences](https://github.com/jpolaczek/wotr-client/blob/ba0e6ad14bbea67b9382aab8cab330d1d8491db2/docs/ui/UI-10-NETWORK.md) separate unit tests from acceptance from PostgreSQL and Kestrel. T21 reports remain the results of the earlier code; they do not replace the required regression after the UI-10 amendments.

T07: `PostgresTests` and `TransportTests` require `WOTR_TEST_DB`. `bash scripts/test.sh` launches a separate local cluster and a full set without skip. Without the variable `dotnet test` explicitly skips integration. CI has PostgreSQL 17 and sets the variable; it does not replace the database with the atrap. [Test range T07](server/T07-TRANSACTIONS.md).

Make a selection of the cost after the Ring, covered by the card attacker, losses, retreat, Hunt, choice of the route of disclosure, failure of the sovereign, the Council setup and the game end.

1. Accept the decision, save snapshot and receive, interrupt the answer to the client.
2. Restart the server, read both page projections and a pending decision.
3. Renew the same `commandId`: one expenditure, the same saved draw result.
4. Send the second command with an old revision and command with the second device: at most one
   correct state change.
5. Compare the result with the execution without failure.

Check the crash before the commit separately: no partial write and no undisclosed result. The publication error WSS cannot undo the successful transaction.

<a id="prywatność"></a>
## Privacy

T06: `ProjectionTests` compares raw JSON for various foreign hands, deck order, cards for obliged, Council and private king's tiles; it also examines preview, history, Visible variant and secure error codes. The 64 for candidates' subset protects from response containing all combinations.

For two states that differ only in secret compare payoads, options, previews, errors and the history of the opponent. Exceptions are allowed only where the rule reveals the information. Check the direct calls API with ID with the evening batches/cards/jetonu, reconnect in half of the secret selection and the data entered into the operating logs.

Public prompt cannot betray the lack of legal card by the mere fact of omitting a window. Skipcombat cards preference remains private. The visibility test is not just about missing text in Unity, we check the server's raw answers.

<a id="bramki-etapów"></a>
## gate stages

| Gate | Condition |
| --- | --- |
| G-A | Documentation, environment, source/rule directory, model and verified data needed for the first vertical passage. |
| G-B | Two clients perform legal recruitment/movement, reject illegal command and return after disconnection. |
| G-C | Full base, all 96 cards in both applications, characters, four victories and base interactions UI. |
| G-D | Eight combinations of expansions, all legal variants settings and tests of significant effects intersections. |
| G-E | Correspondence, fault resistance, UI scenarios, Windows/macOS builds and proven implementation playback. |

It is not enough to pass a full batch that does not accidentally select difficult card. The manifesto of the content must tie each card and both applications with the tests. For configuration all legal setup combinations are small and should be checked exhaustively; gameplay sequences are covered with targeted scenarios.

<a id="odbiór-ui-i-wydajności"></a>
## acceptance UI and performance

Applicable [list S01–S24](https://github.com/jpolaczek/wotr-client/blob/ba0e6ad14bbea67b9382aab8cab330d1d8491db2/docs/ui/SCENARIOS.md) We measure local feedback time separately from the server response. The report provides hardware, build, number frames/pr bellows and network conditions; the “fluid” itself is not the result. A version without original graphics can pass acceptance if it maintains readability and resource interchangeability.

## Platform acceptance on real machines

| Platform | Accepted | Evidence |
| --- | --- | --- |
| macOS (Universal) | DIST-06 (DS14) | The owner's Mac: install, start, update, uninstall. |
| Linux x64 (AppImage) | LNX-02, 2026-10-18 | The owner's Linux PC with the released client and in-game updates. |
| Windows 10/11 x64 | DIST-13, 2026-10-18 | The owner's check: the released game installs and runs on Windows. Uninstall and reinstall were not reported separately. |
| Steam Deck | open (DECK-01) | Runs the Linux AppImage; still marked experimental. |

<a id="exp-00"></a>
## Expansion client tests — triage EXP-00 (2026-10-19)

Run on client 60ea0081 (test clone) against test backend `ddae9b1de03c`, fresh database, one Unity process per pack
(`packs.sh`), the Council/Setup/Guide/Resource/Sovereign classes then one class per process with a 20-minute watchdog.
Totals: 115 of 127 expansion PlayMode tests pass, including the full desktop game with all expansions
(`T36DesktopGameTests.AllExpansions…`). Every failure has an owner in the expansions program
(~/.claude/plans/validated-sleeping-dahl.md); none points at the rules engine.

| Test | Cause | Kind | Owner |
| --- | --- | --- | --- |
| ExpansionCorruptionNetworkTests ×4 (Hidden, Sequential draw/stop, Visible) | the attempt starts from the Nenya die in the pool row, which is no longer there | obsolete host | UI-274 (attempt button beside the portrait) |
| ExpansionTokenNetworkTests.BalrogEntryOffersAnOptionalReserveFigure | the reserve figure click adds no Elite since the reserve-plan change | regression | UI-271 |
| ExpansionTokenNetworkTests.RulerTokenPaysForAllThreeExplicitRecruitments | dragging the text Ruler-token chip times out | obsolete host | UI-276 |
| KingsCards04NetworkTests.EnemySeparatesFieldArmyFromGarrison | Field/Garrison buttons left the decision tray | obsolete host | UI-278 |
| LordsFinalCardsNetworkTests.Madness… | the card window no longer shows the resolving card id | regression | UI-272 |
| LordsShadowNetworkTests.LoreChangesOneRolledEye… | Lore choices are no longer tray buttons | obsolete host | UI-272 |
| FellowshipGuideNetworkTests.CouncilVersionAppears… | the Guide tooltip of Gandalf Keeper reads the base Gandalf text (no "Narya") | regression | UI-269 |
| InGameDenseLayoutTests.…FiveSovereigns… | the Effects window header now has two buttons | obsolete expectation | UI-274 |
| CouncilHomelandPanelNetworkTests.TwoAndFiveDepartures… | `TooManyRequests`: the per-address sign-in limit (20/min) is fixed in `AuthenticationRateLimit` and cannot be raised for client integration like the other budgets | test environment | EXP-00 (configurable sign-in budget, raised by `wotr-client:scripts/test-client.sh`) |

Process finding: the five Council/CompanionSetup/FellowshipGuide/ResourcePanel/Sovereign classes run together in one
Unity process hung for over three hours (repeated `MatchConnection` receive errors), while each finished alone; this
belongs to QUAL-06 (isolated PlayMode runs).

## Expansion bot games and saves — EXP-03 (2026-10-19)

- `ExpansionBotGameTests`: two demo CPUs play 14 configurations (each expansion alone, with each of its variants, the
  pairs, all three, all variants) with two seeds each. At every expansion decision and every 25th step the save reads
  back to the same bytes and projections, and each side's projection is unchanged by the other side's hidden
  information (`HiddenInformation.Perturbed`: hand against deck, deck order, random samples, unreadable corruption
  tiles). `EveryExpansionDecisionKindComesUp` plays four families with three seeds; Dismay comes from
  `TheBotAnswersADismayWindowBothWays`. About three minutes in the default suite.
- `ExpansionCorpusTests` over `tests/fixtures/expansion-regression` (11 frozen saves from CPU games, 1.6 MB, exported
  with `bash scripts/dotnet.sh run --project tests/Wotr.Scenarios -c Release -- --export-expansion-corpus <empty dir>`;
  see its README): read-back, the recorded answer and the following decision, and privacy.
- `ExpansionHttpGameTests`: one all-expansions game over the HTTP command route and PostgreSQL, restarted every 120
  moves, with identical projections after each restart (2026-10-19: 687 moves, six server runs, Shadow by corruption
  in turn 23).

## Fate of Erebor release gate — EXP-04 (2026-10-19)

The automated part of the Fate gate passed; the owner's playtest on a test server is still open, so Fate is not yet
released (EXP-05 waits for it).

| Gate item | Check | Result |
| --- | --- | --- |
| Bot games and saves | `ExpansionBotGameTests` (EXP-03): Fate alone, with New Cities, in the pairs and with all three, two seeds each; saves and privacy at every expansion decision | PASS |
| Whole game over HTTP with an observer and its replay | `ReplayEndToEndTests.AWholeFateGameWithALateObserverGivesTheSameReplayToPlayerAndObserver`: Fate + New Cities, demo CPUs on both sides, the observer joins in turn 3; the player's and the observer's replay are the same file, keep the Fate rules and match the recording | PASS (25 s) |
| Client full games on two clients | `wotr-client:FateAcceptanceNetworkTests`: Fate at 100% (10 turns, 146 decisions) and Fate + New Cities at 125% with reduced motion (12 turns, 180 decisions), both through the normal lobby with reconnects; every decision has its object host and the generic decision window never opens (`GenericWindowWatch`); the 125% game checks both roles' layouts at every decision | PASS |
| Replay in the library | the 100% game's replay is saved from the end screen and played before sign-in: the Fate crest on its row and the phase plaque, the Fate board with the Shadow's Erebor, the last step opens the end screen | PASS |
| Observer | `wotr-client:ObserverBoardNetworkTests.AnObserverWatchesAFateGameFromBothSides`: the crest and the Fate board for both viewing sides | PASS |
| Fate cards and Gimli | UI-266: `FateCardsNetworkTests` 6/6, `FateGimliNetworkTests` 3/3, each step on its object; the six cards in the test menu's Fate tab | PASS |
| Owner playtest | a Fate game by the owner on a test server, with an observer and a saved replay | open |

Limit: the client full-game driver (`FullGameChoices`) plays a narrow strategy and plays no Fate card; the cards'
client steps are covered card by card (UI-266) and every Fate decision by the bot games.
