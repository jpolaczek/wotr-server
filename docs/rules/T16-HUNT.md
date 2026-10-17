<a id="t16--polowanie-poza-mordorem"></a>
# T16 — Hunt outside Mordor

Sources: BASE p. 37–42; FAQ 1.2 p. 1–2 (original affiliation and each Stronghold on disclosure). Tile statistics: verified T04 directory. Mordor is T17, specific T20 character capabilities, cards T21 programs; the current task ensures their order and durable windows.

<a id="ruch-i-rzuty"></a>
## Movement and throws

`MoveFellowship` requires hiding, action FP and dice Character/Will of the West. It only increases abstract progress. die is Resolving to the end of the entire Hunt, then it goes to Hunt; traffic with card does not carry its cost to Hunt. We do not choose the route at the usual traffic.

Number of dice throw = dice Shadow in Hunt, at most five. Bonus = previous dice FP used for traffic in this turn; current paid die does not increase the bonus yet. Natural score 1 always misses, except for the sum of at least 6 hits. The public score stores the first and final values, not future samples.

Each of the three presences in the last known region gives one available reroll: Stronghold under control of Shadow, Army units Shadow, Nazgûle. The number within the category does not multiply reroll. Shadow chooses to the limit failed dice or resigns; after success additional hits can still change the Eye injury. We do not draw automatically all the reroll. With zero level or no success there is no tile.

<a id="pule-i-pojedynczy-kafel"></a>
## Pules and single tile

The tiles are separated by pool, set-aside, prepared, drawn, discard and removed. T21-FP-CHAR-01 further clarifies `spent` (used in the ongoing action) and `return-after-action` (listed, waiting for return). With the empty pool, only the earlier rejected standard tiles return; the current ones do not return before the end of the action. Special tiles and permanently removed do not return in the usual supplement; the prepared special one awaits Mordor. The draw of the server selects the available tile index, without sending the pool order. The number of tiles in the pool is public. [Source settlement and exchange cycle](T21-FP-CHAR-01.md).

The number value gives damage, the Eye gives the number of success of the normal throw; the Eye tile with events/Stronghold has zero. The dice icon requires separate draw after the release of the tile. The negative value heals to a minimum of zero. Reveal and Stop are independent features; the Stop action on the track implements T17.

Order: tile → exchange → injury determination → one appropriate event of table protection → ability Guide → possibly one loss Companion → the remaining Corruption → disclosure. T21-FP-CHAR-01 separates Mithril from Axe protection; the decision to replace Shelob precedes her injury throw. A public, empty window without a registered available source from the table does not require clicking. We do not skip the window on the basis of a secret hand. The conditions and effects of a specific card remain her program.

`ResolveHuntDamage` chooses `Ring`, `Guide` or `RandomCompanion`. It does not accept any ID victims. The draw includes Guide and all other Companion, never Trustees/Golluma. The level of the eliminated reduces the damage; the excess Corruption is settled later. The change of composition triggers the selection of the new Guide and its window before the rest of the injury. Any further Guide can use the right capacity in the same Hunt; this does not give you a second normal loss.

<a id="ujawnienie-i-kontynuacje"></a>
## Disclosure and continuation

After the effects are settled, the T15 tile selects the route. Then we perform the effects of the events/ability related to the disclosure, and at the end of the additional tiles of various Shadow fortresses. Each tile settles to the end, with the change of the Guide, before the next. Another Reveal icon does not cause the redisclosure of the already revealed Fellowship and the loop of additional tiles.

The Hunt frame saves origin, return, stage, throw, bonus, reroll, current tile, other injuries and already offered guides. Effects frames, Fellowship and Hunt are in one stack, and the engine resumes the owner of the upper frame to the next decision. The order of registration of dealers cannot decide the order of rules.

Corruption at least 12 ends the batch immediately; you must not continue to select another tile. T17 adds competing victory by destroying the Ring and checks its priority.

<a id="weryfikacja"></a>
## Verification

`HuntTests` and `HuntResolutionTests` perform V10, Limits/bonus/natural 1, three categories of reroll and voluntary resignation, Eye for various reasons, recovery of the pool and negative/special tiles, guide/random/ring, new Guide before Corruption, sequence reaction/disclosure/many fortress and no Reveal loop. `HuntPersistenceTests` restarts the actual host HTTP/PostgreSQL at the flip, reactions from the new table Guide, injuries, Guide and route. Retry retains samples of RNG, cost and original victim. Comparison of JSON after changing the pool order does not reveal future tile or victims.

Results 2026-10-05 : `bash scripts/test.sh --no-restore` **184/184 . NET**, without omission, also after the final review of the structure. `bash scripts/test-client.sh` **15/15 PlayMode**, without omission, 02:44:21 UTC . `scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/T16-edit.xml -logFile .artifacts/T16-edit.log` **22/22**, without omission, 02:47:07 UTC The client has undergone a regression of extended contracts; a useful view Hunt remains to UI-08 No program. `test.*` not added to the production directory.
