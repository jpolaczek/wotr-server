<a id="t24--kości-i-pierścienie-lords"></a>
# T24 — dice and the Rings of Lords

Source: [LORDS](../../WOTR005-Rulebook_EN_web.pdf), p.3,7–15,19–20,26; [audit of obverses](../sources/SRC-04-AUDIT.md). T24 implements cost and window module. The inputs, full character capabilities and the selection of their versions belong to T25/T26, events and deck composition to T27. The launch of the incomplete expansions remains `ContentNotImplemented`. We do not place the base decks under the public Lords party. The module tests openly prepare the presence of guards/Minion.

<a id="rzut-wybór-i-odzyskanie"></a>
## Throw, selection and recovery

The following six walls are compared to the graphic pages 9 and 11, not only with the order of the text PDF. The star means to print Remove, whose execution depends on the state at **next Recovery**.

| die | Results in order of illustration |
| --- | --- |
| Vilya | Army*, Muster, Event, Card Draw, Card Draw, Eye* |
| Nenya | Character*, Muster, Event, Card Draw, Card Draw, Eye* |
| Narya | Character, Muster, Event, Card Draw, Army, Eye |
| Balrog | Character*, Use Balrog, Use Balrog, Muster, Card Draw, Eye* |
| Gothmog | Army*, Army, Muster, Muster, Card Draw, Eye* |

Elrond/Galadriel/Balrog/Gothmog make available die at Recover after entering. Narya requires Gandalf Keeper as a Guide and retrieved dice Movement FP with Hunt; except is the initial Guide in turn 1 Eye Keeper does not meet this condition or the mandatory allocation Shadow Selecting Guide later does not add dice inside turn Death removes die in Recover. Gothmog loses die in Recovery when Black Captain is present, but he retains figure (p. 26 ).

The special dice are thrown together with standard ones; they are never assigned to Hunt before the throw. After the throw each page with more than one special dice selects exactly one. If the dead Eye, the candidates are only the Eye. With several Eye decisions remain public. The priority has a page with more **all actually thrown**dice; the draw gives FP. dice previously assigned to Hunt is not part of this number. The selection has its own revision and recapt.

The unelected results are only put aside to the next turn. Eye selected by FP powers Hunt Shadow, retaining its owner and origin. It does not increase the bonus of earlier Fellowship moves. The selected non-Eye counts to pass and can pay the requirement of ‘any die’. Card Draw selects card from the selected deck; it does not work as Event to play the event. Use Balrog plugs T25; it is still a legitimate cost of ‘any die’.

In Recovery, the previously selected run die Keeper disappears permanently if White is then in the game; Lesser is analogous to any version of Witch-king. The unelected run die does not disappear. Death of White/Witch-kinga before Recover changes this check. The Remove tag itself does not remove dice after rzucie/akcji.

Example s.13: Vilya Eye* and Nenya Eye*, White is alive. FP points out Vilya; Vilya powers Hunt, Nenya waits outside the jackpot. With the next Recovery only Vilya is removed for the rest of the game. Restart between selection and Recover does not change the score.

<a id="pierścienie-i-granice-okien"></a>
## Rings and window boundaries

Three Rings have separate ID. A simple change of the result does not require a guard, but cannot change Keeper/Lesser. Limit of one normal use to turn does not restrict the ability of the guards. Each use of FP transmits the right Shadow Ring; Shadow uses it according to the base principles.

- **The present Gandalf Keeper in the impregnable settlement nation FP, the specific Ring of Narya
  and the available die. The nation of this settlement becomes active and At War. The won settlement
  is not legal; it is recovered again. You cannot choose another nation or perform this ability from
  inside Fellowship.
- **Nenya:** in the exchange window of the selected standard Eye. Pass Nenya, remove this tile
  permanently and select a new one by the saved RNG. The decision is next to other available
  exchanges of this window; the player selects order. It does not cancel a special red Eye or a
  numeric tile. It does not restore the removed Eye at an empty pool or entry to Mordor.
- **Vilya:** After the entire paid action has been settled, before the dice is put off and the actor
  is changed. The specific Ring and Elrond must be still in play. FP can only stop the die, except
  for the result of Will of the West. We will remember the cost score before the normalization of
  Will to action. After the Fellowship movement stopped die does not go to Hunt. Refusal ends the
  usual procedure; acceptance does not repeat the move, battle, Hunt or their draws. The end of the
  lot does not open any further decision by Vilya.

<a id="zapis-projekcja-i-weryfikacja"></a>
## Record, projection and verification

The module has separate types for wall definition, status selected dice, election queue and cost completion. Optional write fields have neutral lack in the base to keep the T23 body remaining historical input. The projection shows the source, current score, print Remove, choice, permanent removal and public place dice; only the owner receives decision options. The autority validation of preview/commit, the lack of RNG in the preview and rejection of another person's already selected dice remains.

Required tests: 30 walls, many special dice Eye, tie and advantage of both sides, no special on the hunt, the terms of Narya's recovery, the death of the guard and White/Witch-kinga , Gothmog/Black Captain, no return permanently removed dice Card Draw/any die/pas , ban on changing the Ring, three capabilities and their illegal costs, round-trip and reboot HTTP/retry , confidential projections, full regression of the base with unchanged entrances of the T23 corps. UI expansions receiving UI-09/T34.

<a id="przecięcia-z-bazą"></a>
## Base intersections

Narya is a Companion (LORDS s.19–20) capability. Wormtongue print in BASE-2.4.1 s.23 allows activation by Companion ability, so Narya can activate Rohan; after activation Wormtongue expires. The political shift of Companion ability also removes Threats and Promises according to its condition. Example: Keeper in the retrieved Edoras releases Narya and die, activates Rohan and moves it at War. The `LordsNaryaTests.NaryaIsACompanionAbilityForWormtongueAndThreatsAndPromises` test checks both outs, without changing the base exceptions ruchu/odwrotu.

Nenya and Mithril Coat and Sting share `hunt.replace` (LORDS s.15/19, obverses BASE fp.character.05 ed SRC-01). After the exchange, the new tile has its own window: Mithril → standard Eye allows you to use Nenya; Nenya → new tile allows you to use the unused Mithril. The ring/card from the previous step is not available again. `LordsNenyaTests.PlayerCanChooseEitherOrderWhenNenyaAndMithrilBothApply` saves and resumes both orders; only the tile cancelled by Nenya is permanently removed, the tile cancelled by Mithril returns after the end of action.

Shared `FinishAction` ends policy/effects/pula, then can open Vilya before `CompleteActionCost`. Acceptance or refusal only triggers costing and choosing the next actor. It does not re-cause `FinishAction` or execute the previous command. The `LordsVilyaTests` tests include battle, Hunt, excess hand, Elrond's death and plain chain Ring → Narya → Vilya. The win from Corruption ends the state without opening Vilya.

<a id="wyniki-weryfikacji"></a>
## Verification results

- dice + Turn + body: **158/158**, no omissions, `t24-dice.trx`.
- First integration Rings + dice/Hunt/Turn/korpus: **250/250**, zero omissions,
  `t24-rings-first.trx`.
- Lords with four reboots HTTP/PostgreSQL: **95/95**, no omissions, `t24-http.trx`. Preview neither
  draws nor saves, the bad actor is rejected, restarts after the commit and retry retain the
  projection and costs.
- After adding the final cases Hunt/Vilya: full regression **1851/1851**, no omissions, 57 s,
  `.artifacts/test-results/t24-full.trx` and `t24-full.log`. The T23 body remains unchanged.
  `card_coverage.py --check --results` confirmed 477 methods/990 96 cards from the base.
- Precision of Narya fixture with the actual ID and transfer cards from the table: **1/1**,
  `t24-narya-tables.trx`, without changing the production code.
- Unity EditMode: **118/118**, zero omissions, `.artifacts/t24-editor.xml`.

All TRX are in `.artifacts/test-results` Commands `.NET` used `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=<raport>.trx' --results-directory .artifacts/test-results` , with a class filter in partial runs. EditMode : `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t24-editor.xml -logFile .artifacts/t24-editor.log` Full PlayMode : **173/173**, no omissions, 457,76 s ( 23:11:48–23:19:26 UTC ), `.artifacts/t24-play-full.xml` , `t24-play-full.log` , `t24-play-server.log` Command: `bash scripts/test-client.sh` The usual batch ended after 12 turn and 162 decisions: RingCorrupted , Corruption 12 , Mordor steppe 2 ; public record `.artifacts/t24-full-game.json` The final review then added two public tags. dice (choice/persistent removal) and restored the base text of the cost of selection in configuration without Lords. Result of verification of the final contract below.

Final contract (choice/permanent removal + base tooltip): re-full . NET **1851/1851**, no omissions, 58 s, `.artifacts/test-results/t24-final.trx` ; cover 477 methods/990 cases cards PASS . EditMode **118/118**, `.artifacts/t24-editor-final.xml` Two clients test after this change **1/1**, no omissions, `.artifacts/t24-play-final.xml` command `bash scripts/test-client.sh -testFilter Wotr.PlayTests.NetworkTests` There were no manual tests of the user, acceptance UI expansions or the launch of the incomplete public party Lords. Full characters/cards/UI integrate next tasks. T24 completes the rule module dice The Rings and his server contract.
