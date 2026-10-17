<a id="t29--ruler-tokeny-i-mroczni-wodzowie"></a>
# T29 — Ruler, Tokens and Dark Leaders

Sources: KINGS 1.0 pp. 6–10, 14–15, 17 and three AWW KM 1/3-3/3 with [SRC-06](../sources/SRC-06-AUDIT.md). All three AWWs were re-examined and the full Ruler/token rules were read. T28 provides five sovereigns and their capabilities; T31 integrates Dismay, variants and special cuts of Lords. Full support for new client decisions is provided by UI-09.

<a id="kości-i-kolejność"></a>
## dice and order

Each page has one physical die `ruler`, with just six walls of its site. die is initially Removed. Recovery only makes it available in the next turn if on board is Awakened FP or Dark Chieftain SH. You can never give a separate dice for each figure. The death of the last eligible figure does not remove the result during turn; the next Recover removes die. It can come back after the new entry. The Ruler SH is never a candidate for allocation to Hunt.

After the throw, the player who threw the Ruler must reject one of the eligible die : First SH , then FP Rejection SH allows Ruler Eye, but not standard Eye nor die already assigned to Hunt. Other cast Eye SH They only go to Hunt after his choice. FP chooses standard or Ruler. dice Keeper/Lesser are always excluded from discard (p. 17 Queue and each selection are saved; preview indicates cost without re-slip. Strategic selection does not replace automatic discard based on list order.

The point of entry to the discards is separated from the usual `BeginActions`; the continuation of the special selection dice Lords reaches the same point. T31 completes Dismay there and verifies the full order for both expansions.

<a id="token-i-natychmiastowa-zdolność"></a>
## Token and immediate ability

Five tokens retain the identity given at the opening. Available token is free to release only for the still present Awakened and available Ruler FP. Token changes the score to Muster for Branda/Dáina/Théodena, Event Denethor or Character Thranduil. The cost of the token and the use of the right second capacity are one command, with one preview and approval. There is no window to use the changed die to another action. The entire target action must be legal.

Token is a disposable in batch. Using the FP Ring in a given turn blocks the token, and using the token blocks the Ring to another turn (KINGS p.10). The ban also applies to the transfer of the Ring by the Keeper's ability; this does not change the separate base limit of the normal change of dice. History and UI show the identity of the token and the paid ability. Dismay and the variant without Ruler tokens are a separate range of T31.

<a id="wejście-i-ruch-wodzów"></a>
## The entrance and movement of the chiefs

Black Serpent: Gondor At War, army with unit SE; level/Command 2/2. Shadow of Mirkwood: Dwarves/Elves/North At War, army with Sauron; 3/1. Uglúk: Rohan At War or revealed Fellowship; army with Isengard or free region revealed Fellowship or neighboring; 2/1. Hidden Fellowship does not give a second location even in Rohan War. Cost: any Muster or Ruler result outside of Eye. Each identity enters once; death does not allow re-entry.

Intersection with Mordor (T39): BASE p. 43 disable Fellowship on the track from each region of the map. Obverse Uglúka KM 3/3 and KINGS p. 14–15 still require a region for an alternative entry point. The disclosure on the track meets the entry condition, but does not create a region or neighbours: there are places with the Isengard army. Example: revealed Fellowship step by step 1 and the army of Isengard in Orthanc allow to enter there, but not to Gorgoroth/Morannon From the very close of the track. Hidden Fellowship still requires Rohanu At War. Tests `FellowshipOnMordorTrackOffersOnlyIsengardArmyEntry` and `RevealingFellowshipInMordorRestoresBothViewsAndDoesNotRepeatDamage` check both sides' options, reject bad targets and restart HTTP/PostgreSQL after the kaflu Reveal without a repeat cost/obra ginseng/ RNG .

The chiefs are the Servants for the ordinary kart/efekts, but not for the transformation of Gandalf White, also historically. They move like the non-flying Mouth: level, edges, impermeable boundaries and siege; they do not fly like Nazgûle. The Southrons & Easterings icon (`southrons-easterlings` in the obverse index) maps openly to the existing identifier nation board `southrons`. Production metadata and condition of the participating army use the same ID.

<a id="walka-i-polowanie"></a>
## Fight and Hunt

The chief chooses to replace his own legal card before a shared reveal. The opponent does not recognize her identity before. card remains played, the original effects do not work simultaneously with the replacement. The cost of the ability is separated from her later effect and does not return after cancellation.

- Red Wrath asks and settles 1 hit your own participating army before reveal: the player can choose
  Regular or Downgrade Elite. Then +1 to Combat SH on the initiative 3, +2 on legal Relativeless
  Assault, without a reroll bonus.
- Lord of the Bats dedicates its own Leadership Mirkwood. Premium +1 to Leader re-roll at the 4
  initiative; Swarm of Bats adds the cancellation card FP at 0. Leaving the zero Leadership does not
  give a fictional spread.
- I Command gives both armies +1 Combat and Leader re-roll at 3; with Desperate Battle only SH is
  premium. Cost does not include Leadership.
- In March Day and Night gives +1 only to the actual Hunt re-roll when Uglúk is with Fellowship or
  next to it. It does not create a source of reroll or bonus for the first throw; the ability
  itself also works with hidden Fellowship.

The initiative draws the defender. Daring/Brand can cancel the replacement. Sorcererer still sees the card played. The choice of the leader, cost of loss and programs of initiatives have a permanent record, without being dependent on animation/Registration traders.

<a id="weryfikacja-i-przekazanie"></a>
## Verification and transmission

Plan: V12 (entry/Recover, walls, allocation, both types of Eye, discard order, Lords excluded from cost); five tokens with correct/illegal action and Rings in both orders; all source cases of three chiefs, privacy before reveal, costs/cancellation/Sorcerer, Hunt. Manifesto binds obverses to behavior tests. HTTP/PostgreSQL reboot before discards, token and combat costs, comparison of both views and retrams without RNG. Then the regression of bases/Lords/Kings and compilation of shared DTO Unity. The actual commands and results will be added after execution.

Checkpoint dice/entry: `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~Kings|FullyQualifiedName~LordsDice|FullyQualifiedName~LordsRecovery|FullyQualifiedName~BaseCorpusTests' --logger 'trx;LogFileName=t29-entry-verified.trx' --results-directory .artifacts/test-results`: **339/339 PASS, 0 ski**, including three new reboots HTTP/PostgreSQL. includes 12 from the Ruler, Recovery and Re-source walls, order of both discards, disable allocation/special dice, conditions and all results of the entry of the chiefs, role/White, Uglúk at revealed/hidden and non-flying traffic.

The first set had 277 PASS/25 FAIL: the earlier fixture T28 indicated the Ruler without owner, and the control of the base enums did not disable the new decision expansions. After pointing out FP and adding RulerDiscard to the deactivation body remained unchanged: 320/320 PASS. The entry tests revealed the nation's alias SE, improved in production and tested above. Tokens, combat capabilities and manifesto of the chiefs remain in progress; this checkpoint does not finish T29.

Checkpoint Tokens: `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~Kings|FullyQualifiedName~LordsNarya|FullyQualifiedName~LordsNenya|FullyQualifiedName~LordsVilya|FullyQualifiedName~BaseCorpusTests' --logger 'trx;LogFileName=t29-token-verified.trx' --results-directory .artifacts/test-results` : **353/353 PASS , 0 skip**. `KingsRulerTokenTests` includes 28 cases, including two new reboots HTTP/PostgreSQL. All five tokens change the score and immediately performs the ability; preview does not mutate the state, retry does not duplicate cost/figures , bad targets/zdolno ed dice Denethor works for the draw and event with nested selection; after the run-up, additional selection is mandatory. Thranduil supports Hunt and longer separation along with selection Guide The ring is ordinary, Keeper and cost card table block token; reverse order blocks all three costs for another turn First attempt: 352 PASS/1 FAIL — separation test was too early to expect `Spent` , before the mandatory selection Guide The completed scenario preserves `Resolving` by recording and ending the cost after conscious choice.

Token options do not number the figur/trasy/kart. combination They use existing providers on the target result; cache in a single projection limits the number of copies to three possible walls, regardless of the number of tokens. Normal command and Shadow projection do not create these copies. Validation/preview copy the state only for the overt command with the token, retaining no mutations when rejected.

<a id="odbiór-t29"></a>
## acceptance T29

`bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t29-full.trx' --results-directory .artifacts/test-results`: **2543/2543 PASS, 0 skip**, 1 min 27 p. `Kings|HuntTests|BattleTests|BaseCorpusTests` area set before acceptance: **462/462 PASS**. Nine new tests HTTP/PostgreSQL includes entry, both discards, token with recruitment, extra draw Denethor, pre-reveal selection, Red Wrath cost, Mirkwood cost and metastas Uglúk. Each checks both projections after reboot, authorisation, preview without RNG, reference state and receipt after next reboot without resuming.

`bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t29-editor.xml -logFile .artifacts/t29-editor.log`: **129/129 PASS, 0 skip**. Confirms the compilation of shared DTO and regression of existing presentation. New expansions controls were not received in PlayMode; they remain in UI-09, and normal configuration starts in T33/UI-09/T34. Windows, manual user tests and implementation are not counted by T29.

[Manifesto of three chiefs and five tokens](../../content/kings-chieftain-implementation-manifest.json) has 7 ability and set 15 cases of obverses. `KingsCharacterManifestTests` compares statistics, alias of the nation SE , five token results, a full list of source clauses and active test methods. Verification of references by `card_coverage.verify_results` real `t29-full.trx` : **49 methods, 111 Passes PASS**; earlier manifesto T28 continues ** 45/113 PASS * All of them 13 The obverse has now `implemented=true` ; 19 events still belong to a separate T30. `source_kings_characters.py` , `check_catalog.py` , `tasks.py validate` ( 88 tasks) and `git diff --check` : PASS .

The fight checks privacy before the reveal, legality of the dedicated card, a single choice from among several chiefs, the preservation of the original, the new round, both Red Wrath and downgrade Elite results, Mirkwood 0/4 initiative, both Daring/Brand directions, Sorcererer despite the cancellation and the lack of reroll without the remaining Leadership. When the cost of Red Wrath eliminates the last unit, the usual ending of battle rejects the obliged card and removes the accompanying leader without a throw. Uglúk gives a bonus to both armies or only SH at the Desperate Battle and in Hunt only the selected metastatics; the natural one remains a box.

First test of combat: 21 PASS/3 FAIL — fixture mistakenly pointed to Horn instead of Daring and expected location `removed` instead of an existing `casualties` Another attempt: 35 PASS/1 FAIL — expected Leadership Dáina 2 instead of 1 Fixed data/waiting Test, not rules; the full set above has passed. The review of the structure and the limited cost of the projection is recorded in CODE_QUALITY . Another range: T30-CARDS-01 Dismay and the intersections of the variants remain in T31.
