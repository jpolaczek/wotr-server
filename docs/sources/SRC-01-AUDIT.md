<a id="src-01--awersy-96-kart-bazy"></a>
# SRC-01 — 96 cards obverses

The review of 2026-10-05 includes four decks after 24: print number, title, icon of the event type, full condition and effect of the event, maintenance/discard from table, condition and combat effect and initiative. [Index](base-card-index.json) binds each card to a specific cut of one of the four sheets in [Manifest](manifest.json). `effectsVerified` concerns source reading; `implemented=false` does not allow to confuse it with the working card.

<a id="pochodzenie-i-metoda"></a>
## Origin and method

Reproduction addresses on Steam CDN were found in [resource index](https://github.com/johnchampaign/war-of-the-ring/blob/6716a03d3c6ce99a41693a38adaf6d541b10844c/assets/asset-urls.json), commit `6716a03d3c6ce99a41693a38adaf6d541b10844c`. These are reproductions of obverses, not interpretation of a fan engine. No foreign rule code was copied. Twelve references in the old index indicated four different files; the manifest deduplicates them by SHA-256.

The full text of both halves of all 96 images with the help of the local Apple Vision OCR, was compared with four contact boards, icons and printed numbers. There was no initiative or transcription numbering. Visual control includes separately the ambiguous number 11, types cards Shadow, two-step Mûmakil initiative, and fragments about exceptions to recruitment, losses outside the fight, forced retreat and expiration.

The second edition was tested by the layout and examples of cards from local BASE p.23 (Wizard’s Staff, Wormtongue, Character/Army/Muster symbols), a set of four decks and prints of 1–24. [Physical photography Elven Cloaks](https://i.ebayimg.com/images/g/UUgAAOSwo-VniE~m/s-l1600.webp) from [cards 2E kit](https://www.ebay.com/itm/286266559485) additionally agrees with the obverses: 1/24 number, both half, icon and initiative. It is not proof of the other 95 cards; these have their own images from the index. The Ares and archive site continued to return HTTP 403.

Materials excluded as a source of a full catalog: `cecards.pdf` from warofthering.eu (Collector’s Edition 2009), older card 2004, a fan transcript of a card tracker and a mixed client package of Java WOTR-GANDALF2. The latter contains separate overwriting of the edit, so it is not allowed to accidentally connect its main directory with the 2E database.

<a id="korekty-i-wymagania-dla-implementacji"></a>
## Corrections and implementation requirements

- Candles of Corpses has the number SH Character 8; The Nazgûl Strike! has 11. The old index
  mistakenly attributed both to 8. Separated by print, without changing production slots.
- The transcriptions of "Fear! Fear! Foes!" and "Hordes From the East" were corrected according to
  the obverses on "Fear! Fire! Foes!" and "Horde from the East". The candidate title is not the
  source of the print.
- The documentation used `base.shadow.*` while the production decks from T04 have `base.sh.*`. The
  index and eight packages of Shadow now use the existing production ID. The saved parts do not
  change identifier and do not require migration.
- Mûmakil at SH Strategy 7 and 8 has initiatives**3 and 5**; the directory stores the list, not a
  single number. Both windows are the responsibility of T18/T21.
- The icons determine the cost regardless of the effect: Half-orcs and Goblin-men, Olog-hai and
  Hill-trolls are Army-type. Recruitment in the content does not automatically mean Muster.
- card distinguishes the elimination, degradation, replacement, political attack and ordinary loss
  from the event. Ents and Dead Men have special effects of the army elimination. T14 must preserve
  the cause and T21 packages test exceptions.
- There are mandatory partial effects, interrupted by another's choice, held on the table, rejected
  at the expense of the opponent and operating in many windows. The condition of play is not
  equivalent to the availability of any future goal.

This audit of the cards printed database. The current FAQ and detailed conflict resolution remain separate SRC-08; the versions replacing card in the expansions have their own source tasks.

<a id="powtórzenie-kontroli"></a>
## Repeating the checks

`python3 scripts/source_cards.py` checks 96 unique ID, compliance with production decks and packages, types, initiatives and cutout limits. `--fetch` additionally downloads four pinned sheets to ignore `.artifacts/card-sources` and compares SHA-256. Images and full transcripts are not included in the client's graphics. Before implementing a particular package, the specified clippings should be reopened; the text card cannot be replaced by the effect name itself.

Local full read work material is located in `.artifacts/source-audit` (images, OCR and contact boards). Durable proof identifies URL, hash and rectangle of each card; you can play it without this cache. Command results and closing date are in the history of SRC-01.
