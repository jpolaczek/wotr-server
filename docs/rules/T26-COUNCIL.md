# T26 — Council of Rivendell

Sources: LORDS p. 30–31 and six obverses LM 5 – 10/10 In [index SRC-04](../sources/lords-character-index.json) ; FAQ 1.2 p. 3 Status acceptance only store task registry.

<a id="kontrakt-przed-implementacją"></a>
## Contract before implementation

Setup remains a private decision of FP before the first Recovery. `ChooseCompanionVersions` selects one version of Gandalf and optional versions of the other six characters. The omission of the character means the base version; two versions of the same figure are not allowed. The guide is a separate choice of stable ID figure. Alternative Council requires a Council configuration, Gandalf Keeper remains available without it. Options group versions after figure, without the numbering of all combinations.

The first Fellowship Phase opens a private decision of the homelands also when FP has no valid alternatives. This means that type/time itself does not betray the version. FP approves the list of figures and allowed places or an empty list. Versions of declared characters are revealed, figure leave Fellowship and apply the normal effects of the nation's icon. No icon does not activate the nation. Gimli places provide a separate location rule, prepared to expand by T32.

For zero declared figures no token; for one SH make a separate decision to choose one; for at least two are awarded both. Both decisions are signed separately. Only after these are returned the ordinary Fellowship Phase. Tokens are public resources separate from dice and Rings: Nazgûla movement/ Minion or one legal political step SH . Custom type command validizes effect, target, no cost dice and the limit once per turn ; used token is not renewed. dice ed pass the text of the text is not changed. LORDS p. 31 with the end of Action Resolution database: SH zero dice can use token when FP I've got it. dice ; when both pools are empty, the phase ends and token does not prolong it.

Statistics, nations and capabilities come from a selected version, not from the base ID figure. The moving level 2 Boromir/Gimli/Legolas does not change the level to Hunt/kart. Their Guide separates, subtracts 1 Damage and collects all separated characters from one tile into one group (FAQ p. 3). The champion checks the Army's defender region and the limit of five dice. Messenger activates the designated nation before a political step; it does not inherit the limit of `unconquered` from the other cards.

Both alternate Hobbits in Fellowship can cancel the standard tile as a Guide cost. Cost eliminates both, reveals versions and does not run back from battle. The Art of Disappearing gives a separate selection of the legal neighbouring region after elimination in battle; normal movement excludes the besieged Stronghold and the impenetrable boundaries.

Strider Dúnadan saves first die Movement Fellowship in a given turn at the start of the movement, regardless of subsequent changes to the Guide. With initial progress ≥ 1 She's putting that one away. die to Fellowship Box; the same Hunt It's still happening. die does not count as retrieved from Hunt for Narya. It does not receive a base Hide ability of any dice.

<a id="plan-weryfikacji-i-granica-odbioru"></a>
## Verification plan and limit of acceptance

Tests: V13, identical projections SH for hidden versions, Guide/victim/separation, the homeland 0/1/2/5, both token choices, limits and attempts of illegal conversion, restarting each page; full `verificationCases` six obverses, regression of the base and T25. Before acceptance manifest source-capacity-test, HTTP/PostgreSQL, .NET and adequate Unity regression. UI expansions remains in UI-09/T34, production gate full Lords also requires T27. This document does not declare the execution of scheduled checks.

<a id="checkpoint-setup-i-tokeny"></a>
<a id="checkpoint-setup-and-tokeny"></a>
## Checkpoint: setup and tokens

2026-10-06 : a catalogue of six versions, a private setup, the first decision of the homelands, award/u Token life and projections and previews. `t26-checkpoint-setup.trx` : `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~Council|FullyQualifiedName~Lords|FullyQualifiedName~BaseCorpusTests'` — **334/334 PASS**, 0 omitted, including four new tests HTTP/PostgreSQL (six replays with the host restart, private preview, rejection of the other side and retra). The first run revealed the erroneous assumption of the test with initially passive Elfas; the activation fixture improved, normal setup was not changed.

The other abilities of the six obverses are still up to implementation/check; the source flags of `implemented` remain false. This checkpoint T26, not acceptance all.

<a id="dalsze-rozstrzygnięcia-awersów-i-integracji"></a>
## Further settlements of obverses and integration

The LM 5/10, 6/10 and 7/10 obverses speak of a minimum of zero, without a condition of positive injury. Unlike the base of the Merry obverse (7/11: condition of injury at least 1), allow you to separate the Guide also with zero injuries. Example: the 0 tile with reveal, Boromir separates and the new Guide Gimli can join the same group. Test: `CouncilHuntTests.AlternateGuidesMaySeparateAtZeroDamageUnlikeBaseHobbits`.

Art of Disappearing queues back only after actual elimination; the preview of losses does not trigger the decision. The queue is handled at the boundaries of command, the effect step and battle, before the further losowaniem/wyborem. Each Hobbit selects its own region, with the written continuation of the previous decision. The cancelled character's abilities exclude return. The interpretation of the location according to the physical position of figure and normal traffic (FAQ p. 3): the neighborhood counts from the region of its army; the attacking army remains in the exit region until the winning progress. The tests include both sides of battle, the joint elimination of the two Hobbits and the actual Black Breath program.

The names on the base card also refer to the selected alternative, without giving its basic capabilities: Athelas, Andúril, Challenge, Grey Company, Stewards, There and Back Again and card requiring Hobbit to use the correct identity. The level 1 remains a level to the effects of combat/Hunt; the route projection shows the level of 2 traffic. `DieLocation.FellowshipBox` is a separate public place of dice; the Unity resource counter shows them when such die occurs.

Checkpoint capability: `t26-checkpoint-abilities.trx` — **555/555 PASS**, 0 filter omitted, Council/Character/Lords/Fellowship/BaseCorpusTests. filter includes 8 tests HTTP/PostgreSQL Council (10 re-use), return of both Hobbits, private condition partner, zero damage, three separated Guide, stats, Champion, policy, existing card and Strider/Vilya/Narya. module Council has been grouped together in `src/Wotr.Domain/Lords/Council`; domain, DTO, transport and Unity have remained separate layers. Manifestivity and full regression remain the next step of acceptance.

Unity EditMode after changing contracts and counter Fellowship Box: **118/118 PASS**, 0 after missing (`bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t26-editor.xml -logFile .artifacts/t26-editor.log`, 00:48:06–00:48:08 UTC).

<a id="odbiór-t26--2026-10-06"></a>
## acceptance T26 — 2026-10-06

The Manifesto [six cards](../../content/council-character-implementation-manifest.json) includes 23 runtime capability, 23 source capabilities, all 30 `verificationCases` SRC-04 and 8 actual test methods HTTP/PG. A common manifesto test checks the exact division of 8+6, the set of identifier and the existence of test methods. The SRC-04 index has `implemented=true` for these six obverses.

Checks on macOS 15.6.1 / Apple M1 Pro, .NET SDK 10.0.401, PostgreSQL 17.11 and Unity 6000.6.0f1:

- `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
  --logger 'trx;LogFileName=t26-full.trx' --results-directory .artifacts/test-results` — **2058/2058
  PASS**, 0 omitted; domain, sources, architecture, privacy, real HTTP/PG and frozen base body.
- `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t26-editor.xml
  -logFile .artifacts/t26-editor.log` — **118/118 PASS**, 0 omitted.
- `bash scripts/test-client.sh -testFilter
  'Wotr.PlayTests.NetworkTests;Wotr.PlayTests.T23FullGameNetworkTests'` — **2/2 PASS**, 0 omitted;
  actual Kestrel/PG, two Unity clients, reconnect and full batch **base** with production
  randomness. 00:52:35–00:53:40 UTC, 65,26 s; log copies/XML `t26-play*`.
- `python3 scripts/tasks.py validate` , `python3 scripts/check_catalog.py` , `python3
  scripts/source_lords_characters.py` , `python3 scripts/source_lords_cards.py` , `git diff --check`
  — PASS. `scripts/test.sh` additionally performed `source_cards.py` : unchanged 96 cards and 14
  form base game.

Commission checkpoints: `b3fce6d` (setup/tokens), `1406aaf` (capacity, return, base exceptions cards, Fellowship Box counter). Changes include `Lords/Council`, character directory, public integrations Characters/Hunt/Battles/Effects/Turns, projection/DTO, server registration, testing and documentation of rules/contracts.

The boundaries remain open: full start Lords requires T27; interactions with Kings/Fate and views of expansions are received in further tasks. Manual user and Windows tests were not performed in T26. The next task is determined by `ready --stage D` according to ROADMAP; the priority is given to packages of Lords content. No private data or test artifacts have been added to the repository.
