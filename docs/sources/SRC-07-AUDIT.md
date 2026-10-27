<a id="src-07--pełny-pakiet-źródeł-fate-of-erebor"></a>
# SRC-07 — Full Source Pack Fate of Erebor

<a id="zakres-i-stan"></a>
## Scope and state

Audit includes full English setup and changes board , New Cities, six cards events with both parts and both versions of Gimli. This is verification of sources before T32, not implementation expansions. [Index](fate-index.json) and [checker](../../scripts/source_fate.py) The coordinator has integrated the source log, gate and assigning packages; final registration check passed.

<a id="materiały-i-pochodzenie"></a>
## Materials and origin

First, [Ares](https://www.aresgames.eu/games/war-of-the-ring-line/the-fate-of-erebor), [announcement by the publisher](https://www.aresgames.eu/29600) and [publisher store](https://ks.aresgames.eu/product/war-of-the-ring-the-fate-of-erebor-mini-expansion-pre-order/) were rechecked. Direct reading continued to return 403. The product description does not replace obverses or instructions.

The photographs of the three complete English pages of the chapter XIII "The Fate of Erebor" in the manual The Battle of Five Armies Collector’s Edition: pages 44–46, published 2021-08-01 by Lord_LT. The last photograph shows the printed number 46; the previous pages contain the beginning of the chapter and full rules. This reproduction of the printed source, not the paraphrase of the author of the page:

| Page | Photo and direct image | Scope |
| --- | --- | --- |
| 44 | [Publication page](https://ludopedia.com.br/jogo/war-of-the-ring-the-fate-of-erebor/imagens/180918), [image](https://storage.googleapis.com/ludopedia-imagens-jogo/2053d_180918.jpg) | Introduction, composition, caps and set of eight cards. |
| 45 | [Publication page](https://ludopedia.com.br/jogo/war-of-the-ring-the-fate-of-erebor/imagens/180917), [image](https://storage.googleapis.com/ludopedia-imagens-jogo/5d122_180917.jpg) | Whole setup, regional changes, Lords/Council and optional New Cities. |
| 46 | [Publication page](https://ludopedia.com.br/jogo/war-of-the-ring-the-fate-of-erebor/imagens/180919), [image](https://storage.googleapis.com/ludopedia-imagens-jogo/711f6_180919.jpg) | Summary of all six events and two Gimli versions, with numbers and indication of the replaced cards. |

[Full sheet of eight obverses](https://steamusercontent-a.akamaihd.net/ugc/2538423216290327847/E48CB71CD54473A3F80D36BC7612C9593FBCE545/) is the size of 2075×1500 and the layout of 5×2. Each obverse is read directly; you can see the `FE` notes, slot numbers, icons, initiatives and all combat applications. The content of events and altered Gimli's capabilities were compared to the physical side of 46. The sheet has two blank fields — they are not missing card.

The sheet locator is from [attached resource directory](https://github.com/johnchampaign/war-of-the-ring/blob/6716a03d3c6ce99a41693a38adaf6d541b10844c/assets/asset-urls.json). The tags and names in this file are used only to find reproduction; rules and printed numbers come from obverses. Eight covers in the collector's edition are four elements in two sizes; it does not mean eight map changes. No downloaded image becomes automatically the client's production graphics.

The Kings integration was verified in the local [instructions KINGS](../../WOTR015-Rulebook-EN-web.pdf), p. 18–19. The base rules and Lords are still in force in the unchanged range; the Council's capabilities and their exceptions are in the [SRC-04](SRC-04-AUDIT.md).

<a id="porównanie-z-samodzielnym-wotr018-dla-second-edition"></a>
## Comparison with the WOTR018 independent for Second Edition

The release was not compatible on the basis of the common title. The [photo of the interior of the self-instruction](https://boardgamegeek.com/image/6836942/war-of-the-ring-the-fate-of-erebor), with the heading “War of the Ring — Second Edition”, and [back of the same package](https://boardgamegeek.com/image/6836941/war-of-the-ring-the-fate-of-erebor), has the date of publication of 2022-04-28 UTC. The back shows the code WOTR018 and copyright 2011/2021. Both reproductions in the size provided by BGG are clear, pinned SHA-256 in the index and reproducible by the checker; the original photographs have 9248×6936 and 6936×9248 in the pixels.

| Tested range | Result of direct comparison |
| --- | --- |
| Whole Setup figures and Politics | The self-contained instruction and CE p. 45 give the same six pairs of region/nar, the end values and the activity of Dwarves. No change in political step was found. |
| board and victory | The same Erebor Saurona, neutral fortification Dale, the city of Iron Hills, and score points and maintain the remaining rules. |
| Six events swap | WOTR018 lists exactly FP Character 17, FP Strategy 4/19/22 and SH Strategy 20/22; CE p. 45–46 attributes the same slots, names and summaries. Full use of events and battles were read separately from eight `FE` sides. |
| Both versions of Gimli | The list of WOTR018 includes Son of Gloin and Dwarven Lord; setup replaces the normal card, and a separate paragraph of Lords orders to use an alternative at the Council and allows Iron Hills or Ered Luin instead of Erebor. It is the same range as CE p. 45–46 and read obverses. |
| New Cities | In both prints, the option turns Ered Luin and South Rhûn into a city for all purposes. |
| Integration Kings | None of these older Fate prints describe later Kings. The source of Dáin/Brand's shutdown and two events swaps is directly KINGS p. 18; the slot table was tested on p. 19. This exception was not transferred by guess. |

The text of the operative paragraphs of Setup, Board Changes, Lords and New Cities is consistent with the sentence comparison. The physical composition varies: CE gives eight overlays, or four types in two sizes; photographed by a separate WOTR018 gives four overlays in one size. CE also has a summary page of eight cards, which is not inside a self-advised leaflet. These are not additional rules or other numbers of cards. The obverse sheet is a separate source of content of cards; we do not assign to it a print date that does not show reproduction.

The application specifically concerns these reproductions and the `FE` set of obverses. The date of publication of the photo is not the date of printing the copy. We do not claim that any subsequent printing or deluxe edition is identical; the product description available in the search engine index now also mentions the second size of the covers, which is not proof of changes in the rules or the basis for overwriting the printed print. This restriction of origin is also machine-written in `editionComparison`.

<a id="setup-plansza-i-konfiguracja"></a>
## Setup, board and configuration

Based on p. 45 we save the exact values after change, we do not add them twice to the base setup:

| Region | figures status after change |
| --- | --- |
| Erebor | No Dwarfs; two regular Saurons. |
| Dale | No North units, including base commander. |
| Carrock | A total of two regular Norths and one commander. |
| Ered Luin | Two regular Dwarfs in total. |
| Iron Hills | A total of two regular Dwarfs and one their commander. |

Dwarfs are initially active; the instruction does not move their tag to another political degree. The remaining setup does not change. figure removed from the base deployment remains available in the usual reserves as they are not a loss or removal of the component from the game.

Erebor is the original Stronghold Saurona; it does not give Shadow initial points, and the winning FP receives two. Dale is a fortification outside all nations, without a city point. Iron Hills remains a Dwarf region and becomes a city. Victory retains the base thresholds. Instructions do not change neighborhoods or impossible borders.

New Cities is an option inside Fate: Ered Luin becomes the city of FP, South Rhûn the town of Shadow. They retain their nations; the city is valid for all purposes, including recruitment, defense and point value. Without New Cities they both retain Town; without Fate the variant is unavailable according to the configuration of MVP.

<a id="tożsamości-i-pełne-zastosowania-kart"></a>
## Identity and full use of cards

The `fate-index.json` index attributes six separate ID events to T32-CARDS and two card characters to T32-GIMLI. The replacements include the base FP Character 17, FP Strategy 4/19/22 and Shadow Strategy 20/22. Names Bardings and Dwarves of the Iron Hills respectively replace King Brand’s Men and Dáin Ironfoot’s Guard; the other titles of the events are preserved. None card is expansions to the number cards deck.

The event capabilities are recorded as rules for implementation, not as text from unverified client. Read combat applications: Brave Stand (3), Advantageous Position (3), Shield-wall (6), Valour (3), Onslaught (7) and Desperate Battle (3). The variant retains their printed terms and full operation; the fact that the name is compatible with the base does not replace the read of the obverse.

Gimli Son of Gloin: printed `1/11 FE`, level 2, Leadership 1 and icon of the Dwarfs. Captain of the West does not change; Dwarf of Renown works in Iron Hills or Ered Luin instead of Erebor.

Gimli Dwarven Lord: printed `LM 6/10 FE`, level 1 (2 only for movement), Leadership 1, without the icon of the nation. Preserved Guide, Dwarven Champion and Messenger of Elrond, but the start of the Council and Messenger refer to Iron Hills or Ered Luin. Erebor is not a allowed starting point or changed ability. The choice of one version does not introduce Gimli's second figure. Son of Gloin remains a regular version starting in Fellowship; the choice of alternatives and its disclosure is subject to the Council.

<a id="jawna-integracja-lords-i-kings"></a>
## The Public Integration of Lords and Kings

Fate does not remove the remaining expansions. With Lords, the appropriate Gimli version is used and the changed Council sites (p. 45). Kings do not have Dáin or Brand sovereigns; two Kings replacements for the base FP Strategy 19/22. are bound by Bardings and Dwarves of the Iron Hills of Fate (KINGS p. 18–19). Other Kings replacements work normally. The common configuration requires an open slot plan, not a order of loading directories.

The control of T32/T33 includes all eight configurations of Base/Lords/Kings/Fate, New Cities on/off at Fate and both versions of Gimli at the legal Council. The identity history of card indicates separately ID Fate and the replaced slot.

<a id="weryfikacja-i-przekazanie"></a>
## Verification and transmission

The final `python3 scripts/source_fate.py --fetch` run: **PASS** for all six sources, also two reproductions of the WOTR018 self-contained. Control includes SHA-256, size and number of bytes, six full events/force effects, two versions of Gimli's single identity, six accurate sets, New Cities and all eight deck combinations. Checker combines actual base index records, Lords and Kings; Fate does not change the number of 96/108/104/116. Exceptions of two cards Kings are clear and independent of the loading order. This controls the sources manifests, not the test of the running game with expansions.

`python3 -m py_compile scripts/source_fate.py`: **PASS**. The script uses only the standard library and existing `source_lords_cards.image_dimensions`, has separate controls of obverses, identities, deck composition, setup and registration; it does not add dependence to Unita or the engine or the second parser of images.

The coordinator has combined recommendations with `.artifacts/source-audit/fate/shared-recommendations.json` : six sources, update of earlier Fate entries, close GAP-FATE , accurate ID T32-CARDS/T32-GIMLI description F01/X03 and preread instructions. Final `python3 scripts/source_fate.py --registered` : **PASS**, six manifesto SHA-256 and all eight assigned ID according to index. Final `python3 scripts/tasks.py validate` : **PASS**, 87 tasks; graph, gate , requirements, rules, links, history, index and SHA-256 sources.

Transfer: gate source T32-SETUP/T32-CARDS/T32-GIMLI/T32 is closed, but their implementation dependencies and T33 remain. No unresolved conflict in the read range has been found. The limitation on the print date is described above. The next work on the client (UI-10) requires separate start after acceptance aggregate T21; SRC-07 does not change runtime or contracts.

<a id="player-texts-ui-365"></a>
## Player texts (UI-365, 2026-10-27)

The owner found Dwarf of Renown's tooltip senseless ("any die in X or Y moves the Dwarves"): the condition did not
name Gimli. Every Fate text a player reads was checked against the criteria of UI-365 (the condition names its
subject, who spends what, one or two short sentences, the UI's game terms, no ids or engine words) and against the
meaning in the manifests, T32 and the engine. No text contradicted the rules; the rewrites keep each meaning and drop
clauses that only restate general rules. Ability titles and the cards' printed wording stay where they were clear.

| Location | Problem | New text |
| --- | --- | --- |
| Ability text, Gimli: Captain of the West | Three sentences; two restate the five-dice cap and Combat Strength in general. | When Gimli takes part in a battle, add 1 to the Free Peoples army’s Combat Strength (at most five Combat dice). |
| Ability text, Gimli: Dwarf of Renown | A bare "In Iron Hills or Ered Luin" without Gimli; "die result", four clauses. The Dwarves are active from the Fate setup, so "does not activate" never changes play; Erebor is not in the list. | While Gimli is in the Iron Hills or Ered Luin, even if the Shadow controls it, you may spend any Action die to move the Dwarves one step toward war. |
| Ability text, Gimli, Dwarven Lord: Guide | Three sentences; "decision"; the Council's shared group rule. | When a Hunt tile is drawn while Gimli is the Guide, you may separate him to reduce its damage by 1 (minimum 0), unless that damage must be taken as Corruption. |
| Ability text, Gimli, Dwarven Lord: Dwarven Champion | "whose defending army is in", a meta sentence about "the current Fate board". | When Gimli takes part in a battle in a Dwarven region (not Erebor), attacking or defending, add 1 to the Free Peoples army’s Combat Strength (at most five Combat dice). |
| Ability text, Gimli, Dwarven Lord: Messenger of Elrond | The same bare "In Iron Hills or Ered Luin"; the icon clause repeats Movement level. | While Gimli is in the Iron Hills or Ered Luin, even if the Shadow controls it, you may spend any Action die to activate the Dwarves and move them one step toward war. |
| Ability text, Gimli, Dwarven Lord: Movement level | Four sentences; "With Council, its starting homelands". | Gimli moves as Level 2 but is Level 1 for everything else, such as Hunt casualties. He has no nation icon and may start in the Iron Hills or Ered Luin. |
| There and Back Again, event | Region names without "the"; "advance Dwarves". | … in Carrock, the Iron Hills or the Woodland Realm, activate the North and advance the Dwarves, Elves and North by one political step each. |
| Book of Mazarbul, event | "in that region" is ambiguous with a Companion in both. | … recruit one Dwarven Regular or Elite in that region (your choice if both). |
| Orcs Multiplying Again, event | "distribute a limited reserve between the legal regions" is engine language and repeats the choice. | Recruit three Sauron Regulars in Dol Guldur and three in either Mount Gundabad or Erebor. If fewer than six remain in the Reserve, you choose how to split them. |
| Dwarf of Renown option detail (server) | Shared generic "advance the nation's politics". | Spend any Action die to move the Dwarves one step toward war. (Base abilities keep theirs.) |
| Monsters Roused plan details (server) | "Regulars in …" without a count. | One Sauron Regular each in …, and a Sauron Elite in Trollshaws / in one of them. |
| Monsters Roused Elite detail (server) | "together with its Regular". | Recruit the Sauron Elite here, beside this region's Regular. |
| Orcs Multiplying Again region detail (server) | "up to three"; "Share the available reserve". | Recruit three Sauron Regulars here and three in Dol Guldur. If the Reserve runs short, you choose how to split them. |
| Scenarios `fate-gimli-renown`, `fate-gimli-messenger` | The same subjectless "any die"; the made-up title "Messenger of the Dwarves". | While he is there, spend any Action die …; the title is the printed "Messenger of Elrond". |
| Scenario `fate-gimli-guide` | "his response in the Hunt window"; the damage reduction unsaid. | … separate him from the Fellowship before the damage to reduce it by 1. |
| Scenarios `fate-card-book-of-mazarbul`, `fate-card-monsters-roused`, `fate-card-orcs-erebor-held` | "move them, then the Dwarves go to war" (moving both away cancels it); "place what it allows"; "leaves one plan". | "you may move them, and if one still stands in either region afterwards …"; "recruit what the reserve allows"; "leaves nothing to split". |

Found clear and kept: the six ability titles, the other three events, all six Combat texts and titles, the Monsters
Roused plan titles, the Orcs and Elite option titles, the Council version labels, and 25 of the 31 Fate test
scenarios. Base-game texts with the same faults (the base Captain of the West's subjectless "While participating",
the political options' "Advance dwarves") belong to `main` and were only reported.
