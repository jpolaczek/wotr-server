<a id="src-02--14-bazowych-kart-postaci"></a>
# SRC-02 — 14 base cards character

Review 2026-10-05 He has encompassed all the obverses: the cards Companions/Trustee the card Golluma and three card Minion . [Index](base-character-index.json) store 11 blue numbers 1–11 and three oranges 1–3 , values, icons, entry conditions, identifier skills and cut limits. Audited by SRC-02 left `implemented=false` T20 changed these tags to `true` ; [Implementation manifest](../../content/character-implementation-manifest.json) assigns tests to everyone 14 input and 29 the ability entries.

<a id="dowód-i-edycja"></a>
## Proof and editing

Reproductions are from one Steam CDN sheet indicated by [resource index](https://github.com/johnchampaign/war-of-the-ring/blob/6716a03d3c6ce99a41693a38adaf6d541b10844c/assets/asset-urls.json). URL, size and SHA-256 are saved as `BASE-CHAR-SHEET-*` in [Manifest](manifest.json). Full text of 14 clippings has been read using Apple Vision OCR and both sets of averses have been viewed; values and icons are from the image, not from social index tags. Other cards from the same sheet has not been evaluated.

Model cards Gandalf Grey/White/Witch-king, the meaning of icons and values were compared to BASE p.24–25, the number of cards with p.4–5, the composition of Fellowship and the separation of values with p.36–39. Base versions, not card Lords or Fate with similar titles were selected. Previous physical photography of the FP with SRC-01 further confirms the numbered set of 11 blue cards; the basis for full reading remains pinned clips.

<a id="tożsamość-i-wartości"></a>
## Identity and values

| Card | Level / Leadership | Icon of the Nation | Additional die | Role |
| --- | --- | --- | --- | --- |
| Gimli | 2 / 1 | Dwarves | 0 | Companion initial |
| Boromir | 2 / 1 | Gondor | 0 | Companion initial |
| Legolas | 2 / 1 | Elves | 0 | Companion initial |
| Gandalf the Grey | 3 / 1 | any FP | 0 | Companion initial, Guide |
| Strider | 3 / 1 | North | 0 | Companion initial; Icon read correction in T21-SH-CHAR-04 |
| Peregrin Took | 1 / 1 | any FP | 0 | Companion initial, Hobbit |
| Meriadoc Brandybuck | 1 / 1 | any FP | 0 | Companion initial, Hobbit |
| Gandalf the White | 3 / 1 | any FP | 1 | Another version of the same character |
| Aragorn | 3 / 2 | any FP | 1 | Another version of Strider |
| Gollum | None/None | none | 0 | Special Guide, not Companion |
| Ring-bearers | None/None | any FP | 0 | Joint Fellowship figurine, not Companion |
| Saruman | 0 / 1 | Isengard | 1 | Fixed Minion |
| Mouth of Sauron | 3 / 2 | Sauron | 1 | Minion moving on connections |
| Witch-king | ∞ / 2 | Sauron | 1 | Nazgûl/Minion, special movement |

`identity` combines Gandalf's versions and Stridera/Aragorna. You can't create a second active character when you change. No printed level is saved as `null`;  ∞ is a separate movement flag, not an arbitrary big number. Gollum does not give Leadership or another Companion to the limit Hunt.  dice from the icons they enter the pool on proper recovery, not in the middle of the current turn  (BASE  s. 19).

<a id="warunki-i-semantyka-zdolności-do-t20"></a>
## Conditions and semantics of T20 capability

- Gimli, Boromir, Legolas, Grey, Strider and Aragon increase the army's strength with 1 in combat;
  the throw limit remains 5. **Gandalf White has no this ability.**
- Gimli spends any die on the political step of Dwarves in an impenetrable Erebor; Boromir
  analogously Gondor in an unattainable city/Stronghold Gondor; Legolas Elves in an unattainable
  elve Stronghold. The activation icon and the ability to move policies are separate.
- Grey as Guide allows you to select dice Event from the corresponding deck. Strider as Guide allows
  you to hide Fellowship from any dice. Both abilities require the correct Guide and do not work
  only due to the presence of characters on board.
- Each base Hobbit as Guide can separate from Hunt ≥1 injuries, reducing them by 1. Eliminated in
  Fellowship returns by separation procedure; this return does not work on Mordor track. The
  capabilities of the two Hobbits are distinct and do not allow any choice of random victim.
- White requires Grey outside Fellowship or eliminated and any Minion currently or previously in the
  game; the cost of Will of the West. Replaces current Grey or appears in Fangorn/unconquered elve
  Stronghold. **The historical status of the Minion entrance must be preserved after its
  elimination.** BASE p.25 clearly allows for appearance in besieged friendly elve Stronghold;
  "unconquered" on reproduction does not exclude siege.
- White moving alone or with one Hobbit has a range corresponding to the level of 4 only for
  movement. At first battle can sacrifice its own Leadership, abolishing Leadership Nazgûli,
  including Witch-king, on the whole battle.
- Aragorn requires a Strider in the impregnable Minas Tirith, Dol Amroth or Pelargir and the cost of
  Will of the West; replaces the Strider. It does not require Gondor activity/war due to card
  itself.
- The trustees enter Gollum automatically after the last Companion has left. Gollum becomes Guide.
  Ignores Reveal on the standard numbered kaflu; this is not ignoring each Reveal. When Hunt does
  not reveal Fellowship, card allows you to select the disclosure for injury reduction of 1, the
  minimum 0. The full window and interactions resolve T16 and FAQ.
- Saruman requires Isengard At War and the unreachable Orthanc, the cost of Muster, appears in
  Orthanc and never leaves it. In the controlled, unobsessed Orthanc edition, Muster allows you to
  recruit one regular at settlement Isengard or replace two regulars in Orthanc elites. Isengard
  elites also count as commanders for movement and combat when Saruman operates.
- Mouth requires Fellowship in Mordor **or** all nations FP At War; the cost of Muster, the target
  is the unobtained Stronghold Saurona. Once on turn allows you to use the Buster as Army. It does
  not receive the Nazgûla flight.
- Witch-king requires Sauron and at least one nation FP At War; the cost of Muster, the target is an
  army Shadow containing the Sauron unit. The input activates all FP nations. Its participation and
  play card in the first round of battle allow you to select from the corresponding deck after using
  card; the ability does not run in each round.

Location/siege, availability of reserves, date of the new dice, appropriate window and elimination exceptions are still subject to the BASE rules. This audit does not resolve future combinations of Lords/Kings/Fate.

<a id="sprawdzenia"></a>
## Checks

`python3 scripts/source_cards.py --fetch` checks uniqueness 14 cards and numbering, seven initial Companion, two pairs of versions, levelless roles, five additional dice and hash sheet. The working materials are in `.artifacts/source-audit/base.character.*`; a permanent index allows you to download the same sheet again. T15/T16/T20 use these sources, but each ability must receive its own execution test.
