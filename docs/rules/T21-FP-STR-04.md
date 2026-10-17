# T21-FP-STR-04 — Strategy FP 19–24

Specification before code; performance and results are in task registry.

<a id="źródła-i-działanie"></a>
## Sources and activities

The Averses Second Edition 19–24 in [index](../sources/base-card-index.json), BASE 2.3 p. 8 (free region), 22–23 (events, partial execution, selection), 26–28 (recruitment), 29–32 (fight). Common recruitment exceptions are described by [previous package](T21-FP-STR-03.md). [Kevin Chapman 20999524](https://boardgamegeek.com/thread/1477010/article/20999524#20999524) confirms the behavior of the Valour bonus after losing elites between play and throw.

| Card | Event, always Muster | Fight |
| --- | --- | --- |
| 19 King Brand’s Men | Two regular Norths in Dale; then choosing one Strategy, without a swap for elites. | Shield-wall, initiative 6. |
| 20 Swords in Eriador | Regular either the North elite in The Shire and the regular or Dwarves elite in Ered Luin. Then the Strategy selection. | Advantageous Position, 3; region defender inside the nation FP, −1 to the first SH throw, natural six still hits. |
| 21 Kindred of Glorfindel | Regular or elite Elves In the Rivendell; then Strategy. | Advantageous Position, 3. |
| 22 Dain Ironfoot’s Guard | Regular or elite Dwarves and commander of Dwarves in Erebor. | Valour, 3; elite FP participates in the play, +1 to the first FP throw. |
| 23 Éomer, Son of Éomund | Regular or elite Rohan and commander Rohan in one free settlement Rohan. | Valour, 3. |
| 24 Thranduil’s Archers | Regular or elite Elves In the Woodland Realm; then Strategy. | Valour, 3. |

Events do not require At War or an active nation. They always carry out the maximum possible range. No units or won settlement does not block the play; selection remains mandatory if the deck is not empty. With Swords missing one nation/target does not remove the other. The army limit works throughout the event; the hand limit after selection, and the response Guide Gandalf Grey after mandatory steps.

Éomer requires free settlement: excludes FP inside the besieged Helm’s Deep, allows FP besieging SH inside this Stronghold. It cannot recruit in an empty settlement controlled by SH, with enemy army in the field or region without settlement. The other card retains the usual recruitment exceptions by the event on both sides of siege. The word free on the card is an open limitation of this card.

Valour checks the elite on the commitment of card; previous losses do not reverse the legally played effect. The bonus does not apply to the relay of commanders or special attacks. Cancellation of card removes the bonus. Advantageous Position checks the original nation of the region defender, not the current control; the modifier does not apply to the spread.

<a id="wykonanie-i-ui"></a>
## Performance and UI

The joint recruitment step is given a limit of the type figure for Brand. Swords uses a common step grouping two independent recruits: the selection of two icons, two known locations on the map, one command approving the entire recruitment. Groups have their own limits and candidates; two North elections do not replace one North and one Dwarves. The lack of reserves shortens only the appropriate group. There is no strategic reason to save the first figure before choosing the second.

Éomer uses the map selection of the region and the existing reserve tray. The cost only takes PlayEvent; the region, groups, selection and excess return to the event frame. All decisions, private options and sketches use existing contracts. Neutral figure remain interchangeable; no numerical fields and mandatory scrolling.

<a id="weryfikacja"></a>
## Verification

V16 of each card; recruitment of any kind, only regular Brand, lack of reserves/target of one or both nations, permanent targets, all cases of free settlement Éomera, both siege, selection/limit/Grey. Valour: condition of participation, loss of elite before effect, threshold and natural ones, no premium for reroll, cancellation. Other fights: condition of region, modifier only first throw, Shield-wall threshold.

PostgreSQL restart/retry for grouped selection, recruitment/region election and limit after selection. Unity EditMode: group and retreat with two figures spirits; PlayMode by two clients: Swords, Éomer, Valour; matrix 1366/1920 × 100/125% for two groups. Full regression of the existing common recruitment step and manifest cards.

acceptance: 844/844 .NET/HTTP/PostgreSQL, 51/51 EditMode, 4/4 focused and 76/76 full PlayMode, without omission. Manifesto includes 49 events and 48 full cards (whole FP). Commands, artifacts, structure overview and boundaries in [TESTING](../TESTING.md). Next task: T21-SH-CHAR-01.
