# T30-CARDS-04 — Work of the Enemy

Source: [obverse SRC-05](../sources/kings-card-index.json) , `KINGS-CARD-SHEET-C906DF19` , rectangle `[996,1800,1328,2400]` , KM 26/24. Common rules: BASE p. 21–23 (choice selection, partial execution, event loss), KINGS p. 8–10 (State of sovereign), p. 7 (replacing the fight). One new card `kings.sh.strategy.26` , without a base slot to replace.

<a id="wejście-koszt-i-kolejność-wydarzenia"></a>
## Entry, cost and order of the event

Muster type, the usual cost of a legal event Shadow. No printed condition “Play if”; no target does not prohibit playing and does not return the cost. card goes to the discard on play; paid action lasts until the end of effect.

1. Shadow chooses a region with sovereign Initial, Awakened or Corrupted. The portrait indication is
   also selected by its region. Removed is not a target.
2. When there are various armies of FP (field and garrison), Shadow chooses one of them. The only
   army is a clear goal. Sovereign does not have to belong to the chosen army or be in the same
   garrison.
3. The server throws exactly three dice. Each 5+ asks one hit; for the selected Corrupted threshold
   it is 4+. This is not a throw and there is no Leadership reroll or cards combat modifiers.
4. Hits open a separate FP loss window. FP indicates regular, elite degradation or elite removal for
   two hits. Overshoots after losing an entire army is lost. Return ends paid event.

With many sovereigns in the region, choice is only relevant between the state of Corrupted and the others; equivalent objectives of the same region/przedzia The hues of the threshold have one representative portrait. This is a clear interpretation of a single ‘that Sovereign’ from the obverse and the choice of the goal by the player ( BASE p. 21 ), according to [His Power Waxes](T30-KINGS-03.md) Example: Théoden Awakened and Denethor Corrupted in Minas Tirith allow you to choose the threshold 5 + or 4 +; implementation does not specify it in order figures .

Partial execution: if there is no sovereign, no choice and no throw. If the chosen sovereign is in the region without the army FP, the three throws still perform a possible printed fragment, but do not lose another army or sovereign. Example: The single Corrupted in Orthanc does not become the target of hits.

The cause of the loss is `Event`: the last unit removes the lone commanders FP (including Initial), but does not kill Companion (including Awakened). Corrupted is not figure from the army. The effect itself does not launch a political attack.

<a id="walka-i-interakcje"></a>
## Fight and Interaction

His Power Waxes, initiative 3 share the proven package program 03 The condition is for the region of the defence **, without the requirement of a sovereign in the fight. Corrupted gives + 1 to Combat and Leader re-roll your own army; the remaining sovereign gives + 1 Both armies for both throws. Replace the leadership ability and cancelling work in the usual order, and card It's played.

<a id="zapis-prywatność-i-ui"></a>
## Record, privacy and UI

The existing effect frame stores the selected region and ID sovereign; the choice of army and the throw are atomicly validated. The `EventLosses` frame captures the results, cause, participants and garrison page. Restart/retry does not repeat the cost or draw. The preview does not download RNG. The client does not receive a pool of tiles or covered values of Corruption.

Existing presentations of Sovereign/Region and losses in a fixed tray: portrait with threshold, field/garrison only on real choice, then figure losses of FP. Each step has a click, preview and one approval; no numbers input, forced drag and scroll.

<a id="weryfikacja"></a>
## Verification

Plan: module/hand legality/pages/phase/cost; three statuses and thresholds 4/5/6; no sovereign/army, two different thresholds in one region, field/garrison; limits of elite loss and character fate; both uses, privacy, post-stitch cost, HTTP/PG restart/retry, Unity with two clients and 1366×768/1920×1080 at 100/125%. Manifest binds each obverse instruction to the test actually performed. The actual results are given below.


Result of the first full regression ( 2026-10-06 ): `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t30-04-full.trx' --results-directory .artifacts/test-results` — **2815/2815 PASS**, 0 a skip, 1 m 56 p. Two new methods HTTP/PG play all the boundaries of choice, both secret Combat commitments, the choice of sovereign in battle, losses and retras without RNG Manifesto all 19 cards link 131 sentences/cases with 152 methods and **272 credited runs** of which TRX Base regression: 477 methods, 990 credited runs; checker sources Kings PASS .

`bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t30-04-editor.xml -logFile .artifacts/t30-04-editor.log` — **146/146 PASS**, 0 skip, 06:35:48 UTC. Five new cases check out the uneven armies, change region/cofniocie/reconnect, card texts and sovereign names in history with or without figure Leader.


`bash scripts/test-client.sh -testFilter KingsCards`  — **25/25   PASS**,  0  skip,  06:36:25–06:37:26   UTC.  `.artifacts/t30-04-play.xml` / `.log` Two Unity Clients with Real Kestrel/PostgreSQL check all four packages of Kings. Four new scenarios include selection threshold/sovereign,  field/garrison, the degradation of the elite and the choice of sovereign in the combat; each includes reconnect and authoritarian command.  16 discharges `kings04-*.png` cover 1366×768 and 1920×1080 with 100/125%; without scrolla/p The numbers, the buttons fit in the tray and have the required hitboxes. The choice of sovereign, army and losses in 1366 × 768/125%, and after the amendment, re-election of the army.

The first PlayMode 24/25 had a misexpected test: after degradation the unobsessed garrison was properly released, so in the field they remained three regular figure. The test now also checks ID intact garrison. The image review improved the technical ID sovereign in history to the public name. The whole set of 25 tests were re-examined after each of these changes.

Structure review: two steps of the event share `ArmyTarget` , `EventLosses` and RNG ; combat does not duplicate the package program 03 . UI uses existing drafts and separate buttons only for several targets in the region. Documentation/catalog/ sources and `git diff --check` PASS There's no open source gap. acceptance normal start and full UI expansions remains in T33/UI-09/T34 ; Windows and user manual tests were not performed. Next task: T30, aggregate acceptance And the deck connection.
