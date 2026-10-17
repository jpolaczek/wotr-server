<a id="src-04--postacie-pierścienie-i-council-lords"></a>
# SRC-04 — Characters, Rings and Council Lords

Audit of 2026-10-05 includes **14 full of obverse characters** Lords of Middle-earth for second edition: 10 FP and 4 SH . [Index](lords-character-index.json) contains stable ID , Print numbers, icons of nations, application-dependent levels, growth standard/special dice , input, capability, output, version binding and future scenarios required. `effectsVerified=true` means the source reading, and `implemented=false` keeps the limit to T25/T26.

<a id="pochodzenie-i-kompletność"></a>
## Origin and completeness

The first source is the official local [instructions LORDS](../../WOTR005-Rulebook_EN_web.pdf) , 32 pages, SHA-256 `a929dceef1e077a6314a464ab78e17aad615a328adc42f5a9b5dc29342b80651` The composition was checked. 2–3 , terminology/movement s. 6 , dice p. 7–13 , Rings s. 14–15 , characters p. 16–29 and the whole Council s. 30–31 Official [FAQ 1.2](materials/base-faq-v1.2.pdf) , p. 3 , details Sméagol, separation of guides and Hobbits. The current version and the limitation of direct Ares HTTP 403 describes [SRC-08](SRC-08-AUDIT.md) . Indexed articles [Ares of additive](https://www.aresgames.eu/category/articles/designers-notes/page/9) were the publisher's lead; descriptions themselves do not replace obverses.

Full obverses are physical reproduction cards In [one Steam CDN sheet](https://steamusercontent-a.akamaihd.net/ugc/1836922259143026705/2CCC853155AB68E3043470C601867805CFB8438D/) , located by [pinned Resource Index](https://github.com/johnchampaign/war-of-the-ring/blob/6716a03d3c6ce99a41693a38adaf6d541b10844c/assets/asset-urls.json) It's an existing one. `BASE-CHAR-SHEET-EDA5E27F` in the manifest: **14 910 545 bytes, JPEG 8154×8192**, SHA-256 `eda5e27fbf6de13a037188400ae098f0c8f2a686bf5e6674dc39b212fcbcef56` Each of the fourteen cuts was viewed in full; OCR was only a help. An empty place was also read after the icon of the nation, numbers on the right margin and markings LM In the bottom corner. Numbers/metadata repository is not a source of rules: e.g. label `#2` Chief is wrong, she's on the obverse. LM 4/5 **.

Independent Real Photographs cards published 2012-11-10 confirm layout and content:

- [Elrond and Gothmog, photo of
  1471575](https://boardgamegeek.com/image/1471575/war-of-the-ring-lords-of-middle-earth): SHA-256
  `95488cab16621cf6688af3549fbf212ddf92a85f5d4afbc5dc82c396d3b806a1`, 1542×1396; compared full
  abilities and values.
- [Gimli, Peregrin and Meriadoc, photo of
  1471574](https://boardgamegeek.com/image/1471574/war-of-the-ring-lords-of-middle-earth): original
  URL, hash and dimensions are recorded as `LORDS-COUNCIL-PHOTO-2012` in [Manifest](manifest.json);
  confirms the lack of icons of nations, level 1, Gimli's movement 2 and the difference between the
  Guide and the Hobbits' return from battle.

The original and technical clippings are in ignored `.artifacts/source-audit/SRC04/` / `.artifacts/lords-card-sources/`. No graphics have been included or the rights to their publication have been declared. The repository stores the locators, SHA, the limits of the clippings and the Polish rules summary. The control of `python3 scripts/source_lords_characters.py --fetch` plays a check of the attached sheet; it does not require any duplication of the existing format scanner.

<a id="komplet-czternastu-postaci"></a>
## A complete set of fourteen characters

ID in the table have a prefix `lords.character.`. `—` means the actual lack of an icon, not inheriting the base belonging. The identity of the nation is read from the bottom rhombus; the special symbol dice in the right margin is a separate field. `any` means Free Peoples.

| ID / Print | Version | Level / Leadership / nation | die | Scope |
| --- | --- | --- | --- | --- |
| elrond / LM 1/10 | Keeper of Vilya | 0, card combat 3 / 2 / Elves | Vilya | T25 |
| galadriel / LM 2/10 | Keeper of Nenya | 0, card combat 3 / 2 / Elves | Nenya | T25 |
| gandalf-grey / LM 3/10 | Keeper of Narya | 3 / 1 / any | Narya conditional | T25 |
| smeagol / LM 4/10 | Tamed Wretch | X / 0 / — | none | T25 |
| boromir / LM 5/10 | Captain General of Gondor | 1, movement 2 / 1 / — | none | T26 |
| gimli / LM 6/10 | Dwarven Lord | 1, movement 2 / 1 / — | none | T26 |
| legolas / LM 7/10 | Elven Prince | 1, movement 2 / 1 / Elves | none | T26 |
| merry / LM 8/10 | Hobbit of the Shire | 1 / 1 / — | none | T26 |
| pippin / LM 9/10 | Hobbit of the Shire | 1 / 1 / — | none | T26 |
| strider / LM 10/10 | Dúnadan | 3 / 1 / North | none | T26 |
| balrog / LM 1/5 | Evil of the Ancient World | 0, active 2 / 3 / Sauron | Balrog | T25 |
| gothmog / LM 2/5 | Lieutenant of Morgul | 3 / 1 / Sauron | Gothmog | T25 |
| mouth-of-sauron / LM 3/5 | Black Númenorean | 3 / 2 / Sauron | +1 Standard | T25 |
| witch-king / LM 4/5 | Chief of the Ringwraiths | ∞ / 1 / Sauron | +1 Standard | T25 |

These are five new identities and nine alternative versions of existing characters. Printed SH `/5` also includes a special event **LM 5/5 We Shall Get It**, already separately registered in SRC-03; there is no shortage of fifteen characters. The division of the implementation is disconnected: eight cards T25 and six T26. Gandalf belongs to T25, despite the possibility of private choice at the Council; it is also available without this variant. T26 uses its final version and disclosure mechanics, without the second entry card.

<a id="pierścienie-i-kości"></a>
## Rings and dice

LORDS p.14–15 separates **the Ring identity**, its usual use and special guard capacity. FP can normally change one standard score dice to turn, regardless of the presence of the guard; it selects Narya/Nenya/Vilya. specifically Does not change the Keeper or Lesser Minion score. Using the guard's ability is outside the limit of one usual use The Ring to turn, still requires the proper token and character presence. After using the regular or special token, it goes to the Shadow page. Three Rings for the Elven-Kings recovers the tokens first outside the game, then the Shadow tokens, with the identity selection (SRC-03).

- **Vilya / Elrond:** without additional charge dice stops the die just used, except Will of the
  West. It also applies to the place of laying down after the Fellowship: die does not go to Hunt.
  Test T25: approval action with the nested Hunt/battle, the Vilya window in the right end and reboot
  do not perform the action again.
- **Nenya / Galadriel:** replaces the selected **standard Eye**, removes the cancelled tile for the
  rest of the game. It is not a general cancellation of any Hunt. Test T25: Eye → cost → save → new
  tile and only its further settlement; Eye does not return when preparing Mordor.
- **Narya / Gandalf Keeper:** in unconquered settlement FP releases Narya and any die, activating
  its nation directly At War. Test T25: captured and reflected settlement, correct identity Ring, no
  mistake in ability with adding Narya die.

dice Special is added to the next Recovery after the guard enters/ Minion Not half turn Exception of Narya: the initial Guide gives it in turn 1 Later on, at Recovery Gandalf must be Guide and have at least one retrieved die FP from Hunt; Keeper Eye himself does not meet this. Gandalf becoming a Guide in Fellowship Phase does not get dice Right away (p. 10,19–20 We keep the genus and source. dice , selected wall and removal run. Removing the selected runed result is evaluated in Recovery when White or any Witch-king is in the game, respectively. We do not normalize special dice to standard `+1` .

<a id="wejścia-wersje-i-istotne-okna"></a>
## Inputs, versions and essential windows

**Elrond/Galadriel** (p.17–19): Muster, Sauron or Elves At War, own unconquered Stronghold. Besieged Stronghold the original owner is still unconquered, as previously lost and reflected (p.6). They do not exit Rivendell/Lórien and die on takeover; the level 3 concerns cards effects combat, not traffic. Elven Elite Elrond is the commander for the fight; Galadriel waives recruitment during siege, without abolishing all other conditions. Tests: the extent of the exception, downgrade Elite and control of Stronghold.

**Gandalf Keeper** (p.19–20) does not have a base Captain of the West. The selection of the version is hidden if it is not Guide, also when the Council is off. Transformation White retains the identity of `gandalf`. Balrog, Gothmog and Chief do not meet the condition of Minion for White input; Black Númenorean is not covered by this exception. Test: the three excluded characters themselves do not unlock White; the correct Mouth yes.

**Sméagol** (p.21–22, FAQ p.3) has X-level dependent on the other Companion, it is mandatory to Guide, increase the allocation limit Hunt and may be the victim; never command an army. The pick-up of his tile runs **before** the usual Hunt sequence, so Mithril Coat and Sting does not block the entry nor We Won’t Go Back. The separation or declaration in unconquered city/Stronghold FP removes him and introduces him in Shall Get It. When he becomes the only Companion, Gollum **without enters the event. After leaving, the next cast; Sméagol and Gollum are not interchangeable names for the conditions of cards. The index tests include writing after selection, dynamic level, entry and the exception of the single Séagol.

**Balrog** (p.23–25): The Muster in Moria without condition of its control; control unconquered and lack of siege restricts the additional recruit, not the entrance itself. Activation gives the level of 2, without immediate motion. Another Use Balrog has three alternatives. Fire and Shadow reacts to the route declarations/disclosure, including the region; ignores the reveal tile. Eye eliminates Balrog and Guide, but with Gollum it is rejected without both effects. FP deactivates Will or Character at present Gandalf; outside Moria it means elimination. Test: separate decisions activation/movement, Hunt with Gollum, deactivation and write after the cost.

*The movement of the non-flying Minion** (p.6): Balrog active/Gothmog/Mouth move to level by ignoring the enemy army, but do not exceed the limit. They do not enter or exit the region of their own Stronghold besieged by the opponent. Without the army they do not enter the Stronghold controlled by FP unless they are besieged by SH. Test: do not replace this validation with the Nzgûli flying rule.

**Gothmog** (p.25–26): Black Captain blocks the entrance; Chief does not. The recruiting ability requires an army in a free region and selects Regular or upgrade of the current Regular to Elite. The appearance of Black Captain does not remove figure Gothmoga. Reverse shortens the sentence of removing its dice at the Black Captain input; the complete instruction clearly places the check and removal in **Recover**, as well as other conditions. This is a supplement to the time from the source, not a new errata. Test: Black Captain enters inside turn, figure Gothmoga remains, die removed at Recover.

**Black Númenorean** (p.27): input at FP VP ≥1 or Fellowship on Mordor Track, to unconquered Stronghold Sauron. Another living or previously eliminated Mouth excludes entry. Evil Knowledge has a limit once**au action** and gives an additional round of assault at no cost to Elite. Great Sorcery has a limit once **au turn** and uses Muster as Character only for the current Mouth army. Do not move these meters to the round of battle or to die; restart retains both independently.

**Chief** (p.28–29): an entry in the Fellowship region, but the replacement of the existing Black Captain in the region **his figure**. Still requires Fellowship outside of unconquered Stronghold FP. The reverse switch applies the terms of Black Captain and the same region; only one conversion in the entire game, death blocks both versions. Spear is concerned with the Character deck, not any card with the Character icon; with Palantir it gives only one selection. He Sees responds to the declared, not to the same reveal. Tests: both directions of conversion, loss after conversion, private selection and refusal of reaction movement after a returnal.

<a id="council-i-alternatywni-towarzysze"></a>
## Council and Alternative Companion

LORDS p.30–31 gives you a private version selection at the setup; Guide is public. The disclosure is also made with later inclusion of the Guide, selection as Hunt victim or separation. The lack of Nation icon itself is a strategic property of the selected version and cannot reveal it to the opponent by a list of options or tooltip.

First Fellowship Phase allows you to choose a start outside Fellowship **only** alternative: Boromir → Minas Tirith, Gimli → Erebor, Legolas → Woodland Realm, Merry/Pippin → The Shire. It is not free displacement of any Companion during the setup. No choice leaves them in Fellowship. The treatment as separated involves the disclosure and effects of the icon of the nation: Legolas has Elves, Boromir/Gimli/Hobbits have no icon. Gimli's additional location with Fate remains separate gate SRC-07/T32.

- Boromir/Gimli/Legolas have a level **1**, and **2 can only be separated to move**. The guide can
  reduce the damage Hunt has to 1, minimum 0. FAQ p.3 requires one group to be one group as few
  separate from the same tile; they are not allowed to choose different regions. Champion requires
  an army defender region: Gondor or Osgiliath, Dwarves, Elves, respectively. The bonus also works
  when FP attacks, and retains the limit of five dice.
- Messenger Boromir/Gimliego/Hobbits explicit activates the nation and moves it one step. Prince of
  Mirkwood Legolas moves one step; activation results from its icon. Do not add `unconquered`s from
  the base versions to texts that do not contain it; apply the exact location printed on this
  version and the general rules of lokalizacji/zdolnosc.
- Both Hobbit of the Shire at Fellowship can be eliminated as a cost of the Guide's ability,
  cancelling the **standard** Hunt tile. Both alternative versions are required, not any
  Merry/Pippin. Art of Disappearing concerns elimination in **battle**, not that cost. Return to the
  neighboring region respects the usual move of Companion; FAQ forbids return to besieged
  Stronghold.
- Strider Dúnadan does not have the basic Hide capability of any dice If at the first dice Movement
  Fellowship In turn progress is at least 1 , die He's going to the Fellowship Box instead of Hunt.
  die recover normally in the next turn This is not a new ‘first move’ after each change of the
  Guide nor die retrieved from Hunt for Narya.

To zero characters outside Fellowship SH gets zero tokens; for one **sam picks**one, for more gets both. One token allows Nazgûli/ Minion , second shift of the nation SH They're used instead of action dice , at most one per turn , each time per game. They are not used for the cost of “any die”, are not subject to the Rings and do not count to the permission pass T26 requires both private/public the decision stages and their resumption; examples of tests: 0/1/2/5 characters, both elections SH at one, a test of the other token in turn , the projection of the version before and after disclosure.

<a id="wynik-i-granice-wdrożenia"></a>
## Result and boundaries of implementation

There was no conclusive contradiction that required the new gate. FAQ clarifies the print, and the full manual sets the moment of removal of dice Gothmoga. This is an audit of sources, not the implementation of Lords: T25 still requires T24, T26 requires T25; integration of Kings/Fate remains in its tasks. All scenarios above and in the index are **requirements of future tests**, not a declaration of the listed domain tests.

Checks carried out: `source_lords_characters.py --fetch` — PASS 14 obverses, 5 new identities + 9 alternatives, statistics, disconnects `character_ids` T25/T26 8 + 6 and SHA-256/dimensions sheet; `source_lords_cards.py --fetch` — PASS 14 events/special, 108 cards waist and 5 sheets; `source_cards.py` — PASS unchanged 96 cards and 14 the form of the base; `check_catalog.py` — PASS 30 families/34 windows; `tasks.py validate` — PASS 87 The register keeps `character_count` separate from `content_count` , since the limit of six concerns existing packages of events and T25/T26 are not such packages. Not launched .NET/Unity or hand tests: the game code and its resources have not been changed.

GAP-LORDS-CHAR is closed. gate source T25/T26 is unlocked, their implementation dependencies remain unchanged. The next independent source tasks are SRC-05/SRC-06/SRC-07; their allocation coordinates root.

Implementation update T25, 2026-10-06: eight cards without the required Council has `implemented=true` and [manifest behaviour](../../content/lords-character-implementation-manifest.json), with full regression .NET 1975/1975. Historical audit result above remains source result from 2026-10-05. Six versions of the Council still have `implemented=false`; T25 itself does not turn on the full Lords in the production start. Current protocol acceptance and further boundaries: [T25](../rules/T25-LORDS-CHARACTERS.md).

T26 Implementation update, 2026-10-06 : Six versions of the Council receive `implemented=true` and [separate manifest](../../content/council-character-implementation-manifest.json) , of 23 capabilities, 30 cases in obverses and 8 tests HTTP/PostgreSQL. All 14 the characters now have implementation in separate groups T25/T26 ; full regression . NET : 2058/2058 PASS Dated audit results/checkpoints above remain history. Further limit of full Lords (T27 events and integration UI ) remains public in [T26](../rules/T26-COUNCIL.md) .
