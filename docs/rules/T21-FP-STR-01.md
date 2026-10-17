<a id="t21-fp-str-01--strategy-wolnych-ludów-16"></a>
# T21-FP-STR-01 — Strategy Free Peoples 1–6

<a id="źródła-i-granice"></a>
## Sources and boundaries

The Second Edition obverses: [Source index](../sources/base-card-index.json) , slots `base.fp.strategy.01` – `.06` Instructions BASE 2.3 , p. 19 (traffic cost and minimum allocation Hunt ), 22 (conditions, maximum possible execution, loss of events), 24 (move), 27–32 (recruitment, battle , politics). almanacs serves for navigation; the solutions below come from Kevin's statements Chapmana/Krieghunda.

- [Last Battle and conditions cards on the table](https://boardgamegeek.com/thread/2449842): entries
  35136937 (condition for Army presence checked at the end of the entire action), 35147998
  (disclosure removes card immediately), 36787694 (can be played with already revealed Fellowship).
- [Disclosure before paying for traffic](https://boardgamegeek.com/thread/2426363): 34796264; some
  events can be played without having an effect.
- [A Power Too Great](https://boardgamegeek.com/thread/2426947): 34805023, ban on Army movement and
  attacks, not character movement or recruitment.
- [Retreat is a movement for the protection of the
  region](https://boardgamegeek.com/thread/2466138): 35346180. Exit from the protected region is
  allowed; entry to another protected region is prohibited.
- [Retreat does not activate the nation by Companion](https://boardgamegeek.com/thread/1560835):
  22414380 and 22509972. The next normal action from movement can activate the nation also when
  Companion remains in place.
- [Ordinary losses of events and Entowie](https://boardgamegeek.com/thread/1705569): 24749574,
  24749790. Spirit/Faramir leave Nazgûle and Minions after the destruction of the army.

<a id="reguły-wydarzeń"></a>
## Rules of events

| ID | Legality and cost | Implementation, decisions, end |
| --- | --- | --- |
| 01 Last Battle | Army/Event; Aragon (not Strider) with an army of FP outside the borders of the nation of FP, also in a region without a nation. | card on the table. die Fellowship moves all over Hunt for use if the protection is still working. Declaration or actual disclosure removes it immediately, so die of this move is already going to Hunt. Loss of condition Aragorn checked all over action. The same earlier disclosure Fellowship does not block the play. |
| 02 A Power Too Great | Army/Event, without additional condition. | Elfs +1 politics, protection Lórien/Rivendell/Grey Havens against the movement and attack of armies SH, also siege/reverse. Removal: one available die SH any result + from hand after one Army and Character card. card The Buster of Strategy does not meet the cost of Army. |
| 03 Tom Bombadil | Army/Event, without additional condition. | North +1 politics, protection Old Forest/Shire/Buckland. Removal identical to 02; the choice of exactly one card on the table. It does not activate the passive nation. |
| 04 Book of Mazarbul | Muster/Event; any Companion outside Fellowship. | Optional normal move of any number of groups. Without separation, entry to siege or ignoring the stop in Stronghold SH. Shadowfax retains its range. Then Companion in the unattainable Erebor/Ered Luin activates Dwarves and moves directly to At War. It does not remove Threats and Promises. No movement is an open choice. |
| 05 Spirit of Mordor | Army/Event; target is the army of SH with units of at least two nations of SH. | The region's selection; five D6, 5+ asks a hit. The owner chooses the loss. Nazgûl does not count as a second nation. Without political attack, without eliminating characters/Nazgoli for loss of army. |
| 06 Faramir’s Rangers | Army/Event. | Mandatory choice of army SH if there is a Osgiliath/North Ithilien/South Ithilien; three D6, 5+ hits. Then, if the army FP stands in Osgiliath: one unit of Gondor (choice of Regular/Elite) and one Leader of available reserves. The Army FP does not need to contain Gondor. Without the army SH performs possible recruitment without both goals card does not give effect. Loss of ordinary event, without changing politics. |

All costs and targets validate the server. Paid card from hand SH are rejected covered: the opponent learns the cost, not their identity. Protected regions and active card are public. Minimum allocation Hunt depends on dice in Hunt Box at the end of the previous turn , according to p. 19 Not the fact of the movement under Last Battle.

<a id="reguły-walki"></a>
## Rules of combat

| ID | Combat | Condition and effect |
| --- | --- | --- |
| 01 | Daylight, 3 | Up to three dices of the SH combat kick. It does not limit the number of extra hits. |
| 02 | No Quarter, 5 | At least one hit of the fight kick or Leader re-roll FP adds one hit. |
| 03, 04 | Advantageous Position, 3 | The defending army is within the borders of the FP nation, regardless of the control of the region and the attacking side. −1 to Combat roll SH; natural 6 hits. |
| 05 | Scouts, 1 | FP defends itself in battle field (also defense against rescue), not in siege storm or sortie. Chooses a legal neighbouring free region or available own Stronghold. Ends the fight before throw; overcrowding normally settled, the attacker decides to enter. Without legal reversal, the effect is not performed. Swarm of Bats about the initiative 0 can cancel it. |
| 06 | Shield-wall, 6 | With at least two normal hits SH cancels one before removing losses. It does not change the saved Leader re-roll results or the previous Mûmakil test on the 5 initiative. |

<a id="mechanizmy-zapis-i-ui"></a>
## Mechanisms, save and UI

The permanent effects are stored by the modifier type, source and region. One line of effect on the protected region allows you to use the existing projection. Removing protection is atomic command `DiscardProtection`: locally select card on the table, two cards from hand and dice, one preview and one approval. It does not consume dice at the panel opening itself. The public projection does not reveal the cost of cards SH.

`EventLossProcedure` saves the cause of the losses so that ordinary events do not inherit the exception of Ents. Book saves the journey with the separation ban, any number of groups and the option to stay. Scouts is the decision of the effect with the targets on the map and a separate entry button to Stronghold; without a list of regions duplicating the map.

Plan V16: All six events and Combats, incorrect cost/target/window , partial reserves, exact expiry time Last Battle, protection march/obl ed Retreat/sustaining, surviving characters after Spirit/Faramir , Scouts and overflow, natural 6/Leader re-roll, save/restart/retry every new window. Regression .NET/PostgreSQL and actual decisions in Unity, the layout 1366/1920 on a scale 100/125%. The results are added when the startup is done.

<a id="odbiór-2026-10-05"></a>
## acceptance 2026-10-05

Full silnik/HTTP/PostgreSQL: 670/670 of which 65 new cases. New Unity scenarios: 6/6 ; full PlayMode : 63/63 ; EditMode : 46/46. No omissions. 12 systems and visual reading 1366 × 768/125% includes security, Book and Scouts. Accurate commands and artifacts: [TESTING](../TESTING.md) . Updated manifest 33 events/ 30 full cards , source index, contracts and rule directory. Without new standalone or measurement build FPS ; Manual user test is postponed. Next package: T21-FP-STR-02 .

Correction in T21-FP-STR-02: Fear test detected a faulty identifier `shire` in Tom's protection. The correct identifier map is `the-shire`. The protection and presentation has been improved; the regression checks the existence of each region and the legal movement before the application of protection. The historical result above describes the actually executed set that did not detect this typo.
