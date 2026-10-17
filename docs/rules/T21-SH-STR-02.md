<a id="t21-sh-str-02--strategy-cienia-712"></a>
# T21-SH-STR-02 — Strategy Shadow 7–12

<a id="źródła-i-granice"></a>
## Sources and boundaries

Full observatories of `base.sh.strategy.07`–`12` with [index SRC-01](../sources/base-card-index.json) were viewed. Rules of traffic: BASE 2.4.1 p. 29; mandatory maximum partiality of p. 22; recruitment by events p. 27. Almanach p. 32–35 and 45 serves exclusively as an index for source statements. Copys of API are in `.artifacts/source-audit/sh-str02-thread-*.json`; authorship of Kevin Chapman (34645) was conducted.

- [Gather/Lengthens: targets from the start state
  action](https://boardgamegeek.com/thread/2966601/article/41116946#41116946), [ban on returning to
  the source after division](https://boardgamegeek.com/thread/2966601/article/41117105#41117105),
  [The beginning of action and the next movements of the
  army](https://boardgamegeek.com/thread/2966601/article/41126427#41126427) and [two
  armies](https://boardgamegeek.com/thread/2457664/article/35231381#35231381).
- [Traffic through settlement and stack control across
  action](https://boardgamegeek.com/thread/2432138/through-day-and-night), BASE p. 29.
- [Corsairs: traffic before
  battle](https://boardgamegeek.com/thread/2429095/article/34837037#34837037), [temporary presence
  of three armies](https://boardgamegeek.com/thread/2449665/article/35128143#35128143). The detailed
  order and contract are documented separately before the Corsairs implementation.
- [Rage: No recruit also disables
  traffic](https://boardgamegeek.com/thread/2264757/article/35274846#35274846); at least one
  available figure is enough. We do not add this as an unprinted condition for playing card.
- [A border not exceeding does not mean a
  neighbourhood](https://boardgamegeek.com/thread/1258801/meaniing-adjacent-shadow-character-card-11-rage-du),
  BASE p. 10.
- [Swarm vs. Daring: Initiative and White
  Rider](https://boardgamegeek.com/thread/2438936/article/34980940#34980940), [Uneffective card
  remains played card](https://boardgamegeek.com/thread/2438936/article/35408166#35408166). Daring
  can be played at White Rider, but you can not sacrifice its Leadership twice.

<a id="wydarzenia"></a>
## Events

Owner: Shadow. card 7–10 use Army cost, 11–12 Muster, each also Event. Normal validation of dice, pages, windows and hands precede mutation. After payment, the next election does not use the additional dice; Jerkír only chooses after the entire procedure.

| ID | Legality and enforcement |
| --- | --- |
| 07 Shadows Gather | One army, to three regions. The target region included another unobsessed army Shadow at the beginning of action. The own source is not such a goal. The composition specified before the move; it does not select or leave figures on the way. Each edge and national restriction checked normally. Passing through the empty settlement acquires it. |
| 08 The Shadow Lengthens | Two different source armies, each to two regions; one only when there is no legal movement of the other uninvolved army after its declared route. The targets are eligible according to the initial state, but the movements are carried out in the selected order. An army in the target region can leave within this action. Neither the figure nor the source army moves twice. Stack limits only after all movements. Maximum partiality does not create an additional unprinted play condition. |
| 09 The Shadow Is Moving | All nations Shadow At War. To four different armies of one region each; a simple move, without additional condition of another army in order. Joint approval and limit after all movements. |
| 10 Corsairs of Umbar | Southrons & Easterlings At War. One army from the Umbaru field lands in the Gondor region before battle. Selection of participants, excess after connection, shelter, relief and mandatory continuation require separate states. The army already besieged in Umbara cannot leave. Details in the Corsairs document. |
| 11 Rage of the Dunlendings | Isengard At War. In the free neighbouring region to North or South Dunland recruits to two available regular Isengards, mandatory to maximum possible. Then optionally transfer to this region to four Regular/Elite Isengards from both Dunlands. Direct movement, without Nazgûla/Minion/other nations. With zero recruitment there is no purpose of later movement; card ends without it. The intransiable border to Orthanek does not give a legal purpose. |
| 12 Return of the Witch-king | Witch-king present in the game. First moves to Angmar; then recruits two regular and one Sauron elite according to the availability of reserves and the region's legality. No recruits do not undo the shipment made. |

Gather/Lengthens opens the choice only when there is legal movement; in the absence of effects card can be paid no-op according to BASE p. 22. Lengthens requires two possible moves: printed "Move two" and the obligation BASE p. 22 do not authorize any omission of the second available army. This literal interpretation, no additional resolution of FAQ. Moving prints "up to four", so it also allows a conscious ending without movement. We do not shorten the declared route or change the composition once after approval. From the initial state comes only a set of eligible targets; the current availability of the region is checked after previous movements. Local draft of all movements is reversible to one commit.

We do not extend the ruling on unchanging targets to simultaneous traffic. Chapman 41126427 distinguishes the beginning action from the start of the movement of each army; BASE p. 27 and 32 preserves the usual consequences of the besieging departure. Therefore, in order A→B, the first move can free the garrison FP That will block the route of the second army. figures In different initial sources, it is not a proof of the feasibility of both movements. Example: one besieging each Shadow in Moria and Lórien, each above the garrison FP . Movement of Moria→Dimrill Dale→Lórien frees FP in Moria; the second army cannot return there in a move. Full replacement is illegal, one move is the maximum execution. Leaving the legal rear guard can change the score and remains the player's choice. `LengthensCannotSwapTwoBesiegersThroughFreedEnemyArmies` and `MovementOrderMustPreserveAnExistingSiegeBeforeBesiegerLeaves` they are fixing the case.

Gather options also include a route back on the way through its own source region, if the player leaves an army there supporting siege. Two units of SH in Dol Amroth can split: one remains at the garrison FP, the other goes through an empty Lamedon, returns through Dol Amroth and ends up with another army in Anfalas. An attempt to complete the stack would release the garrison and would be illegal. The generator additionally checks the single variant with the unit leaving, keeping the full list of figures to choose from; it does not choose for the player of this guard. `GatherOffersAReentryRouteWhenAChosenRearguardCanKeepTheSiege` binds the offer with the actual subset.

<a id="efekty-bojowe"></a>
## Combat effects

| Cards | Effect |
| --- | --- |
| 07/08 Mûmakil | Play Condition: Southrons & Easterlings participating elite. 3: +1 Combat initiative, without a re-roll bonus. 5 initiative: when the sum of your own hits exceeds your opponent, an additional one hit; the comparison takes into account special FP hits before Combat. The play condition is not re-checked after previous losses. |
| 09/12 Swarm of Bats | 0 initiative: cancels card FP. If FP did not play card, give +1 to re-roll. Playing card without effect still excludes this bonus. When you tie with Daring first is the defender; the cancelled Swarm is not working. |
| 10 Deadly Strife | 3 initiative: both armies +2 Combat and re-roll, a common proven program with 01 package. |
| 11 Relentless Assault | Initiative 3 : optional 0–2 your own hits before Combat; actually paid hits give + 1 each to Combat. Joint voluntary loss program from the package 01. |

Natural 1/6 and the limit dice retains normal meaning. Special throws do not modify the bonus to Combat/re-roll.

<a id="kontrakt-zapis-i-ui"></a>
## Contract, write and UI

The traffic plan includes the real ID figures, source, target and full route of each group. The order `Moves` determines the order of execution. The new `ArmyMoveChoice.Route` field has an empty default value; the previous single marches retain the existing `CommandPayload.Route`. The server rejects mixing both representations in one tab and does not trust the limits from the client. The recording after the commit perpetuates all movements and the possible selection of excess, without retaliating the movement after the reboot.

Lengthens makes available `Minimum=1` The server simulates it on a copy board and lazyly checks whether any unmoved army has another legitimate move to the initially eligible goal. If so, preview returns `MoreArmyMovesRequired` and requires completion of the plan. If not, accept partial execution. This is an error based solely on public board This is an interpretation of the sequential execution and the obligation to execute a possible part, not Chapman's literal response to a counterexample of siege Entries 41126427/35365813 do not require the choice of the previous route according to the global optimization of the next traffic; we do not add such undocumented limitation. We do not calculate the split combination figures : each candidate takes a qualified, yet unmoved figure the source. Short routes have a maximum of three steps; for the approved plan we copy board Once, and for the sake of checking the next move, we end up after the first candidate found.

The option generator does not list subsets figures After measurement on dense board removed full copy board from typical candidates: generator has already checked the edges by `CanEnter` , the protection of regions, the national borders of the entire route and the actual group of its own figures in the field and mobile characters. Movement retains the same group ID ; national political responses are implemented after action That's why. figure The besieging garrison in the visited region remains there after the group leaves, and the availability of such a region does not deteriorate. Except for the source: the departure of the last unit may release the enemy garrison there. Each return by the source with the enemy garrison continues to use full simulation and additional variant of the guard. The full path also remains for states with their own Leader to retain control of the leaving of the lone commander; the base Shadow such figures There's no way.

This optimization applies only to options. Authorized `Plan` , the order of movements and lazy checking of the Second Army of Lengthens have not been changed. `ShadowStrategy02MarchParityTests.DenseOptionsExactlyMatchAuthoritativePlansIncludingUnfilteredRoutes` compares the exact set of routes and eligible routes offered figures ed `Plan` for all graph paths, also overlooked by the generator: Gather, Lengthens and Moving on board ed 32 armies, three siege , Nazgûlem, Witch-king, non-mobile Saruman and blocking army FP The limited option adds protection of the region and nations against war. Six cases and previous march tests: **PASS 20/20**, without omissions. The invariability of the entire input state is also controlled.

Measurement of options: MacBookPro18 , 3 , ARM64 , 8 cores, 16 GiB RAM , macOS 15.6.1 , . NET 10.0.12 , Release, workstation GC ; 12 warm-up and 60 samples, percentiles of the nearest rank. `GC.GetAllocatedBytesForCurrentThread` ; not forced GC between samples. Results include generating and validating domain options, without HTTP and serializing the projection.

| board / march | p50 before → after (ms) | p95 before → after (ms) | Allocations before → after (B) |
| --- | ---: | ---: | ---: |
| 32 armies / Gather | 55,720 → 7,282 | 59,437 → 10,608 | 69 561 088 → 8 327 424 |
| 32 armies / Lengthens | 7,179 → 0,957 | 7,642 → 1,497 | 10 106 608 → 1 332 904 |
| 32 army, 3 siege / Gather | 55,613 → 8,581 | 59,118 → 11,298 | 70 094 480 → 9 576 208 |
| 32 armies, 3 siege / Lengththens | 7,277 → 0,954 | 8,127 → 1,448 | 10 111 344 → 1 335 976 |

The hash of each source state and number of legal armies, routes and targets are identical before and after the change: 2870 routes Gather and 458 Lengthens on both dense board. The separate cost of `CopyBoard` was 15 632 B; already copies of the legal routes themselves were about 64% routes from earlier Gather allocations. We do not declare on this basis the client delay; times may depend on layer compilation and other processes. Unstable time-serves were not added. Measurement is in `.artifacts/march-profile/baseline.json` and `optimized.json`, and the independent trustness and description of the restoration in the same directory.

Actual commands after optimization:

```sh
bash scripts/test.sh --no-restore --filter 'FullyQualifiedName~ShadowStrategy02March' --logger 'trx;LogFileName=shadow-march-optimized.trx' --results-directory .artifacts/test-results > .artifacts/march-profile/focus.log 2>&1
bash scripts/dotnet.sh run -c Release --project .artifacts/march-profile/Wotr.MarchProfile.csproj -- --warmup 12 --iterations 60 --output .artifacts/march-profile/optimized.json > .artifacts/march-profile/optimized.log 2>&1
```

The map leads to a click of the source and subsequent regions. Miniatures of movements allow you to edit the composition and route; selecting four armies does not require scrolling. Reach and legal endings come from server options. Recruitment and Dunlendings use figures and preview, without entering the number. No strategic goal automation, composition or dice.

<a id="plan-weryfikacji"></a>
## Verification plan

Each card : Legal/illegal conditions, both costs, inappropriate page/window , maximum partiality, no targets/reserves. Traffic: full and partial routes, politics, take over along the way, impermeable boundaries, mixed nations, garrison, ban on re-movement and simultaneous exit of the target. Corsairs: each mode battle pile before card , no interruption, retreat and shelter, three armies and bilateral elimination. Combat: Mûmakil before/after loss of elite, comparison with special hits, Swarm/Daring in both roles and White Rider; regression of common effects. HTTP/PostgreSQL restart/retry all new windows and both safe projections. Unity: real network travel and all contexts in 1366/1920 and 100/125%.

Results are recorded after actual execution; the prepared scenario is not a credit.

<a id="wyniki-integracji"></a>
## The results of integration

Full domain and server after last march change: `bash scripts/test.sh --no-restore --logger 'trx;LogFileName=shadow-strategy-full-final.trx' --results-directory .artifacts/test-results` — **1638/1638 PASS**, zero omissions, 62 s; `.artifacts/sh-str02-04-full-domain-final.log`. Manifesto covers all 96 base ID and both applications of each card. The HTTP/PostgreSQL tests include five 02 package procedures and five Corsairs: playback after subsequent instances of the server, exactly one cost and RNG, both safe projections and resecuring the resecipt.

Final Editor after joint amendments: **74/74 PASS**, `.artifacts/sh-str02-04-editor-final.xml` Package layouts 02 : **52/52 variants**, confirmed again in `.artifacts/sh-str02-04-layout-fifth.xml` (three tests in common, 132 four moving routes were observed at the 1366 × 768/125%: bookmarks, current route, figure The controls actually have at least 32 px, after taking into account scale roundings. The result of the full and final focus run is given below; the previous failed run did not constitute itself acceptance .

Structure overview: cards declarations, march/desantu rules, battle continuation, projection and draft Units are separate modules. Typical options do not copy the entire board and do not generate sub-groups figures; the approved plan retains full validation on the copy. No new stand-alone or measurement of FPS client in this package has been performed; manual user tests remain postponed.

When finished, the frame will be saved battle remains saved until settlement `StackLimits` - I'm not. `Battle.Stage=Complete` and decisions `RemoveExcess` tray shows current excess, without inactive kart/ko Completed battle ; the previous round is available by `Last battle` . No change in the status or order of the server. Separate active case battle with excess still shows its context. Two editor tests protect this distinction, and fourteen seed and network test include a real retreat FP connection 12 figures Shadow and the final return of two figures .

Final fix of the excess window after the rescue: `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/sh-str02-relief-editor.xml -logFile .artifacts/sh-str02-relief-editor.log` — **76/76 PASS**, 18:21:04 UTC . `bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.ShadowStrategy02;Wotr.PlayTests.BattleLayoutTests;Wotr.PlayTests.CardPack04LayoutTests'` — **17/17 PASS**, zero skip, 18:21:15–18:22:01 UTC ; `.artifacts/sh-str02-relief-final.xml` Includes all 13 travel of network packages 02 , all 56 System Organ Class 14 states, both tests of the overall system battle and regression FP Character04. The new actual screenshot before clicking approval shows the correct excess title, 12 figures and the footer available; the previous fight result remains in Last Battle. The earlier only full run error 148/149 it was re-executed and counted. We do not present historical 148/149 as 149/149.
