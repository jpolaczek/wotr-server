<a id="t21-sh-char-02--karty-postaci-cienia-712"></a>
# T21-SH-CHAR-02 — card Shadow 7–12

<a id="źródła-i-interpretacja-przed-implementacją"></a>
## Sources and interpretation before implementation

The Averses Second Edition in [Source index](../sources/base-card-index.json) and `content/base-cards.json` : Foul Thing from the Deep, Candles of Corps, Nazgûl Search, Cruel Weather, The Nazgûl Strike!, Morgul Wound. Averse number 11 has been visually checked; local name of audit file `base.shadow.character.08-alt.png` is from corrected OCR Instructions BASE p. 24–25 , 29–32 , 37–43 : events, battle , Fellowship and Hunt Almanach s. 23–24 , 37 , 42–43 is used to search for exceptions; priority is given to obverses, instructions and confirmed explanations.

Kevin Chapman explains [Foul Thing, 17846874 Response](https://boardgamegeek.com/thread/1291852/article/17846874): the capabilities of Guide and the table protection precede the mandatory random victim; with zero remaining injuries there is no victim. [Black Breath and Heroic Death, Response 38617289](https://boardgamegeek.com/thread/2739215/article/38617289): with equal initiative, the defender's priority, removed figure cannot be reused. [Response 37574343](https://boardgamegeek.com/thread/2647982/article/37574343): the presence condition to play does not become a re-executive condition unless the same effect requires it.

Ancillary almanac discrepancy: the claim that the Hunt reroll must be used is not a footnote. BASE p. 41 uses "can"/"may"; we keep the documented selection of 0.. the maximum of reroll from T16. SRC-08 remains an audit of the newer official FAQ. We do not overwrite the instructions with an unsupported secondary claim.

<a id="wydarzenia"></a>
## Events

All six cards uses the usual cost of Character/Event from the engine. card and the cost are settled once before its next windows. Rejecting card ends its operation; no target later part does not return the cost.

| Card | Legality and order |
| --- | --- |
| 7 Foul Thing from the Deep | Common condition of stay outside the original settlement FP, also in Mordor. Picking a tile, replacing possibility; Eye and special tile FP are ignored. Other tile: regular Hunt with table protection and the capabilities of subsequent guides. If injured and Companion, the server draws the victim before using the Ring. There is no strategic choice of a particular victim or accepting injuries instead of a draw. The only Companion is also a random victim. After it, the usual choice of new Guide, capabilities, other contamination, disclosure and additional Hunt fortresses. With 0 the injury does not perish Companion. |
| 8 Candles of Corpses | The same condition of the region, also Mordor. Three dice: +1 contamination for each 4+ and with Gollum as Guide only for 6. There is no Hunt: no tile, protection, victim or disclosure; threshold 12 checked immediately. |
| 9 Nazgûl Search | Progress at least 1 on the Fellowship track, outside Mordor. Shadow moves any Nazgûle, including Witch-king, or leaves all at the site. Normal flight restrictions; Mouth and Saruman excluded. After approval, if Nazgûl is at figure Fellowship, there is a mandatory release by FP with a simple route selection, progress reset and additional fortress tiles. You can play only for the figures move. |
| 10 Cruel Weather | The same condition for progress. Shadow selects the region adjacent by the legal edge of the map, also settlement FP. Moving the figure itself: Progress and hiding unchanged, without Hunt, declarations, disclosures and attempts to move in Mordor. It does not start the expiration of Last Battle from ujawnienia/deklaracji. |
| 11 The Nazgûl Strike! | The same condition of progress and movement Nazgûli as 9. If Nazgûl is at Fellowship: according to the "you may either" can be completed, one card Character from the table FP or toss it on Hunt. Strategy and hand are not targets. Rejection also removes the permanent effects of a given card. Hunt has a normal dice, a bonus of previous movements and reroll (including the moved Nazgûle), the Eye is the number of successes. It does not add progress, dice FP to Hunt nor the second cost. Lack of success ends the procedure without a kaflash. |
| 12 Morgul Wound | Fellowship revealed, also Mordor. When contaminated 0–3 adds 2, with 4+ adds 1. There is Hunt; without protection and victims. Immediate contamination control 12. |

<a id="walka"></a>
## Fight

| Battle card | Condition, initiative and effect |
| --- | --- |
| 7 They Are Terrible | At least 1 available Leadership Nazgûli. Initiative 4: mandatory sacrifice of one specific point; +1 to all dice own relay of commanders. No change to Combat Roll and special attacks. |
| 8 Dread and Despair | At least 1 available Leadership Nazgûli. Initiative 3: selection of 1.. all specific points; FP throws a lot less dice Combat Roll, minimum 1. The penalty works after a limitation of up to five dice, also after Captain of the West. You can sacrifice more than you need to minimum. |
| 9 Foul Stench | At least 1 available Leadership Nazgûli. Initiative 3: if the sum of Nazgûli is not less than the total of Leadership FP, the limit of the FP's transmission is 0. It does not remove Leadership; earlier Sudden Strike works normally. |
| 10 Cruel as Death | Joint implementation of the 01 package: 3 initiative, two Nazgûli points for +1 Combat Roll. |
| 11–12 Black Breath | The presence of Nazgûl, including WK, regardless of White Rider and available Leadership. Initiative 6: with at least one hit of its own reroll, you can also remove the live leader of FP or Companion, whose level does not exceed the number of these hits, or refuse. Only participants of battle. Hits continue to cause normal losses. |

The cost of Nazgûla counts only the usual Nazgûle and WK. Points WK selected separately; ban on use of Mouth, Saruman and the elite of Isengard. The sacrifice is public only after approval, lasts until the end of the round, gives the correct remaining limit of the reroll. White Rider blocks the playing of cards requiring this Leadership. The gifting Defense also cancels the cost, the Advantageous Position does not relieve the cost.

The common amendment `CombatDicePenalty`: Brave Stand text also subtracts dice from the throw, not from the unlimited force. Both card require subtraction after the reduction of the number of dice. The regression will include forces above five, Captain, minimum and existing reductions of the entire battle.

<a id="stan-kontrakt-i-ui"></a>
## State, contract and UI

- Serialized `HuntDamageRule.RandomCompanionFirst` defines forced random sacrifice; all normal
  reductions remain legal. `HuntProcedure.Rolled` distinguishes Hunt throw from card from automatic
  selection of the tile; `Movement` retains the meaning of actual motion. The older recording of the
  movement still works.
- `EffectWait.NazgulTravel` and the added `DecisionKind.CharacterEvent` stop card on the move.
  Existing `MoveShadowCharacters`/`CompanionMoves`, open empty selection and no additional dice. The
  server validates figure, unique group, start and goal. After approval, it returns to the next step
  of the same card.
- Unity uses existing groups and maps, common preview and one confirmation. Names selected by
  portraits/figures, points Leadership by chips; no numbers and scrolling fields. The heading and cost
  description include 1, 2 or variable number of points. Cruel Weather selects the region on the
  map; Strike shows only public card from the table and two other options.
- Each strategic window experiences reboot and reboot; the opponent does not get a private draft.
  Forced draw takes place on the server; preview does not draw.

<a id="weryfikacja"></a>
## Verification

Tests of all conditions, both applications six cards , mistaken actor/cost/target , empty motion, WK/Mouth , sieges and fortresses; order of protection, draw and guide; zero injuries, additional Hunt and Mordor; numbers dice and contamination thresholds; Black Breath of Heroic Death/White Rider and life figures . PostgreSQL : reboot, identical projections of both pages and idmpotent retras of each new type of window. Unity: actual server, selection movement/variable cost/region/Effect , preview, approval and arrangement 1366/1920 at the scales 1/1 , 25 Results below.

<a id="wynik"></a>
## Result

Six events and all half-war have been implemented; the manifesto includes 61 events and 60 full cards Full .NET/HTTP/PostgreSQL: 1012/1012 ; Full Unity PlayMode : 87/87. After finalizing Weather preview: 95/95 Focused tests domains/HTTP/PG , 11/11 PlayMode and 55/55 EditMode All without omissions. 24 system variants, including visual six states 1366/125% , does not require scrolling or numerical fields. Command details, errors discovered and limitations in [TESTING](../TESTING.md) . The responsibility of the steps, the common cost of General, the event trip and serialization and presentation were reviewed. Manual acceptance user remains deferred; new measurement not performed FPS No builds standalone. Next task: T21-SH-CHAR-03 .
