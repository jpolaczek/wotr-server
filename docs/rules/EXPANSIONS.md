<a id="dodatki-i-ich-łączenie"></a>
# expansions and their merging

Sources: `LORDS` — local instruction WOTR005; `KINGS` — WOTR015; `FATE-PHOTO` — photographs of English instruction, confirmed in [SRC-07](../sources/SRC-07-AUDIT.md). Full audits of cards and characters closed separate gate in [gaps.json](../sources/gaps.json); this does not mean that the tasks of implementing expansions are performed.

<a id="l01--setup-i-kości-lords"></a>
## L01 — Setup and dice Lords

Source: LORDS p. 2–3, 7–13. The addition brings 13 cards events and special card Sméagola; one card replaces the base. Two standard size Sméagol tiles go into the initial pool. We do not treat additional large tiles for the collector's edition as additional pieces in the pool.

SRC-03 confirms [14 obverses and their exceptions](../sources/SRC-03-AUDIT.md) and [cards Sources manifest](../sources/lords-card-index.json). `lords.sh.character.17` replaces `base.sh.character.17`; 12 the other events increases the normal deck to 28/26/28/26. The special `lords.sh.special.05` is never shuffled or selected. Its Character icon does not mean a dice charge; once you enter the table it can be removed by the appropriate event effect. The obverses are confirmed, the implementation belongs to T27, and the characters have a separate SRC-04.

dice Keeper and Lesser Minion retain the owner, source and mark of the removed wall. With several special dice the player chooses one by the rules; Eye forces the choice of the Eye, but different dice with the Eye does not have to be strategically equivalent. The order of the selection depends on the number of dice thrown, with the FP draw first. We check the rules for removing the selected dice in the appropriate recovery, not immediately after drawing the symbol in UI.

The Eye Keeper can power Hunt Shadow. We do not identify it with dice FP released on Fellowship traffic when checking the mandatory allocation for the next turn. The exact schedule of cooperation with Ruler requires a T31/T33. settlement table.

T24: [accurate walls, costs and window limits](T24-LORDS-DICE-RINGS.md) describes the `src/Wotr.Domain/Lords` module. Tests `LordsDiceTests` and `LordsRecoveryTests` check 30 walls, election queue, coercion Eyes, delayed removal, Narya, separate origin Hunt and ban on changing special dice with Ring. Setup of this module prepares five dice and three named Rings. The composition of the decks and Sméagol tiles integrate T25/T27; public start Lords remains blocked to complete content.

<a id="l02--postacie-sméagol-i-pierścienie"></a>
## L02 — Characters, Sméagol and Rings

Source: LORDS pp. 6–29 and card. Elrond, Galadriel, Balrog, Gothmog, Sméagol and alternative versions of Minion need separate entry, movement, ability and exit conditions. Not all Servants fly; “unconquered” requires checking the current control of the original owner, also after the rebound.

[SRC-04](../sources/SRC-04-AUDIT.md) confirms the [14 characters obverses](../sources/lords-character-index.json) set, connections to base versions and Ring rules. Eight cards belongs to T25, six only to Council/T26. Levels have separate meaning: Elrond/Galadriel 0, but 3 for cards Combat; alternative Boromir/Gimli/Legolas 1, but 2 only for movement; Balrog 0 or active 2; Sméagol X. The lack of an icon of the nation on an alternative card is an open version property.

T24 implements the separate costs and windows of Narya, Nenya and Vilya, described in [module specification](T24-LORDS-DICE-RINGS.md). Narya activates the nation settlement, Nenya permanently lists standard Eye, Vilya stops the just used result outside Will of the West throughout action. Tests use explicit character permissions; their full entry/exit belongs to T25. Sméagol modifies the composition/state of Fellowship, Guide and the tiles pool; we do not implement it as a normal Companion with a level.

T25 implements [contract of eight characters](T25-LORDS-CHARACTERS.md) : Inputs/Outputs, One-time Switch of Witch-king, Keeper combat/recruitment , private choice Grey, active costs and separate counters, Fire and Shadow, both Chief reactions and full Sméagol cycle. [Manifest](../../content/lords-character-implementation-manifest.json) binds every ability and 40 source cases with tests. In Shall Get It hits the public table at the proper departure of Sméagol; effect card , additional tiles from the event and the rest of the deck belong to T27. The public launch of Lords requires complete integration; UI expansions is the scope UI-09/T34. Status acceptance specify the register.

Three T27 packages implement all 13 ordinary events and special We Shall Get It: [FP](T27-LORDS-01.md) , [Shadow](T27-LORDS-02.md) , [Madness and card special](T27-LORDS-03.md) Their manifesto has 13 applications Event/Combat and automatic input and response SpecialEvent without Combat. Madness keeps the recruitment branch before loss; We Shall Get It settles before exchanges FP T27 checks completeness cards and cut into [receipt](T27-ACCEPTANCE.md) and [14 cards report](T27-CARD-COVERAGE.md) . The integrated set of eight configurations belongs to T33, its UI ed UI-09/T34 ; gate The peace starts with this acceptance - It's all set. cards does not remove this gate .

Gandalf Keeper is an optional private version also without the Council. die Narya depends on the Guide on Recover and recovery dice FP from Hunt; choosing a Guide only in Fellowship Phase does not add it in this turn . Elrond/Galadriel/Balrog/Gothmog • If you are a doctor, you may not be able to die special from next turn and removal after death or selected runed wall follows in Recovery. Obverses Gothmoga shortens the Black Captain condition: full instruction p. 26 retains this phase and does not remove the form itself. The rings have separate identities; the guard's ability does not use the limit of one normal use of the Ring on turn (p. 14–15 ).

## L03 — Council of Rivendell

Source: LORDS p. 30–31. Versions selected Companion are private, except Guide and moments of disclosure: taking the role of Guide, losing in Hunt or separating. The first phase of Fellowship allows you to declare eligible alternatives of Companion as starting outside Fellowship.

One such Companion gives Shadow a choice of one action chip; more than one gives both. The chip replaces action in the right window, has a specific effect, is one-time and one at most can be used in turn. It does not count to pass, is not any dice and cannot be changed with the Ring. The absence of an icon of the nation on an alternative form has real effects.

SRC-04 binds the Council to six card T26 and Gandalf's version of T25. The own icon is preserved by Legolas (Elves) and Strider (North); Boromir, Gimli and both Hobbits do not have it. FAQ 1.2 p. 3 : separation of alternative Guides resulting from the same tile form one group; Art of Disappearing Hobbits respects normal movement Companion And he won't let you enter the besieged Stronghold . Detailed sources save [audit SRC-04](../sources/SRC-04-AUDIT.md) and implementation and execution of cases describes [T26](T26-COUNCIL.md) The Manifesto of the Six Averses includes 23 and 30 cases; full regression domeny/HTTP/PostgreSQL: 2058/2058 PASS The production start of the full Lords still requires T27 content and further integration UI .

<a id="k01--ruler-dice-i-żetony"></a>
## K01 — Ruler dice and tokens

Source: KINGS p. 6–7, 10. die Ruler enters the next turn after the condition has been fulfilled by suwerena/wodza. In the appropriate recovery phase, it disappears when there is no form of maintaining access; it can return after the condition has been fulfilled again. The Shadow Ruler must be thrown, it is not the usual dice allocation Hunt.

After the throw, the obligation to reject dice: first Shadow, then FP, with the correct moment of the transfer of the eye. Shadow may reject the Ruler with the Eye, but not the usual die with the Eye. With the Lords, you cannot use Keeper/Lesser Minion as the rejected dice.

The token Ruler works directly before action Ruler, selects the right second ability and requires its use. In the same turn player does not use this token and Ring at the same time. UI must distinguish normal Ruler score, special capacity and action after the release of the token.

[Implementation T29](T29-RULER-CHIEFTAINS.md) retains two physical dice, Recovery and order SH discard → Eye to Hunt → FP discard → action. Token and target capacity are one cost and command; you cannot use the changed dice for another purpose. Prohibition The Ring also includes Keeper and the cost of card tableware. Dismay and the complete order with the Lords belong to T31.

<a id="k02--suwereni-przebudzenie-i-zepsucie"></a>
## K02 — Sovereigns, Awakening and Disorder

Source: KINGS p. 8–13. Each sovereign has its initial state, Awakened, Corrupted or removed. Awakened, it acts as Companion with the appropriate card; corrupted is not a normal character/Minion and does not move like them. Retardability after waking and returning the tiles after changing the state requires explicit transitions.

The attempt to corrupt is an alternative to normal action Shadow provided, among others, that FP has an unused dice and more than one authorized Eye in Hunt. Shadow selects the authorized Eye to release, selects up to three tiles according to the number of remaining Eyes and variant rules. Usually, it selects one covered tile and the rest reveals and returns. FP does not receive access to the pool or secret values.

Eye/tiles special selection requiring re-election and Kings tiles have their own procedure. Kings tile interrupts selection and works according to its value in this procedure. The Hunt manipulation interventions are not automatically legal when attempting to corrupt. Recover the sovereign and remove the Corrupted Sovereign require the appropriate characters/icons, cost and window — not the ordinary army attack.

[SRC-06](../sources/SRC-06-AUDIT.md) confirms [13 obverses and tokens](../sources/kings-character-index.json). The initial sovereign is Leader FP, Awakened is Companion, and Corrupted does not meet the requirements of Companion/Character/Minion and does not die due to battle alone. The resistance of Brand/Dáin/Denethor/Théoden/Thranduil is 2/4/3/3/4. respectively The weaknesses range is not identical: Brand blocks Leader/Elite North everywhere; Théoden blocks Leader/Elite in Rohan regions; Dáin and Denethor in one designated Stronghold; Thranduil only Elite in Woodland Realm. Awakening with movement applies the Awakening level Awakened and legal route, not teleport. Ruler token is the identity of the sovereign and changes Ruler only to its second ability with immediate use.

[Implementation T28](T28-SOVEREIGNS.md) and [manifesto of the obverses](../../content/kings-sovereign-implementation-manifest.json) bind transformations, 20 capability and 47 source cases with behavior tests. Kings tile has zero only in failure; normal Hunt uses printed 1/2 and normal odrzutu/refill. Policy offers a random return of the covered tile, and Anárion and Far-sighted retain the player's open choice and the cost to the end of nested decisions. The Ruler/tokens pool is implemented in T29, variants are owned by T31, handling new decisions in Unity UI-09.

<a id="k03--dark-chieftains-i-karty"></a>
## K03 — Dark Chieftains and card

Source: KINGS p. 7, 14–16, 19 and card. The conditions for entry of the chiefs and their abilities are implemented with cards. In the appropriate battle, you can abandon the effect played card combat for the master/water capacity. card is still played; it does not become a "card" for other effects. Cancellation of effects and initiative apply the special rule.

19 cards events Kings includes 11 substitutions and 8 new. The substitution manifest includes a page, deck and number, not only a title. Integration with Fate disables two specific substitutes.

Sources of K03 confirm SRC-06 The chiefs choose to replace card before the reveal; Brand after the reveal. card remains played and must be legal, replacement effect is cancelled. Black Serpent has Combat-only bonus in the initiative 3 , Uglúk Combat and Leader re-roll in 3 , Mirkwood Leader re-roll in 4 and optional cancellation in 0 for Swarm of Bats. Théoden has a passive Leadership 3 in the first round without sacrificing card Brand and Théoden are implemented in T28; T29 combines full three obverses, five tokens and V12s with [execution manifest](../../content/kings-chieftain-implementation-manifest.json) The chiefs' costs are before the reveal, the cancellation does not return them, and Sorcererer still sees played. Uglúk does not create Hunt re-roll: gives + 1 only authorised reroll, including from hidden Fellowship .

[SRC-05](../sources/SRC-05-AUDIT.md) confirms [19 full of Kings events](../sources/kings-card-index.json) : 11 replacements and eight new cards Database+Kings has 104 card From Lords 116 Two promos of the Seeing Stones are out MVP Exception. KINGS p. 18 disables the replacements of Brand and Dáin at Fate; it does not determine the order of loading files. [T30](T30-ACCEPTANCE.md) integrate complete decks with MatchFactory ; [report](T30-CARD-COVERAGE.md) bind both halves 19 cards , 11 replacements, eight expansions and Fate exceptions with tests. Lobby remains behind gate full integration T33/UI-09/T34.

## X01 — Lords + Kings

Source: KINGS p. 17–18. When you try to break Sméagol tiles, skip and draw again. dice added to Hunt by Captain of Despair cannot pay for the sample and do not increase the selection of tiles; the origin of dice must be preserved.

The authorised Eye Keeper/Lesser Minion counts according to the integration rule. Their removal to pay the sample has a specific place of set-off. We do not normalize all Eyes to the identical chip Shadow.

With the Council Shadow, you can use the action chip together with the authorized dice to enter the Dark Chieftain input with the exception of the normal condition. Companion without the nation icon cannot perform the Corruption/sovereign deletion tasks that require this icon.

<a id="x02--dismay-i-warianty-kings"></a>
## X02 — Dismay and variants of Kings

Source: KINGS p. 17–18. Dismay is optional; default enabled for Lords + Kings. Proper use of the Ruler chip or death of the awakened sovereign gives Shadow Dismay chip. After the throw allows the appropriate Ruler reroll and is removed. The exact order in many elections is set by T31 based on sources.

- Visible Corruption Tiles: values are public; the card reference to hidden Corruption Tiles means
  in this variant tiles other than Kings.
- Sequential Corruption Tile Draw: the player can stop choosing in the allowed limit; he uses the
  last selected tile. He does not retain the best selection of all previous ones.
- Renovating Ruler Tokens: no Ruler chips are used; Dismay is awarded due to the death of the
  awakened sovereign. In MVP the variant available at Lords + Kings.

Options are part of a closed configuration and recording, never a local client preference.

T31 implements [integration table and variants](T31-INTEGRATION.md) Separate decisions keep the order of Lords → discard SH → Reject FP → Dismay/Lore → action . SH selects the order of two of your own voluntary effects. Selected special Eye counts to Corruption And after paying, they go to the used FP Captain does not give you any cost or additional selection. The council can pay for the entrance of the token commander together with the eligible dice And the lack of the icon of the nation still blocks the cleansing of the sovereign. Sequential only keeps the last tile; Sméagol exchange/ Eye/Special is mandatory. Visible changes the visibility of assigned tiles and the displayed meaning of “hidden” without revealing the ongoing selection. Removing Ruler Tokens creates only Dismay after the death of Awakened. acceptance client expansions and full setup still belongs to UI-09/T33/T34.

## F01 — Fate of Erebor

Sources confirmed in [SRC-07](../sources/SRC-07-AUDIT.md) : Full self-contained WOTR018 and collectors' chapter, eight obverses, and KINGS p. 18–19. [Index](../sources/fate-index.json) records origin, public replacements and required tests. four/o The smile of physical overlaps between releases does not change the rules.

- Erebor is the original Stronghold Saurona. Shadow starts with zero points behind Erebor; the gain
  by FP gives two. Dale becomes a neutral fortification, Iron Hills town of Dwarves. Neighborhood
  and impermeable boundaries remain the same.
- Final setup: Erebor without Dwarfs and with two regular Sauron; Dale without North; Carrock has
  two regular Norths and a commander; Ered Luin two regular Dwarfs; Iron Hills two regular Dwarfs
  and commander. These are the final values, not expansions to the base setup. Dwarfs are active,
  without changing the political degree.
- Six explicit swaps concern FP Character 17, FP Strategy 4/19/22 and SH Strategy 20/22. They do not
  increase deck size. [T32-CARDS](T32-FATE-CARDS.md) implements full programs of both applications;
  [manifest](T32-CARD-COVERAGE.md) binds each sentence of the obverse to performance tests.
- [Both versions of Gimli](T32-FATE-GIMLI.md) represent one figure, with separate ID obverses Fate.
  The changed capabilities refer to Iron Hills/Ered Luin; the Council variant allows you to start an
  alternative in one of these regions instead of Erebor. The usual card retains the level of 2 and
  the Dwarf icon, the alternative level of 1 and the lack of an icon (2 only to move).
- New Cities, only available at Fate, turns Ered Luin and South Rhûn into a city for all purposes.
  The nations and the original owners are preserved.

T32 implements this diff map, setup, deck and character; source checker is not a gameplay implementation. Victory thresholds remain base.

[T32-SETUP](T32-FATE-SETUP.md) implements `fate-setup-1` : map metadata, final deployment, reserves, active Dwarfs and New Cities. Movement, recruitment, hit thresholds, Hunt , entry of forms and regional conditions cards read together board with configuration. Points use the owner of the setup, regardless of the dynamic White Hand. `RegionOverrides` is a full set of overwriting for both sides, used by Unity without Fate rules. Tests `FateSetupTests` , `FateBoardRuleTests` and `FateCardRegionTests` binding examples to execution and reboot HTTP/PostgreSQL. T32-CARDS adds public replacements and programmes, with sustainable regional choices, figures and variants. T32-GIMLI adds two versions, the private Council, both starting sites and full of capabilities, with [Source manifesto-test](T32-GIMLI-COVERAGE.md) . [acceptance T32](T32-ACCEPTANCE.md) confirms three components, accurate diff sources and performing all related tests ( 79 methods / 192 The T33 will check the entire configuration configuration, UI-09 The full Council interface.

<a id="x03--kings--fate-i-pierwszeństwo"></a>
## X03 — Kings + Fate and priority

Source: KINGS p. 18–19. Dáin and Brand do not appear as sovereigns. Kings does not replace cards Dáin Ironfoot’s Guard and King Brand’s Men; the appropriate card of Fate script applies. It is not enough to impose all substitutes sorted by the name expansions.

Construction plan of the content:

1. Select the right edit database.
2. Set the setup rules and map for the expansions set.
3. Use a clear manifesto cards, characters, dice and tiles, with exceptions to integration.
4. Verify the uniqueness of cards slots, the expected number and the lack of inaccessible
   components.
5. Then shuffle, select and run private preparation decisions.

<a id="macierz-odbioru"></a>
## Acceptance matrix

The module conditions and unresolved exceptions are also found in the [catalog](catalog.json); the private decision windows in [decisions.json](decisions.json). [TRACES](TRACES.md) sets the limit of specifications and sources. Closed audits of SRC confirm sources; readiness to implement still determine the task dependency.

| Configuration | Specific control |
| --- | --- |
| Base | 96 cards, base characters and setup. |
| Base + Lords | Special dice, Sméagol, Rings, Council on/off. |
| Base + Kings | The Ruler, the sovereigns, the hidden tiles and two independent variants of the tiles. |
| Base + Fate | Map, setup, six replacements, Gimli, New Cities on/off. |
| Base + Lords + Kings | Order dice, origin of the Eye, Council, Dismay, Renovating Ruler Tokens. |
| Base + Lords + Fate | Both Gimli versions and the beginning outside Fellowship. |
| Base + Kings + Fate | No Dáin/Brand, disabled two King replacements. |
| All | Full manifesto, all the above intersections and variants. |

[T33](T33-COMPOSITION.md) defines the exact folding plan and 93 legal configurations with 512 explicit combinations. Further 256 unset Dismay checks the default value (69 legal). Each legal configuration passes the actual setup to the first action, comparison of private projections and two rebooted approvals HTTP/PostgreSQL. Targeted sequences include overlapping effects, not only batch launch. Illegal server combinations reject before room acceptance and before RNG. The deck data conflict indicates slot, rule and source page; diagnostics do not receive party status. UI-09/T34 still receive expansions client.

<a id="potwierdzone-erraty-i-granica-aktualności"></a>
## Confirmed Erats and News Limit

[SRC-08](../sources/SRC-08-AUDIT.md) maps the full FAQ 1.2 to the tasks. 1.2 version concerns the base and the Kings, not the Kings. pinned instruction `BASE-2.4.1` retains a description of the losses from events known from 2.3; the complete comparison has closed GAP-FAQ. The separate Lords/Kings/Fate obviates confirm the audits SRC-03–SRC-07 described above. We do not replace the solution with the fan implementation text.

Checkpoint T26 (2026-10-06): a private version selection, first home decision and both types of SH tokens have implementation, projection, validation and reboot tests. This is not yet acceptance six full obverses. Contract and actual results: [T26 — Council](T26-COUNCIL.md).

T30-CARDS-01 supply [Six first Kings events](T30-KINGS-01.md) with both halves and [source manifest](../../content/kings-card-implementation-manifest.json) . Stewards/Riders/Brand maintain a clear selection of branches and appropriate selection of the Strategy; Wisdom/Dark have different rules of refusal after private inspection. Aid checks the region after traffic, Banner sacrifices actually available full Leadership selected participant, God of Old settles separate losses before Combat roll. Awakened Sovereign is also Companion for Brave Stand. Welcome/Folly are an open point of integration; card Shadow and other 13 events still belong to further packages, and start the entire configuration to T30/T33/UI-09/T34.

T30-CARDS-02 extends the catalogue to 12 cards Kings: [second pack](T30-KINGS-02.md) adds Dáina/Thranduil, Service, Villages, Welcome and West. Recruitment retains the choice of type/region and the restriction of different nations; West's attempt returns to the event without an additional Eye; the restrictions of Welcome also apply to events and awakening in combat. Manifesto saves source sentences and tests. Variants Visible Corruption/Sequential Draw and their texts/Integration remain within the T31 range; the current card use the common procedure draws/removal, and tests of this package do not declare acceptance unimpled variant configuration.

T30-CARDS-03 extends the catalogue to 18 cards Kings: [Shadow strategy](T30-KINGS-03.md). Five further substitutions of base slots and additional Jerkír have their own conditions of event, initiative and extinction. Denethor’s Folly includes two regions and does not require siege; Threes distinguishes the usual Muster from the possibility of Ruler and expires on the actual Corruption/Political shift; Stormcrow retains the order of withdrawal → the secret tile → the loss selected by FP. Jerkír offers once on turn the apparent cost of Muster as Event, also for selection and in collaboration with Orthang. The variants of the tiles, Dismay and full Lords/Kings/Fate remain in T31/T33; the last card and the connection of the set are included in T30-CARDS-04/T30..
