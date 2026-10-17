<a id="src-05--awersy-19-kart-wydarzeń-kings"></a>
# SRC-05 — Averses 19 cards Aspects Kings

Review from 2026-10-05 includes **19 full of obverses**: 10 Free Peoples and 9 Shadow , 11 replacements and 8 new cards . Print number, page, deck, event icon, both applications, conditions and order, elections, table discards and combat initiative have been tested. [Index](kings-card-index.json) contains separate ID , source rectangles and summary rules of each card . `effectsVerified=true` means the source reading. On the day of the audit all 19 cards has `implemented=false` ; current implementation status updates T30 packages. Index scripts are test requirements and actual results save [acceptance T30](../rules/T30-ACCEPTANCE.md) .

<a id="źródło-edycja-i-zakres-porównania"></a>
## Source, edit and scope of comparison

First, the publisher's materials were checked. [description Ares „Unveiling Kings of Middle-earth”](https://www.aresgames.eu/34316) , originally published in Game Trade Magazine 284 October 2023 , confirms the addition to Second Edition and 19 events. Local `KINGS` , [WOTR015-Rulebook-EN-web.pdf](../../WOTR015-Rulebook-EN-web.pdf) Version 1.0 — 2023.08.10 ** p. 3 defines the breakdown 10 + 9 , 11 + 8 and exchange according to the same title and number; p. 19 lists 11 cards Kings. Page 4 contains Aid in Time of Need and Your Welcome Is Doubtful: both uses, numbers and reproduction initiatives were compared. No specific physical date of reprinting was set. cards or attributed to it on the basis of the date of collection.

The full sheet is [Physical reproduction cards on Steam CDN](https://steamusercontent-a.akamaihd.net/ugc/2269315845048499268/C906DF19BD2FDDEE792567CB43399826BB89D5FF/). It comes from the same [a tight resource locator](https://github.com/johnchampaign/war-of-the-ring/blob/6716a03d3c6ce99a41693a38adaf6d541b10844c/assets/asset-urls.json) that indicates the resources of the TTS 1831369203 fashion by DevKev. The location is not a source of rules: titles, icons, numbers and effects read from images, not from a foreign engine. Each of the 19 obverses was viewed separately; Apple Vision OCR helped in searching and was compared with images. The sources of characters from the same sheet are within SRC-06.

| Common sheet property | Value |
| --- | --- |
| ID in the manifest | `KINGS-CARD-SHEET-C906DF19` |
| Format / Size | JPEG, 3320×2400, 3 632 090 bytes |
| SHA-256 | `e4139bd403a61853ad0c50a6bf3aa16aef7171d26143c6e8df315632e2e718c0` |
| Grid | 10 columns × 4 rows; cell 332×600 |
| Scope of events | cells of 13–33; excluding 16 and 24 |

The cell number is counted from zero, rows from the top left corner. `source.bounds` in the index contains the pixels lewy/g/prawy/dolny. W `.artifacts/source-audit/SRC05` has been saved 21 from the clippings and results OCR, the location data, the cutting script and the auxiliary index generator. The entire sheet was shared with the SRC06 cache and independently checked SHA-256. Re-downloading the checker is included in `.artifacts/kings-card-sources`. Full reproductions and OCR were not included in the client distribution.

Sheet has **21 the obverse events, but only 19 belonging to the Kings box**. Cell phones 16 and 24 have a public print `KM PROMO 1` and `KM PROMO 2` : Knowledge of the Seeing Stones and The Jerkír of Minas Tirith. They are written only in `excludedFronts` Official [tender Ares Essen 2023](https://ks.aresgames.eu/product/kings-of-middle-earth-and-the-seeing-stones-essen/?v=7366ef27aad4) lists separately 19 events and promotional set Seeing Stones. Promosas remain outside REQUIREMENTS Table p. 19 The instructions also include replacing Lords and Warriors; it is not allowed to count all of it as the Kings census.

The real error was found in the locator: **Stormcrow has a `init=3` typed, but its Great Host on the obverse has the initiative of 7**. The index accepts 7 and checker protects this value. This is not an unresolved conflict of two rule sources; the metadata of the locator are given to the readable obverse. The icons of events are read independently of their subject: Return to Valinor and Denethor’s Folly are Army; the other events of Strategy Kings are Muster.

<a id="id-paczki-i-skład-talii"></a>
## ID, packages and deck composition

Order: FreePeoples before Shadow, Character before Strategy, then number and stable ID. Division retains set in the plan **6+6+6+1** and does not change the dependency. The Kings prefix distinguishes the new card 25/26 from cards Lords with the same numbers.

| ID (prefix `kings.`) | Print | Event / type | Combat / initiative | Pack T30 |
| --- | --- | --- | --- | --- |
| fp.character.23 | 23*/24 | House of the Stewards / Character | Brave Stand / 3 | 01 |
| fp.character.25 | KM 25/24 | Aid in Time of Need / Character | The King’s Banner / 4 | 01 |
| fp.character.26 | KM 26/24 | It Is Not So Dark Here / Character | Like a God of Old / 2 | 01 |
| fp.strategy.08 | 8*/24 | Wisdom of Elrond / Muster | Confusion / 4 | 01 |
| fp.strategy.16 | 16*/24 | Riders of Théoden / Muster | Daylight / 3 | 01 |
| fp.strategy.19 | 19*/24 | King Brand’s Men / Muster | Shield-Wall / 6 | 01 |
| fp.strategy.22 | 22*/24 | Dáin Ironfoot’s Guard / Muster | Valour / 3 | 02 |
| fp.strategy.24 | 24*/24 | Thranduil’s Archers / Muster | Valour / 3 | 02 |
| fp.strategy.25 | KM 25/24 | Ever at Your Service / Muster | There Is Hope for Victory / 4 | 02 |
| fp.strategy.26 | KM 26/24 | Men of Little Villages / Muster | There Is Hope for Victory / 4 | 02 |
| sh.character.25 | KM 25/24 | Your Welcome Is Doubtful / Character | Servant of the Shadow / 3 | 02 |
| sh.character.26 | KM 26/24 | The West Has Failed / Character | Battle Is Vain / 1 | 02 |
| sh.strategy.01 | 1*/24 | Return to Valinor / Army | Deadly Strife / 3 | 03 |
| sh.strategy.03 | 3*/24 | Denethor’s Folly / Army | Devilry of Orthanc / 3 | 03 |
| sh.strategy.05 | 5*/24 | Threats and Promises / Muster | Devilry of Orthanc / 3 | 03 |
| sh.strategy.06 | 6*/24 | Stormcrow / Muster | Great Host / 7 | 03 |
| sh.strategy.18 | 18*/24 | The King Is Revealed / Muster | Relentless Assault / 3 | 03 |
| sh.strategy.25 | KM 25/24 | The Jerkír of Barad-dûr / Muster | His Power Waxes / 3 | 03 |
| sh.strategy.26 | KM 26/24 | Work of the Enemy / Muster | His Power Waxes / 3 | 04 |

The Christmas returns the designation mentioned card On the obverse, not the multiplier. 11 records `setup.operation=replace` indicates the exact base ID Same side, waist and number. cards 25/26 has `operation=add` Database+Kings gives four decks after **26 cards , 104 total**. Database+Lords+Kings gives **30/28/30/28 = 116** ordinary cards ; special card Sméagola remains outside the deck. card the characters and two promos do not increase these sums.

**Fate of Erebor is an overriding exception.** KINGS p.18 turns off Brand and Dáin and recommends not to replace King Brand's Men and Dáin Ironfoot's Guard card Kings. The `kings.fp.strategy.19` and `.22` records have `setup.excludedWith=[FateOfErebor]` and the KINGS locator p.18. This is not a precedent solved by the order of charging expansions. The exact eight swaps of Fate and the combination composition are the object of SRC-07/T31; the Kings index does not overwrite them alone.

<a id="logika-wymagająca-osobnych-scenariuszy-t30"></a>
## Logic requiring separate T30 scenarios

- **A wake-up after movement.** Aid first ends the move Companion, then selects the unwoken,
  uncorrupted Sovereign in the Companion region. Awakening takes place in a place. KINGS pp.4 and
  obverses; example: Companion reaches the Sovereign, and the server only then makes the awakening
  available. The King’s Banner requires Aragorn or Awakened Sovereign to participate and the actual
  sacrifice of it Leadership.
- ** House, Ever and four national card recruits draw a hidden tile. It Is Not So Dark Here and
  Wisdom reveal tiles to the authorized FP and allow you to choose. The widgets have an optional
  return after an inspection. Private insight, selection and RNG must survive the record; the other
  projection cannot fix the exposed tiles beyond the right permission. KINGS p. 11–13 and the
  obverses. Example: the reconnect in the Wizard window does not give a re-slot or reveals the
  Shadow tiles as a new public event.
- **The cards national games.** Brand, Dáin, Théoden and Thranduil first remove their version of
  Corrupted Ruler if it is in play. Only `Otherwise` includes the recruitment/tile selection and the
  following Strategy selection. Example: removing the Corrupt Théoden does not select card. Do not
  copy the base recruitment program without this branch. Edors does not require Companion when
  recruiting; the alternative region of Rohanu already requires it. Each averse is a separate source
  of this distinction.
- **The variant visible Corruption.** KINGS p.18 defines hidden tiles as all except the Kings tiles
  if the transparency variant is enabled. Example: sovereign has an uncovered plain tile and Kings;
  Ever draws only ordinary. Do not replace this with rejection of any tile.
- **The West Has Failed explicitly abolishes the removal of the Eye and the requirement of the
  unused dice FP , within the same or adjacent range of the Nazgûla region. The remaining sequence
  of tests and the variant of the next selection remain necessary. Example: FP No, there's not. dice
  , Shadow does not pay Okem, but still retains the privacy of the selected tiles. KINGS p. 11 –
  13/18.
- **Your Welcome Is Doubtful blocks the Corruption removal by FP and requires At War to wake up; the
  Character discard needs a common region of Companion and a non-corruptant Sovereign. Threats and
  Promises restricts the use of the Muster result and has three printed recoil triggers. Jerkír of
  Barad-dûr has a counter once on turn and the cost of Will or die+Ping. These are separate
  limitations and windows, not one flag of the "Kings block". Removing the Corrupt Sovereign is not
  the same action as removing his Corruption.
- **New Denethor’s Folly.** Kings version requires Denethor in the game and unawakened, prohibits
  his awakening and cards FP in Minas Tirith **or region. There is no basic requirement for siege or
  the elimination of the commander at the entrance. Any dice kick requires Gandalf, Boromir or
  Aragorn at Denethor; Will does not require this position. Combat to Devilry of Orthanc. Example:
  Denethor outside Minas Tirith means two regions of ban, not moving the entire ban from Minas
  Tirith. Source: all avers replacing card, KINGS s.3/19. ZXZ
- **Stormcrow adds a tile and army loss only when Sovereign is not Awakened. The King Is Revealed
  accepts Aragorn or a specific version of Denethor, Lord Steward of Gondor, and the recruit bonus
  counts Awakened Sovereigns. Return to Valinor has an alternative condition for Corrupt Sovereign
  and the threshold of 5+ at least two. Work of the Enemy has a threshold of 5+/4+, and His Power
  Waxes is a bonus for both armies with uncorruptible Sovereign. The character status is part of
  validation, not only the card's signature.
- **KINGS p.7 retains the fact that card plays after replacing it with the character's ability
  effect. New card battles require checking the effect off by Brand and Dark Chieftains, the time of
  special attack, the Leadership changes and the return to the normal battle sequence. The source
  and character's abilities are separate SRC-06/T28/T29..

No conflict of sources has been found preventing full reading of 19 from the obverses. Audit does not provide evidence of all possible interactions with expansions or of a solution without sources. If implementation reveals an additional contradiction, it should be saved and verified separately, instead of extending the literal condition of card by a similarity to the base.

<a id="sprawdzenia-i-przekazanie"></a>
## Verification and transfer

`python3 scripts/source_kings_cards.py --index-only --fetch` — **PASS**: 19 event/combat , 11 replacements and 8 expansions , 104 and 116 cards decks, packages 6 + 6 + 6 + 1 , separate rectangles, exclusion of two promos, Fate exception, Stormcrow initiative and downloaded 3 632 090 bytes according to SHA-256 and dimensions. Mode `--index-only` explicitly does not check the merge of tasks, directory and closures yet gate The default startup of the checker requires these elements and will be the final condition for closing SRC-05 .

Root has integrated the common sheet, the closing GAP-KINGS-CARDS , `card_source_review` K02/K03 , four `content_ids` and `read_before` T30 and its parcels, as well as references in README sources, EXPANSIONS and the job documents. `character_source_review` SRC-06 and pending implementation status. The implementation relationship of the T29 packages remains. Runtime, Unity and contracts have not been changed and no launch has been performed. .NET/Unity.

Final checks after integration: `python3 scripts/source_kings_cards.py --fetch` — **PASS**, complete integration with the register, directory and closed gate and compatible hash/dimensions ; `python3 scripts/check_catalog.py` — **PASS**, 30 families and 34 windows; `python3 scripts/tasks.py validate` — **PASS**, 87 tasks, graph, gate , documents and SHA-256 - Before. `python3 scripts/source_cards.py` confirmed the unchanged set 96 cards and 14 The structure maintains separate indexes of sources of events and characters, and checker Kings shares reading of image dimensions with the Lords checker instead of introducing a second implementation of parser JPEG .
