# T21-FP-STR-03 — Strategy FP 13–18

The status of the performance is in task registry. Specification before implementation; results of acceptance are added after the tests are performed.

<a id="źródła-i-karty"></a>
## Sources and card

Averses Second Edition, slots 13–18 In [Source index](../sources/base-card-index.json) . BASE 2.3 p. 8 (free region and coast), 22–23 (events), 26–28 (recruitment, reserves, siege , Limits), 29–32 (fight and initiative); FAQ 1.2 : Riders of Théoden. Coast read from [board](../sources/materials/board-reference.jpg) Almanach is used only to find sources.

| Card | Event, always Muster | Fight |
| --- | --- | --- |
| 13 Círdan’s Ships | Elves At War. Two Elves units, any combination of regulars and elites, in one coastal region containing an army of FP. | Charge, initiative 2; requires the FP elite in battle. |
| 14 Guards of the Citadel | Regular or elite of Gondor and commander of Gondor in Minas Tirith. | Charge, 2. |
| 15 Celeborn’s Galadhrim | Regular or elite Elves in Lórien, then one card Strategy. The existing T14 program remains a single definition. | Daylight, 3; existing limit of three dice opponent's fight. |
| 16 Riders of Théoden | Regular or elite Rohan and commander Rohan in one region: Edoras or Rohan region with Companion. | Daylight, 3. |
| 17 Grimbeorn the Old, Son of Beorn | Regular or North elite and North commander in Carrock. | Scouts, 1; FP as a defender, battle half or half. |
| 18 Imrahil of Dol Amroth | Regular or elite of Gondor and commander of Gondor in Dol Amroth. | Shield-wall, 6. |

Charge throws a dice number equal to the number of participating elites remaining at the time of execution, up to five. It is a separate attack with a simple, unmodified hit threshold. Losses apply before the throw; change the number of dice from the opponent. The race is decided by the defender. The destruction of the army ends with battle before the second effect. The condition of play is checked earlier than the size of the effect — [Kevin Chapman, 20999524](https://boardgamegeek.com/thread/1477010/article/20999524#20999524).

<a id="wspólna-rekrutacja"></a>
## Joint recruitment

Events use existing `RecruitmentRules.EventGarrison`: allowed both sides siege, not allowed empty settlement under control of SH and region with enemy army in the field. Policies do not restrict these cards except the printed condition of Círdan. Riders can recruit in besieged Helm’s Deep only with Companion, also in Eastemnet/Fords of Isen without settlement; presence of Companion alone does not release the acquired settlement.

The maximum possible number of recruits is made. No reserves or legal target does not block the event itself; the other instructions still work. You cannot abandon the second available figure Círdana. The player over-resolutions after the event; he can surrender the unit of any nation. The ability of Guide Gandalf Grey only after the completion of mandatory decisions, including the excess and limit of the hand.

<a id="architektura-i-ui"></a>
## Architecture and UI

`RegionDefinition.Coastal` is an overt fact of board, not a conclusion from the location of a polygon. The selection step of the region saves `EffectExecution.RegionId` and only makes available legal regions. Recruitment takes on the number of figures and offers at most the equivalent reserves of each type as you can recruit. Two figure Círdana is selected together; the server verifies the exact number required and their availability. The whole use the existing recording of the effect frame, private options, preview and idmposite command.

Selecting the target on the map → the reserve icons in the fixed tray → preview both figures in the target region → one recruitment approval. Without the number fields, list of all regions and additional dice. The only legitimate choice is made by the engine; the choice between regulars and elites remains the player. Charge losses use the existing special attack and loss panel.

<a id="plan-weryfikacji"></a>
## Verification plan

V16 for six cards: legal and illegal costs, windows, targets, event/combat. Círdan: full list of coasts, army of any nation, two types, partial reserves, forced full recruit, both siege and excess. Riders: common region of both recruits, lack of political constraint, Companion, busy settlement Celeborn: selection despite lack of recruits and order of hand/Grey limit. Charge: lack of elites, limit of five, current participation, thresholds Field/city/obl fertilisation, unmodified throw, immediate loss, initiative draw and end battle.

Each real selection window: PostgreSQL restart/retry; no secrets and mutations of preview. Unity: a selection of map and two figures, Selection/cofni, visible preview, two clients via HTTP, tray without scrolling in the matrix of size and scale.

acceptance: 793/793 .NET/HTTP/PostgreSQL, 50/50 EditMode, 4/4 focused and 72/72 full PlayMode, without omission. Manifest: 43 events, 42 full card. Artifacts, actual commands, overview of structure and boundaries in [TESTING](../TESTING.md). Next task: T21-FP-STR-04.
