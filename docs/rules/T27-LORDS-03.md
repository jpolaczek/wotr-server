<a id="t27-cards-03--madness-and-horror-i-we-shall-get-it"></a>
# T27-CARDS-03 — Madness and Horror and We Shall Get It

<a id="źródła-i-kontrakt-przed-implementacją"></a>
## Sources and contract before implementation

The reverses LM 26/24 and LM 5/5 ed SRC-03 ; full rules and cases include [index](../sources/lords-card-index.json) . LORDS p. 22 defines automatic input card special, FAQ 1.2 p. 3 — exchange tiles against Hunt protection. Status and checks performed are in the to-do register.

Madness and Horror is an Army event, paid for one dice Army or Event (also part of Army with Army/Muster Sauron must be At War. Shadow He's choosing an army. FP in the region Stronghold **Saurona** or adjacent. Server throws three dice , 5–6 it gives a hit; FP distributes the loss of the event. Companion They survive the loss of an army, the ordinary commanders do not. Then they recruit to two regular Sauron from the reserve. For the purpose of besieged inside the acquired Stronghold Saurona recruitment takes place in the same region, on the besieging side; in other cases Shadow indicates one free region adjacent **to the target**. Target status and branch siege are preserved against losses, also with total elimination. No reserve or free target does not undo the previous effects. Overshooting the stack solves the usual final window of the event.

The neighborhood confirms [Chapman 35552456](https://boardgamegeek.com/thread/2482588/article/35552456#35552456): Eagles' Eyrie attack allows you to recruit in free Mount Gundabad, Old Ford, Carrock; Mount Gram does not adjacent to the target. The definition of the free region comes from BASE pp.10 and p.31: it also includes the region of its own army besieging with the enemy Stronghold, but not its own besieged garrison.

Swarm of Bats, initiative 0, tho-shares the core program: cancels card FP. Bonus +1 to all dice to the spread Leadership only works if FP has not played card. Inactive or cancelled card has still been played. The first defender's race is a draw for the first; the cancellation and the reset of the round remain in the existing runtime battle.

In Shall Get It is a special event outside the deck and hand, without fighting and cost dice. Removing Sméagol teaches them automatically; replacing the last Sméagol Gollum is an exception. card can wait on the table at another Guide. Gollum is a condition**of entry and durability. [Chapman 21001986](https://boardgamegeek.com/thread/981433/article/21001986#21001986) also confirms the removal by Wind from the West. Safe Paths works according to its obverse. We do not record a fictional play from hand or combat program to fill the manifesto.

After selecting the tile and priority Sméagol Shadow with Gollum as Guide, he can reject We Shall Get It to replace the tile. Refusal preserves card for later ([Chapman 12467544](https://boardgamegeek.com/thread/981433/article/12467544#12467544)). The replacement occurs before Mithril/Nenya, injury determination and protection; the ability then used concerns the new tile. The first tile remains out of the pool during the replacement selection and returns after action, according to the common Mithril cycle and the principle of the tiles used ([Chapman 38652169/38653463](https://boardgamegeek.com/thread/2741857/article/38652169#38652169)). We do not repeat the Wizard's Staff window before the replacement selection.

<a id="stan-widoczność-i-ui"></a>
## Status, visibility and UI

Madness preserves the region of the target and branch of recruitment in the effect frame. Every choice of army, loss and recruitment site returns after reboot. Uniform regular figures are interchangeable, do not require additional choice. The server validates the map, reserve, cost and decision-holder again at approval.

Hunt receives a clear stage of the Shadow reaction before replacing FP, only in Lords. Options are based on a public table and Guide, never on the hand. Pool/RNG remains private; both views show the actually selected tile, unresolved injuries and waiting for the right side. Retry does not repeat draws.

Unity uses a map, existing figures losses and constant trays. card special has full text and optional response, without an empty Combat tab. We do not add numbers or scroll decisions. Manifesto clearly distinguishes card Event/Combat from SpecialEvent by mapping all source rows for testing.

<a id="plan-weryfikacji"></a>
## Verification plan

- Madness: War/pok The region, all costs Stronghold and neighborhood, not allowed Stronghold
  Isengard; three throws, threshold 5 , loss of event, elite and lack of exchange; destroyed
  garrison; zero/au reserve density, free targets and stack overflow, legal options/podgl No ovum RNG.
- Swarm: both roles, no card, cancellation, card ineffective, draw Daring initiative 0, actual
  reroll and reset round.
- In Shall Get It: both entry and exception of single Sméagol, missing in decks/Hands/Combat, wrong
  Guide and window, conscious refusal, actual exchange, Sméagol after departure, Mithril/Nenya and
  protection of new tile, additional Hunt, Wind/Safe Paths and empty pula/refill.
- Each new window: HTTP/PostgreSQL reboot before/after decision, retry, second player, random preview
  and both projections. Unity Editor and PlayMode on the real server, geometry of basic resolutions
  and scales UI.
- Full .NET and frozen base body; manifesto with real TRX; structure overview, validation of
  documentation and local commits.

The manual acceptance user remains deferred. acceptance full Lords module and common configurations does not automatically result from acceptance this package.

<a id="checkpoint-domeny"></a>
## Checkpoint domains

2026-10-06: Madness and special reaction are connected to `EffectCatalog.All`; Swarm and Hunt exchange share base programs. `MadnessTarget` saves region and primary siege; `ShadowReplacement` precedes FP exchanges also after replacement selection of Mithril/Nenya. New frame field omits null in JSON.

`scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false` with `LordsMadness|LordsWeShallGetIt|LordsSwarm|LordsNenya|LordsWeWontGoBack|BaseCorpusTests` filter (each member as `FullyQualifiedName~…`): **149/149 PASS, 0 skip**, `.artifacts/test-results/t27-03-core-final.trx` report. Work runs have detected fixture errors: hostile Carrock, regulars among losses as legal elite exchange, reroll performed only after the selection of both sides and the Sorcerer before the new round. The scripts have been corrected, without changing the rules of the game. They remain HTTP/PG, manifest, Unity, full regression and acceptance documentation; checkpoint does not finish the task.

Checkpoint integration: **2287/2287 . NET PASS** ( `t27-03-full-final.trx` ), **129/129 Editor PASS** ( `t27-03-editor-final.xml` ) and **4/4 PlayMode PASS** ( `t27-03-play-final.xml` Eight methods of persistence include 21 decision reboots. Manifesto 14 cards indicates 148 methods and 248 the previous course of the PlayMode had 5 PASS/1 FAIL : the test expected an open card after entering the event already started; it was corrected by clicking the public source button. `NetworkTests` and `T23FullGameNetworkTests` They've been through this. NET had 2286 PASS/1 FAIL as a result of waiting for no win in the final field of Mordor; the wait was improved on RingDestroyed The rules of the win remain unchanged.

The 1366×768 image review on the 1,25 scale confirmed visible controls and text cards, but showed the repetition of region names and general confirmation of Madness. Correction of UI before acceptance: no duplicate region, details of the effect from the authorized option and the proper designation of the currently settled card.

<a id="odbiór-końcowy"></a>
## acceptance final

All the following commands were executed by 2026-10-06 on macOS, .NET 10.0.401, PostgreSQL 17.11 and Unity 6000.6.0f1. `test.sh` used local SDK and `DOTNET_CLI_HOME=$PWD/.artifacts/dotnet-cli`, disabled telemetry and workload updates notifications. The tests did not require manual user play.

| Verification | Actual result |
| --- | --- |
| `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t27-03-full-final.trx' --results-directory .artifacts/test-results` | **2287/2287 PASS**, 0 skip, 1m22s. Domain, HTTP/PostgreSQL, 21 reboots in 8 methods T27-03 and frozen base body. |
| The same runner after correction of the recruitment label, `--filter 'FullyQualifiedName~LordsMadness\|FullyQualifiedName~LordsCards03PersistenceTests\|FullyQualifiedName~LordsCardManifestTests'`, TRX `t27-03-labels.trx` | **33/33 PASS**, 0 skip, including the re-21 reboots. |
| `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t27-03-editor-labels.xml -logFile .artifacts/t27-03-editor-labels.log` | **129/129 PASS**, 0 skip, after the last changes to UI. |
| `bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.LordsFinalCardsNetworkTests'` | **4/4 PASS**, 0 skip, `t27-03-play-labels.xml` report, `t27-03-play-labels.log` / `t27-03-server-labels.log` logs. Actual Kestrel, PostgreSQL and two Unity clients. |
| `python3 scripts/lords_card_coverage.py --results .artifacts/test-results/t27-03-full-final.trx` | **14 cards, 148 methods, 248 cases PASS**; all printed terms and instances of sources assigned. |
| `python3 scripts/source_lords_cards.py`, `python3 scripts/check_catalog.py`, `python3 scripts/tasks.py validate`, `git diff --check` | **PASS**: 13 of ordinary events + 1 SpecialEvent, deck 108, parcel 6+6+2; 30 of families / 34 of window, 88 of tasks and consistent documents. |

The first PlayMode with `LordsFinalCardsNetworkTests;NetworkTests;T23FullGameNetworkTests` filter additionally confirmed connections and a full batch of the database (both PASS); the above error of the inspector opening test was corrected and checked in two consecutive 4/4. runs We do not present the initial 5/6 as a full PASS.

Four tests are performed **32 system controls** (some repeated views), save **28 different drops** of seven views in 1366×768 and 1920×1080 on a scale 1 and 1,25 Controls and full text of the inspector are located, the map is more than half the height, no ScrollView and the number fields in the decision. Small images were watched screen/scale   1,25 for the choice of army, loss, recruitment and card special; after re-training, after re-fixing the labels. The result indicates ‘Place 2 Sauron Regulars”, without repeating the target name. Screenshots remain in ignored `.artifacts/screenshots/lords03-*`.

The revised modules: `Domain/Lords/Cards`, Hunt and the effect frame contract, manifesto and source index, Unity inspector/st/task, scenarios/tests, CONTRACTS, TESTING, CODE_QUALITY, rule and inventory directory UI. Checkpoints: `ad2b299` (domain), `e77170d` (integration and manifest); acceptance has a separate commit. The structure review is in CODE_QUALITY. No manual user test, Windows build or acceptance full start of Lords has been performed. They remain in the right tasks; the next priority is the aggregate **T27**.
