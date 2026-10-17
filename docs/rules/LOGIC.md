<a id="logika-gry-podstawowej"></a>
# Base-game logic

This description defines the implementation model and edge cases. It does not
replace complete card or rulebook text. Page numbers refer to PDF and printed
pages in the local files; their numbering agrees in these three rulebooks.
Rule identifiers are indexed in [catalog.json](catalog.json).

Comparison source `BASE`: [War of the Ring Second Edition](../../WOTR001-Rulebook_EN_web.pdf).
After SRC-08, the rule catalog points to the [official rulebook 2.4.1](../sources/materials/base-rulebook-v2.4.1.pdf)
as `BASE-2.4.1`; BASE in the locators below retains the page numbering.
The [audit](../sources/SRC-08-AUDIT.md) found no changes to 1v1 rule meaning
relative to 2.3. SHA-256 checksums in the [source list](../sources/README.md)
identify versions. [EXPANSIONS](EXPANSIONS.md) describes expansion exceptions.

<a id="b01--setup-i-tożsamość-komponentów"></a>
## B01 — setup and component identity

Source: BASE pp. 4–17. Deck composition, starting regions, figure counts and
political state come from versioned data. Setup is deterministic apart from
explicit shuffles/draws and private variant decisions. Apply expansion changes
before drawing starting cards.

Every figure, die, card and token has a persistent ID and a type definition.
Army counts are a projection of figures. Distinguish reserves, casualties,
the board, the Fellowship and removal from the game. A figure cannot be in
two places at once.

Do not reconstruct the complete map by eye from artwork: every region,
adjacency, impassable border, nation, settlement and original owner requires
verification in T04. Card/character sources have separate gates.

<a id="b02--cykl-tury"></a>
## B02 — turn cycle

Source: BASE pp. 18–19. Order:

1. Recover dice, accounting for additions/removals; draw one card from each
   deck and resolve the hand limit.
2. Fellowship phase: declare position, perform permitted healing/activation,
   and select a Guide.
3. Allocate Shadow dice to the Hunt.
4. Roll action dice and resolve resulting expansion choices/effects.
5. Alternate actions, starting with the Free Peoples; the player taking an
   action does not necessarily own its reactions.
6. Check military victory; start a new turn if the game continues.

An automatic step advances to the next actual choice, not through the entire
turn. An effect requiring a card discard pauses progression to the next stage.

<a id="b03--kości-pas-i-rezygnacja"></a>
## B03 — dice, passing and forfeiting

Source: BASE pp. 18–21. FP initially has 4 ordinary dice, Shadow 7. Gaining or
losing a character's die takes effect when recovering dice next turn.
Availability is not just a count: a die has an owner, type, origin, result,
state and an optional removal marker.

Passing is legal only with fewer unused action dice than the opponent.
Discarding one of your dice without effect is a separate operation. This also
applies after using an Elven Ring: [BASE 2.4.1, printed p. 21](../sources/materials/base-rulebook-v2.4.1.pdf#page=21)
— “He can also skip or pass the action, if eligible to do so.” Waiting for an
action after a Ring therefore does not prohibit passing or forfeiting a die.
A pass ends that wait and hands over play; the Ring stays spent (QUAL-04,
`RingPassTests`: 2/3 dice allows passing; 3/3 and 4/3 do not; a Shadow Eye
created with a Ring reduces Shadow's die count). When only one side has dice,
it takes its remaining actions. Council tokens do not increase the die count
for passing.

The model distinguishes available, paying for the resolving action, spent,
in the Hunt, discarded this turn and removed from the pool. The UI may combine
some visual representations without losing their semantics.

<a id="b04--rodzaje-akcji-i-koszt"></a>
## B04 — action types and costs

Source: BASE pp. 20–21. A die determines the action type, not merely a resource
to subtract. Army: move up to two different armies or make one attack;
Character: move/attack with one army that has the required Leadership,
character/Fellowship actions or a card of the appropriate type; Muster:
politics, recruitment when its conditions are met, or a card; Event: draw from
a selected deck or play any event card. A combined result allows one category
to be selected. Will of the West retains its additional uses for transforming
or bringing characters into play.

A cost identifies specific components. The server validates them immediately
before execution. Equivalent ordinary dice may be selected automatically,
but a special die, Will of the West, a Ring or a token with different future
value requires a deliberate choice.

<a id="b05--pierścienie-elfów"></a>
## B05 — Elven Rings

Source: BASE p. 21. FP uses a Ring and passes it to Shadow; Shadow uses it and
removes it from the game. A player may use at most one per turn. The result
changes before the normal action, which may use a different die from the one
changed.

FP cannot obtain Will of the West this way. Shadow may change an available die
to an Eye, move it to the Hunt, then take a normal action or, if eligible under
B03, pass or forfeit an action (BASE p. 21, QUAL-04). An Eye cannot be changed
back to another result. Resolution and persistence must preserve this
intermediate step; the UI must not require use of the changed die.

<a id="b06--karty-i-czas-działania"></a>
## B06 — cards and timing

Source: BASE pp. 22–23, 29. The base game has 96 cards: 24 in each of four
decks. A card has an event use and a combat-card use; supporting one use does
not complete its implementation.

The hand limit is six; discard excess cards face down after the entire current
draw procedure ends (FAQ 1.2, p. 1). In the recovery phase, draw both cards
before discarding. An empty deck is not reshuffled. Effects kept on the table
store their source, scope, duration condition and ending time. Execute an
instruction to the extent possible and required by its text; partial execution
does not authorize skipping a mandatory part.

A card exception overrides only the rule it identifies. Describe play
conditions, costs, targets, order, reaction windows, partially unavailable
targets, expiration and visibility separately. Do not reduce everything to
`Apply()` without pausing for choices.

<a id="b07--polityka-i-aktywacja"></a>
## B07 — Policy and activation

Source: BASE p. 33–35. Nation activity and position on the political track are separate states. FP can enter At War by proper progress only after activation. Movement, recruitment and attack check nation limitations, not only sides.

Attack and take-over settlement may cause separate political steps. T21-SH-CHAR-04 their causes are remembered and activation and displacement are decided at the end of the whole action according to a confirmed explanation of 2021 r. Retreat does not activate the nation by entering or Companion Effects cards may be of particular importance as an attack. In T11, you need to write a table of triggers with sources, including derogations for Ents and the Dead; do not assume that there is no standard battle means no political consequences.

T11: [Trigger table, command and costs](T11-MUSTER.md) corresponds to the modules `Politics` and `Armies`. `AdvancePolitics` determines the cost by DiceRules; the nation's name comes from payload, but the owner and legality of the server.

<a id="b08--rekrutacja-i-rezerwy"></a>
## B08 — Recruitment and reserves

Source: BASE p. 20, 26–27. Ordinary recruitment requires the At War nation and appropriate free, friendly sediments. Typical combinations: one elite or two figure with permitted types in two different settlement. A smaller number is required to thoroughly check the rule; UI does not force the issue of non-existent reserves.

Recruitment by card may have other restrictions, openly encoded for effect. We do not reject it globally solely because the nation is not At War or Stronghold is besieged. The reserves of FP and losses of FP are separate; losses of Shadow return to reserves according to the rule.

T11 checks the entire set of deployments before mutation. Oversize after action creates `RemoveExcess`, keeping the cost of Resolving to answer. The lone commander of FP is immediately removed (BASE p. 8) and the preview reveals a known loss. `MusterTests` checks rejections, exceptions, boundaries, options and serialization; [details](T11-MUSTER.md).

<a id="b09--armie-ruch-i-limit"></a>
## B09 — armies, movement and limit

Source: BASE p. 26–28. Friendly field units in the region form an army; we model separately a garrison besieged Stronghold. Regulars and elites count to limit, commanders and characters do not.

The movement selects specific figure and a neighboring legal target. When moving to two armies, no figure moves twice in this action, also after being connected to another army. The movement with Character requires the right Leadership in the moving group. When split, it is necessary to check whether the commanders of FP remain without units; characters and Nazgûle may be subject to other rules of self-remainance.

The field limit is 10 units at the end of action. The allowed transitional excess cannot cause the rejection of the entire legal action; the engine creates the decision to remove the excess at the correct time. Removing the limit is not automatically treated as a combat loss.

T12 implements one planer for validation, preview and execution; [description of the movement](T12-MOVEMENT.md) also includes control of the blank settlement, strengthening and lowering siege. `MovementTests` checks two moves, division, re-use of figures after the merger, mixed composition policy, Leadership and round-trip excess. Fortification is not settlement (BASE p. 10, 32): does not recruit, change control and cause political shift for capture.

<a id="b10--postacie-i-przemieszczenie"></a>
## B10 — Characters and displacement

Source: BASE p. 24–25, 37–39 and card form. Level, Leadership, icons of nations, entry, ability and exit conditions are separate data. The Companion groups have a range of the appropriate level; Names and different Minion do not have to use the same movement rule.

The solitary character in the opponent region is not automatically a participant in battle. Separation from Fellowship, entry into the game and transformation are different command. You need to keep the effects for dice on the next turn and the legality of subsequent versions of the same character. The exact capabilities require verified cards, not summaries from memory.

T12 provides a model of character participation in the Army movement (level, Leadership, nation icon), public projection of figures on board and mechanism testing on explicit fixtures. It does not replace the T20 catalog and capabilities or the movement of independent characters.

SRC-02 Confirms [all 14 cards and their semantics](../sources/SRC-02-AUDIT.md) Index separates identity from version, no level from movement ∞,  Guide Golluma from Companions and the abilities of Grey and White. A fixation of the historical entrance is required Minion for the condition of transformation White; removal Minion does not take back that fact.

T15 implements [statistics directory, separation and independent movement Companion](T15-FELLOWSHIP.md). Groups use the highest level, each figure at most in action. The entry to Stronghold controlled by Shadow ends traffic also when it was originally FP. Friendly siege blocks the input/exit of Companion, but not Fellowship. T20 complements this move with Nazgûle/Minion and character abilities.

T20 implements [14 characters and all abilities](T20-CHARACTERS.md), with [a manifesto of tests of every capacity](../../content/character-implementation-manifest.json). Includes inputs and versions, traffic Minion, Shadowfax, policy Companion, Voice and disposable cost Messenger. The historical input Minion and dice on the next recovery do not depend on the current presence of figure.

<a id="b11--rozpoczęcie-bitwy-i-zobowiązanie-kart"></a>
## B11 — Start of battle and commitment of cards

Source: BASE p. 28–29. Check the right to attack, the origin of the army, participants, left figure and the nature of the battlefield. card the attacker is selected before the decision of the defender; both content are revealed simultaneously after the election. The reverse of the declared card is visible to the opponent.

The lack of card attacker does not take away the choice of defender. With an equal initiative the first is card defender; with different lower initiatives it is earlier, taking into account a particular text window. You cannot sacrifice Leadership already cancelled. Additional attacks cards do not automatically inherit all modifiers and spreads of ordinary combat.

The T18: [complete procedure and examples](T18-BATTLES.md) implementation. Participants and rear guards have separate identifier saved and each part must contain an army unit. Return after nested attack card resumes the correct effect cursor. The 96 cards combat programs implement T21 packages; the T18 test programs are not their implementation. [T21-FP-CHAR-01](T21-FP-CHAR-01.md) delivers six full cards including It Is a Gift, Blade of Westernesse and Mighty Attack. The conditions of attendance at the play are separate from the effect requirements, and the Leadership cost and the box replacement are approved together.

<a id="b12--rzut-i-straty"></a>
## B12 — Toll and losses

Source: BASE p. 29–30 . Maximum five dice combat kick and five eligible metastatic failures dice . Modifiers are accounted for in their windows; natural 1 remains a box, natural 6 A hit.

After the throws, both sides receive hits; losses are first chosen by the attacker. One hit: remove the regular or demolish the elite. Two: you can remove the elite. Demolition requires regular of previous losses, and if missing from available reserves. If there is no regular, the elite is eliminated without additional benefit. UI-19 implements the accepted none/↓/× cycle to figure; shows the status, order and full meaning in the tooltip. The gesture does not change the type of operations Downgrade/Remove or their rules.

T18 records the natural score, threshold and modifier separately. Players indicate allowed failed dice to be met, from zero to limit; both choices precede a joint settlement and the first choice remains private. The losses of the ordinary fight are determined before the attacker is eliminated and still burden the defender. The special attack has its own frame of immediate losses, without reroll and without modifiers card from the opponent. Degradation takes the regular of the same nation and joins its ID to attend.

In the ordinary battle, the death of all units also means the removal of accompanying commanders and characters according to their exceptions. We do not transfer this rule without checking it out to the loss of the event: BASE p. 23 distinguishes between the fate of the commanders of FP and characters/Nazgli outside battle. Each loss mechanism must give a reason.

T20 adds Captain of the West to the number dice (limit five), White Rider before card first round and Sorcerer after the full completion of the first round. The selection response waits for the reverse and the reduction of the elite when extending siege. Witch-king must remain a live contestant; canceling card does not invalidate its play.

<a id="b13--kontynuacja-odwrót-postęp"></a>
## B13 — Continuation, retreat, progress

Source: BASE p. 30–32. After the round, the attacker decides to abort. Only on continuation will the defender choose a legal retreat or stay. The retreat requires a legal region available, and the possibility of shelter to Stronghold has separate rules.

After that, battle set up a living army, the possibility of entering the attacker, control of region/sediments, points and political effects. Do not assume that the indication of the attack immediately moves the army to the target region.

<a id="b14--miasta-fortyfikacje-i-oblężenia"></a>
## B14 — cities, fortifications and siege

Source: BASE p. 31–32. City/fortification changes the hit threshold in the first round; siege has a separate mode. After going back to Stronghold, the garrison limit is five units and the excess goes to the reserves according to the rule. The presence of the siege army in the region does not mean gaining Stronghold itself.

siege is standard end after one round; extension requires the right choice and degradation of the attacking elite. The garrison does not use a simple retreat. Outbreaks, rescue, participants and control have open variants of the battle machine. UI distinguishes the besieged from the garrison without opening a separate board.

T19 implements [modes Field/Siege/Sortie/Relief](T19-SIEGES.md). The shelter precedes the optional progress of the opponent; only progress establishes siege and a limit of five. When the defender refuses to return to the field without removing the excess. The crash and rescue hit 5+, the assault on 6+ for each round. The check is checked after both ordinary losses: without a living army in the region, mutual destruction does not gain Stronghold. The winning out does not offer progress outside of its region, and the rescue does not use Leadership+ for each round. The reduction of the elite and the usual losses share the physical exchange of figures.

<a id="b15--drużyna-postęp-i-deklaracja"></a>
## B15 — Fellowship: progress and declaration

Source: BASE p. 36–39. Public figure indicates the last known position, and progress is an abstract distance. We do not store the secret, pre-choice route of each movement. The hidden movement Fellowship increases progress, starts Hunt, and the used die goes to Hunt after the settlement of action.

The declaration in the Fellowship phase and the forced disclosure are different procedures. The declaration allows you to choose a legal position within the scope of progress, also to stay in place, reset progress and keep hidden. The disclosure resets progress and sets Revealed, with other limitations of purpose and consequences of the Shadow fortress. Before the next ordinary move revealed Fellowship must be hidden.

When choosing a route with different effects, the decision belongs to FP. The engine checks the passages and prohibited boundaries; it does not automatically select a favorable route. Do not identify the route Fellowship with the Army route.

<a id="b16--przewodnik-leczenie-oddzielenie"></a>
## B16 — Guide, treatment, separation

Source: BASE p. 36–39, card. Guide is a specific character; the change takes place in the permitted window and with the appropriate conditions of poziomu/zdolno. The treatment and activation of the nation is due to a legal declaration in the appropriate settlement, not from each disclosure.

Separation of one Companion or group selects range by progress and level, checks legal sites and updates the composition and Guide. Companion, Trustees and Gollum are not interchangeable elements of one list. A change of composition can change the later allocation limit of Hunt.

T15: Declaration once per week turn, route disclosure and additional obligations to Hunt, hiding behind Character, the choice of the highest level and Gollum at the empty lineup are in silniku/serwerze.   FAQ   1.2  s.  1 prohibits disclosure only in the original settlement   FP still controlled by FP Treatment requires the same affiliation/control; activation on declaration requires a native unattainable settlement A nation. T21-FP-CHAR-03 corrects the previous omission of the check in accordance with BASE  s. 32 and Chapman's explanation of activation by settlement.  [Specification and testing](T15-FELLOWSHIP.md) They separate these cases. T16 clears additional tiles; UI-08 builds interaction support.

<a id="b17--przydział-i-skuteczność-polowania"></a>
## B17 — allocation and effectiveness of Hunt

Source: BASE p. 19 , 40–42 . Shadow assign normal dice in the number limit Companion but he can assign one without even Companion The obligation of at least one depends on the appropriate dice Movement FP recovered from Hunt in the previous phase. Same Eye with expansions They may have other meanings.

With a throw of up to five dice Hunt; the modifier depends, among others, on previous FP movements in this turn. The conditions for the reroll result from the location and presence of the right components. The number of dice, the number of hits, the damage to the tile and the bonus of the movements are different values.

T16: [throw, jackpot and full decision sequence](T16-HUNT.md). Shadow selects failed dice to be spread or resigns, also after partial success. Natural 1 always misses. Current cost does not raise the bonus yet. Traffic with card returns to the overarching effect and does not transfer the event cost to Hunt.

<a id="b18--kafle-obrażenia-i-ujawnienie"></a>
## B18 — tiles, injuries and disclosure

Source: BASE p. 40–42. The pools of standard, special, rejected and removed tiles are openly separate. The recovery of the empty pool does not automatically restore any special or deleted tile.

When you select the tile, settle the corresponding kart/ability windows. When the rule allows you to bear the loss of Companion, the choice is to sacrifice Guide or draw Companion from the right pool, not free to choose any portrait. Other injuries increase Corruption. Changing Guide can open immediate capability; disclosure can cause you to further select the tile at Stronghold Shadow.

Each such choice is a separate permanent decision point. The random selection preview shows the reverses and possible procedure, the future result of RNG.

T16 executes this order in a separate module `Hunt` Ability of new Guide has a window in front of the others Corruption First, the effects of revealing the tiles, then the effects of revealing events/capacity, at the end of each of the different Stronghold The eye from the extra tile has zero injuries. Fellowship We don't re-disclose from another tile. Corruption 12 immediately complete the batch; T20 provides the character's capability and other specific interventions cards are owned by T21. Tests `test.*` check the mechanism, do not replace texts cards .

The T20 supports the Hobbit Guide Chain, their return after being eliminated outside Mordor and the Gollum disclosure. The group against one tile receives one deployment from the remembered places/Progress before disclosure; it does not give a second normal loss. Gollum may also voluntarily disclose at zero injuries, without descending below zero.

T21-FP-CHAR-01 separates the Mithril replacement from later Axe protection. The replaced tile and used in the ongoing action wait outside the discards to the end of the entire action, also by additional Hunt. Axe expires before further effects after Gimli and Legolas leave. Full sources, examples of pools and windows include [specification of the package](T21-FP-CHAR-01.md).

T21-SH-CHAR-01 : [six cards Shadow](T21-SH-CHAR-01.md) adds four red tiles and forced Hunt Patrol/Bane. Original settlement FP It protects despite the conquest. Events ignore the Eye and the tiles FP , also after replacement; Bane transmits the injuries directly to Corruption , keeping the previous windows Staff/Mithril and ignore the standard Reveal by Gollum. Additional Stronghold has independent ordinary Hunt Stop is only valid for the motion test. Cruel as Death has a printed initiative 3 , cost of two points Leadership They awarded and bonus only for the fight kick.

T21-SH-CHAR-02: [cards 7–12](T21-SH-CHAR-02.md) distinguishes the mandatory random victim of Foul Thing from direct contamination of Candles/Wound and the usual Hunt throw Strike without real motion. Search reveals by decision FP after any Nzgûli movement, Cruel Weather moves only figure. They Are Territorial pays one point before the reroll, Dread and Despair any positive number before Combat Roll, Foul Stench compares Leadership and turns off the reroll itself. Black Breath requires the presence of Nazgûl at play, and then further removes one acceptable figure without issuing hits; priority defenders at a draw with Heroic Death. Common correction with Brave Stand: subtracting dice is followed by a limit of up to five, minimum one.

T21-SH-CHAR-03 : [cards 13–18](T21-SH-CHAR-03.md) introduce Lure, separation by Breaking and permanent reactions of Worm, Crebain and Balroga. Worm responds to the actual victim; Lure and Breaking are not such a victim. Worn/Crebain They expire on declaration in an impregnable sanctuary before Balrog. Balrog retains the full route of declaration and disclosure and settles before additional Stronghold The whole declaration precedes the decision to enter the Mordor track and restore the pool. Lidless replaces 0 .. 3 other dice Words cancels Leadership and the ability of one Companion only for the round; Durin’s Bane checks two legal edges from Moria. Balrog from the table has a secret combat choice and joint disclosure without revealing origin by hand size.

## B19 — Mordor

Source: BASE p. 43. Entrance, preparation of the pool, movement on the track, discovery/hiding and effects of the tiles use separate rules. We do not move without changes of the regional movement and the usual Hunt throw to the Mordor track. The specific order of the tile, injuries and shift must have a script for each type of tile.

When the track ends, the engine determines the required effects and immediate victory. It does not display the win of FP before checking the Corruption border.

T17: [entrance and full track run](T17-MORDOR.md). Mordor is not a region of Gorgoroth. Movement also selects a tile without dice Hunt; Eye counts all dice already in Hunt, but not the current cost of Resolving. Reveal does not stop the shift itself; Stop stops, but is still an attempt to move in turn. No attempt movement/hide gives Corruption at the end of the phase of action, before the military victory. The separation effect removes Companion instead of placing it on the map; normal action separation remains unauthorized.

<a id="b20--koniec-gry"></a>
## B20 — Game over

Source: BASE p. 44 Priority: ( 1 ) Corruption at least 12 — Shadow immediately; ( 2 ) destruction of the Ring at Corruption below 12 — FP immediately; ( 3 ) at least 10 military points Shadow ; ( 4 ) at least 4 Military points FP Military check in phase 6 Not after every win.

The enemy city gives 1 point, Stronghold 2, according to the original membership and current control. The changes of the Fate scenario modify the base state of the belonging, rather than adding Shadow free initial points. The completed batch rejects further command change and remains available for reading.

The T17 saves `VictoryCondition` together with `Winner` through the common `GameEnding`. `RingVictory` checks Corruption before the final step, and `MilitaryVictory` refuses to resolve beyond the 6 phase. The final movement costs go to Hunt, frame and decisions are closed, the story remains. The HTTP/PostgreSQL test reboots both ends with the final kaflu before the damage and after the result; the retra draws no more, the new mutation returns `GameFinished`.

<a id="jak-dopisywać-reguły"></a>
## How to Write Rules

T03: [decision directory](decisions.json) contains 32 windows with owner, visibility and checkpoint. [Specification traces](TRACES.md) passes turn, fight and Hunt with reaction. `catalog.json` contains one normal and border case for each of the 30 areas; these are requirements, not a declaration of performance of future tests.

T03 develops this index to specific cases: entry condition → allowed choices → cost → effects → next window → visibility → test. Each new interpretation has a source and location, confirmation status and indication of implementation and test. We do not mark the entire area ‘verified’ if only its main rule has been verified.

<a id="wdrożenie-cyklu-t10"></a>
## implementation Cycle T10

[Phase Machine, Legality of Commands, Fixed Continuations and Restrictions](T10-TURNS.md) describes the working range. Basic selection, dice, allocation, pass and Rings are connected to the server and Unity tray. The Fellowship movement has a cost integration point after Hunt; its full procedure is not replaced by the simplified command.

<a id="potwierdzone-erraty-i-granica-aktualności"></a>
## Confirmed Erats and News Limit

[SRC-08](../sources/SRC-08-AUDIT.md) maps the full FAQ 1.2 to the tasks. 1.2 version concerns the base and the Kings, not the Kings. pinned instruction `BASE-2.4.1` retains a description of the losses from events known from 2.3; the full comparison closed GAP-FAQ. New conflicts require separate sources and scenarios. We do not replace the solution with the fan implementation text.

The [T21-FP-CHAR-02](T21-FP-CHAR-02.md) package includes full applications of cards 7–12: Horn, Staff, Athelas, Another Way, I Will Go Alone and Bilbo’s Song. Before the Shadow selection, there is a separate Staff window. The movement from the event retains the cost as used, and the card separation uses the usual route with the saved bonus. In the fight, the choice of the dedicated figure and the number of canceled hits is atomic; Andúril retains the identity of the selected misses. The Daring checks the full cost of leadership of all Companion. Fateful does not subtract the hit and requires two hits of the spread to remove Witch-king. The sequence of the action still gives priority to the defender.

Package [T21-FP-CHAR-03](T21-FP-CHAR-03.md) Adds full card 13–18 : Replacement of unused dice Mirror, the selection of three tiles Challenge by FP , two trips of one group with the possibility of ending in siege , There and Back Again and Eagles with withdrawals selected by Shadow Specification explains the difference between Gwaihir's constant level and bonus Swifter/Shadowfax. Sudden Strike uses available Leadership for a separate special attack, Brave Stand counts the living Companion on the execution. settlement does not serve to activate or shift policy over this settlement — concerning card , separate Companion and declarations Fellowship ; the previous interpretation of T15 was corrected with a regression test.

[T21-FP-CHAR-04](T21-FP-CHAR-04.md) domika 24 card Character FP: three Enty, Dead Men, House of the Stewards and Grey Company. Losses of attack with event, mandatory retreat and free play another card have separate sequels. Enty/Dead Men trigger a political response from the attack, decided at the end of the entire action and destroy the commanders along with the army; the usual unit removal event retains separate rules. Dead Men takes over settlement only with actual recruitment; Grey Company atomicly replaces Regular by Elite. Two selected strategy precedes the arm limit and optional selection of Grey. New fights: Ents’ Rage, Huorn-dark, Nameless Wood; final explanation of the Mûmakil initiative was written for the later Shadow package.

### B06/B11 — Strategy FP 1–6

[Full specifications and sources](T21-FP-STR-01.md) describes all applications cards 1–6 Last Battle remains legal with already revealed Fellowship but next disclosure/declaration remove card before adding dice traffic to Hunt ; Aragorn's army condition is checked all over action The protection of the region is effective when retreating and storming, and the movement of the characters themselves and recruitment continue to use the usual rules. cards Army/Character Out of hand.

The book allows you to move many independent groups without separation, and then settles Dwarves in the unrecognizable Erebor/Ered Luin. Spirit and Faramir are just losses of event: they do not launch attack policy or eliminate Nazgûli/Minion after losing an army. Scouts end the fight before the throw, with maintaining overfilling and decision attacking the trespassing. The usual retreat and Scouts do not activate the nation by the current Companion; the later action movement including Companion can activate the nation also after remaining in the same city. Shield-wall does not remove historical reroll.

### B06/B09/B11/B14 — Strategy FP 7–12

[Specification](T21-FP-STR-02.md) describes Fear, Wisdom, Red Arrow, Help, Wises and Through a Day and a Night. March retains a permanent group along with Companion , wins blank settlement along the way and check the excess over the entire route. Woses cannot enter Minas Tirith controlled by SH nor strengthen any side of it siege He can bring the entire garrison out of Rohan: a lone commander dies, Companion remains, control Stronghold He's going to the besieging. Help creates a simple rescue and postpones the end of the event to the end. battle ; garrison units are reduced dice SH Fear activates North, Red Arrow does not activate Rohan. Confusion counts only natural first-throw singles. SH ; are hits FP without the possibility of reroll.

The regression of this package improved ID Shire in Tom Bombay on `the-shire`, in accordance with the board directory. The protection test confirms the legal movement before its establishment and the existence of all protected regions.

T21-FP-STR-03 is supplemented by B06/B07/B11: [Strategy rules 13–18](T21-FP-STR-03.md). card is recruited to the maximum extent possible also outside the war (except for Círdana), on the right side of siege. Two figure Círdana goes to the same coastal army; Riders requires Companion outside Edoras and recruits Commander Rohan. Charge counts current elites and uses losses immediately with the initiative of 2, before fighting.

T21-FP-STR-04 closes all 48 cards FP: [Strategy 19–24](T21-FP-STR-04.md). Brand recruits only regulars; Swords requires separate nations in two permanent regions and jointly approves recruits. Éomer requires free settlement, excludes FP under siege, allows FP besieging enemy. Valour requires elite to play and retain +1 for the first shot after her previous loss; without any bonus for the spread.

T21-SH-CHAR-04 : [cards 19–24](T21-SH-CHAR-04.md) They're closing in on Character. Shadow Dreadful counts figure Not points. Leadership Grond forces three rounds siege without degradation and condition first card FP presence Companion Jerkír chooses after the entire event, after all rounds and Sorcerer; Saruman's death removes this selection. Wormtongue blocks the usual Rohan activation, not the political progress itself. Ringwraiths and Black Captain separate lot/rekrutacj ed optional maneuver requiring Nazgûla/ WK in a selected group. Devilry works for both sides siege , exclusively on Combat Roll.

T21-SH-STR-01: [Strategy Shadow 1–6](T21-SH-STR-01.md) describes Valinor, Uruk-hai, Denethor, Dawn, Threats and Stormcrow and both applications. Voluntary hits are paid before bonus; an additional throw Onslaught remains part of the same sequel also after losing his own army while Relentless does not resurrect the future throw of the fight.


T21-SH-STR-02: [Strategy Shadow 7–12](T21-SH-STR-02.md) and [procedure Corsairs](T21-SH-STR-02-CORSAIRS.md) define the initial goals of Gather/Lengthens, sequential routes, four different armies Moving, landing before battle, recruitment before Rage march and Witch-king return. Mûmakil compares the usual and special hits after the flip; Swarm distinguishes the absence of card from the cancelled effect of card.

T21-SH-STR-03/04: [Strategy 13–18](T21-SH-STR-03.md) and [19–24](T21-SH-STR-04.md) describe the maximum possible recruitment from one common pool, the choice of unit type, region quotas, conditions characters/politics and both sides siege. The deficiency does not give regions priority by data order. In Come to Kill counts elites surviving after ordinary losses; Desperate Battle modifies the usual throw and spread of both sides, not a separate special attack.

[T21 — acceptance cards](T21-ACCEPTANCE.md) confirms the 96 set of both applications and additional real intersections cards. [Report](T21-CARD-COVERAGE.md) binds each of them to programs, initiatives and test methods; the full regression 1648/1648 includes the new cases battle and Hunt. It does not replace acceptance with the full T23 batch.

UI-31 — rebuttal record B06: BASE , p. 22–23 and 29 ; FAQ 1.2 , p. 1 . After complete selection can be made single-handed; as long as the hand exceeds six cards , you can't go to the next action or effect. Example: eight cards → one recoil → seven and the same continuation → another recoil → six and resumption. This is a division of decision writing, without changing the limit or timing of selection. Tests: `HandDiscardTests` , `TurnTests.EventDrawKeepsPaidDieResolvingUntilDiscardAndWildCostWorks` .
