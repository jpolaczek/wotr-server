<a id="t21-sh-char-04--character-cienia-1924"></a>
# T21-SH-CHAR-04 — Character Shadow 19–24

<a id="źródła-i-interpretacja"></a>
## Sources and interpretation

Six confirmed Avers Second Edition with [SRC-01](../sources/SRC-01-AUDIT.md) , both applications in `base.shadow.character.19..24` . BASE p. 19 , 22–23 , 26–35 ; rules WK and Saruman from SRC-02 Almanach s. 28 – 30/42 read directly to Kevin Chapman's decisions: [Jerkír only after the whole action](https://boardgamegeek.com/thread/1384018/article/19480575#19480575) , [no selection after playing Jerkír himself](https://boardgamegeek.com/thread/1384018/article/35129694#35129694) , [removing Jerkíra uses the use of Ring in turn](https://boardgamegeek.com/thread/1585632/article/22851460#22851460) , [policy at the end of action, activation with offset](https://boardgamegeek.com/thread/2674208/article/37881658#37881658) , [Retreat does not activate, move at zero can](https://boardgamegeek.com/thread/1560835/article/22536963#22536963) , [The figure left behind by the moving army does not participate in the movement](https://boardgamegeek.com/thread/1560835/article/43470671#43470671) , [won settlement does not activate by Fellowship either](https://boardgamegeek.com/thread/2445036/article/35068922#35068922) , [Devilry in both roles siege, not out](https://boardgamegeek.com/thread/2467589/article/35365796#35365796) Answers API saved in `.artifacts/source-audit/sh-char04-thread-*.json` .

Common revision: so far, the policy of the attack was decided at the start of the battle Confirmed settlement with 2021 r. Moves the political response to the end of the whole action ; confirmation of 2022 r. in the thread 1560835 clearly replaces the older finding. The record stores the cause, the nation and region of reaction, regardless of subsequent losses. This is necessary, among others, when Saruman dies in battle : Wormtongue expires immediately and the delayed reaction activates Rohan at the end. We do not activate backwards from the former standing army or from WK The currentity of the entire set of errat still in SRC-08/GAP-FAQ.

<a id="wydarzenia"></a>
## Events

Icon control during this task corrects the earlier SRC-02 : Strider ( 5/II ) has a symbol **North**, other than a collective symbol FP on Gandalf ( 4/II ), Aragorn ( 9/II It doesn't activate Rohan. `nationIcon` in the data, maintaining a common rule for matching icons; Wormtongue test includes Strider, Aragorn, Gandalf, Merry and Boromir. Source: compared obverses SRC-02 and BASE p. 9 , 34 Reading an earlier recording fixes only incorrect `NationIcon=free-peoples` Strider version; preserves identifier figure Aragorn, location, decision and revision. It does not reproduce historical movements or undo previously activated nations.

Normal cost Character/Event Shadow, legality before cost. RNG only on the server, any decision established at cost and continuation.

| Card | Condition and enforcement |
| --- | --- |
| 19 Dreadful Spells | The Shadow Army with Nazgûle is adjacent or is in the same region as the FP Army. Select the source army and the target; the field and garrison are separate. WK counts as one figure Nazgûla, not two points Leadership. Number of dice min(5, figures Nazgùli in the source army); 5+ gives a hit, FP selects the usual event loss. No battle, metasta, Sorcerer and political reaction from the injury itself. The usual commanders do not survive the army's loss, Companion+ survive; the empty besieg Stronghold is captured. The source can be in the garrison, and the target in the adjacent region; the boundaries of the mountains are not yet adjacent. |
| 20 Grond | WK in an army game Shadow besieging Stronghold The choice of participants can leave WK in the legal rear guard. The storm has three mandatory rounds without degradation as long as both armies live; death WK After the third simple extension for the elite. In the first round FP He can play card only with Companion In battle ; consecutive rounds without this limitation. Sorcerer in his usual window. |
| 21 Jerkír | It requires Saruman. On the table to be eliminated or removed by FP. The event results are not selected for the later card event after full completion of this action selection from the selected deck, then the possible limit of the hand. The same play ed Jerkír does not select; the regular action Event selection does not match the second time. Character or free card do not start. Saruman's death at the time of the settlement removes the permission. FP removes as its own action: Will of the West or any available die along with the Ring; the transfer of the ring to Shadow and the use of the limit one on turn in the same transaction. |
| 22 Wormtongue | It requires Saruman; card on the table, without undoing the existing activation. Rohan activates only the right Companion, Fellowship declaration in the unrecoverable Edoras/Helm’s Deep or attack on the army in these regions. Entering the empty settlement, attack elsewhere, the Vision of Elrond and the input WK do not activate. Political shifts continue to be allowed to the penultimate field at the passive nation. card disappears immediately after Rohan activation or Saruman elimination. The play with already active Rohan therefore immediately ends card. |
| 23 Ringwraiths Are Abroad | The movement of any Nazgûla (WK yes, Mouth no), then the optional movement to two armies containing Nazgûla or one attack such an army. Nazgûl must actually belong to the przemieszczanej/attacking group. No figure moves twice in the army section; the earlier flight is a separate part of card. The usual limitations of politics, boundaries and sieges; the excess only after the whole movement. |
| 24 Black Captain Commands | It requires WK. First, the choice of two available Nozgûli in its region (partial reserves allowed) or a normal flight of any Nzgûli. Then optional movement of one army with WK or attack with WK among participants. WK cannot remain in the rear guard of this attack. No second dice, without moving Mouth in part of the flight. |

<a id="walka"></a>
## Fight

- 19 Devilry of Orthanc (3): an Isengard Army unit among the participants of Shadow, a defender
  inside the besieged Stronghold. Works for the attacking and defending Shadow, not in battle
  polowej/wypadzie/odsieczy. +1 exclusively for Combat Roll, not for Leader Re-roll; natural 1 still
  misses.
- 20: existing Dread and Despair (3).
- 21: existing Cruel as Death (3), correct print has priority over wrong almanac header (4).
- 22/24: existing Foul Stench (3).
- 23: existing Words of Power (1).

<a id="architektura-prywatność-i-ui"></a>
## Architecture, privacy and UI

New card records small typical steps. Dreadful uses common event losses, Grond from the ordinary siege machine with the saved limit of mandatory rounds and the limitation of the first card. The maneuvers extend existing `ArmyEvent` by both types of action and a public optional ending; they do not form a combination of all army subsets. The required figure is a separate selection limit, not a guess by title. Jerkír saves the permission at the time of payment and checks the source at the end of the event. Political reactions behave after reboot, not reading historical text events.

On the map, the choice of source and Dreadful goal, with the indication of the garrison; the tray shows dice 5+ and gives the losses of FP. Grond shows the round against three, without unnecessary confirmation of forced continuation. Maneuver: Move/Attack/Skip maneuver in the same tray, group figures and target regions, one commit. Jerkír: two decks to choose, paid removal shows a specific die and ring before approval. The change of plan does not issue a cost. No number boxes and mandatory scrolling are required. Public modifiers and results are common; hands and combat obligations retain the existing privacy.

<a id="weryfikacja"></a>
## Verification

For each half card : Legality, illegality actor/cost/window , partial lack of targets, boundaries and proper effects. Rider/Sorcerer , Saruman's death, cancellation card , garrison, reserves and surplus. Separately three rounds of Grond, a ban on omission and a fourth round for the elite; Jerkír across the entire Grondo and without selection after the death of the source; Wormtongue at the attack and the usual movement to settlement and after withdrawal Companion . PostgreSQL restart/retry for each new window. Unity: both clients by Kestrel/PostgreSQL , preview/undo/reconnect , 1366/1920 with 100/125%. Results to be completed after execution.

Client verification: complete **105/105 PlayMode** by Kestrel/PostgreSQL, final **9/9** after specifying the override button and the width of the tags, **61/61 Editor**. The matrix has 36 layouts with six card, full options and two Army plans; viewed Grond, Dreadfulful, maneuver and Jerkíra discharges. Details of results and improved early runs in [TESTING](../TESTING.md).

The final full regression of .NET/HTTP/PostgreSQL: **1184/1184**, no omissions, 37 s, `.artifacts/T21-SH-CHAR04-domain-final.log`. The validation of the registry, directory and all the obverses has passed. No manual acceptance user nor measurements of FPS were performed.
