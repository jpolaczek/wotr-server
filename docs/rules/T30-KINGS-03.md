<a id="t30-cards-03--strategia-cienia"></a>
# T30-CARDS-03 — Shadow strategy

Sources: confirmed obverses SRC-05 , `kings.sh.strategy.01/03/05/06/18/25` In [index](../sources/kings-card-index.json) , [audit](../sources/SRC-05-AUDIT.md) , KINGS p. 6 , 9–13 ; BASE p. 21–22 (choice selection, partial execution), p. 29–32 (fight). The document defines implementation before the code; results acceptance will be added after actual checks.

<a id="zdarzenia-i-decyzje"></a>
## Events and decisions

| Card | Condition, cost and order |
| --- | --- |
| Return to Valinor #1 | Army/Event. Shadow controls Stronghold Elfs or any Sovereign is Corrupted. Consider all unobsessed, unobsessed Elf fortresses, choosing order on the map. Throw behind the Elf Regular/Elite, max. 5; hits 6, or 5+ at least two Corrupted. FP chooses casualties** in this region, also other Nations: obverse Kings speaks about hitting the army, while the number of dices only determines Elfs. |
| Denethor’s Folly #3 | Army/Event. Denethor Initial or Corrupted on board. card table blocks his awakening and card from the FP fight in Minas Tirith and the current region of Denethor. It does not eliminate the commander and does not expire at the end of siege. Rejection: Will of the West or any die if Gandalf, Boromir or Aragorn is in the Denethor region (without the requirement of a common garrison). After removing Denethor remains banned in Minas Tirith and the possibility of Will being discarded. |
| Threats and Promises #5 | Muster/Event, table. Blocks the usual action Muster moving the passive Nation and waking up behind the Muster. Separate possibility to wake up for **free Ruler result** (KINGS p. 9–10) remains legal, as does the effects of Event/Combat. Will used as a normal Muster is prohibited; the Ruler pays its own independent opportunity. Mandatory discard after Corruption Sovereign or real displacement of the FP Nation due to attack or ability of Companion. The activation itself, gaining settlement or moving the event is not the trigger. |
| Stormcrow #6 | Muster/Event. Nation Selection FP non-At War, inside which he is Fellowship or Companion First, the withdrawal of politics. If his Sovereign is not Awakened: one numerical tile Corruption ( Eye/Special revealed and replaced; Kings = 0 ), then FP selects any one of your own Regular/Elite This Nation on board and removes all figure Without leaders and without conversion Elite to Regular. No Initial Sovereign skips impossible addition of a tile, but not loss of an individual; no units skips loss. The only tile does not create a seemingly strategic choice. This direct effect card , without the cost of Eye and without changing the limit of normal samples Corruption . |
| The King Is Revealed #18 | Muster/Event. Aragon or awakened Denethor on board. In Minas Morgul five Sauron Regular and one Nazgûl, then for each Awakened Sovereign selection of additional Sauron Regular or Nazgûla. Make the maximum possible recruitment from the reserves, without replacing Regular by Elite. |
| The Jerkír of Barad-dûr #25 | Muster/Event, table. The optional cost of Muster as Event once on turn Shadow: both playing card as selection. For card the choice of the usual cost and conversion remains apparent (it matters for Jerkíra Orthanku). Limit stored in the state, common to re-play in the same turn; the physical score of dice is not changed. Reject by FP: Will or any die + one legal Elven Ring, with normal limit of Rings and limitations of Rulers. Separate action re-off for two Jerkírs. |

The interpretations are derived from the range of obverse conditions and partial execution rules; they are not a new variant. For Threes we check separately the Ruler and the usual action Muster. For Stormcrow we check the missing, removal and Corruption Sovereign to not make the legal loss dependent on the success of the earlier effect.

<a id="walka"></a>
## Fight

Deadly Strife [ 3 ]: both armies + 2 Combat and Leader re-roll. Devilry of Orthanc [ 3 ] (two card ): attends Isengard Regular/Elite , Army of the defender inside Stronghold ( siege , the region itself with Stronghold not enough); + 1 Great Host. [ 7 ]: After simple losses at least twice the number of army units gives one hit. 3 ]: optional 0–2 actually used your own hits before Combat, each gives + 1 only Combat. They use existing, proven programs, but conditions and initiatives of each card they remain public.

His Power Waxes [3]: Sovereign must be in the Army's defense region, without requiring participation in battle. Shadow points out Sovereign if several present give different results; the choice of target retains conditional content of card and the general rule of choice of targets (BASE p. 21), without the order of the list. Corrupted gives +1 its own Combat/re-roll; Initial/Awakened gives +1 both armies. The identical results do not require additional click. Cancellation and conversion by Kings abilities occur before the program is performed.

<a id="stan-prywatność-i-ui"></a>
## State, privacy and UI

The server checks the site, phase, source card, cost, target and limit before taking randomness. The client gets legal costs, goals and own decisions. Stormcrow tile is classified for FP; public are the number of tiles, rejected Eye/Special and possible disclosure at Corruption. The preview does not draw. The order of the Valinor regions, losses, bonus recruitment and eventual selection of Sovereign in the fight have fixed continuations, retras without a return cost or RNG.

UI uses the existing decision tray and map: click target, preview, one approval. The cost of converting Jerkíra has the capacity name; the state of use results from the projection of legal options. Additional figure selects by buttons, without entering numbers, mandatory drag or scrolling.

<a id="plan-weryfikacji"></a>
## Verification plan

V16 for six cards: both catches, proper and foreign turn/pages, illegal costs/targets, disabled add-on and replaced slots. Limit scenarios described above, table cuts with events and fighting, costs of both Jerkírs, extinction, cancellation, lack of reserves and stack limit. HTTP/PostgreSQL restart/retry for each type of window, projections of both sides and no RNG in preview. Manifest binds each audit sentence to the performed test. Unity: EditMode and two actual clients with reconnection and control of the 1366/1920 circuit, 100/125%. acceptance manual user remains deferred.

<a id="odbiór-2026-10-06"></a>
## acceptance 2026-10-06

- `t30-03-targeted.trx`: 86/86 PASS, 0 skip, including eight methods restart/retry HTTP/PostgreSQL.
- Full `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
  --logger 'trx;LogFileName=t30-03-full.trx' --results-directory .artifacts/test-results`: 2794/2794
  PASS, 0 skip, 1 m 52 p. Manifesto 18 cards: 125 From the source sentences, 138 methods, 251 runs
  PASS; database body still 477 methods/990 PASS. The control of the manifesto also checks initiatives
  and open turns.
- Unity EditMode: `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t30-03-editor.xml -logFile .artifacts/t30-03-editor.log`: 141/141 PASS, 0 skip.
- `bash scripts/test-client.sh -testFilter KingsCards`: 21/21 PASS, 0 skip, start 06:05:37 UTC;
  copies `.artifacts/t30-03-play.xml` and `.log`. Nine new scripts, 36 x 1366/1920 × 100/125%. both
  Jerkír, Stormcrow and King costs were visually reviewed at 1366×768/125%.
- First start was fairly uncounted: non-existent enum in the test; then the Kings 503/521 regression
  revealed the lack of validation of the battle phase, the old ID replaced by cards, the wrong
  distribution of figures and the wrong expectation of missing the selection of the Orthank deck.
  The production validation and fixtures were corrected, and the full regression was performed. The
  first PlayMode 20/21 detected the 31,87 px button; after the correction of 33 px all three
  packages were re-checked.
- Structure overview in `CODE_QUALITY`, new costs and continuations in `CONTRACTS`. Manual user and
  Windows tests do not declare. T31 is responsible for variants/Integration, UI-09 for full
  presentation Kings; next ready package T30-CARDS-04.

Core, rules documentation, manifest and tests recorded in the `7cc16d1` commit; the final commit includes Unity, lyrics, scripts and results of acceptance.
