<a id="źródła-i-braki"></a>
# Sources and shortcomings

Update T01: [audit result](T01-AUDIT.md), [reproduction of the board](materials/board-reference.jpg), [FAQ 1.2](materials/base-faq-v1.2.pdf) and [cards slot index](base-card-index.json). No map closed after comparison; the remaining eight deficiencies have the tasks SRC-01–SRC-08. The following information about the failed Ares reads describe the access history, no missing copy of FAQ.

SRC-01 (2026-10-05): [audit of all 96 obverses](SRC-01-AUDIT.md) closes GAP-BASE-CARDS. The index includes confirmed numbers, types of cards, initiatives and references to the clippings of four pinned sheets. Sources are read, effects still require implementation and testing in T21 packages. The remaining seven shortcomings are separate.

SRC-02 (2026-10-05): [14 of the obverses of the database character](SRC-02-AUDIT.md) closes GAP-BASE-CHAR. [Character Index](base-character-index.json) saves values, icons, version identities and T20 capabilities. There are six shortcomings that exist regarding expansions, Fate and the news FAQ.

SRC-08 ( 2026-10-05 ): [audit of FAQ and the version of the instructions](SRC-08-AUDIT.md) close GAP-FAQ after receiving a full official PDF 2.4.1 comparison 48 pages from 2.3 and rechecking the indexes FAQ 1.2 . Copies of Ares' archival answers confirm origin; direct host still returns 403 This one has been removed. gate T23/T33 , while maintaining their implementation relationship. The other five shortcomings concern the obverses expansions And Fate.

SRC-03 ( 2026-10-05 ): [audit of Lords events](SRC-03-AUDIT.md) close GAP-LORDS-CARDS after reading 13 full cards event/combat and special We Shall Get It. [Index](lords-card-index.json) binds the obverses to five fastened sheets, openly replaces Balrog and divides 14 position on packages 6 + 6 + 2 Normal decks with Lords contain 108 cards ; card Special remains outside the deck/hand. `python3 scripts/source_lords_cards.py --fetch` plays a complete and hash control. The remaining four deficiencies concern the characters Lords, two groups Kings and Fate.

SRC-04 (2026-10-05): [audit of the characters, Rings and Council](SRC-04-AUDIT.md) closes GAP-LORDS-CHAR. [Index](lords-character-index.json) contains 14 full obverses, version identities, icons and required scripts; separates eight cards T25 and six T26. `python3 scripts/source_lords_characters.py --fetch` checks the set and pinned sheet. Confirmed exceptions FAQ for groups separating guides and returning Hobbits from battle. Remains three gate: Kings events, Kings characters and Fate.

The instructions in the main directory are sources of the second edition rules. [manifest.json](manifest.json) identifies specific files by SHA-256; [gaps.json](gaps.json) separates available material from unconfirmed content. Closure of the audit does not mean that all deficiencies are closed.

<a id="materiały-lokalne"></a>
## Local materials

| ID | Material | Scope |
| --- | --- | --- |
| BASE | [WOTR001](../../WOTR001-Rulebook_EN_web.pdf), 48 pages | War of the Ring Second Edition, base principles and setup. |
| BASE-2.4.1 | [Official instruction 2.4.1](materials/base-rulebook-v2.4.1.pdf) , 48 pages | pinned source of base rules in the directory; [proof of origin and comparison](base-rulebook-v2.4.1-audit.json). Original BASE and BASE-2.3 is preserved. |
| LORDS | [WOTR005](../../WOTR005-Rulebook_EN_web.pdf), 32 pages | Lords of Middle-earth, of which Council of Rivendell. |
| KINGS | [WOTR015](../../WOTR015-Rulebook-EN-web.pdf), 20 pages | Kings of Middle-earth; instruction marked 1.0 — 2023.08.10. Integration and variants on p. 17–19. |

The instructions do not constitute a complete set of readable obverses of all cards and a complete verified map. The implementation of all exceptions requires a separate directory of content. We do not assume that the text of the first edition, fan application or War of the Ring card is compatible with this project.

<a id="materiały-internetowe"></a>
## Internet materials

- [Ares: Fate of Erebor](https://www.aresgames.eu/games/war-of-the-ring-line/the-fate-of-erebor):
  the source of the publisher for the product scope. Automatic access during the re-try 2026-10-04
  returned 403; we do not consider the product description as the full text of cards.
- [Ares: FAQ](https://www.aresgames.eu/2648): 1.2 version with 2014-09-09 still indicated in the
  publisher index during SRC-08; includes the database/Lords. We do not extend its authority to
  Kings. Local PDF is available despite 403 direct host.
- [Ares: archive](https://www.aresgames.eu/downloads/archive): the location of the search for
  official instructions, errat and player help.
- Photographs of the English Fate instruction: [page
  1](https://ludopedia.com.br/jogo/war-of-the-ring-the-fate-of-erebor/imagens/180918),
  [rules](https://ludopedia.com.br/jogo/war-of-the-ring-the-fate-of-erebor/imagens/180917), [Further
  material](https://ludopedia.com.br/jogo/war-of-the-ring-the-fate-of-erebor/imagens/180919). This
  material is re-released, helpful to establish deficiencies; it does not confirm the total content
  of expansions yet. The reopening of the rules page failed with the cache tool.

In the absence of access, we are looking for another copy of the official document and checking the edit. We do not replace the missing text with a social translation without comparing the original. Requesting the user for material is the ultimate way to unlock a particular missing, after using available sources.

<a id="hierarchia-i-rozstrzyganie"></a>
## Hierarchy and Settlement

1. Set the editing, errat used and batch configuration.
2. The basic rules apply with open exceptions to cards and included expansions.
3. The specific cooperation instructions of expansions shall take precedence in the cut described.
4. Official FAQ/errata solves the problem only in the area to which they relate.
5. Social material is a clue to find the source; the contradiction goes into the register and blocks
   the implementation.

There is no one global rule “appendix always wins” or “newer file wins”. The entry of the rule indicates a specific overwritten condition and the limit of exception.

<a id="kryterium-zamknięcia-braku"></a>
## Non-compliance criterion

The entry in `gaps.json` is given to `resolved` only after the recording of `resolution`: material, location, date, range of comparison and result. For cards a list of stable ID, page/talia/number, editing, full data of both applications and public replacements is required. For the map: regions, all edges, forbidden boundaries, settlement, nations and original owners.

We don't close the gap. cards by counting 96/14/19/8 components. We do not close the lack of a map based on a working prototype with several regions. Proof should allow another agent to repeat the comparison.

<a id="katalog-reguł"></a>
## Rule Folder

T04: [data control summary](T04-DATA-AUDIT.md), [format Production](../../content/README.md). The origin of geometry and secondary verification of the times of tiles are clearly separated from the sources of instructions.

[catalog.json](../rules/catalog.json) is an index of areas, sources and locations. T03 develops it for detailed decisions and related cases. cards data later have its own manifesto, because the presence of the "card combat rule" does not confirm the action of each cards.

SRC-06 ( 2026-10-05 ): [audit of sovereigns and chiefs of Kings](SRC-06-AUDIT.md) close GAP-KINGS-CHAR after comparison 13 full of obverses and five tokens with instructions 1.0 . [Index](kings-character-index.json) split five pairs Awakened/Corrupted and three Dark Chieftains; T28/T29 are received separately `character_ids` 10 + 3 Accurate levels, resilience, icons and weaknesses range have required future tests. `python3 scripts/source_kings_characters.py --fetch` check the manifest, assign and pinned sheet. Other gate It's a gaps.json.

SRC-05 ( 2026-10-05 ): [audit 19 events Kings](SRC-05-AUDIT.md) close GAP-KINGS-CARDS . [Index](kings-card-index.json) split 11 replacements and eight new cards per package 6 + 6 + 6 + 1 Both halves and initiatives are read from obverses; two Seeing Stones promos are disabled. Database+Kings has 104 card , Database+Lords+Kings 116 . `python3 scripts/source_kings_cards.py --fetch` check the set, assignment and pinned sheet. Remains gate Fate and implementation expansions has separate tasks.

SRC-07 (2026-10-05): [Full Fate audit](SRC-07-AUDIT.md) closes GAP-FATE. [Index](fate-index.json) includes six full events and two versions of Gimli, accurate setup, board, New Cities and exceptions Lords/Kings. Complete standalone instruction WOTR018 is compared to the sentence after a sentence with a collectible chapter; the difference in the number of physical overlays and limits of print identification is recorded in the audit. `python3 scripts/source_fate.py --fetch --registered` checks six pinned reproductions and eight deck configurations. All recorded source shortages have been closed; implementation of expansions and acceptance their interaction still require separate tasks.
