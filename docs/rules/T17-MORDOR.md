<a id="t17--mordor-i-wynik-partii"></a>
# T17 — Mordor and the score of the party

Sources: BASE p. 39 (forbidden of regular separation), 43 (Mordor), 44 (priorities of victory); catalogue 24 tiles T04. The mechanics tile/reaction remains in common with T16, but the beginning and end of the movement on the track are distinct.

<a id="wejście"></a>
## Entry

FP can use `EnterMordor` in its phase Fellowship, when the figurine is in Morannon or Minas Morgul, also after the account has been settled. The entry is optional and does not require hiding. It cannot be started during the ongoing declaration effect. After entry, the position is the step 0, the state Hidden/Revealed remains, and Fellowship does not belong to any region, especially to Gorgoroth. `RegionId` uses the `mordor-track` tag; we do not add it to the board graph. Selection of Guide and the end of the phase remain available.

The pool remains unpaved tiles. The cast-off Eye and the special tiles prepared return. The usual rejected numerical tiles do not return at the entrance; no permanently removed tile does return. The recovery of the completely empty pool is still subject to the T16 rule.

<a id="próba-ruchu-i-ukrycie"></a>
## Movement and hiding test

`MoveFellowship` still costs Character/Will of the West and requires Hidden. There is no throw or spread on the track: one tile is drawn immediately. The eye of such movement has the value of all dice in Hunt, also earlier dice of the FP traffic; the current Resolving is not yet in Hunt. Without dice in Hunt and so we choose the tile. The eye selected from the separate event has the value of the event, defaults zero.

The tile is settled to the end before the move: reactions, victim, Corruption, Reveal. Reveal on the track just changes the cover, does not open the route and does not create additional tiles of fortresses. No Stop moves one step; Stop leaves the position. Cancellation of the entire tile also cancels Stop. Only after the settlement a familiar new step arises; preview does not promise a shift or victory before RNG.

The test of motion counts in turn Even with Stop. The cover also counts, but does not move or move dice Hunt. movement/hiding they also record this test with the correct effect. If at the end of the phase action There were none of these tests on the track, we add one point Corruption The entrance to Mordor is not a substitute for trial. ruchu/ukrycia.

<a id="towarzysze"></a>
## Comrades

The usual action separation remains forbidden on the track (BASE 39). When the separation results from the authorized events/zdolno, the selected Companion are removed from the game instead of appearing on the map (BASE 43). This forced decision takes the group without a route and leads to a change of Guide. We do not create figures in Gorgoroth, we do not activate the nation, we do not allow you to reconnect. The specific condition of the ability/cards is owned by T20/T21.

<a id="wynik"></a>
## Result

Immediately: Corruption ≥12 gives Shadow a win before the Ring is destroyed. At the completed step 5 and Corruption <12 wins FP. Militarian conditions are assessed only in the 6 phase: first Shadow ≥10 points, then FP ≥4. The points depend on the original ownership and current control of settlement, not the presence of the army, including the siege army.

The result saves the page and the cause. We finish the paid cost, remove the open decisions/frames and leave the history clear. Another mutation is rejected as `GameFinished`; re-release of the earlier approved command retrieves its receiver. The reboot does not draw the final tile again.

<a id="weryfikacja"></a>
## Verification

`MordorTests` checks both inputs and prohibited windows, preparation of the pool, all 24 tiles with independently saved expected result, zero dice and Eye with dice both sides, Stop/Reveal/hiding and idle turn, forced removal and separate movement/tile with effect. `VictoryTests` checks four V11 wins and thresholds, simultaneous conditions and control points.

`VictoryPersistenceTests` reboots the host HTTP/PostgreSQL before the final damage and after both Ring results, compares the reading of both pages, retrieves the receipt and rejects the further mutation. This reboots the same version of the rules. Migration of old command lists and fingerprints after changing the DTO version has been explicitly added to the T37 audit; it is not faked by adding a neutral field to JSON.

The final regression .NET: `bash scripts/test.sh --no-restore`, **233/233, without omission**. The PlayMode regression by `scripts/test-client.sh` includes the existing 15 scripts; UI-08 receives new track interactions. Unity results and validation of documentation saves T17 history.
