<a id="t21-fp-char-02--postacie-wolnych-ludów-karty-712"></a>
# T21-FP-CHAR-02 — characters Free Peoples, card 7–12

<a id="źródła-i-kolejność"></a>
## Sources and order

Both catches of six cards: verified images and text in `base-card-index.json` and `content/base-cards.json`. Basic instruction: p. 27–31 (fight), 37 (event movement), 39–40 (separation), 43–44 (Mordor). Official instruction: [FAQ 1.2](../sources/materials/base-faq-v1.2.pdf), p. 2: Heroic Death removes its own commander FP.

Clarification of designer Kevin Chapman: [Daring is mandatory if the opponent played card](https://boardgamegeek.com/thread/781863/article/8785939#8785939); [a tie of the initiative and White Rider](https://boardgamegeek.com/thread/2438936/article/34980940#34980940), [You can't sacrifice Gandalf's leadership again.](https://boardgamegeek.com/thread/2438936/article/45505470#45505470); [Heroic Death and Black Breath, defender first](https://boardgamegeek.com/thread/2739215/article/38617289#38617289); [presence condition checked on play, Blade/Fateful](https://boardgamegeek.com/thread/2647982/article/37574343#37574343); [Foul Stench blocks effects from reroll](https://boardgamegeek.com/thread/2439492/article/34988142#34988142); [Wizard does not block the replacement Mithril tile](https://boardgamegeek.com/thread/2421974/article/34738173#34738173).

<a id="wydarzenia"></a>
## Events

All card costs a legal available die Character/Event or legal replacement; the cost and ownership validates the server. The cost is paid once before the effect. card only gets on the table in cases of 7–8, the remaining ones on the rejected stack. Treatment limited to the current Corruption; the absence of Corruption does not block playing or other effects.

| ID | Condition and effect | Decision/End |
| --- | --- | --- |
| 07 Horn of Gondor | Boromir in Fellowship. After selecting the tile by Shadow rejection card lowers damage to 1, the minimum 0. | One common `hunt.table` protection window, as Axe and Bow. card disappears immediately after Boromir leaves. |
| 08 Wizard’s Staff | Gandalf Grey in Fellowship. Rejection prevents single selection by Shadow. | `hunt.draw` before drawing and revealing the tile, also additional Hunt and events. It does not include replacing Mithril or selecting FP. Ineffective Hunt does not open a window. In Mordor, stopping the selection allows you to normally complete the test of movement. Grey departure removes card. |
| 09 Athelas | Three D6 servers, one treatment per 5+; if Guide is Strider, one treatment per 3+. | Three results were also drawn at zero Corruption. Results and actual public treatment, no client decision. |
| 10 There is Another Way | First treatment 1. If Gollum is Guide, optional cover or movement according to normal restrictions. | A clear choice: without further effect or available ukrycie/ruch. Move launches full Hunt and its continuations; die paid for the event remains used, does not go to Hunt. |
| 11 I Will Go Alone | At least one Companion in Fellowship. Mandatory separation of one Companion or one group; normal range +1. Then treatment with 1. | Selection of the group and route in the existing tray, one approval. The fortresses and siege are still valid. In Mordor, the group leaving Fellowship is removed. Change of Guide and expiry of cards before further effect. |
| 12 Bilbo’s Song | Treatment of 1, with Gollum as Guide 2. | Re-use of existing program, without second registration. |

<a id="walka"></a>
## Fight

The attendance conditions apply to participants on the right side; not all figures in the region. The initiative settles the order, with the first defender tie. The effects do not affect separate special attacks.

| ID / effect | Initiative | Maintenance |
| --- | --- | --- |
| 07 Mighty Attack | 4 | Joint Program with T21-FP-CHAR-01: dedicated leadership of one Companion and selected missed result approved together. |
| 08 Servant of the Secret Fire | 3 | Gandalf Grey or White in battle; +1 only for Combat Roll. The presence is also sufficient after White Rider or cancelling the ability. |
| 09 Andúril | 4 | Strider/Aragorn in battle. The full leadership of this figure is cost. Strider replaces one selected missed score; Aragon to two. The clear choice of dice, also with the ban on the specific dice; the ban of the entire Leader Re-roll disables the effect. For Aragorn, “up it” allows zero: a separate choice of missing behavior with visible cost. Without missing, there is no effect or cost. |
| 10 Heroic Death | 6 | Own commander FP or Companion on the play. Optional sacrifice of one living figure : the commander cancels one upcoming regular hit, Companion The selected number of no more than his Level and other injuries. "Equal to or less" also allows you to keep hits on sacrifice Companion . figure and the number of hits cancelled is one decision, the alternative is to skip. In the absence of upcoming hits, no window. Cancelled Leadership does not change the level or the possibility of sacrifice. It does not use again figure removed by an earlier effect. |
| 11 Daring Defiance | 0 | Companion on play. If Shadow played card, it is mandatory to devote full leadership of all participants of Companion to cancel it. Everyone must be able to pay the whole. White Rider prevents the cost, also with other Companion. Without card Shadow no cost. |
| 12 Fateful Strike | 6 | The own commander or Companion on play. At least one actual Leader Re-roll hit allows you to remove the participating Nazgûl; from two hits the player can choose Minion instead. Witch-king is Minion and requires two. If there is no regular Nazgûl, the option of the Minion has the option to skip. It does not use the hit and does not recheck the presence of its own commander after Black Breath. |

<a id="kontrakty-i-prezentacja"></a>
## Contracts and presentation

Complex choices retain identifier single figur/kostek. selection limits depend on step, not on cards names in the client. Groups receive the selection limit, the dependent options can indicate the required selection figure, the standalone option removes the remaining selections. Heroic Death: figure and the clickable hit symbols; Andúril: direct selection one/two dice. With six or more figure dedication/target of the nations/Companion tab limits the view, leaving the clear selection of each figure. The change of the bookmark does not select or remove the target. Without entering the numbers, the product of the combination, automatic sacrifice or scrolling.

The status of continuation contains a window before selection and a bonus of forced separation range. Save/restart retains stage and private approved choices; the opponent does not receive a draft. Repeated command does not repeat cost, draw or elimination.

<a id="weryfikacja"></a>
## Verification

Plan: Legality and cost of all events, Staff before the draw and Mordor/additional tile/Mithril, Horn and expiration, Athelas progs and buckle, Another Way all branches, Alone range/grupa/Mordor/przewodnik. Fight: each program and initiative, both sides of the Daring draw, cancelled leadership, selection of 0/1/2 conversion, Heroic boundaries/skip/tie/special, Fateful thresholds/choice/Witch-king behavior and hit behavior. Separate tests to fix each new family and client by the actual server; control of tray sizes.

The results of the executed commands will be added after implementation in TESTING and the task history. This description is not a test pass.
