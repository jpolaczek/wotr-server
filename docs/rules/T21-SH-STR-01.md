# T21-SH-STR-01 — Shadow Strategy 1–6

<a id="źródła-i-kolejność"></a>
## Sources and order

Averses Second Edition 1 – 6/24 ed [index SRC-01](../sources/base-card-index.json) , BASE p. 22–23 , 28–32 , 33–35 and confirmed statements by Kevin Chapman: [35346194](https://boardgamegeek.com/thread/2466144/article/35346194#35346194) — Valinor losses only among elves; [35387928](https://boardgamegeek.com/thread/2465426/article/35387928#35387928) — Denethor covers each battle with a defender in Minas Tirith; [35426600](https://boardgamegeek.com/thread/2472706/article/35426600#35426600) , [35428935](https://boardgamegeek.com/thread/2472706/article/35428935#35428935) and [35429088](https://boardgamegeek.com/thread/2472706/article/35429088#35429088) — the difference between the Relativeless Assault and Onslaught after the elimination of their own army. 2026-10-05 ; local answers API In `.artifacts/source-audit/sh-str01-thread-*.json` Almanach served to find the statements, does not replace the obverse or the author's explanation FAQ .

<a id="wydarzenia"></a>
## Events

| ID / card | Condition, cost and effect |
| --- | --- |
| 01 Return to Valinor | Army/Event; Shadow controls at least one Stronghold elf. Clear each unobsessed Stronghold: as many dice as the elf of the army units, at most five; each 6 is a hit. FP chooses the losses exclusively from the elf units of this army. Commanders die only after the loss of the entire army, the other Companion remains. No political consequences of the attack. We also skip the captured Stronghold elf besie by FP. |
| 02 The Fighting Uruk-hai | Army/Event; Saruman in the game and besieging army with an Isengard unit. Attack of this Stronghold: three mandatory rounds and the first card FP only with Companion in the fight. Isengard units can remain in the legal rear guard. Losing Saruman/Isengard does not interrupt the established procedure; the elimination of the army ends battle. After the third round, the usual cost of the elite for each next one. Jerkír only after the entire battle. |
| 03 Denethor’s Folly | Army/Event; FP is besieged in Minas Tirith by Shadow. card on the table; Shadow player points to the removal of one of his nation's commanders in the region, if any. Companion is not such a commander. cards on the table; FP on the table; siege player points to the departure and rescue when the defender is in Minas Tirith. Exit after this siege is stopped. FP on his own action can release Will; any die only with Gandalf Grey/White or Aragorn in the besie garrison. Strider does not meet the condition. |
| 04 The Day Without Dawn | Army/Event; all nations Shadow At War. Available dice FP with Will score go to released. Other results, dice already released and Hunt unchanged. Recover the next turn restores dice. |
| 05 Threats and Promises | Muster/Event; on a table without additional condition. The passive nation of FP cannot advance action Muster, also by Will used as Muster. Active nation and events can still advance. card disappears after the actual promotion of FP due to an attack or special ability of Companion; the activation, attack of the nation already At War or blocked last step Rohan is not enough. |
| 06 Stormcrow | Muster/Event; Fellowship or Companion is within the boundaries of the FP nation which is not At War. Shadow indicates a legal nation together with card and at cost; the tag goes back one step, at most to the end of the track. Then FP selects the commander, regular or elite unit of this nation from anywhere board. The entire figure is removed without degradation of the elite. No goal is not to reverse nor to play politics. This is not an attack. |

Valinor settlement already stores the regions served; the selection of the next Stronghold precedes its draw, and the choice of losses FP must be completed before the next region. We do not choose as a player figures or assume that availability of elite replacements is equivalent. The event controller indicates another unaccounted for region; it is a clear way to organize independently the mentioned regions, no additional choice of target allowing any to leave out.

<a id="zastosowania-bojowe"></a>
## Combat uses

| Cards | Application and window |
| --- | --- |
| 01 | Deadly Strife (3): both armies +2 Combat and Leader re-roll; natural 1 still box. |
| 02 | Onslaught (7): After simple losses, you can accept from zero to four additional hits on your own army participants. After their use, the dice throw for each actual hit; 4+ hits the opponent, without the reroll and modifiers of the ordinary fight. The shot also follows the loss of all its own units. The bilateral elimination does not win the region. |
| 03, 05 | Devilry of Orthanc (3): an existing joint program, an Isengard unit among participants and an inside defender Stronghold; +1 exclusively Combat. |
| 04 | Relativeless Assault (3): before Combat from zero to two hits on its own participants. +1 to Combat for each hit taken; without the Leader re-roll bonus. The elimination of the entire army ends the fight before the throw. |
| 06 | Great Host (7): the number of living army units after simple losses, without commanders and characters; the elite counts as one figure. At least two times the advantage gives one hit immediately. |

Voluntary losses use the same validation of Remove/Downgrade as normal losses. The player selects figure and approves one preview, also empty selection. We do not enter a numeric field or a separate question about the number of hits. The limit is saved in the loss frame; the actual cost incurred returns to the effect frame, so restart does not repeat the elimination or draw.

<a id="weryfikacja-i-widoczność"></a>
## Verification and visibility

Plan: conditions, cost, region and participants of each card; Valinor in several regions and mixed army; three rounds of Uruk-hai and rear guard; Denethor with the choice of Shadow in all modes, expiration and both costs of removal; retrieve Will; Threats with Wormtongue and capabilities; Stormcrum with elite and without figures. Tests of both sides of voluntary losses, zero, limit, absence of regular replacement, cancellation and both armies destroyed by Onslaught. Real HTTP/PostgreSQL: restart/retry in each new window, arm privacy and RNG. Unity: selection on the map, figure and degradation, no scrolling in four resolution/scale configurations, screenshots written and network scenarios.

Results: 1331/1331 .NET/HTTP/PostgreSQL, 64/64 EditMode. Full PlayMode indirectly 113/115 with two geometry errors; after improving them, the final course of new journeys and all affected 13/13. [Commands, previous errors and verification limits](../TESTING.md#t21-sh-str-01--strategy-cienia-1–6) systems.

Correction during the review: the previous draft gave the choice of the commander Denethor FP by analogy to take hits. The obverses do not ask a hit; orders the player to remove the figure (the sentence starts with “When you play this card”). The performer of the selection remains Shadow. This literal interpretation of the obverse, not an additional Chapman ruling; Stormcrow clearly indicates FP as the elimination performer. Tests and UI must retain this difference.

The voluntary loss preview shows the loss of figures and the unknown follow-up if the throw occurs: Onslaught at a positive cost or Relentless at a living army. The saved flag `BattleLossProcedure.AdditionalAttack` distinguishes these sequels without recognition of the title card in the preview.
