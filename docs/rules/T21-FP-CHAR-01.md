<a id="t21-fp-char-01--wydarzenia-i-walka-kart-16"></a>
# T21-FP-CHAR-01 — events and combat cards 1–6

Sources: six Second Edition obverses in [index](../sources/base-card-index.json), BASE p. 23, 28–30, 40–43. The cards numbers are part of the ID, and the event cost is specific die Character or Event; Will requires an open choice of use. Combat usage does not pay dice and removes card from hand at secret approval. The reverse is public, and the reverses appear after the choice of the defender. The conditions of play are not rechecked as performance conditions unless the effect requires.

<a id="sześć-kart"></a>
## Six cards

| ID `base.fp.character.*` | Event | Fight |
| --- | --- | --- |
| 01 Elven Cloaks | Prepare a special 0 (`special-fp.0.0.1`) tile; at the Mordor track right into the pool. | It Is a Gift, initiative 3. |
| 02 Elven Rope | How 01, separate tile 0 (`special-fp.0.0.2`). | It Is a Gift, initiative 3. |
| 03 Phial of Galadriel | How 01, tile −2. | It Is a Gift, initiative 3. |
| 04 Sméagol Helps Nice Master | How 01, tile −1. | It Is a Gift, initiative 3. |
| 05 Mithril Coat and Sting | card on the table. After the Shadow selection of the tile, the optional discard replaces it with the second. The first one waits outside the pool until the end of the entire action. | Blade of Westernesse, the 6: Hobbit initiative required for play. One hit of the Leadership reroll eliminates the selected participating Minion instead of asking for an army loss. |
| 06 Axe and Bow | card on the table only with Gimli or Legolas in Fellowship. The recoil in the security window reduces the tile damage about 1, the minimum 0. It immediately expires when both have left Fellowship. | Mighty Attack, the 4 initiative: Companion required to play. After throwing a fight, before the reroll, all available Leadership from one participating Companion turns selected missed die into a hit. |

The assignment of identical physical tiles of 0 to 01/02 is a stable implementation identity; both have the same rules. 03/04 confirms the printing of values on the obverses. The event's play does not add a second copy of the already prepared tile. Each of the four tiles retains its own pool cycle after the event has been rejected.

It Is a Gift checks the region of the defense army, also when FP attacks. Fellowship on the Mordor track is not in the region. The +1 modifiers apply to all normal dice combat and reroll in this round; they do not change the natural 1/6 or special attacks.

Blade uses a hit only with a live Servant participating in battle, never Nazgûl or the rear guard. The choice is mandatory if there is a hit and a target; with several Servants it chooses FP. No hit of the metasta or target means no effect. The deducted hit retains the original record of dice; the initiative 5 can count them for Mûmakil first. Killing Hobbit by the earlier effect does not reverse the previously played card. Source: [Kevin Chapman, 37574343](https://boardgamegeek.com/thread/2647982/article/37574343#37574343).

Mighty Attack requires still alive Companion with available Leadership. Canceled Leadership is not at cost. No spread is also disabled by this effect — [Kevin Chapman, 34988142](https://boardgamegeek.com/thread/2439492/article/34988142#34988142). Player points Companion and missed die in one tray and confirms once. These are two groups of candidates, without a list of all combinations. Selection of dice is important for the limitations of its later spread; the server does not select strategically for the player. The cost and conversion of dice are recorded atomicly.

<a id="polowanie-i-trwanie"></a>
## Hunt and Duration

Selection → tile exchange window → determining injuries (including Shelob throw) → one card protection → Guide → loss → Corruption → disclosure and additional tiles. Mithril does not use the protection window, so after the exchange you can use Axe. Resolution: [34738173](https://boardgamegeek.com/thread/2421974/article/34738173#34738173). Shelob exchange is selected before you know the throw: [37682047](https://boardgamegeek.com/thread/2657189/article/37682047#37682047).

Cuffs used in the current action and the tiles waiting for the return are separate from the old discards. The empty pool is played in the process action only if the next selection is required, without the tiles still settled. Finally, Mithril tiles return, the others become discards; the empty pool then complement the authorized standard tiles. Source: [38652169](https://boardgamegeek.com/thread/2741857/article/38652169#38652169) , [38653463](https://boardgamegeek.com/thread/2741857/article/38653463#38653463) This way the first tile doesn't return before the extra Hunt After disclosure. The win also closes these states.

Windows are created only on the public basis card on the table and public terms; the secret hand does not affect their presence. The recording includes a stage before fixing the injuries, the page selector and the locations of both tiles. The view shows the unresolved injuries as `?`, without the future result of RNG. Axe can protect the second tile and expires when the composition changes before the next effect.

<a id="weryfikacja"></a>
## Verification

Each card: cost, legality and both functions. Intersections: exchange + protection + Guide, Shelob before throw, additional tile after Reveal, empty pool, expiration of Axe by separation/loss, no reroll/cancelled Leadership, natural 1/6, rear guard and Hobbit elimination. Decisions: serialization and actual PostgreSQL/restart/retry and confidentiality of both sides. Unity: both Mighty groups in one tray, preview, withdrawal, reactions and reconnect. The results of the checks performed will be recorded in TESTING and the task register.
