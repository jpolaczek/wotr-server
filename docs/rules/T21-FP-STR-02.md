# T21-FP-STR-02 — Strategy FP 7–12

The status of performance is in task registry. This document defines rules and design before the code; the results of acceptance are added only after the checks are done.

<a id="źródła"></a>
## Sources

The Averses Second Edition in [index](../sources/base-card-index.json) , slots `base.fp.strategy.07` – `12` . BASE 2.3 p. 22–23 (execution of events), 26–29 (movement, recruitment, movement through events), 30–32 ( battle and rescue), 33–35 (policy); FAQ 1.2 Almanach is used to find the settlements, not to replace primary sources.

- Kevin Chapman, [34881862](https://boardgamegeek.com/thread/2432138/article/34881862#34881862):
  march passes through friendly armies, gains empty enemy settlement along the way; does not pass
  through enemy army. The rules cited above require a permanent group and check limits only after
  the entire movement.
- Kevin Chapman, [38053040](https://boardgamegeek.com/thread/2689263/article/38053040#38053040):
  Woses does not strengthen any siege side Minas Tirith nor does it earn an empty Stronghold
  controlled by Shadow. In these situations, the goal is a legal neighbor.
- The condition for friendly control of settlement for Fear is the same as for Book of Mazarbul:
  [interpretation of the previous package](T21-FP-STR-01.md), BASE p. 34.

<a id="karty-i-kolejność"></a>
## card and order

| Card | Event | Fight |
| --- | --- | --- |
| 07 Fear! Fire! Foes! | Muster. Any group already separated Companion move according to the usual rules; you can leave everyone. Then the presence of Companion in friendly Shire/Bree activates North and moves directly to At War. No Carrock/Dale. | Shield-wall, the 6 initiative: at least two hits of the opponent cancel one. |
| 08 Wisdom of Elrond | Muster. The choice of the FP nation, activation and one political step; the nation can already be active. The existing T14 program remains a single definition. | Confusion, 4: natural first-run singles Shadow give FP hits and lose the possibility of reroll. Hits settled with regular round losses. |
| 09 The Red Arrow | Muster, Gondor active. One step Rohan without activation; then regular or elite and commander Rohan in Edoras, to the maximum possible extent, according to recruitment by event. | Scouts, 1: the previous retreat/asylum procedure before the throw. |
| 10 Help Unlooked For | Army. One legal army FP from the neighboring region attacks the SH army obstinate Stronghold FP. The usual rescue, participants At War, all rounds and decisions of battle. After calculating the force of SH and the limit of five subtracts the number of units of FP within Stronghold, the minimum one die. Commanders/Companion do not count; elite counts once. | No Quarter, 5: at least one hit of your own roll/reroll gives you an additional hit. |
| 11 Paths of the Woses | Army, Rohan At War. One army from any region of Rohan, also the besieged garrison, moves directly to Minas Tirith. At the control of SH or siege Minas Tirith chooses his freedom for the neighbour's army movement. He does not bypass the limitations of the nations. After leaving the entire garrison Shadow wins Stronghold, the lone commanders of FP perish, Companion remain outside siege. | Sudden Strike, 2: existing special attack according to Leadership, requires commander/Companion. |
| 12 Through a Day and a Night | Army, army containing Companion. Selected group with at least one Companion moves through one or two neighbouring regions before moving. No selection/leaving figures on the way. They are bound by army blockades, national borders and siege. Empty enemy settlement also gained along the way; limit only at the end. | Confusion, 4, identical to 08. |

Conditions and costs of events checked before rejection of card. The cost is one appropriate die paid with PlayEvent. Traffic decisions, recruitment and battle do not download another dice. Empty reserves do not block Red Arrow policy; the resulting lone commander is subject to the usual rules. Fear/Wisdom/Red Arrow do not remove Threats and Promises by the very progression of Event. The capability of Guide Gandalf the Grey opens after the event, including the entire battle Help, excess and progress.

<a id="procedura-i-kontrakt"></a>
## Procedure and contract

The event frame stores the `ArmyEventOrder` type and the public expectation of `Army`; the decision of `ArmyEvent` only provides MoveArmies or AttackArmy. The server sets the traffic policy on the basis of the program, never the field sent by the client. The existing command use one group of `Moves[0]`, and the march saves steps in `Payload.Route`, without the initial region. The same planning validates and describes the preview before the state changes. Return with movement/battle leads to the overriding event.

Private options include public figure, acceptable beginnings and short routes along with authorized ID. We only subscribe routes with a maximum length of two, never subsets of army. Additional metadata `ArmyEvent` specify no further cost, route length, requirement Companion and a clear exception of stranded commanders at Woses. Cost card, decision, group, return and modifier of the entire battle survive the record.

## UI

Army click → figure in a permanent tray → further illuminated regions on the map → one preview and approval. After the first step of the march, the player can either approve or select the other. The route is visible; the step back and change of the group do not approve action. Wosses distinguishes the garrison and shows in the preview the loss of left commanders and control of Stronghold. Help shows participants and help without moving the figures. Without numerical fields, mandatory scrolling and list of all regions.

<a id="weryfikacja"></a>
## Verification

V16 for six cards: condition/cost/illegal target, event and combat effect, partial lack of reserves, natural ones modifiers and ban on their reroll, policy without activation, two rounds of rescue, limitations of routes and a fixed group, gaining along the way and the final excess, exiting the whole/part of the garrison, both cases siege Minas Tirith. Each new decision type: PostgreSQL restart/retry and the projection of the opponent. Unity: map gestures, undo/reconcile, two clients and a size/scale tray matrix.

Done 729/729 tests .NET/HTTP/PostgreSQL , 48/48 EditMode , 5/5 Focused and 68/68 full PlayMode The Manifesto includes 38 events and 36 full cards ; exact commands, limitations and artifacts in [TESTING](../TESTING.md) The review of the structure confirmed the separation of the rules of movement, options, continuation and three client responsibilities, without sub-listing figures Next assignment: T21-FP-STR-03 .
