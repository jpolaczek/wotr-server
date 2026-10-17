# T21-SH-STR-04 — Shadow Strategy 19–24

<a id="źródła-i-zakres"></a>
## Sources and scope

Viewed Avers Second Edition 19–24/24, pinned in [index SRC-01](../sources/base-card-index.json). All six events have a Muster type. [BASE 2.4.1](../sources/materials/base-rulebook-v2.4.1.pdf), p. 8–10, 22–23, 26–32: free region, maximum possible execution, reserves, recruitment by events, excess, struggle and initiative. Geographical objectives have been compared with [board](../sources/materials/board-reference.jpg).

[Kevin Chapman, 34892507](https://boardgamegeek.com/thread/2431471/article/34892507#34892507) confirms that with two regulars in the Orcs Multiplying Again reserve, it is possible to separate them 2+0 or 1+1. The order of regions in the same sentence does not give them priority. The reading of API with 2026-10-05, author of 34645, the recording of `.artifacts/source-audit/sh-str04-thread-2431471.json`. The conditions of Onslaught and common effects of both armies use [package arrangements 01](T21-SH-STR-01.md).

<a id="wydarzenia"></a>
## Events

| ID / card | Condition and required enforcement |
| --- | --- |
| 19 Shadows on the Misty Mountains | Without condition At War. Two Sauron units, either regular or elite, and one Nazgûl. All available recruits in one selected region: Mount Gram or Moria. |
| 20 Orcs Multiplying Again | Without the At War condition. Three regular Saurons in Dol Guldur and three in Mount Gundabad. At the reserve shortage, the player separates all available figure, not exceeding three in each region. |
| 21 Horde from the East | Southrons & Easterlings At War. Five regulars of this nation in one free region of its borders on the eastern edge: East Rhûn, South Rhûn, Khand or Far Harad. |
| 22 Monsters Roused | Without condition At War. After one regular Sauron in Angmar, Ettenmoors and Weather Hills and one elite in Trollshaws. The common regular deficiency separates the player; the unavailable elite does not block regulars and vice versa. |
| 23 Musterings of Long-planned War | All three nations Shadow At War. Five regular Southrons & Easterlings in Gorgoroth and five regular Sauron in Nurn. These are two different reserves; the absence of one does not block the other. |
| 24 Pits of Mordor | Sauron At War. After two regular Saurons in three different original Stronghold of this nation. Goals: Barad-dûr, Dol Guldur, Minas Morgul, Morannon, Moria, Mount Gundabad; current site siege and control continue to apply. |

The cost of each event is one legal die Muster/Army-Muster used as Muster or Event, according to server options; card disappears from hand. The recruitment plan itself does not use another dice. Event paid dice Event can only run Jerkír after the entire procedure, also after the excess has been settled.

Recruitment with card bypasses the usual At War requirement only where the obverses do not introduce it, and allows their own besieged garrison. Also allowed are the own besieged in the field; figure goes to the right side of siege. An enemy field army is not allowed or the captured empty settlement, which is not free. Protected by the event regions do not block recruitment of the current army anymore, because the ban on entry is not a prohibition of recruitment.

Mandatory execution includes maximum possible figures with current reserves, legal targets and text limits. The player retains the choice of Common/elite and a limited pool chapter. Example: Pits with five regulars gives 2+2+1 in three Stronghold; with two allows 2 in one or 1+1 in two. We do not demand to indicate empty regions for figures that is not present. The common choice is atomic; recruitment in the first region does not take the resources of the later.

The player points to figure to be removed from the entire army in a given polu/garnizonie. Sauron can choose an individual of another own nation. The field has a limit of the, a garrison of five army units; Nazgûl does not use this limit.

The lack of all reserves or legal sites causes empty execution after the printed condition is met. The obverse Horde requires At War and the selection of the free region describes the effect. We do not accept an additional ban on playing in the absence of an unsupported goal with an unreserved adverbal sentence of almanac: the source are obverses and BASE p. 22, not secondary development.

<a id="zastosowania-bojowe"></a>
## Combat uses

| Cards | Effect and initiative |
| --- | --- |
| 19, 20 | Onslaught (7): a common voluntary loss program 0–4 after normal losses, then a separate throw for the hits you take, 4+ hits. The own total elimination does not cancel the additional throw. |
| 21 | Deadly Strife (3): both armies +2 Combat and Leader re-roll. |
| 22, 23, 24 | Desperate Battle (3): both armies +1 Combat and Leader re-roll. |

None of these applications have an additional condition for presence. The usual natural 1/6, the limits dice, the order of initiative, the priority of the defender at the draw and the cancellation of card remain unchanged. Onslaughter does not inherit the modifiers of normal throws or reroll. Desperate Battle combines with the modifier of the second played card; it does not extend to separate attacks.

<a id="architektura-i-interakcja"></a>
## Architecture and Interaction

The 03/04 recruitment describes the common `RecruitmentBatchStep`: the public nation/region/kind/count limits and the maximum number of regions. It uses the existing `ChooseEffect` and `CommandPayload.Placements`, containing the actual `UnitId`/`RegionId`. It does not accept the size of the reserves, limits or siege pages from the client. The server checks the current maximum legal plan, unique figure and any target before mutation.

The tray shows available figure and the preset locations; regions indicate the map. Each figure can be moved or withdrawn from the plan before one approval. No numbers and scrolling fields. Changing local selections refreshes existing controls; it does not play the entire screen. Click and drag perform the same draft. Selection in the same revision survives the reboot; new server decision cancels the previous draft.

Client implementation splits responsibility into `EventRecruitDraft` (choice and withdrawal of deployments), `EventRecruitCapacity` (can the maximum required plan be completed yet) and `EventRecruitChoices` (fixed buttons and refresh them). Example shared with the package 03 : New Power with a reserve of two regular and two elites requires four figures Regular in Orthanc would block the place for the required elite, so this step draft is not available. You can still choose any allowed breakdown of regulars between the two Dunlands. The filter does not deploy anything as a player; it only removes the roads the server will not accept as a full plan. The calculations operate on a limited list of targets and reserve representatives on change draft Without searching the entire state in every frame.

The region can be pointed out before or after figure. The progress symbols show the remaining locations, and the marked figure have separate return buttons for the reserve; the map shows ghosts in the target regions. The Back button restores the earlier draft. The controls retain identity after adding and removing figure. The `ArmyFigures` used by the 02/03 packages also selects the region by map, but retains multiple selection of figures when moving to the next region.

<a id="weryfikacja"></a>
## Verification

Scheduled: for each card legality/illegality of cost, window and condition; each effect and boundaries of the reserves/goal; no political activation by recruitment; both sides of the siege; other control; excess and Jerkír. The deficiency of the common pool, the mandatory maximum and the choice of mixed composition have separate scenarios. The fight will check all six ID, natural 1/6, both modified throws, summing up, canceling, Onslaughter zero and victim with subsequent attack.

Each real window: save, reboot, same receipt and retra without double recruitment; both projections, no other hand and RNG. Unity: actual server, map/figures/undo/reconnect, no scrolling and numerical fields, minimum visible 32 px in 1366×768 and 1920×1080 in 100% and 125%, screenshots and visual overview. Results will be added after centrally coordinated runouts; this document does not mean that the tests are passed.

Added sets: `ShadowStrategy04EventTests` , `ShadowStrategy04RecruitmentTests` , `ShadowStrategy04CombatTests` , `ShadowStrategy04PersistenceTests` , `EventRecruitDraftTests` , `ShadowStrategy04NetworkTests` ( 10 travel of two clients) and `ShadowStrategy04LayoutTests` ( 11 production projections × 4 sizes/scale Layout also includes excess after manual plan, full hand six cards , full plan of six recruits and confirmed steps of the event. Common widget checks the parallel package 03 The first central domain course detected battle preparation errors in the tests (number of samples RNG and lack of Companion for the Daring Defiance); fixtures were corrected. No rules were changed to match them to the wrong test.

Central Editor of 2026-10-05 : **72/72 PASS**, including five new ones `EventRecruitDraftTests` ; record `.artifacts/sh-str02-04-editor-first.xml` Review before Play detected no base `row` In `BoardScreen.Row(customCss)` , therefore both new control groups have a transparent compass `flex-direction: row` and `flex-wrap: wrap` Confirmed steps of the event during `RemoveExcess` keep 33 px, also after class is disabled `card-planning` Global button geometry has not been changed. acceptance the full regression Play remains open.

Full domain and server: **1632/1632 PASS , zero omitted**, central `bash scripts/test.sh --no-restore --logger 'trx;LogFileName=shadow-strategy-full.trx' --results-directory .artifacts/test-results` , log `.artifacts/sh-str02-04-full-domain.log` , TRX `.artifacts/test-results/shadow-strategy-full.trx` The first focus Unity confirmed **10/10 new network flows 04** Layout stopped at the nation's headline during `RemoveExcess` : When group selection is disabled, the button is inactive. 24 px replaced by a label; real buttons and bookmarks retain the required click area. Full Misty drop showed a small vertical stock before plans with two lines figures The name of the region is therefore sent to the pallet line, and the shoveled intervals are smaller; figure , previews and size controls remain visible. Another layout and regression of this amendment are still required.

Fourth central focus of the layout: **44/44 package layouts 04 PASS**, `.artifacts/sh-str02-04-layout-fourth.xml` ; the remaining joint run error concerned a new stress case package 03 The simple decision buttons battle use a shovel 33 px to be rounded to 125% continue to provide the actual minimum 32 px; figure and card retain larger sizes. Visual overview of full Pits and manual Misty→oversize in 1366 × 768/125% confirmed the readability of the map, all locations and six cards Common renderer confirmed figures was subsequently packaged due to a confirmed overflow of New Power; is required acceptance This change in Editor, re Layout and full Play.

After grouping history, central Editor: **74/74 PASS , zero omitted , `.artifacts/sh-str02-04-editor-final.xml` ( 2026-10-05 18:04:58–59 UTC Fifth focus of layouts:** 3/3 PASS **, `.artifacts/sh-str02-04-layout-fifth.xml` ( 18:05:05–52 UTC ), all 132 packages 02–04 , again** 44/44 package layouts 04 **. Final surveillance `shadow-strategy04-sh-str04-misty-excess-1366-1.25.png` : Shortcut `◆2 N1 · Moria` preserves the types and number of recruits, primary and new figure Both nations are visible, the approval button fits with the backup, and the map splits the field and garrison. The full regression Play remains the last open client check.

The actual coordinator commands for the last Editor and layouts (layout plays production projections saved by the generator; a separate full `test-client.sh` checks the running server):

```sh
bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/sh-str02-04-editor-final.xml -logFile .artifacts/sh-str02-04-editor-final.log
WOTR_CLIENT_TEST_CONFIG="$PWD/.artifacts/client-integration.json" bash scripts/unity.sh -runTests -testPlatform PlayMode -testFilter 'Wotr.PlayTests.ShadowStrategy02LayoutTests;Wotr.PlayTests.ShadowStrategy03LayoutTests;Wotr.PlayTests.ShadowStrategy04LayoutTests' -testResults .artifacts/sh-str02-04-layout-fifth.xml -logFile .artifacts/sh-str02-04-layout-fifth.log
```

Full Central `bash scripts/test-client.sh` , 2026-10-05 18:06:24–18:13:07 UTC : **148/149 PASS , zero omitted , `.artifacts/sh-str02-04-full-client.xml` and `.log` All of them. 11/11 Play Pack Tests 04** passed: the flows of two clients with a real server and 44 Deal. TRX domains also confirm separately **73/73 `ShadowStrategy04` PASS**, inside the previously given regression 1632/1632. The only Play failure is `ShadowStrategy02NetworkTests.CorsairsReliefKeepsGarrisonSeparateUntilRetreatThenCombines` : Over-age after completed rescue continued to render card and dice closed battle . acceptance this separate package window 02 remains open. Play not listed as a complete pass; all your own parcel criteria 04 and regressions of current flows have passed.

<a id="odbiór-paczki-i-dalsza-pomoc"></a>
## acceptance packages and further help

Final domain course after optimization of marches 02 : **1638/1638 PASS**, still **73/73 parcel cases 04 PASS**, without omission. Command: `bash scripts/test.sh --no-restore --logger 'trx;LogFileName=shadow-strategy-full-final.trx' --results-directory .artifacts/test-results` ; log `.artifacts/sh-str02-04-full-domain-final.log` Criteria six cards 04 are fulfilled: both applications, sources and manifesto, preview, validation, restart/retry , 10 network flows, 44 systems and appropriate tests Editor as a result 74/74. No new standalone or measurement builds were made FPS in this package; hand-held acceptance the user remains deferred.

Structure overview: `ShadowStrategyCards04` declarations use the public common `RecruitmentBatchStep`; UI splits the validation of draft, calculates the remaining capacity, view recruits and view confirmed steps. No domain dependencies from Unity have been added. Limited counters and dictionary maps replace the calculation of the combination; local layout retains controls and does not rebuild the entire screen.

Support with separate [packs 02](T21-SH-STR-02.md) error: After the diagnostic image, the presentation has been changed only for `Battle.Stage=Complete` and `DecisionKind.RemoveExcess`. The tray retains the title of excess and figure, and the report remains under Last Battle; the active battle still shows context. Two `BattleExcessTrayTests` and the scenario of the rescue are in a separate, ongoing acceptance 02. Their preparation has not been counted as a result of the 04 package tests. The next assigned work is audit of Fate sources in `SRC-07`.
