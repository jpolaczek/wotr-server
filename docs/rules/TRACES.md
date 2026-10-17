<a id="ślady-specyfikacji-t03"></a>
# Traces of the T03 specification

2026-10-04 performer review: the following traces have passed the description, owner and recording control. This is a specification review, not running engine tests. `verification_cases` directory provides the planned cases; `tests` field does not succeed in performing them.

| Run | Step and Attribution |
| --- | --- |
| Turn | B02 RecoverDice → B06 DrawBothDecks (automatic) → DiscardCards FP/SH only if excess (subscription) → FellowshipPhase FP (subscription) → B17 AllocateHunt SH (subscription) → RollDice/TransferRolledEyes (automatic) → B03 Action FP (subscription), Action SH (subscription), alternately → B20 CheckMilitaryVictory (automatic) → another turn. |
| Fight Round | B11 attack already paid → CombatCard attacking (write, covered) → CombatCard defending (write) → RevealCommittedCards → initiative order and possible reaction (separable write) → B12 CombatRoll and reroll (automatic) → Losses attacking (write) → Losses defending (write) → B13 BattleContinue attacking (write) → DefenderRetreat defending, if continuation (write) → next round / Advance / end. |
| Hunt with reaction | B15 paid for move FP → progress → B17 roll/rerolls → B18 tile → Reaction of owner of legal intervention (return registration) → intervention effects → HuntDamage FP (write) → possible Guide FP (write) → FellowshipRoute at disclosure (write) → additional tile if required → return → die traffic to Hunt → another action. |

Review border cases: eight cards after the full selection procedure gives a single two-discard window; card's refusal to attack does not ignore the defender; the reaction does not change the actor action; the re Hunt is a frame back, not overwrite the first. The clear log shows the intervention played, not the list of unelected cards's opponent.

The modules are entry conditions, not overwrite order. Without Lords there are no Council/Keeper windows; without Kings — Ruler/Corruption; F01 and the dependent part of X03 remain `unresolved` to GAP-FATE. Full cards B06/B10/L02/K03 texts have their own gate. Normal cases of these areas describe the rule framework; they do not authorize implementation with unknown capability.
