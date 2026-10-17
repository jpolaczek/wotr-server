<a id="t21-sh-str-03--rekrutacja-i-elity-cienia"></a>
# T21-SH-STR-03 — recruitment and elite Shadow

<a id="źródła-i-granice-interpretacji"></a>
## Sources and limits of interpretation

Range: `base.sh.strategy.13`–`18`, both applications cards Second Edition. The obverses were verified with the `BASE-CARD-SHEET-40183E02` sheet, attached to [SRC-01](../sources/SRC-01-AUDIT.md). The general basis are BASE, p. 22–23 (conditions and execution of a possible part of the event), p. 26–27 (reservations, recruitment and exceptions of events), p. 29–30 (initiative and special attacks). Almanach served exclusively to find sources.

Kevin Chapman, [34892507 response from 21.05.2020](https://boardgamegeek.com/thread/2431471/article/34892507#34892507), confirms that the regions listed in one clause do not impose the order of recruitment. With the two available figure players can break them up 2+0 or 1+1. Only the public "then" introduces order. identifier author in original API: 34645.

We apply the maximum possible performance of the entire clause, also with common reserves. Example A New Power: at two Regular and two Elites, with three available goals, you need to deploy four figure (Regular in Dunland, Elite in Orthanc). Two Regulars in Orthanc leaving both Elite in the reserve do not perform a possible part of the event. This is the application of the general principle of BASE, rather than an example literally considered in Chapman's response. Voluntary partial regular Muster does not create an exception for the mandatory effect of the event.

<a id="wejście-koszt-i-warunki"></a>
## Entry, cost and conditions

Each card requires its own hand, a proper window and a legal cost. card 13–15 have Army symbol; 16–18 Muster. die Event is an alternative cost. The cost system, rejection of card and return to the next action remain common. The absence of figures or legal space does not add the condition of playing unprinted on the card: after fulfilling the condition of the obverse event performs possible effect, also empty.

| Card | Condition | Event |
| --- | --- | --- |
| 13 Half-orcs and Goblin-men | Isengard At War | One Isengard Regular or Elite in the region of any army Shadow. Nazgûl or Minion himself is not an army. |
| 14 Olog-hai | Sauron At War | One Sauron Regular or Elite in the region of any army Shadow. |
| 15 Hill-Trolls | Sauron At War | Two Sauron Regular on board replaced by two Sauron Elite with reserve; they may be in one or different armies. They retain the region and the siege website. |
| 16 A New Power Is Rising | Saruman in the game | Two Isengard Regular in North Dunland, two in South Dunland and two Isengard Regular or Elite in Orthanc; one plan, a common reserve, a limit of two per region. |
| 17 Many Kings to the Service of Mordor | No additional condition | Two Southrons & Easterlings Regular in three different settlement of this nation; does not require At War. |
| 18 The King Is Revealed | Aragon in the game | Five Sauron Regular and one Nazgûl in Minas Morgul; does not require Sauron At War. Strider does not meet Aragorn's condition. |

Legal settlement card 17 at the base: Far Harad, Near Harad, North Rhûn, South Rhûn and Umbar. East Rhûn and Khand are not settlement. At the deficiency figures the choice is only for regions actually receiving meals: up to three regions, up to two figure. No additional regions need to be indicated.

Exchange with card 15 is `min(2, available Regular on the board, Elite in reserve)`. Regular Shadow returns to reserve, Elite hits the site before removing Regular. Do not remove figure without replacing. Replacement and recruitment are not traffic or attack, change politics and do not violate attack/entry bans to the protected region.

<a id="wspólna-rekrutacja-wydarzeniem"></a>
## Joint recruitment by event

`RecruitmentBatchStep` describes the clause by the limits of `(nation, region, figure types, maximum count)` and the optional maximum region counter. Regions filter the existing `RecruitmentRules.EventGarrison`: own garrison can receive meals by event; own army besieging the other's garrison receives figure in the field; occupied by the enemy settlement without its own army is not the target.

The server calculates the maximum possible result from legal regions and reserves. It does not lower it to the stack limit: excess is a separate decision after the event and can also remove figure present earlier. The common reserve is not distributed by region order in the definition. Algorithm also operates counters of nations/types and limits, without generating a combination of figures and regions. A restriction of many regions supports homogeneous, equal limits of one type; a restriction to one region also of complex types of Army and Nazgûl.

command `ChooseEffect` transmits `Placements` with real `UnitId` and `RegionId`, without synthetic identifier or trusted client counters. The server rechecks the uniqueness of ID, availability figures, owner, region legality, type, limit of each target, number of regions and full possible execution. The usual results choices continue to use `Ids`; adapters have neutral default behavior.

<a id="zastosowania-bojowe"></a>
## Combat uses

| Cards | Effect | Initiative |
| --- | --- | --- |
| 13–15 | We Come to Kill | 7 |
| 16–17 | Great Host, a common program described in [STR-01](T21-SH-STR-01.md) | 7 |
| 18 | Relativeless Assault, a common program described in [STR-01](T21-SH-STR-01.md) | 3 |

In Come to Kill, you need your Elite to choose card. After the usual Combat and Leader Reroll, after removing their losses, you throw one dice for the still participating Elite, up to five. Each hits 5+, also during siege and in the first round of the city. This special attack without the reroll of the commanders and without the regular Combat/Reroll. Elite modifiers in the drainage, demoted and removed does not count. Saruman does not double dice for Elite. At Elite age it does not take randomness. Hits open the usual instant loss window. Cancellation card works through the existing battle mechanism.

<a id="decyzje-widoczność-zapis-i-ui"></a>
## Decisions, visibility, recording and UI

All figure reserves, regions and effects of these cards are public. Plan options do not read the opponent's hand. Only the owner of the active selection receives options; the public condition does not reveal the unapproved draft nor the previous private choices of other effects. The recording includes the region/figures/plan/exchange selection window, approved deployments, excess stack and fighting losses. Retry does not repeat the cost or throw.

The map and the fixed tray show possible targets, available types of figures and the remaining places to fill in. Player click figure and the targets; can undo draft and view server preview before one approval. It does not enter numbers, does not scroll a long list of all combinations. Exchangeable figure of one type are grouped; strategic decisions region/Regular/Elite are not automated. A single effect without strategic selection is performed without an empty window.

<a id="weryfikacja-i-przekazanie"></a>
## Verification and transmission

Check plan: all conditions, costs and windows of six cards; lack of reserves, partly unavailable targets and maximumity; any sharing of the common reserve; garrison/oblegaj; duplicates and counterfeits ID; excess after the whole effect; privacy; preview without mutation; proper threshold and order of special attack; reuse of two existing combat programs. Integration requires HTTP and restart/retry with essential windows and automatic Unity tests in sizes/scalach project. The results of the actual checks are recorded below and in the history of the task.

<a id="zrealizowana-struktura-i-scenariusze"></a>
### Structures and scenarios implemented

Definitions cards are in `Cards/ShadowStrategyCards03.cs`, exchange and conditions in `Cards/ShadowStrategyRecruitSteps.cs`, and a special attack in `Cards/WeComeToKillStep.cs`. The common effect module distributes fixed limits (`RecruitmentQuota` and `RecruitmentCapacity`), the calculation of the current legal plan (`RecruitmentPlan`) and the continuation/projection (`RecruitmentBatchStep`). The usual choices with `Ids` use neutral `EffectChoiceStep` adapters; the runtime does not check identifier cards.

`EventRecruitmentOption` has a neutral `MaximumRegions=0` field and the `RecruitmentLimit` list; no `ActionOption.EventRecruitment` means the choice to date. The existing `RecruitmentOption` contains the actual ID available figures. The continuation remains in the existing `EffectExecution`; approved deployments record `EffectSelection` with figure and the region. The automatic execution only applies to the unambiguous allocation equivalent to figures; the strategic selection is not ignored.

Server scripts: `sh-str03-half-orcs-region`, `sh-str03-olog-reserve`, `sh-str03-hill-trolls`, `sh-str03-new-power`, `sh-str03-new-power-shortage`, `sh-str03-many-kings`, `sh-str03-king-excess`, `sh-str03-we-come-loss`, `sh-str03-new-power-excess`. The layout previews include all nine, also a full plan of six figures and a shortage of four. The last scenario approves six real deployments of New Power by the engine, then checks the surplus in Orthanc with two nations and a full track of six recruits. This case includes a condition after a manual plan that was not represented by an automatic King Revealed.

Stress New Power in the central fourth focus of the layout confirmed overflow at six separate, repeating full name figures entries. `EffectTrailChoices` groupes confirmed figure by source and region: symbols show types and abundance, tooltip full names of nations and types. Separate source button and inspection card remain available. Original entries `PlayerView.EffectTrail`, the recording and history of the server do not change. Various sources/regions and non-figure choices remain separated. Controls retain the required hitboxes; the re Layout and full regression will check the new presentation.

The first coordinated course of `.NET` detected case preparation errors, not changing the rules: Westemnet is Town, so placing an army in the fixture must have consistent Shadow control; card Daring Defense requires participating Companion. Fixed tests, scripts and continuation HTTP. The choice of two Hill-Trolls has an open group of `Replacements`, making draft retains two figure also when changing the region on the map. The results of the reruns are recorded below.

<a id="potwierdzony-wynik-domeny-i-trwałości"></a>
### Confirmed domain result and durability

Central course of the coordinator recorded in `.artifacts/sh-str02-04-domain-third-focus.log` and `.artifacts/test-results/shadow-strategy-final-focus.trx` : all **117 cases `ShadowStrategy03` past**, including all six tests PostgreSQL/HTTP `ShadowStrategy03PersistenceTests` . The results of individual cases from TRX This common course contained six failures in the package 04 ; not applicable to the package 03 . `python3 scripts/tasks.py validate` : PASS , 87 tasks. Final regression and acceptance Unity is described below.

Re-central run after fixture and real entry control to Field/Siege/Sortie/Relief: **PASS 300/300** in the 02–04 and Corsairs package, without omissions.

```sh
bash scripts/test.sh --no-restore --filter 'FullyQualifiedName~ShadowStrategy02|FullyQualifiedName~ShadowStrategy03|FullyQualifiedName~ShadowStrategy04|FullyQualifiedName~Corsair' --logger 'trx;LogFileName=shadow-strategy-final-focus.trx' --results-directory .artifacts/test-results > .artifacts/sh-str02-04-domain-fourth-focus.log 2>&1
```

The `.artifacts/sh-str03-manifest-recommendations.json` file indicates the exact existing test methods for each of the six components. Each appeal was checked against the code and result of the `Passed` in the central TRX; the central manifest is supplemented by the coordinator.

Full central regression of the domain and server after optimization of the march: **PASS 1638/1638**, without omissions, along with the 96 cards manifesto, a guard amendment and six comparisons of march options with authoritative plan. Earlier run before optimization passed 1632/1632. Actual final command of the coordinator:

```sh
bash scripts/test.sh --no-restore --logger 'trx;LogFileName=shadow-strategy-full-final.trx' --results-directory .artifacts/test-results > .artifacts/sh-str02-04-full-domain-final.log 2>&1
```

The first focus Unity was not included as acceptance. The New Power test also clicked the newly shown undo button before its first layout; a wait for the layout and direct asertia of the emptying of the plan after removal was added. The common layout with the maximum interface size also required geometry adjustments. The subsequent runs described below confirmed the corrections.

Final Central Editor after adding history group: **74/74 PASS , zero omitted , `.artifacts/sh-str02-04-editor-final.xml` ( 2026-10-05 18:04:58–59 UTC ), including two behavior tests `EffectTrailTests` The fifth focus of three layouts:** 3/3 PASS **, `.artifacts/sh-str02-04-layout-fifth.xml` ( 18:05:05–52 UTC ), total 132 systems, including all** 36 layout 03 **. Again viewed `shadow-strategy03-sh-str03-new-power-excess-1366-1.25.png` : the source and three regional groups are in one row, all eleven figures remains available, approval is entirely on screen, six cards hands readable. Two lines figures Saurona does not require scrolling. No reduced required hitboxes. The result of the full regression Play is described below.

The exact commands of these two common runs are recorded in [verification of packages 04](T21-SH-STR-04.md#weryfikacja). Layout uses the saved engine projections and does not replace network flows; full `bash scripts/test-client.sh` launches the actual server and generator of new seeds.

Full Central `bash scripts/test-client.sh` , 2026-10-05 18:06:24–18:13:07 UTC : **148/149 PASS , zero omitted , `.artifacts/sh-str02-04-full-client.xml` and `.log` All of them. 9/9 Play Pack Tests 03** Passed: eight network and full journeys 36 the only error in the entire course occurred in `ShadowStrategy02NetworkTests.CorsairsReliefKeepsGarrisonSeparateUntilRetreatThenCombines` , after completed a rescue during excess; this is a separate case of the package 02 who continued to present the full context of the completed battle The entire regression as PASS or attributed to this failure to the package 03 . Custom package criteria 03 are confirmed; analysis of the common window 02 It's independent.
