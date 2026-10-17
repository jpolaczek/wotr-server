<a id="t30-cards-01--pierwsze-sześć-wydarzeń-kings"></a>
# T30-CARDS-01 — The First Six Events of Kings

<a id="źródła-i-zakres"></a>
## Sources and scope

Six full obverses were re-examined SRC-05 , sheet C906DF19 cell 13/14/15/18/21/23. [Index](../sources/kings-card-index.json) and [audit](../sources/SRC-05-AUDIT.md) They're tying numbers, both halves and Fate's exception. KINGS 1.0 p. 3 – 4/7/10 – 13/18 – 19 and BASE 2.4.1 p. 22 – 23/28 – 32 determine the replacement, the awakening, the partial execution and the order of the fight.

<a id="wejście-koszt-i-decyzje"></a>
## Entry, cost and decisions

The own card, Action Resolution and the right Event or Print Cost action are checked by a common validator. Kings must be enabled. The cost is paid once and remains Resolving by motion, tiles, losses, limits and reactions. Metadata of this package include six implemented cards; the other 13 is a range of subsequent packages. Full decks/startThe configurations receive T30/T33/T34.

| Card | Event | Combat |
| --- | --- | --- |
| FP Character 23, House of the Stewards | Boromir in Gondora. Selecting one branch: Regular/Elite Gondor at Boromir or a random return of the covered tile of Denethor. Then 2 Strategy. | Brave Stand (3): Participating Companion, number dice SH reduced by number of participating Companions, min.1. |
| FP Character 25, Aid in Time of Need | any/all Companion movement outside Fellowship, including refusal of movement. Then the selection of Initial Sovereign sharing region with Companion and awakening in place, without normal cost/movement. | The King's Banner (4): Aragon or Awakened attends; select one and dedicate the entire Leadership (must be fully available), cancel Leader re-roll SH. Do not sacrifice the cancelled Leadership. |
| FP Character 26, It Is Not So Dark Here | Select the FP nation, move one field without automatic activation. Then select the uncorrupted sovereign and watch the tiles; select one to return. | Like a God of Old (2): Awakened attends, a special attack with the number dice his Leadership, 3+, separate losses before a regular throw. Selection at multiple sovereigns. |
| FP Strategy 08, Wisdom of Elrond | One branch: activation and politics of the chosen nation FP or private viewing the tiles of the uncorruptened sovereign with an optional return of one. | Confusion (4): Natural 1 SH give his army hits and can not be moved. |
| FP Strategy 16, Riders of Théoden | Corrupted Théoden: Delete and End. Otherwise: recruit R/E + Leader Rohanu in Edoras or Rohan region from Companion, or a random return of the covered Théoden tile; then 1 Strategy. | Daylight (3): limit 3 dice From the SH combat kick. |
| FP Strategy 19, King Brand's Men | Corrupted Brand: Delete and End. Otherwise: recruit 2 Regular North in Dale or a random return of Brand's covered tile; then 1 Strategy. | Shield-Wall ( 6 ): with at least 2 regular enemy hits cancel 1 from loss. |

The replacements have an exact base ID 23/08/16/19. Brand is disabled at Fate (KINGS p.18); base/Fate version is not overwritten by file order. The usual recruitment events use reserve limits, control and siege, rather than the At War requirement. Riders place all possible figure in one selected region. The lack of reserves or targets does not remove the following "either" branch remains open-ended; a possible part of the selected branch is performed (BASE p.23). We do not automatically replace it with the second one after the missing target.

<a id="kafle-poufność-i-integracja"></a>
## Cables, confidentiality and integration

Random return includes ordinary covered tiles, excludes Kings and does not reveal the drawn value FP . Full insight gives value to the owner of the ongoing decision only; the selection of one tile is a separate saved window, also when the candidate is only one. The widget adds a refusal. After the window has finished, the permission disappears. The public history does not save the value; the own context cannot preserve the ID other inspected tiles after the effect has been closed.

Your Welcome Is Doubtful limits the removal of corruption and requires At War also for the wakening by Aid. Denethor's Folly blocks the Awakening Denethor. A clear common restriction point will also be used by future card table; their input/rejection and full intersections receive further paczki/T33. Checking this package reproduces the public effect present on the table as an input condition, do not consider the unimplemented card as received.

<a id="plan-weryfikacji"></a>
## Verification plan

V16: both halves of all six cards, bad actor/window/cost/configuration,  reserves/targets/At  War,  branch/draw,  participants/stra rear, Leadership and cancellation, natural 1 And the threshold of a special attack. Privacy tests change only the tiles, check both raw JSON And the view's out. HTTP/PostgreSQL Saves branches, watching, movement/awakening, cost Leadership and special losses, check preview without RNG and retra. Manifesto binds every source sentence to an active test. Unity will receive public English texts and a joint election contract; full tray expansions remains in UI-09 The results will be added after the actual performance of the check.

<a id="checkpoint-silnika--2026-10-06"></a>
## Checkpoint engine — 2026-10-06

`test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false` with `Kings|BaseCorpusTests` filter: **360/360 PASS** (`t30-01-baseline.trx`). First check of events, inspections, recruitment and Aid: **47/47 PASS** (`t30-01-events.trx`). This checkpoint, not acceptance package; manifest, full regression and Unity are still in progress.

The preview does not draw a draw. The inspection only saves ID of the sovereign viewed in passive effect state; the FP projection reveals its tiles only in an active decision. `RecruitmentBatchStep` solves dynamic quotas Riders by the same recruitment and validation plan as users of static quotas. New programs and limitations are found in `Kings/Cards`; four common combat programs are not duplicated. The Manifest Lords limits completeness control to its own identifier after joining Kings.

<a id="wyniki-silnika-i-śledzenie-źródeł"></a>
## Engine results and source tracking

Full `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t30-01-full.trx' --results-directory .artifacts/test-results`: **2630/2630 PASS**, 0 skip, 1 min 31 p. Focused checkpoint before additional granicami/manifestem: **77/77 PASS**, including nine HTTP/PostgreSQL methods with multiple decisions.

[Manifest](../../content/kings-card-implementation-manifest.json) includes **6 cards , 41 the opinions/source cases, 44 methods and 87 Passes PASS** full TRX . `KingsCardManifestTests` check existing, non-exclusive methods and compatibility of programmes, initiatives, source text and conversions. card has Legal/illegal play both halves and restart cost and return; the remaining tests play all new decision types. Catalog tests eight combinations Kings/Lords/Fate And no Brand swap on Fate.

The first failed controls have improved the name of the nation's metadata field and the test fixture: sovereign must have a tile before corruption, Aragorn/Gimli increase Combat strength, Awakened Dáin counts as Companion, and losses Shadow return to reserves. The ultimate full run includes these cases. Unity EditMode: **131/131 PASS**, `t30-01-editor.xml`; the first import detected an invalid hand field name in the new PlayMode test, improved for the `PlayerView.Hand` contract. The PlayMode network score is below.


<a id="odbiór-klienta-i-przekazanie"></a>
## acceptance client and transfer

`bash scripts/test-client.sh -testFilter KingsCards01NetworkTests` : **5/5 PASS , 0 skip**, `t30-01-play.xml` Two authenticated Unity clients use the real Kestrel and PostgreSQL . Examined the clear selection of the Visdom branch, two ways of closing the inspection, access only FP , reconnect with draft , Awakening Aid and joint approval of both figures Riders. Each scenario controls the layout 1366×768 and 1920×1080 with 100/125% , none ScrollView/TextField in decisions and uncut rules in the inspector. Saved 20 discharges `screenshots/kings01-*.png` ; low views of inspections and recruitment were observed at 125% The tiles show the value/symbol and the name of the sovereign, and the detailed rules are in the opened inspector.

First Preparation PlayMode rejected KingsSetup After the Setup phase; only fixture was repaired, without circumventing the production condition. 0 warnings/errors. `source_kings_cards.py` , `source_kings_characters.py` , `check_catalog.py` , `tasks.py validate` and `git diff --check` : PASS File sharing, shared amounts, serialization of private permission, substitution index and refresh has been reviewed UI .

No manual user or Windows build tests were performed. No normal Kings startup was opened — the entire decks, the remaining card, the full common UI and the expansions matrix are among the other tasks. Another package: T30-CARDS-02. Checkpoint engine: `3da9acf`; commit acceptance contains a manifesto, public texts, client and this report.
