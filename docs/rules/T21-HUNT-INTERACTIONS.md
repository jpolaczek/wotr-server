<a id="t21--przecięcia-polowania-kart-na-stole-i-przewodników"></a>
# T21 — intersections Hunt, cards on the table and guides

Ancillary audit of acceptance T21 with 2026-10-05. It does not add the rules of the game. The `HuntRuntime` sequence, the Fellowship and `EffectRuntime` sequels with existing source and package testing. Previous tests already include Staff/Mithril/Axe, the random victim of Foul Thing, the change of guides, Vorn, Balroga and Jerkír after the multi-round battle. The missing cut is the completion of the entire action Jerkíra after nested decisions of Hunt.

<a id="źródła-i-oczekiwane-zachowanie-przed-testami"></a>
## Sources and expected pre-test behaviour

- [FP Character 1–6](T21-FP-CHAR-01.md): Mithril before the injury is determined; Axe expires after
  Gimli and Legolas leave. Obverses and BASE p.40–43.
- [FP Character 7–12](T21-FP-CHAR-02.md): Staff prevents selection before RNG; the withdrawal of the
  earlier discard is not the result of a later additional tile.
- [SH Character 1–6](T21-SH-CHAR-01.md): Bane passes the damage directly to Corruption; with 12, the
  victory breaks down further effects.
- [SH Character 7–12](T21-SH-CHAR-02.md): Foul Thing first allows protection and Guide, then draws a
  mandatory sacrifice, opens the Guide change and its ability. The additional Stronghold has its own
  normal Hunt.
- [SH Character 13–18](T21-SH-CHAR-03.md): The Vorn after a real loss can remove the public card
  Character from the table or random from a secret hand; the private identity of the discard does
  not go to Shadow. Disconnecting with Hobbit ability does not give a second loss.
- [SH Character 19–24](T21-SH-CHAR-04.md): Jerkír only chooses after the entire subsequent action
  event paid Event, with the selection of decks. Source: Kevin Chapman 19480575's reply indicated in
  the specification. The current cost remains Resolving by reactions and return.

<a id="nowe-przypadki"></a>
## New cases

| Intersection | Expected order |
| --- | --- |
| Foul Thing + Axe + Worm + Guide Hobbit + Jerkír | Refusal of protection → Gimli's random loss and termination Axe → Merry's selection → personal discard of the Worm → Merry's separation for reduction → common deployment → only Jerkíra deck selection. |
| Foul Thing + Worn + Staff + additional Hunting | Staff refusal at first kaflu → random victim Hobbit → Worn rejects Staff → back Hobbit → disclosure in Moria → additional tile without non-existent Staff → Jerkír after settling injuries. |
| Bane + Mithril + Jerkír + victory | Exchange Eye to 3 → Corruption 12 → game end and closed stack; no later selection of Jerkíra. |
| Staff + various selection events + Jerkír | Staff completes selection without a tile and RNG, but does not cancel the paid event; Jerkír selects once upon return. Also Breaking, despite another purpose of the procedure. |

Tests use the actual directory and engine commands, with the deterministic RNG and the reading of the saved decisions. `BaseCardHuntInteractionTests.PalantirWaitsForFoulThingWornNewGuideAndHobbitPlacementAcrossRestarts` checks the real HTTP/PostgreSQL: three approvals after reboot, retras in the active and restarted host, identical projections before/after reboot and no draw again. Other methods: `WornRemovesStaffBeforeAdditionalStrongholdHuntAndPalantirStillWaits`, `BaneReplacementVictoryCancelsPendingPalantirDrawAndReturnsHeldTile` and four accidental `StaffCancelsTileRatherThanEventAndPalantirDrawsOnceWithoutRandomness`.

The agreed focus with 2026-10-05: **PASS 7/7, no omissions**, no domain changes. Command: `bash scripts/test.sh --no-restore --filter 'FullyQualifiedName~BaseCardHuntInteractionTests' --logger 'trx;LogFileName=t21-hunt-interactions.trx' --results-directory .artifacts/test-results > .artifacts/t21-hunt-interactions.log 2>&1`. Report `.artifacts/test-results/t21-hunt-interactions.trx`. No differences were found in the rules in these intersections. acceptance whole 96 cards, addition of methods to the manifesto and full regression coordinates root in T21; this focus does not replace the end course.
