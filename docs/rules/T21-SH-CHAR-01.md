<a id="t21-sh-char-01--character-cienia-16"></a>
# T21-SH-CHAR-01 — Character Shadow 1–6

<a id="źródła-i-granice"></a>
## Sources and boundaries

Second Edition, printed obverses shadow/character 1–6 ed [Source index](../sources/base-card-index.json) and [audit SRC-01](../sources/SRC-01-AUDIT.md) ; BASE p. 22–23 , 28–30 , 40–43 ; [FAQ 1.2](../sources/materials/base-faq-v1.2.pdf) , p. 2 Almanach is an auxiliary index: his description of Cruel as Death erroneously gives the initiative 4 , while the obverses 5/6 and the table of the same almanacs states **3**. The implementation uses verified obverses, without changing the rule on the basis of secondary text.

[Kevin Chapman, 34738173](https://boardgamegeek.com/thread/2421974/article/34738173#34738173) separates the chip exchange, prevents selection and reduces injuries; [34740197](https://boardgamegeek.com/thread/2421974/article/34740197#34740197) excludes Staff from the exchange of the already selected chip. Conclusion from this order and Bane text: the injury reduction ban does not prohibit the previous replacement of Mithril or prevent the selection of Staff. The new chip is still subject to all the limitations of the event. [17846874](https://boardgamegeek.com/thread/1291852/article/17846874#17846874) confirms the distinctness of protection, capabilities of Guide and losses of Companion in the Hunt procedure. The Bane injuries do not reduce any of these three roads. Ignoring the normal symbol of the release by Golluma does not reduce the injury and remains allowed.

<a id="wydarzenia"></a>
## Events

Common entry: Own card in hand, own decision action, available die Character or Event (including legally skonwertowana/WotW). Cost is paid once, card rejected, event frame waits for all decisions Hunt and returns to the queue action. There is no additional dice with movement or stroke Hunt.

| Number / card | Action | Boundaries |
| --- | --- | --- |
| 1 — Shelob’s Lair | Prepare a special Shadow tile: dice throw, Stop. | Before Mordor outside the pool; on the track immediately in the pool. Without Reveal. |
| 2 — The Ring is Mine! | Tile Eye, Reveal, Stop. | As far as I can go. |
| 3 — On, On They Went | Tile 3, Stop. | As above, without Reveal. |
| 4 — Give it to Uss! | Tile 1, Reveal, Stop. | As far as I can go. |
| 5 — Orc Patrol | Select the tile; the eye (also special) and the tile FP reject without success. The others decide as successful Hunt. | Fellowship except the original settlement FP. Regular interventions, Guide and one loss. |
| 6 — Isildur’s Bane | Same selection and filter; injuries are mandatory to increase Corruption. | The same condition of the region. Without protection of injuries, sacrifice/dissection of Guide, loss of Companion and voluntary disclosure of Gollum. |

settlement means Town, City or Stronghold; Fortification is not settlement. We take the original owner, so the conquered Minas Tirith continues to protect Fellowship, while the captured by FP Moria does not protect. The condition is checked against the Fellowship figurine, not abstract progress. On Mordor track, both events are legal; the special red tiles are working, but Stop does not apply to the event without movement. There is no shift or pass-through of the movement test, even after Staff. When you reveal outside Mordor, you normally choose the route and settle additional fortresses; the additional Hunt has its own usual procedure, no heirs ban Bane.

Mithril occurs before determining injuries, including before Shelob throws. Only the final tile is filtered; after rejection Eye/FP We don't open the injury window or the exposure. Standard/SH The tiles pass the right windows. Isildur’s Bane bypasses the choice of loss and damage reduction reactions, because there is no strategic choice here. Disclosure and possible route remain real decisions. Corruption 12 ends the game immediately.

<a id="walka"></a>
## Fight

| Cards | Effect | Initiative / Legality |
| --- | --- | --- |
| 1–4 | One for the Dark Lord: +1 for the combat and relay kick of the commanders Shadow. | 3; the Army's Protecting Region includes the Fellowship figure. Hidden and progress do not change legality; Mordor's track is not the battle region. |
| 5–6 | Cruel as Death: must sacrifice 2 points Leadership Nazgûli, then +1 only for the fight throw. | 3; at least 2 available points Leadership General. White Rider/cancelled Leadership does not provide cost. |

Leadership Nazgûli includes normal figure and Witch-king (2), without Mouth, Saruman or the elite of Isengard. The player points out two point tags assigned to figures; he can either give both Witch-king or split the cost. This one choice and one approval without entering numbers. The sacrifice of specific sources is important after the commander has been eliminated. The advanced Position cancels the kick bonus but no cost required. The cancellation of the entire card by the Daring Defense does not perform its program. Natural 1/6 retains the general rule. The cost expires with the round.

<a id="stan-prywatność-i-ui"></a>
## State, privacy and UI

The typical tile and injury rules are part of the `HuntProcedure` frame, regardless of ID card frame. The default values are maintained by the existing Hunt. The client receives only public information about the direct Corruption frame, the selected cablu and known effects; no pool order, RNG and other hand. The reactions are owned by FP; the decision to dedicate Leadership to Shadow. The same tray supports card → reaction → injury/routes and card combat → two Leadership tags. The reconnect plays the current step, and the retra does not repeat the selection or cost.

<a id="plan-weryfikacji"></a>
## Verification plan

For all 6 cards : proper window, property and cost wydarzenia/walki. Four grand. before/after Mordor, no duplication and real use. Patrol/Bane: settlement primary/conquered , fortification, Mordor, standard Eye/FP/SH Eye, Shelob, ordinary disclosure tile, both protections, guides, Staff, Mithril, additional Stronghold , winning at 12 Fights in both roles: location Fellowship , modifiers of both throws, natural 1/6 , cost 2 , Witch-king, White Rider, non-Nazgûle, cancellation premium/ca owl card . Each new window: real PostgreSQL/HTTP , restart/retry and projections of both sides. Unity Client: reaction/cost , reconnect, matrix 2 size × 2 scale.

The results will be recorded after execution; this description does not count the tests.

Review UI: context battle retains region/round and gives the choice of two points openly. After approval, the assignment of the cost to figures is public (`BattleForceView.ForfeitedLeadership`) and available in force details, also to the opponent and after the reconnect. It is not enough to show the sum itself, as the subsequent elimination of a particular commander can change the available Leadership. This is a copy of the round dictionary, without reference to the server model; the new entry in the report of the previous round retains this information.

<a id="wynik-wykonania"></a>
## Result of implementation

All 6 events and 6 combat applications are in the catalog and manifest, without change gate news FAQ . Final 917/917 .NET/HTTP/PG , 52/52 EditMode , 80/80 full PlayMode before the last presentation amendment and 15/15 A proper regression after her. [Accurate commands, dates and corrections of tests](../TESTING.md) . UI retains one commit of two points and the public cost assigned to figures returns after reboot. Next package: T21-SH-CHAR-02 ( 7–12 ); others card and expansions They're still not finished.
