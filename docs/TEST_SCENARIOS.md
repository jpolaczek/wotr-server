<a id="scenariusze-testowe"></a>
# Test scenarios

The test scenario mode (R31) launches the ready-to-use, named game situations without clicking multiple rounds. The mechanism describes TEST-01 , scenarios dice base game TEST-02 , client service TEST-03 From 2026-10-14 MVP I'll see you later. base game ( TEST-05 ): scenarios dice expansions ( TEST-04 ) lie on branches `expansion-test-scenarios` And they'll come back after they've merged it.

<a id="jak-działa-scenariusz"></a>
## How the Screenplay Works

- Each dice scenario starts with standard base game setting in turn 1, in the action phase, with
  dice already thrown to the selected results. The change is only what the tested action (policy
  track, card in hand, figure, characters) requires.
- The page in the column “Start” has the first decision. The game continues to run normally, both
  sides on a change.
- Free Peoples have four dice. Shadow in Free Peoples scenarios has five results and Hunt with one
  assigned dice and one Eye; in its scenarios six results and one assigned die.
- The same scenario always builds the same situation (solid grain), also the order of the deck.
  Draws during the game (fight, Hunt) are normal.
- `BaseDiceScenarioTests` tests check for each scenario:
  - action option is enabled;
  - die with the tested result can pay it;
  - example command passes through the server engine.

<a id="katalog"></a>
## Catalog

| Id | Start | Title in Game | What I'm testing |
| --- | --- | --- | --- |
| `setup` | Free Peoples | Standard setup | New batch from standard setting, from the Fellowship phase. |
| `fp-character-move-fellowship` | Free Peoples | Move the Fellowship | Character: movement Fellowship; in Hunt die and Eye, so Shadow hunts. |
| `fp-character-hide-fellowship` | Free Peoples | Hide the Fellowship | Character: Hidden the revealed Fellowship in Rivendell. |
| `fp-character-separate-companions` | Free Peoples | Separate companions | Character: separation Companion from Fellowship. |
| `fp-character-move-companions` | Free Peoples | Move companions | Character: the movement of Legolas (Lorien) and Gimlego (Erebor). |
| `fp-character-lead-army` | Free Peoples | Move or attack with a leader | Character: Army movement or attack with Gondor leader from Minas Tirith on Osgiliath. |
| `fp-character-event` | Free Peoples | Play a Character event | Character: Bilbo's Song, Wizard's Staff, Mirror of Galadriel. |
| `fp-army-move` | Free Peoples | Move armies | Army: move to two armies. |
| `fp-army-attack` | Free Peoples | Attack an army | Army: an attack from Minas Tirith on Sauron's army in Osgiliath (Gondor and Sauron in the war). |
| `fp-army-event` | Free Peoples | Play an Army event | Army: Faramir's Rangers, A Power too Great, The Power of Tom Bombadil. |
| `fp-muster-politics` | Free Peoples | Advance a nation | Muster: politics; passive Gondor step away from war cannot move (BASE p. 33–35). |
| `fp-muster-recruit` | Free Peoples | Recruit reinforcements | Muster: recruiting Gondor at war. |
| `fp-muster-event` | Free Peoples | Play a Muster event | Muster: Wisdom of Elrond, Book of Mazarbul, Guards of the Citadel. |
| `fp-army-muster` | Free Peoples | Army or Muster action | Army/Muster: movement, attack, recruitment or politics. |
| `fp-event-draw` | Free Peoples | Draw an event card | Event: Picking from the Character deck or Strategy. |
| `fp-event-play` | Free Peoples | Play any event card | Event: Play card of any type. |
| `fp-will-any-action` | Free Peoples | Will of the West as any result | Will of the West as Character, Army, Muster or Event. |
| `fp-will-gandalf-white` | Free Peoples | Bring in Gandalf the White | Will of the West: Gandalf the White (Grey outside the team., Saruman game). |
| `fp-will-aragorn` | Free Peoples | Bring in Aragorn | Will of the West: Aragorn (Strider In the Minas Tirith). |
| `fp-elven-ring` | Free Peoples | Use an Elven Ring | Change result dice The Ring of Elves, which goes to Shadow. |
| `fp-pass` | Free Peoples | Pass | pass with fewer unused dice. |
| `sh-character-move-minions` | Shadow | Move Nazgûl and Minions | Character: Nazgûla and Witch-king movement. |
| `sh-character-lead-army` | Shadow | Move or attack with a Nazgûl | Character: Army movement or attack with Nazgûl from Minas Morgul on North Ithilien. |
| `sh-character-event` | Shadow | Play a Character event | Character: The Ringwraiths Are Abroad, Flocks of Crebain, The Lidless Eye. |
| `sh-army-move` | Shadow | Move armies | Army: move to two armies. |
| `sh-army-attack` | Shadow | Attack an army | Army: attack from Minas Morgul on Gondor's army in North Ithilien. |
| `sh-army-event` | Shadow | Play an Army event | Army: Shadows Gather, The Shadow Lengthens. |
| `sh-muster-politics` | Shadow | Advance a nation | Muster: the Shadow nation's policy. |
| `sh-muster-recruit` | Shadow | Recruit reinforcements | Muster: Sauron's recruitment at war. |
| `sh-muster-saruman` | Shadow | Bring in Saruman | Muster: Saruman In the Orthanku (Isengard In the War). |
| `sh-muster-witch-king` | Shadow | Bring in the Witch-king | Muster: Witch-king at Sauron's Army (Sauron and Gondor in the War). |
| `sh-muster-mouth` | Shadow | Bring in the Mouth of Sauron | Muster: Mouth of Sauron (all nations of Free Peoples in war). |
| `sh-muster-event` | Shadow | Play a Muster event | Muster: Orcs Multiplying Again, Many Kings to the Service of Mordor, Monsters Roused. |
| `sh-army-muster` | Shadow | Army or Muster action | Army/Muster: movement, attack, recruitment or politics. |
| `sh-event-draw` | Shadow | Draw an event card | Event: Selecting card. |
| `sh-event-play` | Shadow | Play any event card | Event: Play card of any type. |
| `sh-eye-hunt-allocation` | Shadow | Allocate dice to the Hunt | Allocation of dice to Hunt before the throw; the cast-out Eye comes after the throw. |
| `sh-elven-ring` | Shadow | Use a captured Elven Ring | The Elves' Ring taken over: the result is changed, also to the Eye. |

### Expansions — TEST-04, restored by TEST-10 (2026-10-19)

The expansion scenarios start in turn 1 of a game with the named module. The setup (the Gandalf version, the
Council) and the Fellowship phase go through the engine with fixed choices; the characters in a description entered
the game earlier. Each scenario names its module (`TestScenarioView.Modules`); the server lists and starts it only
while that module is released (`Wotr:Release:Modules`), otherwise it is hidden and its start answers 404
`ScenarioNotFound`. `ExpansionDiceScenarioTests` checks each one like the base dice scenarios, and that its declared
module is the module its game uses.

| Id | Module | Start | Title in game | What it tests |
| --- | --- | --- | --- | --- |
| `lords-special-dice-choice` | Lords | Shadow | Choose a special die | Choosing one of the special dice after the roll; the side with more rolled dice first (LORDS pp. 9–13). |
| `lords-forced-eye` | Lords | Shadow | An Eye forces the choice | Vilya's Eye beside Nenya's Character: after the Shadow keeps a die, the client keeps the Eye for the Free Peoples without a click (UI-267). |
| `lords-keeper-eye-choice` | Lords | Free Peoples | Keeper Eyes go to the Hunt | Eyes on both Keeper dice: only an Eye can be kept, and it joins the Shadow's Hunt. |
| `lords-fp-card-draw` | Lords | Free Peoples | Card Draw with a Keeper die | The Card Draw face of the Vilya die (Elrond): draw a card. |
| `lords-fp-narya-die` | Lords | Free Peoples | Muster with the Narya die | The Narya die (Gandalf the Keeper as Guide): Muster for politics. |
| `lords-fp-nenya-die` | Lords | Free Peoples | Character with the Nenya die | The Nenya die (Galadriel): Character to move the Fellowship. |
| `lords-sh-use-balrog` | Lords | Shadow | Use Balrog | The Use Balrog face: activate the Balrog in Moria. |
| `lords-sh-gothmog-die` | Lords | Shadow | Army with the Gothmog die | The Army face of Gothmog's die: move or attack with an army. |
| `lords-sh-balrog-entry` | Lords | Shadow | Bring in the Balrog | Muster: the Balrog enters (the Elves and the Dwarves at war). |
| `lords-sh-gothmog-muster` | Lords | Shadow | Gothmog's Muster abilities | Muster: recruit or upgrade with Gothmog's ability. |
| `lords-fp-narya` | Lords | Free Peoples | Narya | Narya: Gandalf the Keeper in Bree activates the North and puts it at war. |
| `lords-fp-vilya` | Lords | Free Peoples | Vilya | Vilya: keep the die just used to draw a card. |
| `lords-council-token` | Lords | Shadow | Shadow Council token | The Advance Politics token instead of a die (Council of Rivendell). |
| `lords-card-wind-finish` | Lords | Free Peoples | Wind from the West · Finish | UI-347 (D7): no Shadow table card; Inspect the hand is the console's button, Pass finishes. |
| `lords-card-orderly-withdrawal` | Lords | Free Peoples | Orderly Withdrawal | UI-347 (D10): Wind's Combat side at the Fords of Isen after every die hit; the "−1" on the Shadow's hits cancels one and withdraws, Pass keeps it. |
| `lords-card-balrog` | Lords | Shadow | A Balrog is Come! | UI-347 (D11): the active Balrog in Moria with two Orcs; his figure moves him alone, a figure of the army moves it with him, Pass finishes. |
| `kings-brand-heir` | Kings | Free Peoples | Heir of Bard | UI-348 (D17): Brand Awakened and the North attack from Dale; after the reveal the gilt chip beside the own card cancels the Shadow card, Pass keeps the card. |
| `kings-brand-shadows` | Kings | Shadow | Shadows in the North | UI-348 (D17): Brand Corrupted; the Shadow's chip cancels the Free Peoples card after the reveal, Pass keeps the card. |
| `kings-anarion` | Kings | Free Peoples | Palantír of Anárion | UI-348 (D7): the Ruler die drew a card with Denethor in Minas Tirith; a deck's click draws the extra card, Pass skips it. |
| `kings-chieftain-substitution` | Kings | Shadow | A Chieftain's substitution | UI-277 (D7, D17): the Black Serpent beside the Sauron regulars the North attacks; before the reveal the Shadow's gilt chip substitutes Red Wrath, Pass keeps the card. |
| `kings-dismay` | Lords, Kings | Shadow | Dismay | UI-276 (D7, D20): turn 2 after the roll, the Shadow holds Théoden's Dismay token; the click on the Free Peoples' glowing Ruler die spends it, Pass keeps it. |
| `kings-far-sighted` | Kings | Free Peoples | Far-sighted | UI-276 (D18): Thranduil Awakened and the Ruler die on a Character; with it selected, his glowing picture moves with Far-sighted. |
| `kings-card-wisdom` | Kings | Free Peoples | Wisdom of Elrond | UI-278 (D15): Wisdom of Elrond and It Is Not So Dark Here in the hand, Dáin and Denethor with tiles; a nation token activates and advances its nation, a king shows his tiles, Pass keeps them. |
| `kings-card-recruit-or-cleanse` | Kings | Free Peoples | Recruit or remove corruption | UI-278 (D15): the Stewards, the Riders, Brand's Men, Dáin's Guard and Thranduil's Archers in the hand, every king with a tile; a Reserve figure of the king's nation recruits, the king cleanses. |
| `kings-card-hope` | Kings | Free Peoples | There Is Hope for Victory | UI-278 (D16): Denethor and Théoden attack Osgiliath; after the reveal both glow in the battle window's army and the one clicked awakens. |
| `kings-card-palantir` | Kings | Shadow | The Palantír of Barad-dûr | UI-278 (owner's decision 17): the card on the table, two Muster dice; the card's click, then a glowing Muster die, spends it as an Event die once this turn. |
| `kings-card-power-waxes` | Kings | Shadow | His Power Waxes | UI-278 (D16): the Shadow attacks Edoras; Denethor glows in the defending army, the Corrupted Théoden on his portrait. |
| `lords-evil-knowledge` | Lords | Shadow | Evil Knowledge | UI-347 (D12): the Lords Mouth of Sauron besieges Minas Tirith after a round without hits; his portrait extends the siege. |
| `kings-fp-ruler-ability` | Kings | Free Peoples | Ruler die: Mustering of the Mark | The Ruler die (Muster): recruit with Théoden's ability. |
| `kings-fp-ruler-token` | Kings | Free Peoples | Ruler token | Denethor's token turns the Ruler die into his other ability (draw or play a card). |
| `kings-ruler-discard` | Kings | Shadow | Discard a Ruler die | The discard after the roll: the Shadow first, then the Free Peoples (KINGS pp. 6–7). |
| `kings-sh-chieftain-entry` | Kings | Shadow | Bring in a Dark Chieftain | Muster: the Black Serpent enters with a Southron army (Gondor at war). |
| `kings-sh-ruler-die` | Kings | Shadow | Shadow Ruler die | The Shadow's Ruler die (any result but the Eye) pays for another chieftain's entry. |
| `fate-gimli-renown` | Fate | Free Peoples | Gimli's Renown | Event: Gimli in the Iron Hills moves the Dwarves on the political track. |
| `fate-gimli-messenger` | Fate, Lords | Free Peoples | Messenger of the Dwarves | UI-349: Gimli, Dwarven Lord (Council of Rivendell), in Ered Luin; any die activates the passive Dwarves and moves them one step. |
| `fate-gimli-guide` | Fate, Lords | Free Peoples | The Dwarven Lord as Guide | UI-349: the Hunt hits the Fellowship guided by Gimli, Dwarven Lord, alone near Hollin; his response in the Hunt window leaves before the damage. |
| `fate-gimli-homeland` | Fate, Lords | Free Peoples | Gimli's homeland | UI-349: the Council's setup with Gimli, Dwarven Lord, chosen: he may start in the Iron Hills or Ered Luin. |
| `kings-fp-awaken` | Kings | Free Peoples | Awaken a sovereign | Muster: awaken a sovereign (every nation at war). |
| `kings-fp-remove-corrupted` | Kings | Free Peoples | Remove a corrupted sovereign | Boromir in Minas Tirith removes the corrupted Denethor. |
| `kings-sh-corruption` | Kings | Shadow | Attempt corruption | A corruption attempt instead of an action: two Eyes in the Hunt (KINGS pp. 10–13). |

Not yet here: the Nenya reaction (swapping a drawn Eye in the Hunt) and Dismay; they are not die results. Scenarios
per mechanic are added with the UI tasks of each module (UI-265–UI-278).

### Lords of Middle-earth — TEST-12 (2026-10-27)

The Lords tab of the test menu (every scenario whose first module is `lords`; the client strips `Lords · ` from the
group) holds a scenario for every Lords card and for every new Lords mechanic, besides the dice scenarios above and
the owner's decisions of UI-347. Each one starts after the real setup of a Lords game (`LordsBoard`): the private
Companion versions and Guide, the Council's homelands and the Shadow's token when it plays with the Council, the
Fellowship phase and the roll of turn 1 run through the engine; the standard dice then go back to the pool so
`ScenarioBoard` (`ScenarioBoard.Of`) sets the turn's results. Earlier steps of the imagined turn — a Shadow card played
on the table, an attack, a separation, a move into the Hunt, the start of turn 2 — run through the engine's own
commands. Where an effect follows a roll or a draw, the builder makes that step with fixed dice and the scenario starts
at the decision it opens; elsewhere the board makes the effect act whatever the server rolls, and the few left to the
dice say "almost certain" (five Hunt dice and +2, as UI-300). Every scenario names Lords first in `Modules` (then
`kings` for the Lords + Kings ones), so the server lists it only while those modules are released.

- **Cards** (`LordsCardScenarios`, groups `Lords · Free Peoples cards` and `Lords · Shadow cards`): ids
  `lords-card-<title>-event` and `lords-card-<title>-combat`, titles `<card> · Event` and `<card> · Combat: <combat
  title>`, as TEST-09. The event scenario has the card in hand, a die of its event type and an Event die on turn 1 and a
  board where every clause acts (`LordsCardEvents`). The combat scenario waits on the owner's Combat card choice in one
  battle per side (`LordsBattles`), whose board meets every condition of that side's cards and gives each effect its
  target: Saruman and the Witch-king lead Isengard from Orthanc against the Rohan army at the Fords of Isen, a Rohan
  region where the Fellowship stands with Strider, Gandalf the Grey, Boromir and Merry; the Witch-king, two Nazgûl, a
  Southron Elite and Sauron and Isengard units attack the Elves and Legolas besieged in Lórien, two regions from Moria;
  A Balrog is Come! has the active Balrog lead a Sauron army from Westemnet against Edoras, far from Moria. Follow-ups
  (`LordsCardFollowUps`) start where a card's lasting or random part decides.
- **Mechanics** (`LordsCharacterScenarios`, `LordsSmeagolScenarios`, `LordsCouncilScenarios`, `LordsKingsScenarios`;
  groups `Lords · Characters and Rings`, `Lords · Minions`, `Lords · Special dice`, `Lords · Sméagol`,
  `Lords · Council of Rivendell`, `Lords · With Kings`): a scenario per step a player wants to try, or one that reaches
  the next step by play.
- **Proof:** `LordsCardEndToEndScenarioTests`, `LordsMechanicScenarioTests` and `LordsCouncilScenarioTests` build each
  scenario (the mechanics twice, to the same state; `TestScenarioSessionTests` repeats every one and starts it over
  HTTP), check its Lords tab and modules, offer its effect as a legal option of the first decision, commit it through
  the engine and check the result: the clause acted (tiles added, Corruption healed, units
  recruited, a table card discarded, a die changed, a Ring recovered, a Minion eliminated, a decision opened); for the
  lasting cards the test plays on to the trigger (We Won't Go Back's Sméagol tile, the Gates at the reveal into Moria,
  the Captain's next Character die joining the Hunt Box).

| Id | Start | Title in game | What it tests (condition met → effect) |
| --- | --- | --- | --- |
| `lords-card-you-know-the-way-there-event` | Free Peoples | You Know the Way There · Event | Sméagol's two other tiles set aside since the setup → both join the Hunt Pool. |
| `lords-card-you-know-the-way-there-combat` | Free Peoples | You Know the Way There · Combat: Blade of Westernesse | Merry (a hobbit) defends, Saruman and the Witch-king attack → a Leader re-roll hit can eliminate a Minion. |
| `lords-card-safe-paths-in-the-dark-event` | Free Peoples | Safe Paths in the Dark · Event | Sméagol guides, Corruption 2, We Won't Go Back on the Shadow table → heal 1 and discard the card. |
| `lords-card-safe-paths-in-the-dark-combat` | Free Peoples | Safe Paths in the Dark · Combat: It Is a Gift | The defending army stands in the Fellowship's region → +1 to the Combat roll and the Leader re-roll. |
| `lords-card-wind-from-the-west-event` | Free Peoples | Wind from the West · Event | The Gates and the Lore on the Shadow table → discard one (the hand branch: `lords-card-wind-finish`). |
| `lords-card-wind-from-the-west-combat` | Free Peoples | Wind from the West · Combat: Orderly Withdrawal | A field battle with a Leader and Companions → cancel a hit and withdraw (after the roll: `lords-card-orderly-withdrawal`). |
| `lords-card-three-rings-for-the-elven-kings-event` | Free Peoples | Three Rings for the Elven-Kings · Event | Narya out of play, Nenya and Vilya held by the Shadow → each 5–6 of three dice recovers one. |
| `lords-card-three-rings-for-the-elven-kings-combat` | Free Peoples | Three Rings for the Elven-Kings · Combat: Orderly Withdrawal | As Wind from the West's combat side. |
| `lords-card-every-little-is-a-gain-event` | Free Peoples | Every Little is a Gain · Event | Pelargir, Lossarnach and Lamedon free, Gondor not at war → three Regulars recruited, a Strategy card drawn. |
| `lords-card-every-little-is-a-gain-combat` | Free Peoples | Every Little is a Gain · Combat: Shield-wall | Isengard's five dice → one hit cancelled whenever it scores two or more. |
| `lords-card-rangers-of-the-north-event` | Free Peoples | Rangers of the North · Event | A Sauron army in the Weather Hills beside Bree and the North Downs → three dice against it, then a North unit and Leader recruited beside it. |
| `lords-card-rangers-of-the-north-combat` | Free Peoples | Rangers of the North · Combat: Advantageous Position | The defence in Rohan, a Free Peoples nation → −1 to the Shadow's Combat roll. |
| `lords-card-a-balrog-is-come-event` | Shadow | A Balrog is Come! · Event | The inactive Balrog in Moria → active at Level 2 (the active branch: `lords-card-balrog`). |
| `lords-card-a-balrog-is-come-combat` | Shadow | A Balrog is Come! · Combat: Durin's Bane | The Balrog takes part far from Moria → three dice hitting on 4+ before the roll; Flame of Udûn in the strength. |
| `lords-card-we-wont-go-back-event` | Shadow | We Won't Go Back · Event | Sméagol guides; the Hunt ahead holds his second tile → on its draw, a Character card or the table card gives 2 Corruption. |
| `lords-card-we-wont-go-back-combat` | Shadow | We Won't Go Back · Combat: Fear of Their Masters | The Witch-king (a Minion) and two Nazgûl lead → their Leadership forfeited for +2; natural 6s hurt the attacker. |
| `lords-card-the-lore-of-the-elven-rings-event` | Shadow | The Lore of the Elven Rings · Event | Elrond in Rivendell, so the Vilya die rolls next turn → the card on the table (its use: `lords-card-lore-change-die`). |
| `lords-card-the-lore-of-the-elven-rings-combat` | Shadow | The Lore of the Elven Rings · Combat: Fear of Their Masters | As We Won't Go Back's combat side. |
| `lords-card-the-gates-are-closed-event` | Shadow | The Gates are Closed · Event | The Fellowship in Hollin, Reveal tiles in an almost certain Hunt → a reveal into Moria offers the additional tile. |
| `lords-card-the-gates-are-closed-combat` | Shadow | The Gates are Closed · Combat: Words of Power | Two Nazgûl attack, Legolas defends → his Leadership and abilities cancelled for the round. |
| `lords-card-captain-of-despair-event` | Shadow | Captain of Despair · Event | The Witch-king in play, Strider guiding, a second Character die → after its action the die joins the Hunt Box as an Eye. |
| `lords-card-captain-of-despair-combat` | Shadow | Captain of Despair · Combat: Servant of the Shadow | The Witch-king's full Leadership 2 → the Elves roll two fewer dice (at least one). |
| `lords-card-shadow-of-dol-guldur-event` | Shadow | Shadow of Dol Guldur · Event | Three Nazgûl in Dol Guldur → three Regulars recruited, then the army may attack the Elves in the Narrows of the Forest. |
| `lords-card-shadow-of-dol-guldur-combat` | Shadow | Shadow of Dol Guldur · Combat: Mûmakil | A Southron Elite attacks → +1 to every die, an extra hit when the Shadow scores more. |
| `lords-card-madness-and-horror-event` | Shadow | Madness and Horror · Event | Sauron at war; a Gondor army beside Minas Morgul and a North army besieged in Mount Gundabad → both recruitment branches. |
| `lords-card-madness-and-horror-combat` | Shadow | Madness and Horror · Combat: Swarm of Bats | The Free Peoples' card cancelled, or +1 to the Shadow's re-roll when they play none. |
| `lords-card-three-rings-recover` | Free Peoples | Three Rings for the Elven-Kings · Recover the Rings | Rolled 5, 6, 1 → Narya (out of play) first, then Nenya or Vilya from the Shadow. |
| `lords-card-blade-minion` | Free Peoples | You Know the Way There · Blade of Westernesse on a Minion | Every Combat die missed, the re-roll hit → spend one hit on Saruman or the Witch-king. |
| `lords-card-lore-change-die` | Shadow | The Lore of the Elven Rings · Change a die | Turn 2, the Lore on the table, Vilya rolled an Eye → change one Shadow result before the actions, or keep them. |
| `lords-card-lore-discard` | Free Peoples | The Lore of the Elven Rings · Discard it | Will of the West, or any die and an Elven Ring → the Lore leaves the Shadow table. |
| `lords-card-gates-discard` | Free Peoples | The Gates are Closed · Discard it | Gandalf the Grey guides → Will of the West or a Character die discards the Gates. |
| `lords-card-gates-declaration` | Free Peoples | The Gates are Closed · At a declaration | Turn 2's Fellowship phase two steps from Hollin → declared in Moria, the Shadow's Gates (×) or Pass. |
| `lords-card-we-shall-get-it` | Shadow | We Shall Get It · Replace a Hunt tile | Sméagol left with the group, Gollum guides, the Hunt drew a tile → discard the card for a replacement, or Pass. |
| `lords-fp-keeper-entry` | Free Peoples | Bring in Elrond or Galadriel | Muster dice, the Elves at war, Rivendell and Lórien unconquered → either Keeper enters at home. |
| `lords-fp-nenya-ring` | Free Peoples | Nenya: replace a drawn Eye | Galadriel in play, Nenya held, an almost certain Hunt of Eyes → Nenya removes the Eye for good and draws again. |
| `lords-fp-galadriel-siege-muster` | Free Peoples | Galadriel: recruit inside besieged Lórien | Galadriel and the Elves besieged in Lórien, the Elves at war → a Muster die recruits inside the siege. |
| `lords-fp-elrond-household` | Free Peoples | Household of Elrond: Elven Elites lead in battle | Sauron attacks Rivendell, defended in the field with Elrond → the Elven Elites count as Leaders. |
| `lords-fp-keeper-gandalf-setup` | Free Peoples | Gandalf the Grey, Keeper of Narya: the setup choice | A new Lords game → the private choice of Gandalf's version and the Guide; the Keeper as Guide brings the Narya die. |
| `lords-fp-keeper-white` | Free Peoples | Gandalf, Keeper of Narya, becomes Gandalf the White | The Keeper left the Fellowship, Saruman in play, Will of the West → the White takes the Keeper's figure. |
| `lords-sh-minion-entries` | Shadow | Bring in Gothmog, the Black Númenórean or the Chief | Sauron at war, the Free Peoples hold Angmar (1 VP), the Fellowship in Hollin, no Witch-king → all three can enter. |
| `lords-sh-witch-king-switch` | Shadow | The Witch-king becomes the Chief of the Ringwraiths | The Black Captain in Minas Morgul, the Fellowship in Hollin → a Muster die changes his version once, in place. |
| `lords-sh-spear-of-terror` | Shadow | Spear of Terror: the Chief draws a Character card | The Chief with the Fellowship, the Lidless Eye in hand → its play opens Spear of Terror, or Pass. |
| `lords-sh-he-sees-he-knows` | Free Peoples | He Sees, He Knows: the Chief follows a declaration | Turn 2, the Chief in Minas Morgul → a declaration outside a Free Peoples Stronghold lets him fly there free, or Pass. |
| `lords-sh-great-sorcery` | Shadow | Great Sorcery: a Muster die moves the Black Númenórean's army | His army beside Gondor's (both at war) → once a turn a Muster die moves or attacks as a Character die. |
| `lords-sh-use-balrog-active` | Shadow | Use Balrog: move or attack with the active Balrog | The active Balrog in Moria with Orcs, Elves in Dimrill Dale → his die moves him alone or his army. |
| `lords-sh-fire-and-shadow` | Free Peoples | Fire and Shadow: the Fellowship passes the Balrog | Turn 2, the Balrog in Moria → a declaration into Moria draws an additional tile; an Eye kills the Balrog and the Guide. |
| `lords-fp-balrog-deactivate` | Free Peoples | Deactivate the Balrog with Gandalf | The active Balrog in Dimrill Dale with Gandalf the Grey → a Character die (or Will) deactivates him, outside Moria eliminates him. |
| `lords-dice-removed-at-recovery` | Free Peoples | Remove symbol: a Keeper die leaves at Recovery | Gandalf the White in play, Vilya's Eye (Remove) kept → after the last die, Vilya leaves at Recovery for good. |
| `lords-smeagol-joins` | Free Peoples | Sméagol joins the Fellowship as Guide | An almost certain Hunt of Sméagol's tiles → no damage, the tile leaves, Sméagol guides. |
| `lords-smeagol-casualty` | Free Peoples | Sméagol as a Hunt casualty | Sméagol guides, an almost certain damaging Hunt → he absorbs his level (the highest other Companion's) and We Shall Get It enters. |
| `lords-smeagol-departure` | Free Peoples | Sméagol leaves: We Shall Get It or Gollum | Sméagol guides in Hollin → separating him with the group enters the card; leaving him last brings Gollum without it. |
| `lords-smeagol-declared` | Free Peoples | Sméagol leaves at a declaration in Lórien | Turn 2 a step from Dimrill Dale → a declaration in Lórien discards Sméagol, enters the card, a new Guide is chosen. |
| `lords-council-setup` | Free Peoples | Council of Rivendell: choose alternative Companions | A new Council game → the private versions of the seven Companions and the Guide. |
| `lords-council-homelands` | Free Peoples | Council of Rivendell: start outside the Fellowship | Alternative Boromir, Gimli, Legolas and Merry → none, one (one token) or more (both tokens) start at home. |
| `lords-council-token-choice` | Shadow | Council of Rivendell: the Shadow chooses a token | Boromir alone started outside → the Shadow chooses Move Nazgûl and Minions or Advance Politics. |
| `lords-council-token-travel` | Shadow | Shadow Council token: move Nazgûl and Minions | Two Companions started outside, both tokens held → the Travel token moves a Nazgûl without a die. |
| `lords-council-messengers` | Free Peoples | Messengers of Elrond and the Prince of Mirkwood | Boromir, Gimli, Legolas and Merry started at home → any die activates and advances Gondor, the Dwarves or the North, or advances the Elves. |
| `lords-council-champion` | Free Peoples | Champion of Gondor: the alternative Boromir in battle | Sauron attacks Minas Tirith where Boromir started → +1 Combat strength in Gondor's region. |
| `lords-council-guide-separate` | Free Peoples | Alternative Boromir, Gimli or Legolas as Guide: lessen the Hunt | Only those three walk, Boromir guiding, an almost certain damaging Hunt → each Guide in turn separates to lessen it by one. |
| `lords-council-hobbit-guide` | Free Peoples | Alternative Merry or Pippin as Guide: cancel a Hunt tile | Only the hobbits walk, Merry guiding, an almost certain Hunt of Eye and Reveal tiles → the Guide cancels the standard tile. |
| `lords-council-art-of-disappearing` | Free Peoples | Art of Disappearing: the hobbits escape a lost battle | Merry and Pippin started in the Shire, Sauron scored two hits → the army is lost and each hobbit escapes to a neighbouring region. |
| `lords-council-strider-guide` | Free Peoples | Alternative Strider as Guide: the first move die stays out of the Hunt | The alternative Strider guides, Progress 1 → this turn's first move die goes to the Fellowship box; the Hunt still happens. |
| `lords-council-strider-aragorn` | Free Peoples | Alternative Strider becomes Aragorn | The alternative Strider in Minas Tirith, Will of the West → Aragorn on his figure. |
| `lords-kings-smeagol-corruption` | Shadow | Sméagol's tiles are redrawn in a corruption attempt | Lords + Kings, two Eyes, Sméagol's four tiles and one tile of value 1 → drawn Sméagol tiles are set aside and redrawn. |
| `lords-kings-captain-corruption` | Shadow | Captain of Despair Eyes cannot pay a corruption attempt | The Captain's Eye beside three allocated Eyes → it cannot pay and the draw limit counts only the others. |
| `lords-kings-keeper-eye-corruption` | Shadow | A Keeper Eye pays a corruption attempt | Vilya's Eye beside an allocated Eye → it pays and then lies with the Free Peoples' used dice. |
| `lords-kings-council-chieftain` | Shadow | A Council token pays a Dark Chieftain's entry | Lords + Kings + Council, both tokens, Gondor not at war → a Muster die and a token bring the Black Serpent in (D13). |
| `lords-kings-iconless-cleanse` | Free Peoples | A Companion without a nation icon cannot remove a corrupted sovereign | The iconless Boromir beside the Corrupted Denethor cannot; Gandalf the Grey moved in from Osgiliath can. |

**Coverage of the Lords module** (cards: event and combat; characters, Rings, dice, Sméagol, Council, X01). Existing
scenarios are reused where they already show an effect whole.

| Card, ability or rule | Scenario | Condition met |
| --- | --- | --- |
| You Know the Way There — event / Blade of Westernesse | `lords-card-you-know-the-way-there-event` / `-combat`, `lords-card-blade-minion` | Tiles set aside / a hobbit and two Minions in battle, a re-roll hit |
| Safe Paths in the Dark — event / It Is a Gift | `lords-card-safe-paths-in-the-dark-event` / `-combat` | Corruption and We Won't Go Back on the table / Fellowship's region |
| Wind from the West — event / Orderly Withdrawal | `lords-card-wind-from-the-west-event`, `lords-card-wind-finish` / `-combat`, `lords-card-orderly-withdrawal` | Table cards; none and a Shadow hand / field battle, Leader and Companions, hits |
| Three Rings for the Elven-Kings — event / Orderly Withdrawal | `lords-card-three-rings-for-the-elven-kings-event`, `lords-card-three-rings-recover` / `-combat` | Used Rings out of play and with the Shadow; two successes |
| Every Little is a Gain — event / Shield-wall | `lords-card-every-little-is-a-gain-event` / `-combat` | Free regions and reserves / two or more Shadow hits (dice) |
| Rangers of the North — event / Advantageous Position | `lords-card-rangers-of-the-north-event` / `-combat` | A Shadow army beside the North, free North regions / a Free Peoples nation |
| A Balrog is Come! — event / Durin's Bane | `lords-card-a-balrog-is-come-event`, `lords-card-balrog` / `lords-card-a-balrog-is-come-combat` | Inactive and active Balrog / the Balrog in battle away from Moria |
| We Won't Go Back — event / Fear of Their Masters | `lords-card-we-wont-go-back-event` / `-combat` | Sméagol Guide and his tile in the Hunt / a Minion with Leadership |
| The Lore of the Elven Rings — event, change, discard / Fear of Their Masters | `lords-card-the-lore-of-the-elven-rings-event`, `lords-card-lore-change-die`, `lords-card-lore-discard` / `-combat` | A Keeper Eye after the roll; any die and a Ring |
| The Gates are Closed — event, reveal, declaration, discard / Words of Power | `lords-card-the-gates-are-closed-event`, `lords-card-gates-declaration`, `lords-card-gates-discard` / `-combat` | A route through Moria; Grey Guide / Nazgûl and a Companion in battle |
| Captain of Despair — event / Servant of the Shadow | `lords-card-captain-of-despair-event` / `-combat` | The Witch-king, a Guide other than Gollum, a second Character die / a Minion's full Leadership |
| Shadow of Dol Guldur — event / Mûmakil | `lords-card-shadow-of-dol-guldur-event` / `-combat` | Three Nazgûl in Dol Guldur, a target / a Southron Elite |
| Madness and Horror — event / Swarm of Bats | `lords-card-madness-and-horror-event` / `-combat` | Sauron at war, a target beside and one inside a Sauron Stronghold |
| We Shall Get It — entry / replacement | `lords-smeagol-departure`, `lords-smeagol-casualty`, `lords-smeagol-declared` / `lords-card-we-shall-get-it` | Sméagol leaves / Gollum Guide and a drawn tile |
| Elrond — entry, Vilya, Household | `lords-fp-keeper-entry`, `lords-fp-vilya`, `lords-fp-card-draw`, `lords-fp-elrond-household` | Muster, Elves at war; a used die; an Elite in his region |
| Galadriel — entry, Nenya, Valour of the Elven People | `lords-fp-keeper-entry`, `lords-fp-nenya-ring`, `lords-fp-nenya-die`, `lords-fp-galadriel-siege-muster` | A drawn standard Eye; besieged Lórien |
| Gandalf, Keeper of Narya — setup, Narya, die, White | `lords-fp-keeper-gandalf-setup`, `lords-fp-narya`, `lords-fp-narya-die`, `lords-fp-keeper-white` | Private choice; a Free Peoples settlement; Keeper Guide; a qualifying Minion |
| Sméagol — tile entry, Guide and level, casualty, departures | `lords-smeagol-joins`, `lords-smeagol-casualty`, `lords-smeagol-departure`, `lords-smeagol-declared`, `lords-card-we-wont-go-back-event` | His tiles drawn; his second tile while present |
| Balrog — entry, activation, active die, Fire and Shadow, Flame of Udûn, deactivation | `lords-sh-balrog-entry`, `lords-sh-use-balrog`, `lords-sh-use-balrog-active`, `lords-sh-fire-and-shadow`, `lords-card-a-balrog-is-come-combat`, `lords-fp-balrog-deactivate` | Moria; Use Balrog; Level 2; the Fellowship's route; battle; Gandalf in his region |
| Gothmog — entry, Fling Them into the Fray, die | `lords-sh-minion-entries`, `lords-sh-gothmog-muster`, `lords-sh-gothmog-die` | Sauron at war, no Black Captain; his army |
| Black Númenórean — entry, Evil Knowledge, Great Sorcery | `lords-sh-minion-entries`, `lords-evil-knowledge`, `lords-sh-great-sorcery` | One Free Peoples VP; a siege round without hits; his army |
| Chief of the Ringwraiths — entry, change, Spear of Terror, He Sees, He Knows | `lords-sh-minion-entries`, `lords-sh-witch-king-switch`, `lords-sh-spear-of-terror`, `lords-sh-he-sees-he-knows` | The Fellowship's region; a Character event; a declaration |
| Keeper and Lesser Minion dice — choice, forced Eye, Eye to the Hunt, Card Draw, Remove | `lords-special-dice-choice`, `lords-forced-eye`, `lords-keeper-eye-choice`, `lords-fp-card-draw`, `lords-dice-removed-at-recovery` | Several special dice; Eyes; White in play |
| Council — versions, homelands, tokens | `lords-council-setup`, `lords-council-homelands`, `lords-council-token-choice`, `lords-council-token`, `lords-council-token-travel` | Council on; one or more outside |
| Council cards — Messenger / Prince, Champion, Guides, Art of Disappearing, Strider | `lords-council-messengers`, `lords-council-champion`, `lords-council-guide-separate`, `lords-council-hobbit-guide`, `lords-council-art-of-disappearing`, `lords-council-strider-guide`, `lords-council-strider-aragorn` | Printed regions; Gondor's region; alternative Guides; an annihilated army; Progress 1 |
| X01 — Sméagol in corruption, Captain's Eyes, Keeper Eyes, token for a Chieftain, iconless Companion | `lords-kings-smeagol-corruption`, `lords-kings-captain-corruption`, `lords-kings-keeper-eye-corruption`, `lords-kings-council-chieftain`, `lords-kings-iconless-cleanse` | Lords + Kings (+ Council) |

Not given a scenario of their own (they act inside the scenarios above or have no decision): the Keepers' Combat card
level 3 and their staying at home, the Balrog and Gothmog not counting as Minions for Gandalf the White, the Black
Númenórean's and the Chief's extra die, the Black Captain removing Gothmog's die at Recovery, a Lesser Minion's Remove
die (the same rule as `lords-dice-removed-at-recovery` with any Witch-king), the Council Companions' movement level 2
and the other Champions (Dwarves, Elves, North), and the alternative Strider's +1 Combat strength. Shield-wall,
Mûmakil's extra hit, Swarm of Bats' re-roll bonus, Durin's Bane's and Rangers' and Madness' hits depend on the server's
dice; their scenarios meet every board condition.

### Fate cards — UI-266 (2026-10-19)

The six Fate of Erebor cards whose choices have their own objects on the board (`FateCardScenarios`, group
`Fate · Cards`, module Fate; the client shows the group as "Cards" in the Fate tab, beside "Gimli"). Each one starts
like a base card's event scenario (TEST-09) in a Fate game: the owner acts on turn 1 with the card in hand, a die of its
event type and an Event die, and the board meets the card's condition. `FateCardScenarioTests` plays each card from the
hand and checks that the game then waits on the card's own choice.

| Id | Start | Title in game | What it tests |
| --- | --- | --- | --- |
| `fate-card-there-and-back-again` | Free Peoples | There and Back Again · Event | Separation on the Fellowship portraits; Legolas in the Carrock advances the North, the Dwarves and the Elves. |
| `fate-card-book-of-mazarbul` | Free Peoples | Book of Mazarbul · Event | Moving the companions outside the Fellowship on the map (Gimli in the Iron Hills, Legolas in Ered Luin), then the Dwarven recruit. |
| `fate-card-bardings` | Free Peoples | Bardings · Event | The recruitment region next to Dale or Dale itself, and the North reserve plan. |
| `fate-card-dwarves-of-the-iron-hills` | Free Peoples | Dwarves of the Iron Hills · Event | The recruitment region next to Dale or Dale itself, and the Dwarven reserve plan. |
| `fate-card-orcs-multiplying-again` | Shadow | Orcs Multiplying Again · Event | Mount Gundabad or Erebor on the map; four Sauron Regulars left, so the reserve is split. |
| `fate-card-monsters-roused` | Shadow | Monsters Roused · Event | The West or East plan in the console; one Sauron Regular left, so the reserve plan decides what is placed. |
| `fate-card-bardings-combat` | Free Peoples | Bardings · Combat: Shield Wall | UI-349: the base cards' defence at the Fords of Isen (TEST-09) in a Fate game; choose Bardings as the defender's Combat card. |


<a id="tor-mordoru--test-06"></a>
### Tor Mordor — TEST-06

Fellowship stands hidden in Minas Morgul (Corruption   3 It's like she's finished wandering. command   `EnterMordor` Phase Fellowship, so the pool Hunt is prepared as in the game (BASE  s.  43,  T17 Step and Corruption the final scenario sets the builder upon entry; tiles Hunt They usually do.

| Id | Start | Title in Game | What I'm testing |
| --- | --- | --- | --- |
| `fp-mordor-enter` | Free Peoples | Enter Mordor | Fellowship phase in Minas Morgul: track input (gate on map or panel button), camera with Free Peoples and track pulse with Shadow; then Hunt and a simple throw. |
| `fp-mordor-track` | Free Peoples | Walk the Mordor Track | Step 0, three dice Character and Will of the West: attempts to move with the tile (Stop, Reveal, injury), hiding, Corruption behind turn without attempt. |
| `fp-mordor-crack` | Free Peoples | One step from the Crack of Doom | Step 4, Corruption 6: step without Stop reaches the Doomsday Slit and destroys the Ring if Corruption stays below 12 (B20). |

<a id="koniec-gry--test-07"></a>
### Game over — TEST-07

Four ways to complete the base game, each command (BASE p. 44, LOGIC B19, B20). Situations build real figure and real entrance to Mordor. The certainty of the result gives only the Hunt pool where the tiles remain guaranteeing the end; the rest waits in the rejected. Victory gives the usual rule: The Extinction Slit, Corruption 12 or the victory check at the end of turn. The end of the game screen in the client (UI-169) is proven on these scenarios.

| Id | Start | Title in Game | What I'm testing |
| --- | --- | --- | --- |
| `fp-ring-destroyed` | Free Peoples | The Ring is destroyed | Step 4 track, Corruption 6, in the pool only the 0 tiles: move Fellowship dice Character comes to the rift and destroys the Ring. |
| `sh-ring-corrupted` | Free Peoples | The Ring-bearers are corrupted | Step 1 track, Corruption 11, in the pool only the damage-taking tiles: movement Fellowship and the injuries accepted on the Ring give Corruption 12. |
| `sh-military` | Shadow | Shadow military victory | Shadow holds Minas Tirith, Dol Amroth, Helm's Deep, Erebor and Lorien (10 PZ), has the last die turn; its use ends action and the victory check gives military victory. |
| `fp-military` | Free Peoples | Free Peoples military victory | Free Peoples hold Orthanc and Dol Guldur (4 PZ), have the last die turn; its use ends action and gives military victory. |

The `GameEndScenarioTests` tests check for each scenario with eight seeds of the server winner and reason after command and a view of both sides.

The eye as a result of the throw also works in `fp-character-move-fellowship`: The eye lies in Hunt and strengthens the hunt after the Fellowship movement.

<a id="card-scenarios--test-09"></a>
## Card scenarios — TEST-09

The catalog has two scenarios for every base-game card (96 cards, 192 scenarios), so each card can be tried in both of
its uses without playing up to it. They are built from the card catalog (`CardScenarios`), not listed here one by one.

- **Ids:** `card-<card id>-event` and `card-<card id>-combat`, e.g. `card-base.fp.character.01-event`.
- **Groups and tabs:** `Free Peoples cards · Character`, `Free Peoples cards · Strategy`, `Shadow cards · Character`
  and `Shadow cards · Strategy`, by the card's side and deck. The client shows the text before ` · ` as a tab
  (Free Peoples cards, Shadow cards) beside the other scenarios.
- **Titles:** `<card title> · Event` and `<card title> · Combat: <printed combat title>` (combat titles from
  `content/base-card-text.json`, `CardTextCatalog.CombatTitle`). The description names what is prepared.
- **Event scenario:** turn 1 of the standard setup with the card's owner to act, the card in hand, a die of the card's
  event type and an Event die (the Free Peoples four results, the Shadow six and one die in the Hunt Box). Cards with a
  condition get the board it needs, using the engine's own conditions (`FreePeoplesCardEvents`, `ShadowCardEvents`):
  e.g. Gandalf the White and a Companion in Fangorn for The Ents Awake, Strider with the Minas Tirith army for
  Challenge of the King, a Sauron army besieging Minas Tirith for Denethor's Folly, the Fellowship in Hollin for the
  Hunt cards that need it outside a Free Peoples settlement, a nation at war or a Minion in play.
- **Every clause can act (UI-300, owner's decision 2026-10-22):** a scenario is not complete while its card would do
  nothing, so the board also lets the clauses the engine may skip act. Corruption to heal (Athelas with Strider as
  Guide; There is Another Way and Bilbo's Song with Gollum as Guide after every Companion left; I Will Go Alone;
  Mirror of Galadriel in unconquered Lórien with a second Character result). Companions outside the Fellowship within
  a move of the clause's regions (Book of Mazarbul: Gimli in Dale and Legolas in the Woodland Realm; Fear! Fire!
  Foes!: Strider in the Weather Hills and Merry in Buckland; Gwaihir and Swifter: Strider in Bree; There and Back
  Again: Legolas in the Woodland Realm; Riders of Théoden: Gimli in Helm's Deep; Dead Men: Gimli beside Strider in
  Edoras and a Southron army in Erech). A target (Faramir's Rangers: a Sauron army in North Ithilien). A Hunt Pool of
  the 4 Eyes and 2 other tiles for Challenge of the King, so a draw shows Eyes. A second Character Event (Elven
  Cloaks) for the Ents' free play. The Fellowship in Hollin, outside a Stronghold the Nazgûl may not enter, for
  Nazgûl Search, Cruel Weather and The Nazgûl Strike, which also finds Mithril Coat and Sting on the table. Three
  Nazgûl in Minas Morgul for Dreadful Spells, and two Free Peoples Will of the West results for The Day Without Dawn.
- **A kept card meets its trigger (UI-300, follow-up the same day):** a card kept on the table or put into play acts
  only later, so its scenario brings that moment within reach. Mithril Coat and Sting, Axe and Bow, Horn of Gondor and
  Wizard's Staff: played with the Event die, the next move with the Character die meets a Hunt that almost surely
  hits (`ScenarioBoard.HuntAhead`: five Shadow dice in the Hunt Box, two Free Peoples dice that already moved the
  Fellowship this turn for +2, a pool of damage tiles only) and the Shadow has no die left, so the move follows at
  once. Flocks of Crebain and Worn with Sorrow and Toil (Athelas in the Free Peoples hand) prepare the same Hunt for the
  Free Peoples' move after a Switch side; Balrog of Moria stands the Fellowship in Hollin, next to Moria, with a pool
  of Reveal tiles. The eight special Hunt tile cards start on step 0 of the Mordor Track with one other tile in the
  pool, so the played tile is drawn within two steps (`ScenarioBoard.OnMordorTrack`). The Palantír of Orthanc finds
  Orcs Multiplying Again in hand for the Event die, Wormtongue Wisdom of Elrond in the Free Peoples hand, A Power too
  Great and The Power of Tom Bombadil a Sauron army at war next to the regions they protect; The Last Battle and
  Threats and Promises say what to try after the play.
- **Combat scenario:** a battle at the card owner's Combat card choice, started by the engine's own attack command
  (`CardBattles`). Free Peoples cards: Isengard attacks the Rohan army at the Fords of Isen, a field battle in a Rohan
  region where the Fellowship stands with Strider, Gandalf the Grey, Boromir and Merry (Leader, Elites, Companions and a
  hobbit); the Shadow plays no card, so the defender chooses. Shadow cards: a Sauron, Isengard and Southron army with
  Elites and two Nazgûl attacks the Elves besieged in Lórien (two regions from Moria), where the Fellowship shelters.
  Every combat condition of the side's cards holds in its battle.
- **Proof:** `BaseCardScenarioTests` builds all 192: the event scenario offers the card as a legal `PlayEvent` paid by a
  die of the turn and the play is accepted by the engine; the combat scenario waits on the owner's `CombatCard`
  decision, offers the card in `CommitCombatCard` and committing it is accepted.
  `BaseCardScenarioEffectTests` (UI-300) plays every event scenario with its event die, lets the demo CPU answer the
  effect's decisions (a journey of Companions outside the Fellowship moves one of them a region) and requires a board
  that differs from the card merely discarded, under one of six seeds for cards that roll or draw; it also checks on the
  starting board each clause listed above; for the Hunt cards it moves the Fellowship and requires a Hunt that offers
  the card, and for the special tiles it walks the Mordor Track until the played tile is drawn.
- **Production deployment, 2026-10-22** (owner's order "tak" to deploying UI-300): the server went from
  `48953f68b5fc` to `94c0c87ac09c` (UI-300, the LOBBY-05 deployment record and the observer lease test on top of
  LOBBY-05; the rules engine is unchanged, the expansions were not deployed). The commits reached the host as a git
  bundle; nothing was pushed. Copies before the update: `/srv/wotr/backups/2026-10-22-before-ui300` and `…-ui300-b`.
  No migration; `infra/production.sh build server` and `up -d --wait` recreated only `server`, and the image
  `wotr-server:48953f68b5fc` stays for a rollback. Checks: `/health` ok (protocol 2), `/health/ready` 200; five 502
  responses in the second of the swap (10:44:29 UTC) and none after; the lobby, rooms, matches and test-scenario
  requests since then answer 200. A first attempt fed the remote steps on stdin and the backup consumed them, so only
  the first copy was made; the steps now run from a copied script.
- **Production deployment of the follow-up, 2026-10-22** (the owner reported the Axe and Bow scenario unchanged and
  asked for the new version): the server went from `94c0c87ac09c` to `5fca25377bfa` (the triggered cards above and
  the first deployment record), again as a git bundle. Copy before the update:
  `/srv/wotr/backups/2026-10-22-before-ui300-triggers`; the image `wotr-server:94c0c87ac09c` stays for a rollback.
  Checks: `/health` ok, `/health/ready` 200; no 5xx after the restart (20:07 UTC), the lobby, rooms and matches
  answer 200. The client needed no new build: the scenarios are built by the server.

<a id="dodawanie-scenariusza"></a>
## Adding a scenario

The script is an entry in `src/Wotr.Domain/Testing` (`FreePeoplesDiceScenarios`, `ShadowDiceScenarios` or a new group included in `TestScenarioCatalog`). The builder `ScenarioBoard` sets dice, Hunt, policy track, hand, figure and characters. The new script requires waiting in `BaseDiceScenarioTests` (or the appropriate group test) and a row in this directory.
