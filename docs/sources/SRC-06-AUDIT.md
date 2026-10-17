<a id="src-06--suwereni-i-dark-chieftains-kings"></a>
# SRC-06 — sovereigns and Dark Chieftains Kings

Audit with 2026-10-05 includes **13 full obverses** Kings of Middle-earth: five Awakened, five Corrupted and three Dark Chiefs, their icons, statistics, conditions, abilities, tokens and attendance cycle. [Index](kings-character-index.json) separates 10 cards states for T28 and three card chiefs for T29. There are five sovereign identities, not the independent characters. `effectsVerified=true` confirms the source reading; at the time of the audit all entries had `implemented=false`. Later T28 received the sovereigns' averses, and the status of implementation are updated in the index regardless of the historical audit result.

<a id="materiał-i-powtórzenie-weryfikacji"></a>
## Material and re-examination of the verification

The original source is [KINGS](../../WOTR015-Rulebook-EN-web.pdf) , 20 pages, release **1.0 — 2023.08.10** on p. 20 , pinned SHA In [Manifest](manifest.json) Read p. 2–3 (composite/setup), 6–7 ( Ruler/cards combat), 8–13 (subereni, tokens and Corruption), 14–15 (watermen) and 17–19 ( Integration/you Connections). Graphical pages were viewed 4,5,9,10 : examples of obverses, legend of statistics, both sides of all five tokens and overlay Helm’s Deep. Official [Ares: WOTR015](https://www.aresgames.eu/games/war-of-the-ring-line/kings-of-middle-earth) and [announcement by the publisher](https://www.aresgames.eu/34316) index the second edition and 13 cards ; the direct opening of the two returned HTTP 403 Official local available PDF and readable obverses allow to verify content despite the limited transport.

Full obverses were downloaded from [Steam CDN sheet](https://steamusercontent-a.akamaihd.net/ugc/2269315845048499268/C906DF19BD2FDDEE792567CB43399826BB89D5FF/) found by [pinned Resource Index](https://github.com/johnchampaign/war-of-the-ring/blob/6716a03d3c6ce99a41693a38adaf6d541b10844c/assets/asset-urls.json) This index is only a reproduction locator; no foreign gameplay code was used as a source of rules. Sheet has **JPEG 3320×2400 , 3 632 090 bytes**, SHA-256 **`e4139bd403a61853ad0c50a6bf3aa16aef7171d26143c6e8df315632e2e718c0`**, grid 10×4 . Joint ID with audit of events SRC-05 This `KINGS-CARD-SHEET-C906DF19` Each of the opposite characters has 332×600 , and the exact rectangles are recorded in the index.

All 13 the cuts were viewed in full, including numbers/icons And the bottom print. OCR was a help, not proof: e.g. Mirkwood initiative `[4]` and the number of Uglúk `KM 3/3` require visual reading. Examples of Brand, Dáin Corrupted and Black Serpent were compared with the full text layer of the official PDF and its reproductions of obverses s.4/9 ; the other obverses are in line with the legend, numbering and composition of the instructions. We do not specify the date of the particular scan print or assign to everyone card limited edition.

The original, headers, clippings and auxiliary OCR are included in the ignored `.artifacts/source-audit/SRC06/`. The repository records rules and locator summaries; graphics are not sent to Unity and are not approved for publication. `python3 scripts/source_kings_characters.py --fetch` checks completeness, assignment T28/T29, statistics, five tokens, and SHA/dimensions from one sheet. It uses a common source control function; it does not introduce an additional image parser.

<a id="komplet-i-statystyki"></a>
## Complete and statistics

The following ID have a prefix `kings.character.`. card Corrupted instruction s.4 includes the components FP; their **controller is SH**. The value `side` cannot replace the character classification or its controller. The resistance printed on the Awakened card concerns the appropriate identity before waking up, not the susceptibility awakened to another attempt.

| ID | Print | Level / Leadership | Resistance | Icon |
| --- | --- | --- | --- | --- |
| brand.awakened | KM 1/10 | 1 / 1 | 2 | North |
| brand.corrupted | KM 2/10 | 0 / 0 | — | North |
| dain.awakened | KM 3/10 | 1 / 1 | 4 | Dwarves |
| dain.corrupted | KM 4/10 | 0 / 0 | — | Dwarves |
| denethor.awakened | KM 5/10 | 1 / 1 | 3 | Gondor |
| denethor.corrupted | KM 6/10 | 0 / 0 | — | Gondor |
| theoden.awakened | KM 7/10 | 2 / 1 | 3 | Rohan |
| theoden.corrupted | KM 8/10 | 0 / 0 | — | Rohan |
| thranduil.awakened | KM 9/10 | 2 / 1 | 4 | Elves |
| thranduil.corrupted | KM 10/10 | 0 / 0 | — | Elves |
| black-serpent | KM 1/3 | 2 / 2 | — | Southrons & Easterlings |
| shadow-of-mirkwood | KM 2/3 | 3 / 1 | — | Sauron |
| ugluk | KM 3/3 | 2 / 1 | — | Isengard |

The sovereigns are initially ordinary Leader FP: Brand in Dale, Dáin in Erebor, Denethor in Minas Tirith, Théoden in Edoras, Thranduil in Woodland Realm (s.3,8). This is a condition before card Awakened/Corrupted, without their ability. Awakened becomes Companion; Corrupted is not Leader, Companion, Character or Minion for effects and requirements. Not moving, gives Leadership or dice. The chieftains are non-flying Minions; none meet the entry condition of Gandalf White (s.14–15). They do not add after one standard dice: they maintain one common SH Ruler die.

<a id="przebudzenie-i-zdolności-awakened"></a>
## Awakening and Awakened Skills

action Awakening usually requires Muster or any Ruler result. The conditions of the obverse are different: Brand needs active North and chooses unconquered city/fortresses au FP ; Dáin, Denethor and Théoden require an active own nation and unconquered designated settlement ; Thranduil requires **Elves At War**, not only active. Movement before waking up uses Awakened levels and normal traffic restrictions Companion (p. 10 ); is not a teleport. Illegal target, including entrance from outside to besieged Stronghold , blocks this one action Awakened returns all Corruption tiles to Hunt, gives its token and turns on capabilities. The Ruler joins only in the next stage of Recovery. Awakened is resistant to Corruption.

- **Brand:** Heir of Bard requires Brand and North unit in battle, legal own card, selection after a
  reveal and the limit once on battle; cancellation card SH has the initiative of 0. Call to Arms at
  North At War uses the Ruler Muster: one Regular/Leader in two different free settlement North and
  an additional Regular/Leader in the free region of Branda. Do not add the requirement that the
  additional region must be the third different settlement.
- **Dáin:** Dwarvenking at At War and presence in Erebor releases the Buster of any dice on the
  upgrade of one Regular in his army to Elite, also during siege. Iron Guard uses the Ruler Muster:
  Regular **or Elite** in his region if free or under siege; then Regular **or Leader** in another
  free settlement Dwarves.
- **Denethor:** Stewardship is an analogous upgrade of Gondor in his army in Minas Tirith, also
  besieged. Jerkír of Anárion only operates in Minas Tirith, after **Event on Ruler**, both used
  for selection and performance of the event: optional additional selection from the selected deck.
  It does not require At War in the latter capacity.
- **Théoden:** Forth, Eorlingas! gives Leadership **3 in the first round of each battle**, then
  regular 1. It does not require card or sacrifice its effect; there is no printed initiative. The
  Mustering of the Mark at Rohan At War uses the Ruler Muster: two Regular/Leader in various free
  settlement Rohan and an additional in the free region of Théodena.
- **Tranduil:** Elvenking gives Elf upgrades to his army in Woodland Real, also besieged, at At War.
  Far-sighted requires Fellowship outside Mordor Track and Ruler Character: a choice of Fellowship
  traffic with a reduction of dice Hunt number by 1 (minimum 1) or movement/separation Companion with
  an additional region. It does not give both alternatives at once or bonus to the pitch value.

Each upgrade returns Regular to reserves and requires available Elite (p. 11 Any standard Will of the West does not automatically become special dice Ruler. The Ruler Will of the West can run any other ability (p. 9 Examples of tests T28/T29: 0/1 available Elite, upgrade in own besieged army, awakening from the forbidden route, Denethor draw/play with the selection of deck after the Event resumes, Théoden without card and reset bonus only in new battle .

<a id="pięć-corrupted-i-zasięgi-słabości"></a>
## Five Corrupted and Reaching Weaknesses

After Corruption figure is placed in a place printed on the card, regardless of the former position: Dale/Erebor/Minas Tirith/Edoras/Woodland Realm. Weaknesses activate immediately. **There is no single common blockade of all elites and commanders in your own nation.**

| Character | Recruitment ban, except Event | Second weakness |
| --- | --- | --- |
| Brand | North Leader and Elite in all regions | After a reveal once per battle with North Unit SH He can replace his card cancellation card FP , initiative 0 ; Brand's presence in the region is not required. |
| Dáin | Leader and Elite in Erebor | Once on battle siege in Erebor remove SH Regular instead of downgrade Elite to extend. |
| Denethor | Leader and Elite In the Minas Tirith | Same type of cost in Minas Tirith, once on battle siege. |
| Théoden | Leader and Elite in Rohan regions (recruitment site) | Helm’s Deep controlled by SH is Stronghold Isengard, still counted as being won by Shadow for victory. |
| Thranduil | **Just Elite** In the Woodland Realm | Cost of SH Regular to extend the local battle siege, once on battle. |

Corrupted does not die with the army or as a result of acquiring its region. FP removes it by Character die at the present Companion with the icon of its nation or Free Peoples; an alternative is the right event. It cannot be awakened. Removal immediately ends weakness (p.13), also changing Stronghold from Théodena. The Corrupted icon does not make it Companion able to remove another sovereign.

T28 Tests: Leader recruitment at Corrupted Thranduil legal; Brand operates nationally, Théoden in Rohan regions; Brand responds to remote battle from North; cost Regular in three specific storms and a limit after reboot; battle in Edoras does not remove Théoden; overlay Helm’s Deep disables retains proper control and points; the Council version without an icon cannot be removed.

<a id="dark-chieftains-i-zastępowanie-kart"></a>
## Dark Chieftains and cards Replacement

The entrance costs Muster or any Ruler outside Eye, with the condition of the right obverse and the selected legal region. The leader can be moved as if the non-flying Minion (p.15): the level range, no exceeding the limit, the limitations of the own siege Stronghold and the enemy Stronghold FP. The lone chief does not get the Nazgûl movement.

| Chief | Condition and region of entry | Combat effect |
| --- | --- | --- |
| Black Serpent | Gondor At War; Army with SE unit | Before the reveal devotes the card effect and settles 1 hit to its own units. +1 Combat SH, or +2 when card was a Relativeless Assault; the 3 initiative, without a bonus to reroll. |
| Shadow of Mirkwood | Dwarves/Elves/North At War; army from Sauron unit | Before the reveal sacrifices the card effect and its own Leadership. +1 to Leader re-roll SH at 4; when card is Swarm of Bats, also canceling card FP at 0. |
| Uglúk | Rohan At War or revealed Fellowship; army with Isengard unit **or*** free region with revealed Fellowship / next to it | Before the reveal sacrifices the card effect. +1 Combat and Leader re-roll of both sides; Desperate Battle gives this bonus only to SH; the 3 initiative. |

Uglúk also has a We March Day and Night: from the same or adjacent region +1 to **Hunt re-roll**, not the first throw. It does not create the right to an additional spread and does not require revealed in the same text of this ability; revealed is a condition for an alternative entry. At Rohan At War and the hidden Fellowship you can still choose an entrance at the army of Isengard but not any place next to Fellowship.

KINGS p.7 requires that the dedicated card be legal. It is still `played` for Sorcerer and other effects; the effects are replaced by card effects and can be cancelled, for example, by the Daring Defiance. The condition of the named card includes a special bonus based on its identity, not the actual original content. It is not possible to realise the original card and the leadership's ability simultaneously. The decision of the chiefs is **before the disclosure of cards; Heir of Bard and Corrupted Brand are selected**after the disclosure.

T29 Tests: legality of card before conversion; hidden selection before reveal; Black Serpent hit with downgrade Elite and lack of Leader re-roll bonus; Mirkwood two 0/4 initiatives, dedicated to Leadership and no reroll at the rest equal to 0; Uglúk both/only own dice; Daring with both roles, Sorcerer and retras at cost. Detailed resolution of simultaneous initiatives and elimination at its own expense must use the existing combat procedure and source T29 scenarios, not the order of registration of traders.

<a id="ruler-tokeny-i-corruption"></a>
<a id="ruler-tokeny-and-corruption"></a>
## Ruler, tokens and Corruption

Two dice The Ruler is common to the parties. FP Gets his from turn After the first Awakened, SH after the first Dark Chieftain. In Recovery, no maintenance form removes die ; a new entrance can bring it back later. SH does not assign the Ruler to Hunt before the throw. After Action Roll first SH selects the Ruler (even Eye) or standard die **Without Eye**, then moves the other Eye to Hunt; then FP rejects one of its own die . Keeper/Lesser Minion do not qualify for this discard (p. 6,17 ).

Five tokens have a separate sovereign identity, a Ruler page and Dismay page. Token gets received at the awakening, uses at most once per game, immediately before action Ruler. Changes the Ruler score to the capacity required by the **second** of this sovereign and requires the immediate use of this ability. This is not an arbitrary change. dice to another purpose. FP can't be in one turn use both Ruler token and Elven Ring (s. 10 When Dismay is enabled, the token used or the death of Awakened with unused token transmits it SH to Dismay; serves for proper Ruler reroll, then disappears (p. 17 The Renovating Ruler Tokens variant changes the source of Dismay to the elimination of Awakened (p. 18 T31 continues to be responsible for the full integration table and the combinations of options.

Corruption (p. 11–13 ) is action SH instead of dice : FP must have unused die , Hunt more than one qualifying Eye; SH selects and removes one Eye, then selects one to three tiles according to ** the remaining Eye. Usually selects one covered, shows the others and returns them. The values and composition of the pool do not go to FP The same sovereign cannot be the target of a simple trial twice in turn ; Event is an exception.

Standard Eye and Special tiles are revealed and replaced and then return without effect. Kings tile interrupts selection, openly hits the sovereign with the value of Corruption **0**, returns the remaining tiles. Hunt's protection must not be used to manipulate this attempt. Once immunity is achieved, all tiles are revealed, the highest standard (at a higher return draw), returns the others including Kings, and introduces Corrupted. Awakened or death of the initial sovereign returns his tiles to Hunt. Activate/post The political velcro with the appropriate Companion in the region allows the removal of a random covered tile of the initial sovereign, with a return to the pool (p. 13 ).

T28 Tests: boundaries of 1/2/4 Eye, cost before the number of selections, record between selections, Kings as first/second/third, reveal tie-break, rejection of the test on Awakened/Corrupted, two normal Corruption attacks in turn, random removal without leakage of value and recovery of the pool. The explicit variant turns the appeal to hidden tiles all over outside Kings; the sequential variant uses the last selected tile. These variants remain T31 range.

<a id="granice-i-integracja"></a>
## Borders and integration

KINGS p.18 turns off Brand and Dáin at Fate; SRC-07/T32 confirms full setup. The council allows you to release the SH token along with the appropriate dice to the commander's entrance with the exception of the normal condition; it does not give you any entry location. Companion without Nation icon do not remove Corruption or Corrupted. dice with Captain of Despair are excluded from the cost/counting trial, and Sméagol tile requires replacement (p.17–18). These rules have open integration into T31/T33. Warriors and promotional materials remain outside MVP.

No discrepancy between the read obverses and the instructions requiring a new gate. Official FAQ 1.2 includes the base/Lords, so he was not assigned the Kings settlements. The absence of the averses was distinguished from future specific interaction tests: the above scenarios are the requirements of T28/T29/T31, **not counted runtime tests**. Final validation, the merging of the common manifesto and the closure of GAP-KINGS-CHAR are coordinated by root.

<a id="wynik-sprawdzeń"></a>
## Test result

After root merged the common manifesto, `character_ids` T28/T29 and documentation: `python3 scripts/source_kings_characters.py --fetch` — PASS 13 obverses, five pairs of states, three Chieftains, five tokens, separate division 10 + 3 and SHA-256/dimensions sheet. `python3 scripts/check_catalog.py` — PASS 30 families/34 windows. `python3 scripts/tasks.py validate` — PASS 87 tasks, graph, gate , links, history and SHA local sources. All obverses and graphic pages 4/5/9/10 viewed; the full text of the instructions is compared to the above. First auxiliary call OCR failed without the folder argument; the correct call read 13/13 images and did not replace visual control.

GAP-KINGS-CHAR is resolved. gate T28/T29 source unlocks; their implementation dependencies remain unchanged. .NET/Unity has not been run or hand tests because the game code has not been changed. Before the final recording, there was no space on the disk; root removed the re-active residues of the installer, then the recording and control were repeated.

Correction of the T28, 2026-10-06: a comparison of the obverses KM 2/10 and KM 8/10 (`kings.2123-1.png`, `kings.2120-7.png` from the attached sheet) revealed a too wide paraphrase of Théoden's ban. Brand indicates figure North, Théoden place ‘in Rohan’. Index and tests apply this literal condition; we do not transfer Brand's national restrictions to Théoden.
