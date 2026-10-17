<a id="t27-cards-01--wydarzenia-wolnych-ludów-z-lords"></a>
# T27-CARDS-01 — events Free Peoples with Lords

<a id="źródła-i-zakres"></a>
## Sources and scope

Six full obverses were re-examined 2026-10-06:  LM  Character  25–28 and Strategy 25–26,  [index SRC-03](../sources/lords-card-index.json) and [audit of exceptions](../sources/SRC-03-AUDIT.md).  LORDS  s.  2–3,  14–15,  21–25 and BASE  s.  22–23,  28–32 specify the composition, Rings, Sméagol, partial execution and battle. Status  acceptance is only in tasks.json.

<a id="wejście-koszt-i-wykonanie"></a>
## Entry, cost and execution

Each event requires its own card on hand, the Action Resolution phase and the legal Event cost or printed type; the existing public cost exceptions remain common. The program does not work without Lords. card and die are paid once before next decisions. The usual combat application does not pay an additional dice. The metadata directory separates the database from expansions; the mere existence of metadata does not allow the use of an unimplemented program.

| ID | Event — Order and Borders | Fight |
| --- | --- | --- |
| Character 25 | Move two additional physical Sméagol tiles from the set-down to the pool. Do not restore two sets or removed tiles; LORDS p. 22. | Blade of Westernesse, 6 initiative: participating Hobbit; cost of one actual reroll hit; selected participating Minion. |
| Character 26 | Treat 1, minimum Corruption 0; remove without performance In Shall Get It or We Won’t Go Back if it is on the table. Removes the active effect. | It Is a Gift, 3: an army-defensive region equal to the regional Fellowship figures; +1 combat and reroll. |
| Character 27 | Mandatoryly remove one card events from the table SH Only at an empty table: optionally view the hand SH , then reject one selected card. | Orderly Withdrawal, 6; details below. |
| Character 28 | Drop 3 dice once; for each 5–6 score, retrieve one used Ring. Select identity: first from outside the game, then from SH to the available number. | Orderly Withdrawal, 6. |
| Strategy 25 | After one regular Gondor in Pelargir, Lossarnach and Lamedon; then selecting Strategy, also if the recruitment could not take place. At the failure of the common reserve FP selects the distribution of available figures. | Shield-wall, 6 : Cancel 1 simple hit at least 2 ; does not act on a special attack. |
| Strategy 26 | Choose an army of SH in the North or region bordering the North; 3 dice, 5–6 hits; SH selects event losses. Then FP selects one free region of the North adjacent to the attacked and recruits a regular or elite and commander of the North. None of target/reserves only skips an impossible part. | Advantageous Position, 3: a battlefield within the nation's FP; −1 to Combat Roll SH, without affecting the metastatic and natural 6. |

<a id="jawne-decyzje-i-prywatność"></a>
## Public decisions and privacy

The wind divides the permission to view the hand from the recoil selection. Access to other people's obverses only lasts in the saved frame of this selection and only for FP. The reboot plays the same decision; the approval removes the permission. Later projections, history and options do not include the remaining cards. Removing card from the table never passes for hand-view. We Shall Get It is card on the table and blocks this alternative (SRC-03, Chapman settlement).

The rings retain ID Narya/Nenya/Vilya. Example: two successes, one ring outside the game and two with SH → the first mandatory, the second one chooses FP. The selection fixes the pitch score, and the retra does not draw again or change the limit of use The Ring in the current turn.

Orderly Withdrawal requires a participating commander of FP or Companion and battle field (also rescue) or outing; not siege. With the initiative 6 FP can cancel one regular hit walki/przerzutu. Refusal does not impose consequences. Use of the obligation to take this round: after losses attacking FP ends the attack, defending FP performs a legitimate retreat (including the available Stronghold). No possible retreat allows to continue the normal procedure. The selection of the retreat goal does not perform the automatic. The effect does not remove the enemy's losses, does not move to the next round and does not arise after the cancellation of card.

<a id="plan-sprawdzenia-i-granica-odbioru"></a>
## Check plan and limit acceptance

V16 tests will include legality, actor, window and cost of all six cards, both applications, resource shortage, partial execution, identity of Council/Lords characters and cancellation. HTTP/PostgreSQL tests will play any new type of decision, compare both projections and re-assemble command without a re-RNG/kosztu. Manifest will link six ID to programs and actually running methods.

The full start of Lords also requires the remaining T27 packages and integration. The expansions interface is developed by UI-09/T34; the contracts of this package must retain map objectives, visual choices and no number entry requirement.

<a id="uściślenia-integracji"></a>
## Accumulation of integration
+ + + BASE p. 31 (End of Battle) ends battle After the elimination of one army. +Orderly Withdrawal does not start retreating after such an end: test + `EliminatingTheOpposingArmyEndsBattleBeforeAnyRetreatDecision` includes + FP as a defender who cancels the hit and eliminates the attacker. With both +living armies, the effect is determined after losses and before the ordinary decision of the +attacker. The fall preserves the garrison; legal defense half of its own + Stronghold allows shelter in it ( BASE p. 30–32 + +The Rangers scripts use the actual borders of the nation: Arnor adjacent to +with North Downs, Angmar does not border directly with the North region. +A lone newly recruited commander without an individual is subject to a common rule + removal of commanders; a test of the missing unit gives him an existing army FP +Shield-wall does not transfer the cancellation to a separate Onslaught attack. Gandalf +Grey selects after a full event from the right Lords deck and only then +opens the excess discard; we do not skip this reaction in production. + +First checkpoint `3894f6d` : 138/138 tests, including the base body. +Next run `t27-01-integration.trx` : 67/67 tests of this package, +including eight methods HTTP/PostgreSQL ; after it was added two combat cases +and manifesto, so the final regression is recorded separately below. +Client uses six public English descriptions, source preview + card and the existing selection of the Ring groups. Metadata 14 obverses and +source verification still do not mean the execution of packages 02/03. *** End Patch
<a id="odbiór-automatyczny--2026-10-06"></a>
## acceptance automatic — 2026-10-06

- Full regression .NET/HTTP/PostgreSQL: **2128/2128 PASS****, no skip, `bash scripts/test.sh
  --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false`,
  `.artifacts/test-results/t27-01-full.trx`.
- Manifesto: `python3 scripts/lords_card_coverage.py --results
  .artifacts/test-results/t27-01-full.trx` — six cards, compatible with programs/initiatives, 43
  method and 69 casesPASS. Manifesto contains only tests relevant to a given card; the base 96
  remains separately.
- Unity: **121/121 EditMode**, **2/2 Network PlayMode**, without skip; files
  `.artifacts/t27-01-editor.xml` and `.artifacts/t27-01-play.xml`. Network range is a regression of
  the base, not a full Lords script. Details and no manual acceptance are saved in
  [TESTING](../TESTING.md).
- Source controls: `source_cards.py`, `source_lords_cards.py`, `check_catalog.py`, `tasks.py
  validate` and `git diff --check` — PASS.

Structure overview: six short modules in `Lords/Cards`; common costs, losses, private options and continuations remain in their modules. Metadata are loaded once, access checked in dictionaries, and local selection change does not rebuild the entire UI. No Domain dependencies from Unity/transportu. Next ready task according to ROADMAP: T27-CARDS-02.
