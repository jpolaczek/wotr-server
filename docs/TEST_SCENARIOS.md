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
| `kings-dismay` | Kings, Lords | Shadow | Dismay | UI-276 (D7, D20): turn 2 after the roll, the Shadow holds Théoden's Dismay token; the click on the Free Peoples' glowing Ruler die spends it, Pass keeps it. |
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

<a id="kings-of-middle-earth--test-13"></a>
## Kings of Middle-earth — TEST-13 (2026-10-27)

Owner's request (2026-10-27): every new Kings card and mechanic can be tried end to end from the Kings tab of the test
menu, on a board where whatever condition makes the effect act is met. Every Kings scenario names Kings first in its
modules (`kings-dismay`, which named Lords first and so stood in the Lords tab, now names Kings first), so the client
shows it in the Kings tab; a scenario that needs Lords as well (Dismay, the Council) names Lords second, and the server
offers it only while both modules are released. The client strips "Kings · " from the group inside the tab.

- **Cards** (`KingsCardScenarios`, groups `Kings · Free Peoples cards` and `Kings · Shadow cards`): a combat scenario
  for each of the 19 cards, `kings-combat-<title>`, titled `<card> · Combat: <printed combat title>`, and an event
  scenario `kings-event-<title>` for each card whose event no decision scenario shows whole yet. The event starts as a
  base card's (TEST-09): turn 1 of a Kings game, the card in hand, a die of its event type and an Event die, and the board
  of `KingsCardEvents`, where every clause acts, an "if" clause included (UI-300). The four national cards' event
  scenarios show the removal of their Corrupted king; their recruit-or-cleanse branch is `kings-card-recruit-or-cleanse`.
- **Battles** (`KingsCardBattles`): the Free Peoples cards share a defence at the Fords of Isen against Isengard and a
  Nazgûl, with five Companions (the Awakened Théoden among them), the Initial Denethor and a Rohan Elite in the army; the
  Shadow cards share a siege of Minas Tirith by ten army units (Sauron and Isengard) and two Nazgûl against Gondor's three
  units and the Initial Denethor, with the Black Serpent added for Your Welcome Is Doubtful. Each waits at the card
  owner's Combat card choice after the engine's own attack command.
- **Mechanics** (`KingsSovereignScenarios`, `KingsCorruptionScenarios`, `KingsRulerScenarios`,
  `KingsChieftainScenarios`, `KingsVariantScenarios`): one scenario per step a player wants to try, with the acting side
  holding what pays for it. Battles are played up to the step through the engine, with dice that miss
  (`KingsBattleSteps`).
- **Proof:** `KingsCardEndToEndScenarioTests` plays each event (new or reused) as a legal `PlayEvent` accepted by the
  engine, requires the board to change once the demo CPU answers the effect's decisions, checks each clause's condition on
  the starting board and plays the conditional clauses to their result (the Corrupted king removed, Dáin awakened in
  Erebor, Brand corrupted without an Eye spent, no Free Peoples Combat card at Minas Tirith under Denethor's Folly, Threats
  and Promises discarded by a corruption); each combat scenario offers its card at the owner's choice in a battle where
  its effect has something to act on, and committing it is accepted. `KingsMechanicScenarioTests` checks that every
  scenario whose id starts with `kings-` stands in the Kings tab with the modules its game uses and an enabled option,
  that each mechanic is offered and, committed, acts (the king awakens, the tile returns, the recruits arrive, the token
  is spent, the substitution is made), and plays the following steps where the scenario promises them.

| Id | Group | Start | Title in game | Condition met · effect seen |
| --- | --- | --- | --- | --- |
| `kings-combat-house-of-the-stewards` | Free Peoples cards | Free Peoples | House of the Stewards · Combat: Brave Stand | Five Companions defend; the Shadow's five dice drop to one. |
| `kings-event-aid-in-time-of-need` | Free Peoples cards | Free Peoples | Aid in Time of Need · Event | Gimli in Dale, Merry in Westemnet, Dáin with a tile: move them, then the king a Companion joined awakens there and his tile returns, with no political requirement. |
| `kings-combat-aid-in-time-of-need` | Free Peoples cards | Free Peoples | Aid in Time of Need · Combat: The King’s Banner | The Awakened Théoden takes part and the Shadow's Nazgûl gives it a Leader re-roll to cancel. |
| `kings-combat-it-is-not-so-dark-here` | Free Peoples cards | Free Peoples | It Is Not So Dark Here · Combat: Like a God of Old | The Awakened Théoden (Leadership 3 in round 1) rolls a special attack of three dice. |
| `kings-combat-wisdom-of-elrond` | Free Peoples cards | Free Peoples | Wisdom of Elrond · Combat: Confusion | The Shadow rolls five dice whose 1s hit its own army. |
| `kings-event-riders-of-theoden` | Free Peoples cards | Free Peoples | Riders of Théoden · Event: remove the Corrupted Théoden | Théoden is Corrupted: the card removes him and draws nothing. |
| `kings-combat-riders-of-theoden` | Free Peoples cards | Free Peoples | Riders of Théoden · Combat: Daylight | Five Shadow dice are cut to three. |
| `kings-event-king-brands-men` | Free Peoples cards | Free Peoples | King Brand’s Men · Event: remove the Corrupted Brand | Brand is Corrupted: the card removes him. |
| `kings-combat-king-brands-men` | Free Peoples cards | Free Peoples | King Brand’s Men · Combat: Shield-Wall | Five Shadow dice, so two hits can come and one is cancelled. |
| `kings-event-dain-ironfoots-guard` | Free Peoples cards | Free Peoples | Dáin Ironfoot’s Guard · Event: remove the Corrupted Dáin Ironfoot | Dáin is Corrupted: the card removes him. |
| `kings-combat-dain-ironfoots-guard` | Free Peoples cards | Free Peoples | Dáin Ironfoot’s Guard · Combat: Valour | A Rohan Elite defends: +1 to the Combat roll. |
| `kings-event-thranduils-archers` | Free Peoples cards | Free Peoples | Thranduil’s Archers · Event: remove the Corrupted Thranduil | Thranduil is Corrupted: the card removes him. |
| `kings-combat-thranduils-archers` | Free Peoples cards | Free Peoples | Thranduil’s Archers · Combat: Valour | A Rohan Elite defends: +1 to the Combat roll. |
| `kings-event-ever-at-your-service` | Free Peoples cards | Free Peoples | Ever at Your Service · Event | Dáin carries two tiles: he recruits a Dwarf in Erebor and one tile returns at random. |
| `kings-combat-ever-at-your-service` | Free Peoples cards | Free Peoples | Ever at Your Service · Combat: There Is Hope for Victory | The Initial Denethor takes part: chosen, he awakens; +1 to the Leader re-roll. |
| `kings-event-men-of-little-villages` | Free Peoples cards | Free Peoples | Men of Little Villages · Event | Four nations with free settlements and figures in the reserve: a Regular or Leader of each. |
| `kings-combat-men-of-little-villages` | Free Peoples cards | Free Peoples | Men of Little Villages · Combat: There Is Hope for Victory | As Ever at Your Service's combat. |
| `kings-event-your-welcome-is-doubtful` | Shadow cards | Shadow | Your Welcome Is Doubtful · Event | Gondor active but not at war, Boromir with Denethor (a tile): Denethor's awakening and every tile return are barred; the Character die discards the card. |
| `kings-combat-your-welcome-is-doubtful` | Shadow cards | Shadow | Your Welcome Is Doubtful · Combat: Servant of the Shadow | The Black Serpent, a Chieftain counted as a Minion, gives up Leadership 2: the defenders' three dice drop to one (his own substitution is offered before the reveal; Pass keeps the card). |
| `kings-event-the-west-has-failed` | Shadow cards | Shadow | The West Has Failed · Event | Brand (a tile) beside a Nazgûl, three Eyes, no Free Peoples die, a pool of counting tiles: three tiles drawn without spending an Eye corrupt Brand. |
| `kings-combat-the-west-has-failed` | Shadow cards | Shadow | The West Has Failed · Combat: The Battle Is Vain | Two Nazgûl's Leadership against the defenders' Leadership 2. |
| `kings-event-return-to-valinor` | Shadow cards | Shadow | Return to Valinor · Event | Théoden and Denethor Corrupted: playable without a captured Elven Stronghold, and the four Elven Strongholds' armies are hit on 5+. |
| `kings-combat-return-to-valinor` | Shadow cards | Shadow | Return to Valinor · Combat: Deadly Strife | Both armies +2 in the siege. |
| `kings-event-denethors-folly` | Shadow cards | Shadow | Denethor’s Folly · Event | Gondor at war, Boromir with Denethor, a Sauron army in Osgiliath: Denethor cannot awaken, any die discards the card, and the Shadow's second Army die attacks Minas Tirith, where the Free Peoples play no Combat card. |
| `kings-combat-denethors-folly` | Shadow cards | Shadow | Denethor’s Folly · Combat: Devilry of Orthanc | Isengard units besiege the stronghold. |
| `kings-event-threats-and-promises` | Shadow cards | Shadow | Threats and Promises · Event | The Elves at war (Thranduil awakenable), Brand with a tile, two Eyes, a counting pool: Muster no longer awakens or advances a passive nation; corrupting Brand discards the card. |
| `kings-combat-threats-and-promises` | Shadow cards | Shadow | Threats and Promises · Combat: Devilry of Orthanc | Isengard units besiege the stronghold. |
| `kings-event-stormcrow` | Shadow cards | Shadow | Stormcrow · Event | Boromir in Minas Tirith, Gondor two steps from war: Gondor steps back, Denethor draws a tile (a 3 corrupts him at once), the Free Peoples lose a Gondor unit. |
| `kings-combat-stormcrow` | Shadow cards | Shadow | Stormcrow · Combat: Great Host | Ten besiegers against three, so they stay twice the defenders whatever the casualties. |
| `kings-event-the-king-is-revealed` | Shadow cards | Shadow | The King Is Revealed · Event | Denethor and Théoden Awakened (no Aragorn), Minas Morgul empty: five Regulars and a Nazgûl, then two more figures. |
| `kings-combat-the-king-is-revealed` | Shadow cards | Shadow | The King Is Revealed · Combat: Relentless Assault | Ten Shadow units to take up to two hits. |
| `kings-event-the-palantir-of-barad-dur` | Shadow cards | Shadow | The Palantír of Barad-dûr · Event | Played with a Muster die it stays on the table; the second Muster die pays as an Event; the Free Peoples may discard it with Will of the West or a die and an Elven Ring. |
| `kings-combat-the-palantir-of-barad-dur` | Shadow cards | Shadow | The Palantír of Barad-dûr · Combat: His Power Waxes | The Initial Denethor stands in the besieged region: both armies +1. |
| `kings-event-work-of-the-enemy` | Shadow cards | Shadow | Work of the Enemy · Event | The Corrupted Théoden at Edoras with Rohan's army: hits on 4+, the other kings' regions 5+. |
| `kings-combat-work-of-the-enemy` | Shadow cards | Shadow | Work of the Enemy · Combat: His Power Waxes | As the Palantír's combat. |
| `kings-fp-awaken-ruler-die` | Sovereigns and corruption | Free Peoples | Awaken a sovereign with the Ruler die | Thranduil's Ruler die on an Event, Rohan at war, Théoden (two tiles) in Westemnet: he awakens with the Ruler die, travels home to Edoras and his tiles return. |
| `kings-politics-cleanse` | Sovereigns and corruption | Free Peoples | Politics return a Corruption tile | Boromir with Denethor (two tiles): advancing Gondor offers a random tile back; Pass keeps them. |
| `kings-corruption-three-tiles` | Sovereigns and corruption | Shadow | Corruption attempt: choose one of three tiles | Four Eyes: one paid, three tiles drawn, one kept face down. |
| `kings-corruption-kings-tile` | Sovereigns and corruption | Shadow | Corruption attempt: the Kings tile | Only Kings tiles in the pool: the first ends the draw, face up, worth zero. |
| `kings-corruption-reaches-resistance` | Sovereigns and corruption | Shadow | Corruption attempt: Brand is corrupted | Brand at 1 of 2, a counting pool: the tile drawn corrupts him (tiles revealed, the highest discarded, his marker at Dale). |
| `kings-brand-call-to-arms` | Sovereign abilities | Free Peoples | Brand's Call to Arms | Ruler die on Muster, the North at war: three recruits in two settlements and his region. |
| `kings-dain-iron-guard` | Sovereign abilities | Free Peoples | Dáin's Iron Guard | Ruler die on Muster, the Dwarves at war: an Elite in Erebor and a Regular in another settlement. |
| `kings-sovereign-upgrades` | Sovereign abilities | Free Peoples | Dwarvenking, Stewardship and Elvenking | Three kings at home, their nations at war, Muster dice: each upgrades a Regular to an Elite. |
| `kings-theoden-forth-eorlingas` | Sovereign abilities | Free Peoples | Théoden's Forth Eorlingas | The Awakened Théoden in the Fords battle: Leadership 3 in round 1, 1 after. |
| `kings-far-sighted-hunt` | Sovereign abilities | Free Peoples | Thranduil's Far-sighted against the Hunt | Ruler die on a Character, three Hunt dice, Legolas outside the Fellowship: the move rolls two Hunt dice; companions go one region further. |
| `kings-corrupted-recruitment` | Corrupted sovereigns | Free Peoples | Corrupted kings forbid recruits | All five Corrupted, every nation at war, Muster dice: the printed Leader and Elite bans by nation and region; Regulars still come. |
| `kings-theoden-white-hand` | Corrupted sovereigns | Shadow | Théoden's Mark of the White Hand | Théoden Corrupted, Isengard holds Helm's Deep: an Isengard Stronghold where Isengard recruits. |
| `kings-siege-lonely-mountain` | Corrupted sovereigns | Shadow | Dáin's Fate of the Lonely Mountain | Dáin Corrupted, Erebor besieged, a round without hits: a Sauron Regular is removed to go on, not the Elite downgraded. |
| `kings-siege-white-tower` | Corrupted sovereigns | Shadow | Denethor's Fate of the White Tower | The same at Minas Tirith. |
| `kings-siege-mirkwood` | Corrupted sovereigns | Shadow | Thranduil's Fate of Mirkwood | The same at the Woodland Realm. |
| `kings-ruler-token-brand` | Ruler dice | Free Peoples | Brand's Ruler token: Call to Arms | Ruler die on an Event: the token turns it into Muster for Call to Arms in one command and is spent. |
| `kings-ruler-token-dain` | Ruler dice | Free Peoples | Dáin's Ruler token: Iron Guard | The same for Iron Guard. |
| `kings-ruler-token-theoden` | Ruler dice | Free Peoples | Théoden's Ruler token: Mustering of the Mark | The same for Mustering of the Mark. |
| `kings-ruler-token-thranduil` | Ruler dice | Free Peoples | Thranduil's Ruler token: Far-sighted | Ruler die on Muster, three Hunt dice: the token makes it a Character for Far-sighted; the Hunt rolls two dice. |
| `kings-ruler-token-or-ring` | Ruler dice | Free Peoples | A Ruler token or an Elven Ring | Denethor's token and the Elven Rings: a Ring used blocks the token this turn, the token used blocks the Rings. |
| `kings-ruler-die-recovery` | Ruler dice | Free Peoples | The Ruler die leaves at the next Recover | The only Awakened king fell: his Ruler die still pays now; at the next Recover it is gone while the Serpent keeps the Shadow's. |
| `kings-dismay-from-token` | Ruler dice | Free Peoples | Dismay from a spent Ruler token | Lords and Kings: Denethor's token spent for the Palantír of Anárion becomes the Shadow's Dismay token. |
| `kings-ugluk-entry` | Dark Chieftains | Shadow | Uglúk beside the revealed Fellowship | Rohan not at war, the Fellowship revealed in Hollin: Uglúk enters in or next to its region. |
| `kings-mirkwood-lord-of-the-bats` | Dark Chieftains | Shadow | The Shadow of Mirkwood's Lord of the Bats | The Shadow is Moving committed: his Leadership given up, +1 re-roll at 4 and the Free Peoples card cancelled at 0. |
| `kings-brand-vs-swarm` | Dark Chieftains | Shadow | Brand against the Swarm: initiative 0 | Brand Awakened among the attackers: after Lord of the Bats, Heir of Bard is offered, but the defending Swarm cancels the Free Peoples card first. |
| `kings-serpent-relentless-assault` | Dark Chieftains | Shadow | The Black Serpent's Red Wrath with Relentless Assault | Red Wrath takes a hit before the reveal and gives +2 with Relentless Assault printed. |
| `kings-ugluk-i-command` | Dark Chieftains | Shadow | Uglúk's I Command | Return to Valinor committed: I Command gives both armies +1. |
| `kings-ugluk-desperate-battle` | Dark Chieftains | Shadow | Uglúk's I Command with Desperate Battle | Desperate Battle printed: only the Shadow gains +1. |
| `kings-ugluk-hunt` | Dark Chieftains | Free Peoples | Uglúk's We March Day and Night | Uglúk next to the Fellowship, a Nazgûl with it, three Hunt dice: each Hunt re-roll adds +1. |
| `kings-council-chieftain-entry` | Dark Chieftains | Shadow | A Council token brings in a Chieftain | Lords with the Council: no Chieftain's condition holds, the Council token and a Muster die bring one in. |
| `kings-variant-visible-corruption` | Variants | Shadow | Visible Corruption Tiles | Both sides see Dáin's tile values; the attempt places its tile face up. |
| `kings-variant-sequential-corruption` | Variants | Shadow | Sequential Corruption Tile Draw | Four Eyes: tiles one at a time; stopping keeps the last. |
| `kings-variant-removing-ruler-tokens` | Variants | Shadow | Removing Ruler Tokens | Lords and Kings: no tokens; the fallen Awakened Théoden gave the Shadow Dismay, spent on the Free Peoples' Ruler die. |

### Kings coverage

Every Kings card, ability and mechanic, the scenario that shows it and the condition met there (EXPANSIONS K01–K03, X01,
X02; rules T28–T31). Existing scenarios are reused where they already show the effect whole.

| Card, ability or mechanic | Scenario | Condition met |
| --- | --- | --- |
| House of the Stewards · event | `kings-card-recruit-or-cleanse` | Boromir in Minas Tirith, Denethor with a tile: recruit at Boromir or cleanse, then two Strategy cards. |
| House of the Stewards · Brave Stand | `kings-combat-house-of-the-stewards` | Five participating Companions. |
| Aid in Time of Need · event and The King's Banner | `kings-event-aid-in-time-of-need`, `kings-combat-aid-in-time-of-need` | Companions a move from Dáin and Théoden; an Awakened king and a Shadow re-roll. |
| It Is Not So Dark Here · event | `kings-card-wisdom` | A nation to advance; Dáin and Denethor with tiles to inspect. |
| It Is Not So Dark Here · Like a God of Old | `kings-combat-it-is-not-so-dark-here` | An Awakened king with Leadership. |
| Wisdom of Elrond · event and Confusion | `kings-card-wisdom`, `kings-combat-wisdom-of-elrond` | Both branches; five Shadow dice. |
| Riders of Théoden, King Brand's Men, Dáin Ironfoot's Guard, Thranduil's Archers · event | `kings-event-riders-of-theoden`, `kings-event-king-brands-men`, `kings-event-dain-ironfoots-guard`, `kings-event-thranduils-archers`; `kings-card-recruit-or-cleanse` | The king Corrupted (removal); otherwise each king with a tile and the recruits in the reserve. |
| Daylight, Shield-Wall, Valour ×2 | `kings-combat-riders-of-theoden`, `kings-combat-king-brands-men`, `kings-combat-dain-ironfoots-guard`, `kings-combat-thranduils-archers` | Five Shadow dice; a Rohan Elite. |
| Ever at Your Service · event and There Is Hope for Victory | `kings-event-ever-at-your-service`, `kings-combat-ever-at-your-service`; `kings-card-hope` | Dáin with tiles; an Initial king in the battle (at the choice, and after the reveal). |
| Men of Little Villages · event and There Is Hope for Victory | `kings-event-men-of-little-villages`, `kings-combat-men-of-little-villages` | Four nations' free settlements; an Initial king in the battle. |
| Your Welcome Is Doubtful · event and Servant of the Shadow | `kings-event-your-welcome-is-doubtful`, `kings-combat-your-welcome-is-doubtful` | An awakenable king of a nation not at war with a tile and a Companion; a Chieftain as Minion. |
| The West Has Failed · event and The Battle Is Vain | `kings-event-the-west-has-failed`, `kings-combat-the-west-has-failed` | A Nazgûl beside Brand, three Eyes, no Free Peoples die; Nazgûl and Free Peoples Leadership. |
| Return to Valinor · event and Deadly Strife | `kings-event-return-to-valinor`, `kings-combat-return-to-valinor` | Two Corrupted kings. |
| Denethor's Folly · event and Devilry of Orthanc | `kings-event-denethors-folly`, `kings-combat-denethors-folly` | Denethor awakenable, Boromir with him, an attack on Minas Tirith; Isengard in a siege. |
| Threats and Promises · event and Devilry of Orthanc | `kings-event-threats-and-promises`, `kings-combat-threats-and-promises` | An awakenable king, a corruption that succeeds; Isengard in a siege. |
| Stormcrow · event and Great Host | `kings-event-stormcrow`, `kings-combat-stormcrow` | A Companion in a nation that can step back with an Initial king; ten against three. |
| The King Is Revealed · event and Relentless Assault | `kings-event-the-king-is-revealed`, `kings-combat-the-king-is-revealed` | Denethor and Théoden Awakened, room in Minas Morgul. |
| The Palantír of Barad-dûr · event and His Power Waxes | `kings-event-the-palantir-of-barad-dur`, `kings-card-palantir`; `kings-combat-the-palantir-of-barad-dur`, `kings-card-power-waxes` | A second Muster die, Will of the West and a Ring; an Initial king in the battle (and a Corrupted one). |
| Work of the Enemy · event and His Power Waxes | `kings-event-work-of-the-enemy`, `kings-combat-work-of-the-enemy` | A Corrupted king with a Free Peoples army. |
| Ruler die · entry and discard | `kings-ruler-discard` | Thranduil Awakened and the Serpent in play: both dice rolled on turn 2, the Shadow discards first. |
| Ruler die · results | `kings-fp-ruler-ability`, `kings-sh-ruler-die` | Muster for Théoden's ability; any Shadow result but the Eye for a Chieftain. |
| Ruler die · loss at Recover | `kings-ruler-die-recovery` | The last Awakened king fell this turn. |
| Ruler tokens · each king | `kings-ruler-token-brand`, `kings-ruler-token-dain`, `kings-ruler-token-theoden`, `kings-ruler-token-thranduil`, `kings-fp-ruler-token` | The Ruler die on a face the ability cannot use; the nation at war; Hunt dice for Far-sighted. |
| Ruler token and Elven Ring | `kings-ruler-token-or-ring` | Both available in one turn. |
| Awakening | `kings-fp-awaken`, `kings-fp-awaken-ruler-die` | Muster with every nation at war (Dáin travels to Erebor, Brand chooses a city); the Ruler die, a journey home, tiles returned. |
| Awakened abilities | `kings-brand-heir`, `kings-brand-call-to-arms`, `kings-sovereign-upgrades`, `kings-dain-iron-guard`, `kings-anarion`, `kings-theoden-forth-eorlingas`, `kings-far-sighted`, `kings-far-sighted-hunt` | Each with the die or battle it needs. |
| Corruption attempt | `kings-sh-corruption`, `kings-corruption-three-tiles`, `kings-corruption-kings-tile`, `kings-corruption-reaches-resistance` | Two to four Eyes and unused Free Peoples dice. |
| Corrupted weaknesses | `kings-corrupted-recruitment`, `kings-brand-shadows`, `kings-theoden-white-hand`, `kings-siege-lonely-mountain`, `kings-siege-white-tower`, `kings-siege-mirkwood` | The king Corrupted and the region or battle his weakness names. |
| Cleansing | `kings-politics-cleanse`, `kings-card-wisdom`, `kings-card-recruit-or-cleanse`, `kings-event-ever-at-your-service`, `kings-fp-awaken-ruler-die` | A king with tiles and the Companion, card or awakening that returns them. |
| Removal of a Corrupted king | `kings-fp-remove-corrupted`, the four national cards' event scenarios | Boromir with the Corrupted Denethor and a Character die; the Corrupted king of the card. |
| Dark Chieftains · entry | `kings-sh-chieftain-entry`, `kings-sh-ruler-die`, `kings-ugluk-entry`, `kings-council-chieftain-entry` | Gondor at war; the Elves at war; a revealed Fellowship; a Council token. |
| Dark Chieftains · substitutions and initiative | `kings-chieftain-substitution`, `kings-serpent-relentless-assault`, `kings-mirkwood-lord-of-the-bats`, `kings-ugluk-i-command`, `kings-ugluk-desperate-battle`, `kings-brand-vs-swarm` | The Chieftain in the battle and a committed card, before the reveal; Brand after it. |
| Uglúk's Hunt bonus | `kings-ugluk-hunt` | Uglúk next to the Fellowship and a re-roll source. |
| Dismay | `kings-dismay-from-token`, `kings-dismay`, `kings-variant-removing-ruler-tokens` | Lords and Kings: a token spent; an Awakened king fallen; a Ruler die to re-roll. |
| Variants | `kings-variant-visible-corruption`, `kings-variant-sequential-corruption`, `kings-variant-removing-ruler-tokens` | Each in the configuration that enables it. |

Not given a scenario, with the reason: a Chieftain never enables Gandalf the White and Muster's corrupted-king bans spare
event recruitment (rules that only forbid; `KingsChieftainEntryTests`, `KingsWeaknessTests`); the Lords + Kings
exceptions of X01 — Sméagol tiles redrawn in a corruption draw, the Captain of Despair's dice paying no attempt, Keeper
and Lesser Eyes, Companions without a nation icon unable to cleanse or remove (`KingsLordsIntegrationTests`,
`KingsCouncilIntegrationTests`); the two cards left out with Fate of Erebor (`KingsCardCatalogTests`); and an Awakened
king falling during play, which no scenario can make certain — `kings-dismay` starts just after Théoden fell.

<a id="dodawanie-scenariusza"></a>
## Adding a scenario

The script is an entry in `src/Wotr.Domain/Testing` (`FreePeoplesDiceScenarios`, `ShadowDiceScenarios` or a new group included in `TestScenarioCatalog`). The builder `ScenarioBoard` sets dice, Hunt, policy track, hand, figure and characters. The new script requires waiting in `BaseDiceScenarioTests` (or the appropriate group test) and a row in this directory.
