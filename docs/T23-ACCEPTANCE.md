<a id="t23--odbiór-bazy-i-korpus-zapisów"></a>
# T23 — acceptance database and registration body

acceptance completed 2026-10-05 UTC : ** 1753/1753 . NET , 118/118 EditMode , 173/173 PlayMode Two full parties from a normal lobby, 28 The regression records and the set of base content have evidence below. Sources: [logic B01–B20](rules/LOGIC.md) , [FAQ and the manual 2.4.1](sources/SRC-08-AUDIT.md) , [96 kart](../content/implementation-manifest.json) , [14 characters](../content/character-implementation-manifest.json) . acceptance UI and 22 types of base decisions: [T22](https://github.com/jpolaczek/wotr-client/blob/2dd29f0e27bd99ef7f28c41e7472cfb9eac6363d/docs/ui/T22-ACCEPTANCE.md) .

<a id="pełna-partia"></a>
## Full batch

Two accounts form a new room by the public API, accept the same version of the base configuration and run a normal setup. Two mounted Unity clients receive their own projections by HTTP/WSS. All gameplay decisions perform real controls and normal approval. The server uses the production RNG; the test does not swap state, deck, tiles or dice.

The clear test scenario leads to Fellowship via High Pass and Rhovanion towards Morannon. FP Moves/hides Fellowship available dice ; Shadow assign the required minimum to Hunt Other action They deliberately reject die Both sides refuse optional responses, Shadow keeps the first shot Hunt , FP He's taking the Ring injuries. cards is selected and rejected covered. Declaration routes and disclosures are separate choices. The script does not assume which side wins: expects legal completion and compatibility of both clients. This is a test script, not a function AI product or proof of balance of strategy.

Each approved decision must increase the search by one and reach both clients. Every few turn follows the reconnect. After the end, both projections retain the result, no option and no further refusal of the next command. The report in `.artifacts` contains only public run, without passwords and sessions. The manual acceptance user remains deferred; Windows/macOS editions and the measurement of FPS belong to Stage E.

<a id="korpus-regresyjny"></a>
## Regression corpus

The stored input files are to cover each base decision family, four endings and state after using the Ring. Internal recording includes the full state of the server; it is not DTO or client resource. Manifesto attaches SHA-256, versions and expected decision/result. Control does not generate the current implementation again. Export is an open, separate test tool.

The tests will check reading/repeated record, save costs and follow-up, legal response after reading and strict projections of both sides. Comparison after changing only the secret hand/rail order of the deck is to keep the enemy's view. Restart HTTP/PostgreSQL during battle and Hunt continue to check the actual integration tests; comparison JSON does not replace the host reboot.

<a id="wymagane-kontrole"></a>
## Required checks

- Full regression of .NET/HTTP/PostgreSQL including V01–V11/V16, reserves and empty decks.
- Manifesto 96 cards in both applications and 14 cards in the character.
- Unity EditMode and full PlayMode along with a new party.
- Four targeted endings, read after reboot, retra and confidentiality of raw JSON.
- The consistency of the directory, sources, documentation and task graph.

Below is a chronological record of actual results, including indirect checks.

First attempt of the full batch: `bash scripts/test-client.sh -testFilter Wotr.PlayTests.T23FullGameNetworkTests` — **1/1 PASS , 0 skip**, 2026-10-05 22:28:06–22:28:43 UTC Normal room `f7409743334b45c59fc10d18f1a0d6fe` : **7 Turn, 92 decisions, RingCorrupted**, Corruption 12 After entering the Mordor track. Two sessions, the reconnet in progress and after the result, the refusal of new commands for both parties. Report: `.artifacts/t23-game-first-report.json` ; wynik/logi: `.artifacts/t23-game-first.xml` , `t23-game-first-unity.log` , `t23-game-first-server.log` The run also has selection/rejection cards , declarations, disclosures, hiding and transitions of turn; is not a script set at the end. Full regression and body remain to be performed.

<a id="utrwalone-decyzje"></a>
## Persisted decisions

[Corps](../tests/fixtures/base-regression/README.md) has **28 snapshots**, 24 command resumes and four ends. [Manifest](../tests/fixtures/base-regression/manifest.json) pin 22 base types of decisions, owners, costs and results. Files are derived from T18–T22 scripts; written command has reviewed the source descriptions of these tasks. `strategy-march` uses the main `Payload.Route` according to the Through Day and Night contract; Shadow marches have another, described group contract.

| Scope | Body Files | Checked return |
| --- | --- | --- |
| phase/costs | turn-hunt-mandatory, allocation, field, discard, ring-paid | Assignment, throw, change actor, discard to six, further action after the Ring. |
| cards/armies | grey, sh-str03-king-excess, another-way, strategy-march, shadow-search | Reaction, excess, choice of effect, movement of armies and Nazgûli; no recharge of event. |
| Battle | policy-fp, secret-combat, no-regular, strategy-confusion, retreat, sh-str02-corsairs-relief, relief, sh-str02-corsairs-defense, siege | Secret first choice, reroll, loss without regular, continuation, retreat, progress, defense Stronghold and extension. |
| Fellowship/Hunt | boundary-random-companion, reveal, guide, alone, hunt-reroll | Damage, route with further Stronghold, new Guide, separation and projection behavior. |
| Ends | ring-destroyed, ring-corrupted, military-fp, military-sh | Irreversible result, empty decision/Stock, no cost left. |

The first build of the new tests detected a collision of the `Projection` field name with the type; the name was changed to `Views`. This is a compilation error of the test code, without any cases performed. Then `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~BaseCorpus' --logger 'trx;LogFileName=t23-corpus-second.trx' --results-directory .artifacts/test-results` — **85/85 PASS, 0 skip**, including three reboots of HTTP/PostgreSQL from the body, refusal of the foreign side and retry after the second reboot. For all records, strict projections were tested after changing only the opponent's hand, the waist order and the RNG records. Full regression was still underway.

<a id="powiązanie-v01v11v16"></a>
## Link V01–V11/V16

| Case | Existing behavior tests and acceptance |
| --- | --- |
| V01 | TurnTests , T22TurnNetworkTests : smaller/r an equal amount and no cost pass. |
| V02 | TurnTests.V02RingChangesOneDieThenAnotherPaysAndSecondRingIsRejectedAfterRestart; body ring-paid and his restart. |
| V03 | MovementTests, T22BoardNetworkTests: two armies, a connection, an identity of figure and a deferred limit. |
| V04 | MusterTests, Strategy03RecruitmentTests, ShadowStrategy03BatchTests/ShadowStrategy04RecruitmentTests: ordinary recruit, cards exceptions and shared deficiency. |
| V05–V07 | BattleRulesTests/BattleRollTests/BattleCardTests/BattleLossTests, cards and T22BoundaryNetworkTests packages: limits, natural results, order and causes of losses. |
| V08 | SiegeTests/SortieReliefTests/SiegePersistenceTests, StrongholdNetworkTests and body: garrison, rescue, out, reduction of elite, control. |
| V09–V10 | FellowshipTests/CompanionTests/HuntTests/CharacterHuntTests, HuntPersistenceTests, FellowshipNetworkTests and body: route, victim, Guide windows, additional tiles and the exact cost. |
| V11 | VictoryTests/VictoryPersistenceTests, FellowshipNetworkTests and T22BoundaryNetworkTests: both results Ring, military thresholds and priority; full party T23. |
| V16 | EffectManifestTests and [96 lines programme/tests](rules/T21-CARD-COVERAGE.md); character manifesto and CharacterManifestTests complement 14 obverses. |

Deck exhaustion: `TurnTests.EmptyDeckDoesNotReshuffleAndCannotPayToDrawFromIt`; return of the tiles pool: `HuntTests.ExhaustedPoolReturnsOnlyDiscardedStandardTiles`. Reserve deficiency: recruitment packages tests, degradation without regular in battle and extension of siege. Evidence of crediting these names will be full TRX; presence in the table itself does not mean execution.

Full Regression domeny/serwera: `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t23-full.trx' --results-directory .artifacts/test-results` — **1753/1753 PASS , 0 skip**, 57 s tests. Log `.artifacts/t23-full-domain.log` . `python3 scripts/card_coverage.py --check --results .artifacts/test-results/t23-full.trx` confirmed 96 cards , 477 related methods and 990 the same function `verify_results` , called for the sum `entryTests` and `abilities.tests` from the character manifesto, confirmed **14 obverses, 29 methods, 51 cases PASS**. It was therefore checked that the related tests were performed, not the mere presence of their names.

<a id="końcowy-odbiór-klienta-i-przekazanie"></a>
## Final acceptance client and transfer

`bash scripts/unity.sh -runTests -testPlatform EditMode -testResults
.artifacts/t23-editor-final.xml -logFile .artifacts/t23-editor-final.log`
— **118/118 PASS, 0 skip**, 2026-10-05 22:38:03–22:38:05 UTC.

`bash scripts/test-client.sh` — **173/173 PASS , 0 skip**, 2026-10-05 22:38:45–22:46:21 UTC , 455,74 p. One full sample includes the previous 172 tests and new full batch. Logs and XML : `.artifacts/t23-play-full.xml` , `t23-play-full.log` , `t23-play-full-server.log` The second normal batch `f1f5ce9d1b4f425899493fd63600904a` : **11 Turn, 145 Decision** RingCorrupted , Corruption 12 step by step 4 Mordor, report: `.artifacts/t23-game-full-report.json` The result of both tests is due to RNG and the scenario choices, not from changing the start or re-starting to win. Targeted military victories and destruction of the Ring were credited in the same regression.

Registration validation (88 with this-dos), directory (30 families/34 ed window), sources, manifests and `git diff --check`: PASS. No new source conflict detected. Structure overview is recorded in [CODE_QUALITY](CODE_QUALITY.md). Changes: full batch test and common Unity room, three script export tools, three test classes .NET, 28 snapshots/manifest, rules and documentation bindings. No change in the production logic of the database in T23.

Limitations: Clients' trials were performed at Unity Editor 6000.6.0f1 per macOS, with two independent connections to Kestrel/PostgreSQL. Manual user tests, a pair of stand-alone releases Windows/macOS and acceptance 60 FPS remain in stage E; they are not counted by this protocol. The body does not prove all possible cuts to future expansions. Next ready task: **T24**.
