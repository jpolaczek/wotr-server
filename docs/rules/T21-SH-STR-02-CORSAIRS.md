<a id="t21-sh-str-02--corsairs-of-umbar-reguła-i-procedura"></a>
# T21-SH-STR-02 — Corsairs of Umbar: Rule and procedure

The rule document written before implementation under the formally started T21-SH-STR-02. The test results will be added after their actual execution by the coordinator.

<a id="źródła"></a>
## Sources

- Obverses Second Edition `base.sh.strategy.10`, index SRC-01; condition Southrons & Easterlings At
  War, Army/Event, movement of one army from Umbar to the coastal Gondor, immediate limit after
  connection, battle with army FP and ban from ending the attack outside the previously existing
  siege FP.
- BASE-2.4.1, p. 8–10 (regions, settlement), 27–32 (move, army composition, attack, retreat, siege
  and rescue), 34–35 (politics). [board source](../sources/materials/board-reference.jpg): Anfalas,
  Dol Amroth, Lamedon and Pelargir. Fixed `Coastal` Lamedon with false on the true after comparing
  its stretch of coast between Dol Amroth and Pelargir. The joint amendment also applies to Círdan,
  with the regression of `CoastalRegionsFollowPrintedSeaBorderRatherThanNearbyRivers` and
  `CirdanTargetsEveryCoastalArmyButNotLoneLeadersOrInlandArmies`.
- Kevin Chapman, [34837037](https://boardgamegeek.com/thread/2429095/article/34837037#34837037),
  2020-05-15: the army Shadow reaches the region before the start of battle, so its composition
  should be indicated before the defenseman's decision to stay in the field or enter the Stronghold.
- Kevin Chapman, [35128143](https://boardgamegeek.com/thread/2449665/article/35128143#35128143) ,
  2020-06-17 : Time allowed action an exception of three stacks in the region — a besieged garrison
  Shadow , the army FP In the field and the army arrived Shadow.
- Kevin Chapman, [35308092](https://boardgamegeek.com/thread/2449665/article/35308092#35308092),
  2020-07-09: the composition of the attacking army must be established before further decisions
  concerning the attack.
- Almanach v98, p. 34 and general rules for playing conditions p. 46: an index of examples and
  references to the above rules; it does not replace obverse or original decisions. Local readings
  of JSON and text are found in `.artifacts/source-audit/`.

<a id="warunek-i-legalny-wybór"></a>
## Condition and legal choice

The play condition is only applicable to Southrons & Easterings At War. No army in Umbar or no legal movement is not an additional play condition: card and the cost is settled and the unattainable effect is omitted. An army besieged inside Umbar cannot leave. The army besieging FP there can leave, leaving the legal rear guard or releasing siege by the departure of the last unit.

Shadow indicates one group of its own units and mobile characters in the field in Umbar, along with one legal coastal region of Gondor. It must select an army unit; the Nazgûle/characters alone are not sufficient. The choice is a subset of figures, no duplicates, other people's figures, garrison, additional dice, second group and land route. The usual limitations of a nation not being At War and leaving commandants still apply. Movement is direct; land neighbourhood and intermediate regions do not participate in the settlement. Protected from entry the region remains inaccessible.

<a id="kolejność-rozstrzygania"></a>
## Order of resolution

1. The server validates the entire selection. The preview shows the indicated figure, the target,
   the policy, the acquisition of the empty settlement and the possible excess; it does not perform
   movement or draw.
2. After approval, all selected figures leave Umbar and are located in the target region. We release
   the left-over garrison of FPs in Umbar if the sieges have ended. The entrance to Gondor is
   subject to ordinary political consequences.
3. If there is no army FP, we take over the empty settlement. If there is an army Shadow, a
   connection occurs and an immediate public selection of figures over the limit of the. The sieged
   garrison Shadow remains a separate stack with a limit of five.
4. Only after the excess selection is completed we determine the living forces and open battle. The
   state of motion and the continuation stage are serialized. No army FP ends the effect without
   battle and without RNG.
5. FP in an unobsessed field in the undisturbed Dol Amroth receives a simple choice of field or
   shelter only after the arrival army has been revealed. The shelter ends this battle without a
   round; Shadow already remains in the region and begins siege.
6. With the besieged garrison Shadow in Dol Amroth battle is a rescue. He fights only the army
   arrived, and the garrison does not increase strength, leadership or a pool of losses. After
   winning or retreat, the FP garrison comes out and connects with the survivors; we account for the
   excess before the end of the event. If the army arrives, the existing siege remains. Mutual
   elimination of the field army releases the living garrison.
7. If FP has already been besieged in Dol Amroth, the army arrived first connects with the besieging
   army. The limit is valid before the first card battle. Then follows the usual battle siege: one
   round, another with the cost of the elite and the possibility of ending.
8. In the remaining battle there is no decision Shadow Continue/Stop. After round FP retains the
   usual reverse or stay-out choice; no legal retreat passes to the next round. Army death and
   permitted effects ending battle are still supported by the engine.
9. After the settlement of battle there is no voluntary Advance or return to Umbar: figure has
   already landed. The living winner takes over the available settlement; the bilateral elimination
   without the living garrison Shadow does not give over.
10. battle returns to the overarching frame of the event. The Jerkíru haul occurs only after
    battle and all limits, and the end of action only once. The usual political response queue is
    settled at its existing end point action.

<a id="model-i-integracja"></a>
## Model and integration

Dedicated landing rules belong to the Armies module; the definition of card only folds operations. The choice uses the existing Army tray: `MoveArmies`, one group, clicked figure, region on the map and preview. We do not add a numerical field, scrolling or a separate fictional decision to attack after traffic.

The master effect frame keeps the destination and the waiting mark for the settlement of limitu/bitwy. battle saves the clear attack mark after landing; the same tag is responsible for forcing the continuation outside Siege and automatic termination of the region. Do not guess the exception after the title card, the order of loading or comparison of regions.

Implementation: `Armies/CorsairRules.cs` jointly validates and plans traffic and builds options. `CorsairBattleStep` is the next step of the program after the saved `EffectExecution.RegionId` and the decision of excess. `BattleState.ArrivalBeforeBattle` and the safe `BattleView.ArrivalBeforeBattle` field determine the fact of early landing. The small `Battles/BattleLanding.cs` module supports the obligation to continue fighting outside Siege and ending without further question about Advance. The parent of the event retains the cost, Jerkír and cursor also during losses or reactions in nested battle.

With three stacks, you must not start the usual `StrongholdRules.ReconcileRegion` before the fight is over. The existing `Location` + `Garrison` + `BattleForce.Participants` is sufficient to separate the garrison and field army. The continuation of `EffectReturn.Parent` protects Jerkír and restart without re-moving.

<a id="plan-testów"></a>
## Test plan

- At War condition, evil page/cost/window; play without army and without allowed movement, without
  RNG.
- Exactly four Gondor targets, no teleport to another country/region land; Lamedon also for existing
  Coastal data users.
- A correct subset of the mixed army, characters and Nazgûle; no duplicates, foreign figure,
  garrison, still Saruman, the commander himself, a nation non-At War; legal rear guard and a
  departure from Umbar.
- Preview without mutations/RNG; immediately visible landing, acquisition and empty policy settlement.
- Connection to existing army and limit before battle; connection to besieging FP, a simple sequel
  to Siege.
- Refuge FP after landing, without throw and without Advance; you cannot leave the incoming army in
  Umbar.
- Field: no StopBattle, no retreat/remain FP, consecutive rounds, automatic occupation after
  victory, mutual elimination without acquisition.
- The three stacks' rescue: the garrison does not fight, remains after the landing loss, after
  winning or retreat FP combines with survivors with the choice of excess.
- PostgreSQL/HTTP reboot and idmpotent retry: select movement, limit before battle, defend
  Stronghold, decisions cards/rerolli/casualties, retreat FP, limit after rescue and delayed Jerkír.
  Each approved step and sample RNG only once; the privacy of both sides' hands.

<a id="przykłady"></a>
## Examples

Eight figures Shadow flows to Dol Amroth with four besieging figure Shadow: first twelve figures in the field, then two in the reserve, then card and a throw against the garrison FP.

Seven figures Shadow arrives at Dol Amroth with a five-figure garrison Shadow besieged by FP. Seven arrivals fight separately. After the retreat FP five figures exits from Stronghold; Shadow selects two surplus figure out of twelve unless the landing has suffered losses.

<a id="scenariusze-implementacji"></a>
## Implementation scenarios

`CorsairMovementTests` checks the condition, four goals, full and partial composition, ordinary political restrictions, departure from Umbar, excess after peaceful connection, preview and rejection of illegal payoad without RNG. `CorsairBattleTests` includes mandatory continuation, voluntary retreat FP, shelter with a limit of five, simple extension of existing Siege, a three stacks' help, elimination and Scouts's before throw. Examples above correspond to `ExistingSiegeMergesAndRemovesExcessBeforeCombatThenAllowsNormalSiegeExtension` and `ThreeStackReliefKeepsGarrisonOutOfCombatThenMergesAfterDefenderRetreat`.

Five `CorsairPersistenceTests` uses the actual HTTP/PostgreSQL and three consecutive instances of the server for each decision being examined. Checks payment and landing, shelter, excess before Siege, cards/reroll/both casualties/retreat and the final connection of the rescue before the Jerkíru. Helper compares the projections of both sides, repeats the same command before and after the reboot and requires unchanged draws and snapshots. The result of the launch remains in the central report T21-SH-STR-02; preparing these tests does not mean that they are credited.

The central course of `.artifacts/sh-str02-04-domain-fourth-focus.log` ended with 300/300 PASS without omission. It included all 43 cases Corsairs, including five PostgresFact; the result confirms the report TRX `shadow-strategy-final-focus.trx`. It was a focused test; the final full regression .NET and Unity saves the package coordinator. The agent developing this section did not run a parallel build or pass manual user tests.

Final fix of the excess window after the rescue: `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/sh-str02-relief-editor.xml -logFile .artifacts/sh-str02-relief-editor.log` — **76/76 PASS**, 18:21:04 UTC . `bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.ShadowStrategy02;Wotr.PlayTests.BattleLayoutTests;Wotr.PlayTests.CardPack04LayoutTests'` — **17/17 PASS**, zero skip, 18:21:15–18:22:01 UTC ; `.artifacts/sh-str02-relief-final.xml` Includes all 13 travel of network packages 02 , all 56 System Organ Class 14 states, both tests of the overall system battle and regression FP Character04. The new actual screenshot before clicking approval shows the correct excess title, 12 figures and the footer available; the previous fight result remains in Last Battle. The earlier only full run error 148/149 it was re-executed and counted. We do not present historical 148/149 as 149/149.
