<a id="weryfikacja-i-odbiór"></a>
# Verification and acceptance

Historical acceptance interface UI-24 describes UI-24 . It is not proof of the current model UI-73 – UI-80 neither acceptance Common UI-77 Full. NET ed PostgreSQL : **3550/3550 PASS**, without omission, `ui24-full.trx` ed 2026-10-07 Unity EditMode after all changes: **245/245 PASS**, `ui24-editor-tooltip.xml` . Final full PlayMode : **309/309 PASS**, without omission, `ui24-play-accepted.xml` ( 02:50:28–03:06:02 UTC Checker). expansions confirmed 43 required scenarios on this report, directory 30 families/35 windows also PASS Development Player: **28/28 measurements PASS**, 104,88–120,00 FPS and feedback to 27,14 ms on Apple M1 Pro, [raw report](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui24-macos-accepted.json) Previous failures and their corrections are explicitly recorded in UI-24 The other entries of this document are evidence of individual tasks, not a declaration of testing subsequent changes.

Client release **0.2.0-rc1** with `c180caa`: builds macOS Universal and Windows x64 without Development, control ZIP/SHA-256 and start of unpacked macOS with clean profile **PASS** (`t39-smoke-f67f8986/result.json`). Screenshot of login viewed, previous profile restored. Windows runtime, a common batch between platforms, Intel Mac, host and manual tests remain postponed. Packages require a 2 protocol server; their manifest covers only clients.

T31: full regression `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t31-accepted.trx' --results-directory .artifacts/test-results`: **3032/3032   PASS**,  0  skip,  2026-10-06 Includes 176 new cases T31:  Dismay  38 sequence 17 matrix 86  (84 configurations and two West), Council 21,  Lords/Kings   9 and five multi-stage reboot tests HTTP/PostgreSQL.  Unity  EditMode: **153/153   PASS**,  0  skip,  `.artifacts/t31-editor-pass.xml/log` Checkers cover to the full TRX: Kings  157 methods / 313 cases, Lords 148  /  248, Base 477  /  990 Early results and amendments and the border acceptance   UI   expansions save [T31](rules/T31-INTEGRATION.md). Final `bash scripts/test-client.sh -testFilter KingsCards`: **25/25   PASS**,  0  skip,  07:28:40–07:29:40   UTC,  `.artifacts/t31-play-final.xml/log`.

This document specifies the required tests. Indeed, the checks performed saves the task history in `tasks.json`; the environmental commands are in [DEVELOPMENT](DEVELOPMENT.md). Manual user tests are postponed, not considered to have been performed. The agent launches the available automatic controls and builds.

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

T02 saves actual build and test commands to the environment instructions file. Until then, we do not create fictitious `dotnet test` or Unity Test Runner results.

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

UI-10: [transport](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/UI-10-TRANSPORT.md), [reconciliation](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/UI-10-DRAFTS.md), [raw server payodays](server/UI-10-ACCEPTANCE.md) and [real Unity sequences](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/UI-10-NETWORK.md) separate unit tests from acceptance from PostgreSQL and Kestrel. T21 reports remain the results of the earlier code; they do not replace the required regression after the UI-10 amendments.

UI-10 , 2026-10-05 : After unlocking the environment **1668/1668 . NET ed PostgreSQL , 118/118 Unity EditMode and 3/3 new network scenarios PASS - Full. PlayMode : 150/152 ; two failures concerned a reset of independent fixtures during revisions 0 After the amendment, the entire changed class `BoardInteractionTests` :** 6/6 PASS **. No omissions in all the mentioned runs. It is a full regression with a targeted repair re-up, not a single report 152/152. The exact commands, logs and previous locks are in the protocol UI-10 . acceptance whole product UI and the indicated presentation shortcomings belong to T22.

T22: [decision matrix and scenarios](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/T22-ACCEPTANCE.md) includes the entire base. `BaseTurnScenario` , `BaseAbilityScenario` , `BaseBoundaryScenario` , `BaseInformationScenario` and `BaseBoardScenario` prepare only isolated test records; the client has no production endpoint change of position; or RNG First post-integration checks: `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t22-full.trx' --results-directory .artifacts/test-results` — **1668/1668 PASS , 0 skip**, test time 61 p. Build `Wotr.Scenarios` in Release: 0 errors/warnings. `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t22-editor-first.xml -logFile .artifacts/t22-editor-first.log` — **118/118 PASS , 0 skip**, 2026-10-05 22:01:36–22:01:38 UTC . PlayMode and acceptance the new arrangement is in progress; the above checks do not close T22 themselves.

Final T22: `bash scripts/test-client.sh` — **172/172 PASS , 0 skip**, `.artifacts/t22-play-full.xml/log` , 22:13:25–22:20:03 UTC ( 397,84 s). final EditMode as corrected by: **118/118 PASS , 0 skip**, `.artifacts/t22-editor-final.xml/log` , 22:20:20–22:20:22 UTC Includes 20 new PlayMode and full earlier regression, eight frame settings information/history and S01– S16/S20 -S21 with a real backend. 13/20 , next 18/20 and targeted repairs are preserved in the protocol. direction/odst New panels, their collision with the report battle , Lighting empty ID Rings and synthetic indicator between test panels. No changes were made to the rules, thresholds of victory or RNG . Further acceptance full batch and body of records: T23.

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

T23: [protocol acceptance bases](T23-ACCEPTANCE.md) connects the V01– matrix V11/V16 , a full batch with a normal lobby and [body 28 records](../tests/fixtures/base-regression/README.md) New body tests: **85/85 PASS , 0 skip**. Full regression. NET ed PostgreSQL : **1753/1753 PASS , 0 skip**, `.artifacts/test-results/t23-full.trx` Manifesto 96 cards Answers 477 made methods/990 cases; manifesto 14 forms — 29 methods/51 The first full Unity party: **1/1 PASS**, 7 Turn, 92 decisions, RingCorrupted production RNG Final client: **118/118 EditMode and 173/173 PlayMode , 0 skip**; `.artifacts/t23-editor-final.xml` and `.artifacts/t23-play-full.xml` Second full batch in full regression: 11 Turn, 145 decisions, RingCorrupted step by step 4 The T23 is closing the base; expansions and desktop releases retain separate stages criteria D/E.

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

Applicable [list S01–S24](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/SCENARIOS.md) We measure local feedback time separately from the server response. The report provides hardware, build, number frames/pr bellows and network conditions; the “fluid” itself is not the result. A version without original graphics can pass acceptance if it maintains readability and resource interchangeability.

<a id="integracja-klienta-t09"></a>
## T09 Client Integration

`bash scripts/test-client.sh` creates one-time accounts in `wotr_test`, launches Kestrel on loopback and performs all Unity PlayMode: two sessions, lobby, configuration, roles, WSS reconnect, cancellation of session and layout. The server process is closed after the test. Without the `WOTR_CLIENT_TEST_CONFIG` variable, the network test is explicitly omitted, so we use the script to acceptance. Results and limits describe [report](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/T09-INTEGRATION.md); the test does not replace the production TLS or Windows–macOS game.

T10 extends this test with an approved command with a lost response HTTP, a receipt without a second execution, a Hunt assignment and a first throw. A separate test checks the tray with 10 dice, 8 card and 3 Rings in two resolutions. [Report T10](rules/T10-TURNS.md) provides range and results: 54 tests .NET, 6 EditMode and 4 PlayMode, all counted without omissions.

<a id="sekwencja-ruchu-i-rekrutacji-t13"></a>
## Traffic and recruitment sequence T13

`MovementNetworkTests` mounts two independent panels Unity 1366×768, two sessions, controllers `TurnInteraction` and connections HTTP/WSS to Kestrel/PostgreSQL. Performs a clicked move Minas Tirith–Osgiliath, a stalled recruitment of Sauron's elite, a clicked recruitment of Gondor and a dragged Orthac–Gap of Rohan move. Each step compares public figure, control, policy, dice and the decision maker of both parties. Hidden hands does not compare as common data.

The first response to the move is not performed until the actual request is executed. UI recovers the saved receipt after the break of WebSocket; the camera position is unchanged, one expense, one revision and an additional idmpotent re-upgrade of the identical command. A separate request for an illegal jump to Rivendell must leave the entire projection unchanged. Restart the host HTTP with an expected removal of excess after recruitment includes `MusterPersistenceTests` in the regression .NET.

The entry point remains `bash scripts/test-client.sh`. The `tests/Wotr.Scenarios` tool creates normal accounts by `AccountService`, a clear snapshot with At War nations and established standard results of dice and two invitations for the lobby test. It refuses to operate without `WOTR_TEST_DB` or the database name ending with `_test`. Passwords and invitations are only sent to the ignored `.artifacts/client-integration.json` file. The tool is not part of the server, does not provide the endpoint and does not change the production RNG. The startup status is fixture; validation, previews, command, recording and communication are real.

S01–S06 retain detailed attempts of models and interactions UI-04/UI-05; T13 adds a network sequence to them. This acceptance foundation game on two Unity clients in macOS, not a full batch or a Windows–macOS test. The actual results of the current run and builds save the T13 history.

<a id="efekty-t14"></a>
## Effects T14

`EventTests` , `EffectRuntimeTests` , `CasualtyTests` and `EffectManifestTests` check normal/conditional/au • execution, cost sources, expiration, privacy and nested reactions. `EffectPersistenceTests` launches real HTTP/PostgreSQL , destroys the host after playing Celeborn’s Galadhrim, resumes the selection figures and selection and recovery of both receipts without the other kosztu/doboru. It's a full regression. NET : 130/130 , no omissions. Unity EditMode : 19/19 ; PlayMode ed `scripts/test-client.sh` : 12/12 without omission. Infrastructure tests with programs `test.*` are not a declaration of the rules implemented cards .

<a id="karty-i-wieloetapowy-interfejs-ui-06"></a>
## card and multi-stage interface UI-06

[S07/S08](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/SCENARIOS.md) describe the full scope and limitations acceptance New `CardDraftTests` check the strategic use of Will of the West, reconcile and identity of click/drop. `CardInteractionTests` examines all texts, nine cards Focus, pin, two targets and a reaction. `CardNetworkTests` adds nine revisions by the production server, four implemented events, private discard and reconnect. Seeder `CardScenario` only exists in the test tool and maintains the completeness of the waist. PlayMode has 15 tests, without omission; regression . NET still 130/130.

`bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/UI06-edit.xml -logFile .artifacts/UI06-edit.log`: **22/22**, no omissions (2026-10-05 01:55:39 UTC).

<a id="drużyna-i-towarzysze-t15"></a>
## Fellowship and Companion T15

[Specification](rules/T15-FELLOWSHIP.md) binds V09 to the rules BASE 24/34/37 – 39 and FAQ 1.2 . `FellowshipTests` , `CompanionTests` and `FellowshipPersistenceTests` add 25 cases including three restart moments HTTP/PostgreSQL , two idempotent approvals and private options. Earlier empty composition tests received the correct Gollum. Full . NET : **155/155**, without omission. Unity Regression PlayMode : **15/15**, without omission ( 2026-10-05 02:19:43 UTC ); does not count S14–S16 before UI-08 .

<a id="polowanie-t16"></a>
## Hunt T16

[Specification and result](rules/T16-HUNT.md) covers V10 and the dependent end by Corruption 12. Three new test classes separate roll/pools, a rule sequence and a real restart HTTP/PostgreSQL. Controlled RNG only works by replacing the test host dependencies; the production server still uses `ServerRandom`. The rebooted hosts use normal handling and transaction configuration. The sampling and result-by-retry sample tests compare the number of samples and the result-by-retry, not only the final Corruption counter.

Final **184/184 . NET**, **22/22 Unity EditMode**, ** 15/15 Unity PlayMode *, no skip. Time to end PlayMode 2026-10-05 02:44:21 UTC , EditMode 02:47:07 UTC S15 in interface, Mordor and full of capabilities/ card are not counted by infrastructure programme tests; they have tasks UI-08 , T17 and T20/T21.

<a id="mordor-i-zakończenie-t17"></a>
## Mordor and T17 End

[Description and boundaries of acceptance](rules/T17-MORDOR.md): `MordorTests` checks all 24 tiles with expectations independent of the production directory and input/pool/ukrycie/bezczynno. `VictoryTests` covers V11 and four results, `VictoryPersistenceTests` destroys the host before final damage and after both Ring ends. The final step is not seen as a win before the settlement of Corruption; the new action then returns `GameFinished`.

`bash scripts/test.sh --no-restore`: **233/233, zero omissions**. Unity regressions are in the history of T17; S16 in the usable interface remains to UI-08. Passing between contract versions requires a separate T37 test: an old command list and an old fingerprint receipt are not checked by the reboot of the current host itself.

<a id="drużyna-polowanie-i-mordor-w-kliencie-ui-08"></a>
## Fellowship, Hunt and Mordor in the UI-08 client

[acceptance S14–S16](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/SCENARIOS.md) describes six saved scenarios of two clients with Kestrel/PostgreSQL, including two additional fortresses, refill, reconnect, entry/hidden and both border limits of the end of Mordor. `FellowshipDraftTests` verifies local reversibility and lack of strategic default choices. `FellowshipLayoutTests` performs a size/scale matrix and responds to the open-ended fixture of the protocol; actual intervention rules have T16 tests, and specific capabilities and card remain in T20/T21.

Results: **234/234 .NET** (`scripts/test.sh --no-restore`), **27/27 EditMode** (`.artifacts/UI08-edit.xml`), **19/19 full PlayMode** (`scripts/test-client.sh`, end 03:47:31 UTC), without skips. Final layout control has `.artifacts/UI08-layout.xml`. Manual user and Windows runtime tests remain postponed; they are not replaced by rendering another resolution on macOS.

<a id="bitwy-polowe-t18"></a>
## battle field T18

[Rules and limits](rules/T18-BATTLES.md) . `BattleRulesTests` , `BattleRollTests` , `BattleLossTests` , `BattleMovementTests` , `BattleCardTests` and `BattlePersistenceTests` cover V05–V07: mandatory rear guard, natural 1/6 , limits of five dice and metastatic cancelled/after Holy Leadership , secret elections and joint disclosure, several initiatives of one card , tie for defender, cancel and immediate loss of special attack, ending before throw, both order losses, degradation of the elite with available regular and in the absence of figure , overshoot, overshoot/cross the border, stack limit and optional progress and control. A separate case resumes the event program after nested battle .

HTTP/PostgreSQL launches a new host after the secret attacking card and after its elimination. The defenseman's reading does not reveal his face before his choice; the retra of each saved command does not draw again. The attacker continues to ask the previously obtained hit. After recent losses there is one used cost and an empty queue of settlements.

`bash scripts/test.sh --no-restore` : **280/280**, without omission; Battle filter **47/47** (includes an earlier loss test). Unity EditMode : **27/27**, no skip, `.artifacts/T18-edit.xml` , 2026-10-05 04:23:01 UTC . UI battle S09–S12 remains to UI-07 and base programs cards T21. We do not include them with infrastructure tests `test.*` .

Full client regression with real Kestrel/PostgreSQL: `bash scripts/test-client.sh`, **19/19 PlayMode**, without skis, `.artifacts/client-play.xml`, end 2026-10-05 04:25:02 UTC. No manual acceptance or new FPS measurement was performed.

<a id="twierdze-t19"></a>
## Fortress T19

`SiegeTests`, `SortieReliefTests` and `SiegePersistenceTests` add 23 cases V08 compatible with [source description](rules/T19-SIEGES.md). Includes the withdrawal of six units and the refusal of progress without reduction to five, the limit after setting up siege, several extensions, the ban on the use of the elite rear guard, no regular, all combinations of destruction/rear guards and the change of points after reflection Stronghold. They also check the absence of the Stronghold bonus in the field, exit and rescue, the limit of the after connection, the ending of siege by event, the lonely character and shelter at the total environment.

The HTTP test runs another host before and after the elite selection. Both ID elites still occur in the options, the selected nation provides regular, the second elite remains, and the renewed command does not consume figure or dice again. Full regression .NET: `bash scripts/test.sh --no-restore`, **303/303, zero skip**. The previous recruitment, traffic, Companion and events tests continue to check the limitations of siege; we do not replace them with tests of the new module itself. acceptance S12 interface remains in UI-07.

Unity T19 regression: **27/27 EditMode**, `.artifacts/T19-edit.xml` , end 2026-10-05 04:42:32 UTC ; **19/19 PlayMode** of Kestrel/PostgreSQL ed `scripts/test-client.sh` , `.artifacts/client-play.xml` , end 04:46:04 UTC No skip. acceptance and the new performance measurement was not performed.

<a id="interfejs-bitwy-ui-07"></a>
## battle UI-07 interface

Scope and distribution of real scenarios from fixtures: [S09–S12](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/SCENARIOS.md), [UI-07](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/UI-07-BATTLES.md). `BattleDraftTests` adds four cases of reversible, deep copy of losses, open selection of cards/post, compliance with click/stitch and maintain the retreat target. `BattleReportTests` checks the audience of the report, no private reroll, read after the end of battle, and no shared references of DTO to the server state.

Results: **305/305 . NET**, `bash scripts/test.sh --no-restore` ; **31/31 EditMode**, `.artifacts/UI07-edit.xml` , end 2026-10-05 05:17:09 UTC ; **26/26 full PlayMode**, `bash scripts/test-client.sh` , `.artifacts/client-play.xml` , end 05:26:00 UTC . Without omission. Two previous tray overflows were improved with a common footer and a fixed throw bar; multiple test logins were replaced by re-use of actual sessions, keeping the production limiter. Manual acceptance , new standalone build and measurements FPS they are outside of this check.

After the last correction of the upper opponent's pool and closing the report on the game change: **5/5 PlayMode** for `BattleLayoutTests;TurnLayoutTests;FellowshipLayoutTests` , `.artifacts/UI07-layout.xml` , end 05:33:12 UTC The matrix also checks the dice the opponent, the actual height of the two-line title/story, five-score groups of a longer special attack and the preservation of the site for approval with many eliminated characters. `battle-RerollBattle-1366-1.25.png` and `battle-ExtendSiege-1920-1.25.png` In `.artifacts/screenshots` .

<a id="prywatny-skrót-kart-ui-11"></a>
## Private Shortcut cards UI-11

[S13](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/SCENARIOS.md) and [project](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/UI-11-AUTO-DECISIONS.md) are the range. domains/persistence tests check both roles, manual and automatic lack of card, order, hand change in effect, ending and next battle, versions CAS, retry, restart, rollback, both order of the cancellation race, confidentiality of projection/receipt and addressing private notification. The revised `BattleCommands` continues to undergo a full regression of previous battles and effects.

Results: **317/317 . NET**, `bash scripts/test.sh --no-restore` , no skip; **33/33 EditMode**, `.artifacts/UI11-edit.xml` , end 05:56:14 UTC ; **28/28 full PlayMode** with the actual server by `bash scripts/test-client.sh` , `.artifacts/client-play.xml` , end 05:53:40 UTC . Compilation of the initial new test and its incomplete test directory required amendments; final full . NET pass. No change in the manifesto implemented cards based on the test effect.

Manual acceptance user, current standalone and new measurements FPS were not performed. The final layout matrix after the concentration of three resource counters has a separate `UI11-layout.xml` result.

The final `BattleLayoutTests`: **2/2 PlayMode**, zero skip, `.artifacts/UI11-layout.xml`, end 05:57:38 UTC. Viewed the `battle-RerollBattle-1366-1.25.png` and `battle-CommitCombatCard-1920-1.00.png`: all 10 own dice, three Rings, the Settings button and Round Information are located in the panel; the tray does not need scrolling. The editing and regression of the client used Unity 6000.6.0f1 on macOS/M1 Pro.

<a id="postacie-t20"></a>
## Characters T20

[Specification and sources](rules/T20-CHARACTERS.md) , [14 cards manifest and every ability](../content/character-implementation-manifest.json) . `CharacterEntryTests` , `CharacterActionTests` , `CharacterTravelTests` , `CharacterHuntTests` , `CharacterReactionTests` and `CharacterBattleTests` check the entry conditions, Versions/stable ID , no re-entry Minion , dice when recovering, traffic restrictions and siege , both versions of Voice and reserve shortages, three applications of the Messenger cost and reaction window. The tests include, among others, no Captain u White/Hobbit cancelled card and the premature end of the round for Sorcerer, the death of Witch-king during the reduction of the elite, the change Guide during the event, the hand limit of six cards , a group of two Hobbits and a return from outside Hunt .

`CharacterPersistenceTests` launches the actual hosts and PostgreSQL between the transformation, Grey selection and Hobbit deployment; the command re-sustains no duplicate effect, and the opponent does not receive the selected card. `CharacterManifestTests` checks the compatibility of all capabilities with the source directory and the existence of the tests indicated. Manifesto is a cover map, it does not replace the performance of tests.

Domain Results: `bash scripts/test.sh --no-restore`, **375/375 .NET**, zero skip, `.artifacts/T20-final.log`. Unity: **37/37 EditMode**, `.artifacts/T20-edit.xml`, end 2026-10-05 07:10:51 UTC. Four `CharacterDraftTests` include version, flight without intermediate clicks, fixed group, Shadowfax and public choice Messenger.

Screenplays `CharacterNetworkTests` use four saved batches of `CharacterScenario`: entrances White/Aragorna/Witch-kinga And a joint flight, a private selection of Grey after reconnect, a chain Merry/Pippin/Gollum and White Rider with public Leadership.  `MatchTestRoom` sharing the assembly of two clients also with tests battle.  `CharacterLayoutTests` checks the entrances, capabilities and all 11 separate groups Shadow in 1366 × 768/1920 × 1080 on a scale 100% / 125%, without scrolling and number fields. Early runs found a cut of text and footer; improved padding/high Cut buttons and compact group selector.

Final full PlayMode real Kestrel/PostgreSQL:  **33/33**, no skip, `bash scripts/test-client.sh`,  `.artifacts/client-play.xml`, End **2026-10-05   07:16:32   UTC**. The entrance and everyone were watched 11 groups, especially `characters-MoveShadowCharacters-1366-1.25.png`; Full Layout matrix and earlier panels cards/movement/battle passed. The higher minimum button is limited to the character panel to keep the site in dense loss views. The review of the structure maintained 12 small Characters domain files, separate drafty/UI and the existing common pile of effects. `format whitespace`, validation of tasks, directory of rules and sources passed. Manual acceptance user, new standalone builds and measurements FPS were not performed in T20.

<a id="t21-fp-char-01--pierwsze-sześć-pełnych-kart"></a>
## T21-FP-CHAR-01 — first six full cards

[Rules, examples and sources](rules/T21-FP-CHAR-01.md) , [manifest of both applications](../content/implementation-manifest.json) . `HuntProtectionCardTests` checks four grand before/after Mordor, all costs, the cycle of both cards on the table, the expiration of Axe after separation and loss, Shelob from throw, replacement + protection, additional tile after disclosure and empty pools. `CompanionCombatCardTests` includes four Gifts in attack and defense, natural hits and modifiers, Blade after Hobbit elimination and real choice Minion , Mighty with two groups of candidates and ban reroll/cancelled Leadership . `CardPackPersistenceTests` rebooting HTTP/PostgreSQL before and after decisions and repeats the same receipt without re-introducing RNG , cost and elimination.

Full .NET: **413/413**, zero skip, `bash scripts/test.sh --no-restore`, `.artifacts/T21-01-final.log`. The first regression indicated four old scenarios allowing the re-match of the tile from the same action. Source confirmed the `spent`/`return-after-action` cycle required separate physical tiles in these scenarios. No change in expected injuries or order of decision was made.

Unity EditMode: **38/38**, `.artifacts/T21-01-edit.xml`, end **07:42:42 UTC**. The new draft test selects after one position from two groups, changes Companion unchanged dice, withdraws and rejects an incomplete set.

Full real PlayMode: **37/37**, zero skips, `bash scripts/test-client.sh`, preserved copy `.artifacts/T21-01-play-full.xml`, end **2026-10-05 07:56:53 UTC**. Three new scenarios connect two clients at Mithril/Axe, Mighty and Blade. Layout control uses seven Companion and five boxes, both resolutions and both scales, without przewijania/wpisywania. The first runs detect no space and no height rounding at 125%; short names with detail on request and 33 px limited to cards in battle remove both problems. The `.artifacts/screenshots/mighty-attack-1366-1.25.png` image is over half screen and the Confirm remains visible.

After the review, the Aragorn fixture was improved: it retains ID Striper figures, changing its version, without a second identity in Fellowship. Again completed full **413/413 .NET** and **3/3** Network scripts of the packet, `bash scripts/test-client.sh -testFilter Wotr.PlayTests.CardPackNetworkTests`, end **07:59:01 UTC**. The script takes additional arguments Unity, keeping a full run without parameters. The client shows “Aragon” for ID Striper and uses two points Leadership.

Structure overview: four `Domain/Cards` files, after 40–58 line, separate the packet register, Hunt steps and battle steps. The directory is indexed once, options do not generate the combination of Companion × die, the draft does not rebuild the trays after selection. Formatting and validation of sources/tasks have passed. Build standalone, new measurements of FPS and manual acceptance user did not belong to this check.

<a id="t21-fp-char-02--karty-712"></a>
## T21-FP-CHAR-02 — cards 7–12

[Rules, order and sources](rules/T21-FP-CHAR-02.md) , [manifest](../content/implementation-manifest.json) . `FellowshipEventCardTests` and `FellowshipTravelCardTests` include ownership/costs, expiration Horn/Staff prevention of selection before RNG , Mordor, extra tile Stronghold separation Staff/Mithril , Atelas frets, all branches of Another Way and forced separation with bonus. `LeadershipCombatCardTests` and `CasualtyCombatCardTests` check both role draw initiative, full cost of Daring, White Rider, no reroll, choice 0/1/2 conversions, partial cancellations of hits, optional sacrifices, presence at play and eliminations at the initiative 6 Preview tests confirm no state modification and no unknown designation Hunt After choosing the move.

`CardPack02PersistenceTests`: seven scenarios of real host and PostgreSQL, reboot before and after the decision, retry by third host, confidentiality of election, no re-kosztu/RNG/leczenia/eliminacji. Complete full domain run: **471/471, zero omitted**, `bash scripts/test.sh --no-restore`, `.artifacts/T21-02-domain.log`. Validations of 87 tasks, 30 rule families, 32 windows, 96 cards and 14 characters have also passed.

Unity EditMode: **40/40**, `.artifacts/T21-02-edit.xml`, end **2026-10-05 08:28:06 UTC**. Two new tests check the selection of many dice, a standalone alternative, the dependence of hit numbers on figure and the reversible draft. First full PlayMode: **43/44**, end **08:35:07 UTC**; all six new network scenarios passed, the test of the extreme large selector detected a vertical layout of bookmarks going beyond the screen. The direction of the system in the selector style has been corrected; the result will be saved below.

Final full PlayMode as corrected by: **44/44, zero left out, `bash scripts/test-client.sh`,  `.artifacts/T21-02-play-full.xml`, End** 2026-10-05   08:47:17   UTC **. Separate Layout control** 1/1 **ended by** 08:42:28   UTC ** and was again performed in full regression. Images `heroic-effect-tab-*.png` and `heroic-death-*.png` include every nation, all seven Companion and 20 Commanders, personnel are under surveillance. 1366×768 and 1920×1080 with 125%; hit and Confirm symbols are located in one line, the bookmarks do not change the choice, the map takes more than half of the screen. With smaller sets the commanders have short symbols with full description on request.

The structure review retained separate cards programs, shared Hunt/Dru proceduresA lynch/battle and small steps, without engine dependence on Unity. The Draft and compact figures selector have separate files; the line construction in `CardChoices` is separated from the button creation. The preview uses the directory supporting command. The figure × number of hits in options or the whole tray on a single selection. The function length, limited list of candidates and file structure has been reviewed; formatting and validation of documents and sources past. The new standalone, FPS and manual acceptance measurements have not been performed in this package.


<a id="t21-fp-char-03--podróże-polityka-i-orły"></a>
## T21-FP-CHAR-03 — Travel, Politics and Eagles

[Rules and sources](rules/T21-FP-CHAR-03.md) , [manifest](../content/implementation-manifest.json) Six events and both applications cards 13–18 are associated with the tests. `MirrorAndChallengeTests` check cost, availability dice treatment, condition Stridera/Aragorn , 0–3 Eye, special eye, pool replenishment and no reaction Hunt . `CompanionJourneyCardTests` and `ThereAndBackCardTests` include one group, a permanent level/bonus/Shadowfax , legal edges, siege as goal/start Narrow/obstacle, Mordor, None Companion change Guide And politics. `EaglesCardTests` checks both armies, shut off Witch-king, thresholds/limit dice selection Shadow , an illegal actor, fortresses and privacy trace elections. `LeadershipAttackCardTests` checks all conditions of attendance, initiative with the priority of the defender, special attack thresholds, immediate casualties/End battle and Brave Stand on canceled Leadership .

Seven `CardPack03PersistenceTests` saves and reproduces decisions by subsequent actual hosts HTTP/PostgreSQL decisions Shadow inside the event FP and losses of Sudden Strike. Retry before and after the next reboot does not repeat the cost, throw, treatment, policy or displacement. A common helper compares projections of both sides after the reboot. Full server: `bash scripts/test.sh --no-restore` * 542/542 **, zero skip, `.artifacts/T21-03-domain.log` The former T15 test allowed activation in the acquired settlement : the expectation and specification have been corrected in accordance with BASE p. 32 , Chapman's explanation and Ring-bearers' entry in Almanac; the regression of the declaration and the ordinary arrival has been preserved Companion .

Unity: Two new `CardJourneyDraftTests` check the limit of one Groups/End route to siege and drag dice as a choice of effect. **42/42 EditMode**, `.artifacts/T21-03-edit.xml` Six `CardPack03NetworkTests` uses authentic Kestrel/PostgreSQL: Mirror in the pool dice Gwaihir to the garrison, six steps Shadowfax/Swifter with reconnect, There and Back Again after Guide purpose FP/withdrawal Shadow After Eagles and special losses Sudden Strike before a regular throw. No switch was found in the tests Separate/Move and lose the second quick click after the tray has been rebuilt. The switch occupies the header, and one group retains controls between the selections.

`CardPack03LayoutTests` checks the selection of the region and dice in 1366×768 and 1920×1080 on a scale 100% / 125%, without scrolling, text fields and duplicating map targets in tray. `pack03-Region-1366-1.25.png`; eight staff were saved. Fixed to round the approval height to less than 32 px. Focused PlayMode: **7/7**. Full `bash scripts/test-client.sh`: **51/51**, no skip, `.artifacts/T21-03-play-full.xml`, End **2026-10-05   09:34:37   UTC**. After this process, the only change of client was to shorten the description of the movement of one group; the compilation/re-checked EditMode No new standalone, measuring FPS or manual acceptance user.

Structure overview: a package register separated from small steps families (Mirror/Challenge, travel/politics, Eagles, two combat effects). A common stack and planner are the only source of rules; UI does not calculate legality on its own cards texts. A common preparation scenario battle has been separated from the 02 package, without copying the setup.

<a id="t21-fp-char-04--enty-umarli-i-rekrutacja-gondoru"></a>
## T21-FP-CHAR-04 — Enty, Dead and recruiting Gondor

[Sources and rules](rules/T21-FP-CHAR-04.md) , [manifest](../content/implementation-manifest.json) . card 19–24 full-use closure 24 cards Character FP . `EntsCardTests` includes conditions, thresholds, policies, both sides siege , single Saruman, degradation/elimination, commanders and chain of three Ents with one additional card for one die . `DeadMenCardTests` checks both versions of the king, leaving siege , optional group, illegal figures/targets , mandatory retreat, no retreat, excess Shadow recruitment 0/1/3 and acquisition settlement Only when you're recruiting. `GondorRecruitCardTests` checks regions and reserves, both sides siege , the atomicity of the exchange and the order of two cards , rejection and selection of Grey.

`EntsCombatCardTests` and `CardPack04ValidationTests` include cost/window all cards, terms of both halves, region defender, natural results and difference Combat/Leader Re-roll, Huorn-dark limit, additional hits Nameless Wood, no limit of five and no special attack. Added regression of the extra dice after eliminating the character in the usual battle; previously the use of copied figures did not refresh this player field.

Four `CardPack04PersistenceTests`s pass through the actual hosts of HTTP/PostgreSQL and all new decision families: event losses, free play, Groups/target selection, Shadow retreat, recruitment and replacement. Each window is saved and played; both projections, retras before and after the next reboot are compared, it does not repeat effects or RNG. Full `bash scripts/test.sh --no-restore`: **605/605**, zero skips, `.artifacts/T21-04-domain.log`. The previous course of 604/605 reported only no new manifesto entries; after adding them the entire regression went through.

Unity First Focused: **5/5 network scenarios**, one system error (rounding 32 to 31,8655 when scaling). Adjusted height of event tray buttons to 33 and symbols of the dense list of Grey Company. `CardPack04NetworkTests` executes losses with degradation, chain without dice, full Dead Men with handing over the decision to both players and taking over settlement and both recruitments. Reconconnect retains local choice of losses and group. `CardPack04DraftTests` check to click/ drag free card and single optional selection Companion.  `CardPack04LayoutTests` includes losses of the figures, six optional Companion, three recruits and the replacement candidates, at 1366 × 768/1920 × 1080 and 100% / 125% Full. PlayMode after correction: **57/57**, no skip, `.artifacts/T21-04-play-full.xml`, end 2026-10-05   10:22:32   UTC First full run 55/57 revealed a overflow of two earlier panels after a global change in height; the adjustment was limited to cards and losses of events, after which the entire regression went through. After formatting **44/44   EditMode**,  `.artifacts/T21-04-edit.xml`, end 10:22:57   UTC Saved. 16 frame `pack04-*`, the journey, losses and dense exchange at 1366 × 768/125%; the reconnect message is the state of the isolated system test.

Structure overview: cards registry separated from four small families steps, common `ArmyLossRules` instead of the second loss algorithm, separate `EventLossProcedure` snapshot. Recruitment law shared with the previous Celeborn. UI uses existing figures, cards pool and map. There is no new stand-alone, FPS measurement or manual acceptance user.

<a id="t21-fp-str-01--ochrona-regionów-book-i-scouts"></a>
## T21-FP-STR-01 — Protection of Regions, Book and Scouts

[Specification and sources](rules/T21-FP-STR-01.md) , [manifest](../content/implementation-manifest.json) . `LastBattleCardTests` , `RegionProtectionCardTests` , `MazarbulCardTests` , `StrategyArmyCardTests` , `StrategyCombatCardTests` and `Strategy01ValidationTests` check both applications of six cards , cost/window/illegal target, partial reserves, no effect, immediate disclosure and end condition action , protection movement/storm/retreat , independent groups, ordinary events loss, initiative/cancellation Scouts, overcrowding and shelter and real reroll hits. Joint Ents modifiers are subject to full regression.

`Strategy01PersistenceTests` includes six tests with eight restart windows PostgreSQL/HTTP: paid security removal, Last Battle movement, Book, Spirit target selection and loss, losses and recruitment Faramir, Scouts. Each rerun retains costs, draw result and views of both sides.

Domain result: **670/670 . NET**, no omissions ( `bash scripts/test.sh --no-restore` , `.artifacts/T21-FP-STR01-domain.log` ); including 65 new cases. First correct integration of new Unity scenarios: **6/6 PlayMode** ( `bash scripts/test-client.sh -testFilter Wotr.PlayTests.Strategy01` , `.artifacts/T21-FP-STR01-focused-play.xml` , end 2026-10-05 10:57:02 UTC Two initial failures involved a synthetic click before the new control system was converted; the tests are now waiting for the frame UI , like earlier scenarios. No rule's legality changed to a test. Full client regression: **63/63 PlayMode** ( `bash scripts/test-client.sh` , `.artifacts/T21-FP-STR01-full-play.xml` , end 2026-10-05 11:01:32 UTC ) and **46/46 EditMode** ( `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/T21-FP-STR01-edit.xml -logFile .artifacts/T21-FP-STR01-edit.log` , end 11:02:23 UTC ), without omissions.

`Strategy01LayoutTests` renders 12 protection systems, seven Companion Book and Scouts (1366×768/1920×1080, 100/125%): minimum control 32, visible approval, missing ScrollView/TextField. `.artifacts/screenshots/strategy01-*.png` files; a visual overview of the smallest security systems, Book and Scouts was performed. The reconnect marking in these shots comes from a deliberately local fixture, the actual connection checks `Strategy01NetworkTests`. Without new measurement of FPS, the builds stand-alone or manual user test in this package.

<a id="t21-fp-str-02--marsz-woses-odsiecz-i-confusion"></a>
## T21-FP-STR-02 — march, Woses, rescue and Confusion

[Specification and sources](rules/T21-FP-STR-02.md) , [manifest](../content/implementation-manifest.json) . `ArmyEventMovementTests` , `WosesCardTests` , `HelpUnlookedForTests` , `Strategy02PoliticsTests` , `Strategy02CombatTests` and `Strategy02ValidationTests` : both functions of each card , conditions/costs/windows illegal figure and routes, national boundaries, exit of the whole/part of the garrison, both directions siege Minas Tirith, sediment control along the way, final excess, mandatory Companion , policy without activation, two rounds will save, Timing Gandalf the Grey and natural only Confusion. `Strategy02PersistenceTests` re-creates march, Woses, nested rescue and both approvals cards , Fear, Red Arrow and both Confusion's metastatic choices with PostgreSQL/HTTP/retry and a projection of both sides.

`bash scripts/test.sh --no-restore`: **729/729**, zero omissions, `.artifacts/T21-FP-STR02-domain.log` (59) new cases. `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/T21-FP-STR02-edit.xml -logFile .artifacts/T21-FP-STR02-edit.log`: **48/48**, end of 2026-10-05 11:32:13 UTC. New draft tests check out the open route, no additional dice, undo covering the group/route and rejection of the route obsolete after the reconnect.

First Focused PlayMode : **4/4 network scenarios passed**, system control required correction. It also checked zero size of items inside the inactive tab. The size control helper now omits the elements invisible by the ancestor, and the new test openly opens each tab and controls all figure After displaying. Another result revealed height 31,8655 on a scale 125% ; style 33 pixels has a local range `tray.event-army-planning` and does not change other panels. Final focused result: **5/5 PlayMode**, no omissions, `.artifacts/T21-FP-STR02-focused-play.xml` , end 2026-10-05 11:37:51 UTC Full regression after formatting: **68/68 PlayMode**, no omissions, `bash scripts/test-client.sh` , `.artifacts/T21-FP-STR02-full-play.xml` , end 11:42:26 UTC Four network scenarios use Kestrel/PostgreSQL and both clients; the arrangement includes 12 variants and switching of all the Army tabs. Saved `.artifacts/screenshots/strategy02-*.png` , all three trays were watched at 1366 × 768/125%. The reconnect message comes from an isolated fixture of the system. No new measurement was performed FPS , standalone builds or manual user test in this package.

The regression also detected a production error of the previous package: `shire` instead of `the-shire`. The protection of Tom Bombay and the sources of UI/testy. Test is now compared to the legal traffic before and after the establishment of the protection and checks the existence of all protected regions.


## T21-FP-STR-03 — Strategy 13–18

Specification: [rules](rules/T21-FP-STR-03.md).  V16 includes each of the six cards: cost, property, window, legal and illegal targets, event/combat.   `Strategy03RecruitmentTests` checks the choice of two figures one command all combinations Common/elite, coercion of the maximum recruit, partial reserves, both sides siege, return excess of any nation, policy and condition Companion  Riders.  `BoardTests` and `Strategy03ValidationTests` They're checking all of them. 17 Coastal regions (Lamedon confirmed and added during T21-SH-STR-02), lack of army, clean preview and privacy options. `Strategy03CombatTests` includes the current Charge elites, limit of five, unmodified hit thresholds, immediate losses, a draw of initiative, completion battle and cancellation; other uses use verified Daylight/Scouts/Shield-wall. Existing `EventTests` / `CharacterReactionTests` protect the order of the selection of Celeborn, the limit of the hand and Grey.

`Strategy03PersistenceTests` executes restart/retry of any type of decision of this package, including the selection of the region, both recruits, immediate losses and Scouts. The command `bash scripts/test.sh --no-restore` passed 793/793 .NET/HTTP/PostgreSQL without omission; the write `.artifacts/T21-FP-STR03-domain.log`. Focused attempt before additional cases V16: 63/63.

Unity EditMode : 50/50 , no omissions, `.artifacts/T21-FP-STR03-edit.xml` ( 2026-10-05 12:04:01 UTC Two new tests check multiple selection, the limit of readiness, Selection/undo , reconnect and remove the outdated spirit figure Focused. PlayMode : `bash scripts/test-client.sh -testFilter Wotr.PlayTests.Strategy03` , 4/4 , no omissions, `.artifacts/T21-FP-STR03-focused-play.xml` ( 12:05:43 UTC Three scenarios go through the actual Kestrel/PostgreSQL and two clients. Layout performs eight variants: coast and recruit selection × 1366/1920 × 100/125% , controls buttons and missing scrolla/p number salt. Coast/recruit discharges were reviewed 1366 × 768/125% ; `.artifacts/screenshots/strategy03-*.png` Offline fixture shows a reconnect message; it is not the result of a network test.

Structure overview: separate definitions of cards, region selection and Charge effect; recruitment shared by all previous packages. Functions do not list subsets of figures, recruitment options have up to four candidacy options. New FPS measurement, standalone or manual builds acceptance user not performed in this package. Full `bash scripts/test-client.sh`: 72/72 PlayMode, zero omissions, `.artifacts/T21-FP-STR03-full-play.xml` (2026-10-05 12:10 UTC). Also included after shortening the recruitment header for Choose reinstructions. Validators tasks/catalog/source_cards past.


## T21-FP-STR-04 — Strategy 19–24

[Specification](rules/T21-FP-STR-04.md) . `Strategy04RecruitmentTests` check regular Brand and no spare parts, four pairs regular/elite Words in one command , requirements of both groups, clean preview of two targets, partial lack of reserves/haj hair settlement , goals and nations and four cases of Helm’s Deep for Éomer. Selection works despite lack of recruitment and an empty deck is not shuffled again. `Strategy04ValidationTests` includes cost/In the tickle/window event and fight each card . `Strategy04CombatTests` checks the Valour bonus only for the first throw, natural one, lack of elite at play, retain the bonus after her previous death, cancel and actual effects Advantageous Position and Shield-wall.

`Strategy04PersistenceTests` plays before/after command every new decision, including Words → selection → Grey → hand limit, map region Éomera and Valour before the reroll. `bash scripts/test.sh --no-restore`: 844/844 .NET/HTTP/PostgreSQL, without omissions, `.artifacts/T21-FP-STR04-domain.log`; the focused attempt 51/51. Manifest contains 49 events and 48 full cards: the whole FP.

Unity EditMode : 51/51 , without omissions, `.artifacts/T21-FP-STR04-edit.xml` ( 2026-10-05 12:24:16 UTC The new test protects independent groups, two ghosts, undo and reconcile. Focused PlayMode after correction: 4/4 , no omissions, `.artifacts/T21-FP-STR04-focused-play.xml` ( 12:26:57 UTC Full). `bash scripts/test-client.sh` : 76/76 PlayMode , no omissions, `.artifacts/T21-FP-STR04-full-play.xml` Three new scenarios use Kestrel/PostgreSQL and two clients; layout checks 1366/1920 × 100/125% , minimum buttons and no scrolla/p So, the first focused attempt is going to be 3/4: test Swords mistakenly assumed an empty hand of the script that starts with two card ; improved expectation of an increase in number cards one relative to the initial condition.

The Swords 1366×768/125%: dropout is readable, the tray does not require scrolling. `.artifacts/screenshots/strategy04-swords-*.png` drops. The review of the structure has separated the joint recruitment, leaving the definitions cards short; the recognition of Companion is in CharacterRules, and the setup of 02/03/04 scripts shared. In this package no new standalone builds, measuring FPS or manual user tests were performed.

<a id="character-cienia-16--t21-sh-char-01"></a>
## Character Shadow 1–6 — T21-SH-CHAR-01

[Rules and sources](rules/T21-SH-CHAR-01.md) , [manifest](../content/implementation-manifest.json) . `Shadow01EventTests` , `Shadow01HuntTests` , `Shadow01CombatTests` and `Shadow01ValidationTests` check both applications of all six cards , settlement according to original owner, costs, four special tiles before/after Mordor, filter Eye/FP , Shelob, Staff/Mithril , protection and prohibition of reduction, Golluma, additional Stronghold and Corruption 12 Fight includes both roles, both throws, natural 1/6 , Witch-king, White Rider, assigning points to figures no cost after cancellation of the entire card and the mandatory cost despite the Advantageous Position. The cost projection is public and does not alias the state dictionary.

`Shadow01PersistenceTests` reboots real HTTP/PostgreSQL before paying each card , in prevention Selection/exchange/protection/loss/new Guide , disclosure and additional Stronghold and at the cost of Nazgûla and both private reroll. command is re-established before and after the reboot; does not repeat RNG No cost. `NazgulLeadershipDraftTests` checks two points of the same figure , borderline of choice, Back and reconciliation after changing options. `Shadow01NetworkTests` conducts three flows through two clients, and `Shadow01LayoutTests` check the points and direct Corruption in matrix 1366 × 768/1920 × 1080 × 100/125%.

The first compilation test had outdated names SelectGuide/Guide, and the initial expiry test expected the round 2 before the Sorcerer reaction; the scripts were corrected. The full regression revealed the old stop wait when calling without motion: the infrastructure test extended with a real motion test and an event without motion, as per the text cards. The first Unity compilation contained a non-existent field name in the unnecessary test Assertion; this alignment was removed. The dumps showed the need for an open cost header and public assignment dedicated to Leadership; the projection and the tooltip of forces before acceptance were developed.

Final .NET/HTTP/PostgreSQL After refining the projection: **917/917**, without omissions, `bash scripts/test.sh --no-restore`,  `.artifacts/T21-SH-CHAR01-domain.log`  (22 (s) Final EditMode: **52/52**, no skip, `.artifacts/T21-SH-CHAR01-edit.xml`, End **13:06:43   UTC**. Full PlayMode before final refinement of the heading and projection of the cost: **80/80**,  `.artifacts/T21-SH-CHAR01-full-before-review.xml`, End **13:04:13   UTC**,  212,7 p. After this change: **15/15   PlayMode**, without omission, filter `Wotr.PlayTests.Shadow01;Wotr.PlayTests.CardPackNetworkTests;Wotr.PlayTests.CardPack02NetworkTests;Wotr.PlayTests.BattleLayoutTests`,  `.artifacts/T21-SH-CHAR01-review-play.xml`, End **13:08:06   UTC**,  25,3 p. This is the actual server Kestrel and PostgreSQL; the result includes a new card, earlier costs/casualties combat and layout battle. Initial Focused Package 4/4 is in `.artifacts/T21-SH-CHAR01-focused-play.xml`.

Drops read `shadow01-cruel-1366-1.25.png` and `shadow01-bane-1366-1.25.png` ed `.artifacts/screenshots` all 8 Layout combination passed. Reconnect is deliberate in disconnected layout fixture; network tests check active confirmation. Formatting and overview of structure leave 34 -row directory cards , a specific condition for the region and 57 -row cost step; contractor Hunt continues to divide stages into short methods. No new measurement was performed FPS neither builds standalone/r uncommon acceptance The user in this package.

<a id="character-cienia-1318--t21-sh-char-03"></a>
## Character Shadow 13–18 — T21-SH-CHAR-03

[Rules and sources](rules/T21-SH-CHAR-03.md) , [manifest](../content/implementation-manifest.json) . `Shadow03EventTests` , `Shadow03BreakingTests` , `Shadow03HuntTests` , `Shadow03BalrogTests` , `Shadow03CombatTests` , `Shadow03InteractionTests` and `Shadow03PersistenceTests` check six full cards The scope covers both applications, costs and illegal windows, Golluma/Hobbit This/Mordor, replacing the tile, distinguishing the separation from the victim, privacy of the Worm, RNG After Crebain, termination at the declaration, Balrog's entire route, secrecy card from the table, Words/White Rider/Captain/And úril/ Daring/Brave Stand/Heroic Death and Durin’s Bane initiative draw. Restart PostgreSQL and retras include actual selection windows, public reactions and secret Balrog. Additional scenarios of both gates check the ban on entering the track until the end of the declaration and the re-enactment of the pool only after Enter Mordor.

- `bash scripts/test.sh --no-restore`: **1090/1090**, zero omitted;
  `.artifacts/T21-SH-CHAR03-domain.log`. After adding two gate tests: `--filter
  'FullyQualifiedName~Shadow03'`, **80/80**, `.artifacts/T21-SH-CHAR03-final-focused.log`. The first
  attempt of new tests detected a faulty tile condition in the fixture, corrected before crediting.
- `bash scripts/test-client.sh -testFilter
  'Wotr.PlayTests.Shadow03NetworkTests;Wotr.PlayTests.Shadow03LayoutTests'` : **9/9**,
  `.artifacts/T21-SH-CHAR03-focused-play.xml` full `bash scripts/test-client.sh` : **96/96**, zero
  omitted, `.artifacts/T21-SH-CHAR03-full-play.xml` ( 14:50:34–14:54:38 UTC Eight scenarios, two
  clients each and the actual Kestrel/PostgreSQL. Separate 32 systems: 1366 × 768/1920 × 1080 ×
  100/125% , with full hand; `.artifacts/screenshots/shadow03-*` No evidence. ScrollView/TextField
  And the tray geometry, the eight narrowest variants were viewed.
- First PlayMode **6/9**: Worm crossed the area by 2,4 px at 125% , and two tests clicked before
  the reaction panel was rebuilt. Short names with full inspection and waiting for a test for the
  new panel fixed all three cases. `.artifacts/T21-SH-CHAR03-first-play.xml` And log.
- `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults
  .artifacts/T21-SH-CHAR03-editor.xml -logFile .artifacts/T21-SH-CHAR03-editor.log`: **57/57**, zero
  omitted (14:55:08–14:55:09 UTC). New draft tests include multiple dice selection and an exact
  Companion selection with preview and playback after revision.
- Formatting both projects, `tasks.py validate`, `check_catalog.py` and source control cards pass.
  Manifesto includes **67 events and 66 complete cards**. Structure overview: small steps in Cards,
  common effect removal in Effects, state and privacy of commitments in Battles, serialized sequels
  in Fellowship/Hunt. No new domain dependencies from Unity/transportu.

Manual user tests, measurements of FPS and release builds were not performed in this package; they have separate tasks. The control of the current errat was later performed in [SRC-08](sources/SRC-08-AUDIT.md), closing GAP-FAQ after comparison of the instructions 2.4.1.

<a id="character-cienia-712--t21-sh-char-02"></a>
## Character Shadow 7–12 — T21-SH-CHAR-02

[Rules and sources](rules/T21-SH-CHAR-02.md) , [manifest](../content/implementation-manifest.json) . `Shadow02EventTests` , `Shadow02HuntTests` , `Shadow02TravelTests` , `Shadow02StrikeTests` , `Shadow02CombatTests` , `Shadow02BlackBreathTests` and `Shadow02PersistenceTests` include six events and all half-war. settlement according to the original owner, costs/actor This/window, random victim after protection, Hobbits/Golluma, replacement/prevention of selection, independent additional Stronghold , direct contamination and victory. Flight checks WK/Mouth , empty choice, groups, duplicates, null- ID , siege and no second dice Strike checks the public filter cards , removal of permanent effects, optional termination, bonus/rolls/rerolls The fight checks the cost 1 .. 10 points, large armies after the five-lime, Captain/Brave Stand, White Rider, Daring Defiance, Retainment of the Metastatic Bonus against Advantageous Position, Sudden Strike before Stench and four effects dependent on the cancelled metastatic. Black Breath includes both roles, level thresholds, additional commander/ Companion the refusal, absent/eliminated figure and a draw of initiative with Heroic Death.

Six tests PostgreSQL restarts hosts before paying each card and with all new types of windows, compares both projections and renews command before and after the reboot. The first run revealed no presentation CharacterEvent in the projection; repaired with automatic opening draft Unity. Initial errors of the script itself: incorrect ID north-dunland, no army besieging Rivendell, White level= 3 , the mistaken obverses of Sudden Strike/And úril and no Hobbit for Blade. The anticipation of Advantageous Position: lowers Combat Roll, not metastasized. Full regression after these corrections: **1012/1012 .NET/HTTP/PostgreSQL**, no skip, `bash scripts/test.sh --no-restore` , `.artifacts/T21-SH-CHAR02-domain-complete.log` ( 26 (s)

`Shadow02NetworkTests` checks six flows by Kestrel/PG and two clients: flight → disclosure FP, reconnect → Strike → rejection, Weather on the map, the Dread points, one Terrible point and an additional loss of Black Breath. PlayMode   3/7 detected the actual overflow of the short list of Black Breath; aligned figure in one row, and long lists use tabs. Other test errors concerned the target buttons not used on the flight to the entire map and too quick click after the group's redevelopment — the tests use the existing region's indication and wait for the panel to be updated. 7/7 **Focused PlayMode,  `.artifacts/T21-SH-CHAR02-focused-play.xml`, end 13:50:09   UTC. Review of images detected for a long sign of refusal in the tabs: “Keep all” is located next to the nations; an assertion of text width has been added. PlayMode after correction:** 87/87 **, no skip, `bash scripts/test-client.sh`,  `.artifacts/T21-SH-CHAR02-full-play.xml`, End** 13:56:58   UTC **,  229  s.

After a full run, a spatial Weather preview was developed: public target metadata gives a separate tag and arrow, without shifting the state before approval. After this change **95/95** tests domains/HTTP/PG  (`--filter FullyQualifiedName~Shadow02`),  `.artifacts/T21-SH-CHAR02-domain-preview.log`; **11/11   PlayMode**, without omission, filter `Wotr.PlayTests.Shadow02;Wotr.PlayTests.FellowshipLayoutTests;Wotr.PlayTests.CardPackLayoutTests;Wotr.PlayTests.Shadow01LayoutTests`,  `.artifacts/T21-SH-CHAR02-preview-play.xml`, End **14:01:49   UTC**; final **55/55   EditMode**,  `.artifacts/T21-SH-CHAR02-final-edit.xml`, End **14:03:45   UTC**. Checked 24 discharges of six states; visually read all six at 1366 × 768/125%, including improved Black Breath and Weather. Layout test includes 1366 × 768/1920 × 1080 and scales 100/125%, all Black Breath target bookmarks, nine flight groups, four card table and maximum cost the points. pinned Inspection in the dumps comes from an isolated fixture; active confirmations check network scenarios. No new standalone builds were performed in this package, measuring FPS or manual acceptance user.

<a id="t21-sh-char-04--ostatnie-sześć-character-cienia"></a>
## T21-SH-CHAR-04 — last six Character Shadow

[Rules, sources and boundaries](rules/T21-SH-CHAR-04.md) . `Shadow04EventTests` and `Shadow04CombatTests` check both applications, actor, cost and window of all six cards Further sets include counting figures They've reported and lost the event, three forced rounds of Grond, four for the elite, death. WK , limitation of the first card FP , full order Sorcerer/Jerk ír, Saruman's death, hand limit, paid for removing the Ring or Will, Wormtongue and postponed policy, lot/rekrutacj and one/two armies with the required commander. `Shadow04PersistenceTests` reboots three hosts for each command and repeats the receipt without RNG And the second cost. The control of the obverses detected the Strider icon misread in SRC-02 ; regression checks only North activation and the idmpotent correction of the previous record without changing Aragorn.

`bash scripts/test.sh --no-restore`: **1184/1184**, zero skip, `.artifacts/T21-SH-CHAR04-domain-final.log`, 37 p. Previous runs detected abnormal expectations for the moment of policy, inadequate RNG sequence in fixture, cost of Leadership requiring ID single points and wrong Strider icon. The final regression includes improved T11/T18/Ent/return tests and manifesto **73 events / 72 full cards**.

Unity: `Shadow04NetworkTests` assembles two clients from Kestrel/PostgreSQL. Verifies source/target on map, losses FP , Grond storm, reconnect selection, preview of the exact cost of Jerkír's removal, recruiting/ending Captain, two Ringwraiths armies and Rohan's activation by Gandalf. `Shadow04LayoutTests` uses a full set of server options providers and a six-card hand, checking nine contexts in two resolutions and two scales. Preliminary image review detected a lack of traffic provider in the layout generator; added it and the assertions of two plans, Attack and Finish. Early client testing detected region mapping on ID Army, too low trace buttons cards/Jerk íra and test click before calculating the layout. Focused score: **9/9**, `.artifacts/T21-SH-CHAR04-focus-play.xml` , end 15:53:12 UTC Final `bash scripts/test-client.sh` : **105/105 PlayMode**, zero skip, `.artifacts/T21-SH-CHAR04-full-play.xml` , 15:55:10–15:59:28 UTC ( 257,7 s). 36 The layouts include the actual choice of two armies and a set of options; the maneuvering staff, Dreadful, Grond and the removal of Jerkír in were viewed 1366×768 with 125% Final Editor: **61/61**, `.artifacts/T21-SH-CHAR04-editor-final.xml` , 16:00:42–16:00:43 UTC The structure review retained separate modules of rules, costs, policy queues, projections and local draft ; new operating methods are short. Manual user tests, standalone builds and measurements FPS were not performed in this package.

After the final review, ‘Skip maneuver’ clearly ignores the maneuver and marked ⁇ figure they have a symbol and a name. Repeated package **9/9 PlayMode** with the whole matrix 36 Label width systems and control: `.artifacts/T21-SH-CHAR04-final-focus-play.xml` , 16:02:54–16:03:12 UTC The character entry preview takes into account the deferred activation of the nation on a copy; regression `CharacterEntryPreviewIncludesDeferredActivationWithoutChangingTheLiveNation` It also compares the lack of change in the original state.

<a id="t21-sh-str-01--strategy-cienia-16"></a>
## T21-SH-STR-01 — Strategy Shadow 1–6

[Logic and sources](rules/T21-SH-STR-01.md) New classes `ShadowStrategy01EventTests` , `ValinorTests` , `DenethorTests` , `UrukTests` , `CombatTests` , `SacrificeTests` , `GreatHostTests` , `PreviewTests` and `PersistenceTests` (with prefix `ShadowStrategy01` ) cover both uses, conditions, incorrect command Seven tests. HTTP/PostgreSQL restore each new window through three instances of the server, repeats the receipt and checks the lack of another kosztu/RNG.

Full `bash scripts/test.sh --no-restore` : **1331/1331**, no omissions, 44 s, `.artifacts/sh-str01-full-domain.log` - Earlier attempt. 8 new cases detected a wrong region in fixture Valinor (Forlindon instead of Grey Havens); domain correctly rejected the condition. Also improved the Sorcerer window and number test ratings dice The review of the obverse has adjusted the selecting Commander Denethor to play Shadow .

Unity: 12 server state projection, four resolution/scale variants and nine network scenarios. First focus attempt: 17/18 (nine new network travel and eight SH04 regression included), `.artifacts/sh-str01-first-client.xml`. Full run 16:40:15–16:44:52 UTC: 114/115, `.artifacts/sh-str01-full-client.xml`. Control of the layout detected rounding of buttons heights from 32 to 31,86557: First cost of Denethor, then Downgrade in Relentless. The requirement of 32 px remains unchanged; after the style correction is re-verification. The review also detected the outdated Guide in two sessions after Gandalf's transfer; the amendment concerns consistency of the script, not the production rules of the game.

Second full PlayMode after the minimum height adjustment: **113/115**, `.artifacts/sh-str01-second-full-client.xml` , 16:51:45–16:56:09 UTC New height revealed the crossing of the lower border `Keep all` in two attempts of the system. Geometry requirements remain unchanged. **64/64 EditMode**, `.artifacts/sh-str01-editor-final.xml` , 16:50:58–16:50:59 UTC.

Final correction: `bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.ShadowStrategy01;Wotr.PlayTests.BattleLayoutTests;Wotr.PlayTests.CardPack04LayoutTests'` — **13/13 PASS**, zero skip, 16:59:37–17:00:11 UTC , `.artifacts/sh-str01-final-focused-client.xml` Includes nine new journeys by Kestrel/PostgreSQL , all 48 package layouts, both general system tests battle and regression casualties/recruitment FP Character04. The amendment reduced only the top padding trays loss from 6 ed 2 px, keeping the real objectives at least 32 px. `Keep all` occurs only with explicit voluntary losses; the simple loss test controls the lack of this option. Both full run failures have been re-done and counted; we do not present an earlier full score 113/115 as 115/115.

The final Onslaught and dense Stormcrow frames at 1366×768/125%, formerly Denethor1920×1080/125%: map, figures selector and approval are located without scrolling. Isolated layout scenarios have a reconnect message; nine network travel uses real connections. Structure overview: six new domain modules have 28–67 line; rules, costs of card removal, transport and presentation remain separated. Options do not generate combinations, local change figure refresh existing controls. No new standalone, FPS measurements or manual user tests were performed in this package.


<a id="t21-sh-str-020304--ostatnie-18-bazowych-kart"></a>
## T21-SH-STR-02/03/04 — last 18 base cards

Specifications [7–12](rules/T21-SH-STR-02.md),  [Corsairs](rules/T21-SH-STR-02-CORSAIRS.md),  [13–18](rules/T21-SH-STR-03.md),  [19–24](rules/T21-SH-STR-04.md) The Manifesto contains all 96   ID basic, separate programmes and initiatives Event/Combat and the actual test methods. Facing the manifesto does not replace acceptance Client.

Central fourth focus: `bash scripts/test.sh --no-restore --filter 'FullyQualifiedName~ShadowStrategy02|FullyQualifiedName~ShadowStrategy03|FullyQualifiedName~ShadowStrategy04|FullyQualifiedName~Corsair' --logger 'trx;LogFileName=shadow-strategy-final-focus.trx' --results-directory .artifacts/test-results` — **300/300 PASS**, zero omissions; `.artifacts/sh-str02-04-domain-fourth-focus.log`. Includes all conditions, objectives, costs, maximum partial execution, both applications cards and HTTP/PostgreSQL with reboots and reboots of each new window. Earlier failures detected inconsistent Westemnet control fixtures, required by the Combined Daring and the length of the determinist stream RNG; improved test preparation. The new Gater pass case by its own siege with the left guard is waiting for the next regression after the option generator has been corrected.

`bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/sh-str02-04-editor-first.xml -logFile .artifacts/sh-str02-04-editor-first.log` — **72/72 PASS**, including three sketches of separate routes and five draft group recruitment. The first `bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.ShadowStrategy02;Wotr.PlayTests.ShadowStrategy03;Wotr.PlayTests.ShadowStrategy04'` stopped during the construction of the Seed Swarm, before Unity Tests: selected card FP required a national region, and North Ithilien does not have it. Seed received the legal card Daylight; the score and full regression remains to be added.

Full regression after rear guard and manifest completion: `bash scripts/test.sh --no-restore --logger 'trx;LogFileName=shadow-strategy-full.trx' --results-directory .artifacts/test-results` — **1632/1632 PASS**, zero skips, 69 s; `.artifacts/sh-str02-04-full-domain.log`, `.artifacts/test-results/shadow-strategy-full.trx`. It also checks the set of programs of all 96 cards and the existence of the specified tests.

First Completed PlayMode new packages: **25/34 PASS**, `.artifacts/sh-str02-04-client-first.xml` , 17:39:42–17:44:03 UTC All the networked packages 04 over. Open correction: the geometry of headers of excess and dense plans, the layout of separate routes and three marches without sent command Two Corsairs tests selected non-existent button/ed • The new power test clicked on a new layout before the layout frame. Test fixes retain actual clicks and additional asertion of effective removal without bypassing the interface. The result is not acceptance No three packs.

Two more focused circuit sequences (without network, static actual server projections): `.artifacts/sh-str02-04-layout-second.xml` — **0/3**, all packages reached excess at 1920 × 1080/125% and rounding the approval amount 31,86554 px. `.artifacts/sh-str02-04-layout-third.xml` — **1/3**, the whole set 03 credited; in 02 and 04 the remaining identical generic rounding menu/Back battle Watching Moving and Pits at 1366 × 768/125% confirmed the visible routes, all planned figure and foot without scrolling. Criterion at least 32 px remains unchanged.

After optimization of the full march `bash scripts/test.sh --no-restore --logger 'trx;LogFileName=shadow-strategy-full-final.trx' --results-directory .artifacts/test-results`: **1638/1638 PASS**, no omissions, 62 s; `.artifacts/sh-str02-04-full-domain-final.log`. Six new tests compares the set of routes offered with the authoritative plan on the dense board. The result of the measurement and its limitations are recorded in [March specifications](rules/T21-SH-STR-02.md).

Fourth focus layout: **2/3 PASS**, `.artifacts/sh-str02-04-layout-fourth.xml` , 18:00:08–18:00:46 UTC Package. 02 She's got all the girls. 52 layouts, package 04 all 44 New ninth scenario 03 — six manually approved recruits and a later excess — revealed a footer on 829 px at the screen 768 px. Screenshot confirmed repeated long recruitment entries in three lines. Entered grouping of visual trace after source and region retains actual entries, Types/liczebno au figures , detailed tooltip and full history; acceptance this amendment remains open.

Final Editor: `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/sh-str02-04-editor-final.xml -logFile .artifacts/sh-str02-04-editor-final.log` — **74/74 PASS**, 18:04:58–18:04:59 UTC, including two trace grouping tests without mutations. Before running, `bash scripts/dotnet.sh format whitespace unity/Wotr.Client/Assets --folder` was performed.

Fifth focus: `WOTR_CLIENT_TEST_CONFIG="$PWD/.artifacts/client-integration.json" bash scripts/unity.sh -runTests -testPlatform PlayMode -testFilter 'Wotr.PlayTests.ShadowStrategy02LayoutTests;Wotr.PlayTests.ShadowStrategy03LayoutTests;Wotr.PlayTests.ShadowStrategy04LayoutTests' -testResults .artifacts/sh-str02-04-layout-fifth.xml -logFile .artifacts/sh-str02-04-layout-fifth.log` — **3/3 PASS**, all **132 System(**) 52 + 36 + 44 ), 18:05:05–18:05:52 UTC This run uses the actual saved server projections and mounts the client without connection; it is not a network test. NewPower -Excess at 1366 × 768/125% has been viewed: four compact steps, all 11 figures and approvals are visible without scrolling. Full regression PlayMode new Kestrel/PostgreSQL it was started separately.

Full `bash scripts/test-client.sh`: **148/149   PASS**, no skip, 18:06:24–18:13:07   UTC;  `.artifacts/sh-str02-04-full-client.xml` All previous cases and all packages 03/04 past. The only failure involved `CorsairsReliefKeepsGarrisonSeparateUntilRetreatThenCombines`: retreat FP has been approved and the subsequent approval of excess Shadow failed. fourteenth seed added 02 After the actual reverse and the geometry control after the legal network preview to check exactly this transition. We do not present this result as a full credit 149 testing.

Diagnosis of the only failure: `bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.ShadowStrategy02LayoutTests;Wotr.PlayTests.ShadowStrategy02NetworkTests.CorsairsReliefKeepsGarrisonSeparateUntilRetreatThenCombines'` — **0/2**, `.artifacts/sh-str02-relief-diagnostic.xml`, 18:16:23–18:16:31 UTC. Both tests now directly indicate geometry: lower edge of the button 785 px at height 768 px. The real previewshot confirms the full cards/ko already completed battle and the wrong title “Resolve effect” over the decision RemoveExcess. The amendment is intended to apply only to `Battle.Stage=Complete` along with this decision; the active battle and the autority frame remain unchanged.

Final fix of the excess window after the rescue: `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/sh-str02-relief-editor.xml -logFile .artifacts/sh-str02-relief-editor.log` — **76/76 PASS**, 18:21:04 UTC . `bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.ShadowStrategy02;Wotr.PlayTests.BattleLayoutTests;Wotr.PlayTests.CardPack04LayoutTests'` — **17/17 PASS**, zero skip, 18:21:15–18:22:01 UTC ; `.artifacts/sh-str02-relief-final.xml` Includes all 13 travel of network packages 02 , all 56 System Organ Class 14 states, both tests of the overall system battle and regression FP Character04. The new actual screenshot before clicking approval shows the correct excess title, 12 figures and the footer available; the previous fight result remains in Last Battle. The earlier only full run error 148/149 it was re-executed and counted. We do not present historical 148/149 as 149/149.

<a id="t21--kontrola-wszystkich-kart-bazowych"></a>
## T21 — control of all cards base

[Report 96 kart](rules/T21-CARD-COVERAGE.md) and [cut matrix](rules/T21-ACCEPTANCE.md) separate event/fight programs from actual test results. `python3 scripts/card_coverage.py --results .artifacts/test-results/shadow-strategy-full-final.trx` confirmed four decks after 24 , 16 separate completed packages, initiatives of all obverses, and 470 methods/980 the cases identified by the manifesto. `--check` is part CI.

`bash scripts/test.sh --no-restore --filter FullyQualifiedName~EffectManifestTests --logger 'trx;LogFileName=t21-manifest.trx' --results-directory .artifacts/test-results` — **1/1 PASS**, without omissions, `.artifacts/T21-manifest.log`. Control now requires both halves of each of 96 cards, idle tests and no Skip. The result of the subsequent aggregate regression is saved below.

During source audits, the environment reported a lack of space. Only the unpacked installer and Windows support package with `.artifacts/tools` were removed, after comparing DLL with the properly installed Unity module; approximately 1,3 GiB was recovered. Sources, user resources, tests, reports and installed tools were preserved.

Additional actual combat cuts: `bash scripts/test.sh --no-restore --filter FullyQualifiedName~BaseCardBattleInteractionTests --logger 'trx;LogFileName=t21-battle-interactions.trx' --results-directory .artifacts/test-results` — **3/3 PASS**, zero omissions. Swarm/Scouts, Scouts before the costs of Onslaught/Relentless and seven restarted commands Corsairs/Sorcerer/Palantír. The methods have been attached to the proper cards in the manifesto; details and previous test preparation errors are described in the T21 report.

`bash scripts/test.sh --no-restore --filter FullyQualifiedName~BaseCardHuntInteractionTests --logger 'trx;LogFileName=t21-hunt-interactions.trx' --results-directory .artifacts/test-results` — **7/7 PASS**, no omissions; `.artifacts/t21-hunt-interactions.log`. [Specification](rules/T21-HUNT-INTERACTIONS.md) includes Jerkír after full nested Hunt, Worn/Staff, guides and victory before further selection.

The first attempt of full regression T21 ended before the tests were performed with the MSB3491 error: no space for saving coverage mapping. Log `.artifacts/T21-full-first-disk-failure.log` remains preserved. Additionally, outdated test builds and 19 cache images identical byte to preserved canonic copies (297 139 960 B); source URL, SHA, indexes and reports remain. The second full attempt was launched after the release of the space.

<a id="końcowy-wynik-t21"></a>
### Final T21 score

`bash scripts/test.sh --no-restore --logger 'trx;LogFileName=t21-full-final.trx' --results-directory .artifacts/test-results` — **1648/1648 PASS**, no omissions, after integration of all packages and the new interactions. Log: `.artifacts/T21-full-final.log`; report: `.artifacts/test-results/t21-full-final.trx`.

`python3 scripts/card_coverage.py --check --results .artifacts/test-results/t21-full-final.trx` — **PASS : 477 methods, 990 reported cases related to 96 card**, four decks after 24 , complete 16 separate packages and source-compatible initiatives. Full TRX is compatible 1648 Definitions/Entries/Results and execution counters without errors/ omissions. Other full regression tests include the basic rules, server and infrastructure.

T21 range received. Further UI-10 checks the entire interaction contract and immunity draft, T22 set of screens, T23 full batch and acceptance with the base. The previous Unity Packing results remain in TESTING; T21 did not perform a new Unity run or acceptance hand-held.

<a id="t24--kości-i-pierścienie-lords"></a>
### T24 — dice and the Rings of Lords

[Specification, sources and full protocol](rules/T24-LORDS-DICE-RINGS.md) include 98 from new cases including 30 from walls, V12, each Ring capability and four HTTP/PostgreSQL. Fixture reboots openly provides characters; full T25 input/output and T27 decks are not replaced by this fixture in production.

Final `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t24-final.trx' --results-directory .artifacts/test-results`: **1851/1851 PASS**, 58 s, no omissions. T23 Corps preserved without change of input/hashy. Coverage 96 cards: **477 methods / 990 counted cases**. Final Unity EditMode: **118/118 PASS**, `.artifacts/t24-editor-final.xml`.

Full client by Kestrel/PostgreSQL: **173/173 PASS**, 457,76 s, `.artifacts/t24-play-full.xml` Full party from a normal room: 12 Turn, 162 decisions, RingCorrupted ( Corruption 12 , Mordor steppe 2 ), `.artifacts/t24-full-game.json` . After adding public tags selected/Permanent removed dice full checks were re-checked . NET , EditMode and **1/1 NetworkTests** ( `.artifacts/t24-play-final.xml` ). UI Lords are picking up UI-09/T34 ; public incomplete start expansions remains goalted. Without manual acceptance user.

<a id="t25--osiem-postaci-lords"></a>
### T25 — Eight Lords Characters

[Contract and full protocol](rules/T25-LORDS-CHARACTERS.md) and [manifest](../content/lords-character-implementation-manifest.json) bind inputs, outputs, 25 capabilities and 40 cases SRC-04 with real tests. The regression includes private setup Grey, identity, siege/reflection of fortresses, Balroga, Gothmoga and Mouth costs, Chief/Jerkír, Fire and Shadow reactions, all Sméagol's departures, Nenya/Mithril/Challenge/Lure/Breaking intersections, and base card Gandalf.

Final **1975/1975 .NET/HTTP/PostgreSQL**, 0 a skip, 58 s ( `.artifacts/test-results/t25-final.trx` ); **118/118 Unity EditMode** ( `.artifacts/t25-editor.xml` ) and **2/2 PlayMode** by Kestrel/PostgreSQL ( `.artifacts/t25-play.xml` ): sessions/reconnect and a full batch of the base. Twelve Lords tests restart save/host before command and after it, compares both projections, checks out an unauthorized actor and preview without changing the state, then retry without retry again RNG/kosztu. Early error in the public table privacy test corrected to different hidden hand compositions of the same size, maintaining a close comparison of the projection; the final regression passed. No manual acceptance user. Public start of Lords, his UI and decks still require the remaining Stage D tasks.

T26 checkpoint setup/tokeny: `CouncilSetupTests` , `CouncilTokenTests` and `CouncilPersistenceTests` include V13, 0/1/2/5 home, both decisions of the parties, privacy of the whole JSON , rejection of illegal costs/ version/routes , pass no counting tokens, reboot and reboots without a repeat cost. Regression Council/Lords/ frozen records of the base: 334/334 PASS ( `t26-checkpoint-setup.trx` Other Council and full capacity acceptance remain open in [T26](rules/T26-COUNCIL.md) .

T26 checkpoint capability: 555/555 PASS in the regression Council/Character/Lords/ Fellowship/BaseCorpusTests (`t26-checkpoint-abilities.trx`); 118/118 Unity EditMode (`t26-editor.xml`). New scenarios include a full group Guide with one tile, zero damage on alternatives, no return at cost Guide, separate returns of two Hobbits, region validation, capability cancellation, Strider/Hunt/Vilya/Narya and base card using alternative names. Eight Council HTTP/PG tests perform 10 with reboot scenarios. Full regression and acceptance manifest still before closing.

acceptance T26: **2058/2058 .NET/HTTP/PG**, **118/118 Unity EditMode** and **2/2 Unity PlayMode** ( NetworkTests + full base party T23), all without omissions. Manifesto 6 obverses/ 23 Capacity/ 30 the source cases shall be checked jointly with the T25 manifesto. command , environment, logs and boundaries: [Protocol T26](rules/T26-COUNCIL.md) .

<a id="t27-cards-01--sześć-kart-lords-fp"></a>
### T27-CARDS-01 — six cards Lords FP

[Rules and sources](rules/T27-LORDS-01.md) , [Implementation manifest](../content/lords-card-implementation-manifest.json) Manifesto includes both applications, all index sentences SRC-03 , 13 source cases and 43 methods / 69 performed cases. Separate `LordsCardManifestTests` compares the manifesto to the runtime directory. Eight methods `LordsCards01PersistenceTests` performs 35 restart scenarios HTTP/PostgreSQL: cost of each card , secret approval and disclosure of the fight, Wind before/In the progress/after hand access, choice card table, pitch score and ring selection, common reserve, target/casualties/region/recruits Rangers and cancellation/casualties/A side-by-side retreat. Each scenario checks both projections, the refusal of the opponent, no RNG in the preview and the receipt after reboot.

Final `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false`: **2128/2128 PASS**, zero skip, `.artifacts/test-results/t27-01-full.trx`, approximately 69 p. `python3 scripts/lords_card_coverage.py --results .artifacts/test-results/t27-01-full.trx` confirms the execution of all indicated cases. The full set includes frozen base body; its records and control sum have not been regenerated.

Unity EditMode : **121/121 PASS**, zero skip, `.artifacts/t27-01-editor.xml` , 01:22:55–01:22:57 UTC , 2026-10-06 Three new tests check the descriptions of both halves of six cards and the selection of specific Rings with priority origin. Unity PlayMode : **2/2 PASS**, zero skip, `.artifacts/t27-01-play.xml` , 01:23:34–01:24:41 UTC ( 67,58 s), Filter `Wotr.PlayTests.NetworkTests;Wotr.PlayTests.T23FullGameNetworkTests` These are two clients, actual. Kestrel/PostgreSQL and full batch **base** of production RNG It's not a full declaration. acceptance UI Lords nor party with expansions ; UI-09/T34 and integration of the rest of the T27 remain separate. Manual user tests and new measurements FPS they were not executed.

The first scenario errors concerned Grey's reaction to dice Event, incorrect assumptions about the neighborhood of Angmar, the lone commander and the number of dice from Captain. Fixed Preparation/waiting tests based on sources, without shutting down the correct production rules.

<a id="t27-cards-02--sześć-kart-cienia-lords"></a>
### T27-CARDS-02 — six cards Shadow Lords

[Sources, logic and acceptance](rules/T27-LORDS-02.md) New sets `LordsCards02BoundaryTests` , `LordsShadowManeuverTests` , `LordsShadowCombatTests` , `LordsShadowCombatInteractionTests` , `LordsCaptainTests` , `LordsGatesTests` , `LordsLoreTests` , `LordsTableRemovalTests` , `LordsWeWontGoBackTests` check both sides cards and cuts. `LordsCards02PersistenceTests` : 12 methods, 48 actual runs HTTP/PostgreSQL with reboot, preview, privacy of both views and idempotence. A common helper also requires the acceptance of a reference command Before comparing it to the server's recording.

Final result 2026-10-06 : `.artifacts/test-results/t27-02-full.trx` **2226/2226 PASS**, 0 a skip, including a frozen base body without regeneration. `lords_card_coverage.py --results .artifacts/test-results/t27-02-full.trx` : **12 cards , 102 methods 179 PASS**; manifest covers a full two-pack. Unity Editor `t27-02-editor-final.xml` : **125/125 PASS** Network `LordsShadowNetworkTests` + `NetworkTests` + `T23FullGameNetworkTests` : **6/6 PASS**, `t27-02-play.xml` After improving signatures UI four new scenarios again: **4/4 PASS**, `t27-02-ui-final.xml` .

Checked clicks die / result/Back/refusal, reconnect  draft Lore, remove Lore with selected Ring and Gates dice Character, Sméagol cost. Four windows × two resolutions 1366/1920   × scale 1.0/1.25:   16 geometry control without scrolling/p text salt. Drops `.artifacts/screenshots/lords02-*`; watched Lore and WWGB with 1366 × 768/1.25. Seed only in Wotr.Scenarios, and the server itself uses normal validators and writing. This acceptance packages, not a full Lords party or a whole UI   expansions; these belong to T27/UI-09/T34. Windows and manual user tests are not included here. Precise commands in the package report.
<a id="t27-cards-03--karty-1314-lords"></a>
### T27-CARDS-03 — cards 13–14 Lords

`LordsMadnessTests` and `LordsMadnessRecruitTests` check cost/war, regions of Sauron fortress, throws and losses of event, original siege, free regions, limited reserve and excess stack. `LordsSwarmTests` includes both roles, Daring on the initiative of 0, ineffective card FP, actual reroll and reset of round. `LordsWeShallGetItTests` and interaction tests include entrance outside the deck, no Combat, Golluma, refusal, priority, Nenya/Mithril, selection of Sméagol after departure, refille, removal of the table and Mordor final.

`LordsCards03PersistenceTests` restart HTTP/PostgreSQL before and after any new type of decision, rejects the opponent, checks preview without RNG , compares the full reference state and both projections and repeats the receipt. `LordsFinalCardsTests` in Editor checks text, zero initiative, no empty Combat tab and public card No artificial modifier. PlayMode `LordsFinalCardsNetworkTests` uses the actual two clients and server, maps, losses, recruitment, reactions and reconnect. Manifest scheme 2 has a separate semantics SpecialEvent ; `lords_card_coverage.py --results <TRX>` requires the actual results of all related tests. Detailed commands, results and limitations: [T27-LORDS-03](rules/T27-LORDS-03.md) .

<a id="t27--odbiór-całego-manifestu-lords"></a>
### T27 — acceptance the whole Lords manifesto

[Report 14 kart](rules/T27-CARD-COVERAGE.md) and [receiving matrix](rules/T27-ACCEPTANCE.md) combine three completed packages with real tests. `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t27-aggregate.trx' --results-directory .artifacts/test-results`: **2287/2287   PASS**,  0  skip,  1  min  21 p. Both checkers `--check --results`: Lords **148 methods / 248 cases**, base **477  /  990**, all PASS. Real MatchFactory preserves 108   cards in Lords and 96 in the database, only one version of Balrog and a special card A new control requires a full manifesto, three completed parcels and methods without Skip; four negative manifesto experiments were rejected. CI also checks the result TRX. No change in production or Unity; earlier acceptance parcels remain proof of them UI Normal start configuration and follow-up acceptance belong to T33/UI-09/T34. Full orders and limits in the report T27.

<a id="t28--suwereni-kings"></a>
### T28 — Sovereign Kings

[Rules and actual checkpoints](rules/T28-SOVEREIGNS.md) includes five identities, the obverses, private selection, politics, struggle and weakness. The last set of `Kings*|BaseCorpusTests` by `scripts/test.sh`: `t28-ruler-abilities.trx`, **236/236 PASS, 0 skip**. Eight scenarios HTTP/PostgreSQL checks the reboot in selection/before selection, policy, assault, recruitment, Far-sighted and additional selection Anárion; compares both raw projections, state, preview without RNG and retry. dice Ruler are a openly prepared fixture condition; their actual pool and tokens verify T29.

acceptance : `t28-full.trx` **2431/2431 PASS**, 0 a skip; `t28-editor.xml` **129/129 PASS** Manifesto 10 obverses/ 20 Capacity/ 47 bind 45 methods and 113 actual runs PASS Border tests confirm normal 1/2 Kings during Hunt, no attempt/turning the tiles to Awakened, random choice only among hidden tiles, Brand vs. Daring, Anárion after Denethor's death, and the open Far-sighted mode and limitations siege . Accurate commands, previous failed checks and further integrations in the T28 report. PlayMode new decisions, Windows and manual tests not included.

<a id="t29--ruler-pięć-tokenów-i-trzy-dark-chieftains"></a>
### T29 — Ruler, five tokens and three Dark Chieftains

[acceptance and sources](rules/T29-RULER-CHIEFTAINS.md):  `t29-full.trx`  **2543/2543   PASS,  0  skip**,  1  min  27  s;  `t29-editor.xml`  **129/129   PASS**. Manifest  3 obverses/ 7 Capacity/ 15 cases and 5 Tokens refers to **49 methods / 111 Passes PASS** with This TRX Nine new scenarios HTTP/PostgreSQL verifies restart/retry And private projections. V12 includes full pool, Recover, allotment, various Eye, order SH/Eye/FP and none dice The Tokens require immediate capability, block the Rings in both directions and keep the cost in the nest. The fight preserves played/cancellation, cost before reveal, roles and initiatives, Hunt separates the Metallage bonus from the sources and the initial throw. The base body has not been reprocessed. UI-09/T31/T33/T34 have their own follow-up acceptance;  EditMode does not replace new interactions in PlayMode or Windows tests.

<a id="t30-cards-01--pierwsze-sześć-kart-kings"></a>
### T30-CARDS-01 — first six cards Kings

[Rules and manifesto](rules/T30-KINGS-01.md) binding 41 the source opinions/cases from 44 methods and 87 real runs PASS . `t30-01-full.trx` : **2630/2630 PASS , 0 skip**, 1 min 31 p. Common recruitment amounts, politics, combat programs and base body are subject to full regression. Nine new methods HTTP/PostgreSQL checks all windows, both projections, insights and its expiry, payment, fixed RNG , preview and retry after reboot. `KingsCardManifestTests` and `KingsCardCatalogTests` control both halves, sources, initiatives, public replacements and Fate exception. Unity EditMode : `t30-01-editor.xml` * 131/131 PASS **, without omission. Tests of two clients with real Kestrel/PostgreSQL: `t30-01-play.xml` * 5/5 PASS **, 0 sky. Everyone includes layout 1366/1920 with 100/125% , without scroll and numbering, with reconnect and joint approval. Inspection and Riders drop was watched in 1366 × 768/125%. Manual user and Windows tests remain unchecked. Command details and previous fixture adjustments in the report.

<a id="t30-cards-02--druga-paczka-kart-kings"></a>
### T30-CARDS-02 — Second Package cards Kings

[Rules](rules/T30-KINGS-02.md) and the manifest include 12 Implementation cards Kings: 82 source sentences/cases , 93 methods and 166 counted cases indicated by the manifesto. Full `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t30-02-full.trx' --results-directory .artifacts/test-results` : **2709/2709 PASS**, 0 omissions, 1 m 40 s, `.artifacts/t30-02-full.log` Nine methods of the new package check HTTP/PostgreSQL restart/retry: every event, branch, choice figure , location, random return, each step West, both Welcome costs, secret commit/reveal Hope and both forms of dedication to Leadership. The manifesto of the base continues to confirm 477 methods/990 cases; frozen records have not changed hashy.

Unity EditMode : `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t30-02-editor.xml -logFile .artifacts/t30-02-editor.log` — **136/136 PASS**, 0 New tests check different nations, undos, compact groups, private tile, cost readiness and a clear choice of zeros for Vain.

`bash scripts/test-client.sh -testFilter KingsCards` : **12/12 PASS**, 0 omissions, start 2026-10-06 05:35:57 UTC , `.artifacts/t30-02-play.xml` and `.artifacts/t30-02-play.log` It's both packs. cards , two real clients, Kestrel/PostgreSQL , preview, clicks and reconnet. The second package gives 36 discharges `.artifacts/screenshots/kings02-*.png` : nine views × 1366/1920 × 100/125%. Overlooking Villages, Vain, Service (Lord's Choice) and West (tiles's Choice) in 1366 × 768/125% ; map, decisions and approval remain visible, without scroll and number fields. The first test compiler detected a reference to private helper; after correction 6/7 The tests went through and the seventh one detected 27 px history button in window Corruption It was corrected to 33 px; full re-up of both packages gave the result above. No manual user tests or Windows test were performed; the full Kings interface remains in UI-09 .

<a id="t30-cards-03--strategia-cienia-kings"></a>
### T30-CARDS-03 — Shadow Kings strategy

[Rules](rules/T30-KINGS-03.md) : Six further cards both halves, three lasting effects, order Corruption/casualties and the transparent conversion of costs. Checks: `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t30-03-full.trx' --results-directory .artifacts/test-results` — **2794/2794 PASS**, 0 a skip, 1 m 52 s, `.artifacts/t30-03-full.log` Aimed course of the new package: `t30-03-targeted.trx` * 86/86 PASS **, 0 a skip, 12 p. Eight methods HTTP/PostgreSQL restores every event and combat, regions/casualties Valinor, loss Stormcrow, choice and excess King, both conversions, both table discard costs, sovereign choice and Relentless cost; restart/preview/retry control both projections and wear of randomness. Manifesto 18 cards has 125 the source of the opinions/cases, 138 methods,** 251 credited runs**.

`bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t30-03-editor.xml -logFile .artifacts/t30-03-editor.log`: **141/141 PASS**, 0 skip, 2026-10-06 06:02:29 UTC. Tests include conversion costs on click/stitch, reconnect, separate selection Muster/Event, Ring/Will and six public texts and initiatives.

`bash scripts/test-client.sh -testFilter KingsCards`: **21/21   PASS**,  0  skip, start  2026-10-06   06:05:37   UTC,  `.artifacts/t30-03-play.xml` / `.log` Includes all three packages, two actual Unity clients, Kestrel, PostgreSQL, reconnect and authoritative approvals. Nine new scenarios give 36 discharges `kings03-*.png`  (1366 × 768/1920 × 1080,  100/125%) The Jerkír election and cost of the event, Stormcrow and King in 1366 × 768/125%; without scroll and number fields, map and approval visible. First run 20/21 detected rounding the cost button height to 31,87 px. Selection mode has now min. 33 px; the entire Kings set is renewed. Windows and manual user tests remain postponed, full UI Kings in UI-09.


### T30-CARDS-04 — Work of the Enemy

[Description and results](rules/T30-KINGS-04.md). Full regression `.artifacts/test-results/t30-04-full.trx`: **2815/2815   PASS**,  0  skip,  1  m  56 p. Including 21 new runs engine/HTTP, thresholds of three statuses, partial execution, field/garrison loss elite/characters, cost limits, legality of both halves, cancellation/Strength bathe fighting, privacy and restart. Manifesto 19   cards:  131 sentences/accidents, 152 methods, 272 passed runs. Unity EditMode   `.artifacts/t30-04-editor.xml`: **146/146   PASS**,  0  skip,  06:35:48   UTC.  `bash scripts/test-client.sh -testFilter KingsCards`: **25/25   PASS**,  0  skip,  06:36:25–06:37:26   UTC the result `.artifacts/t30-04-play.xml` / `.log` Four new scenarios of two clients, Kestrel/PostgreSQL, reconnect,  16 discharges 1366/1920   ×   100/125%, control of the hitboxes and the lack of scroll. The sovereign, armies and losses were watched at 1366/125%. Wrong expectation of release of garrison and technical ID The sovereign in history; the Kings set was repeated after the revisions. Windows and manual user tests remain postponed.


<a id="t30--kompletny-zestaw-kings"></a>
### T30 — Complete Kings Set

[Acceptance](rules/T30-ACCEPTANCE.md) and [report](rules/T30-CARD-COVERAGE.md) Real MatchFactory : 104 card from Kings, 116 from Lords; 11 replacements, 8 new cards , deactivation of two swaps with Fate. Old decks bases/Lords keep the composition and order. Tests include the cost of Ruler and reversed registration of programs for all 19 events and six random returns of the tiles at both disclosure settings ( 12 cases).

Full `scripts/test.sh`,  `.artifacts/test-results/t30-aggregate-full.trx`: **2856/2856   PASS**,  0  skip,  1  m  52  s.  `kings_card_coverage.py --check --results` Confirms 157   methods/313 and 131   clauses/przypadk The Regression of the Lords manifestos 148/248 and bases 477/990   PASS Five negative checkser attempts in memory rejects incompleteness and loss of Fate exception. After changing the waist `scripts/test-client.sh -testFilter KingsCards`: **25/25   PASS**,  0  skip;  `.artifacts/t30-aggregate-play.xml` / `.log`,  2026-10-06   06:51: 31Z. Both clients, Kestrel/PG, reconnect and layout. UI not amended; last EditMode   146/146 comes from a pack 04 Manual user and Windows tests were not performed.

<a id="t32-cards--sześć-wydarzeń-fate"></a>
### T32-CARDS — Six Fate Events

`FateCardsTests` includes eight deck compositions, exclusion Baseline/Kings slots and cost, ownership, window and configuration for everyone Event/Combat. `FateCompanionCardTests` , `FateFreeRecruitmentTests` and `FateShadowRecruitmentTests` check full sequences, no targets/reserves, independent movements, policy without additional control requirement, actual army in Dale, options choices, common scarcity, garrison and final surplus. `FateCombatTests` use ID Fate: participants, threshold after five, natural 1/6 , Combat a Leader re-roll, summing/cancellation/Challenge A dream, Onslaught also after losing his last unit.

Seven `FateCardPersistenceTests` methods perform HTTP/PostgreSQL reboot before and after each decision class, retra, both projections, rejection of another page and preview without mutation. Includes the cost of playing all six, separation and Guide, movement and recruit Book, both selections and hand limit, alternative Orcs, both Monsters branches and both Onslaught loss decisions. [Manifest](../content/fate-card-implementation-manifest.json) binds all source sentences with 39 methods / 102 done cases; `FateCardManifestTests` compares links with runtime.

2026-10-06 results:

- `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
  --logger 'trx;LogFileName=t32-cards-full.trx' --results-directory .artifacts/test-results`:
  **3177/3177 PASS**, no omissions, 08:14:23–08:16:32 UTC. File
  `.artifacts/test-results/t32-cards-full.trx`.
- `python3 scripts/fate_card_coverage.py --check --results
  .artifacts/test-results/t32-cards-full.trx`: PASS 39/102; regression of the 477/990 database
  manifests, Lords 148/248, Kings 157/313 also PASS.
- `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults
  .artifacts/t32-cards-editor.xml -logFile .artifacts/t32-cards-editor.log`: **157/157 PASS**, no
  omissions, 08:08:04–08:08:06 UTC.
- `bash scripts/test-client.sh -testFilter FateCardsNetworkTests`: **6/6 PASS**, zero omissions,
  08:10:37–08:10:56 UTC; copy `.artifacts/t32-cards-play.xml`. Actual Kestrel/PostgreSQL, two
  clients, reconnect, 24 1366/1920 × 100/125%. Monsters 1366/125% and Orcs 1920/100%, without
  cut-out choices, ScrollView or number fields have been reviewed.
- `python3 scripts/source_fate.py --registered`, `python3 scripts/tasks.py validate`, `git diff --check`: PASS.

Earlier attempts showed two fixtures errors: old waiting for base slots Branda/D áina at Fate ( 133/137 ) and not included die Captain of the West in Legolas in controlled throws ( 145/157 Expectations already indicate the right ID Fate, and neutral test participant is Merry. Focused regression after amendment: 164/164 , `.artifacts/test-results/t32-cards-behavior.trx` Full score above also includes later added manifesto test and Onslaught attack after its own elimination. No new Windows build, measurement FPS Or manual user testing in this package. UI expansions and the entire configuration line-up remains separate receptions UI-09/T33/T34.

<a id="t32-gimli--dwie-karty-jednej-figury"></a>
### T32-GIMLI — two card one figure

`FateGimliSetupTests` verifies the normal version of the Lords/Kings or without them, disconnected ID figure and obverse, default version, illegal stare/double elections, both homelands without New Cities, ban on Erebor and identical JSON Shadow before you reveal it. `FateGimliAbilityTests` checks six normal cost walls, control Shadow , as appropriate window/In the owner/resource/target, activation separation, levels Hunt/Combat/movement And the actual icon for the Kings. `FateGimliBattleTests` check Captain/Champion on both sides battle , Region of Defender, Participants, Cancellation, New Round and Limit Five. `FateGimliHuntTests` : Zero injuries, three more Guides in the group, rejection CorruptionOnly , victim of the level 1/2 And no return to Mordor.

The four `FateGimliPersistenceTests` methods include 12 actual HTTP/PostgreSQL: reproductions, homeland, Shadow chip, both policies, Guide response, new Guide and group movement. Each checks both projections, previews without mutations/RNG, authorization and idmpotent retries after reboot. [Manifest](../content/fate-character-implementation-manifest.json) includes six identifier runtime, seven obverse sentences, and seven source cases. `FateGimliManifestTests` compares statistics, response and an exact set of mappings with C#; [report](rules/T32-GIMLI-COVERAGE.md) requires 23 methods / 51 done cases, without omissions.

Done 2026-10-06 : Full `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t32-gimli-full.trx' --results-directory .artifacts/test-results` — **3229/3229 PASS**, no omissions, 08:36:14–08:38:27 UTC . `fate_character_coverage.py --check --results .artifacts/test-results/t32-gimli-full.trx` count 23/51 , `fate_card_coverage.py` 39/102 ; manifests bases/Lords/Kings remain included 477/990 , 148/248 , 157/313. Unity EditMode : `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t32-gimli-editor.xml -logFile .artifacts/t32-gimli-editor.log` — **158/158 PASS**, no omissions, 08:36:15–08:36:17 UTC Results of previous attempts and corrections of fixtures are recorded in [Task rules](rules/T32-FATE-GIMLI.md) The full private council interface still belongs to UI-09 ; we do not pass it by the test of the protocol itself.

`bash scripts/test-client.sh -testFilter FateGimliNetworkTests` : **3/3 PASS**, no omissions, 08:39:05–08:39:16 UTC , copy `.artifacts/t32-gimli-play.xml` Two clients and a real one. serwer/PG: Renown without activation, Messenger with activation, Hunt Guide and traffic with range 3 After offset Fellowship Reconconnect retains draft and approved stages. Four trays × 1366/1920 × 100/125% • If you are a doctor, you may not be able to 16 No scroll and no number fields. Messenger and separation were reviewed at 1366 × 768/125%: decision controls are visible. On the map the signature of the planned South Dunland partially occurs on the Fords of Isen tag; a specific case to UI-12 ( `.artifacts/screenshots/fate-gimli-departure-1366-1.25.png` This is not a whole score. UX map. No manual user, new Windows build or measurement FPS In this package.

<a id="t32--zbiorczy-odbiór-fate"></a>
### T32 — Collective acceptance Fate

[Acceptance](rules/T32-ACCEPTANCE.md) adds a deterministic checker `fate_acceptance.py` to CI, before the tests and with their real TRX. Compares the exact setup and New Cities with the SRC-07 index, requires three finished components and a set of manifestos of six events and two Gimli versions. It does not need a local cache of source images.

`bash scripts/test.sh --no-build --no-restore --filter 'FullyQualifiedName~Fate|FullyQualifiedName~BaseCorpusTests' --logger 'trx;LogFileName=t32-acceptance.trx' --results-directory .artifacts/test-results` : **300/300 PASS**, no omissions, 2026-10-06 08:44:00–08:44:12 UTC . `python3 scripts/fate_acceptance.py --results .artifacts/test-results/t32-acceptance.trx` : **79 methods / 192 cases PASS**. Recheckers events 39/102 Gimli 23/51 and `source_fate.py --registered` PASS Five mutations in memory (no pair of setup, wrong number figures , owner, no New City, no active nation) rejected. `py_compile` , validation of tasks and diff PASS No new runtime change; full .NET/Unity remain the results of the packages described above. expansions It belongs to T33.

<a id="t33--osiem-składów-i-93-konfiguracje"></a>
### T33 — eight configurations and 93 configurations

[Plan and acceptance](rules/T33-COMPOSITION.md) binds the setup order, slots, dice , characters, tiles and variants with specific sources. New tests include 93 sets to the first action , 93 privacy comparison, 512 clear flags and 256 enter with default Dismay. Independent table expects, respectively 93 and 69 legal entrances; errors are rejected before RNG The set of ingredients is checked after each reading; changing the secret does not change JSON the opponent. Each legal configuration has two scenarios HTTP/PostgreSQL: First setup decision and first spending. 26 scenarios check Book/Gimli/Council/Commander , Lords/Ruler/Dismay and Corruption Sméagola/Captain at four variants of tiles. 212 restart scenarios**, each with a re-read before and after the decision, authorisation, clean preview and retry.

2026-10-06 results:

- `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
  --logger 'trx;LogFileName=t33-full.trx' --results-directory .artifacts/test-results` : **
  3434/3434 PASS **, no omissions, 09:06:51–09:10:19 UTC. 205 new cases; all previous rules and base
  body without changing hash.
- `python3 scripts/expansion_acceptance.py --results .artifacts/test-results/t33-full.trx`: **1107
  methods / 2560 cases PASS**. Checker in CI combines source metadata, Unity copies, eight
  compositions, event/form manifestos and requires 93 cases of each matrix. Separate 477/990
  database manifestos, Lords 148/248, Kings 157/313, Fate 79/192 PASS.
- `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t33-editor.xml
  -logFile .artifacts/t33-editor.log`: **158/158 PASS**, no omissions, 09:06:55–09:06:57 UTC.
- Three metadata mutations and no one case in the TRX copy: correctly rejected. `python3 -m
  py_compile scripts/expansion_acceptance.py`, `tasks.py validate`, `check_catalog.py`, `git diff
  --check`: PASS.

Previous attempts: 118/118 setup/decks; 192/192 matrix and basic reboots; 13/13 targeted cut-off with save. 8/10 trial indicated an excess selection of the only region of Book in the test – corrected without changing the rule. The exception property name error was removed before the first run. The prepared later states of rare cuts are not a full batch record. New PlayMode, manual user trials, Windows or FPS were not launched in the T33. Full client of expansions and the room start remain UI-09/T34.


<a id="t34--komplet-decyzji-dodatków-i-normalne-lobby"></a>
### T34 — expansions decision set and normal lobby

Full .NET: **3441/3441 PASS**, without omission, `.artifacts/test-results/t34-full.trx`. `ExpansionLobbyTests` additionally starts with HTTP all 93 legal configurations, verifies Versions/consent, projections of both roles and invariability after start. `expansion_acceptance.py` confirms 1107 related methods / 2560 made manifesto cases.

Client: new difficult compositions/windows tests **21/21**, configuration **8/8**, Lore and regression **14/14**. Reports and previous failures and repairs: [T34-ACCEPTANCE](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/T34-ACCEPTANCE.md). `expansion_ui_acceptance.py --results
<NUnit reports from oldest to newest>` Controls the border 22 Baseline /
8 new windows and execution 37 scenarios expansions . Later result of the method replaces the earlier; does not count omitted or unexecuted methods. PlayMode : **254/257**, `.artifacts/t34-client-full.xml` ; three failures (two old bookmarks and the application of the bar on card ) repaired and renewed: **18/18 PASS**, `.artifacts/t34-regression-fix.xml` The full base party went to 9 turn and 121 decisions. Final symbols expansions : **174/174 EditMode** and **14/14 PlayMode**, no omissions, `.artifacts/t34-symbols-editor.xml` and `.artifacts/t34-symbols-play.xml` Checker on three reports confirms 37/37 scripts. Revision of description dice The Ruler also has **15/15** domain Lore/KingsLords. Full of commands, times, earlier results and limitations are in the T34 report.

<a id="t35--trwała-lista-i-podsumowanie-powrotu"></a>
### T35 — Permanent list and summary of return

[Report and Commands](server/T35-CORRESPONDENCE.md) : **3448/3448 . NET PASS**, no omissions, `.artifacts/test-results/t35-full.trx` , 11:50:10–11:53:51 UTC Seven new tests. PostgreSQL/HTTP restart battle/Hunt/Council/Kings , the continuation of the procedure exactly once, 55 lot filters/paginacj , all types of decisions, privacy of both roles, authorisation and monotonous ack and race with command Final EditMode **175/175 PASS**, `.artifacts/t35-editor.xml` , includes the format of new DTO Unity. Checker expansions 1107 methods/2560 cases PASS Testing SQL lists 55 lot: 4,367 ms locally, without dessertization of snapshots or generating legal options. UI-12 integrates the list and local return; this acceptance does not count her views or new tests PlayMode .

<a id="ui-12--rzeczywisty-powrót-do-klienta"></a>
## UI-12 — actual return to client

[Protocol](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/UI-12-RETURN.md) documents six new PlayMode attempts with ClientFlow, atomic local record and normal API. This one. family of entries checks both sides, legal options, secrets and no automatic command after entry. The crash test stops the Kestrel process after the saved command and lost response, restarts it with PostgreSQL and plays fresh ClientFlow/HTTP/session/panel. This is not a test to kill the Unity process or a shared Windows–macOS game; this range remains in acceptance release.

Two devices, offline, map/pin reproduction draft no substitute cost, private policy, list 20 + 3 and ack after rendering: **11/11 PASS** With five regressions UI-10/UI-11 , `.artifacts/ui12-second-play.xml` , 12:33:38–12:34:15 UTC After completing the waiting for the entries of both journals **186/186 EditMode PASS**, `.artifacts/ui12-flush-editor.xml` , 12:38:32–12:38:38 UTC New EditMode include damaged writing, range host/account/game , fixed ID error I/O before HTTP , late receipt, list and ack, as well as closing with ongoing records. Full PlayMode the last version is running; the final result is separately.

Final regression UI-12 : Full PlayMode **263/264**, zero skip, `.artifacts/ui12-full-play.xml` , 12:40:26–12:55:21 UTC The old motion test expected a hand check result despite automatic recovery of the result. After switching to actual GET the original receipt and one expense was re-established the entire class with Return/UI10/BattlePolicy: **11/11 PASS**, zero skip, `.artifacts/ui12-regression-fix.xml` , 12:57:39–12:58:18 UTC The production code has not changed with this test repair. This is a combined result of a full run and targeted re-renewal; not a report 264/264.

<a id="ui-13--desktop-preferencje-i-profiler"></a>
## UI-13 — desktop, preferences and profiler

[Report UI-13](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/UI-13-DESKTOP.md) describes actual results and limitations. [Numerical report](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui13-macos.json) retains mean/p95/max, the number of samples and the availability/units of meters. Workload exports perform `DesktopProfileFixtureTests` in EditMode; benchmark launches `python3 scripts/profile-client.py` after `BuildTasks.BuildMac`. No account or database required. This measurement of presentation; rules and transport receive tests with the actual server.

Build scripts openly select macOS Universal and Windows x64. The 2026-10-06 user recommended preparing the Windows build and saving the lack of its test. We do not count Windows runtime/profilera or games between Windows and macOS. Manual user attempts also remain postponed.

<a id="t36--pełne-partie-i-końcowa-ergonomia"></a>
## T36 — Full lots and final ergonomics

[Protocol](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/T36-ACCEPTANCE.md) save both full starts from normal lobby and production RNG , earlier errors and map repair. `T36DesktopGameTests` : **2/2 PASS**, 0 skips, 13:59:27–14:01:23 UTC ; base 148 decision/11 Turn, full expansions 100 decision/7 Turn. `t36-regression.xml` : **60/60 PASS**, 0 skips, 14:02:10–14:07:53 UTC , difficult interactions and layout matrix. The report gives an accurate filter.

After the MacOS redevelopment, a separate Player performed 16 workloads, without the parallel Unity build. [Raw measurements](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/t36-macos.json) include FPS , mean/p95/max frame times, handling, feedback and allocations. acceptance S01–S24 links these trials to protocols T22/T34/UI-10/UI-12/UI-13 ; is not a declaration to restart the entire historical matrix on Windows.

<a id="t37--odporność-i-zgodność-wydania"></a>
## T37 — immunity and conformity of release

[Report T37](server/T37-HARDENING.md) bind the matrix failure/prywatno Full .NET/PostgreSQL: **3473/3473 PASS**, zero skip, `t37-full.trx` Final Hash Tests/ migracji/granic: **26/26 PASS**, `t37-final-boundaries.trx` Unity EditMode : **196/196 PASS**; after last change of connection loop `ApiFailureTests` : **6/6 PASS**, no skip.

`python3 scripts/check-legacy-fixtures.py` has played all the old write bytes, command and receive the original `6dcc0b6` engine. This is the control of the actual previous contract; the frozen fixture has not been generated again by the current release code. Snapshot v1 migrates to v2; it plays normal command lists without repeating kosztu/RNG.

Targeted PlayMode: **12/14 PASS**, zero skip, `t37-play-first.xml`; 12 real network scenarios have passed. After correction of two assumptions of the new fixture layout repeated class: **2/2 PASS**, zero skip, `t37-save-layout.xml`. The T37 report retains full filter, causes and fixture chapter of the error message from the real HTTP/PostgreSQL..

<a id="t38--infrastruktura"></a>
## T38 — infrastructure

`t38-hosting-pass.trx` : **18/18 PASS**, no skip. Separate `t38-migration.trx` : **1/1 PASS**, zero skip. Pure Compose built and run locally, roles PostgreSQL And the readmission is checked. [Report T38](server/T38-DEPLOYMENT.md) preserves also uncounted attempts: image archive and full `.NET` interrupts the lack of disk space ( `53100` PostgreSQL After the release of the seat, the full one was resumed acceptance : `t38-full-resumed.trx` **3494/3494 PASS**, zero skip, 16:14:32–16:18:33 UTC . [Result restore](server/t38-restore.json) : both backups and playbacks, nine tables, HTTPS/WSS , non-mutation retries and continuation battle (revision 5 → 8 , 98 preserved and six new samples RNG ) The image archive was also read by `docker image load` ; ID agree. Older completed TRX before T37 was preserved as a wasteless `.trx.gz` .

Backup correction after switching the database: `t38-backup-config.trx` **19/19 PASS**, zero skip (DeploymentDatabaseTests/HostingTests). `python3 -m unittest discover -s tests/deployment -p 'test_*.py' -v` script tests: **10/10 PASS**, without Docker; include the selection of an active base, changing the container, incomplete export, total copy and refusal overwrite. One attempt to check the completion of the real progeny process after a controlled error I/O. Extended integration test performed a second copy after the restors and continuation of battle; the restoration retained new decisions, and the original base did not change.

<a id="t39--prywatny-release-candidate"></a>
## T39 — private release candidate

[Issue report](release/T39-RELEASE.md) preserves failed first PlayMode  **269/270**, detected projection error Shadow after Reveal in Mordor, source of the amendment and new tests. Regression of the Uglúka/Mordor border: **62/62   PASS**,  0 skips (`t39-mordor-pass.trx` Full backend after the amendment: **3499/3499   PASS**,  0 skips (`t39-full.trx`),  16:46:54–16:51:02   UTC. Checker  expansions confirmed**1109 methods / 2565 cases** and matrix 93 configuration. Final EditMode: **198/198   PASS**,  0 skips (`t39-editor-final.xml` Re-full PlayMode: **270/270   PASS**,  0 skips, 16:58:39–17:15:24   UTC  (`t39-play-final.xml`).  [A compilation and SHA-256](release/t39-validation.json) includes reports and full parties from a normal lobby: database 172   decisions/13  tur (RingDestroyed), base at 125% /Reduced motion  96/7  (RingCorrupted), all expansions with Council/Dismay/New  Cities  217/15  (RingCorrupted Each undergoes reconnect and compatibility of both clients; the tests do not control production RNG.

[New Backend Image](release/t39-deployment.json) has again passed the T38 procedure: TLS/WSS, nine tables, two restors, no mutations, and a new round battle. The archive has read `docker image load` and both ZIP have passed the control of CRC and SHA-256. The building `20bbd24` do not use Development: macOS Universal and Windows x64.

[Smoke macOS](release/t39-macos.json): Unpacked Player launched with clean data, showed [login screen](release/t39-macos-login.png), reported no error in the log and restored the previous profile. The first helper confused pixels with Retin points; the history of failed attempts was preserved. Windows runtime, Windows ↔ macOS batch, Intel hardware test, player's connection to the target host or manual acceptance user was not performed.

<a id="ui-22--komponenty-dodatków-i-wyjątki-kart"></a>
## UI-22 — expansions components and exceptions cards

Report UI-22 saves all failed attempts and corrections. Backend **3550/3550 PASS** ( `ui22-full.trx` ), EditMode **241/241 PASS** ( `ui22-completed-editor.xml` .) Wide PlayMode **182/205**, then **70/75** and final regression **27/27** fixed cards , panels and hover. Two Far-sighted costs, four costs Balrog/Mouth , private tiles and matrix 93 configuration × 2 pages × 4 Layouts go through. Checker UI confirms43 actual scenarios executed; directory30 families/35 windows, checker add-ons1109 methods/2567 cases, Fate79/192 PASS The full T36 party used the old driver gestures; its migration and acceptance are clearly planned in UI-24 We don't count it on the basis of the matrix.

<a id="ui-23--powiadomienia-i-powrót"></a>
## UI-23 — notifications and return

UI-23 : EditMode **245/245**, actual server tests Correspondence/ActionSequencePersistence/Transport **13/13** and final PlayMode **27/27 PASS**, no omissions ( `ui23-final-editor.xml` , `ui23-server.trx` , `ui23-final-play.xml` ed) (previous16/ 20 and improved 16/16 The attempts include all return families of both sides, a receipt after reboot, local draft offline and spent cost of the second device, saved first march, capture by reconnect of the same revisions/changes The controller and navigation only after clicking the notification.

<a id="ui-47--pełny-odbiór-walki-dziennika-i-kontekstu"></a>
## UI-47 — full acceptance combat, log and context

Report UI-47 includes UI-38 – UI-46 and UI-47-CONTEXT , a map of evidence BA01 – BA16 and previous failures and their improved re-enactments. Full .NET/PostgreSQL: **3651/3651 PASS**, manifesto expansions **1109 methods / 2567 cases PASS** Final Unity EditMode : **346/346 PASS**; full PlayMode : **376/376 PASS**, **118 classes in 21 packages**, without omissions. [Manifest PlayMode](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui47-play-tests.json) preserves all filters, cases and SHA-256 reports. The distribution of hosts prevents the fixture from exceeding the login limits; production limits remain unchanged.

A joint party of two clients passes two marches behind one die, attack, card both sides, natural rolls/rerolls, losses, Continue, retreat and entry. Alerts, old warehouses and reports read real data HTTP/WSS/PostgreSQL. Matrix expansions, restart/retry, hidden information and controls of both sides remain subject to full regression.

[Measurement of Player](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui47-macos.json) : **56/56 PASS** Apple M1 Pro / 16 GB / macOS 15.6.1 Unity 6000.6 .0f1, 1366×768 and 1920×1080 with 100/125%. Minimum average **96,87 FPS**, the greatest gesture time → end of rendering **20,65 Ms**. 5000 Public duplicates DTO verify actual scroll/kart and most 12 This is the measurement of presentation without network and delay of equipment; 120 the actual commits check separate PlayMode Overwatched. 32 four-chips dumps and improved Book and login.

Builds with `09c68d3`: macOS Universal and Windows x64 without Development and separate macOS Development for measurement. macOS startup with clean profile **PASS**, user data restored. [Summary](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui47-validation.json) and [Source manifest](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui47-build-source.json) bind results with code, input and build files; 232 files C#/assembly in accordance with the commit. Windows runtime, Intel, the Windows/macOS, handheld acceptance and the target host remain unexecuted. New plans UI-49+ / MAP-12+ are beyond this acceptance.

<a id="ui-54--odbiór-polowania-i-integracji"></a>
## UI-54 — acceptance Hunt and integration

UI-54 uses a separate host for each package PlayMode without changing the limits of production login. Manifesto `.artifacts/ui54/batches.json` contains 96 unique full method names; `collect-results.py` checks the exact collection XML , deficiency/expansions/duplicates results and SHA-256 , without running the runner. Local new build and Development Player have their own directories, entrance manifests and `clean_profile` with the restoration of preferences in the final. The profile measures the local gesture until the end of rendering, without delay of the device, monitor and network. Windows/Intel/host/r elated acceptance they are deferred.

The screenshots are in existing real-life tests and do not replace them with: `HuntRoleScreenshots` restores viewport/scale/opening after four layouts of both roles. Seven methods to re-check has a separate `screenshot-batches.json` manifesto (18 and the highest 9 logins to two hosts). Hand/Victory regression retains the former actual hit-test, and the new layout also checks the saved entries. Back to the completed batch checks the banner, inspection of the result and no replay, according to the final presentation of UI-60.

Final score: 99 unique methods PlayMode ( 96 major + 3 Victory), 106 required executions with seven repetitions for PNG , 0 full. Editor449/449 and .NET3723/3723. Performance report [UI54 macOS](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui54-macos.json) describes 56 the cases and the exact entrances of the frozen build. The first dragons caught splash; the final smoke-final showed the correct login screen. No log-in to the production host or client was launched Windows/Intel.


<a id="ui-63--profile-legacy-oraz-in-game"></a>
## UI-63 — Legacy and in-game profiles

`scripts/profile-client.py --suite in-game --views <projekcje> --report <raport>` activates eight types in four systems; `--suite legacy` preserves earlier 14×4 Both sets are to use the same Development Player. Validator requires an accurate set 32/56 , 120 Warmup frames, 360 samples, correct reduced motion and measurement outside the Editor. New suite has a uc 24 samples of the carrier and render-end. Negative/niesko results to be completed, FPS < 60 or maximum feedback> 100 ms reject the report. Main counters not available Thread/GC remain outright null; do not replace mandatory times. Render-end does not include a monitor, input device or network.

InGameProfileDriverTests runs the same driver and strict panel.Pick in all 8×4 sequences but not measuring FPS In Editor. InGameDenseLayoutTests uses both clients of the actual server; checks full pools, five portraits, Effects with Help, privacy and no change of server decision at the inspection. Full boundaries and pics of portraits complement the control elements themselves. Collector connects G01–G18 with full methods names, actual XML/TRX and SHA ; rejects lack, omission and failure. acceptance Player/builda/PNG is a separate step. History of trials, final reports and limitations: UI-63 .

The report with the correct time limits and exit code 0 is not accepted if the Player's log contains `Layout update failed to stabilize`: the recursive layout update invalidates acceptance from these metrics. `validate_player_log` checks the log after the process, before the report is accepted. Python validator has **13/13 PASS**, including a clean log and the actual error message between completed workloads; the rejected measurement remains historic.

<a id="końcowy-odbiór-ui-63"></a>
### Final acceptance UI-63

acceptance UI-63 : Full . NET 3726 full Editor 551 required 36 unique methods and final methods 44 cases in seven batch PASS Do not add up the repetitions to cover. Collector checks the exact names/cases and none fail/skip ; manifestos bind sources, entrances and artifacts. New builds macOS/Windows/Development and macOS dragons PASS Profiles 56 + 32 PASS : Minimum 98 , 941/107 , 880 FPS , maximum render-end24, 2851/24 , 9241 ms on Apple M1 Pro, Mac OS X 15.6.1 Unity 6000.6 .0f1; both logs without system error. `profile-client.py` reject `Layout update failed to stabilize` in the log even with the completed FPS/time ; generator uses the same validator. Local measurements handlera/render-end do not include a physical device, monitor, network or server execution command .

<a id="ui-72--characters-przygotowanie-i-menu"></a>
## UI-72 — Characters, preparation and Menu

`scripts/profile-client.py --suite board --views <ui72-profile-views.json> --report <raport>` uses the existing Development Player. This one. types of × 4 The layout includes Characters FP/SH , setup Lords/Council/Fate FP , private expectation SH , the homeland of both parties and the Menu during losses battle FP Hunt SH Every case requires 120 Warmup frames, 360 samples and 24 measured gestures handler/render-end ; the validator rejects the missing or duplicate workload, the incorrect Reduced motion, the measurement in the Editor, FPS < 60 or maximum feedback 100 ms. Still rejects the system stabilisation error in the Player log, even when the metrics and output code are correct.

BoardProfileDriverTests performs the same 40 sequences through actual events, control `panel.Pick` and appearances after the render. The extended cycle of the driver visits each portrait and version group, checks the legal goals, exact cost and behavior of draft at Menu/Help. The driver check in the Editor is not a measurement of FPS. The number of 24 means actual gestures, not 24 different state changes; a re-match of the sole portrait of Lords may retain the same choice. The profile does not perform network commands.

Required Play Tests is 39 existing methods and two new methods BoardCompletionLayoutTests ( 40 views roster/setup/homelands and 16 views Menu/Settings/Help Total 41 A separate driver is 42 . method, without summing up overlapping sets. Packages have a fresh server HTTP/WS/PostgreSQL , independent aliases of mutations and less than 20 Restart and loss of response preserve separate generation of the server, accurate draft and single receipt. Selected regressions UI-63 remain openly historical with dates, SHA reports and control of sources; not counted as new executions UI-72 Collector maps CM01 – CM18 for full method names and actual XML/TRX The report also lacks.

acceptance final 2026-10-08 : Full . NET **3729/3729 PASS** ( `ui72-domain.trx` , 18:27:00–18:31:33 UTC ), including three new export tests and actual evidence 93 configuration HTTP and reboots; full Editor **551/551 PASS** ( `ui72-edit.xml` , 19:04:11–19:04:26 UTC , after final correction). Python Validator has **15/15 PASS** ( `ui72-profile-python.log` ), including a set 40 cases and mandatory 24 Samples. Required Play **41/41** and a separate driver **1/1** past; the new two methods include 56 views. [Matrix CM01–CM18](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui72-validation.json) preserves accurate methods, dates and SHA 28 reports, separating 74 historical regressions from fresh performances. 267 identical and one clear exception to Characters received six fresh reports.

New three builds macOS/Windows/Development and native macOS dragons PASS . [Profile board](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui72-macos-board.json) : **40/40 PASS**, minimum 104,322 FPS , maximum trader 11,2127 ms and render-end 24,1485 ms on Apple M1 Pro, 16 GB , macOS 15.6.1 , Metal, Unity 6000.6 .0f1. Log without stability error; offline projections do not replace server tests. 56 End PNG network, 40 Player and separate Login dragons. Original application profile restored. Commands, previous failures and boundaries Windows/Intel , physical smoothing, manual acceptance and host: UI-72 .

<a id="zwarte-panele--odbiór-ui-78"></a>
## Compact panels — acceptance UI-78

`ui78-integrated14.xml` In `.artifacts/ui77` performed 16 tests: 15 PASS , the only FAIL is the height 187 px after Army selection. Matrix 186 projection in four systems ( 744 configurations) and full resource pools, Hunt, Fellowship and two clicks of discard have passed. After reducing unnecessary intervals `diagnostic15.xml` He's had a good time. CompactBoardLayoutTests : both views, four layouts, real choice dice Army, buttons through panel.Pick, empty and dense regions, return retra, unchanged camera/projection and zero POST The height threshold of the straight tray remains < 180 units UI - I'm not sure. MultiUseDice (reserves two clicks still), dense InGameDenseLayout and ContextHelpLayout The whole diagnostic15 has 15/16 PASS ; independent failure concerns an additional helper canceling you in UI-73 And it needs a re-up.

Commands: `WOTR_UNITY_EDITOR="$PWD/.artifacts/ui25/unity-isolated.py" python3 .artifacts/ui77/run14.py` and `python3 .artifacts/ui77/run15.py`. These local runners save the exact filters/commands; each call `scripts/test-client.sh` creates a fresh seed PostgreSQL and a real server HTTP/WS. Manifestos `import-14.json` and `import-15.json` contain SHA each of the 1333 actual isolated project files. These tests are components; final measurements, builds and new appearance Fellowship belong to UI-77.

<a id="kadr-mapy-i-cele-terenu--ui-73"></a>
## Map crew and site targets — UI-73

acceptance Implementation: 191 unique cases of the plan has the latest score PASS after imports13– 16 ; `terrain-coverage16.json` indicates source XML each result. This is not a 191 test performed at once on one version. Additionally, `edit16.xml` 26/ 26 checks the mathematics of cover and the framing between panels, and `play16.xml` count MapViewport2 , ActionMap1 , ActionContext3 And full of homeland. FAIL whole game 8/9 is a separate test of the initial position of the hand with UI-74 Commands and previous failures: UI-73 Matrix BI01/02/03/06 preserves actual gestures, server result and privacy limits; final builds and profile remain in UI-77 .

<a id="odbiór-ui-74--końcowe-ponowienie"></a>
## acceptance UI-74 — final renewal

The results below concern the version with descriptions opened after clicking. UI-80 changes only the information tooltips to hover/fokus; old attempts remain evidence of UI-74, without including this new part.

`.artifacts/ui77/victory18.xml` 1/1 PASS closes the test of hand availability and stability of the recorded position in four systems. `inspection-coverage18.json` 96/96 and `terrain-coverage16.json` 191/191 with last result PASS document the subsequent runs and their sources, not one aggregate run. Details, initial failures and command: UI-74 Final regression after UI-79/UI-80 and buildy/profile belong to UI-77 .

<a id="graficzne-tory-drużyny--ui-79"></a>
## Graphical tracks Fellowship — UI-79

Editor19 **40/40 PASS**, layout20 **15/16**, after the revision of the Guide layout21 **16/16 PASS**. Seven further isolated server runs21 **35/35 PASS**; total51 Play methods with last PASS Eight projections of both pages/four The systems and eight border personnel were inspected individually. Filters, sources, earlier FAIL and boundaries: UI-79 Final profile/build only after UI-80 In UI-77 .

<a id="odbiór-ui-80--informacja-i-stabilne-cele"></a>
### acceptance UI-80 — information and stable targets

Final Play: **60/60 different methods PASS**, with the latest result of any method in `.artifacts/ui77/ui80-coverage25.json` Report combines actual import launches22– 25 ; fails to pretend to one run. Recent tests include four layouts, both views, full text, disabled Activate, area without description, one Esc, actual two-click discard with hower, receipt/reconnect , cards/battles The drivers checked 40 variants of Board and 32 In-game.

Full Editor: `.artifacts/ui77/final-edit25.xml` **600/600 PASS**, without omission; command and SHA sources in `final-edit25-command.json` The first failures were explicitly preserved: early Location in the site test, actual panel jump after late appearance ActionProgress and the ideal height setting32 instead of rendering height32/ 40 pixels. Corrected booking with selected dice does not show a fictional budget, and an actual fund figures/stage/trays are unchanged before and after the selection. Details and actual commands: UI-80 Native profile and builds of this version belong to UI-77 .

<a id="końcowy-odbiór-planszy-i-informacji--ui-77"></a>
## Final acceptance board and Information — UI-77

Final Import Editor 28 : **600/600 PASS**, no omissions. Play set has **60 different methods with last PASS** of 9 actual reports; we do not add repetitions or historical indices to it 191/96. Last move 6/6 also check the inspection while waiting Guide in 4 layouts. Common driver 3/3 includes 40 Board and 32 In-game. Network packages use real HTTP/WebSocket/PostgreSQL. Full names, dates, SHA and older evidence: [matrix BI](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui77-validation.json) .

[Profile legacy](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui77-macos-legacy.json) has 56/56 PASS , minimum average 97,739 FPS and maximum render-end 22,2877 ms. [Traffic profile](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui77-macos-motion.json) has 32/32 PASS , minimum average 102,859 FPS and maximum render-end 34,3251 ms. Hardware: Apple M1 Pro, 16 GB , macOS 15.6.1 , Metal, Unity 6000.6 .0f1. Each variant includes 120 warm-up frames and 360 measured; movement requires 24 input samples. Options 1366/1920 with 100/125% include both views and a reduced motion. Validation checks the actual input SHA before/after presence 96 PNG and phase chronology, intermediate position and end of the cap. 8 pairs of subsequent public projections exported in the import server test 24 Logs are free of exceptions and looping the layout.

Builds macOS Universal and Windows x64 and pure macOS smokes; the player profile has been restored. After the adapter was corrected, only Development was rebuilt: changed sources are excluded from the normal client. First incomplete profile and test failures 26/27 are preserved and described in UI-77. No veiled region was hidden or clicked by the panel: benchmark explicitly closes the Guide and uses the Inspect, retaining the decision. Measurement is presentational, without network time and physical devices; does not mean a minimum guarantee 60   FPS In every frame. Windows/Intel runtime, physical plaice, hand-held party and host have still not been tested.

<a id="regresje-krawędzi-i-dwukliku--ui-81ui-82"></a>
## Edge and double-click regressions — UI-81/UI-82

Reproductions and subsequent runs are included in `.artifacts/ui82/`; each `import-N.json` contains SHA real isolated project files. The sandbox scene and the user build profile are not copied to it. The server runner remains `scripts/test-client.sh` (HTTP/WS/PostgreSQL), with `WOTR_UNITY_EDITOR="$PWD/.artifacts/ui25/unity-isolated.py"` and separate `-testFilter`, `-testResults` and `-logFile` for each batch.

The new `EngineMouseInput` uses PointerState and conversion InputForUI attached Unit 6000.6.0f1. The engine calculates the next clickCount; Pick, capture and ClickDetector remain real. This test of the engine input layer, without declaring physical mouse clicking OS. Calibration has detected that the previous manual count1/count2 omitted a series of clicks started on another object. Both surfaces of the discard have a reproduction of FAIL before the correction, described in UI-82.

`map-correction10.xml` **7/7 PASS**: six methods of MapViewport and correction of the choice of the second army. The map is tested for both sides, 1366/1920, 100/125%, exact edges/corners, figure under motionless mouse, LeaveWindow/Enter, capture, Menu/Settings and the limit of the single axis. The tests retain cover, match status and zero navigation commands.

`card-map8.xml` contains **14 PASS cards/inputu and 1 FAIL maps** (map repaired in progress10). We do not describe the entire report as PASS . Rejection of both sides 8 → 7 → 6 with miniatures and previews includes 32 single command in eight games, receipt, no strangers ID and protection against third click. Also checked count1 as a fresh series and transition of the second press into drag. PNG confirmed full text and available targets in four layouts. Further regressions, final Editor and builds are a separate step acceptance .

Final Play for UI-81/UI-82: **52 different methods with the latest PASS**, without counting the repetitions. `hunt-layout13.xml` 12/ 12 also checks the stability of the first target for three reactions, four systems and open/close sloped Fellowship . `cards13.xml` 2/ 2 requires POST After the second click before the tail; covers both surfaces, both sides and 32 single discards. Previous failures and fixture fixes are public in UI-81/UI-82.

Full End Editor13 **608/608 PASS**, macOS Universal and Windows x64 built; 303 the sources of runtime of both builds have SHA tested snapshot13. Pure macOS smokes PASS , the Login form was viewed, the player profile was restored. [Index UI-81/UI-82](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui82-validation.json) reports, environment and continuity of sources; commands are in UI-82 Final inspection24 PNG Hunt and 2 re-entered cards without kolizji/clippingu. Windows/Intel runtime, physical devices, hand batch and host without acceptance ; previous measurements FPS ed UI-77 are not a new measurement of this version.

<a id="rzeczywisty-stary-backend-i-zamykanie-ręki--ui-83"></a>
## Real old backend and hand closing — UI-83

The UI-82 tests used the current test server. They did not prove the compatibility of the new client with the older image running on localhost:8443. UI-83 has played an error on the actual copy of the recording and DLL from the running container: min2 rejects single ID, min1 the current server supports 8→7→6. The copy in memory does not execute user-based commands.

The new CardServerCompatibilityTests checks the gestures of both pages in the thumbnail and preview content: the old contract displays a full message, does not change the selection and does not send POST. RED before implementation, GREEN after it. HandDiscardDrawerTests checks Close/Esc, reopen, refresh, new decision and no automatic opening for the opponent's decision.

Checkpoint: integrated-green.xml 7/7 Play PASS , editor.xml 608/608 PASS , server-regression.trx 13/13 PASS ( HandDiscard , SaveMigration , MigrationRecovery , ActionLogMigration Build server without warning. Backup and restore PASS ; the trial and the local reboot target retained the checkssumy of all four records/revisions. HTTPS health200 verified with local certificate CA Default storage CA Python didn't know that. CA ; verifications not excluded TLS Private drop and backup are outside Git. Final recovery-green.xml: 3/3 PASS ; total 9 different Play methods. Fresh builds macOS Universal and Windows x64 PASS macOS dragons and profile behavior control PASS None. Windows/Intel runtime and manual acceptance . [Index UI-83](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui83-validation.json) and UI-83 contain the sum of the reports, actual instructions and previous failures.

<a id="routing-ekranu-i-natywna-granica-okna--ui-84"></a>
## Screen Routing and native window border — UI-84

The UI-81 tests sent PointerMove directly to visualTree, giving Map.worldBound coordinates. They checked geometry and timer, but bypassed the panel selection and the conversion of screen coordinates. The EngineMouseInput driver with UI-82 also assigns the target via Pick/SendEvent; the correct click counter does not provide evidence of native routing at the window border.

EngineScreenPointer ed UI-84 Passes through DefaultEventSystem and FindTargetAtPosition/ScreenToPanel Unity fastened 6000.6 .0f1. RED `.artifacts/ui84/screen-red2.xml` recreated the right bank ( 1366,384 ): routed=false, newMoves= 0 , and Leave carried the previous ( 1362,384 ) which stopped the timer. GREEN `map-green.xml` : **8/8 PASS**, both sides, 1366/1920 and 100/125%. Includes exact boundaries, last pixel, corners, motionless indicator, outside/focus/presence/buttons/capture , Menu/Settings , inside Cards, cover and further traffic in the corner when one axle reached the limit.

A separate Development Player MapEdgeProbe bypasses login, API and saved sessions. A CoreGraphics system event-controlled cursor goes through macOS to Player; it only watches and samples the same source MapScreenPointer as the client. It does not use SendEvent or a guard of synthetic input. It was built with separate company/product/bundle ID to not read or replace the profile of the active user's clients.

The native RED `native-red-partial-read/trace.jsonl` retained 20 with subsequent focus samples and RawMouse.x=1366 with 1,04 s: Active=false, Sir unchanged. The error in reading the incomplete last row JSONL was about the wrapper; the complete closure trace confirms this reproduction. Earlier attempts with the wrong focus were not counted as the matrix of four edges.

Native GREEN for runtime `34cdbd8`, macOS/Apple Silicon, Retina:

| Mode | Render | Scale UI | Observation on each of the four edges |
| --- | --- | --- | --- |
| Windowed | 1366×768 | 125% | 21/21 Active samples, continuous change of the Pan in the right direction |
| FullScreenWindow | 3024×1890 | 125% | rights 20/20, other 21/21 samples Active, continuous change Pan |

In both runs, RawMouse reached the exact x=Screen.with and y=Screen.height; left and lower sample respectively had x=1 and y=1. In window mode, the right exit (x=1382) and the title bar (y=784), still with focus, gave after 21 samples Active=false and zero change Mr. Reports: `.artifacts/ui84/native-green-window-1.25/result.json` and `.artifacts/ui84/native-green-fullscreen-1.25/result.json`.

The fullscreen test with forced 1366×768 on the screen 3024×1890 had lanes outside the render: the physical upper/bottom edge gave RawMouse.y=811/−43. `native-green-fullscreen-letterbox` is not a viewport limit test; the correct run used native resolution. The panel scale is not a simple DPI multiplier: the reported PixelsPerPoint was 1,25 in the window and about 2,92169 on full screen at the same UI 125% setting.

This is proof of automatic system input, not manual mouse or plaice handling. The native test does not include horns, scale 100% , other monitors or Windows/Intel These limitations are not replaced by a math test. FPS . Final `.artifacts/ui84/editor.xml` : **608/608 PASS**; `cards-green.xml` : **2/2 PASS**, both sides and surfaces cards in four systems, 32 single discards. Including map-green is **10 different Play methods with PASS**, without counting native samples as Play Tests. Buildy nonDevelopment macOS Universal and Windows x64 PASS ; smoke macOS PASS , Login form viewed. SHA Runtime sources of both builds according to the tested import4 and workspace, were also verified the sum of the result files. Player profile: five files without byte changes, both preference domains without changing values. [Index UI-84](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui84-validation.json) record reports and limitations; UI-84 gives the instructions. Probe instructions: [DEVELOPMENT](DEVELOPMENT.md) .

<a id="panele-na-krawędziach--ui-85"></a>
## Panels on edges — UI-85

acceptance checks left exclusivity also for pinned panels, click activation and NavigationSubmit, vertical full order Fellowship/Hunt, Cards at the physical bottom of the bank and Victory in Menu. Four layouts: 1366×768/1920×1080 and 100/125%, both sides. The hand of six cards is also present in the matrix of five Hunt decisions; all buttons must be reachable without scroll and the answer must retain its point of the cursor.

When the column is above the available height, the hand grid extends to the left of the lower Cards, above dice. This does not change the opening or election status of cards. Inspection bypasses the entire column or the extended hand. This behavior is checked with full hand, not with empty fixture only.

The map regression uses a new real outline of the Card section, including its help at the right edge. The approach sample from the screen is 2 px from the shore, inside the existing 4 px pass; the exact maxima/last pixel and continuous movement are still being checked. The attempt of the incoming army sets its camera in a visible luce between Hunt and Cards instead of setting up an empty half of the right bank. Interior, capture, focus and modal still block you. Older Hunt closing tests and hand-shifting have been adapted to the new contract. The movement of other windows continues to check the preview of card.

The results and end commands belong to UI-85.

Final results UI-85 : **11/11** System/ Menu/Hunt * 2/2 ** card * 8/8 **map and** 2/2 **Network Fellowship, without omissions — total** 23 different Play methods**. Full Editor:** 608/608 PASS **. Saved 28 Drops; six were viewed covering four variants and dense Hunt. Imports 8/9 have the same runtime; they differ in three fixtures maps. Both builds nonDevelopment, macOS Universal and Windows x64, have compatible runtime inputs and checked totals of all the output files.

Smoke macOS PASS and Login viewed: separate company/product/bundle, the same runtime, another executable. Active client was not closed, its profile was not ed/czyszczono; the aggregate print before/after changed during its operation, so we do not count byte invariability. No new attempt Windows/Intel, hand batch, FPS and native input OS. Details, previous failures and sums: [index UI-85](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui85-validation.json).

<a id="ilustrowany-górny-pasek--ui-86"></a>
## Illustrated upper bar — UI-86

`Tests/Editor/TopBarTests` check contract 17 texture ( 2 × logical size, import without mipmap/compression , alpha and visible pixels), clean `SideResourceCounts` (also Spent by `SpentWith` ), update of chips in place, order and inactivity of decorations and no reconstruction dice opponent after selection card . `Tests/PlayMode/TopBarLayoutTests` uses real views `ui09-resources` with script generator: Shadow opponent has 10 dice and 2 Special, or budget for the width of the bar. For both sides, 1366 × 768/1920 × 1080 and 100/125% check height 56 pt, placement and no application of elements, full text of the phase outside pass Edge pan, compact mode, projection equal meters, disbanded textures, shadow as the first child stage and the fact that frame and shadow are not the target of Pick.

The regression includes 16 classes of Play touching the bar (edge layout, settings, battle, expansions pool, dice click, Menu, edge pan, hower, reconnect, Hunt, action context, map visibility). We compare failures with the same isolated checkout switched to the previous commit (`.artifacts/ui86/use-tree.sh`), instead of assuming their cause.

Results UI-86 : Editor **613/613**, TopBar Play **1/1**, final regression **59/65**. Six failures occur identically on `a2bf1f6` (after UI-80/UI-85 ): BoardCompletionLayout , ClickInspection , ContextHover , InterfaceSettings CoveredControl and two MapVisibility The only regression UI-86 (Hunt: hover on phase in pass Edge pan) has been repaired and has its own assertion. Details, builds and limitations: [index UI-86](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui86-validation.json) .

<a id="lewa-listwa-i-górna-oprawa--ui-87"></a>
## Left strip and upper frame — UI-87

`Tests/Editor/UiArtKitTests` takes over the graphic contract with TopBarTests and includes the whole set ( 39 texture: size 2 ×, import, alpha) and checks that in `Resources/Wotr/Art/UI` there is no texture outside the contract. `Tests/PlayMode/ LeftRailLayoutTests` (peace `map15-navigation` , both sides, 1366 × 768/1920 × 1080 , 100/125% ) opens each tab and checks: red lacquer only selected, tab icon and headers without Pick, panel frame and header, square buttons within the header, coats of arms of all nations in the name field, tokens in the page enamel, common width of the axis and name legend and entry frame.

Regression 23 Play classes (left panels, bar, map, tooltips, windows, Menu, Hunt): 68/78. All the failures repeat identically on the earlier code in the same checkout: six on `a2bf1f6` ( UI-86 ) and four newly launched on `f5a9161` (stitching pinned already panels, hidden bookmarks Victory/Hunt au UI-85 The only regression UI-87 (Characters closing button sticking out of the header through the global `min-height` action ) was repaired before the regression. Full Editor **614/614**. After rebooting the test command system, the test command was started from `LC_ALL=en_US.UTF-8` , because `LC_CTYPE=UTF-8` Stops the start PostgreSQL 17 . Details: [index UI-87](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui87-validation.json) .

<a id="przydział-polowania-jednym-zatwierdzeniem--ui-88"></a>
## Allocation Hunt one approval — UI-88

`HuntInteractionTests` (Editor) checks switching dice clicks in one set, limit without quiet switch, hints of the pool, minimum, drop on the Eye, snapshot/rekoncyliacj , reading an old snapshot from `HuntAllocationDie` And the Eye as an inconsistible drop target. `HuntNetworkTests` (Play, real server): two dice by clicking, withdrawing from the pool and from the Hunt window, drop, click Eye without command survival refresh/reconnect And one command `AllocateHunt` after Allocate & roll; legal zero; minimum required on the limit. Assents of the allocation test contrary to the attached Hunt window with UI-85 (opposite window closed, Hunt tab, Close hiding) replaced by state UI-85 .

Network tests must be divided into packages with a separate server process: login limit ( 20 for a minute) with rapid defeats he gives `TooManyRequests` Three defeats. HuntNetworkTests (Miss, KeepRoll , NoDice ) and three FellowshipGuideNetworkTests are identical to the `520aa1b` before UI-88.

<a id="podokna-zamykane-z-rodzicem--ui-89"></a>
## Doors closed with parent — UI-89

`WindowFamilyTests` (Editor, 4 ) check the parent from the opening button, the absence of a parent outside the windows, cascade, independence from the unbound window, forgetting the window closed on his own, the parent's behaviour when the navigation inside the window, transferring the connection and stopping checking every frame. `WindowFamilyNetworkTests` (Play, real server): Effects → preview card → × Effects closes preview; preview → “?” → closing Effects closes cascade preview and help; help with Politics closes when opening Effects; help with map buttons and help pinned stay; report battle from the log entry closes with card The unfaltered help closes also after clicking outside of it, so in the tests the cascade of the parent closes without such a click.

<a id="ilustrowane-fellowship-i-hunt--ui-90"></a>
## Illustrated Fellowship and Hunt — UI-90

`RightColumnLayoutTests` (Play, `map15-navigation` and `ui34-hunt-allocation`, both sides, 4 layouts): frame textures, header and buttons, icons without Pick, header ≤ 28, buttons ≥ 24, text titles, button in header with readable status and location, line Guide only without portrait, portraits in one line, empty Hunt as header with dice number, active assignment with content and no counter in the header, no command. The `ReportsPanelHeightsForComparison` test uses only names before UI-90, so the same file measured the column before and after changing. `FellowshipTrackTests` (Editor) checks the line Guide and the transfer of the state line to the header.

<a id="karty-na-pergaminie--ui-91"></a>
## card on parchment — UI-91

`HandCardLayoutTests` (Play, `ui31-discard` , both sides, 4 systems: no ‘Cards’ and visible ‘?’ header, fixed hand, parchment texture, up-end development at the fixed bottom, full width, card on top, no other moves, full text fits, transition ≤ 200 ms, developed card keeps the indicator above the covered card , wraps after the ride; arms first click (light, seal, foot), disarm Esc and click off card without command , discard with a second click after a break longer than a two-click, both clicks over the covered card . Preview-dependent tests after a click, a smoky over card or the ‘Cards’ tab has been adapted to the weapon ( EngineHandDiscard , CardServerCompatibility , CardInteraction , MultiUseDice , RuntimeTooltip , HandDiscard , BattleWindow , BoardMenuNetwork , ReturnRecovery ).

<a id="podgląd-lewej-listwy--ui-92"></a>
## Left strip preview — UI-92

`LeftRailPreviewTests` (Play, `map15-navigation`, 4 layouts): hover develops Politics as a preview with a curtain (nation token unavailable), animation ≤ 200 ms and completed, cursor over the panel maintains preview, slide down; click the tab pin; preview Effects at the attached Politics and return Politics; click the Characters curtains clip and forgets the earlier panel; no commands.

<a id="efekty-na-rozwiniętej-karcie--ui-93"></a>
## Effects on the developed card — UI-93

`HandCardLayoutTests` checks that after the first click of the focus border, it is surrounded by developed lico (indent 3 pt), and waiting for animation is based on a condition with a limit instead of a constant delay. EngineHandDiscard was unstable because the rolled lico by 140 ms blocked by the exposed card; after the change it passes in subsequent runs.

<a id="treść-karty-w-effects--ui-94"></a>
## Content of card in Effects — UI-94

`EffectsNetworkTests.AssertCardInPanel` check when the source clicks: no window card , the text of the event, the type and fight of the catalog on the parchment and the panel above the bottom pass (scene `board-stage` It is used by Effects (Inspect, Expire from "Effective ended" after reconnect), CardPack , LordsFinalCards and WindowFamily . `EffectsLayoutTests` in four layouts requires panel inside the scene and uncut labels card ( `TextFits` ), RemoveProtection hidden text during payment, and text editor tests card , the condition ‘In play’, the lack of parchment in capacity and card special. UI-89 (window → card → help) checks the preview opened directly from the panel, because the Effects entry no longer opens the preview. [ui94-validation.json](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui94-validation.json) .

<a id="płynny-ruch-drużyny--ui-95"></a>
## Liquid movement Fellowship — UI-95

`FellowshipMotionNetworkTests` declares Fellowship by one region through API and checks the `moving-fellowship` flight on both players and the track marker on the way (after 120 ms) and on the 0 field (after 620 ms). `FigureMotionTests` requires that the projection with the hidden progress does not interrupt the flight, and the new movement or removal of figure ended it. Reference: [ui95-validation.json](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui95-validation.json).

<a id="wydanie-bazowe--ui-96"></a>
## Base Edition — UI-96

`LobbyTests` checks the default range (Production without expansions , Development and Testing with expansions , overwrite), reject configuration and start table from expansions , back to base game , base start without sovereigns and HTTP 409 `ExpansionsUnavailable` ed `AccountHost(expansions: false)` . `ConfigurationScreenTests` check menu without switches expansions and return the older table to base game Unity tests with a real server use Development, so path expansions is covered. Index: [ui96-validation.json](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui96-validation.json) .

<a id="panel-kości--ui-97"></a>
## Panel dice — UI-97

`DicePanelLayoutTests` (both sides, four layouts) requires panel frame, pass header with uncut page name, medallions in the page's enamel with wall symbol texture ≥ 20 pt, descriptions without identifier dice and links, non-receiving dice, counter icon with name at the beginning of the description and icon Rings. `DieDescriptionsTests` checks short descriptions of all sides' walls, page differences, single-line status and symbol textures; `UiArtKitTests` includes new graphics. `CompactBoardLayoutTests` ensures that the simple state does not exceed 180 px. Index: [ui97-validation.json](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui97-validation.json).


<a id="zakładka-drużyny-i-polowania--ui-98"></a>
## Bookmark Fellowship and Hunt — UI-98

`EdgePlaqueTests` : Bookmark in the top right corner under the bar 44 pt (frame of panels, ≤ 48 pt, at the right edge), state, place, Progress, Corruption, eye number and dice Free Peoples according to the projection in four layouts and both roles; panels held by the decision (phase Fellowship allocation Hunt ) and rolled after it; the raid develops a full form above the hand without moving it, click and focus do not pin anything, zero commands. `LeftRailPreviewTests` checks interactive preview without a curtain, no click-on attachment, no focus-on keyboard, return the panel opened by context and no "keep open" throughout the tree. Updated: `TopBarLayoutTests` , `RightColumnLayoutTests` , `EdgeBoardLayoutTests` , `LeftRailLayoutTests` , `FellowshipPanelNetworkTests` , `FellowshipLayoutTests` , `HuntLayoutTests` , `ClickInspectionTests` , `BoardInteractionTests` , `MapContourInteractionTests` , `SovereignPanelLayoutChecks` , `TopBarTests` , `UiArtKitTests` .

Start in isolation from MENU-08 : separate checkout Unity and its clone APFS , a server from the commit export at ports 5098/5099 , Bases `wotr_claude*_test` played before each package ( `.artifacts/ui98/run.sh` , `regress.py` Packages of 4 Classes). EditMode au `2d9d897` : **630/630 - Full. PlayMode au `b877b1a` :** 453/552 **to** 450/555 **baseline `4bfbd79` in the same environment; 13 fixed cases, new differences checked by repeats (limit of queries, order, focusless discharges) or fixed in `2d9d897` Failures previously existing and their modified messages are described in the index: [ui98-validation.json](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui98-validation.json) Buildy macOS and Windows nonDevelopment with `2d9d897` ( 997 tracked compatible inputs), isolated identity smokes PASS with a profile identical byte.

<a id="stos-powiadomień--ui-99"></a>
## Notification stack — UI-99

`ActionNoticeLayoutTests` (four layouts, multiple scenario action opponent: one stack instead of a row cards , the latest banner in front, number on the seal, layers of older banners, enamel of the opponent's side; hower reads nothing; click immediately removes the banner and opens card , second click replaces card the next; card on parchment with a lattice header and dark ink, map area unchanged. `ActionNoticeNetworkTests` ensure that the step added in reading returns as unread; `UiArtKitTests` includes new graphics. EditMode ** 630/630 * Regression 20 classes reading notifications, card and history ( `runs/ui99reg.json` )**0 new and changed failures** towards the same classes in full UI-98 , 2 fixed. Buildy macOS and Windows nonDevelopment with `629aa31` ( 1011 tracked compatible inputs), isolated identity smokes PASS Index: [ui99-validation.json](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/ui99-validation.json) .

<a id="ikonki-powiadomień-karta-na-środku-prawy-klik--ui-100"></a>
## Notification icons, card in the middle, right click — UI-100

**Tests.**
- `ActionNoticeLayoutTests` (four systems):
  - a stack of flags for 44 entitled, without labels, tooltip with the latest text action, number on
    the stamp, the enamel of the opponent;
  - Log icon opens and closes the list before the stack;
  - card in the middle of board (tolerance 2 pt) and below the notification bar, on first and
    subsequent notification;
  - no order `map-controls`.
- Rescribed region reading tests: `T22InformationTests`, `DecisionNoticeNetworkTests`. Right click
  on the tag and exposed area (`MapContourProbe.Inspect`, `ExposedTerrain`) retains draft and
  camera.
- Help opened with Menu (`BoardNavigation.Help`), Location from the Fellowship panel.
- The short master in the tests (`PanAt`) exceeds the drag tolerance.

The results.
- EditMode **630/630**.
- Aimed regression 30 classes of notifications, history, assistance, windows, maps and profiles
  (`.artifacts/ui98/runs/ui100-final.json`, with replays `ui100v` / `ui100w`): **83/101**.
- All 18 failures occur in the baseline with the same message: fresh run without UI-100 on `4e12bb7`
  (`base100*.json`) or the UI-98/UI-99. runs These are:
  - rebooting the server in an isolated environment;
  - TaskCanceled In the `LiveActivity`;
  - Codex tests awaiting a fixed right column;
  - profiles and other known failures.
- New failures found along the way and repaired before acceptance:
  - read right in a veiled place (test);
  - frame covered by card or Log List (`ActionMapLayoutTests`);
  - the Locate input through the tab (`MapContourNavigationTests`).
- `MapViewportTests` in baseline once 6/8, once 8/8 (unstable); with UI-100 8/8.
  `MapContourNavigationTests` after the 2/2. amendment

<a id="władcy-w-panelu-characters--ui-101"></a>
## Overlords in the Characters Panel — UI-101

**Tests.**
- New `SovereignSectionLayoutTests` on its own script `ui101-sovereigns` (copy `ui59-awaken` to
  prevent another test from awakening Dáin earlier):
  - portraits are part of the Charters panel and do not hang over the map;
  - the “Sovereigns” section is in four layouts, the buttons are free from curtains and the
    inscription with an empty list is hidden;
  - After selecting dice, the rolled-up tab has a goal distinction;
  - the panel is developed by the open window of the ruler, and when rolled up, the window stands
    next to the tab.
- `SovereignPanelTests` (EditMode): The awakened ruler from the character list hides the button, the
  others remain, the section itself is hidden empty.
- expansions tests open the rulers via `BoardNavigation.Sovereign`: the development of the tab, and
  for the awakened from the list of its entry in the list.
- Tests and “dense” profile check “?” Hunt and Location only with the developed panel. The rolled
  Fellowship panel is achieved by the corner tab and its value Corruption (UI-98).

The results.
- EditMode **631/631**.
- Regression 14 classes expansions, rulers, character and profile lists
  (`.artifacts/ui98/runs/ui101-final.json`): **31/38**.
- 7 failure identical to fresh baseline without UI-101 (`base101.json`, `f6b1633`): server reboot,
  `ClickInspection`, `CharacterRosterLayout` (Gandalf entry under dice), no profile file in
  `DesktopLayoutStability`.
- Fixed against Baseline: `InGameDenseLayoutTests` and `InGameProfileDriverTests`.

<a id="powiadomienia-bez-pasa-ponowny-klik-regionu--ui-103"></a>
## Notifications without pass, reclick of region — UI-103

**Tests.** New `BoardChromeLayoutTests` (fiction without server):
- notifications have `position: absolute`, hang at the lower edge of the top bar and are narrower
  than half of board;
- the scene and the stripe shadow begin with its lower edge;
- Right of the pick notification elements hits the map and the Log icon lies above the stage (four
  layouts);
- the second click on the region area and the army tag closes the region panel, the third opens, and
  the click on another region switches the panel; no commands.

The results.
- EditMode **631/631**.
- Regression 19 classes of notifications, regions, lists and layouts board
  (`.artifacts/ui98/runs/ui103-final.json` with replays `ui103u`/`ui103v`): **49/51** (two cases
  `T22` with limit 429 repeated separately: PASS, `ui103w.xml`).
- 2 failure also occurs in the baseline `base103.json` (`5580a12`, without UI-103): `ContextHover`
  Codexa and `T22` Dense about 12 entitled
- Fixed on the way:
  - card action pushed to notifications at 1366@125%;
  - The character inspector coming up on the dropdown list. `CharacterRosterLayoutTests` passes the
    first time from UI-98.

<a id="płótno-mapy--ui-102"></a>
## Map canvas — UI-102

**Export.**
- `scripts/export_map_canvas.py` in vev with `scripts/requirements-map-canvas.txt`: PASS. The re
  `--check` confirms the compatibility of exits with inputs saved in
  `docs/sources/UI-102-MAP-CANVAS.json`.
- `scripts/export_map_terrain.py --check` PASS after moving `MapTerrain.png` to
  `graphicalassets/maps/terrain-map14`.
- `graphicalassets/ui/render.py --preview` Previews unchanged.

** New `MapCanvasLayoutTests` (fiction, four sets):
- at `Fit` each of the 105 regions can be moved so that the fragment of its site is a map, not HUD;
- at the smallest approximate and camera in the corners the canvas covers the entire screen and
  `MinimumZoom` is less than `FitZoom`;
- The margin point shall not be illuminated, selected and not opened.

`MapContourRenderingTests` checks two textures DXT1   4096 ² without reading CPU and no layer `map-water`.  `MapViewportTests` and `InterfaceSettingsTests` count the cover and limit of the camera with `CanvasBounds`.

The results.
- EditMode **631/631**.
- Regression 22 classes of maps, cameras, regions and profiles
  (`.artifacts/ui98/runs/ui102-final.json`, with replay `ui102u`): **60/69**.
- 9 failure identical to the baseline `base102.json`/`base102b.json` (`cae8063`, without UI-102):
  - legend and visibility of tracers;
  - two unstable `MapViewportTests` edge tests (6/8 on both sides);
  - restart server;
  - `ContextHover` Codexa;
  - profile `characters`;
  - `T22` Dense;
  - `TravelGesture`.
- Overview of `ui102-margins-*` drop: Sea with waves and shore lines, seamless paper, edge of the
  game field only as a line of border regions.

<a id="bez-granicy-mapy--ui-104"></a>
## Without map boundary — UI-104

**Tests.** `MapContourBorderTests.TheFrameIsNotInkedWhileCoastsLakesAndBarriersAre` (EditMode): 13 episodes `Frame` without ink; shore, lake and barrier with ink.

The results.
- EditMode **632/632**.
- Regression 6 Map classes (`.artifacts/ui98/runs/ui104t.json`): **13/15**.
- 2 failures (legends, visibility of tags) are the same as in UI-102 And baseline.
- `ui102-margins-*` drop: no line on the top edge.
- Buildy macOS and Windows nonDevelopment with `921ecf4` (953 input compatible), PASS smokes.

<a id="ruch-wyłącznie-na-mapie--ui-105"></a>
## Movement exclusively on the map — UI-105

**New Tests.**
- `MapClickMoveNetworkTests` (PlayMode, `ui105-nazgul-army` and `ui105-nazgul-alone` scenarios):
  - Nazgûl elected before the soldier stays in the army, which moves after a few target(s) (user
    report);
  - Nazgûl himself flies after a few targets.
- `TravelActionTests.ADestinationClickTakesTheShortestLegalRouteWithinRange` (EditMode): shortest
  route, bypassing the blocked, no exit outside the detention region, will remain, flight.
- `MapContourProbe.AttemptDrag`: Army drag, figures and portraits do not capture and send anything.
- `BoardInteractionTests`: click on the target with the region panel open.

** Travel gestures, cards, expansions and characters have passed from the route steps, drag and chart of Characters on the figure click on the region panel and click on the target. The jointly approved travel tests (Book of Mazarbul, Nazgûli flights from cards Shadow) check the saved target (`PlannedRegions`) and Finish mobility. Tests `DraftInput` and `EngineDoubleClick` use reserve as a drag object.

Improved tests from previous tasks:
- `ArmyContextNetworkTests`: Click the banner after UI-103;
- `CompactBoardLayoutTests`: A bar with history icon after UI-100.

The results.
- EditMode **633/633** (`.artifacts/ui100/runs/ui105edit2.xml`).
- Regression 87 PlayMode classes with motion, panels and map clicks
  (`.artifacts/ui98/runs/ui105t.json`, then replays after one class `ui105r.json`, `ui105f`,
  `ui105s1`–`ui105s3`): **326/345**.
- 19 failure is a set of errors, confirmed in the bases `base`, `base100`–`base103`, `cand` and
  `final` same messages:
  - server reboot;
  - fixed column;
  - legend and visibility of tracers;
  - T22 Dense;
  - Victory;
  - BattleWindow;
  - HandDiscard;
  - Muster;
  - Fellowship;
  - Shadow04 and ShadowStrategy02 Layout.
- `TooManyRequests` (limit 20 logins per minute) forced replays after one class.

<a id="kolejność-lewej-listwy--ui-107"></a>
## Left strip order — UI-107

`LeftRailLayoutTests` checks the Politics, Characters, Effects (one under the other) sequence in four layouts for both sides.

The results.
- `LeftRailLayoutTests` and `EdgeBoardLayoutTests` **2/2**
  (`.artifacts/ui98/runs/ui107t.xml`).
- The `ui87-left-FreePeoples-characters-1920-1.00.png` drop confirms order.

<a id="wejście-postaci-dwuklikiem--ui-108"></a>
## Double-click input — UI-108

**Tests.**
- `ComponentGestures.Enter` (PlayMode) replaces the input button:
  - opens the Characters and checks the `entry-target` award;
  - single click does not open the inspector, two click of the line chooses the input.
- `CharacterRosterActionTests.AnEntryComesFromItsRowAndNeverHostsTheInspector` (EditMode): the only
  input for figure, no button in the inspector, no input without dice, tray in the dock.
- Input tests (Saruman, Witch-king, Mouth, Gandalf White, Black Snake with Council token, Balrog
  with recruiting, second player) use a double click and check for no window and button.

The results.
- EditMode **633/633** (`.artifacts/ui100/runs/ui108edit.xml`).
- PlayMode 14 character classes, expansions and recovery (`.artifacts/ui98/runs/ui108t.json`,
  `ui108r`):
  - all entry tests pass;
  - 3 failures are found (restart server, Setup, fixed column), such as in the baseline `base` and
    `base100`;
  - Batch 1 discontinued Unity start and was repeated as `ui108r` (7/7).

<a id="zwarty-panel-polowania--ui-109"></a>
## Hunt — UI-109

**Tests.**
- `RightColumnLayoutTests.CheckIdleHunt` at rest checks:
  - Class `hunt-idle`;
  - no report bar;
  - dice Free Peoples only at a number greater than zero;
  - height to 84 point.
- `HuntNetworkTests.Report`, `HuntActivityNetworkTests` and `MordorNetworkTests.Finals` open a panel
  from the corner tab (UI-98). After the completed Hunt they expect a compact panel: a report only
  in projections and history, without a late tile gesture.
- `EdgePlaqueTests` measures panels after the end of 140 ms development; earlier, the measurement
  during animation depended on frame time.

The results.
- EditMode **633/633** (`.artifacts/ui100/runs/ui109edit.xml`).
- PlayMode 10 Hunt, Mordor, right column and corner tab (`ui109t.json`, `ui109r.json`,
  `ui109s1`–`ui109s6`): all pass beyond `HuntResponseNetworkTests` 0/3.
- `HuntResponseNetworkTests` is a failure that is found from UI-98: the held decision panel blocks
  the `hunt-tab` sign that the click test is about. Other messages have the baseline `base`, `cand`
  and `final`.
- `ui98-hunt-FreePeoples-1920-1.00` dump shows rest: the header HUNT and the line ‘Eyes · 0’.

<a id="ruch-drużyny-ikoną-toru--ui-110"></a>
## Move Fellowship track icon — UI-110

**Tests.**
- `FellowshipPanelNetworkTests` (movement):
  - After selecting the dice the progress track symbol has the `track-move` class and visible light;
  - no ‘Move’ button;
  - The symbol click sends `MoveFellowship` and extinguishs the glow;
  - drop `ui110-move-glow.png`.
- `FellowshipLayoutTests` checks the symbol without dice: without the light, disabled, without the
  “Move” button.
- EditMode:
  - `FellowshipTrackTests`: the only track buttons are disabled symbols, and after `OfferMove` only
    visible track is switched on;
  - `SideControlTests`: no traffic button in the action row;
  - `UiArtKitTests`: texture `ornaments/move-glow` 44×44.
- `FellowshipTrackLayoutTests` opens the panel from the corner tab (UI-98) and measures the tracks
  with the new button.

The results.
- EditMode **633/633** (`.artifacts/ui100/runs/ui110edit3.xml`).
- PlayMode 10 Fellowship, Mordor, right column, corner tab and Victory (`ui110t.json`, `ui110g2`
  2/2) go beyond errors found:
  - `FellowshipNetworkTests` 1/2 („The same projection does not undo Close”);
  - Victory 3/5 as in `base100`;
  - `FellowshipTrackLayoutTests` 1/2.
- `FellowshipTrackLayoutTests` passes track measurement and falls on Esc in the open code panel
  (UI-98, previously falling on the ‘permanent column’).
- `MordorNetworkTests` **7/7** after one method (`ui110m1`–`ui110m7`): a motion attempt with Mordor
  track icon and game end.

<a id="proste-powiadomienia-i-pergaminowe-karty--ui-106"></a>
## Simple notifications and parchment card — UI-106

**Tests.**
- EditMode `ActionCardEffectsTests`:
  - the effects without source, the dice, the budget and the operating party, when the full record
    retains them;
  - dice assigned to the Hunt field as ‘Hunt Box +N’;
  - the sentence of the notification without the website.
- `ActionNoticeLayoutTests`: tooltip flags are `Sentence` in one row, without the opponent's page
  name.
- `ActionCardLayoutTests`: card without cost line and dice, without step and source headers; page
  only once, in the header.
- `CardViewportLayoutTests`: card open from hand on parchment with dark ink.
- `BoardCompletionContextNetworkTests`: card does not give dice.

The results.
- EditMode **636/636** (`.artifacts/ui100/runs/ui106edit3.xml`).
- PlayMode 13 history classes, notifications, cards and battles ( `ui106t.json` , the replay cards
  after the last amendments `ui106r3` 3/3 ) passes beyond the errors found, such as in the baseline
  `base` , `cand` and `final` :
  - restart server (InGameContext 4, BoardCompletionContext 1);
  - Setup;
  - LiveActivity.
- Drops:
  - `ui100-card-*` ed `ui106r3`: „Shadow · Discard Die” ed „Discarded a die
    without effect.” and „Shadow · Allocate Hunt” ed „Hunt Box +1”;
  - `map17-card-fit-1920-1.00`: Parchment card in hand.

<a id="niedostępne-karty-walki-z-warunkiem--ui-111"></a>
## Inaccessible card combating condition — UI-111

**Tests.**
- EditMode `CombatCardConditionTests`:
  - condition is the first sentence of part of the fight (We Come to Kill, One for the Dark Lord);
  - battle window shows card not available: disabled, without `DraftToken`, with condition; card
    allowed works.
- `BattleWindowNetworkTests` (systems) counts card available and unavailable, and not available must
  have a condition and be disabled.

The results.
- EditMode **638/638** (`.artifacts/ui100/runs/ui111edit.xml`).
- PlayMode 7 battle and cards (`ui111t.json`) classes pass beyond the existing
  `BattleWindowNetworkTests.RegionClickClose…`, the same timeout as the `base`, `cand` and `final`
  baselines.
- The `ui44-combat-1920-1.00` drop shows available card and grey Scouts with the condition “Requires
  the Free P...” (full in tooltip).

<a id="wynik-menu-02--2026-10-11"></a>
## Result MENU-02 — 2026-10-11

`WOTR_TEST_DB='Host=127.0.0.1;Port=55448;Username=wotr;Database=wotr_menu_test' bash scripts/test.sh --filter 'FullyQualifiedName~OpenRegistrationTests|FullyQualifiedName~AccountInputTests|FullyQualifiedName~LobbyTests' --logger 'trx;LogFileName=menu02.trx' --results-directory .artifacts/results`: **30/30 PASS, 0 skip**. `.artifacts/results/menu02.trx` report. Open registration, normal permissions, active session, logout, standardization, name race and previous invitations and configuration received. Tests work on separate 55448 cluster; `scripts/test-menu.sh` captures this selection.

<a id="wynik-menu-03--2026-10-11"></a>
## Result MENU-03 — 2026-10-11

`./scripts/test-menu.sh --no-restore --filter 'FullyQualifiedName~PublicRoomTests|FullyQualifiedName~MenuMigrationTests|FullyQualifiedName~LobbyTests' --logger 'trx;LogFileName=menu03-final.trx' --results-directory .artifacts/results` : **12/12 PASS , 0 skip**. Migration 006 preserves old private rooms, places/codes/Versions , designates a historical host and perpetuates the name in the party header. The public list has a separate indexed projection and pagination ID ; does not serialize RoomView Parallel joint allows one guest, retry preserves the version/place. The first attempt did not compile fixture with ambiguous new(); the second 10/12 revealed no enum converter in fixture HTTP Fixed open type and use BoardCatalog .Json; runtime of the protocol did not require a change.

<a id="wynik-menu-04--2026-10-11"></a>
## Result MENU-04 — 2026-10-11

`./scripts/test-menu.sh --no-restore --filter 'FullyQualifiedName~MenuLifecycleTests|FullyQualifiedName~PublicRoomTests|FullyQualifiedName~LobbyTests' --logger 'trx;LogFileName=menu04.trx' --results-directory .artifacts/results`: **14/14 PASS, 0 skip**. One transaction and room lock protect zaproszenia/Ready/leave/start. New rooms accept only the base also in Development; host-only start, unready, code invalidation, idmpotent leave and rebooted readings received. Eight races host/guest leave with start have retained all participants of each started batch. Old tables without name retain common configuration/start.

<a id="wynik-menu-05--2026-10-11"></a>
## Result MENU-05 — 2026-10-11

The new GatewayScreen and the open Login/Register; Settings stores the server address only before logging in. The old AuthScreen remains for tool testing compatibility. `bash scripts/unity.sh -runTests -testPlatform EditMode -testFilter GatewayScreenTests -testResults .artifacts/menu05-editor.xml -logFile .artifacts/menu05-editor.log`: 3/3 PASS. `WOTR_TEST_DB=…55448… WOTR_CLIENT_TEST_PORT=5098 bash scripts/test-client.sh -testFilter Wotr.PlayTests.AuthRegistrationTests`: 1/1 PASS, 0 skip; actual registration, logout and login. Runner allows the isolated port HTTP.

<a id="wynik-menu-06--2026-10-11"></a>
## Result MENU-06 — 2026-10-11

MainMenuScreen/MainMenuBrowser and HostGameForm use API public/rooms/matches. Two lists, separate cursors, stable lines, generation cancellation and polling 5 s; preserved history/filters existing records. Host by default public/Free Peoples/database ; privacy without expansions . `bash scripts/unity.sh -runTests -testPlatform EditMode -testFilter MainMenuTests -testResults .artifacts/menu06-editor.xml -logFile .artifacts/menu06-editor.log` : 2/2 PASS . `WOTR_TEST_DB=…55448… WOTR_CLIENT_TEST_PORT=5098 bash scripts/test-client.sh -testFilter Wotr.PlayTests.MainMenuNetworkTests` : 1/1 PASS , 0 skip, two accounts create and attach by clicking on the same room named. `.artifacts/menu06-play.xml` .

<a id="wynik-menu-07--2026-10-11"></a>
## Result MENU-07 — 2026-10-11

WaitingRoomScreen/Controller: Ready/unready , host-only start, host-only private invitation and copy of the code, polling 2 s, clear pregame leave and return after the host closes. MatchSession and safe exit board remain the write limit; the expired session turns to login. Settings disconnects server editing after logging in. `bash scripts/unity.sh -runTests -testPlatform EditMode -testFilter 'WaitingRoomTests;CorrespondenceClientTests' -testResults .artifacts/menu07-editor-final.xml -logFile .artifacts/menu07-editor-final.log` : 6/6 PASS New MainMenuNetworkTests : 2/2 PASS (Report `.artifacts/menu07-play.xml` ), both sides on the same board and after return the same revision; private kody/leave/cancel. `WOTR_TEST_DB=…55448… WOTR_CLIENT_TEST_PORT=5098 bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.ReturnRecoveryTests;Wotr.PlayTests.BoardMenuNetworkTests;Wotr.PlayTests.InterfaceSettingsTests'` : 11/11 PASS , 0 skip, report `.artifacts/menu07-regression.xml` ; real process reboot and receipt without re-cost. First regression 10/11: the focus test indicated hidden hunt-tab; it was replaced by a visible open-board-menu without changing the blinding assertion.

<a id="historyczny-menu-08--2026-10-11"></a>
## Historical MENU-08 — 2026-10-11

. NET 3740/3740 , final EditMode 627/627 and focused PlayMode 16/16 PASS ; both nonDevelopment builds and isolated smokes PASS Full PlayMode 439/553 , 114 FAIL ( 36 rate limit, 78 The Baseline confirmed two identical camera errors; 8 map tests PASS separately, 76 other failures without distribution. MENU-08 Blocked; details and actual commands in MENU-08 .

After integration `0a77d64` completed UI-97 : Full EditMode **630/630**, menu server **17/17**, separate packages PlayMode **10/10 + 7/7 PASS**, without omission. 16/17 hit the login limit in ReturnRecovery ; both separate server processes have passed exactly the same 17 cases. Menu tests record recoverys in temporary directory. Both builds and end-of-life dragons PASS , 873 runtime files compatible, existing profile 16 files unchanged. Smoke performs the recording tests after the release PlayerPrefs : the first parallel course did not give a meaningful comparison of the profile. Full . NET of an isolated course has identical inputs after integration; full PlayMode it was not repeated and still blocks MENU-08 Commands and reports: MENU-08 , index `merged_verification` In [menu-validation.json](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/menu-validation.json) .


<a id="izolowana-regresja-menu-08"></a>
## Isolated regression MENU-08

User ordered repair of all 114 previous failures, followed by isolation from UI-98/99. Branch `codex/menu-tests` based on `add6cdf` ( UI-97 We do not consider historical reports before the change as acceptance Claude’s design.

Full isolated .NET: `WOTR_TEST_DB=…wotr_menu_regression_test bash scripts/test.sh --no-restore --logger 'trx;LogFileName=menu08-isolated.trx' --results-directory .artifacts/results`: **3740/3740 PASS, 0 skip**. EditMode `isolated-editor-05.xml`: **631/631 PASS, 0 skip**, including a new check on the group's open stay in the start region. Previous attempts failed to perform the tests; Editor03/04 had 630/631 by Burst process failure.

`scripts/test-client-batches.py` accepts the full list of NUnit reports:

```sh
WOTR_TEST_DB='Host=127.0.0.1;Port=55448;Username=wotr;Database=wotr_menu_play_test' \
WOTR_CLIENT_TEST_PORT=5108 WOTR_CLIENT_TEST_RESET_DATABASE=1 \
python3 scripts/test-client-batches.py \
  --inventory .artifacts/menu-all-play.xml .artifacts/menu-merged-frontend-play.xml \
  --batching mixed --output .artifacts/menu08-regression/full-mixed
```

Total of the censuses includes 555 cases UI-97/menu , including two added after the first full run. Each package has its own server process, seed and report; the production limiter remains active. `429` retains the first report and renews only the affected case on a fresh server. Missing and skip cause failure. JSON save commit, hash diff and results of each package and hash tracked inputs. Change of input interrupts acceptance By default, the classes are separate; `--batching mixed` Packs the entire inventory of eight cases, keeping the fresh server and seed on the package. JSON The projection of completed packages can be preserved as a wasteless `.json.gz` . You must not edit Unity code during runtime.

`mixed` requires independent sessions between classes. `HandCardLayoutTests` and `HandDiscardNetworkTests` mutate common `ui31-discard`; if they reach one package, the second test can get seven cards instead of the required eight. Such classes are launched with the default `--batching class`, each on a fresh server and a session. We keep the first report and the exact condition of eight cards; the re-use and the cause of isolation must be public in the audience.

Filter CLI Unity interprets names as regular expressions. Runner quotes full names by `re.escape` and anchors `^…$` , also parentheses and quotes of parametricated cases. After each package compares the completed list with the requested one; the absence or excess interrupts the course. Non-zero process code despite the cases counted itself also requires clarification. `.artifacts/menu08-regression/inventory-audit.json` compared 553 methods with sources: 555 cases, zero missed or additional methods. `full-mixed` he started before the citation correction; three omitted parametric cases require a separate report.

`WOTR_CLIENT_TEST_RESET_DATABASE=1` is an open-ended opt-in seeder to remove and restore the scheme **dedicated, one-time test database** before the package. Guard the name ending `_test` runs before reset. We use a separate `wotr_menu_play_test` database, never parallel bases. NET or player data. Without this variable, the seeder retains the current action. `WOTR_CLIENT_TEST_REPOSITORY` transmits reboot tests and repository profile to the externally regardless of the nesting of reports.

`repair-gate-01` gave 12/16 PASS (all three parametrized Configuration done), and after the corrections of the remaining four `repair-gate-02` profiles gave 4/4 PASS. Final re-enactment of all 33 related classes includes 98 cases. `scripts/audit-menu-play.py FULL_JSON REPAIR_JSON OUTPUT_JSON` reads the original XML both runs, checks zero missing/outputs, final entries hashe and cover all primary 114 failures. The accepted source differences were clear in the UI-97 script; the report lists each amended file and report assigned to each case.

After the integration of completed UI-98/UI-99 The inventory shall include 557 methods and 559 cases, including three parameterized Configurations and a new Esc regression in the corner tab. Audit compares the full list with the attributes of current C# sources and checks the exact selected cases of each NUnit package. Historical inventory 555 not enough to acceptance This version. With the complete run on unchanged sources, both arguments `FULL_JSON` and `REPAIR_JSON` indicate the same report. If the entrances have been changed after full implementation, the audit requires an open manifesto `--reviewed-delta` (path → SHA256 before/after ) and all related classes by repeated `--affected-suite` The final re-up must include each class indicated in full and correspond exactly to the current inputs. The report gives the actual runtime differences; it does not use a fixed number of tests or a fixed list of files from the previous design version.

`--final-test-rerun RESULTS_JSON` allows you to audit the final revision of the script itself without resetting the unchanged classes. It requires identical runtime inputs against the main rerun, differences only in `.cs` PlayMode, a complete modified class and current `--reviewed-delta` manifest. The result retains the original failures and indicates the actual XML each case. UI10 after recovery waits for the unlocked tray: the disappearance of the Check result button can overtake the final GET. The test explicitly stops this response and checks the buses before release, instead of prolonging the permanent delay.

<a id="końcowy-izolowany-menu-08--2026-10-12"></a>
## Final Insulated MENU-08 — 2026-10-12

Audit**559/559 PlayMode** and everyone **114 primary failures** PASS , zero skip/none evidence includes a full frozen course 553/559 , re-entering all 47 related classes 160/161 and final full class UI10 3/3 After the test itself has been corrected, this is not one green run on the final commit. `ui99-final-audit.json` indicates XML for each case, 10 source differences ( 5 delivered client) and a separate final test change. Nine HTTP429 renewed openly on fresh servers; limit unchanged.

Final. NET **3740/3740** of PostgreSQL ( 903 current identical inputs), Editor **631/631**, four menu layouts and 20 The Watchers PNG PASS Both nonDevelopment builds and isolated macOS smokes PASS ; 891 identical runtime inputs, 16 profile files unchanged. All reports are in isolated worktree `/private/tmp/wotr-menu-tests` ; exact commands, previous trial errors and limitations are recorded in MENU-08 , the current index in [menu-validation.json](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/menu-validation.json) Windows runtime, Intel, target HTTPS and manual acceptance still postponed. Result remained on `codex/menu-tests` , without merge to the main checkout, push or publication.

<a id="menu-09--naprawa-lokalnego-lobby-2026-10-12"></a>
## MENU-09 — repairing local lobby, 2026-10-12

MENU-09 contains actual polecenia/raporty. Server with PostgreSQL **18/18**, full Editor **632/632**, frontend on the real HTTP **7/7**, recovery/Settings **11/11 PASS**, without skip. New attempts include public code and clipboard, > 20 tables, next page of entries, line and scroll behavior after refresh, independent list score at HTTP 404 and Back in the missing endpoint. Four layouts and 20 PNG It's a look-through.

The old active backend ui83 was found; copy/restore/migration 006 and the attempt to get new accounts on a separate copy preceded the update of the local backend. Control of all previous fields of nine tables and HTTPS/readiness with open CA: PASS. No commands were executed in actual batches. Additional ReturnList/ReturnDraft, builds and dragons have their own final entry in MENU-09; earlier global acceptance MENU-08 remains historic.

Final MENU-09 : additional ReturnListTests/ReturnDraftTests **3/3 PASS** after correcting the old Next click (first run 2/3 preserved). Total **21/21 different PlayMode**. `python3 scripts/accept-menu-builds.py --task MENU-09` : macOS Universal, Windows x64 nonDevelopment, isolated smokes and overview PNG login PASS ; 893 compatible runtime inputs and 16 profile files unchanged. [Index with hashs](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/profiles/menu09-validation.json) Full global PlayMode/server not renewed; Windows/Intel runtime and manual acceptance Adjourned.

<a id="plan-odbioru-dystrybucji--dist"></a>
## acceptance Distribution Plan — DIST

DIST-00 checks documentation, graph, DS01-DS20 and preserve earlier records. It does not run Unity/.NET or installation tests; preparing this plan is not proof of the update.

[DIST-01–DIST-08](release/DISTRIBUTION_PLAN.md) then requires:

- Independent builds/CI both repo without checkingout sources second and pinned of the disc / public
  content.
- API/WSS for a compliant, old, new and without headers; manifest signature, full package and atomic
  announcement of complete release.
- Downloaded signed installer on Windows x64, macOS Intel and ARM64; actually installed N → N+1 and
  maintain data after uninstalling and reinstalling. The editor and build Windows do not count
  runtime.
- Zero downloaded bytes of the package before consent, Later/Cancel/Resume, optional notification in
  the batch and required upgrade without game reset.
- Lost receipt before reboot, local write error, no space, wrong signature/hashu, broken
  transfer/substitutions and two instances.
- Public HTTPS, Range/206, WSS by Cloudflare/nginx, certificate renewal, PC reboot, common full
  Windows party ↔ macOS and backup/restore and follow-up decision in bitwie/Polowaniu.

Each result indicates the actual system/architecture, versions, SHA/digest, commands and report. Disaster attempts use isolated bases/profile; do not overwrite active player data. No Windows environment, Intel Mac, public IP or signatures are a condition to be removed before the appropriate acceptance, no deferred automatically inherited from T38/T39.

<a id="baza-podziału-repozytoriów--dist-01-2026-10-13"></a>
## Repository division database — DIST-01, 2026-10-13

Base `claude/dist-01` `155a7e0` : UI-100 – 111 plan DIST and MENU-08/09. Full EditMode 640/640 full PlayMode in packages 567/567 ( 0 missing, empty working diff), . NET 3741/3741 with unchanged backend inputs, macOS Universal and Windows x64 nonDevelopment with manifesto 1025 and isolated macOS smokes: PASS No skip, no orders, no reports, SHA and mapping the menu criteria are in DIST-01 .

Full PlayMode requires earlier EditMode in the same checkout: `DesktopLayoutStabilityTests` reads untraceable `.artifacts/ui13-profile-views.json`, which saves EditMode `Wotr.Tests.DesktopProfileFixtureTests`. Without this test, `FileNotFoundException` ([MONOREPO_DEPENDENCIES](release/REPOSITORY_SPLIT.md)) ends.

Full runs revealed shortcomings invisible in the targeted sets: rules `.has-sovereigns` incompatible with UI-101, tests before UI-105/UI-108, measurement of trays during request and premature recharge of menu list (`ReturnListTests` fell only in the `batch-051` package). Therefore, acceptance database requires one full run on the final commit rather than the sum of the re-runs.

<a id="przypięte-zależności-klienta--dist-02-2026-10-13"></a>
## pinned client relationships — DIST-02 , 2026-10-13

`python3 -m unittest discover -s tests/release -p 'test_*.py'` includes `test_deps.py` ( 18 (s) for `package_deps.py` and `bootstrap_deps.py` : Repeatable archives, the author herself, refusal to overwrite the version, bad hash, unknown version, incompatible Contracts/content , drift `Resources` , restore the older version and the private channel GitHub on a local mockup API (no access, token only to API Tests do not use `gh` user.

acceptance Client without server: checkout with `git archive` containing only `unity/Wotr.Client` , `release` , `unity.sh` , bootstrap and lock; bootstrap of empty `.deps` full EditMode And both builds. EditMode `PublicContentLockTests` check copy verification in build. Results, commands and SHA are in DIST-02 .

<a id="dwa-repozytoria--dist-03-2026-10-13"></a>
## Two repositories — DIST-03, 2026-10-13

Server: `python3 scripts/tasks.py validate` (with links `wotr-client:` and links to the attached client committee), `python3 -m unittest discover -s tests/packages` , cover and acceptance scripts, and `dotnet test` Client: `python3 scripts/check_docs.py` , `python3 -m unittest discover -s tests/release` , EditMode , PlayMode ed `test-client.sh` au pinned archives `wotr-server` and `wotr-seeder` and builds. Results in the fresh server clone and client checkout, including full PlayMode 566/567 and a repeat of the only error ( 1/1 ), save DIST-03 .

<a id="katalog-wydań-i-zgodność--dist-05-2026-10-13"></a>
## Releases directory and compatibility — DIST-05, 2026-10-13

Server (PostgreSQL in `WOTR_TEST_DB`):

```bash
dotnet test tests/Wotr.Tests -c Release --filter "FullyQualifiedName~ReleaseSigningTests|FullyQualifiedName~ReleasePublisherTests|FullyQualifiedName~ClientCompatibilityTests|FullyQualifiedName~ClientReleaseCatalogTests"
```

`ReleaseSigningTests` : Vector RFC 8032 , changed byte, alien key and fake key id, private key mode, SemVer a manifesto from `build-info.json` . `ReleasePublisherTests` : incomplete upload, no platform, extra file, manifest of other platform, bad hash, path outside download host, unsupported protocol, minimum and withdrawal, resumption of publication, lockout and four concurrent readers of the directory. `ClientCompatibilityTests` : matrix 10 identity for GET , login and WSS , repeated heading, transitional period, command without changing the revision and RNG and accounts, sessions, parties and receipts after changing minimum. `ClientReleaseCatalogTests` : a directory without sessions for both platforms, statuses, reloading, withdrawing and rejecting a directory with an incompatible manifest.

Client: `ClientIdentityTests` in EditMode (headnotes, stop after 409, identity text, generator, read the embedded resource, publisher key). PlayMode with `Wotr__Clients__AcceptUnversioned=false` proves that each client request carries the headers. Results: DIST-05.

<a id="minimum-z-terminem-naprawa-i-instalatory--dist-09-2026-10-14"></a>
## Minimum term, repair and installers — DIST-09, 2026-10-14

Server (PostgreSQL in `WOTR_TEST_DB`; on Omary in SDK `10.0.401` container with temporary `postgres:17-alpine` without published ports):

```bash
dotnet test tests/Wotr.Tests --filter "FullyQualifiedName~ReleaseSigning|FullyQualifiedName~ReleasePublisher|FullyQualifiedName~ClientCompatibility|FullyQualifiedName~ClientReleaseCatalog|FullyQualifiedName~ScheduledMinimum|FullyQualifiedName~ClientReleaseSchedule"
```

`ScheduledMinimumTests` : term and order after time ( `TestClock` ) refusal (too early, in the past, lower or unannounced version, no time limit to be appealed, `--at` without `--minimum` ), block of planned minimum, minimum immediate to the time limit, directory and scheme manifest 1 bad, missing or unnecessary installer. `ClientReleaseScheduleTests` : gate 401 → 409 exactly about the time (second before and at the time of the deadline), directory view before/after offer `installed` until the withdrawal, `/api/client/downloads` without the client's headers. `ReleaseSigningTests` : installer in signed manifest, refuse to sign without installer and with the same file, match fields `ClientReleaseManifest` with server type.

<a id="ikona-ostatniej-walki--ui-112-2026-10-13"></a>
## Last fight icon — UI-112, 2026-10-13

**Tests (wotr-client).**
- EditMode `LastBattleIconTests`:
  - the final round report hides ⁇ ; the report of another battle or without the completed round
    does not hide it; the same round in the next snapshot is hidden and the new round and the new
    battle show it; `RoundRead` once per round;
  - the tag in `NotificationReadStore` re-starts the batch and the same battle in another batch
    remains unread;
  - identifier The steps of the banners do not hide the icon.
- PlayMode `LastBattleIconClickTests` : Click ⁇ opens the report and hides the icon, focus passes to
  the report, Esc sent to the focus item closes the report, the same round is hidden, the next round
  shows the icon. `ui112-last-battle-read.png` and `ui112-last-battle-next-round.png`.
- `T22InformationTests` gives in each of the four layouts a new round, because the open ⁇ is hidden.

**Results** (`wotr-client/.artifacts/ui112/`, test server on port 5097, base `wotr_claude_play_test`):
- EditMode **651/651** (`editmode2.xml`).
- PlayMode  **65/65** in 21 classes (`play2.xml`) report of the last fight (T22Information,
  FloatingWindow,  BattleLayout, Battle,  BattleEnding,  LiveActivity,  WindowFamily) and all
  classes from Esc. `CouncilHomelandPanelNetworkTests`   5/6  (`play-council.xml`): Failure is
  `ApiException: TooManyRequests` test server login limit; this test separately on a fresh server
  1/1  (`play-council-rerun.xml`).
- `BuildTasks.BuildLocalMac` and `BuildLocalWindows`: Success.
- Smoke not made: launches Player with user profile.

The first course of PlayMode detected that the hidden under click of the ⁇ loses focus and Esc then goes to the root of the panel, except for `BoardEscape`. Correction: the report takes over focus; the second run above.

<a id="scenariusze-testowe--test-01-2026-10-13"></a>
## Test scenarios — TEST-01, 2026-10-13

`TestScenarioSessionTests` (PostgreSQL in `WOTR_TEST_DB`, HTTP by `AccountHost`):
- each scenario of the directory builds the same state twice, passes the recording and reading of
  `StateCodec` and starts with the declared page;
- `Wotr:Testing:Scenarios`: Development and Testing enabled, Production disabled without open
  `true`;
- Start: the owner sees Free Peoples , token places Shadow ; command the decision-making party
  increases the review; the list of games is empty;
- reboot: new batch with a state equal to fresh build (JSON with sorted keys), old batch 404, old
  token of the place 401, zero lines in `matches`, `match_events`, `command_receipts`,
  `test_matches` and `match_reads`; `DELETE /session` cleans the same;
- technical account: 403 for `/api/rooms` and `/api/test-scenarios`, 404 for a foreign party,
  without logging in; unknown scenario 404 `ScenarioNotFound`;
- three simultaneous launches leave one test batch;
- Without route options, the 404 is given.

Red check: no exclusion from the list of games and no `TestSeatGate` fail two of the six tests.

Results (`.artifacts/test01/`): class 6/6; full `scripts/test.sh` (validate, directory, sources cards, `dotnet test`) **3775/3775**, 0 Skipped (`test01b.trx`). First full run 3774/3775: `MigrationRecoveryTests` has a written migration list 1–6; after adding 7 passes. `tests/packages` 6/6. package tests

<a id="scenariusze-kości-podstawki--test-02-2026-10-13"></a>
## Scenarios dice base game — TEST-02, 2026-10-13

`BaseDiceScenarioTests` (engine and projection with `GameRuntime`, as on the server):
- for each of the 37 scenarios dice:
  - the option of the tested action is enabled for the page that starts;
  - die with the tested result is among dice paying action (traffic and attack costs, cards costs,
    character input);
  - For example, command (move, attack, card, policy, recruitment, character input, Ring, pass, Hunt
    allocation) passes `Validate` and `Execute` with randomness;
- each scenario has an expectation, title, description and group, and `docs/TEST_SCENARIOS.md` lists
  it in the directory.

`TestScenarioSessionTests.EveryScenarioStartsOverHttpWithItsSideToAct` launches each scenario by HTTP on PostgreSQL and checks the decision page with the owner's token or place; at the end, one batch of testing remains. `EveryScenarioBuildsTheSameStateAndStartsWithItsSide` covers the entire directory.

Results (`.artifacts/test02/`): 46/46 script classes; full `scripts/test.sh` **3815/3815**, 0 omitted (`test02.trx`).

<a id="scenariusze-testowe-w-kliencie--test-03-2026-10-13"></a>
## Client test scenarios — TEST-03, 2026-10-13

Client (`wotr-client`, packages from the `eac4afe` channel, then from the Releases):
- EditMode `TestScenarioClientTests` (5): a list in the folder groups with title and description;
  the TEST bar with title, description in the tooltip and button on the other side; `TestSession`
  game with the token of the place and switches to the owner's session; the menu shows the button
  only at the invalid directory; the server without mode (404) gives no directory, another error is
  reported.
- PlayMode `TestScenarioNetworkTests` with a real server (Development): menu → Test scriptios →
  `fp-pass` → board Free Peoples with a belt → pass ( `component-Pass` ) → Play Shadow: same party,
  Shadow has the decision and movement of the army → Restart: a new party with the same revision,
  decision and pass → Exit: menu; test parties are not included in the list of games, and the last
  one gives 404 Drops `test03-scenario-list` , `test03-scenario-free-peoples` ,
  `test03-scenario-shadow` .

Results (`wotr-client/.artifacts/test03/`, test server at port 5097):
- EditMode **655/655** (`editmode-full.xml`) before the 404 test is signed;
  `TestScenarioClientTests` class followed by 5/5 (`edit-new2.xml`).
- PlayMode 17 with menu classes, login, return and opening of the batch (change of
  `ClientFlow.OpenBoard`): 46/48 with one run (`play-regression.xml`). `ExpansionLobbyNetworkTests`
  (excess of starting time of eight lots) separately 1/1; `CouncilHomelandPanelNetworkTests` falls
  on `TooManyRequests` with the limit of logging also in the class (5/6 as with UI-112) and its test
  separately 1/1.
- Buildy `BuildLocalMac` and `BuildLocalWindows`: Success. Smoke not made (Player uses user
  profile).
- Contracts 0.3.0, content 1.0.2, `wotr-server`/`wotr-seeder` `eac4afe342fa` published as releases;
  `bootstrap_deps.py fetch --test` without local channel downloaded all four of Git Huba from SHA-256
  from the lock.

<a id="scenariusze-kości-i-akcji-dodatków--test-04-2026-10-13"></a>
## Scenarios dice and action expansions — TEST-04, 2026-10-13

`ExpansionDiceScenarioTests` (engine and projection with `GameRuntime`) for each of the 21 scenarios Lords, Council, Kings and Fate:
- the launch party has a decision and two designs give the same status;
- the option is enabled (Ruler chip: on the `RulerTokenActions` list);
- the specified die special or Ruler has the expected result and pays action;
- Example command passes `Validate` and `Execute`:
  - the choice of dice special, including forced Eye;
  - Card Draw, Use Balrog, dice Gothmog, Narya and Nenya;
  - Balrog's entrance, chiefs and chiefs paid for dice Ruler;
  - Gothmog, Théoden and Gimli's abilities;
  - The rings of Narya and Vilya, the Council's token, Denethor's token;
  - dice Ruler recoil, awakening, removing corrupt sovereign, attempt to corrupt.

`BaseDiceScenarioTests` now only includes base game catalogues, and document test and start by HTTP — the entire directory (59 scripts).

Results (`.artifacts/test04/`): `ExpansionDiceScenarioTests` 22/22; script classes together 56/56 before adding the last six; full `scripts/test.sh` **3837/3837**, 0 omitted (`test04.trx`).

<a id="stan-naciśnięcia--ui-114-2026-10-13"></a>
## Pressing state — UI-114, 2026-10-13

- EditMode `PressFeedbackTests`: button under indicator (also by children), button disabled and
  plain item without condition, element `pressable`, text field never; `UiArtKitTests` with five
  variants `pressed`. Full EditMode 659/659.
- PlayMode `PressFeedbackClickTests` (actual test server, port 5098): figure losses in the open
  window battle — `pressed` after PointerDown without selection, check when released, photo of the
  state after exit and no action after release beyond; Space keeps the condition on the focus
  control; map area — `MapRegionEmphasis.Pressed` for release, move ≥ 6 pt cancels without choice,
  release on site selects the region.
- Regression 15 classes (`play1.xml` 30/32 + improved `play2`/`play4`), including
  `BattleLayoutTests` with extended set of cosmetic transitions.

<a id="oprawa-okna-bitwy--ui-115-2026-10-13"></a>
## Window setting battle — UI-115, 2026-10-13

- EditMode `BattleArtTests`: figure from the atlas to the nation and type with number, four
  medallions of statistics with server values and modifiers in the tooltip, character medallions
  with a stamp of abolished ability and name line, garrison shelf only at figure outside battle,
  string of stages with tag; `UiArtKitTests` with 24 graphics `battle/` and scene wrap.
- PlayMode: regression of battle classes, windows and history (`play1.xml` 34/35) and
  `BattleLayoutTests` four systems with dropouts; failure of `BattleReportNetworkTests` is found
  (reproduction on `39a4e64`).

<a id="karty-walki-w-oknie--ui-116-2026-10-13"></a>
## card Fights in Window — UI-116, 2026-10-13

- EditMode `BattleCombatHandTests` : the first click armed without playing, the other tile click
  moves the weapon, the second click finalizes the same token ( `CommitCombatCard` ); card not
  available with and without tokens; card pages with reverse before exposure and parchment after.
- PlayMode: class regression cards, battle, cards policy and press (`play1.xml` 21/21) and four view
  layout with dropouts.

<a id="ilustrowane-kości-walki--ui-117-2026-10-13"></a>
## Illustrated dice combat — UI-117, 2026-10-13

- EditMode `BattleArtTests`: illustrated `DiePips` with the wall class, first extinguished after the
  flip, light on the result calculated by the server, the arrow of the metasta, dice Hunt without
  illustration; `BattleResolutionTests` unchanged (two results, modifier "+5 › -2", natural 1 does
  not hit).
- PlayMode: BattleResolutionNetwork (Keep roll/Reroll selected, private choice), Hunt (drawn
  meshes), BattleLayout with drop; `play1.xml` 22/23 with failed report.

<a id="ilustrowany-zestaw-strat--ui-118-2026-10-13"></a>
## Illustrated loss set — UI-118, 2026-10-13

- EditMode `BattleArtTests`: a chip with figure and a coat of arms, a keep cycle → downgrade →
  remove with overlays and badges, hit seals used 1/2 on downgrade/remove elite, budget line, legend
  icons.
- PlayMode: losses in battle and from events (Shadow04, CardPack04), server preview, siege;
  `play1.xml` 22/22.

<a id="decyzje-końca-bitwy-i-kronika--ui-119-2026-10-13"></a>
## End Decisions of battle and Chronicle — UI-119, 2026-10-13

- EditMode `BattleArtTests`: decision mapping for icons, Stop button with `battle-direct` class,
  icon and unchanged text; report with `battle-chronicle` class and round tab;
  `BattleConclusionTests` without change.
- PlayMode: BattleEnding (direct elections, elite cost, report and decision arrangement in four
  views), UI63Siege, LastBattleIconClick, WindowFamily, LiveActivity, ActionCardLayout; `play1.xml`
  18/19 with a failed report.

<a id="portrety-i-tła--ui-120-2026-10-13"></a>
## Portraits and backgrounds — UI-120, 2026-10-13

- EditMode `PortraitArtworkTests` : common portrait key for base game And Lords, atlas 1000×800 ed
  19 separate cells and import UI , medallion with portrait and hidden initial, Fallback of an
  unknown character on a figure, four scenes 336×88 with horizontal wrap.

<a id="odbiór-okna-bitwy--ui-121-2026-10-13"></a>
## acceptance window battle — UI-121, 2026-10-13

- PlayMode on `612656d`: pack1 13 battle/windows 29/30 classes (retained report failure), pack2 16
  cards/maps/Hunt 49/49 classes; EditMode 672/672.
- Player profile (Development, `board` suite, 40 workloads, 120/360 frames): min 102,23 FPS, carrier
  ≤ 7,37 ms, render-end ≤ 12,26 ms; `menu-battle` 105–110 FPS. `legacy` suite historic: stops on the
  Hunt control before UI-109, not applicable to battle.

<a id="obrys-fokusu-tylko-z-klawiatury--ui-122-2026-10-13"></a>
## Focus outline only from keyboard — UI-122, 2026-10-13

- EditMode whole client 672/672 (`.artifacts/ui122/editmode.xml`).
- PlayMode with a attached server on the port 5098 and base `wotr_test` ( `WOTR_TEST_DB=…55432…
  WOTR_CLIENT_TEST_PORT=5098 bash scripts/test-client.sh -testFilter …` ): 12 Focus classes,
  windows, cards , pressing and tooltips 44/44 ( `.artifacts/ui122/play.xml` New
  `FocusIndicatorTests.UI122ClickKeepsFocusWithoutOutlineUntilKeyboardInput` : Click without a
  contour, modifier without a contour, key shows the outline on the preserved seal, next click hides
  it, focus from the code after clicking without a contour, key without focus on the panel root.
  `HandCardLayoutTests` : outline of the line card ( UI-93 ) only after the key.

<a id="karty-wybranej-kości--ui-124-2026-10-13"></a>
## card selected dice — UI-124, 2026-10-13

The Unity user editor had an open client design, so the tests and build were run on a client copy cloned by APFS (`cp -c`) in the session working directory, with `Assets/` synchronization before each run.
- EditMode whole client 672/672 (`.artifacts/ui124/editmode.xml` copy).
- PlayMode `CardInteractionTests`, `HandCardLayoutTests`, `HandDrawerTests`,
  `ActionCardLayoutTests`, `CardGestureNetworkTests` with the attached server at the port 5098:
  11/11. New `CardInteractionTests.UI124ChosenDieLightsItsCardsAndDimsTheRest`: without dice no
  distinction and extinguishing; die Muster distinguishes card at the expense of the Muster and
  extinguishs card Army; change to Army turns the states without redevelopment of hand and shift
  cards; no command. `ui124-muster-cards.png`. ZXXZ.

<a id="grafika-armii-i-rezerw--ui-125-2026-10-13"></a>
## Army and Reserve Graphics — UI-125, 2026-10-13

- `python3 graphicalassets/ui/render.py --only armies --preview`: 47 PNG Category `armies` (four
  sources in the variants of the page and state, sign, ghost outline, reserve slot with two states,
  budget point), `.meta`, manifesto with preserved entries of other categories, preview
  `previews/armies.png` viewed against the background of map paper.
- EditMode the entire client 672/672 on copy APFS of which `UiArtKitTests` ed 47 new contract
  entries (sizes 2 ×, alpha, import UI ).

<a id="znaczniki-armii--ui-126-2026-10-13"></a>
## Army Tags — UI-126, 2026-10-13

Tests on the APFS copy of the client (user editor opened on the project), pinned server with lock on the 5098 port, `wotr_test` database.
- EditMode of the entire 674/674: client new `ArmyMarkerTests` (power per nation, General
  separately, tooltip) and overwritten `MapMarkerVisibilityTests` (levels 1,3×/1,2×, inspection and
  selection locally).
- PlayMode: prescribed `MapArmyArtworkTests` (both levels, both sides, garrison, coats, number,
  image base game for six states, clicks) and `MapVisibilityTests` (stocks when matching, figurines
  after clicking, selection and zoom, row next to settlement without blocking the symbol, four
  layouts); `ContextHoverTests` adapted for no hiding; `MapContourRenderingTests` omits the tag
  filling of all regions and black ink barriers in the measurement.
- Local package 13 35/35, `MapContourRenderingTests` 3/3, `BoardInteractionTests` with context group
  17/17 without failure UI-126, network package 9 with class 26/27; `MovementNetworkTests`
  separately 1/1 (in the package the batch after `FigureMotionNetworkTests`, the order of the tests
  depends).
- Considered failure:
  `ContextHoverTests.ClickedInspectionIsInteractiveStableAndFitsDenseRegionsAtBothScales` also falls
  on `c17fa15` before changes (figure in the panel moves about 0,6 px after selection).
- Builds `BuildLocalMac` (UI-124) and Development `BuildMac` with UI-126: Success.
  `scripts/profile-client.py --suite board` profile with views from `BoardProfileDriverTests`
  interrupted: Player got 1920×1027 instead of 1920×1080 on the external monitor 1080p; measurement
  moved to UI-132.

<a id="ruch-cała-armia-jednym-klikiem--ui-127-2026-10-13"></a>
## Movement: whole army in one click — UI-127, 2026-10-13

A copy of the APFS client, a server from a lock on the port of 5098, a database of `wotr_test`.
- EditMode of the entire 689/689 client (including the MENU-10 tests of the second agent); new
  `ArmyMovementTests`: one step back to gesture, ghost signatures only from server targets (move and
  attack with `BattleMode.Siege`), figure blocking the target in hints without the word "dragged".
- PlayMode: new
  `BoardInteractionTests.UI127OneClickSelectsTheArmyAGhostPreviewsTheTargetAndEscapeClearsIt` (one
  click selects the whole army and lights the targets, click figure indicates it, ghost with the
  signature "Move n figures" without hitbox, first Esc clears the selection with dice, second closes
  the panel, zero commands; `ui127-ghost.png` dump). `ContextHoverTests` and
  `ArmyContextNetworkTests` adapted to select the entire army with a click.
- Packages: `BoardInteractionTests` 8/8 ; traffic and network ( 9 classes) 32/33 →
  `ArmyContextNetworkTests` After adjustment 6/6 ; map and layouts 21/30 at the login limit, and
  `MapContourInteractionTests` 5/5 , `MapContourNavigationTests` 2/2 , `T22InformationTests` 3/3
  separately; log, effects, battle , recruitment, travel, victory 15/15 and 17/17.
- `ContextHoverTests.ClickedInspection…` ( ⁇ 0,5 px) fails. Note: Esc goes to focus element;
  focusless key goes to the panel root, except board (stated, test gives focus map).

<a id="jeden-przepływ-ruchu--ui-128-2026-10-13"></a>
## One movement flow — UI-128, 2026-10-13

- EditMode entire 688/688 client (removed test of the Army drag branch itself;
  `BoardDraftTests.AnArmyDropNeverChangesTheDraft` and converted
  `BattleDraftTests.AttackClickPreservesOriginRequiresAUnitForAnyRearFigureAndIgnoresAnArmyDrop`).
- PlayMode in six packages with a separate server: expansions and battle 33/33 , ShadowStrategy
  01–04 40/40 , Strategy 01–04 15/15 , CardPack and gestures cards 21/21 , KingsCards 01–04 24/25
  Fate, Lords and Muster 21/21.
- The `KingsCards02NetworkTests.HopeChoosesWhichInitialSovereignAwakensDuringBattle` failure (height
  `battle-card-attacker` 31,87 < 32) falls identically on `c17fa15` and on `c535ce5` before the
  session; it does not apply to the Army movement.

<a id="powody-rekrutacji-z-serwera--ui-129-2026-10-13"></a>
## Reasons for recruitment from the server — UI-129, 2026-10-13

- New `RecruitmentAvailabilityTests` (5): Rohan step away from war without targets with description
  "Rohan is 1 step away from war" and code `settlement:helms-deep:at-war`; Gondor with captured
  Pelargirem (`captured`), besieged Minas Tirith (`besieged`) and goals equal to `CanPlaceMuster`;
  in the ongoing sequence `used-this-action`, `elite-budget` and die sequence; Nazgûl outside
  Stronghold (`nazgul-stronghold`) with open Nurn for other figures; each page sees only its
  nations.
- `dotnet test --filter` for new tests, `MusterTests`, `RecruitSequenceTests` and projection: 65/65
  (2 omitted require a database).
- Full `scripts/test.sh` with `WOTR_TEST_DB`: 3840/3842; two tests with isolated PostgreSQL
  (`ActionLogRestartTests`, `HuntActivityPersistenceTests`) did not launch the station (postmaster
  became multi-threaded during startup), and with `LC_ALL=en_US.UTF-8` 2/2.
- `python3 scripts/package_deps.py --test-backend` ed `b48db9c` : Contracts 0.3.0 Content 1.0.2 with
  unchanged totals, `wotr-server-b48db9c0f5cc` and `wotr-seeder-b48db9c0f5cc` in the local channel.
  Client on a copy of the `WOTR_DEPS_SOURCE=file:…/deps-channel` and locally attached lock:
  `MusterNetworkTests` and `MultiUseDiceNetworkTests` 6/6. Publication and pinned in the client
  repository: UI-133 .

<a id="panel-posiłków--ui-130-2026-10-14"></a>
## Meal Panel — UI-130, 2026-10-14

- EditMode the entire client 693/693 of which `ReinforcementRowsTests`  (5): Nation lines with the
  reason for the server, sequence budget, ✓ only in its own recruitment sequence, selecting the
  reserve as one step of undoing.
- PlayMode on a client copy with a locally attached server `b48db9c0f5cc` (
  `WOTR_DEPS_SOURCE=file:…/deps-channel` ), 12 parcel: Muster, MultiUse , BoardInteraction and new
  `ReinforcementsLayoutTests` 16/16 ; T22 15/15 Movement 1/1 ; FigureMotion , ContextHover and
  CardInteraction 18/19 ; card and two-click 15/15 ; KingsCards 01–03 20/21 ; expansions 27/28 ;
  context and policy 6/6 Strategy and ShadowStrategy 32/32 Shadow04, Fate, Lords and siege 30/30
  systems 6/6 ; resources, assistance, Characters and frame 15/16.
- Fails were found:
  `ContextHoverTests.ClickedInspectionIsInteractiveStableAndFitsDenseRegionsAtBothScales` (0,92 px),
  `KingsCards02NetworkTests.HopeChoosesWhichInitialSovereignAwakensDuringBattle` and
  `ExpansionWindowNetworkTests.EvilKnowledgeRetainsTheEliteAndFullBattleResourcesFit` (height
  `battle-card-attacker` 31,87 < 32), the latter identical to `08eb956` before UI-130.
  `CharacterRosterActionNetworkTests.WhiteEntryRecoversTheLostReceiptWithoutPayingAgain` fell only
  in the package; separately class 7/7 (relationship to order).
- `BuildLocalMac` from copy: Success.

<a id="rekrutacja-z-kart-i-zdolności-w-panelu-posiłków--ui-131-2026-10-14"></a>
## Recruitment with cards and capabilities in the Meals Panel — UI-131, 2026-10-14

- EditMode of the entire 698/698 client, including `ReinforcementPlansTests` (5): limits and closure
  of card chips, × spirit as one step of undoing, "ReserveFigures" planned immediately with the
  conversion in a group of one figure, Balrog optional and ruler with slots, no plan at Muster; and
  `KingsCards02Tests.ReinforcementsGroupTenFiguresUnderFiveNationRows`.
- PlayMode on the client copy with the server `b48db9c0f5cc` from local channel: systems
  Strategy03/04,  CardPack04,  ShadowStrategy03/04 and network ShadowStrategy04   12/12  (✓ panel
  approves scenario plan `sh-str04-misty` on the server; ghosts with × in discharges
  `strategy04-ghosts-*`); Fate and  ShadowStrategy03/04   24/24;  Strategy01/03/04 and CardPack04
  16/16;  KingsCards01/02  and Lords  15/16; Expansion  Token/Ability  and Characters  26/26 (Balrog
  again 11/11 after the panel has been drawn to the top); Muster, MultiUse,  BoardInteraction,
  Reinforcements and  T22Board   21/21; Card,  CardPack02   7/7;  CardPack,  KingsCards03 and other
  packages 12/12;  Shadow01 – 03 and LordsShadow   21/21;  ShadowStrategy01   9/9,  ShadowStrategy02
  13/13,  SovereignPanel   5/5; Mordor  7/7 in three packages (limit of logins 20/min on IP
  interrupt the full class); HuntRecovery,  ResourcePanel,  ExpansionWindow and ExpansionCost
  14/15.
- Failures found unchanged:
  `KingsCards02NetworkTests.HopeChoosesWhichInitialSovereignAwakensDuringBattle` and
  `ExpansionWindowNetworkTests.EvilKnowledgeRetainsTheEliteAndFullBattleResourcesFit` (height
  `battle-card-attacker` 31,87 < 32).
- `BuildLocalMac` from copy: Success.

<a id="zwarty-panel-posiłków--ui-134-2026-10-14"></a>
## Compact Meal Panel — UI-134, 2026-10-14

- EditMode of the entire 698/698 client (`ReinforcementRowsTests` checks track status instead of the
  former Politics button).
- PlayMode on the client copy with the server `b48db9c0f5cc` from a local channel:
  `BoardInteractionTests` with new test UI-134 (track of active and passive nation, no step in war,
  no line of reason, `MapCanvas.Reveal` After clicking the token, the bar cards with choice as to
  hands, political step without Confirm) and `ReinforcementsLayoutTests` 11/11 ;
  `PoliticalGestureTests` , `PoliticalPanelTests` , `PoliticalRecoveryTests` ,
  `CardInteractionTests` 11/11 ; `MusterNetworkTests` , `MultiUseDiceNetworkTests` ,
  `T22BoardNetworkTests` 11/11 ; `ExpansionDecisionNetworkTests` , `ExpansionTokenNetworkTests`
  18/18 ; Strategy04 systems, ShadowStrategy04 , CardPack04 3/3.
- `BuildLocalMac` from copy: Success.

<a id="jedno-okno-rekrutacji--ui-135-2026-10-14"></a>
## One Recruitment Window — UI-135, 2026-10-14

- EditMode of the entire 700/700 client, including
  `ReinforcementPlansTests.ARulerOrAbilityMusterIsPlannedWithTwoPointsAndOneElite` and
  `ReinforcementRowsTests.ALeaderThatCouldStandAloneCarriesTheWarning`.
- PlayMode on a copy of the server `b48db9c0f5cc` : pallet tests carried to panel (
  `BoardInteractionTests` , `MusterNetworkTests` , `MultiUseDiceNetworkTests` ,
  `T22BoardNetworkTests` , `T22InformationTests` , `MovementNetworkTests` Results: BoardInteraction
  , Reinformements, ContextHover and EngineDoubleClick 25/26 (remaining ContextHover 0,49 px), as
  corrected by BoardInteraction and Reinformations 11/11 ; Muster and MultiUse 6/6 ; T22Board and
  T22Information 8/8 Movement 1/1 ; Expansion Token and Ability 19/19 ; CharacterRosterAction and
  ResourcePanel 12/12 ; RegionInspection and MapContourAcceptance 4/4.
- Catched and repaired: Click settlement to remove the open panel in front of the Politics drawer
  (`Reopen` instead of `Show`).
- `BuildLocalMac` from copy: Success.

<a id="zaznacz-i-odznacz-wszystkie--ui-140-2026-10-14"></a>
## Select and uncheck all — UI-140, 2026-10-14

- EditMode whole client 700/700.
- PlayMode on a copy of the server `b48db9c0f5cc` : `BoardInteractionTests` 11/11 with new switch
  test (hidden without dice Army, click selects all, second selects, without command );
  `ArmyContextNetworkTests` and `MapClickMoveNetworkTests` 8/8 ; `CompactBoardLayoutTests` ,
  `CharacterLayoutTests` , `FellowshipLayoutTests` 7/7 ; `ContextHoverTests` and
  `RegionInspectionTests` with a difference 0,52 px in ContextHover .
  `ExtremeMixedArmyAndReservesFitAtBothResolutionsAndLargerScale` selects the first button now
  figure , not the first panel button.
- `BuildLocalMac` from copy: Success.

<a id="poświata-opcji-wybranej-kości--ui-136-2026-10-14"></a>
## Highlight of the selected option dice — UI-136, 2026-10-14

- EditMode entire 703/703: `DieOptionsTests` client (Muster without Fellowship, Will of the West
  with Fellowship move and character input, no glow without dice and beyond the player's decision),
  `UiArtKitTests` with `frames/glow`.
- PlayMode on a copy of the server `b48db9c0f5cc` : `BoardInteractionTests` ed
  `UI136ChosenDieLightsTheControlsItCanUse` , `DicePanelLayoutTests` , `PoliticalPanelTests` ,
  `CardInteractionTests` 21/21 ; `MultiUseDiceNetworkTests` , `FellowshipPanelNetworkTests` 4/4 ;
  `CharacterRosterActionNetworkTests` , `CharacterRosterLayoutTests` 8/8 ;
  `BattleWindowNetworkTests` , `BattleLayoutTests` 5/6.
- Considered defeat:
  `BattleWindowNetworkTests.ReliefForcesAndLegalCombatChoicesFitFourLayoutsWithRealHitTargets`
  (Lossarnach tag obscures Minas Tirith garrison), identical to `81c8614` client before UI-136;
  transferred to UI-132.
- `BuildLocalMac` from copy: Success.

<a id="podgląd-kart-walki-i-równa-ramka--ui-137-2026-10-14"></a>
## Preview cards battle and equal frame — UI-137, 2026-10-14

- EditMode whole client 703/703.
- PlayMode on a copy of the server `b48db9c0f5cc` : `BattleLayoutTests` with new preview check card
  fights in four systems (full combat text, on screen, disappears after departure),
  `BattleWindowNetworkTests` , `ReinforcementsLayoutTests` 6/7 ; `BattleResolutionNetworkTests` ,
  `BattleEndingNetworkTests` 6/6 ; `CardInteractionTests` , `CardNetworkTests` ,
  `KingsCards02NetworkTests` 12/13.
- Failures found: the application of the Lossarnach/Minas Tirith tags in `BattleWindowNetworkTests`
  (UI-132) and the height of `battle-card-attacker` 31,87 < 32 in `KingsCards02NetworkTests`.
- `BuildLocalMac` from copy: Success.

<a id="jednolite-przyciski--ui-138-2026-10-14"></a>
## Single buttons — UI-138, 2026-10-14

- EditMode omnicle 704/704 (`UiArtKitTests` ori dic 18 omnicle action,
  `ComponentActionTests.PassAndDiscardWithoutAnOwnerShowAnActionIcon`).
- `python3 scripts/button_inventory.py --check` (client): icon 67, text 105, art 49, glyph 0;
  self-test on `efae28c` detects four omitted places (conditional label, initiator, assignment
  `text`).
- PlayMode on a copy of the server `b48db9c0f5cc` : 11 packages of systems ( 56 classes
  `*LayoutTests` and `ReturnListTests` , `LordsShadowNetworkTests` , `MainMenuNetworkTests` ,
  `BoardInteractionTests` ), including a new test
  `BoardChromeLayoutTests.EveryButtonWithoutWordsExplainsItselfAndNoGlyphButtonRemains` . After the
  icon fixes: `BoardChromeLayoutTests` , `CompanionSetupPanelNetworkTests` , `BattleLayoutTests` ,
  `Shadow02LayoutTests` , `Strategy04LayoutTests` , `HuntLayoutTests` , `CouncilHomelandLayoutTests`
  , `TurnLayoutTests` , `T22TurnNetworkTests` , `ReturnDraftTests` , `HuntNetworkTests` ,
  `TestScenarioNetworkTests` , `HandDiscardDrawerTests` , `CardInteractionTests` ,
  `CardNetworkTests` , `ActionCardLayoutTests` , `ClickInspectionTests` ,
  `ExpansionDecisionNetworkTests` , `CompanionSetupRecoveryTests` ,
  `BoardCompletionContextNetworkTests` , `InGameContextNetworkTests` No failures.
- Failure has been found: `ShadowStrategy01LayoutTests` `battle-no-card` 31,87 < 32, identical to
  `5779645` (same rounding as `battle-card-attacker`).
- `BuildLocalMac` from copy: Success.

<a id="karta-postaci-w-symbolach--ui-139-2026-10-14"></a>
## card characters in symbols — UI-139, 2026-10-14

- EditMode of the entire 707/707 client, including
  `CharacterRosterPanelTests.CardStatisticsAreSymbolsThatExplainThemselves`,
  `InspectorShowsStatusStatisticsAndAbilityChipsWithoutUnrolledText`,
  `RosterRowCarriesTheLevelAndAStatusSymbol` and `UiArtKitTests`.
- PlayMode on a copy of the server `b48db9c0f5cc` : `CharacterRosterLayoutTests` ,
  `CharacterLayoutTests` , `CharacterRosterPanelNetworkTests` (the chip click develops and wraps the
  ability text), `CharacterRosterActionNetworkTests` 7/7 , `BoardCompletionLayoutTests` 2/2 ;
  regression `BoardChromeLayoutTests` , `LeftRailLayoutTests` , `EdgeBoardLayoutTests` ,
  `SovereignSectionLayoutTests` , `ContextHelpLayoutTests` , `ClickInspectionTests` ,
  `CompanionSetupPanelNetworkTests` , `InGameContextNetworkTests` , `Strategy01LayoutTests` ,
  `Shadow01LayoutTests` , `CompactBoardLayoutTests` , `DesktopLayoutStabilityTests` No failures.
- `BuildLocalMac` from copy: Success.

<a id="pula-polowania-i-grafika-kafla--ui-141-2026-10-14"></a>
## Pool Hunt and Tile Graphics — UI-141, 2026-10-14

- Server: `dotnet test --filter HuntPoolProjectionTests` 6/6; full `scripts/test.sh` with
  `WOTR_TEST_DB` and `LC_ALL=en_US.UTF-8` 3848/3848 (without `LC_ALL` two tests with PostgreSQL
  isolated do not start instance as in UI-129). Privacy tests `ExpansionInteractionTests` and
  `KingsSequentialTests` compare worlds with the same open pool.
- `python3 scripts/package_deps.py --test-backend` with `bab4f2d`: Contracts 0.3.1, content 1.0.3,
  `wotr-server-bab4f2d5e2da`, `wotr-seeder-bab4f2d5e2da` in local channel; the client attaches them
  `bootstrap_deps.py pin` with `WOTR_DEPS_SOURCE=file:…/deps-channel` (publication: UI-133).
- Client EditMode 710/710 (`HuntPoolListTests`, `UiArtKitTests`).
- PlayMode on a copy of the server `bab4f2d5e2da` : `HuntLayoutTests` with an open list of the pool
  in all decisions and four arrangements, `HuntNetworkTests` 7/7 , `HuntResponseNetworkTests` 3/3 ,
  `MordorNetworkTests` 7/7 (in pairs with break; together, they exceed the limit 20 logins per
  minute), `MordorLayoutTests` , `RightColumnLayoutTests` , `FellowshipLayoutTests` ,
  `BoardChromeLayoutTests` , `EdgeBoardLayoutTests` , `LordsShadowNetworkTests` ,
  `T22BoardNetworkTests` ; I will find `battle-card-attacker` 31,87 < 32 In
  `KingsCards02NetworkTests` and `ExpansionWindowNetworkTests` .
- `BuildLocalMac` from copy: Success.

<a id="bez-dwukliku--ui-142-2026-10-14"></a>
## No double click — UI-142, 2026-10-14

- EditMode whole client 710/710.
- PlayMode on a copy of the server `bab4f2d5e2da` in packs of 1–6 classes (limit 20 log-ins per
  minute; `MordorNetworkTests` in pairs: gesture tests `EngineClickTests` (changed from
  `EngineDoubleClickTests` ), `DraftInputTests` ,
  `ContextHoverTests.AFastSecondClickCannotCrossDecisionBoundary` , discard with one click (
  `HandDiscardNetworkTests` , `EngineHandDiscardNetworkTests` in four systems,
  `CardServerCompatibilityTests` ), extension siege ( `StrongholdNetworkTests` ), answers Hunt (
  `HuntResponseNetworkTests` click and drop, `HuntNetworkTests` , `HuntAftermathNetworkTests` ,
  `HuntRecoveryNetworkTests` , `HuntActivityNetworkTests` , `VictoryPanelNetworkTests` ,
  `MordorNetworkTests` ), entry from the row of Characters, armies, battle , card and full game
  `T23FullGameNetworkTests` — without failing outside: `ContextHoverTests` 0,49 px and
  `battle-card-attacker` 31,87 < 32 In `ExpansionWindowNetworkTests` . `BattleWindowNetworkTests`
  3/3 after addition `Settle` After clicking the army (panel of the region arranges figure in the
  next frame).
- `BuildLocalMac` from copy: Success.

<a id="pasy-nacji--ui-143-2026-10-14"></a>
## Nation belts — UI-143, 2026-10-14

- Server: `export_map_border_bands.py --check` play file (Shapely) before changing 2.1.2  /  GEOS
  3.13.1 in venv from `graphicalassets/region-map/tools/requirements.txt`); after change
  `nationWidth` exports, `--check`,  `export_map_geometry.py --check --borders`,
  `test_map_boundaries.py`   7/7 and `dotnet test --filter Map|Content|Board`   58/58.
  `map_render_asset_tests.py` does not start: no module `export_map_terrain` (client) — I will.
- `package_deps.py --test-backend` with `dc3e6d6`: content 1.0.5, `wotr-server-dc3e6d61abc0`,
  `wotr-seeder-dc3e6d61abc0` (Contracts 0.3.1 unchanged); the client attaches them from the local
  channel.
- Client EditMode 711/711; PlayMode `MapContourAcceptanceTests`, `MapContourRenderingTests`,
  `MapVisibilityTests`, `MapCanvasLayoutTests`, `ActionMapLayoutTests`,
  `MapContourInteractionTests`, `MapContourNavigationTests`, `BoardChromeLayoutTests`,
  `EdgeBoardLayoutTests`, `CompactBoardLayoutTests` without failure; `ui126-map-fit-*` and
  `ui126-map-zoom-*` discharges viewed (distinguished nations at full view).
- `BuildLocalMac` from copy: Success.
- Not performed: map profile17 Development Player (1920×1080 window on monitor 1080p, as in UI-126);
  UI-143 locked for measurement.


<a id="tor-polityki-bez-zaznaczania--ui-144-2026-10-14"></a>
## Policy track without selection — UI-144, 2026-10-14

- EditMode of the entire 711/711 client (UI-144 alone) and 712/712 (UI-145).
- PlayMode on a copy of the server `dc3e6d61abc0` : `PoliticalPanelTests` (new
  `EveryLegalStepAndActivationShowsWithoutSelectingANation` : Arrows of all legitimate player
  nations and Actiate with Narya without clicking, drop `ui144-politics-actions` ),
  `PoliticalGestureTests` , `PoliticalRecoveryTests` , `RuntimeTooltipTests` ,
  `BoardInteractionTests` , `LeftRailLayoutTests` , `FloatingWindowTests` , `BoardMenuLayoutTests`
  39/39 ; `MusterNetworkTests` and `T22TurnNetworkTests` 8/8 ; `MultiUseDiceNetworkTests` 2/2 (the
  token click moves the nation); `ExpansionDecisionNetworkTests` 7/7 (Narya without selection);
  `ExpansionTokenNetworkTests` 11/11 (Council); `CardNetworkTests` and `T22InformationTests` 4/4.
- The first launch revealed the Meals Panel (open dice Muster) over the open Politics; since UI-144
  the Politics opening is the panel top. The “no bottom policy choice” rating omits the Reopening
  Meals button (UI-130) which is not the choice of nation.

<a id="źródła-kości-character--ui-145-2026-10-14"></a>
## Sources dice Character — UI-145, 2026-10-14

- EditMode of the entire 712/712 client, with UI-143 (barriers) 713/713; new
  `ArmyContextTests.CharacterDieSourcesOnlyForcesWithACommander` (arms in Dale without commander is
  not source at Character, `RequiredCharacterId` narrows the source, die Army shines every army,
  tray tip).
- PlayMode on a copy of the server `dc3e6d61abc0` : `ArmyContextNetworkTests` 6/6 ,
  `MapClickMoveNetworkTests` 2/2 , `T22BoardNetworkTests` 5/5 , `UI63SiegeNetworkTests` and
  `MovementNetworkTests` 3/3 , `ExpansionAbilityNetworkTests` and `ExpansionCostNetworkTests` 12/12
  , `LordsShadowNetworkTests` and `ResourcePanelNetworkTests` 9/9 , systems (
  `ReinforcementsLayoutTests` , `CardPack03LayoutTests` , `FellowshipLayoutTests` ,
  `TurnLayoutTests` , `EdgePlaqueTests` , `CharacterLayoutTests` , `MapArmyArtworkTests` ) 14/14 ,
  `TravelGestureNetworkTests` 3/3 and `FigureMotionNetworkTests` 3/3 in separate packages.
- Zazaze: `FigureMotionNetworkTests` and `TravelGestureNetworkTests` share the rooms of the script
  `travel-drag` and `separate-drag` (seeder), so in one package the second replaces the moved
  Companion (two failure "not null" also unchanged UI-145 in order with FigureMotion).
  `CharacterNetworkTests. VersionsAndMinionEntryThenFlightReachBothClients`: the Open Meal Panel
  dice Will covers the poem Characters — the same failure on `b2aca35` (before UI-144).
- `BuildLocalMac` from copy (tree with UI-144 and UI-145): Success.

<a id="bariery-gór--ui-143-2026-10-14"></a>
## Mountain Barriers — UI-143, 2026-10-14

- EditMode of the entire 713/713 client, including
  `MapContourBorderTests.BarriersArePenLinesNotMarkerStrokes`.
- PlayMode on copy: `MapContourAcceptanceTests` , `MapContourRenderingTests` , `MapVisibilityTests`
  , `MapCanvasLayoutTests` 8/8 , `MapContourInteractionTests` 5/5 , `MapContourNavigationTests` 2/2
  (in separate packages; together they exceed the login limit and get `TooManyRequests` Droppings
  `ui126-map-fit-*` and `ui126-map-zoom-*` viewed: barriers as thin ink lines along the ridges,
  still distinguishable from the passages.
- `BuildLocalMac` from copy: Success.
- Not performed: map profile17 Development Player (no changes).

<a id="zaznacz-wszystkie-przy-wejściu-po-bitwie--ui-146-2026-10-14"></a>
## Select all at the input after battle — UI-146, 2026-10-14

- EditMode whole client 713/713.
- PlayMode on a copy of the server `dc3e6d61abc0` : `BattleLayoutTests` 3/3 , `BattleNetworkTests`
  3/3 , `BattleEndingNetworkTests` 4/4 , `StrongholdNetworkTests` 2/2 , `Strategy01NetworkTests` and
  `LiveActivityNetworkTests` 6/6 , `BattleWindowNetworkTests` 3/3. Helper
  `MatchTestRoom.SelectAllAdvancing` (six input tests) uses the switch: select, select and re-check,
  then click the target.
- Found by occasion: `BattleEndingNetworkTests.SiegeEliteMissing…` waited for cost preview before
  approving the extension siege, which since UI-142 has not been found (one click approves); test
  modified with a separate commit of the `d691c1c` client.
- `BuildLocalMac` from copy (`a085823`): Success.

<a id="karta-walki-jednym-kliknięciem--ui-147-2026-10-14"></a>
## card One-Click Fight — UI-147 , 2026-10-14

- EditMode whole client 713/713 (`BattleCombatHandTests.
  OneClickPlaysTheTileFaceDownThroughTheSharedToken`).
- PlayMode on a copy of the server `dc3e6d61abc0` : `BattleWindowNetworkTests` 3/3 (playing with one
  click, zero window open card ), `CardGestureNetworkTests` and `LiveActivityNetworkTests` 2/2 ,
  `Shadow03NetworkTests` 8/8 ( card Balroga of the table), `BattleLayoutTests` ,
  `CardInteractionTests` , `HandDrawerTests` , `CardViewportLayoutTests` , `ClickInspectionTests` ,
  `ReturnDraftTests` 14/14 , `CardPackNetworkTests` 3/3 , `T22InformationTests` and
  `UI10NetworkTests` 6/6 , `LordsFinalCardsNetworkTests` and `EffectsNetworkTests` 9/9 ,
  `VictoryPanelNetworkTests` and `CardServerCompatibilityTests` 7/7 , `CardNetworkTests` 1/1 (after
  changing the Assertion: played card events do not pin the window), `MultiUseDiceNetworkTests` 2/2
  (in the first run the reserve position difference below the pixel, replay completed).
- `BuildLocalMac` from copy (`4def2e5`): Success.

<a id="politics-dla-karty-z-wyborem-nacji--ui-148-2026-10-14"></a>
## Politics for card with choice of nation — UI-148, 2026-10-14

- State `edb0bf4` : EditMode 713/713 ; `MultiUseDiceNetworkTests` (first start 2/2 ),
  `CardNetworkTests` 1/1 , `PoliticalPanelTests` , `PoliticalGestureTests` ,
  `PoliticalRecoveryTests` , `CardInteractionTests` 12/12 , `CardPackNetworkTests` 3/3 ; discharges
  `ui148-politics-card-*` : Open panel with highlighted nations. Arrows were also visible on the
  flip dice Muster when choosing a nation for card ; from UI-148 are hidden in this mode.
- The subsequent `MultiUseDiceNetworkTests` launches interrupted the difference of 0,01 px reserve
  position in the Meals panel (recruit option, before part with card). After removing the cards
  (UI-150) bar in the total course of UI-148–UI-151 test passes 2/2, including the new hidden arrow
  placement.
- `BuildLocalMac`: Success (`edb0bf4` from hiding arrows and the tree UI-148–UI-151).

<a id="okno-bitwy-posiłki-i-no-card--ui-149-ui-150-ui-151-2026-10-14"></a>
## battle window, Meals and “No card” — UI-149, UI-150, UI-151, 2026-10-14 window

- Total run on a copy from UI-148–UI-151 (`dc3e6d61abc0` server), then replays of the improved
  classes at the final state of `986ffbf`:
  - EditMode whole client 713/713 (`BattleWindowTests.
    ClosingAndRepeatedProjectionKeepTheWindowClosedAndNewBattleOpensIt`).
  - `BoardInteractionTests` (with `UI151NoLegalCombatCardSendsNoCardOnceAndKeepsTheButton`: one
    automatic `CommitCombatCard` without card, grey "No card" pending; `UI134…`: panel without
    cards, card distinguished in hand), `BattleLayoutTests`, `ReinforcementsLayoutTests`,
    `FloatingWindowTests`, `HandCardLayoutTests` — without defeats after amendments.
  - `MultiUseDiceNetworkTests` 2/2 , `BattleWindowNetworkTests` 3/3 , `DecisionNoticeNetworkTests`
    and `ActionNoticeNetworkTests` 4/4 , `BattleNetworkTests` 3/3 , `BattleEndingNetworkTests` 4/4 ,
    `MusterNetworkTests` 4/4 , `CardGestureNetworkTests` 1/1 , `LiveActivityNetworkTests` 1/1 ,
    `StrongholdNetworkTests` and `Strategy01NetworkTests` 7/7 , `Shadow03NetworkTests` 8/8 ,
    `ShadowStrategy01NetworkTests` and `Shadow04NetworkTests` 17/17 , `MapContourNavigationTests`
    2/2 , `T22BoundaryNetworkTests` 5/5 , `CardNetworkTests` and `PoliticalPanelTests` 5/5 ,
    `T22TurnNetworkTests` and `ExpansionDecisionNetworkTests` 11/11.
- Changed Test Establishments: The battle window after the attack is open (UI-149); the page without
  legal card fight corresponds to “No card” alone, so `MatchTestRoom.NoCard` only clicks on the
  legal card, and the security tests (`BattleNetworkTests`, `Shadow03NetworkTests`) read the hidden
  commitment from the first view after it.
- By the way, `HandCardLayoutTests` was waiting for the card to be armed for recoil, and the hand
  was hinting "Click, then click again to discard" — both remains of UI-142 with the enhanced
  `e0b39ef` client commit.

<a id="grafika-i-geometria-toru-mordoru--ui-153-2026-10-14"></a>
## Mordor track graphics and geometry — UI-153, 2026-10-14

- `python3 graphicalassets/ui/render.py --only mordor`: 7 PNG (normal plate, pass and next, rift
  with next variant, gate with next variant); preview in zoomed-in view.
- EditMode the entire client 719/719: `UiArtKitTests` with new textures, `MordorTrackGeometryTests`
  (six steps from both gates to the crater of Orodruina, gates at settlement , steps east of the
  gates, map boundaries; five cases of rejection of incorrect data. 4,24 units from step 0 and
  half-written map data after exception — improved data and order of writing in `BoardPresentation`
  .
- Control overlay `ui153-track-overlay` (data on `terrain.jpg`, 2 px per unit of map): path on the
  plain and slope, step 5 on the crater, gate next to Morannon anchors and Minas Morgul.
- `BuildLocalMac` from copy: Success.

<a id="tor-mordoru-na-mapie--ui-154-2026-10-14"></a>
## Mordor track on the map — UI-154, 2026-10-14

- EditMode ed whole client 722/722: `MordorTrackStateTests` (states of the plates off track and
  on step 3, tooltips, interpolation of the `PointAt` path).
- PlayMode on copy (server) `dc3e6d61abc0` ): `MordorLayoutTests` (new
  `TrackOnTheMapShowsTheFellowshipOnItsStepInFourLayouts` — a figure on the plate of step, figures
  approximately and their absence on the whole map, a badge ‘ 7/12 “ hourglass, tile, gates,
  extinguishing without Fellowship , no layer over-scraping at the panorama;
  `TheFellowshipWalksThePathToItsNextStep` ), `MapContourRenderingTests` , `MapVisibilityTests` ,
  `MapCanvasLayoutTests` 10/10 ; `MordorNetworkTests` in pairs 7/7 (assertion of the hidden figurine
  after entering replaced by a step-by-step figure 0 ); `FellowshipMotionNetworkTests` ,
  `FellowshipNetworkTests` 2/2 , `FellowshipPanelNetworkTests` 2/2 , `HuntNetworkTests` 7/7 ,
  `MapContourAcceptanceTests` , `FellowshipLayoutTests` and `HuntLayoutTests` .
- `ui154-mordor-zoom-*` and `ui154-mordor-fit-*` drops viewed; the first version put the badge on
  the 4 disc and the tile on the 2 disc — row moved to the figurine plate, tile only approximately.
  The Fellowship tag had the default button margin (4 px shift) — zero.
- By the way: `HuntLayoutTests.AllocationRerollsTileAndGuide…` also failed without UI-154 (from
  UI-142 click answers apply it); test improved with the commit of the client `afc80e2`, 4/4.
- `BuildLocalMac` from copy: Success.

<a id="działanie-toru-mordoru-z-mapy--ui-155-2026-10-14"></a>
## Operation of Mordor track from map — UI-155, 2026-10-14

- EditMode whole client 722/722.
- PlayMode per copy (server) `dc3e6d61abc0`):  `MordorNetworkTests` in pairs 7/7 — entrance by
  clicking the gate (plate) 0 and the gate shine), track in view Free Peoples On the entrance and
  camera Shadow without change, button Fellowship includes the entire track, trying to move by
  clicking on the next disc (tooltip with kaflu), hiding by clicking the figurine;
  `MordorLayoutTests`   4/4,  `FellowshipNetworkTests`   2/2,  `FellowshipPanelNetworkTests`   2/2,
  `FellowshipMotionNetworkTests`,  `HuntNetworkTests`   7/7, map and layout tests Fellowship / Hunt
  11/11.
- First run: the camera moved at each appearance of Fellowship in Mordor (also when switching the
  view in the test) — a decision moved to `FellowshipContextInteraction` (only after its own choice
  of input; otherwise pulse). Accurate track centering at the eastern edge limits the camera, so
  tests check the entire track in the view. Three tests with the former "camera is standing at the
  entrance" rule updated.
- `BuildLocalMac` from copy: Success.

<a id="odbiór-toru-mordoru-na-mapie--ui-156-2026-10-14"></a>
## acceptance Mordor track on the map — UI-156, 2026-10-14

- EditMode whole client 722/722.
- PlayMode on copy (server) `dc3e6d61abc0` State `8cb1e10` : `MordorLayoutTests` new
  `AcceptanceScreensForBothSidesAndSteps` (both sides, steps 0/3/5 , Hidden/Revealed , four
  systems), map tests ( `MapContourRenderingTests` , `MapVisibilityTests` , `MapCanvasLayoutTests` ,
  `MapContourAcceptanceTests` ) 13/13 ; `MordorNetworkTests` in pairs 7/7 ; `FellowshipNetworkTests`
  2/2 , `FellowshipPanelNetworkTests` and `FellowshipMotionNetworkTests` 3/3 ,
  `FellowshipGuideNetworkTests` 5/5 , `FellowshipRoutesNetworkTests` 1/1 , `HuntNetworkTests` 7/7 ,
  `VictoryPanelNetworkTests` and `VictoryPanelLayoutTests` 8/8 , `FellowshipLayoutTests` and
  `HuntLayoutTests` 9/9 , `T23FullGameNetworkTests` 1/1.
- `ui156-mordor-*` drops viewed; on Ventilation the badge covered the signature and the 4 disc — moved
  to the left of the crater.
- `BuildLocalMac` from copy: Success.
- Not performed: Map Profile in Development Player (`scripts/review-map-client.py`, window
  1920×1080) — same missing as in UI-143.

<a id="publikacja-paczek-klienta--ui-133-2026-10-14"></a>
## Client packages publication — UI-133, 2026-10-14

- The consent of the 2026-10-14 user (“agreement” to publish the packages that the push requires of
  the client). `git push origin main` server (`75d51b2..c49a28c`, fast scroll; before push the
  review of the diff under secrets and artifacts).
- Four releases of GitHub `jpolaczek/wotr-server` in the format of the previous (archive and
  manifesto, target = commit construction): `com.wotr.contracts-0.3.1` (`bab4f2d`),
  `com.wotr.public-content-1.0.5`, `wotr-server-dc3e6d61abc0`, `wotr-seeder-dc3e6d61abc0`
  (`dc3e6d6`).
- `gh release download` four archives: SHA-256 equal to the client manifest and lock (`c530ac04…`,
  `2a7b21bb…`, `f79c741f…`, `7c3556ec…`).
- In a client copy with `.deps` removed cache: `bootstrap_deps.py fetch --test` without
  `WOTR_DEPS_SOURCE` downloaded and verified all four packages from GitHub.
- PlayMode on the backend of the releases: `MusterNetworkTests` 4/4 (the Meals Panel with Server
  Reasons), `HuntNetworkTests` 7/7 (the pool composition and the lico tile).
- Client Lock (`wotr-deps.lock.json`) has already indicated these versions; unchanged.

<a id="wyraźniejszy-tor-mordoru--ui-157-2026-10-14"></a>
## Clearer Mordor track — UI-157, 2026-10-14

- EditMode whole client 722/722 (`MordorTrackStateTests`: explanation of input in the board and
  gate entrances outside the track, missing it on the track).
- PlayMode `MordorLayoutTests`, `MapContourRenderingTests`, `MapVisibilityTests` 10/10 (no signature
  of the rift); map dumps and approximations viewed: red path on a bright foundation, larger plates,
  readable track without Fellowship.
- The first run interrupted the full system disk (free 273 MB 460 GB); the reproducible test
  artifacts in the scratchpad (drops, logs, copies), freed approx. 5 GB.
- `BuildLocalMac` from copy: Success.

<a id="odbiór-armii-na-mapie-ruchu-i-posiłków--ui-132-2026-10-14"></a>
## acceptance Army on map, movement and meals — UI-132, 2026-10-14

Copy of APFS client (`cbdc3de` and `118f3bd`), test server with lock (`dc3e6d61abc0`) port on 5098 port, base `wotr_test`; Development Player on M1 Pro, external monitor 1080p 75 Hz as window screen.
- EditMode of the entire 722/722 client before and after the amendments.
- PlayMode : series 18 parcel ( 65 classes of map, movement, recruitment, cards , battle ,
  Fellowship , edge board ) 199/211 , the replays separately: `MapContourInteraction` 5/5 and
  `MapContourNavigation` 2/2 (first pass: login limit `TooManyRequests` ), `MovementNetworkTests`
  1/1 (relationship of order to `FigureMotionNetworkTests` ), `MapViewportTests` 8/8 after the edge
  test correction (course at the height of the marker, not the anchor — the government stands above
  settlement ed UI-126 ), `ShadowStrategy04LayoutTests` and `BattleLayoutTests` 4/4 After the
  targets 33 px in window battle , `KingsCards02` and `UI63Siege` 9/9 ,
  `ExpansionWindowNetworkTests` 5/5 (two defeats found 31,87 px removed), window, run and end battle
  10/10 , `BoardInteraction` and `BattlePolicy` 15/15. I'll be as normal as you are:
  `ContextHoverTests.ClickedInspection…` ( 0,65 px) and
  `BattleReportNetworkTests.ClosedReportRejectsLateHttp…` ( UI-115 ).
- I will find the risk with UI-136 (`BattleWindowNetworkTests.ReliefForces…`, the Lossarnach tag
  over the Minas Tirith garrison) to go to `69e752f` and in replays.
- Profile `map17` ( `scripts/review-map-client.py` ): PASS , 36 workloads, 42 discharges, minimum
  168,6 FPS , feedback ≤ 13,8 ms; first time since UI-126 thanks to full screen window for 1920×1080
  on a 1080p monitor ( `ProfileResolution` , mode in report) and the load stage corrections (Helm's
  Deep outside the attached window card , cursor after the region panel is located. 2026-10-08 :
  Apes + 13% , idle frame 0,5–0,7 → 1,1–1,75 ms, belt redevelopment 3,8 → 5,7 ms.
- `board` profile (`scripts/profile-client.py --suite board`): PASS, 40 workloads, 71–73 FPS (VSync
  75 Hz), gesture support ≤ 12,9 ms, until the end of rendering ≤ 21,4 ms; control card reads UI-139
  labels.
- `BuildLocalMac`, `BuildLocalWindows`, Development `BuildMac`: Success.
- `scripts/test.sh` Server (with `LC_ALL=en_US.UTF-8`): 3848/3848. Server
- `python3 -m unittest scripts/test_profile_client.py scripts/test_map_profile.py`
  (client): 25/25; `check_docs.py` (client): 43 files.
- Not performed: `scripts/smoke-macos.py` (carrys real application data and user preferences),
  Windows and Intel Mac profiles, handheld batch.
- Reports: `wotr-client:docs/ui/profiles/ui132-*.json`.

<a id="profil-map17-przed-i-po--ui-143-ui-156-2026-10-14"></a>
## Map Profile17 before and after — UI-143, UI-156, 2026-10-14

Development Players from client copy on commits `e71a226` (before) UI-143 Content 1.0.3 ), `b2aca35` (belts), `dec3387` ( UI-145 ), `7c1c5c4` (barriers), `2bb4442` (before the Mordor track) and after UI-157 each with tools placed on it `cbdc3de` ; `scripts/review-map-client.py` with the same `map17-profile.json` (seeder `dc3e6d61abc0` ), alternately, M1 Pro, monitor 1080p 75 Hz ( 1920×1080 in full screen window). all 10 run: PASS ( 36 workloads, 42 Drops). Summary: `wotr-client:docs/ui/profiles/map17-ab-ui143-ui156.json` .
- acceptance Metrics unchanged: minimum 168–173 FPS, feedback ≤ 19,3 ms (single maximum; p95 8,2–8,4
  ms), static belts do not rebuild; projection and reconstruction of Fate/White Hand 0,99–1,01.
- UI-143 : Apes 915 thousands. → 999 thousands (belts) → 1052 thousands (barriers); idle frame and
  at cursor + 5–13% (approx. 0,1 ms); p99 cursor in 1366×768 of ~ 1,4–1,8 to ~ 4,9–6,5 ms
  (bisection: belts at 125% , barriers to 100% ; UI-144/UI-145 no effect).
- UI-156: tops +3 thousand; idle, cursor, heavy loads ±1%; panorama with zoom at Morannon +0,08–0,2
  ms per frame.
- The noise between runs of the same version: median 1,00, range 0,91–1,12 per workload.
- User decision 2026-10-14: cost accepted (R23 met; strict ‘not worse’ not met for light loads).

<a id="katalog-scenariuszy-tylko-dla-podstawki--test-05-2026-10-14"></a>
## Directory of scenarios only for base game — TEST-05, 2026-10-14

- The `expansion-test-scenarios` branch (with `d331754`) contains full TEST-04; `git diff --stat
  3b8a5ef expansion-test-scenarios -- src tests scripts` branch is empty, so its code passed the
  full 3848/3848 set with UI-132 (with `ExpansionDiceScenarioTests`).
- `main` without expansions scripts: `dotnet test --filter
  BaseDiceScenarioTests|TestScenarioSessionTests` 46/46; full `scripts/test.sh` (with
  `LC_ALL=en_US.UTF-8`) 3826/3826 — 3848 minus 22 script tests expansions scenarios.

<a id="scenariusze-toru-mordoru--test-06-2026-10-14"></a>
## Mordor track scripts — TEST-06, 2026-10-14

- `dotnet test --filter BaseDiceScenarioTests|TestScenarioSessionTests|MordorTrackScenarioTests`
  53/53: each scenario builds the same state and starts the page Free Peoples , runs through HTTP ed
  PostgreSQL ( `EveryScenarioStartsOverHttpWithItsSideToAct` ); `fp-mordor-enter` offers
  `EnterMordor` , a command I'm buying Fellowship step by step 0 ; `fp-mordor-track` (step 0 ,
  Corruption 3 ) and `fp-mordor-crack` (step 4 , Corruption 6 ) offer movement Fellowship dice
  Character, example command Passes through the engine; event `MordorEntered` In history.
- Full `scripts/test.sh` 3832/3833. The only failure
  `MenuLifecycleTests.PublicHostCanShareACodeWithoutExposingItInThePublicList` is unstable and
  unrelated: the list of public rooms has 20 the position sorted by random id, and the shared base
  `wotr_test` has 1194 the rooms with earlier runs; separately PASS, once FAIL. The isolation of the
  test database belongs to the range of QUAL.

<a id="grafika-konsoli-tury-polowania-i-pierścieni--ui-159-2026-10-14"></a>
## turn, Hunt and Rings graphics — UI-159, 2026-10-14

- `python3 graphicalassets/ui/render.py --only console`,  `--only hunt`,  `--only icons`:  15
  console texture (frame, badge) `free` / `shadow`, pole  Hunt with `eligible`, Ring's nest in 8
  variants, balloon and tail), 8 window texture Hunt  (4 Phase icons, 2 scenes, 2 injury choices),
  `icons/ring-captured`; existing PNG unchanged. Preview of the sheet viewed; brightening improved
  `eligible` And the injury icon.
- EditMode of the entire 722/722 client with `UiArtKitTests` including new textures (2 × size,
  import settings, transparent edges, scene repetition); `check_docs.py` 44 files.

<a id="konsola-tury-w-dolnym-pasku--ui-160-2026-10-14"></a>
## turn console in bottom bar — UI-160, 2026-10-14

Client copy in `scratchpad/wotr-client-test`, test server 5098, database 55432; packages by `scratchpad/packs.sh`.

- EditMode client 725/725, including new `TurnConsoleStateTests` (3).
- PlayMode, final run on the final code:
  - Hunt (`HuntLayout`, `HuntNetwork`, `HuntActivity`, `HuntResponse`, `HuntRecovery`,
    `HuntAftermath`) 19/19;
  - `T22TurnNetwork`, `TestScenarioNetwork`, `MovementNetwork`,
    `VictoryPanelNetwork` 11/11; `CardNetwork`, `CardInteraction`,
    `HandCardLayout`, `Shadow02Layout`, `ShadowStrategy01Layout` 10/10;
  - `EdgeBoardLayout` , `RightColumnLayout` , `CompactBoardLayout` , `BoardChromeLayout` ,
    `ClickInspection` , `ContextHover` 17/18 ; playback, assistance and effects (
    `MapContourSavedRecovery` , `ReturnDraft` , `PoliticalRecovery` , `ContextHelpNetwork` ,
    `EffectsNetwork` ) 9/9. Correction from UI-161 : Filters `InGameContextRecoveryTests` and
    `InGameContextDraftTests` they called files, not class (they are parts of
    `InGameContextNetworkTests` ), so these tests did not start then; check their course UI-161 ;
  - `TurnConsoleLayout` , `DicePanelLayout` , `EdgePlaque` 6/6 ; `BattleLayout` , `BoardInteraction`
    , `HuntNetwork` 23/23 ; `ShadowStrategy03Network` , `ShadowStrategy04Network` ,
    `T22BoardNetwork` 23/23 ; `ArmyContextNetwork` , `MusterNetwork` , `Shadow04Network` ,
    `Strategy03Layout` 19/19 ; `T22Information` , `MultiUseDiceNetwork` ,
    `CharacterRosterActionNetwork` , `ReinforcementsLayout` and other green out recruitment package
    `T22BoardNetwork.FinalExcess` who has passed in the replay after the test assistant's amendment;
    `Strategy04Layout` and `CardPack04Layout` green;
  - `FellowshipLayout`, `FellowshipNetwork`, `MordorLayout` green; `MordorNetworkTests` 3/7, the
    other 4 end with `ApiException: TooManyRequests` (limit 20 logins per minute,
    `AuthenticationRateLimit`), also run independently — limitation of test infrastructure, not
    behavior UI.
- Defeats independent of UI-160:
  - `ContextHoverTests.ClickedInspection…` (movement of 0,4–0,7 px) and `ResourcePanelNetworkTests`
    `Dismay…`, `SpecialChoices…` (Kings) occur identically on the pure HEAD `5a2226a` (run of
    `baseline-head`);
  - `BattlePolicyNetworkTests` is unstable: the script draws hands through `ServerRandom`, and
    without legal card fight the client sends “No card” (UI-151), so the revision moves without
    clicking; in ui160g both cases have passed, in ui160k/ui160l have died;
  - expansions tests (Kings, Lords, ResourcePanel, ExpansionDecision) are beyond regression from the
    user's decision (base game as MVP).
- The dropouts were not viewed and `BuildLocalMac` was discontinued before the end (user decisions:
  when passing tests we skip the dropouts, build is done by the user in Unity Editor).

<a id="gest-pierścienia-elfów--ui-161-2026-10-14"></a>
## The Elf Ring gesture — UI-161, 2026-10-14

- EditMode client 728/728, including new `RingChoiceTests` (3): dice only with
  `UseRing.EligibleDice`, walls with `option.Faces` off, gesture end after cleaning draft.
- PlayMode (`scratchpad/packs.sh ui161b`):
  - `RingUseNetworkTests`   2/2 in the scenarios `fp-elven-ring` and `sh-elven-ring`: illuminated
    and extinguished dice cancel again by clicking the ring and Esc without command, a balloon over
    the panel dice (does not block) dice or header, walls ≥   32 pt and reachable), change dice
    without forcing its use, ring Free Peoples at Shadow, Eye Shadow in Hunt and taken over the ring
    removed;
  - `ContextHelpNetworkTests`, `InGameContextNetworkTests` (with Draft and Recovery parts: draft
    gesture recording, server reboot, smoke returns) and `ResourcePanelNetworkTests.RingCommits…`
    together 12/12;
  - `T22TurnNetworkTests` 4/4 in a separate package;
  - earlier in the same gesture code: `TurnLayoutTests`, `TurnConsoleLayoutTests`,
    `DicePanelLayoutTests` green.
- `T22TurnNetworkTests.RingChanges…` was killed once when it was in one package with
  `ResourcePanelNetworkTests.RingCommits…`: both use the named `turn-ring-politics` script and the
  test server holds its status, so the first test has already given the Shadow ring. In separate
  packages they both pass (isolate belongs to QUAL-06).
- No drop and local build (lighter check).

<a id="okno-polowania-i-przydział-w-konsoli--ui-162-2026-10-14"></a>
## Hunt window and allocation in console — UI-162, 2026-10-14

- EditMode client 726/726, including new `HuntWindowStateTests` (3: Hunt key, stages, Mordor, score
  after completion) and allocation test in console (`HuntInteractionTests`); deleted tests of dead
  `PairedPanelPlacement` and `HuntAllocationView`.
- PlayMode (`scratchpad/packs.sh`, ui162–ui162e):
  - `HuntLayoutTests`,  `HuntNetworkTests`,  `MordorLayoutTests`,  `HuntRecoveryNetworkTests`
    17/17 (Auto-open window, closes, closes result, step bar, console window and next to hand in
    four layouts; assignment: click, turn, sign, drop, ✓ console, zero, reconfirmation;
  - `HuntAftermathNetworkTests` , `HuntRecoveryNetworkTests` , `FellowshipGuideNetworkTests` 7/7 ;
    `RightColumnLayoutTests` , `FellowshipTrackLayoutTests` , `CardPackNetworkTests` ,
    `Shadow03LayoutTests` 9/9 ; `BoardCompletionContextNetworkTests` , `BoardCompletionLayoutTests`
    , `BoardMenuNetworkTests` 13/13 ; `FellowshipNetworkTests` 2/2 ; `T22TurnNetworkTests` 4/4 ;
    `EdgePlaqueTests` , `EdgeBoardLayoutTests` , `ClickInspectionTests` , `FloatingWindowTests` ,
    `TurnConsoleLayoutTests` green; `HuntActivityNetworkTests` , `HuntResponseNetworkTests` ,
    `FellowshipLayoutTests` green.
- Discovered and corrected in progress: window growing at the "Check result" on the low screen
  (content option), 640 reaction window on the incoming hand at 1366×768 and 125% (600 pt), rest
  status converted on opening and closing.
- No drop and local build (lighter check).

<a id="pergamin-przy-świecy--map-18-2026-10-14"></a>
## Candle-lit parchment — MAP-18, 2026-10-14

- `scripts/export_map_canvas.py` (venv ed `requirements-map-canvas.txt`):
  - exports and `--check` PASS;
  - re-exporting gives identical textures (deterministic);
  - `UI-102-MAP-CANVAS.json` report refreshed. The belt hash was out of date since UI-143, water
    rings unchanged.
- Contact texture of the margin and terrain: average difference of 1, 4/255 in the north, east and
  south (before the change of 1,3–1,5).
- EditMode client 728/728.
- PlayMode (`scratchpad/packs.sh`, map run18b):
  - the result of 11/11 for `MapContourRenderingTests`, `MapCanvasLayoutTests`,
    `MapVisibilityTests`, `MordorLayoutTests` and `MapViewportEdgeTests`;
  - the first run (map18) did not pass: the red of the attack on Morannon was illegible on the dark
    Mordor;
  - The amendment is the threshold for the lawing of Mordor and the Forest (`WASH_FLOOR`). The share
    of pixels fulfilling the test in the Mordor regions is now 0,97–0,99 (before MAP-18: 0,96).
- No drop and local build (lighter check). Appearance assessment: scratchpad previews, the final
  view in Unity Editor belongs to the user.

<a id="mocniejszy-pergamin--map-19-2026-10-14"></a>
## Stronger parchment — MAP-19, 2026-10-14

- `scripts/export_map_canvas.py`:
  - exports and `--check` PASS;
  - The result is identical to the viewed render of `m19d`.
- Texture contact: average 1,7–2, 0/255 (p95 4,0–4,7); at close-up of the eastern edge without
  suture.
- Red attack in `MapContourRenderingTests` regions (pixel share):
  - Morannon and Minas Morgul 0,98;
  - Nurn 0,96;
  - Erebor 0,94;
  - Moria and High Pass 0,98;
  - Grey Havens 0,85.
- EditMode client 728/728.
- PlayMode (Map Route19): `MapContourRenderingTests`, `MapCanvasLayoutTests`, `MapVisibilityTests`,
  `MordorLayoutTests` and `MapViewportEdgeTests` 11/11.
- Without drop and local build. The view in Unity Editor belongs to the user.

<a id="linia-konsoli-po-przejściu-akcji--ui-171-2026-10-14"></a>
## Console line after action transition — UI-171, 2026-10-14

- Diagnosis on the live user batch (local server 5088, database `wotr`):
  - the `7792ee78…` batch, the 20 revision, the decision of `Action` Free Peoples;
  - both accounts read the 20 (`match_reads`) revision, so the server was correct and the error was
    in the client.
- PlayMode (`scratchpad/packs.sh`, ui171): `T22TurnNetworkTests`, `TurnConsoleLayoutTests` and
  `RingUseNetworkTests` 7/7. Test Pass checks the line of both clients before and after the
  crossing of action.
- No drop and local build (lighter check).

<a id="przydział-polowania-tabliczką--ui-172-2026-10-14"></a>
## Allocation Hunt with a sign — UI-172 , 2026-10-14

- EditMode client 728/728. `HuntInteractionTests` checks the text of the table: finished (Allocate &
  roll · n dice · Click the plaque...) and not ready (a hint with a missing minimum).
- PlayMode (`scratchpad/packs.sh`, course ui172):
  - `HuntNetworkTests` and `HuntLayoutTests` 11/11. Cases:
    - the unset sign does not send anything and does not open the window;
    - minimum required, return dice in the second click, drop on the tablet;
    - one approval with a sign, including at legal zero;
    - quick second click omitted, ✓ Inactive consoles;
  - `T22TurnNetworkTests` and `TurnConsoleLayoutTests` 5/5 (allocation empty and forced by
    `MatchTestRoom.Confirm` with a plate);
  - `FellowshipGuideNetworkTests`, `HuntRecoveryNetworkTests` and `HuntAftermathNetworkTests` 7/7.
- No drop and local build (lighter check).


<a id="typo-01--cinzel-i-alegreya-2026-10-14"></a>
## TYPO-01 — Cinzel and Alegreya (2026-10-14)

Client `7ba6844` : three static varieties TTF ( 614 116 B), Dynamic FontAsset TextCore And one sheet of typography. EditMode After integration with UI-172 : **728/728 PASS**, 0 Jump. Last results of the targeted PlayMode : **17/17 PASS**, 0 skip, in the aggregation of runs cards , menu/r hues, battle , Fellowship , dense windows and allocation Hunt This is not a full set. PlayMode .

Layouts include 1366×768 and 1920×1080 at 100/125%; card, Fellowship and menu were reviewed. The first attachment did not include typography, because USS used a flipback. After the fonts were properly loaded, the test detected a digital cut (14 px text in the 12 px field). Fixed small track fields and Guide, without changing the panel or writing size; repeated with the 6/6 PASS server.

BuildLocalMac (Universal) and BuildLocalWindows (x64): **Succes**. Byte compatibility of full OFL and notes as plain text files in both packages was tested. Windows runtime and manual acceptance user were not performed. Report, SHA-256 XML and sources: `wotr-client:docs/ui/typography-validation.json`; roles and licenses: `wotr-client:docs/ui/TYPOGRAPHY.md`.

<a id="figurki-narodów--art-01-2026-10-15"></a>
## Figures of Nations — ART-01, 2026-10-15

- Generation: 22 figurines by OpenAI  Images  API  (`gpt-image-1.5`,  1024 ², medium, transparent
  background).
  - 2 rejected and generated again: `gondor-elite` had a inscription on his chest, and
    `sauron-regular` was too massive.
  - Journal: `graphicalassets/figures/v3/generations.jsonl`.
- `scripts/build_figure_art.py` and `--check` PASS (atlas `units-v3`
  2048×512, report `docs/sources/ART-FIGURES.json`).
- EditMode client 732/732. New `UnitFigureArtTests` (4):
  - each unit of base game has a figure of its nation;
  - elite and regular are different cells;
  - spare is a figure UI-76;
  - The mixed army shows the nation with the highest number of figures.
- PlayMode (Article 01 course): `MapArmyArtworkTests`, `FigureMotionTests` and
  `ReinforcementsLayoutTests` 6/6.
- Found in progress: `MapArmyArtworkTests` It's raining on a clean one. HEAD (checked by separate
  course `armyhead` ) A higher line of Alegreya font with TYPO-01 caused the garrison to counter the
  token to the emblem. `Armies.uss` : fixed counter height and smaller, higher emblem.
- No drop and local build (lighter check).

<a id="figurki-i-portrety-postaci--art-02-2026-10-15"></a>
## Character figures and portraits — ART-02, 2026-10-15

- Generation: 14 figurines and 14 portraits (`gpt-image-1.5`; portraits on a painted background).
  - 6 portraits too similar to film actors and 1 with white horns was generated again with its own
    features.
  - Versions are rejected in `graphicalassets/figures/rejected/`.
- `scripts/build_figure_art.py` and `--check` PASS (`characters-v1` and `battle-portraits-v2`,
  2048×512; `units-v3` unchanged).
- EditMode client 734/734:
  - new `CharacterFigureArtTests` (2): each character base game has a figure and a painted portrait,
    versions share the portrait, and expansions retain UI-120;
  - `PortraitArtworkTests` checks for cell invasion within only one atlas.
- PlayMode (Article 02 course): `CharacterRosterLayoutTests`, `FellowshipLayoutTests` and
  `MapArmyArtworkTests` 7/7.
- No drop and local build (lighter check).

<a id="kontrakt-struktury-komendy--qual-01-2026-10-15"></a>
## Command structure contract — QUAL-01, 2026-10-15

- New `CommandInputTests`:
  - 33 mutations on the real decision of the phase Fellowship, also in fields not used by
    `ContinueFellowship`: open `null` in an envelope, 12 in text fields and collections, unknown
    enum numbers, for long text, blank distribution, movement, loss and movement Companion and
    `autoDecision`. Preview and execution give `MalformedCommand` without exception, mutations and
    RNG;
  - positive control: correct command passes, empty text is ‘unchosen’, the foreign batch gives
    `NotAuthorized`;
  - The actual HTTP/PostgreSQL: three R02 reproductions (`rulerTokenId`, `councilTokenId`,
    `regionId` = `null`) give 200 with the saved `MalformedCommand` receiver, without 500 and
    without changing the revision.
- `UI10ShapeTests` and `UI10PayloadTests` expect `MalformedCommand` for structural `null`. A correct
  built illegal choice (unknown route region, alien figure) stays at `IllegalChoice`.
- Targeted tests 69/69. Full set of 3876/3879 server, and 3 failures were environmental and pass
  separately (3/3):
  - `initdb` with Homebrew `libpq` in PATH is damaged after the icu4c update;
  - PostgreSQL 17 without the correct `LC_ALL` does not start;
  - The lobby test is unstable in large runs.

Start with `LC_ALL=en_US.UTF-8` and `PATH=/opt/homebrew/opt/postgresql@17/bin:$PATH`.

<a id="budżety-ruchu-kont--qual-02-2026-10-15"></a>
## Account traffic budgets — QUAL-02, 2026-10-15

- New `GameRateLimitTests` (8) on the test host with PostgreSQL and small budgets from the
  configuration:
  - 12 flood of rejected commands gives 5 200 responses with receiver and 7 responses 429 answers
    with `Retry-After`, without receipt and mutations; the opponent still works;
  - two sessions of one account split the budget;
  - the previews have their own budget;
  - Renewing with the same `commandId` after the window gets the receiver;
  - socket: limit 2, third rejected, slot returns after closing and after closing all counter leaves
    no accounts;
  - configuration validation (4 cases).
- Targeted tests with transport and compliance of the 22/22. client
- Full set of 3884/3887. server Three failures repaired and tested separately:
  - The 93 lobby configuration and concurrent “seen” correspondence are load tests; they have
    received `TestBudgets.Stress` (19/19);
  - `MenuLifecycleTests` looks at the public pages of the list, instead of setting up the first page
    in a common database (4/4).
- No load measurement of 300 session / 150 batch with proof review; belongs to QUAL-18.

<a id="cykl-życia-receipt--qual-03-2026-10-15"></a>
## Life cycle receipt — QUAL-03, 2026-10-15

- New `ReceiptRetentionTests` (4, PostgreSQL):
  - 100 rejects leaves 32 receive to the account in the party;
  - approved command removes outdated rejections of both accounts, retains its receipt (repeating
    after loss of response gives the same JSON);
  - Renewing the deleted rejection on a new revision does nothing;
  - `MalformedCommand` does not save the receipt (GET 404);
  - migration 008 on a copy of the 1–7 schedule retains an approved receipt, removes outdated
    rejection, leaves the current one, and the second migration does not change anything.
- `UI10ProtocolFixture` expects no receipt for `MalformedCommand`. `MigrationRecoveryTests` knows
  the 8 version.
- Full set of 3889/3891. server Two fixed and green cases separately (8/8):
  - the record without new columns has conservative default values (used as approved, never
    deleted);
  - a limited public list of rooms test got `TestBudgets.Stress`.
- The client's journal and `CommandSender` tests were not started: the client code has not changed,
  and the protocol of correctly built commands is the same.

<a id="pas-po-pierścieniu-elfów--qual-04-2026-10-15"></a>
## pass after the Elf Ring — QUAL-04, 2026-10-15

- New `RingPassTests` (5). Expectations were derived from BASE 2.4.1, s. 21
  („He can also skip or pass the action, if eligible to do so”) and B03:
  - Free Peoples after the Ring: 2/3 dice allows pass, 3/3 and 4/3 not; the projection option agrees
    with validation;
  - pass ends the wait after the Ring, the Ring is used and transmitted Shadow, dice is unchanged,
    the queue passes;
  - The Shadow eye from the Ring changes the number of 3/3 to 2/3, making pass legal;
  - HTTP/PostgreSQL with reboot host before and after pass.
- `TurnTests.V02…` corrected by: 4/7 dice after the Ring allows pass. The old test perpetuated the
  error with the review.
- dice Tests, Rings, Council, Kings and 137/137. Projection Full Set of 3896/3896. Server
- The client was not started: it uses the server option and the protocol has not changed. Collective
  integration belongs to QUAL-18.

<a id="budżet-połączeń-postgresql--qual-05-2026-10-15"></a>
## Budget of PostgreSQL — QUAL-05, 2026-10-15

- New `DatabaseBudgetTests` (7), host with 2 pool and wait 1 p. Test takes calls from the server
  pool itself:
  - read and command get 503 `ServerBusy` with `Retry-After`, without details and connection chain;
  - command has no receipt and does not change state;
  - the opening of the nest is rejected;
  - the same command is performed once after release and the same receipt is re-used;
  - the open socket is closed by 1013 `ServerBusy` code and the slot returns;
  - validation and open-ended pool values (6).
- `HostingTests` reads the password from the connection chain instead of checking its end.
- Full 3903/3903. server set
- Measurement of 300 fresh+WS and steamy load 150 batch moved to QUAL-18 (range change in ROADMAP
  and in document QUAL-05).

<a id="zastosowanie-widoku--qual-09-2026-10-15"></a>
## Application of the view — QUAL-09, 2026-10-15

- EditMode client 738/738. New `ViewApplicationTests` (4):
  - The throwing observer does not move the version, and the other gets the view anyway;
  - the same view shall be re-used, the older shall not replace the newer one;
  - 3 try to get a newer view and Reconconnect;
  - unrecorded assessment and private version;
  - a line of diagnostics without hand and exception content.
- PlayMode (qual09 and qual09b runs):
  - `ViewApplicationNetworkTests` with real backend: an injected error with Shadow, the status of
    Display error, after removing the error return on the next notification (the first version of
    the test clicked Pass before the console was ready, corrected);
  - `T22TurnNetworkTests` and `HuntNetworkTests` 11/11;
  - `BattlePolicyNetworkTests` and `ActionLogNetworkTests` green;
  - `ActionNoticeNetworkTests` once unstable (open-log decision mark), green at repetition.
- The client tests on a locked server. Integration of QUAL-02/05 codes with the new client belongs
  to QUAL-18.

<a id="menu-główne--menu-11-2026-10-15"></a>
## Main menu — MENU-11, 2026-10-15

- EditMode Client: full pass 738/739, the only failure is the new test stamp “My games” (mark
  visible before the first list); after the amendment `MainMenuTests` 3/3. New test: default page
  Open games, a poem with the name, “Hosted by ...”, “Join as ...” and the code of the page, the
  stamp of the number of parties with your decision.
- PlayMode, menu run11:
  - `MainMenuLayoutTests` green: 1366×768 and 1920×1080 at 100/125%, action column are located, My
    games replaces the list in the same window of the same size, Host form too;
  - `ReturnListTests`, `BoardMenuNetworkTests`, `AuthRegistrationTests` 8/8 (opening the batch by
    `ReturnTestFixture.ShowList` helper);
  - `MainMenuNetworkTests` and `MenuBrowserNetworkTests` have exceeded the time on the common base
    of `wotr_test`. Full server runs have left 124 open public rooms and their ID are broken down
    into multiple pages of the list. On the fresh base of `wotr_menu_client_test` (seeder requires a
    name with the end of `_test`): 5/5 (the menu run11c).
- Server: `PublicRoomTests` and `MenuLifecycleTests` 6/6. Public list with host, no private rooms
  and codes, no contract changes.

<a id="ustawienia-ekranu--menu-12-2026-10-15"></a>
## Screen Settings — MENU-12, 2026-10-15

- EditMode Client: full course 746/746 (menu12). New:
  - `DisplayOptionsTests`: fixed list of 7 presets, greying larger than the screen, full screen
    exclusive modes, return to Native, panel with fixed places, selection in use and rechecking the
    screen when opening;
  - `InterfacePreferencesTests`: mode and resolution will survive reboot, older write and values
    outside the list give Borderless and Native.
- PlayMode   `InterfaceSettingsTests`  (menu12): click Windowed mode saves the selection and sets ✓;
  `MainMenuLayoutTests` green. System test `SettingsFocusAndDenseHeaderFitAtBothSizesAndScales`
  revealed the button to close the settings 28 pt (icon) UI-138) below the threshold 32 pt; 32 pt
  rounding to pixels in 1920×1080 · 125% was 31,87 The button has now 36 entitled After this
  amendment and the split of the panel designer (menu12c):  `InterfaceSettingsTests`   4/4,
  EditMode   `DisplayOptionsTests`,  `InterfacePreferencesTests` and `MainMenuTests`   15/15.
- `Screen.SetResolution` only works in the player; the mode and resolution changes on the screen are
  checked by the user in the build.

<a id="kompaktowa-karta-akcji-przeciwnika--ui-166-2026-10-15"></a>
## Compact card action opponent — UI-166, 2026-10-15

- EditMode client 753/753. New `ActionResultTextTests` (7):
  - Army movement: heading with target and one line with force and route;
  - the order of validity (reception with VP, battle with reverse, then march and politics as "+2
    more");
  - recruitment from the reserve, policy, entry of the character;
  - Fellowship movement with Hunt and Hunt field;
  - only card discovered: declared and selected do not reach the result;
  - action without effect with text without page name;
  - tooltip group flags and compact request with full save.
- PlayMode (transit ui166), one run in two packages, 14/14:
  - `ActionNoticeCompactTests` (4) on server data: SG01 `ui42-moves`, SG02 `ui42-battles`, SG04
    `ui42-hunts`, SG03 `grey` (card line opens preview, "Full record" opens Log with steps);
  - `ActionNoticeLayoutTests`: card compact in four systems;
  - `ActionNoticeNetworkTests` (2), `ActionLogNetworkTests`,
    `ActionCardLayoutTests`;
  - `HuntActivityNetworkTests` (Hunt archive read after ‘Full record’).

<a id="wstęga-your-action-i-świecąca-konsola--ui-167-2026-10-15"></a>
## "Your action" ribbon and glowing console — UI-167, 2026-10-15

- EditMode client 758/758. New `TurnSignalTests` (5):
  - ribbon once at the action pass, without ribbon at first view, the same decision and the second
    own action in a row;
  - the Fellowship phase ribbons and the allocation of Hunt;
  - no ribbon for decisions in battle, Hunt, card and `ActionSequence`, after reconnection and after
    game end;
  - Light to the first step draft (die, selection);
  - ribbon without an indicator, in the enamel of the page, and a flash of console.
- `UiArtKitTests`: new `console/ribbon-free`, `console/ribbon-shadow`, `console/status-glow`. Render
  `graphicalassets/ui/render.py --only console` did not change existing PNG console; first attempt
  interrupted by the killed Chrome process, second pass.
- PlayMode (transit ui167) 3/3:
  - `TurnRibbonTests` (2) on the fresh batch base game with the server: Fellowship phase →
    allocation ribbon with Shadow → action ribbon with Free Peoples, never with the opponent; click
    on the ribbon place goes under it; the ribbon disappears alone; the reduced motion gives a
    flash; the glow fades after the selection of dice;
  - `TurnConsoleLayoutTests`.

<a id="scenariusze-końca-gry--test-07-2026-10-15"></a>
## Game end scripts — TEST-07, 2026-10-15

- `GameEndScenarioTests` (6): each of the four "Game end" scenarios (`fp-ring-destroyed`,
  `sh-ring-corrupted`, `sh-military`, `fp-military`) with eight kernels of the server ends the game
  with the right winner and reason after its command, and the view of both sides shows it; the ring
  scenarios stand on the track with a certain pool of Hunt, military have points and one die.
- `dotnet test --filter
  GameEndScenarioTests|TestScenarioSessionTests|BaseDiceScenarioTests|MordorTrackScenarioTests`:
  59/59 (catalogue in `TEST_SCENARIOS.md` checked by `BaseDiceScenarioTests`).

<a id="ekran-końca-gry--ui-169-2026-10-15"></a>
## Game end screen — UI-169, 2026-10-15

- The client is testing the new test base `53d6a0a1164f` (server and seeder with TEST-07 scripts;
  `package_deps.py --test-backend`, `bootstrap_deps.py pin wotr-server@… wotr-seeder@…`); Contracts
  0.3.1 and 1.0.5 content unchanged.
- EditMode client 762/762. New `GameEndStateTests` (4): title from player perspective, subtitle and
  scene for four reasons, summary, opening once per game and again after "GAME OVER", console
  status. `UiArtKitTests` with `end/*` graphics.
- PlayMode (ui169 and ui168, 1 pack):
  - `GameEndNetworkTests` (2) at the TEST-07 test sessions: `fp-ring-destroyed` (GE01 at both sides,
    GE02 at the buttons and GAME at the OVER, GE03 at the reopening) and `sh-military` (titles of
    both sides, four non-rewinding circuits);
  - `VictoryPanelNetworkTests`: the first run detected the Victory panel covered with the end of the
    game; the panel opening now closes the screen, in the second run green.

<a id="jedna-zasada-okien-otwieranych-samych--ui-168-2026-10-15"></a>
## One rule for windows opened alone — UI-168, 2026-10-15

- Audit of the SIGNALS_UX C table: all compliant windows (derogation from Victory panel repaired in
  UI-169).
- PlayMode `AutoOpenPolicyTests` ( 4 ): AO01 and phase Fellowship on a fresh batch, AO02 battle (
  `ui43-stale` ) and Hunt ( TEST-07 `sh-ring-corrupted` ), AO03 Meals ( `fp-muster-recruit` ) First
  version of the case Hunt used a random throw ( `fp-character-move-fellowship` ): Hunt without
  decision does not open the window ( UI-162 ), so the test waited endlessly; now the step in Mordor
  always draws a tile with injuries. After the amendment 4/4 (transverse ui168b).
- Window regression (transit ui168, 2) 20/24:
  - `HuntResponseNetworkTests` (3): 429 TooManyRequests on a new base with the QUAL-02 budgets,
    because all tests divide two accounts. `test-client.sh` now launches the server with the highest
    budgets (`dee7d29` client); in ui168b 3/3;
  - `HuntLayoutTests` (known failure out of range): after TYPO-01 fonts the Hunt reply labels in the
    low layout are cut. ‘?’ the Companion stack has been corrected (22 > 20 px). The response name
    ‘Ring’ still has 18,4 at 14,6 px (ui168b), in a file not touched by UI-166–UI-170; it stays on
    the fonts.
  - `BattleWindowNetworkTests`, `HuntNetworkTests`, `HuntAftermathNetworkTests`,
    `FellowshipLayoutTests`, `ReinforcementsLayoutTests` green.

<a id="odbiór-sygnałów-okien-i-końca-gry--ui-170-2026-10-15"></a>
## acceptance signals, windows and game end — UI-170, 2026-10-15

- `53d6a0a1164f` Test Backend, the budgets of QUAL-02 the highest for test accounts
  (`test-client.sh`).
- EditMode client 762/762 (run) ui170).
- PlayMode, course ui170:
  - activity of 15/15: `ActionNoticeCompactTests`, `ActionNoticeLayoutTests`,
    `ActionNoticeNetworkTests`, `ActionLogNetworkTests`, `ActionCardLayoutTests`,
    `HuntActivityNetworkTests`, `LiveActivityNetworkTests`;
  - console and signals 10/11: `TurnRibbonTests`, `TurnConsoleLayoutTests`, `InterfaceSettingsTests`
    green. `T22TurnNetworkTests` expected a ban on the pass after the Ring that QUAL-04 abolished
    (BASE 2.4.1 p. 21); the test now follows the server option;
  - Windows and end of game 27/31:
    - `HuntResponseNetworkTests` ( 3 ): 429 on login, limit 20 log-ins per minute from one address;
      `TestScenarioRoom` uses a joint test account session;
    - `AutoOpenPolicyTests` (battle): waited for click No card when Shadow had card; now it answers
      No card through the server.
- After the amendments (transit ui170b): `AutoOpenPolicyTests`, `GameEndNetworkTests` (3, including
  new `fp-military`), `VictoryPanelNetworkTests`, `HuntResponseNetworkTests` 15/15;
  `T22TurnNetworkTests` 4/4.
- Known failure out of range (with UI-168): `HuntLayoutTests` — after TYPO-01 font label response
  name Hunt is cut in a low order.
- Map of SG01–SG08, AO01–AO03, GE01–GE03 and TE01 for testing:
  [SIGNALS_UX](https://github.com/jpolaczek/wotr-client/blob/d43f852e31189a12f379ef55661b787deedb19e3/docs/ui/SIGNALS_UX.md),
  ‘acceptance UI-170’.

<a id="ci-serwera-klaster-testów-restartu-na-linuksie--2026-10-15"></a>
## CI server: Linux reboot test cluster — 2026-10-15

- The "Verify server" has not passed from many commits: `ActionLogRestartTests` and
  `HuntActivityPersistenceTests` run their own PostgreSQL cluster (`IsolatedPostgres`). Without
  `WOTR_PG_BIN` they searched for `initdb` in the Homebrew path with macOS, which Runner Ubuntu does
  not have (3907/3909).
- `IsolatedPostgres` is looking for tools in turn: `WOTR_PG_BIN`, PostgreSQL 17 with Homebrew, the
  latest installation `/usr/lib/postgresql/<version>/bin`, `PATH`. `ci.yml` installs `postgresql`
  when the runner is not there, and prints the used directory.
- Local (MACO): `dotnet test --filter ActionLogRestartTests|HuntActivityPersistenceTests` 2/2.

<a id="bez-changes--menu-13-2026-10-15"></a>
## Without ‘Changes’ — MENU-13, 2026-10-15

- EditMode client 760/760 (removed test of the old `LobbyBrowser`).
- PlayMode (new `wotr_menu_client_test` database, menu run13–menu13c):
  - `MainMenuLayoutTests`, `MenuBrowserNetworkTests`, `MainMenuNetworkTests` green;
  - `ReturnListTests` 2/2. The first run detected that the finished game opens with the end-of-game
    screen (UI-169) over the board menu button. The `ReturnTestFixture` and `BoardNavigation`
    helpers click on the "View board" first.
- Correction: commit of a MENU-13 client to `56820e0` (finish entry in the register is mistakenly
  entered by `bc62677`, commit before amendment).

<a id="bez-help--ui-173-2026-10-15"></a>
## Without Help — UI-173, 2026-10-15

- EditMode client 735/735 (removed `ContextHelpTests` and `HelpPanelPlacementTests`).
- PlayMode (ui173–ui173f runs):
  - `BoardMenuLayoutTests`, `BoardMenuDraftTests`, `BoardCompletionMenuLayoutTests` green.
    `BoardMenuNetworkTests` detected that the finished game opens with the end-of-game screen
    (UI-169) over the Menu button; the test clicks on the "View board" (ui173b green);
  - `WindowFamilyNetworkTests`, `InGameDenseLayoutTests`,
    `DicePanelLayoutTests`, `FellowshipTrackLayoutTests`,
    `HandCardLayoutTests` 8/8;
  - `RingUseNetworkTests`, `BoardProfileDriverTests`, `BoardCompletionContextNetworkTests`,
    `InGameContextNetworkTests`, `BoardCompletionHostTests`, `DesktopLayoutStabilityTests` green;
  - `InGameProfileDriverTests` green (ui173f) after three profile driver fixes that previously
    obscured the Help step:
    - The Close resource button is from UI-138 the icon and the driver search for it in tooltip;
    - Victory clicks the Menu step after opening the Menu when the panel is already sized;
    - dense layouts develop Effects by clicking the tab in the preparation of the scene.

The entry ‘help’ from the export of the attached base is omitted.

<a id="pełny-ekran-bez-pasów--ui-174-2026-10-15"></a>
## Full screen without straps — UI-174, 2026-10-15

- EditMode client 740/740, including:
  - `DisplayOptionsTests`: height labels in full screen modes, 16:10 presets without belts, read old
    16:9 and odd screen width;
  - new `MapScreenPointerTests`: cutting cursor in full screen, exit from window, empty screen,
    `CursorLockMode`.
- PlayMode (ui174):
  - `MapViewportTests` 8/8, with new check: full screen 1, 40 and 400 px cursor scrolls the map
    towards the edge and the loss of focus stops movement;
  - `InterfaceSettingsTests` 4/4.
- No check (requires build at user): full screen on Mac without straps and scrolling at the top and
  bottom of the physical screen.

<a id="wyjście-z-gry--menu-14-2026-10-15"></a>
## Out of the game — MENU-14, 2026-10-15

- EditMode client 743/743 (new `QuitConfirmationTests`: question, order of items of both menus,
  closing the menu hides the question).
- PlayMode (menu14, menu14b):
  - `BoardMenuLayoutTests` with new test in four layouts: menu order, question window is located,
    focus on Cancel, Esc, click next to and Cancel do not exit, Quit reports exit;
    `MainMenuLayoutTests`: ‘Quit game’ is in the column action, question, Esc, Cancel and Quit;
  - `BoardMenuNetworkTests` 5/5. The first run mistakenly assumed that board disappeared before
    leaving. A batch session is closed and the screen remains until the application closes. The test
    now checks for no write error.
- No check (requires build at user): actual shutdown of the application and exit from Play mode in
  Editor.

<a id="tło-mapy-przed-planszą--menu-15-2026-10-15"></a>
## Map background before board — MENU-15, 2026-10-15

- EditMode client 744/744 (new `FrontendBackdropTests`: login, menu and waiting room begin with one
  background without a pointer).
- PlayMode (menu15): `MainMenuLayoutTests` (the background has graphics and covers the entire screen
  on three screens, in four layouts) and `AuthScreenLayoutTests` 2/2.

<a id="autorzy-oryginalnej-gry--menu-16-2026-10-15"></a>
## Original game authors — MENU-16, 2026-10-15

- EditMode client 745/745 (`FrontendThemeTests`: background and authors line on three screens, full
  text, no indicator).
- PlayMode: `MainMenuLayoutTests` and `AuthScreenLayoutTests` 2/2 (menu16b). The line is on screen,
  the text is not cut and does not occur on card login, window, column action, status message, or
  waiting panel in four layouts. The first run (menu16) detected the application on the high panel
  of the waiting room at 1366×768 and 125%. The panel got 20 px margin at the bottom.

## Instalatory Velopack — DIST-06, 2026-10-15

- Spike'and S1–S4 GO : S1/S3/S3b on Mac publisher (macOS 15.6.1 arm64), S2/S4 In GitHub Actions
  38706083369 ); results in the branch `spike/dist-06-velopack` votr-client,
  `spikes/velopack/results/`.
- `python3 -m unittest discover -s tests/release -p 'test_*.py'` in votr-client: 30/30 ( gate
  sources, build identity, banned files, launcher and PE x64, Mach-O architecture, `SHA256SUMS` , a
  pack of dragons outside the issue, SHA-256 Velopack libraries); Verify client PASS.
- Workflow Installers 38715911738 on votr-client `44dc0dd` (previously 38712960272 au `194d88f` ):
  build release GameCI both platforms clean `Library` ; Windows (Server) 2025 ): silent installation
  without game start, shortcuts, self-check, damaged package rejected (exit 1 , without hooks),
  update 0.2.0 -rc1 → 0.2.1 -Smoke with hooks out 0 installation, data `LocalLow` unchanged; macOS (
  26.6.2 arm64: codesign ad-hoc , both architectures, self-check Arm64 and X64 under Rosetta,
  damaged package rejected, password-free update, data unchanged; `package_velopack.py verify` PASS
  .
- DS14 on Mac publisher: `.pkg` from the artifact downloaded with a browser, Gatekeeper "Open
  anyway", installation "only for me" in `~/Applications`, the game started alone with the main menu
  (the first graphical start of the build from Linux), `codesign --verify --deep --strict` OK,
  without quarantine, `x86_64 arm64`, Velopack at `Player.log`.
- EditMode 762/762 on Mac publishers after DIST-06 changes.
- Not made: Windows Tester Tester (DS13) — [DIST-13](tasks/DIST-13.md); Game-level update — DIST-07;
  CI game works without graphics.

<a id="wydania-z-tagu--dist-10-2026-10-15"></a>
## Tag releases — DIST-10, 2026-10-15

- Prebuild refusals: tag `v0.9.0-nonotes` without `release/notes/<version>.md` (Release 38725389737 )
  and tag `v0.9.0-mismatch` not compatible with `version.json` ( 38725392959 They end in a job
  `check` Read message; rest omitted. No secrets: same control as `prepare` In `installers.yml`.
- EditMode in GameCI (`editmode.yml`, CLI v0.1.72): 38727164657 761/762 (no `.artifacts` in clean
  checkout), after the 38728117442 762/762. amendment
- Test editions `v0.9.0-test.3` ( 38728918985 , 12 min) and `v0.9.0-test.4` ( 38728919615 , 14 min:
  all green firs; draft pre-release with 8 files (packages) 64,5 and 72,3 MB , installers 72,6 and
  70,8 MB ), `build-info` and notes consistent with `SHA256SUMS` , both `build-info` with a tag
  commit, protocol 2 and `development: false` Job Re-Re-Re-Re-Re-Re-Re-re-Re-Re-Re-Re-Re-Con.
  `draft` replaced draft .
- `python3 -m unittest discover -s tests/release` in wotr-client: 33/33.

<a id="pierścienie-bez-ramek--ui-175-2026-10-15"></a>
## Frameless Rings — UI-175, 2026-10-15

- EditMode client 745/745 (`UiArtKitTests` without `ring-socket` graphics).
- PlayMode (ui175): `TurnConsoleLayoutTests` and `RingUseNetworkTests` 3/3, with new Assertion: the
  slot has no graphics, color, or border and shows the ring graphics when the ring is resting, armed
  (`selected` and `legal-target`), taken over and removed.

<a id="widok-gry-na-każdym-ekranie--ui-176-2026-10-15"></a>
## Game view on each screen — UI-176, 2026-10-15

- EditMode client 747/747, including `DisplayOptionsTests`:
  - `WotrPanel` in mode Expand (1366×768, ScaleWithScreenSize);
  - `DisplayController` with screen appeal: the full screen passes from 3024×1964 (1080p →
    1662×1080) to the 16:9 (1920×1080) screen, once; the window retains size as long as it fits, and
    takes screen size when it stops.
- PlayMode (ui176):
  - `TurnConsoleLayoutTests` in eight layouts: 16:9 ×4, 1440×900, 3024×1964 at 125% and 2560×1080 at
    100% and 125%. board has a image size, the map reaches both edges, dice are at left, card at
    right, console between them;
  - `MapViewportTests`, `LayoutTests`, `MainMenuLayoutTests` 11/11.
- No check (requires build at user): full screen on the external screen and on the built-in Mac
  screen and moving the window between them.

<a id="wyraźny-krzyżyk-karty-akcji--ui-177-2026-10-15"></a>
## Clear cross card action — UI-177, 2026-10-15

- EditMode client 747/747.
- PlayMode (ui177): `ActionNoticeCompactTests` and `ActionNoticeLayoutTests` 5/5. Crossword has at
  least 30×30 pt and sign 22 pt, stands last in the upper right corner of card and closes it with
  one click (three scenarios `ui42-*` and `grey`).

<a id="tor-polityki-ze-strzałką--ui-178-2026-10-15"></a>
## Policy track with arrow — UI-178, 2026-10-15

- EditMode client 747/747.
- PlayMode (ui178, ui178b):
  - `PoliticalPanelTests` (with new `AssertTrackRows`: one column of blank arrows, no empty column,
    no button frames, four layouts for both sides), `PoliticalGestureTests`, `LeftRailLayoutTests`,
    `BoardChromeLayoutTests` 10/10;
  - `PoliticalRecoveryTests`, `T22TurnNetworkTests` (step sent by an arrow of a row; arrow of the
    opponent expired), `MusterNetworkTests`, `MultiUseDiceNetworkTests` 11/11. The first run
    selected to check the extinguished Gondor arrow, which in this scenario can move; the test takes
    the line of the opponent.

<a id="posiłki-jako-zakładka-listwy--ui-179-2026-10-15"></a>
## Meals as a strip tab — UI-179, 2026-10-15

- EditMode client 748/748. `ReinforcementRowsTests`: lines without political status, new state test
  for inspection (`ReinforcementsState.Browse`).
- PlayMode (ui179, ui179b):
  - `BoardInteractionTests` (UI-130 with the tab held by Muster and the highlighted Gondor; new
    UI-179: tab under Politics, reserves outside recruitment, line without track and action, step
    arrow Politics), `ReinforcementsLayoutTests` (dotted panel, header, illuminated lines),
    `LeftRailLayoutTests`, `PoliticalPanelTests` 19/19;
  - `AutoOpenPolicyTests`, `MusterNetworkTests`, `T22BoardNetworkTests`, `T22InformationTests`
    green. `MultiUseDiceNetworkTests` measured the chip in the first run during the tab insertion;
    the test is waiting for the end of the pass (ui179b 3/3 together with `CardNetworkTests`);
  - `Strategy03LayoutTests`, `ShadowStrategy03/04` (system and network) 21/21;
  - `CardNetworkTests` detected regression: after Back with card waiting for the nation (UI-148) the
    Meals tab was rolled up by Politics, and the re-arming of card did not open it. The key “once
    played” is deleted after the game (ui179b green). `MovementNetworkTests` green;
  - known failure beyond regression base game :
    `CharacterRosterActionNetworkTests.RulerTokenRecruitmentUsesOneCharacterHostAndOneCommit`
    (Kings) — cut down by the inscription ‘Kings’ in `character-edition` character panel, unrelated
    to meals.

<a id="pełny-ekran-domyślnie--menu-17-2026-10-15"></a>
## Fullscreen default — MENU-17, 2026-10-15

- EditMode client 748/748. `DisplayOptionsTests`:
  - in Fullscreen and Borderless always native resolution and no button inactive;
  - the window selects the size on the screen;
  - Fullscreen start, also with 1 version;
  - `DisplayController` after changing the screen.

`InterfacePreferencesTests` assumed Borderless and unknown version of 2; updated (the menu run17b).
- PlayMode  (menu17):  `InterfaceSettingsTests`  (Fullscreen  ✓ and expired resolution, in Windowed
  choice works), `MainMenuLayoutTests`   5/5.
- No check (build requires): full screen application start.

<a id="bariera-kursora--ui-180-2026-10-15"></a>
## Cursor Barrier — UI-180, 2026-10-15

- EditMode Client 751/751, new `CursorBarrierTests`: turning the cursor to the nearest edge of the
  screen, the cursor on the screen unchanged, conditions (full screen, focus, player).
- PlayMode (ui180): `LayoutTests`, `InterfaceSettingsTests` 6/6 (client host with inactive barrier
  in Editor).
- No check was made (requires build on Mac with two screens): calling CoreGraphics (`MacCursor`) and
  stopping the cursor at the edge, slowing down after Cmd-Tab; Confined on Windows.

<a id="zakładka-reserve-i-grafika-zakładek--ui-181-2026-10-15"></a>
## Reserve tab and bookmark graphics — UI-181, 2026-10-15

- EditMode client 751/751 (`UiArtKitTests` with five graphics `rail/edge-tab`). Graphics with
  `render.py --only rail` (headless Chrome).
- PlayMode (ui181, ui181b):
  - `BoardInteractionTests`, `LeftRailLayoutTests`, `ReinforcementsLayoutTests` 15/15 and
    `PoliticalPanelTests`, `BoardChromeLayoutTests` green (ui181);
  - `AutoOpenPolicyTests` (Muster backlights the tab and the light dice, does not develop it;
    opponent sees nothing), `MusterNetworkTests`, `MultiUseDiceNetworkTests`, `T22BoardNetworkTests`
    15/15 (ui181b), `T22InformationTests`, `MovementNetworkTests` green (ui181);
  - `Strategy03`, `ShadowStrategy03/04` (system and network), `CardNetworkTests` 22/22 (plan card
    continues to expand and hold the tab).
- The first run detected:
  - scrolling the bookmark between figure one Muster; an open recruitment sequence holds it;
  - an old tab graphics test (`button-hover`);
  - a close comparison of the position of the chip (0,01 pt. after transition); the test shall
    accept the tolerance of 0,5 pt.

<a id="polecenie-wydawcy--dist-11-2026-10-15"></a>
## Developer command — DIST-11, 2026-10-15

- `python3 -m unittest discover -s tests/release -p 'test_*.py'` : 18/18 Local (Python) 3.14 ) and
  `python:3.9-slim` (Python macOS system); Verify server 38733315628 PASS with this step. Coverage:
  complete draft ; refuses — changed file, no platform, excess file, blank notes, other commit,
  build developer, no protocol, other version in `build-info` and `.nuspec` ; `--dry-run` only signs
  locally; no confirmation sends nothing; re-check after publication only checks; the announced
  version without local signature is rejected; the key must have 0600 and be equal to the host key;
  public checks on the local server HTTP (catalogue, minimum, manifesto bytes, downloads, refusal
  without support of the Range); `release_host.py` : `.env` , the state of the directory, the
  commands ReleaseTool .
- Actual `ReleaseTool` in `dotnet/sdk:10.0.401` on draft files: `keygen`, `sign` × 2,
  `verify-upload` PASS, `publish --minimum --at`, `status` ("scheduled minimum 0.9.0 ... anounced
  0.9.0"), files in `public/releases/0.9.0/<platform>/`.
- Not performed: publication on the host and public addresses (DIST-04), client observation
  (DIST-07).

<a id="zaproszenie-przy-tworzeniu-konta--menu-18-2026-10-15"></a>
## Invitation for account creation — MENU-18, 2026-10-15

- EditMode client 752/752. `GatewayScreenTests`:
  - three fields with optional invitation;
  - Create account rejects code in the wrong format and transmits the correct (no space) code;
  - Login omits the invitation;
  - The empty field is working so far.
- PlayMode (menu18): `AuthRegistrationTests` 2/2 (new screen registration test with `AuthInvite`
  with server fixture and uninvited registration to date), `MainMenuLayoutTests` (screen with third
  field in four layouts).

<a id="poczekalnia-bez-mrugania--menu-19-2026-10-15"></a>
## Waiting room without blinking — MENU-19, 2026-10-15

- EditMode client 752/752.
- PlayMode (menu19, fresh base `wotr_menu19_client_test`): 4/4.
  - New `WaitingRoomPollTests` with HTTP atrap and refresh what 100 ms: Back, Ready and Generate
    invitation constantly active by 1,5 s at least 3 refreshes, player message stays, connection
    error shows message and disappears on server return.
  - `MainMenuNetworkTests` green (waiting with server).
- The first start of the package fell on the lack of a test database (seeder requires an existing
  `_test` database); after `createdb` green.

<a id="tańsze-ci-serwera--build-03-2026-10-15"></a>
## Cheaper CI server — BUILD-03, 2026-10-15

- `python3 -m unittest discover -s tests/ci -p 'test_*.py'`: 5/5. Tests include:
  - Path classification: pinned client in `docs/sources/*.md` is documentation, and `docs/sources`
    and `docs/TEST_SCENARIOS.md` data is code;
  - empty change and unknown base;
  - actual git ed commitem documentation. repository
- `scripts/ci_scope.py 7f450138 HEAD`: 133 changed paths, the documentation itself (previously full
  run).
- GitHub Actions:
  - 38755254215 (commit with documentation only): `checks` 17 s, `backend` omitted, whole 54 s;
  - 38755558439 (change of workflow): `checks` and `backend` green, 14 min.
- The first full run (38754104594) cancelled `concurrency` when the `main` came to commit with the
  documentation itself. On `main` the runs are no longer cancelled.

<a id="zakładki-bez-wstęg-i-ramy--ui-182-2026-10-15"></a>
## Tabs without ribbons and frames — UI-182, 2026-10-15

- EditMode client 752/752 (`UiArtKitTests` with new `rail/edge-tab`, `render.py --only rail`
  graphics).
- PlayMode (ui182): `LeftRailLayoutTests` (new signature checks: one line, centered at the bottom,
  plain font, fits in the tab), `BoardChromeLayoutTests`, `PoliticalPanelTests` 8/8.

<a id="panel-kości-przy-wyborze-kości--ui-183-2026-10-15"></a>
## Panel dice when selecting dice — UI-183, 2026-10-15

- EditMode client 752/752.
- PlayMode (ui183): 6/6 and 13/13.
  - `AutoOpenPolicyTests` (assignment Hunt Shadow with server): panel frame Shadow lights up, panel
    Free Peoples no, dice `hunt-candidate` has a perimeter, frame replaces padding without moving
    dice, after sending the backlight is extinguishing.
  - Also `TurnConsoleLayoutTests`, `DicePanelLayoutTests`.
  - Package 13/13: `HuntNetworkTests`, `RingUseNetworkTests`, `T22TurnNetworkTests`.

<a id="adres-serwera-z-pliku-konfiguracyjnego--menu-20-2026-10-15"></a>
## Server address from configuration file — MENU-20, 2026-10-15

- EditMode client 757/757, new `ServerConfigurationTests`:
  - address for `https://wotr.example.test` builds, for local Editor;
  - the address is a valid HTTPS server;
  - the server indication by test and return;
  - file without both addresses is rejected.
- PlayMode (menu20): 9/9.
  - `AuthRegistrationTests`: Test server by `ServerConfiguration.Use`.
  - `MainMenuLayoutTests`: Settings without Server and Save server fields.
  - `InterfaceSettingsTests`, `LayoutTests`.
- No check (requires build): connect the game installed with `wotr.example.test` without entering
  the address.

<a id="usunięcie-partii-na-serwerze--menu-21-2026-10-15"></a>
## Remove the batch on the server — MENU-21, 2026-10-15

- `dotnet test --filter MatchDeletionTests|TestScenarioSessionTests|MenuLifecycleTests|CorrespondenceListTests|LobbyTests|DatabaseBudgetTests`:
  32/32. New `MatchDeletionTests` (3):
  - removal by each party party started from the room: 204, zero lines in `matches`,
    `command_receipts`, `match_events`, `match_reads` and room; 404 for reading at both; the batch
    is not on the list of both players; re-exit 404;
  - an alien account gets 404, and the party and room stay;
  - The open enemy socket closes in less time than the reading of what 5 s, that is, by
    `RevisionHub.Publish`.
- Full `dotnet test --configuration Release`: 3912/3912 (`TestSessionStore` now uses the same
  `MatchRemoval.Remove`).

<a id="delete-game-w-menu-planszy--menu-22-2026-10-15"></a>
## "Delete game" in the board menu — MENU-22, 2026-10-15 menu

- The client tests on the test base `87d219717994` (MENU-21; `package_deps.py --test-backend`,
  `bootstrap_deps.py pin`).
- EditMode client 759/759, including `ConfirmationDialogTests` (formerly `QuitConfirmationTests`):
  the order of the board menu with the "Delete game", the text and names of the deletion questions,
  the "Game deleted" window.
- PlayMode (menu22): 15/15.
  - `MatchDeletionNetworkTests`: a party from the room of two new accounts; Cancel leaves the party;
    Delete at the host returns to the menu; the guest gets "Game deleted" and returns via "Main
    menu"; no list of "My games" or `GET /api/matches` has a batch; `GET` of the party gives 404.
  - `BoardMenuLayoutTests`: Quit and Delete questions in a common test (four layouts, focus on
    Cancel, Esc, click next door, Cancel, confirmation); for Delete also “Deleting game...”, grey
    button and Retry.
  - Regression: `BoardMenuNetworkTests`, `MainMenuLayoutTests`.
- No check was made: the batch was removed on the host server — it requires implementation MENU-21
  to `wotr.example.test`.

<a id="czytelna-pula-polowania--ui-184-2026-10-15"></a>
## Readable pool Hunt — UI-184, 2026-10-15

- EditMode client 759/759.
- PlayMode `HuntLayoutTests` and `HuntNetworkTests` 12/12 (ui184e), `AutoOpenPolicyTests` 4/4
  (ui184f).
  - New `ThePoolAtRestReadsAsAGridUnderItsHeadingInFourLayouts` (window at rest, as user's throw)
    and common `CheckPool`: headset unpressurized and over tiles, equal cells (treasures tolerance
    to pixels), common columns, Mordor tiles in separate lines, tile ≥ 28, counter ≥ 13 entitled
  - During Hunt, the open pool does not change the position or size of the window, is drawn behind
    the decision and is located above the console.
- On the old style, the new test failed (irregular cells). On the unchanged HEAD
  `RerollsTileAndGuideFitTheHuntWindowInFourLayouts` also failed earlier: the name "Ring" cut in
  step of injury on a short screen; repaired in UI-184.

<a id="większe-figury--ui-185-2026-10-15"></a>
## Bigger figure — UI-185, 2026-10-15

- EditMode client 759/759.
- PlayMode (ui185):
  - package 1, 13/13: `MapArmyArtworkTests`, `MapVisibilityTests`, `RegionInspectionTests`,
    `ReinforcementsLayoutTests`, `BoardChromeLayoutTests`, `Strategy03LayoutTests`,
    `T22InformationTests`;
  - 2, 10/10: `MovementNetworkTests`, `MapClickMoveNetworkTests`, `MusterNetworkTests`,
    `TravelGestureNetworkTests`.
- `MapArmyArtworkTests` (ui185c): gallery set up for larger tags (8 to row, located at 125%); each
  shown figure has its shadow inside the tag and has ≥ 52 entitled heights.
- No visual examination (according to the rule without discharges): whether the shadow is sufficient
  in the brightest terrain; for assessment in the build.

<a id="nazwy-regionów--ui-186-2026-10-15"></a>
## Names of regions — UI-186, 2026-10-15

- EditMode client 761/761, new `MapRegionNamePointsTests` (2):
  - each of the 105 regions has a name point in the region, ≥ 5 units from the boundaries, with a
    horizontal and vertical position;
  - no point lies in the Army's place (0 of 105);
  - search for the entire 147 ms map (first version without simplifying contours: 627 ms);
  - breaking a long name at the middle space.
- PlayMode (ui186): `MapRegionNamesTests` , `MapVisibilityTests` , `MapArmyArtworkTests` ,
  `RegionInspectionTests` , `MapContourAcceptanceTests` First run 7/8: threshold ‘> 15 names across
  the map” was too harsh for 1366×768 with 125% (there are names of fortresses and cities as
  planned) After changing the threshold `MapRegionNamesTests` 1/1 (ui 186b) in four systems:
  - the names do not take the indicator;
  - lie in their regions and on the map;
  - they do not invade any army;
  - approximately ×2 can be seen above 8 names.
- One glance at the drop of the approximation (style): the first version with the border was pale;
  tinted ink, the border replaced by the glow.

<a id="krótkie-opisy-umiejętności-postaci--ui-187-2026-10-15"></a>
## Short character skill descriptions — UI-187, 2026-10-15

- EditMode client 763/763, new `CharacterAbilitySummariesTests` (2):
  - 14 cards base game has on the line on each skill;
  - The shortcuts are only for 22 skill base game, each ≤ 72 characters, one line, no dot;
  - the format ‘title · abbreviation’ after the lines of the tooltipu (Gimli, Gollum);
  - card expansions does not get the line.
- PlayMode (ui187), 6/6:
  - the new `CharacterAbilityTooltipTests`: a portrait in the region panel and a portrait of Gandalf
    in Fellowship — a name line, then exactly the lines of abbreviations, without repetition, with
    the skill of Guide;
  - `RegionInspectionTests`, `CharacterRosterPanelNetworkTests`.
- `CharacterRosterPanelNetworkTests` 3/3 (ui187b): the tooltip of the Gandalf line (showing the
  White version) ends with the Gandalf the White shortcuts.

<a id="małe-okienko-zwykłego-regionu--ui-188-2026-10-15"></a>
## Small window of the normal region — UI-188, 2026-10-15

- PlayMode new `RegionNameOnlyTests` on the 1 and 1,25 scale:
  - the empty region (Andrast) has a class of `name-only`;
  - window width is text plus spaces, less than the former 240 pt;
  - height below 60, without ‘No forces’ line, lines not cut;
  - Minas Tirith with forces retains a permanent arrangement (≥ 240 pt).
- First run (ui188, 3/4): 66 set heights by default Cinzel label and metric spacing. After zeroing
  out the intervals with `name-only`: `RegionNameOnlyTests` 1/1 (ui188c).
- `RegionInspectionTests` and `CharacterAbilityTooltipTests` 3/3 (ui188).
- A glance at the drop: the window “Andrast” in two short lines.

<a id="katalog-dźwięków--audio-01-2026-10-15"></a>
## Sound directory — AUDIO-01, 2026-10-15

- `graphicalassets/audio/generate.py` generator (synthesis, solid grains): 25 WAV mono 48 kHz for 12
  cue. Analysis of each file in `manifest.json`:
  - edges 0 and DC  <  5 · 10 ⁻ ⁴;
  - no overdrive, true peak from −1,3 to −12,6 dBTP;
  - weighted volume K according to the targets (effects −21...−27, alerts −17...−20);
  - lengths in limits.
- Spectrograms (sheet in working directory) detected in the first version of the clippings of cut
  fashions, the paper sym and the sharp doorbell; corrected before listening.
- EditMode client 765/765, new `AudioCatalogTests` (2):
  - 12 cue on the right channels, track pair ≤ 0,9 s;
  - each video: mono 48 kHz, length, silence on edges, top 0,05–0,9;
  - import PCM, Decompress On Load, preload.
- User listening (hearing page): 7/12 No attention. CardInspect , BattleStarted (war horn at
  request), FellowshipAdvanced , CorruptionChanged and DecisionRequired revised according to
  comments. The re-hearsal of the amendments is open (also in AUDIO-08 ).

<a id="moduł-odtwarzania--audio-02-2026-10-15"></a>
## Playback module — AUDIO-02, 2026-10-15

- EditMode client 773/773, new `SoundPolicyTests` (8) on a controlled clock with a recorder:
  - priority of one decision and channels of 40/60%;
  - event once again, new identical event again, new batch from scratch;
  - 100/250 ms and variants consecutively;
  - one clip at a time (important extinguishs, the rest falls off without queue);
  - background filter prior to priority, without catching up after focus return, Background alerts
    disabled;
  - mute stops the clip and re-enables nothing;
  - Journey sequence: shoes → Ring → decision, and click and confirm this step are silent; repeated
    effect does not play; Mordor cancellation after the first stage;
  - The sequence in the background is only playing the final victory.
- PlayMode (audio02b): `SoundModuleTests` (2), `InterfaceSettingsTests` and `BoardMenuLayoutTests`,
  together 14/14:
  - true directory (12 cue), clip and channel volume per source;
  - mutement in ≤ 40 ms;
  - one owner of sound per interface, removed with it.
- The first course of PlayMode detected:
  - Disabled built-in Unity Audio module — enabled in `manifest.json`;
  - log “no audio mailers” through which they failed the Settings tests — the output is provided by
    the mailer;
  - object with `HideFlags.DontSave` invisible for search — deleted.

<a id="ustawienia-dźwięku-i-tło--audio-03-2026-10-15"></a>
## Sound and background settings — AUDIO-03, 2026-10-15

- EditMode client 774/774, new case `InterfacePreferencesTests`:
  - default 40/60% and enabled;
  - After reboot, 0%, the sound and background alerts off are switched off;
  - the 5% step and only the actual changes shall notify;
  - 2 version retains window mode and gets the default sound;
  - the level outside the range returns to the default.

Earlier case of the "3 version unknown" moved to the 4 version. Background filter prior to priority and no catch-up checks `SoundPolicyTests` (AUDIO-02).
- PlayMode (audio03), 8/8: new `SoundSettingsTests`, `InterfaceSettingsTests` (Settings arrangement
  with Sound section in four layouts), `SoundModuleTests`, `MainMenuLayoutTests`. W
  `SoundSettingsTests`:
  - sliders change the levels immediately and nothing plays;
  - Test sound game ActionAccepted, then DecisionRequired at level
    Alerts;
  - shut off grey levels on site and mute;
  - the choice will survive re-loading.
- No check was made: actual focus loss and minimization on Windows/macOS — belong to AUDIO-08.

<a id="dźwięki-gestów--audio-04-2026-10-15"></a>
## gesture sounds — AUDIO-04, 2026-10-15

- EditMode client 774/774.
- PlayMode, the new `GestureSoundsTests` with real pointer and keyboard events and cue recorder:
  - hover silence;
  - click and Enter give one `UiClick` each;
  - release outside the control and the control switched off silently;
  - the gesture opening the preview card gives `CardInspect` instead of a click;
  - the gesture sending command and the gesture omitted by board are silent.

The first two runs failed on the rustling: identifier outside the cards directory, and then the shutter made after action the intercepted button; the shutter is now at the beginning of the pressurization. Result: `GestureSoundsTests` and `PressFeedbackClickTests` 3/3 (audio04c).
- Network regression with gestures playing sound:
  - `CardNetworkTests` and `BoardMenuNetworkTests` green in audio04net;
  - in one package, 18 tests 7 failed on `TooManyRequests` (test server limit);
  - in separate packages (audio04net2): `HuntNetworkTests` 7/7, `MatchDeletionNetworkTests` 1/1,
    `T22TurnNetworkTests` 4/4.
- The cases of the ‘unsent gesture’ and ‘sent command’ were checked by module switches
  (`GestureIgnored`, `CommandSent`); the run on the real server will include the AUDIO-05 tests.

<a id="karty-kości-przyjęcie-i-błąd--audio-05-2026-10-15"></a>
## card, dice, party and error — AUDIO-05, 2026-10-15

- EditMode client 781/781.
  - New `SoundFactsTests` (4):
    - dice action once per turn throw, and the release of dice is silent;
    - round roll battle both sides are one sound; the same pitch is silent again; the metasta and
      the new round with the same results play;
    - Hunt projection and spread;
    - Own card (two selected is one rustle) and the public recoil of the opponent are playing, and
      the hidden declaration card the fight is silent.
  - New `GameSoundsTests` (3) with `ICommandEvents` atrap:
    - the first view is silent;
    - `ActionAccepted` once, regardless of the order of response and projection, and played card
      replaces them;
    - rejection gives one `ActionFailed`;
    - The recovered response and catching up after the reconnect are silent.
- PlayMode with server (audio05):
  - the new `SoundResultsNetworkTests`: adopted "continue" Fellowship is one `ActionAccepted`
    without a button click; after assigning Hunt to dice to action to be one `DiceRoll` to both
    players, without additional acceptance with Shadow; reconnect does not repeat anything;
  - regression `T22TurnNetworkTests` 4/4.
- Not checked on the server (covered in EditMode): rejection by UI, lost response with recovery,
  projection and spread battle and Hunt and cards traffic. Collectively, they belong to AUDIO-08.

<a id="bitwa-i-sekwencja-torów--audio-06-2026-10-15"></a>
## battle and track sequence — AUDIO-06, 2026-10-15

- EditMode client 784/784, new `TrackSequenceTests` (3):
  - march only at the actual step (reset, track entrance and Stop not);
  - Corruption both ways;
  - stage one and order of sounds (travel: Fellowship → Ring, Mordor: Ring → step), pair with end
    signal ≤ 0,9 s;
  - new battle once, new round without signal, single track with own cue.
- PlayMode (audio06):
  - new `TrackPairTests` (2, fixed views):
    - stage one on the panel and both sounds in order, without others;
    - the newer state in the process ends the stage and second sound;
    - Mordor with Corruption first;
    - Reduced motion means a marker;
  - new `BattleSoundNetworkTests`: new battle once for both players, and reopening the window does
    not repeat;
  - regression `AutoOpenPolicyTests` 4/4.
- The Fellowship and Mordor regression (audio06b–d, in separate packages due to the limit of
  requests) detected:
  - `MordorLayoutTests`: steps 0→3→5 with change Corruption are now staged (Corruption before step).
    The test awaits additional 0,38 s;
  - `FellowshipLayoutTests`: expected the former "Guide:" in the portrait tooltip, and since UI-187
    is the line "Guide · ...". It was my error with UI-187: I did not run this test at the time;
  - `MordorNetworkTests.BothFinalReceipts…`: failed independently of AUDIO. The end screen (UI-169)
    was hidden by "Check result", and the Hunt window after the result (UI-162) is no longer Hunt
    Box itself. The test now clicks "View board" and checks that no choice remains after the result.

After the amendments: the layout of Fellowship and Mordor 10/10; `FellowshipMotionNetworkTests` 1/1; `FellowshipNetworkTests` 2/2; `MordorNetworkTests` 4/4 (one at a time, `BothFinalReceipts` in audio06d).
- Track pair not checked on the server: Hunt tiles are random, and test scenarios do not give a
  certain pair in one settlement.


<a id="wymagana-decyzja-i-koniec-gry--audio-07-2026-10-15"></a>
## Decision required and game end — AUDIO-07, 2026-10-15

- EditMode client 786/786, new `DecisionSoundsTests` (2):
  - decision passing to the player once (`decision/<id>`), also reaction after waiting for the
    opponent; own next step and choice omitted by the server silent;
  - Victory/Defeat from player perspective once; further views of the finished party remain silent.
- The background filter prior to priority (battle with decision: in the foreground of the jaws
  battle, in the background of the decision tone) remains checked by `SoundPolicyTests` with
  AUDIO-02.
- PlayMode (audio07, separate packages due to request limit):
  - `TrackPairTests` 3/3, new test: decision of the same solution plays after a pair of tracks, once
    (Fellowship → Ring → decision);
  - `GameEndNetworkTests` 3/3 on TEST-07: Free Peoples hear Victory, Shadow Defeat; View board, GAME
    OVER and reopen the finished batch are quiet;
  - `TurnRibbonTests` 2/2 (common `TurnSignal`);
  - `SoundResultsNetworkTests` 1/1 on the server (audio07b):
    - the Hunt allocation passes to Shadow with a decision tone;
    - when the action throws, it goes to Free Peoples: they hear one tone of decision and Shadow one
      pitch, without their own tone;
    - The reconnect is not playing anything.

The first run failed. The test worked immediately after the decision tone (0,75 s), and the “one clip at a time” policy rejects the less important throw. The test is now waiting for 0,8 s, as the player before the decision. The policies have not changed.
  - `BattleSoundNetworkTests` 1/1 (audio07b): the new battle still once on both players despite the
    decision of the defender in the same settlement.
- Not checked on the server: decision of response in Hunt and effect of card (are in EditMode) nor a
  pair of tracks with decision (permanent views, because the Hunt tiles are random).

<a id="odbiór-dźwięków--audio-08-2026-10-15"></a>
## acceptance sounds — AUDIO-08, 2026-10-15

Client `792b615` (macle in AUDIO_UX `a276d04`), local test server, separate packages with breaks 25–45 s due to the limit of requests:

- full EditMode 786/786 (audio08);
- PlayMode:
  - locally: `SoundModuleTests`, `SoundSettingsTests`, `GestureSoundsTests`, `TrackPairTests` and
    `InterfaceSettingsTests` 11/11;
  - with server: `SoundResultsNetworkTests` 1/1 , `BattleSoundNetworkTests` 1/1 ,
    `GameEndNetworkTests` 3/3 , `TurnRibbonTests` 2/2 , `AutoOpenPolicyTests` 4/4 ,
    `CardGestureNetworkTests` 1/1 , `FellowshipMotionNetworkTests` 1/1 , `HuntResponseNetworkTests`
    3/3 ;
  - Mordor and Fellowship 10/10 system;
  - `MordorNetworkTests` 7/7: 3 in the package, and 4 in one (audio08b) because the package hit
    `TooManyRequests`;
  - recovery: `InGameContextNetworkTests` 3/3 and `BoardCompletionContextNetworkTests` 1/1
    (audio08b; in audio08 file name filter found no classes).
- Open, not passed:
  - rehearsing five improved sounds by the user;
  - actual focus and minimization attempts on Windows and macOS;
  - cases without a test on the server listed in the AUDIO_UX matrix.

<a id="start-demo-i-nadpisywanie--demo-01-2026-10-15"></a>
## Start demo and overwrite — DEMO-01, 2026-10-15

- `dotnet test --filter DemoSessionTests` 4/4 (PostgreSQL):
  - DM01: demo Shadow from the room (CPU = Free Peoples, base game), batch list with opponent ‘CPU’
    and name ‘Demo · Shadow vs CPU’; room further waiting with the same version;
  - DM02: second demo removes first (match, receivers, events, demo line) and the usual batch and
    demo of another account remain;
  - DM03: foreign (404), closed (404) and the room started (409 `MatchAlreadyStarted`) without
    effect; two simultaneous starts leave one demo.
- Regression: lobby, public rooms, removal, test sessions, menu cycle, party list, budget base,
  architecture and implementation bases 56/57. `MigrationRecoveryTests` He was expecting a migration
  list. 1–8 ; after adding the version 9 migration tests pass 12/12.
- `tests/packages/test_package_deps.py` 6/6 (Contracts 0.5.0, content
  1.0.7).

<a id="prosty-bot-podstawki--demo-02-2026-10-15"></a>
## Simple bot base game — DEMO-02, 2026-10-15

- `dotnet test --filter DemoBotTests|ArchitectureTests` 12/12:
  - DM04: bot plays only its own decision, the same condition gives the same move, and the engine
    takes motion; after the game's end the bot has no movement;
  - DM05: "bot vs bot" since the start of base game for 1–10 grains. Each batch ends (winner Shadow
    by failure in turn 10–22, 190–560 steps) without decision unanswered.
  - Decisions encountered: action, Fellowship phase, allocation and transmission Hunt, injuries,
    route of disclosure, Guide, separation, card combat, reroll, continuation, retreat, entry,
    defence Stronghold, siege, losses, cards recoil, excess figures, choice of effect, reaction,
    events of armies and characters.
- The first runs stopped for recruitment with card, elections in groups and march routes with cards.
  Bot now supports them according to DEMO-MODE.
- One-time review of 50 consecutive grains (11–60) in the temporary test: 50/50 batch to the end.
  The test was not left in the repository.

<a id="ruchy-cpu-na-serwerze--demo-03-2026-10-15"></a>
## CPU moves on the server — DEMO-03, 2026-10-15

- `dotnet test --filter DemoRunnerTests|DemoSessionTests` 8/8
  (PostgreSQL, pause CPU 0 ms):
  - DM06 : Player Shadow — CPU performs phase Fellowship alone, and the player gets an allowance
    Hunt Account Receipts `demo-cpu` step ContinueFellowship pages Free Peoples They're in the
    journal. action ( DM07 );
  - the player Free Peoples and CPU on a change by the real command HTTP until the second turn;
  - DM08: demo created without waking (as after reboot) — read the batch resumes CPU. Reading the
    regular batch does not give any command;
  - removing demos during the CPU movement (pause 150 ms) does not leave the receivers, and the new
    demo plays normally.
- Full `dotnet test --configuration Release` 3931/3931 (6 min 18 s).

<a id="przycisk-demo-w-kliencie--demo-04-2026-10-15"></a>
## Demo button in client — DEMO-04, 2026-10-15

Client `587c6ae` (Contracts 0.5.0, content 1.0.7, test backend `489e5be31221` from local channel), new base `wotr_demo_client_test`:

- full EditMode 786/786;
- PlayMode with server:
  - the new `DemoRoomNetworkTests` 1/1 (DM09): room Shadow → "Play demo vs CPU" opens board. CPU
    plays the Fellowship phase himself, and the player gets the Hunt allocation. The list of the
    parties has "Demo · Shadow vs CPU" with the opponent "CPU" and the room is still waiting. The
    second demo from the same room replaces the first;
  - regression: `WaitingRoomPollTests` + `MainMenuLayoutTests` 2/2, `MainMenuNetworkTests` 3/3,
    `MatchDeletionNetworkTests` 1/1.
- Contracts 0.5.0 packages, content 1.0.7 and backend `489e5be31221` are only in the local channel.
  The client's push requires their publication as GitHub releases, which waits for the user's
  consent.

<a id="odbiór-trybu-demo--demo-05-2026-10-15"></a>
## acceptance demo mode — DEMO-05, 2026-10-15

- New `DemoAcceptanceTests` 2/2 (PostgreSQL, pause CPU 0 ms): the whole demo party through real HTTP
  routes. The player moves the same bot in replacement of the human and CPU responds to the server:
  - player Free Peoples : 125 your own moves, end in turn 12 ;
  - player Shadow : 166 your own moves, end in turn 15.

Both parties end up winning Shadow by corruption, and the finished demo is on the batch list with the Filter Finished.
- Previous aggregate results: DEMO-01–DEMO-04 (above), full server 3931/3931, full server EditMode
  786/786, PlayMode demo and menu 7/7.
- Matrix DM01–DM10: `docs/server/DEMO-MODE.md`.

<a id="hosting-na-omarchy--dist-04-2026-10-15"></a>
## Hosting au Omarchy — DIST-04, 2026-10-15

- Stage A, isolated design `wotr-a` (removed after the test): compose config, healthy
  database/server/web with read_only and cap_drop; page with CSP/HSTS , API , Alien Host 400 ,
  WebSocket to the server; 22 login with `CF-Connecting-IP` And a variable counterfeit
  `X-Forwarded-For` → 20×401 + 2×429 , Address B → 401 ; downloads with test certificate: HTTP/2 ,
  Range 206 , 404 outside `/releases/` , POST 403 , none TLS for other names and themselves IP , 8,7
  MB/s on connection, fifth connection 429 re-download 200 MB compatible SHA-256 .
- Stages B-C, production `/srv/wotr` (checkout `daa37b3`): a stack without ports on the host, route
  `wotr.example.test` in the portfolio tunnel; by Cloudflare page, API without challenges (also UA
  Unity and empty UA), WebSocket, http→301, forged `CF-Connecting-IP` rejected on the edge (403). A
  publisher account with a real client, a test account `dist04-probe`, a table, a lot, a probe
  movement → a notification WSS and an immediate refresh of the publisher's client. A mobile
  publisher (other than a home address): a 200 login and a readout of the batch.
- Stage D: token Cloudflare verified by API without display; DDNS created the A record; certificate
  Let's Encrypt (staging, then production, valid for 2027-01-13 ). `ufw-docker` blocked public
  traffic into the container — rule `ufw route allow … port 8443` ; port 443 blocks the operator
  (also at the low router dam), so WAN 50443 Workflow Public Endpoints 38760255550 runner GitHub :
  TLS , HEAD , Range 206 , 404 outside `/releases/` Discontinuation and resumption with SHA-256 OK ,
  429 for the fifth call.
- Stage E: `certbot renew --dry-run` PASS; backup → restore to a separate base with identical game
  tables (accounts, invitations, sessions, tables, party with pending decision, retrieves, events,
  readings, migrations); response time `/health` with median runner 328 → 316 ms at four downloads;
  restart of the Omars — all services of WoTR, the prairie and portfolio returned alone, the data
  preserved, Public endpoints 38762676652 PASS; `nginx -t` in both containers.

<a id="pasek-postępu-po-buildzie-macos--poprawka-audio-09-2026-10-15"></a>
## MacOS build progress bar — AUDIO-09 amendment, 2026-10-15

- Application: The editor's log: "Build succeded!", signed application with Game Mode `false` keys,
  main idle thread. Cause: `MacOSCodeSigning.CodeSignAppBundle`, called from the callback
  `MacGameModeBuildPolicy`, opens the "Code signing app" bar, which closes nothing. Correction
  (client `43fe57b`): `EditorUtility.ClearProgressBar()` in `finally` after signature.
- Full EditMode client 848/848 (with `MacGameModeBuildPolicyTests`, which signs the `.app` package).
- No automatic check was made: the progress bar can only be seen in the interactive Editor. It
  requires user testing (WoTR → Build → Build macOS).

<a id="niższe-szeleszczące-efekty--audio-10-2026-10-15"></a>
## Lower, rustling effects — AUDIO-10, 2026-10-15

- `python graphicalassets/audio/generate.py` (generator version 4):  25 clips in length limits,
  volume consistent with targets, true peak ≤  − 1 dBTP, edges 0,  DC  <  2 · 10 ⁻ ⁴.
- Measurement before (files with `HEAD`) and after the same script: tonality (energy in narrow peaks
  above smoothed spectrum) and energy above 4 kHz. Effects:
  - UiClick 99,6% → 12,6%, ActionAccepted 100% → 44,2%;
  - DiceRoll 60,1% → 8,9%, CardMove 32,6% → 13,3%;
  - CardInspect: Energy > 4 kHz 51,8% → 0,2%;
  - Each effect < 1% energy > 4 kHz.

  Table: AUDIO_UX (client `af63bd8`).
- Full EditMode client 848/848, with `AudioCatalogTests`.
- Open: user listening on the listening page (3 version,
  https://claude.ai/artifact/6DXsjjECtGZG5BRF7dGGQZ).

<a id="okno-regionu-znika-po-odjechaniu-kursorem--ui-189-2026-10-15"></a>
## The region window disappears after passing the cursor — UI-189, 2026-10-15

Client `cbaf5ac`:
- the new PlayMode `RegionPanelLeaveTests` 1/1: window stays above the region and above itself,
  survives the fast passage elsewhere (0,1 s), after 0,12 s beyond both is still there, and after
  0,52 s is closed;
- region panel regression:
  - `RegionNameOnlyTests`, `RegionInspectionTests`, `BoardInteractionTests` together with the new
    17/17 test;
  - Network `ArmyContext` 6/6, `MapClickMove` 2/2, `Muster` 4/4, `TravelGesture` 3/3, `T22Board` 5/5
    and `EffectsNetwork` 5/5;
  - outlines of the map (network, after one class, because the common package was placed on
    `TooManyRequests`) 9/9 and `CompactBoardLayoutTests` 1/1;
  - local package 19 classes: no new errors except those described below.
- Full EditMode 848/848.
- Improved Tests:
  - `CompactBoardLayoutTests` expected an empty 230–281 px window, and since UI-188 is a small
    window of the name itself (179 px; now the range of 100–240 px). This regression with UI-188 —
    this test was not then started;
  - `ContextHoverTests` checked that the window stays after moving to the phase bar. Now checks the
    0,3 s closing.
- Known earlier errors (present already on `1893e41`/UI-184, before UI-185–UI-189; out of range):
  - `ContextHoverTests`: figure in the window moves about 0,5–0,6 px after clicking, which
    interrupts the test before it reaches the step UI-189;
  - `EffectsLayoutTests`: The effect window in one layout protrudes about 16 px.
- Esc in the test model does not close the region window (also sent to the window). Esc support in
  `RegionHover` is unchanged; it has not been further studied.

<a id="usuwanie-gracza-i-czat-poczekalni--menu-24-2026-10-15"></a>
## Remove player and chat waiting room — MENU-24, 2026-10-15

- `dotnet test --filter RoomRemovalTests|RoomChatTests` 5/5:
  - WR01: host removes the guest (free, free, +1 version, zero readiness). Guest gets 410
    `RemovedFromRoom`, and public connection and new code give 409. Guest (409 `HostRequired`),
    foreign (404), empty room (`SeatEmpty`) and the party started (`MatchAlreadyStarted`) do not
    change the room;
  - WR02: the order and pages of the two players' messages, `after` returns only new ones, and
    `after` larger than `Latest` — all. Rejected: empty, space alone, 201 characters, new line,
    control character (400) and alien (404). Tags become text, becomes 50 last, and the new chat
    instance is empty (as after reboot).
- `python3 -m unittest tests/packages/test_package_deps.py` (Contracts 0.6.0, content 1.0.8) and
  full `dotnet test --configuration Release` 3938/3938.

<a id="nowa-poczekalnia-i-czat--menu-25-menu-26-2026-10-15"></a>
## New waiting room and chat room — MENU-25, MENU-26, 2026-10-15

Client `f830023` (Contracts 0.6.0, content 1.0.8, test backend `0d054ce78129`), fresh base `wotr_room_client_test`:

- full EditMode 848/848 (`WaitingRoomTests`: guest sees Start, invitation and Remove grey, not
  hidden);
- PlayMode:
  - `MainMenuLayoutTests` + `WaitingRoomPollTests` 2/2 (WR03). In four layouts, the waiting window
    is the size of the menu window, the controls are located in the window and column of action, two
    equal seats, ‘Back’, and the tags in the chat are text;
  - `WaitingRoomRemovalNetworkTests` 1/1 (WR04): the guest sees Start and Remove grey, the host
    removes the guest, the guest returns to the message menu and does not re-enclose (409);
  - `RoomChatNetworkTests` 1/1 (WR05): empty line does not send anything, Send and Enter (in focus
    field) send back and forth, tags become text;
  - regression: `MainMenuNetworkTests` 3/3, `DemoRoomNetworkTests` 1/1, `ExpansionLobbyNetworkTests`
    1/1.
- On the way, corrected:
  - menu status overwritten by refresh of the list (`MainMenuBrowser.Notice`);
  - Enter caught on the root of the chat panel;
  - Enter test without focus did not reach the field;
  - expected size in tests (compared to tolerance, same title size as menu).

<a id="panele-lewej-listwy-przy-swojej-zakładce--ui-190-2026-10-15"></a>
## Panels of left strip at its tab — UI-190, 2026-10-15

Client `f95ed8f`: `EdgeBoardLayoutTests` 1/1 with new Assertion (both sides, four layouts: Politics, Effects and Characters start at the height of their bookmark or end with 8 under the map) and full EditMode 848/848.

<a id="puste-poczekalnie-znikają--menu-27-2026-10-15"></a>
## Empty waiting rooms disappear — MENU-27, 2026-10-15

- `dotnet test --filter
  EmptyRoomSweeperTests|Migration|RoomRemovalTests|RoomChatTests|LobbyTests|PublicRoom` 30/30.
  `EmptyRoomSweeperTests` has a separate base for each test, with the time of void 1 s and a review
  of what 0,2 s:
  - the room read what 0,3 s by 2,4 s stays; then disappears from the base, from the list of rooms
    and from reading (404);
  - The room closed by the host disappears, and the room with the party started remains.
- `MigrationRecoveryTests` expects the 1–10 version.
- Full `dotnet test --configuration Release` 3940/3940 (the cleaning machine with default 30 s on a
  common test database did not prevent other tests).

<a id="adresy-hostów-poza-git--menu-28-2026-10-15"></a>
## Hosts outside git addresses — MENU-28, 2026-10-15

- Full EditMode client 847/847 on the clon without `.env` (as in CI). New `ServerConfigurationTests`
  check:
  - keys from `.env` (comments, `export`, quotes);
  - priority of the environment;
  - no file (only the default local Editor server);
  - build file requiring server address and downloads;
  - trust updates from the origin;
  - `Use` for testing.
- `git grep vibestudios` in code, configuration and testing files of both repositories (except for
  documentation and page content): none.
- No build in Unity Editor from `.env` was checked (it is done by the user). Workflow installers
  require variable `WOTR_SERVER_URL` and `WOTR_DOWNLOADS_URL` repository that sets the repo owner.

<a id="konta-z-e-mailem--auth-02-2026-10-15"></a>
## Email accounts — AUTH-02, 2026-10-15

Environment: Omarchy (Linux) without local .NET; `dotnet` from `mcr.microsoft.com/dotnet/sdk:10.0.401` image, PostgreSQL 17.11 in container (`postgres:17.11-bookworm`) on `127.0.0.1:55432`, `WOTR_TEST_DB=…;Database=wotr_test`.

- `dotnet test --configuration Release` : 3962/3965 ; `MigrationRecoveryTests` expected a migration
  list without new account migration (improved; after a rebase on MENU-24 ed `010_room_seen` account
  migration has a number `011` ) and two restart tests ( `ActionLogRestartTests` ,
  `HuntActivityPersistenceTests` ) did not find `initdb` in image SDK These three again in the
  painting PostgreSQL 17.11 with the same SDK and `WOTR_PG_BIN=/usr/lib/postgresql/17/bin` : 3/3.
  Total 3965/3965.
- `AccountEmailTests` ( 10 ) and `EmailSenderTests` ( 10 ): 20/20 Three times in a row.
  `IEmailSender` , `TestClock` with a random date for the test (day limits count only their own
  links), actual PostgreSQL : link confirming once and 7 days, reset only to confirmed address,
  equal response 202 , new password cancels all sessions, link reset once, 60 minutes and cancelled
  newer, limits accounts/day/address IP , `--set-email` , in the database only SHA-256 Tokens, request
  to Resend and refuse to configure wrong.
- The first run revealed the daily limit counter without the upper limit of time (counted the links
  from future dates of other tests); the window is now `(now−1 d, now]`.
- Client Seeder (`tests/Wotr.Scenarios`) on separate base `seeder_test`: `AccountA`/`AccountB`
  accounts and password instead of invitation codes.
- Local stack: nginx games ( `infra/nginx/game.conf` , `infra/site` ) before the Development server
  with the supplier `log` Parties `/account/register|verify|forgot|reset` 200 ed CSP ; registration
  200 , busy name and address 409 ; link from the log confirms ( 204 - I'm sorry. 400 ; reset 202
  (also unknown account), for short password 400 new password 204 , old password 401 , new e-mail
  200 Chromium headless ( `--dump-dom` Script of pages works under CSP , supports the wrong and
  missing token. Forms were not sent by clicking on the browser.
- `docker compose config` production: mail variables, `Wotr__PublicUrl` and secret `resend_api_key`;
  `nginx -t` for new location `/account/`.
- Correction 2026-10-16 after the attempt at production: request for reset just after the
  confirmation of the address was sent to 2-minute break after confirmation link (log “An account
  requested for emails too often”). The break now counts separately for each type of message. New
  test `AConfirmationLinkJustSentDoesNotHoldBackAReset`; post office 21/21 twice, full `dotnet test`
  3973/3973 (in image PostgreSQL 17.11).
- Production with Resend (2026-10-16): the user configured the domain and key and then checked with
  a real password registration and reset box; both work. After moving the story full `dotnet test`
  on a fresh base 4005/4005 (7 tests exceeded full time and passed separately), including
  `AccountEmailTests` and `EmailSenderTests`.

<a id="bez-obrysu-fokusu--ui-191-2026-10-16"></a>
## No focus outline — UI-191, 2026-10-16

Client `06f1124`, PlayMode on the test bench with the server attached (port 5098, fresh base `wotr_ui191_test`):

- `NoFocusOutlineTests|InterfaceSettingsTests` 3/3. New test: when you click, the key on the button,
  writing in `TextField` and the seal from the code by the keyboard has no `keyboard-focus` or
  `.focus-indicator` element; focus still goes to the controls.
- `HandCardLayoutTests` 2/2: card with focus on the key without a contour; discard with one click
  unchanged.
- `RoomChatNetworkTests` 1/1: Enter in the chat field continues to send.
- Full EditMode 847/847.

<a id="złoty-medalion-dostępnej-zakładki--ui-193-2026-10-16"></a>
## Gold medallion available on the tab — UI-193, 2026-10-16

Client `b6e5911`:

- `python3 graphicalassets/ui/render.py --only rail`: new variants `edge-tab-available` and
  `edge-tab-available-hover` (188×132 px), base variants without byte changes;
  `edge-tab-available.png` was viewed.
- PlayMode
  `BoardInteractionTests.UI136|EdgeBoardLayoutTests|AutoOpenPolicyTests|PoliticalPanelTests` 10/10.
  `UI136ChosenDieLightsTheControlsItCanUse` checks that the Politics tab does not have rectangular
  halo and has `edge-tab-available` graphics, the Characters tab without option has a regular
  medallion, and the Fellowship tab retains the halo.
- Full EditMode 847/847 (with `UiArtKitTests` for new variants).

<a id="figury-na-mapie-jeszcze-trochę-większe--ui-192-2026-10-16"></a>
## figure on the map even more — UI-192, 2026-10-16

Client `b135654`, PlayMode on the test bench with the server attached:

- `MapArmyArtworkTests|MapVisibilityTests|BoardChromeLayoutTests|FigureMotionTests` 9/10 He was just
  passing.
  `FigureMotionTests.ReducedMotionReconnectAndRevisionGapsShowResultsWithoutReplayingTravel` : The
  test aid had a point of foot ( 22 , 40 ) before UI-185 Same test without UI-192 He's been making a
  run for it. 13,4 pt, so it was red from UI-185 The point of the feet is now constant
  `MapFigureMotion.FlightFeet` reads the test; after the amendment
  `FigureMotionTests|MapArmyArtworkTests` 5/5.
- `MapClickMoveNetworkTests|MovementNetworkTests|MusterNetworkTests|FigureMotionNetworkTests` 9/10.
  `MovementNetworkTests` was launched (revision of the 2 party instead of 0, before the client
  clicked something), because in the same course the batch from the fixture used earlier network
  tests. Started on a fresh base itself passes 1/1, both with and without UI-192.
- `MapArmyArtworkTests` checks the 60 pt figure and all parts inside the tag in the 7 gallery of the
  tags on the row at 100% and 125%.
- Full EditMode 847/847.

<a id="characters-świeci-tylko-przy-akcji-w-zakładce--ui-196-2026-10-16"></a>
## Characters only shines at action on the tab — UI-196, 2026-10-16

Client `3f90cfa`:

- EditMode `DieOptionsTests.ACharacterDieLightsTheCharactersTabOnlyForWhatTheTabOffers`:
  - die Character with the Companion movement itself or servants does not highlight the bookmark;
  - movement paid for by the character's ability to highlight it, but only if the option is
    available;
  - the ability of the character to accept die highlight the tab.
- PlayMode `BoardInteractionTests.UI136` 1/1.
- Full EditMode 848/848.

<a id="czat-w-pokoju-bez-gościa--menu-29-2026-10-16"></a>
## Chat in a room without a visitor — MENU-29, 2026-10-16

- `dotnet test --filter RoomChatTests` 4/4. New test `TheHostWritesInARoomNobodyHasJoined`: room
  established by API, host message before joining the guest passes.
- `curl https://wotr.example.test/health`: public server corresponds to `protocolVersion 2`, but is
  not implemented with MENU-24, so there is no chat. `https://localhost:7443` (default editor
  server) did not work at the time of the check. On the server without chat, the client gets 404 and
  showed “Message not sentence Try again.”
- Client `467ebc5`: EditMode `WaitingRoomTests.AFailedChatMessageSaysWhy` (400/404/429/other), full
  EditMode 849/849, PlayMode `RoomChatNetworkTests` 1/1.

<a id="karta-zagrywana-jednym-kliknięciem--ui-194-2026-10-16"></a>
## card played with one click — UI-194 , 2026-10-16

Client `1e34795`, PlayMode on the test bench with the server attached (new database `wotr_ui194_test`):

- `CardInteractionTests|HandDrawerTests|HandDiscardDrawerTests|PressFeedbackClickTests|VictoryPanelNetworkTests`
  16/16. `ClickInspectionPreservesSelectionAndPlayNeedsTheChosenDie`:
  - without dice, click does not send command;
  - with dice pressed card has `pressed`, before releasing 0 commands, after releasing exactly one
    command with card and dice.
- `CardNetworkTests|CardGestureNetworkTests|HandCardLayoutTests` 4/4: play the server with one
  click, without previewing for one payment method.
- `MultiUseDiceNetworkTests|BattleWindowNetworkTests` 5/5: two ways of paying (Will) still open
  preview with two Play buttons.
- Full EditMode 849/849 (`UiArtKitTests` without seal).
- Long games `T23FullGameNetworkTests` and `T36DesktopGameTests` that play card by
  `CardGestures.Play` (now one click) were not started. The preview
  `graphicalassets/ui/previews/cards.png` was not rendered again; it still shows the old seal.

<a id="nowy-początek-bitwy--audio-11-2026-10-16"></a>
## New Start battle — AUDIO-11, 2026-10-16

Client `db20f82`:

- `python3 graphicalassets/audio/generate.py` (5 version): only `battle-started-1/2.wav` has
  changed, both after 0,88 s, −17 LUFS, true peak −1,0 and −2,6 dBTP, 0 edges.
- Band measurement: the centre of gravity of the spectrum approx. 160 Hz (horn had 357 Hz), 15%
  energy in 150–300 Hz, nothing above 2 kHz.
- PlayMode `SoundModuleTests|BattleSoundNetworkTests` 3/3; full EditMode 849/849
  (`AudioCatalogTests`).
- The listening page updated (4 version). The user's listening has not yet been there, so the task
  is waiting for him.

<a id="osady-jako-kamienie-szlachetne--ui-195-2026-10-16"></a>
## settlement as precious stones — UI-195, 2026-10-16

Client `df12e24`:

- EditMode `MapSettlementGemsTests` 4/4:
  - the grinding depends on the type of settlement;
  - colour depends on the control side (safir, ruby, quartz);
  - the planes facing towards the light are brighter;
  - the outline is scaled around the anchor;
  - The stone in the frame is located under the order of the army 12 called over the anchor.
- Full EditMode 853/853.
- PlayMode
  `MapRegionNamesTests|RegionNameOnlyTests|MapVisibilityTests|MapArmyArtworkTests|MapViewportEdgeTests`
  5/5, `MapClickMoveNetworkTests|MusterNetworkTests` 6/6. The first version also expanded the region
  name zone under figure UI-192 and then `MapRegionNamesTests` showed approximately 8 names and
  required more than 8. Without this change (UI-192 itself) the test passed, so the extension was
  revoked.
- A `ui186-names-zoomed-1920-1.25.png` drop was observed: rubin Orthanku and sapphire Helm's Deep in
  gold frame, emerald sand Edoras, brown towns, trillion Fords of Isen.

<a id="herby-nacji-na-torze-polityki-poświata--ui-197-2026-10-16"></a>
## The Herbs of Nation on the Political Track, the Light — UI-197, 2026-10-16

Client `3a2b7c9`:

- PlayMode
  `PoliticalPanelTests|LeftRailLayoutTests|ReinforcementsLayoutTests|EdgeBoardLayoutTests|PoliticalGestureTests`
  9/9 (last run of `LeftRailLayoutTests|PoliticalPanelTests` 5/5). Tests check:
  - the coat of arms is a medallion high school at the appropriate station;
  - the light is centred on the medallion and does not take over the indicator;
  - the active and passive nation differ in class and appearance;
  - the title and the switch ‘Opponent’ in the heading shall not overlap;
  - in Reserve the glow stands under the coat of arms of the poem and within the token.
- `LeftRailLayoutTests` compared the width of the axis legend and the name column with the 0,5
  tolerance: The width printing showed that in the window scaled to the screen edges attract to
  pixels: the axis steps have the 35,42 and 36,13 to change, and the row and axis have the same
  width (239,43 to change). Assertion now compares the beginning and width of the column with the
  tolerance of one pixel.
- `MusterNetworkTests|T22InformationTests` 7/7; full EditMode 853/853.
- Inspected drop `ui144-politics-actions.png`.

<a id="exit-match-bez-delete-game-pusty-pokój-usuwany-od-razu--menu-32-2026-10-16"></a>
## "Exit match" without "Delete game", an empty room removed immediately — MENU-32, 2026-10-16

- Server: `dotnet test --filter
  EmptyRoomSweeperTests|RoomRemovalTests|RoomChatTests|LobbyTests|DemoSessionTests` 22/22. New
  `TheHostLeavingAnEmptyRoomDeletesItAtOnce` (default 30 s cleaning man, separate base):
  - After `leave` host from an empty row room there is no immediately, and the room reading gives
    404;
  - The room with the guest is closed, not removed.
- Client `54fc8e7`:
  - PlayMode `BoardMenuLayoutTests|BoardMenuNetworkTests` 12/12 (menu without ‘Delete game’, Quit
    question in four layouts);
  - `MatchDeletionNetworkTests|DemoRoomNetworkTests` 2/2: menu has an "Exit match" and there is no
    "Delete game"; the game removed by API gives the "Game deleted" to both players and their menus
    do not show it;
  - full EditMode 853/853.
- Client tests were going on a high-tipped testback before this server change, so immediate removal
  of the room only checks the server test.

<a id="host-a-game-w-układzie-menu--menu-30-2026-10-16"></a>
## "Host a game" in menu layout — MENU-30, 2026-10-16

Client `1152aac`:

- PlayMode `MainMenuLayoutTests` 1/1, in four systems (1366×768 and 1920×1080, the scale 1 and
  1,25):
  - Name field, both card pages, segments, explanation, ‘Back’ and ‘Create game’ shall be located in
    a window and shall not overlap;
  - ‘Back’ stands on the left, ‘Create game’ on the right;
  - the page has no switch;
  - click card selects the page.
- PlayMode `MainMenuNetworkTests` 3/3: private game through the segment "Private", creation and
  connection.
- EditMode `MainMenuTests`: by default, public game Free Peoples, name validation, site selection
  and visibility; full EditMode 853/853.

<a id="demo-vs-cpu-z-menu-głównego--menu-31-2026-10-16"></a>
## Demo vs CPU from the main menu — MENU-31, 2026-10-16

Client `ad93351`, PlayMode on the test bench (new base):

- `MainMenuDemoNetworkTests` (in the package with `MainMenuNetworkTests`, together with 4/4):
  - "Demo vs CPU", then click `demo-shadow` to open the demo on the Shadow side;
  - "Exit match", then `demo-fp` opens the demo on the Free Peoples side, which replaces the first;
  - the ‘Demo · Free Peoples vs CPU’ party with the opponent ‘CPU’;
  - There's no room left in the state of Waiting.
- `MainMenuLayoutTests` 1/1 in four systems:
  - ‘Demo vs CPU’, ‘Sign out’ and ‘Quit game’ are in the column and the latter two are in one row;
  - card demo and "Back" pages are located in the window.

The first run detected that the "Quit game" protrudes 43 pt outside the column; the second showed that USS does not support the `+` selector that blocked the menu sheet. Both improved.
- `DemoRoomNetworkTests` 1/1; full EditMode 853/853.
- The test backend was before MENU-32, so the room after the demo was started was closed, not
  removed. The test just checks that it is not waiting.

<a id="znaczniki-przy-brzegu-widoku--ui-199-2026-10-16"></a>
## Tags at the edge of the view — UI-199, 2026-10-16

Client `f96c81e`, PlayMode on the test bench:

- New `MapMarkerEdgeTests`: Minas Tirith 20 point under the upper bank. The army is shown, its
  location relative to the region point is unchanged and the top edge of the map cuts it off. After
  moving further than the range (160 pt + 200) is hidden.
  - The first run showed a jump on 126: the government bypassed the HUD panels (`Occlusions`) and
    the region's open panel. The government now only gives up to other forces, and the region's
    names still hide under HUD.
- `MapMarkerEdgeTests|MapVisibilityTests|MapArmyArtworkTests|MapRegionNamesTests|MordorLayoutTests|BoardInteractionTests|RegionPanelLeaveTests|RegionInspectionTests|ContextHoverTests`
  32/33. He has only fallen known from `9ad8cff` `ContextHoverTests` (shifting to 0,5 px).
- `MapClickMoveNetworkTests|MusterNetworkTests|FellowshipMotionNetworkTests|ArmyContextNetworkTests|MovementNetworkTests`
  14/14.
- Full EditMode 853/853, before last change (removement of HUD bypass, map logic itself).

<a id="tytuły-kart-i-imiona-postaci-w-tekstach-zdarzeń--ui-198-2026-10-16"></a>
## Titles cards and character names in event texts — UI-198, 2026-10-16

- New `EventTextTests` 2/2:
  - the selection gives the "Drew {title}." only to the selecter, and the title or identifier do not
    go to the events of the opponent;
  - `CardCatalog.Title` and `CharacterCatalog.Title` return titles from all cards and base game
    characters, and unknown identifier unchanged.
- Full `dotnet test` 3937/3944. Seven errors resulted from a change of text:
  - five preservation tests sought the event of `EventPlayed` or `CardDrawn` after identifier in
    text; they are now looking for the title, and the selection test also checks that the title does
    not leak to the opponent;
  - Two cases of `BaseCorpusTests` (`guide`, `alone`) compare the reviewed state shortcut. The new
    step of `CheckAndRestoreNamedTexts` checks the title in each new event of card and Guide, and
    then restores identifier, so the reviewed shortcuts remain.
- After the `BaseCorpusTests|*PersistenceTests|EventTextTests` 320/320. Amendments

<a id="węglowoszary-design-zamiast-granatu--ui-200-2026-10-16"></a>
## Coal-gray design instead of grenade — UI-200, 2026-10-16

Client `1f1dbba`:

- `python3 graphicalassets/ui/render.py`, or full directory after changing pallets: over-schedule 43
  graphics (frames, buttons, dice enamel and tabs, bar plate and console); `frames/panel` frame has
  512×512 px. `frames/panel.png` is viewed: coal with grain, after lightening no longer black.
- Color change control script USS : 84 navy colors turned into coal with the same brightness. The
  second run finds no navy color. Page colors Free Peoples ( `.fp` , track line), greens of legal
  target and red lake have been. Map legend (C#) repainted.
- PlayMode
  `LeftRailLayoutTests|EdgeBoardLayoutTests|BoardChromeLayoutTests|TurnConsoleLayoutTests|MainMenuLayoutTests|PoliticalPanelTests|ReinforcementsLayoutTests|BoardMenuLayoutTests`
  19/19; full EditMode 853/853 (`UiArtKitTests` with 256 pt frame).
- The main menu (1920×1080) and console (1440×900) were screened.

<a id="figury-w-panelach-na-poświacie--ui-201-2026-10-16"></a>
## figure in the panel on the ground — UI-201, 2026-10-16

Client `a86bda8`:

- PlayMode
  `MapVisibilityTests|PressFeedbackClickTests|RegionInspectionTests|FellowshipLayoutTests|CharacterLayoutTests|BattleLayoutTests|ContextHoverTests`
  21/22. has only been known to `9ad8cff` `ContextHoverTests` (shifting to 0,5 px). Tests check:
  - figure in the region panel has a glow in its own tile that does not take over the indicator;
  - loss in battle has a glow and no frame in rest.
- `BattleWindowNetworkTests|ArmyContextNetworkTests` 9/9; full EditMode 853/853.
- Sightseeing `ui114-pressed-loss-figure.png`: figure dropout on the ground, without frames.

<a id="prostszy-górny-pasek--ui-202-2026-10-16"></a>
## Simpler upper bar — UI-202, 2026-10-16

Client `69a9367`:

- `render.py --only frames`: `frames/topbar-frame` (384×88 px) has all the gold lines and a straight
  bar at the ends; the graphics were watched.
- PlayMode `TopBarLayoutTests|BoardChromeLayoutTests|BoardMenuLayoutTests` 11/11; full EditMode
  853/853.

<a id="ciemniejszy-węgiel-i-dopracowane-okna--ui-203-2026-10-16"></a>
## Darker coal and refined windows — UI-203, 2026-10-16

Client `e1325ce`:

- `render.py` (full directory; first run has broken the limit of 15 min, so it was repeated without
  limit). `frames/panel.png` (deep coal, soft discolorations, shadow at the frame) and
  `frames/panel-header.png` (closed pass with bevelled corners) has been watched.
- The first version was placed lower bar above the map. Tests showed regression:
  `ArmyContextNetworkTests` waited for confirmation button, and `ReinforcementsLayoutTests` showed a
  shift by 0,2 entitled Strip returned to its place, without its own CD and line.
- PlayMode: first package of the 12/13 layout (before the back of the bar), then
  `ReinforcementsLayoutTests|TurnConsoleLayoutTests|BoardChromeLayoutTests|MainMenuLayoutTests` 6/6
  and `ArmyContextNetworkTests` 6/6. Full EditMode 853/853 (`UiArtKitTests`: the header now has
  transparent corners).

<a id="wyraźny-sygnał-przydziału-do-polowania--ui-204-2026-10-16"></a>
## Clear allocation signal for Hunt — UI-204, 2026-10-16

Client `0811060`:

- PlayMode `HuntNetworkTests` 7/7. `RequiredAllocation` checks:
  - the pulse class of the dice panel changes after the 0,9 s when candidates are present;
  - before the ready sign there is no glow;
  - after the readiness of the glow is visible and its pulse class changes over time;
  - tooltip talks about a few signs.
- `TurnConsoleLayoutTests|DicePanelLayoutTests|HuntLayoutTests` 7/7; full EditMode 853/853.

<a id="obserwatorzy-pokoju--obs-01-2026-10-16"></a>
## Room Observers — OBS-01, 2026-10-16

Server (PostgreSQL 17, `LC_ALL=en_US.UTF-8`):

- `dotnet test --filter RoomObserverTests` 10/10:
  - OB01 : 18 simultaneous entrances to the audience gives exactly 16 places and two `ObserversFull`
    ; re-enters no line, the player becomes a player. The code lets the viewer in after taking
    places and after starting, but not after the end of the game ( `MatchFinished` );
  - OB02: `SeatTaken`, `ObserversFull`, `RolesLocked`, `PlayerRequired` do not change versions,
    seats or audiences. The cast change will zero readiness and the viewer's input will not. Host in
    the audience configures, starts the game and removes the viewer by name (410, no return). The
    host exit closes the room and removes it when no one is present;
  - OB07: lease 2 s (originally 1 s; under full run load it was not enough, amendment in REPLAY-01).
    The reader also stays at the second `RoomStore` (restart), silent loses space. Two simultaneous
    entries of one account give one row. The public exit releases the seat immediately. The cleaner
    leaves the waiting room with an important lease and removes it after expiry (a separate base);
  - Chat with the role and name of the viewer, public list with and without meters and without
    finished batch, route HTTP `join?role`, `role`, `presence`, `remove` with the name.
- Lobby, rooms, chat, cleaner, migration and demo
  (`RoomObserverTests|RoomRemovalTests|MenuLifecycleTests|PublicRoomTests|LobbyTests|RoomChatTests|EmptyRoomSweeperTests|MigrationRecoveryTests|ExpansionLobbyTests|Demo`):
  68/68.
- Full `dotnet test --configuration Release`: 3954/3954.
- The client was not tested; Contracts 0.8.0 will pin OBS-04.

<a id="odczyty-obserwatora--obs-02-2026-10-16"></a>
## Observer readings — OBS-02, 2026-10-16

Server (PostgreSQL 17):

- `dotnet test --filter MatchObserverTests` 6/6 (OB04):
  - the viewer sees both sides. The hand, story and summary of the players are just like the player
    of this page, and the hands of the pages are disconnected and blank. Options, availability and
    preferences are empty, `Decision` has no `Id` or `Choices`, and the player-owner of the decision
    has token;
  - the action log page is identical (JSON) to the player page of this page, for both parties;
  - Both parties and aliens get 404 on each `/api/observe` route and the player will not open the
    viewer's socket. Unknown perspective gives 400;
  - the viewer on the player's routes (read, command, preview, receive, `seen`, activity, log,
    removal) gets 404, and the player's view (JSON) and the revision does not change;
  - lease 2 s: open viewer's socket gets `privateVersion = 0` and closes after expiration, and reads
    gives 404. Views after `leave` also gets 404.
- Full `dotnet test --configuration Release`: 3960/3960.

<a id="nagrywanie-kroków-partii--replay-01-2026-10-16"></a>
## Recording the steps of the party — REPLAY-01, 2026-10-16

Server (PostgreSQL 17):

- `dotnet test --filter MatchRecordingTests` 5/5:
  - RP01: 30 bot moves demo for both sides by the real `MatchStore`. The frame count is 1 + number
    of public changes, steps 0..n without gaps, revisions grow. Each new frame is equal to the views
    of `Projection.Observe` both sides of the saved state (JSON), with the hand of Shadow. The frames
    have no history, and the largest is less than twice the smallest;
  - RP02: renewed command (the same `CommandId`), error before commit and outdated revision do not
    add frame. The same move by the new `MatchStore` (restart) gives the step 2, equal projection;
  - the private preference in battle (`SetCombatCardPolicy` defender) does not add frames;
  - demo: CPU moves are recorded and the replacement demo removes its frames;
  - A batch with `recorded = false` does not get frames, and the removed batch loses them with a
    cascade.
- First full run: 3964/3965. `CorrespondenceListTests` creates parties with each type of decision,
  and the viewer projection threw an exception for the type without presentation. Now the viewer and
  the frame are given an empty presentation; the player's view continues to report missing.
  `CorrespondenceListTests|MatchRecordingTests|MatchObserverTests|Projection` 118/118.
- Second run: 3964/3965. Not passed `RoomObserverTests.ALapsedObserver…` (rental 1 s under load);
  lease in the test is now 2 p.
- Full `dotnet test --configuration Release` (with REPLAY-02 code in the tree): 3968/3968.
- Measurement in the test base: average approximately 93 KB text per frame (both sides),
  approximately 17 KB after compression of TOAST.

<a id="plik-replaya--replay-02-2026-10-16"></a>
## File replaya — REPLAY-02, 2026-10-16

Server (PostgreSQL 17):

- `dotnet test --filter ReplayExportTests` 3/3 (RP03, RP04) on the ‘sh-military’ batch completed
  with one rejection dice Shadow:
  - during the game 409 `MatchNotFinished` for the player and the viewer;
  - After the end, the player and viewer get the archive `application/zip` `.wotrreplay`. Manifesto:
    format v1, players, winner and reason, completeness. The frames are equal to the frames in the
    base, and the rate of steps and the first turn are consistent. The history of each page has the
    same steps as the player log of this page. The directory cards is in the file;
  - the names of the entries are only from the permanent list and the size and SHA-256 of each entry
    are consistent with the manifest;
  - there are no session tokens in the file, identifier accounts, `passwordHash` or `decks`;
  - foreign and the player on the tour the viewer gets 404 The second download gives the same frames.
    The viewer after the lease expires gets 404 Party without video gives 409 `ReplayUnavailable`
    and removed 404 ;
  - 62 frame gives three servings (25, 25, 12). The temporary file disappears after the stream is
    closed and the cancelled export ends with `OperationCanceledException`.
- `ReplayFormatSampleTests` 1/1: standard file `tests/fixtures/replays/replay-v1-sample.wotrreplay`
  (31 KB, generated by `WOTR_REPLAY_SAMPLE=<path> dotnet test --filter
  ReplayExportTests.AFinishedMatch…`) reads the Contracts themselves, and the totals of entries
  agree.
- After recent changes (Contracts 0.9.0, standard file test)
  `ReplayFormatSampleTests|ReplayExportTests|ActionLog|BattleReport`: 75/75. Full `dotnet test` with
  export code (before standard file test): 3968/3968.

<a id="playobserve-i-widownia-w-poczekalni--obs-04-2026-10-16"></a>
## Play/Observe and the audience in the waiting room — OBS-04, 2026-10-16

Client `a01262a`, test backend `d1d645354168`, fresh database `wotr_obs04_test`:

- Full EditMode 857/857, including new:
  - `MainMenuTests.OpenGamesOfferPlayAndObserve…` — row details, Play/Observe states, room viewed in
    "My games";
  - `WaitingRoomTests` — viewer without Ready, demo and Observe, with “Take seat”; host-viewer
    removes everyone but himself for launch; the viewer's chat line.
- PlayMode `ObserverRoomNetworkTests|WaitingRoomRemovalNetworkTests|RoomChatNetworkTests`
  3/3. OB03 `ObserversJoinChatTakeSeatsAndAreRemovedThroughTheMenu`:
  - the public-line viewer and the second from the code;
  - the chat (observer) at host;
  - simultaneous “Take seat” of two viewers: one sits down, the other gets “That seat was just
    taken.” and sees the state from the server;
  - back to the audience and removed by the host back to the menu.
- PlayMode `MainMenuLayoutTests|MainMenuNetworkTests|MenuBrowserNetworkTests|MainMenuDemoNetworkTests`:
  - the first 6/7: chat ‘Send’ run went beyond the window about 57 set after the addition of the
    audience line;
  - Amendment: seats 128 px, audience line 52 px, chat at least 40 px;
  - After it `MainMenuLayoutTests|WaitingRoomRemovalNetworkTests` 2/2.

<a id="plansza-widza-na-żywo--obs-03-obs-05-2026-10-16"></a>
## board live viewer — OBS-03, OBS-05, 2026-10-16

Client `0a8bf92`, test backend `d1d645354168`, fresh bases:

- Full EditMode 862/862. New `ObserverViewTests` (5):
  - the player's and viewer's routes with perspective;
  - neutral result and console;
  - bar „Observing”;
  - OB06: the same revision on the other hand, and the late response of FP after switching to Shadow
    is rejected;
  - OB08: the presence of every 30 ms in the test stops after the session is closed and board has no
    decision.

Runs before the amendments: 861/862 twice. The 204 dummy stopped the presence loop without content; now the loop survives every error of one attempt. Delay ending at the closing gave one too many renewal; now the loop checks the closure before sending.
- PlayMode `ObserverBoardNetworkTests` (OB05, OB06, OB08):
  - the viewer opens the ongoing batch with “My games”: hand FP under the bar, without decision and
    option, click the map opens the region panel;
  - Shadow switch: Shadow hand, camera unchanged;
  - FP movement is live and the reboot shows the current revision;
  - the viewer's client has not sent any route `/commands`, `/preview`, `/receipts`, `/seen` or
    `/api/matches/{id}…`;
  - The “Exit match” removes the room from his list.

The first run failed because the test checked the wrong panel (`Information` instead of `Hover.Panel`). After the correction: `ObserverBoardNetworkTests|ObserverRoomNetworkTests|MainMenuNetworkTests|WaitingRoomRemovalNetworkTests` 6/6.
- The
  `ActionLogNetworkTests|BattleReportNetworkTests|GameEndNetworkTests|ActionContextNetworkTests|MatchDeletionNetworkTests|BoardMenuNetworkTests`:
  14/15. `BattleReportNetworkTests.ClosedReportRejectsLateHttpAndFailedReadCanRetry` does not pass
  to `a01262a` (before OBS-03), to `0811060` (UI-204) and to `cbd4ccd` (MENU-28, before all the
  changes UI with 2026-10-16): "last-battle is covered by notification-bar". This is an older
  regression, independent from observers; to be explained separately.

<a id="odbiór-obserwacji--obs-06-2026-10-16"></a>
## acceptance observatories — OBS-06, 2026-10-16

- Server:
  - `dotnet test --filter ObserverAcceptanceTests` 1/1 (OB09). The first run failed to close the
    test socket (`ObjectDisposedException` in the test reader), so the reader tolerates the now
    broken socket. Measurements are in OBS-06;
  - full `dotnet test --configuration Release` 3970/3970.
- Client `0a8bf92`:
  - PlayMode
    `ReturnListTests|ReturnRecoveryTests|ObserverBoardNetworkTests|ObserverRoomNetworkTests` 6/6
    (new `wotr_obs06_test` base);
  - full EditMode 862/862 with OBS-03/OBS-05.
- Windows runtime was not checked.

<a id="lokalna-biblioteka-replayów--replay-03-2026-10-16"></a>
## Local Replay Library — REPLAY-03, 2026-10-16

Client `09ffd8e`, EditMode `ReplayLibraryTests` (RP05) on the server reference file:

- the file enters once; the second import and new export of the same batch give ‘AlreadySaved’;
- a netless reader gives the last step from both sides, history and directory;
- export acts as import in the second library; removal of the copy leaves export;
- the damaged variants do not enter and leave the temporary file: cut, altered portion without
  total, newer version, incomplete, alien path `../`, no entry;
- the identity collision does not change the saved file;
- the aborted record (disk full) leaves no entry, the corrupt file in the folder is a separate entry
  with a reason, and the old `.incoming-*` disappears.

The first full course of EditMode hung: `Assert.ThrowsAsync` in NUnit waited in the main thread for the continuation that also needed it. Tests are now waiting through `await` and the library uses `ConfigureAwait(false)`. Full EditMode 871/871.

<a id="zapis-biblioteka-i-odtwarzacz--replay-04-replay-05-2026-10-16"></a>
## Recording, library and player — REPLAY-04, REPLAY-05, 2026-10-16

Client `260ea18`, backend test `d1d645354168`:

- Full EditMode 876/876. New `ReplayPlayerTests` on the synthetic 60 play with steps in 3 turn
  (`ReplayFixture`):
  - RP08: steps and turn in both directions, grey ends, same revision on the other side, Log without
    later steps;
  - RP09: Fast speed gives 58 breaks after 0,85 s to the end, manual step pauses;
  - RP13: later read wins, in memory are the highest 3 portions;
  - REPLAY-04: `ReplaySaver` through the server's attrap: error with “Retry saving”, “Saved”, second
    entry “Allready saved”, party without recording “not available”.

Runs before the amendments: compilation error (`Add` returns `void`), synthetic card outside the directory (step recognized now after revision), bad Log line assertion.
- PlayMode `ReplayLibraryNetworkTests` (RP06, RP07) and `GameEndNetworkTests`: 4/4. Both players
  "sh-military" record the same replay ("Saved", then "Already saved", one entry). Before logging in
  the library without a server: Play, step to the end with "SHADOW WINS", back, Export, Delete,
  Import.
- `MainMenuLayoutTests`: first run failed, because the column footer came out about 43 pt after
  adding "Replays". After changing the lines to 38 px passes, with new library row controls and the
  replay bar: `MainMenuLayoutTests|ReplayLibraryNetworkTests` 2/2.
- The native file selection windows (macOS/Windows) were not automatically checked; tests use the
  `IReplayFilePicker` Attrap.

<a id="efekty-u-widza-i-w-replayu--replay-06-2026-10-16"></a>
## Effects in the viewer and in the play — REPLAY-06, 2026-10-16

Client `0c6f1b8`:

- Full EditMode 878/878. `ReplayEffectsTests` (RP10, RP11) on a synthetic play, in which step 1
  moves figure and changes both tracks, and step 2 ends the game:
  - first view, manual step and undo: no flight, stage and sound;
  - playing: flight figure, two stages of track, sounds; waiting for presentation at the most 3 s;
    no `Victory`/`Defeat`;
  - change of page and jump during play interrupt flight and second stage.

Runs before the corrections: the test did not see the step of 1, because the playback went on (the pace break is now stopped gate in the test); the wait loop did 61 instead of 60 looks (rounding error, corrected to the integer).
- PlayMode `ObserverBoardNetworkTests|ReplayLibraryNetworkTests` 2/2.

<a id="odbiór-obserwatorów-i-replayów--replay-07-2026-10-16"></a>
## acceptance observers and replays — REPLAY-07, 2026-10-16

Details and matrix OB/RP: REPLAY-07.

- `ReplayEndToEndTests` 2/2: server full batch by HTTP with viewer from turn 3, both files equal to
  the recording, removal of the batch; demo to the end and replaced.
- Full `dotnet test`: 3971/3972. Has not passed the demo test, the room of which has disappeared
  through MENU-27; after the correction the test passes.
- Client `7469a76`:
  - full EditMode 879/879 (`ReplayFullGameTests` on the real full batch file);
  - PlayMode 6/6 (replay and viewer) and the 11/11 regression (menu, layout, back to party, turn,
    waiting room).
- Windows runtime unverified.

<a id="najpierw-kości-przy-przydziale-polowania--ui-205-2026-10-16"></a>
## First dice at the assignment of Hunt — UI-205, 2026-10-16

Client `1b526bb`:

- PlayMode `HuntNetworkTests` 7/7. `RequiredAllocation` checks:
  - before selecting dice: the panel has `hunt-choose-dice`, the candidates pulsate, the plate has
    `hunt-allocation-empty`, there is no glow, and the tag shows ‘0/1’ and there is no gold;
  - After the first dice: `hunt-choose-dice` disappears, and the badge has `hunt-count-filled` and
    opaque golden background.
- `RightColumnLayoutTests|TurnConsoleLayoutTests|DicePanelLayoutTests|HuntLayoutTests|T22TurnNetworkTests`
  14/14.
- Full EditMode 879/879.

<a id="logowanie-nazwą-albo-e-mailem-w-kliencie--auth-03-2026-10-16"></a>
## Login name or email in the client — AUTH-03, 2026-10-16

Client `1b526bb` (AUTH-03 in `01a1ec7`), test backend `d1d645354168`, test clone, fresh base `wotr_auth_client_test`:

- Full EditMode 872/872 (new `GatewayScreenTests`: fields and buttons, account page addresses for
  two servers, name and address login, error message; deleted tests of the old `AuthScreen`).
- PlayMode: login and menu 13/13 (`AuthRegistrationTests`, `NetworkTests`, `MainMenu*`,
  `MenuBrowserNetworkTests`, `DemoRoomNetworkTests`, `MatchDeletionNetworkTests`,
  `WaitingRoomRemovalNetworkTests`, login screen layout); observers and replays 3/3; game 14/17 —
  three tests received `TooManyRequests` from the login limit (20/min on IP) in a dense package and
  passed separately 3/3.
- Hand-held games were not checked.

<a id="ilustracja-miejsca-bitwy--ui-206-2026-10-16"></a>
## Illustration of the place battle — UI-206, 2026-10-16

`dbf8720` Client, Test clone with `d1d645354168` backend:

- Full EditMode 889/889, including new `BattleSceneArtTests` (17): group with settlement and pages,
  named places (Fords of Isen, Osgiliath, Edoras, Helm's Deep, Dol Guldur, Dagorlad), set of 20
  images from regions, version independent of order board, import 1536×320 without mipmap and Clamp.
- PlayMode `BattleLayoutTests|BattleWindowNetworkTests` 6/6 on a fresh base `wotr_ui206_test`:
  battle windows with a higher bar are located without scrolling in four layouts.
- Unity did not change the generated `.meta` files.

<a id="większe-figury-w-oknie-bitwy--ui-207-2026-10-16"></a>
## Bigger figure in the battle window — UI-207, 2026-10-16

Client `5aecd61`, test clone, fresh base `wotr_ui207_test`:

- Full EditMode 889/889.
- PlayMode `BattleLayoutTests|BattleWindowNetworkTests|PressFeedbackClickTests|
  BattleResolutionNetworkTests` 10/10. `BattleLayoutTests` checks the minimum size of figures from
  the composition and losses in all battle windows in four layouts. First runs have detected figure
  losses pressed to 33 (improved rule specificity), rounding of the 125% scale (0,5 pt tolerance)
  and the off-screen loss row at 1366×768 and 125% (direct size and scene bar 30 on the low screen).

<a id="decyzje-bitwy-po-stronie-gracza--ui-208-2026-10-16"></a>
## battle decisions on the player side — UI-208, 2026-10-16 decisions on the player side

Client `7f8a776`, test clone, fresh base `wotr_ui208_test`:

- Full EditMode 889/889.
- PlayMode `BattleLayoutTests|BattleWindowNetworkTests|PressFeedbackClickTests|
  BattleResolutionNetworkTests|BattleNetworkTests` 14/14, including the new
  `LossesAndRerollsStandOnTheDecidingPlayersSide` (losses and losses of the attacker and defender:
  figure losses and seals in the losing column of the page, its hidden composition, the composition
  of the other side visible, dice in the player column, player-side buttons) and the Assertion that
  the entire battle window ends over the turn console in all windows and four layouts.

<a id="kafel-polowania-otwiera-okno-z-pulą--ui-209-2026-10-16"></a>
## Hunt Tile opens the pool window — UI-209, 2026-10-16

Client `6a90ea7`, test clone, fresh base `wotr_ui209_test`:

- Full EditMode 889/889.
- PlayMode `HuntNetworkTests|HuntLayoutTests` 12/12: click the tile after the finished Hunt opens
  the window with the visible pool list and the icon selected, the icon rolls it, the re-click of
  the tile unfolds it in the open window; in the unset tile assignment it does not open the window
  or list.

## Console confirm for card decisions — UI-211, 2026-10-16

Client `e808514`, test clone, fresh database `wotr_ui211_test`:

- Full EditMode 900/900, including `CardDecisionsTests` (which decisions belong to the console, card title,
  progress for flights, placements and choices), `DecisionWindowChoicesTests` (a card choice on the board with a
  Confirm opens no window) and `TurnConsoleStateTests` (headline names the card).
- PlayMode card network packs: Shadow character and strategy cards 65/65; Free Peoples strategy and card packs
  36/36. The shared `MatchTestRoom.Confirm` uses the console's ✓ when the window is closed.

## Nazgûl flights on the map — UI-212, 2026-10-16

Client `ce97905`, test clone, fresh databases `wotr_ui212*_test`:

- Full EditMode 903/903, including `NazgulFlightDraftTests` (arming a region or a figure, one flight per
  destination click, the Witch-king only when armed himself, grouping per origin and destination, ↶ restoring
  the armed source, the console's progress and hint, no window choices even with an effect trail).
- PlayMode `Shadow02NetworkTests|Shadow02LayoutTests` 7/7 (Search: lit Barad-dûr armed by a map click, the
  Witch-king picked in the region panel, ghost at Hollin, "Nazgûl moved 1", ✓ in the console; Strike: the armed
  source survives a reconnect; layout: nine Nazgûl flown by map clicks at both sizes and scales without a window).
- PlayMode base card packs (Shadow, Shadow strategy, Free Peoples strategy, card packs, effects, Fate) 132/134 before
  the last fixes; the two failures were the Search assertion fixed above and the known
  `Shadow04LayoutTests.ServerProjectedChoicesAndTwoArmiesFitBothSizesAndScales` (UI-164). `ShadowStrategy02NetworkTests`
  13/13 after scoping the effect-trail rule to card decisions.
- PlayMode map, movement and recruitment: `TravelGesture|ArmyContext|MapClickMove|Muster|Movement|FellowshipRoutes|
  MapVisibility|FellowshipNetwork|Reinforcement` passed; `MapContourInteractionTests` 5/5 and `MovementNetworkTests`
  1/1 after a rerun (the first run hit the test server's TooManyRequests limit). Known failure, also on the client
  baseline `e808514`: `ContextHoverTests.ClickedInspectionIsInteractiveStableAndFitsDenseRegionsAtBothScales` (a
  hover figure moves by about 0.5 px when deselected).
- Server: `dotnet test tests/Wotr.Tests --filter Shadow02TravelTests` 12/12, including
  `OneDecisionFliesNazgulFromThreeRegions`.

## Card recruitment with one armed Reserve token — UI-213, 2026-10-16

Client `8f42869`, test clone, fresh database `wotr_ui213a_test`:

- Full EditMode 905/905, including `EventRecruitDraftTests` (CC04: one armed token fills three settlements, ↶
  removes the last placement and re-arms it; the armed token stops at the card's limits, another token switches it,
  region first still fills the chosen region) and the existing `ReinforcementPlansTests`.
- PlayMode `ShadowStrategy0|Strategy0|FateCardsNetworkTests|CardNetworkTests|CardPack` 94/94, including Pits of
  Mordor placing six Regulars in three strongholds with one token click, the lit tab and token, "Recruited 6 of 6",
  no window and the console's ✓; `MusterNetworkTests|ReinforcementsLayout|Muster` 13/13.

## Card options, figure choices and army events at their objects — UI-214, UI-215, UI-216, 2026-10-16

Client `17bca0d`, test clone, fresh databases `wotr_ui214*_test`, `wotr_ui216*_test`:

- Full EditMode 918/918, including `CardConsoleOptionsTests` (Black Captain orders, The Nazgûl Strike!'s three buttons
  and table cards, Lure of the Ring names its Companion, Dreadful Spells' alternatives after the region, reactions with
  a hand card, too many responses or the Hunt, the Ents follow-up's trail), `CardFigureDraftTests` (Hill-Trolls across
  regions, a single figure, excess outside a battle, the Dead Men's companions) and `CardArmyEventTests` (a lit army
  starts with all its figures, the panel toggles them, "Armies moved 1 of 2", Skip maneuver, no window).
- PlayMode `Shadow0|CardPack|BoardInteractionTests` 65/66, `ShadowStrategy0|Strategy0` 63/63, earlier
  `ShadowStrategy0|Strategy0|FateCardsNetworkTests|FateGimli` 73/73, `T22BoardNetworkTests|MusterNetworkTests|
  TravelGesture|ArmyContext|MapClickMove|MovementNetworkTests` 21/21, `HuntResponseNetworkTests` 3/3,
  `MultiUseDiceNetworkTests` 2/2, `MordorNetworkTests` 7/7 in halves and alone (the test server's TooManyRequests limit). Covered:
  CC05 (Black Captain orders in the console), CC06 (The Nazgûl Strike!'s console buttons and a table card's ×), Lure of
  the Ring, There is Another Way, the Ents follow-up's Decline, CC07 (Hill-Trolls in the region panel), Stormcrow,
  Rage of the Dunlendings, Denethor's Folly, The Grey Company, excess after cards and moves, Dead Men, CC08 (Shadows
  Gather, The Shadow Lengthens, The Shadow is Moving, The Ringwraiths Are Abroad on the map with console progress and
  alternatives), Paths of the Woses, Corsairs of Umbar.
- Known failures, unchanged: `Shadow04LayoutTests.ServerProjectedChoicesAndTwoArmiesFitBothSizesAndScales` (UI-164),
  `ContextHoverTests.ClickedInspectionIsInteractiveStableAndFitsDenseRegionsAtBothScales` (0.5 px, also on baseline
  `e808514`), `ShadowStrategy02NetworkTests.CorsairsRequiresAnotherRoundWhenDefenderStays` flaky in large packs (passes
  alone 2/2: its assertion races the automatic No card).

## Card decisions at their objects, aggregate acceptance — UI-164, 2026-10-16/17

Client `46b286c`, test clone, fresh databases `wotr_ui164*_test`; baseline for comparison `453c88ea` (before UI-210):

- Full EditMode 919/919, including CC09 for the waiting side (`CardConsoleOptionsTests.TheWaitingSideSeesTheCardsTrailWithoutAWindow`).
- PlayMode base cards (both sides) 65/65 (`Shadow0|CardPack|CardNetworkTests|CardInteractionTests|CardGestureNetworkTests|
  EffectsNetworkTests`, including Shadow04 layouts now green and Nazgûl Search's waiting window closed),
  `ShadowStrategy0|Strategy0|FateCards|FateGimli` 72/73, Characters 24/24 after the console response fix, Fellowship,
  Hunt, battles, Politics, T22, map and windows in small packs (the test server's TooManyRequests limit forces reruns):
  `DecisionNotice|NetworkTests|FigureMotion` 6/6, `FellowshipNetwork|FellowshipPanel|FellowshipRoutes|FellowshipTrackLayout`
  7/7, `MapContourNavigation|MapContourRecovery` 6/6, `MapContourInteraction` 5/5, `HuntResponse` 3/3, `T22Turn` and
  `BattlePolicy|BattleSound|AutoOpenPolicy.ANewBattle` green, `HandDiscard` and `CharacterNetworkTests.Grey` 4/4.
- Failures that occur identically on baseline `453c88ea` (not caused by UI-210–UI-164):
  `AutoOpenPolicyTests.AnOrdinaryOpponentActionOpensNothingAndTheFellowshipPhaseOnlyForItsPlayer` (Hunt die border 3 vs 2),
  `BoardCompletionLayoutTests` ×2 (text 1 pt taller), `CardServerCompatibilityTests` ×2, `DraftInputTests.HeldEnterCannot…`
  (no tray ✓ for the Hunt allocation since UI-172), `EffectsLayoutTests.MultipleSources…` (492 > 475.4),
  `RightColumnLayoutTests.IllustratedFellowshipAndHuntWasteNoRows…`, `VictoryPanelLayoutTests.BothScores…`,
  `BattleReportNetworkTests.ClosedReportRejects…`, `PairedDecisionPanelNetworkTests` ×2,
  `MapViewportTests.MovingArmyCannotStopNavigation…`, `ContextHoverTests.ClickedInspection…` (0.5 px).
- Flaky in large packs, green alone: `ShadowStrategy02NetworkTests.CorsairsRequiresAnotherRoundWhenDefenderStays` (races the
  automatic No card), `ViewApplicationNetworkTests.ADrawingFailureIsReported…`.
- Not in the MVP regression (expansions): Kings, Lords, Council, expansion token and ruler tests.
- Not run: a local build (lean verification preference: the user builds in the Editor).

## Base-game regression failures without an owner — QUAL-19, 2026-10-17

Client `4ee71eb`, test clone, fresh databases `wotr_qual19*_test`:

- Full EditMode 919/919.
- PlayMode, final run: `AutoOpenPolicyTests|DraftInputTests|CardServerCompatibilityTests|BoardCompletionLayoutTests|
  EffectsLayoutTests|RightColumnLayoutTests|VictoryPanelLayoutTests|ShadowStrategy03LayoutTests|TurnConsoleLayout|
  CharacterRosterLayout|CharacterLayout|InterfaceSettings|BoardMenu` 37/37;
  `PairedDecisionPanelNetworkTests|ShadowStrategy02NetworkTests|RingUseNetworkTests|T22TurnNetworkTests` 21/21;
  `Shadow0|CardPack|CardNetworkTests|HandDiscardNetworkTests|CharacterRosterPanelNetworkTests` 60/60.
- The thirteen listed failures that reproduced on baseline `453c88ea` now pass; causes and fixes are in
  `docs/tasks/QUAL-19.md`. Still owned elsewhere: BattleReport, ContextHover and MapViewport (QUAL-07), login limits and
  shared-scenario order (QUAL-06).

## Turn console, Hunt window and Rings, aggregate run — UI-163, 2026-10-17

Client `46b286c` (then `4ee71eb` for the AutoOpenPolicy fix), test clone, fresh database `wotr_ui163a_test`:

- Full EditMode 919/919; client `scripts/check_docs.py` PASS (57 Markdown files).
- PlayMode: layouts `TurnConsoleLayout|DicePanelLayout|EdgePlaque|EdgeBoardLayout|TurnLayout|CompactBoardLayout|
  BoardChromeLayout|ClickInspection|FloatingWindow|MordorLayout|HuntLayout` 30/30; `HuntNetwork|HuntActivity|
  HuntRecovery` 11/11; `HuntAftermath|HuntResponse|RingUse` 6/6; `T22TurnNetwork|AutoOpenPolicy` 7/8 then green after
  QUAL-19; Mordor 6/7 with one login-limit failure (`HideAndInactivity…` green alone earlier on the same day);
  `ContextHelpNetwork|InGameContextNetwork` 7/7.
- Owner evidence: UI-159–UI-162 sections above (2026-10-14) and UI-164 (2026-10-16/17).
- Not run, owned by the user (lean verification decision of 2026-10-14): `BuildLocalMac`, `BuildLocalWindows`, and
  `profile-client.py --suite board`, which needs a Development Player build; screenshot reviews are superseded.

