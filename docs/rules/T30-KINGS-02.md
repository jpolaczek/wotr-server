<a id="t30-cards-02--rekrutacja-przebudzenie-i-nacisk-cienia"></a>
# T30-CARDS-02 — Recruitment, Awakening and Pressure Shadow

Sources: full positions obverses 7–12 In [index SRC-05](../sources/kings-card-index.json) images `kings.2110-19/22/17/20/25/26` , KINGS 1.0 p. 11–13 (test Corruption , awakening, removing chips), BASE 2.0 p. 21 (executing a possible part of the event), p. 27–29 (recruitment), p. 31–33 (fight). Status of performance remains in task registry .

<a id="wejście-koszt-i-kolejność"></a>
## Entry, cost and order

All events require card in hand, own window action and dice paying Event or its printed type. Dáin is a substitute for the base strategy 22, Thranduil strategy 24; Dáin is disabled at Fate. The four remaining card add 25/26. slots The game removes card from hand only once; payment and each subsequent decision is subject to server recording.

| Card | Type | Event in order | Fight |
| --- | --- | --- | --- |
| Dáin Ironfoot’s Guard | Muster | Corrupt Dáin: remove it, finish it. Otherwise, select the 1 Regular/Elite + 1 Leader Dwarves in Erebor or the random return of the hidden Dáina chip. Then select 1 Strategy. | Valour, initiative 3: Elite participates in the fight; +1 to Combat Roll. |
| Thranduil’s Archers | Muster | Corrupt Thranduil: remove it, finish it. Otherwise 1 Regular/Elite Elves in Woodland Realm or a random return of the hidden Thranduil chip; then 1 Strategy. | Valour, the initiative of 3 as above. |
| Ever at Your Service | Muster | Select the uncorruptible Ruler on board. Recruit 1 Regular/Elite from his nation in his region. Then randomly return 1 from his hidden chip if there is one. | There Is Hope for Victory, initiative 4: participating uncorruptible Ruler; if initial, awaken it in its region. +1 to FP Leader Re-roll. |
| Men of Little Villages | Muster | Recruit to a total of four figures , one Regular or Leader from four different nations FP ; each in the free settlement Do the maximum possible part of the nation. | There Is Hope for Victory, as above. |
| Your Welcome Is Doubtful | Character | Remains on the table: FP does not remove Corruption; it can awaken the Sovereign only when his nation is At War. FP removes card, releasing Will of the West or Character at the tho-existence of the uncorrupted Sovereign and other Companion. | Servant of the Shadow, initiative 3: participating Minion; resign from all of its Leadership, reduce FP Combat Dice by this value, minimum 1. |
| The West Has Failed | Character | Select the initial Overlord in the Nazgûla region or adjacent. Start the Corruption test without paying Eye and without the requirement of the unused dice FP. | The Battle Is Vain, the initiative 1: at least 1 Nazgûl Leadership; resign from 0–5 its points, reduce the leadership of the army FP by the same amount for this round. |

<a id="jawne-decyzje-i-częściowe-wykonanie"></a>
## Public decisions and partial implementation

- The selection of the national branch does not depend on the content of hidden chips. Unavailable
  recruitment or removal does not replace the selected branch of the second one; selection is still
  taking place. Removing the corrupt Sovereign ends the event before selection.
- Service selects figure, then a type of recruit. No reserve does not skip the subsequent removal of
  Corruption. Awakened is the correct target, Removed and Corrupted not. Ruler and Companion on one
  figure do not pair for the removal of Welcome.
- Villages uses one preview of the deployment: the player clicks nation/figure and region, can undo
  any figure and then approves the whole. The server requires different nations and the maximum
  reachable number of figures, takes into account reserves and free settlement; it does not require
  At War. UI does not choose nation, kind or region as player.
- Hope selects a participating Ruler when there are several of them. Awakening applies the limits of
  Welcome and Folly of Denethor, but the impracticable awakening does not remove the independent +1
  to the reroll. Awakening retains the region and garrison figure; identifier participant battle
  remains the same.
- Welcome is valid for rejection of card. The cost check does not allow you to replace Character
  with another die or use Ring as an unprinted cost. The usual turn ending does not extinguish card.
- West uses the test sequence with KINGS p. 11–12 Because it doesn't issue an Eye, the draw limit is
  `min(3, Eyes w Hunt Box)` One Eye allows one chip; without Eye the event does not draw a draw.
  This event initiates a test, not a regular one action requiring two Eyes. KINGS p. 12 It is
  clearly possible to re-point the Ruler in this turn ed card The test shall be recorded. turn
  target, ignore standard Eye/special tiles, stops at Kings and retains a private choice Shadow
  After it is finished, it returns to the event; it does not finish action - Two times.
- Battle Is Vain pays the available Nazgûli points (including Witch-king). Zero is a legal choice.
  The reduction concerns the combined leadership of the FP army, does not change the printed
  Leadership of individual characters; the result does not fall below zero. The own cost and the
  reduction of the opponent expires with the round.

<a id="prywatność-i-wznowienie"></a>
## Privacy and renewal

The targets and costs are public; the selection of the hidden chip in the sample remains only with Shadow. FP sees the number and allowed disclosures. Recruitment reveals only approved deployment. The preview does not use randomness. The recording includes the selection of the Ruler, type and region of the recruit, each step of the trial, Leadership points and rejection of Welcome. Retry does not repeat payments, draws or selections.

<a id="plan-weryfikacji"></a>
## Verification plan

Each half of card: terms and costs, correct and wrong goal, partial lack of resources, order, boundaries and cuts of rules. Separately: 4 different nations/3 possible nations; Service under Welcome; Hope with the initial and awakened Ruler and Welcome/Folly; West with 0/1/3 Eyes, re-target, break Kings and privacy; Battle Is Vain 0/5, White Rider and change of round. The actual HTTP/PostgreSQL restart/retry in each window, Unity EditMode and two network clients, review trays at low resolution and scale 125%. Manifest combines source statements with tests, and acceptance will save actual results.

<a id="checkpoint-domeny--2026-10-06"></a>
## Checkpoint domains — 2026-10-06

`bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~KingsCards02|FullyQualifiedName~KingsServiceTests|FullyQualifiedName~KingsVillagesTests|FullyQualifiedName~KingsWelcomeTests|FullyQualifiedName~KingsWestTests|FullyQualifiedName~KingsRecruitCardsTests|FullyQualifiedName~BaseCorpusTests' --logger 'trx;LogFileName=t30-02-domain.trx' --results-directory .artifacts/test-results`: **169/169 PASS, 0 omitami, 13 s**. Includes eight tests HTTP/PostgreSQL restart/retry (with loops after card and decisions) and a frozen base body. The first launches detected `null` in substitution metadata and test data errors (inappropriate region Companone, under-defined version of Witch-king, contradictory occupation); they were corrected before the result of PASS. The manual package, full regression and acceptance Units remain to be performed; checkpoint does not close the task.

<a id="odbiór--2026-10-06"></a>
## acceptance — 2026-10-06

Full regression: **2709/2709 PASS**, 0 a skip, 1 m 40 s; `.artifacts/test-results/t30-02-full.trx` Nine methods of this package include actual HTTP/PostgreSQL restart/retry each decision family, including a separate selection of Minion. `verify_results` confirms the manifest 12 cards Kings: **82 sentences/cases , 93 methods 166 counted cases**. The database manifesto still has 477 methods/990 The source cases of options indicate common rules draws/exclusion Kings; acceptance their configuration and modified procedure is T31, not declared by this package.

Unity EditMode **136/136 PASS** ( `.artifacts/t30-02-editor.xml` ). `bash scripts/test-client.sh -testFilter KingsCards` : **12/12 PASS**, zero skip, both sets cards on two clients/Kestrel/PostgreSQL , `.artifacts/t30-02-play.xml` . 36 views of the new package at 1366/1920 and 100/125% ; visually, four views were reviewed at lower resolution and 125% Removed detected by test for a small history button in the test Corruption Details and previous corrections to test data: [TESTING](../TESTING.md) Review of the structure and cost of the planner: [CODE_QUALITY](../CODE_QUALITY.md) Documentation and diff have passed validation. No manual user/Windows tests have been performed. Full deck 19 cards and the usual start Kings remain for the next tasks; the next ready task by priority is T30-CARDS-03 .
