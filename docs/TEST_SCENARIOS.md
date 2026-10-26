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

<a id="dodawanie-scenariusza"></a>
## Adding a scenario

The script is an entry in `src/Wotr.Domain/Testing` (`FreePeoplesDiceScenarios`, `ShadowDiceScenarios` or a new group included in `TestScenarioCatalog`). The builder `ScenarioBoard` sets dice, Hunt, policy track, hand, figure and characters. The new script requires waiting in `BaseDiceScenarioTests` (or the appropriate group test) and a row in this directory.
