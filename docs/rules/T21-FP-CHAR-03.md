<a id="t21-fp-char-03--postacie-wolnych-ludów-karty-1318"></a>
# T21-FP-CHAR-03 — characters Free Peoples, card 13–18

<a id="źródła-i-interpretacje"></a>
## Sources and interpretations

Both catches of cards 13–18 Second Edition: verified images and text in `docs/sources/base-card-index.json` and `content/base-cards.json`. Basic instruction p. 24 (events), 27–31 (fight), 32 (conquered settlement), 34 (activation), 37 (event movement), 39–40 (disconnection), 43–44 (Mordor). Common effects of Heroic Death and Daring Defense describe [pack 02](T21-FP-CHAR-02.md).

Clarification of designer Kevin Chapman: [There and Back Again requires niezdobytej sludges](https://boardgamegeek.com/thread/1291849/article/17844207#17844207), [the same condition applies to normal activation by Companion](https://boardgamegeek.com/thread/1291849/article/35958552#35958552); [Witch-king doesn't even give dice to Eagles](https://boardgamegeek.com/thread/1585632/article/22851793#22851793); [Nazgûle survivors must leave the existing Stronghold](https://boardgamegeek.com/thread/2645899/article/37550465#37550465); [Challenge returns the tiles and supplements the pool only when you need another selection](https://boardgamegeek.com/thread/2741857/article/38653463#38653463).

<a id="wydarzenia"></a>
## Events

The standard cost of Character/Event or legal replacement is validated and downloaded once. All six cards goes to the rejected pile. No target for part of the effect does not match the unprinted condition of the play. Selection of the group and route is one approved decision; no raise, leave or merge groups on the way.

| ID | Condition and order | Borders/decisions |
| --- | --- | --- |
| 13 Mirror of Galadriel | Replace one unused Character FP score with Will of the West. Then heal 1 Corruption if figure Fellowship is in the unconquered Lórien. | die used to pay for the event is no longer available. In the absence of the second dice card still legal, independent treatment. siege and non-zero progression Fellowship do not disable the treatment. Selection of the specific available dice if there are several. |
| 14 Challenge of the King | Strider/Aragorn with an army of FP in the Gondor or Rohan region. FP select three different tiles. If they all have an Eye: Return three to the pool, eliminate Stridera/Aragorna. Otherwise permanently remove the selected Eye, the remaining immediately return. | Osgiliath is a region without a nation and does not meet the condition. Special eye also counts. No damage, protection/Guide windows and effects of selected tiles. Do not run the Hunt Shadow mechanism. Complement from old rejected standard tiles only before selection when the pool is empty. |
| 15 Gwaihir the Windlord | Separate or move one Companion group, treating her Level as 4. | Separation: progress of Fellowship +4. Movement outside Fellowship: 4. You can end in the besieged Stronghold FP, in its garrison. You cannot exit or cross siege. Do not exceed the limit; the entry to Stronghold Shadow ends the route. |
| 16 We Prove the Swifter | Separate or move one group, normal range +2. | Separation: Progress + Highest Level +2. Movement: normal group range +2, also Shadowfax (Gandalf White himself or with one hobbit: 6). Same ending exception in siege as Gwaihir. |
| 17 There and Back Again | Separate one Companion or group, normal range +1. Then, if Gimli or Legolas is in the unreachable Dale, Erebor or Woodland Realm: activate Dwarves and North, move Dwarves, Elves and North one step at a time. | In the absence of Companion skip the separation, check the policy. Gimli/Legolas may be earlier than Fellowship. Check after separation and possible change of Guide. This card does not allow you to enter siege. It does not activate Elves. |
| 18 The Eagles are Coming! | The FP Army with Companion in the same or adjacent region as the Shadow Army with the usual Nazgûl. Select such an army with Shadow. Throw a min(5, number of Nazgûli) D6: 5+ removes Nazgûl into reserve. | Witch-king does not count for the target, the number of dice, losses or displacement. Shadow selects one other unobtainable Stronghold Sauron for all surviving Nazgûli; besieged Stronghold allowed, figure enter the garrison. It does not move politics and does not remove army units. |

Separation with Fellowship located in the besieged Stronghold places the group in its garrison, without further movement. Separation in Mordor removes the selected group; Gwaihir/Swifter can instead still move the group outside Fellowship. The usual rules for changing Guide and the expiry of cards remain valid.

<a id="walka"></a>
## Fight

| ID / effect | Initiative | Maintenance |
| --- | --- | --- |
| 13, 18 Heroic Death | 6 | Joint package program 02; commander of FP or Companion on play. |
| 14 Sudden Strike | 2 | Commander FP or Companion on play. Special attack before Combat Roll: min(5, currently available leadership), without sacrificing. Normal threshold hit this page, without modifiers of regular Combat Roll. Immediate losses, can end battle. Canceled leadership reduces the pool, bans the spread not. |
| 15, 16 Daring Defiance | 0 | A common program of the 02 package; Companion package on play. |
| 17 Brave Stand | 3 | Companion on play. Combat Roll Shadow loses one dice for each live participating Companion, minimum one die. Counts figure, not leadership. It does not affect special attacks. |

The first is the defender on the same initiative. The printed condition is checked when playing; the size depends on the current state of play. Losing the previous participant does not invalidate the already played card.

<a id="kontynuacje-i-ui"></a>
## Continuations and UI

The choice of the effect step may be different from the owner of the event. The expected decision and the private selection trail belong to the actual selector; the owner and settlement of action remain FP. The selected Eagles attack region is a saved continuation field, so the reboot does not repeat the throw.

Travel event saves bonus or fixed level, permission to finish in siege and the limit of one group. The server only lists the appropriate variants: separation/movement ; UI uses existing group chips and click the route on the map. Aims of Eagles and withdraw Nazgûla pointed on the map; without lists of regions requiring scrolling. Mirror presents the replacement of the Character symbol in Will of the West and allows you to choose die Previews do not draw and reveal the hand of the opponent.

<a id="plan-sprawdzeń"></a>
## Verification plan

Each half of each card : conditions, cost, boundaries and window. Additionally: busy settlement and activation; Mirror missing dice paid die , Progress/obl The manger; Challenge 0–3 Eyes, special eye, supplement, Elimination/ko • no windows Hunt ; both trips, illegal routes, siege target/start Children's/passes, Shadowfax, one group, Mordor; policy There and Back Again before/after separation and without Companion ; Eagles multiplicity of targets, WK , thresholds, limit dice selection Shadow and privacy; Sudden Strike initiative/pr leg/special casualties/End battle ; Brave Stand number figures/minimum/you combined leadership.

Save/restart and retra of each new decision family, the actual Unity client by the server for travel, dice swap and opponent selection, tray control at target resolution. Results will be saved after execution in TESTING, manifests and task register; this document does not mean that the test is passed.
