<a id="t11--polityka-i-rekrutacja"></a>
# T11 — Policy and recruitment

Sources: BASE p. 8 , 20 , 26–27 , 32 , 34–35 ; declaration and treatment are separate effects from p. 38–39 . FAQ 1.2 p. 1 More details the activation at the entrance Companion - I'm in. action transport the same authoritative command protocol as in T10.

<a id="polityka"></a>
## Politics

`StepsToWar=0` means At War; `Active` is independent. The usual action diplomatic costs Muster/ArmyMuster/WillOfTheWest and moves one of its own people by one field. A passive nation stops in front of At War. Activation does not move the tag itself.

| Event | Activate | Offset |
| --- | --- | --- |
| The enemy army enters the nation's region | Yes, the people of the region | No |
| Attack on the Army | Peoples of Army Defenders | One step each, once on battle |
| Obtaining settlement by the opponent | A separate trigger for the army's entrance | One step for the settlement nation |
| Declaration by Fellowship in the city/Stronghold in the nation | Yes | No |
| Companion Ends movement/He's in. to Play in the City/Stronghold Nation | Yes, according to the icon | No |
| card ordering army attack; Ents; Dead from Dunharrow | Like an attack. | Like an attack. |
| Other card effect | Only the clearly declared effect | Only the clearly declared effect |

Example BASE p.35: attack to the North in Dale activates and moves the North and later capture Dale moves again. These events are not deduplified as “one action”. The presence of a foreign friendly commander does not add a nation to the units of the attacked army. Triggers' calls add later tasks to the movement, Fellowship and battle in their proper steps; T11 provides and tests the rules of these events.

<a id="rekrutacja-i-koszt"></a>
## Recruitment and cost

Payload `Recruit` indicates specific die and the list `UnitPlacementChoice(UnitId, RegionId)`. The usual Muster allows one elite or two figures Common/dow/General, each in another settlement. You can only use part of the allocation, e.g. one regular; the rest of the possibilities are lost. You can not connect the elite with the other figure.

Each figure must come from the current reserves, belong to its own side and the nation of At War, and the region must be free, unrecoverable settlement from its nation. Names require Stronghold Sauron. Leader FP without army is immediately removed according to BASE p.8: also after being exposed to an empty settlement. Preview indicates such a known loss instead of changing the rule to a recruitment ban. Two figures may belong to different nations of At War. identifier, regions and a set of combinations are checked before paying dice; incorrect command does not change the status nor RNG.

Regular recruitment to besieged Stronghold is not available. `RecruitmentPolicy` allows effect card openly depart from At War, national settlement or block siege The client does not send these exceptions. card still has to define legal nations/Types/regions It's a standard effect. command always uses Muster politics; you can't smuggle flags from card In payoada.

The stack limit is checked after full action: 10 in the field and 5 in the garrison. Recruitment can create an excess; after payment of dice there is a written decision `RemoveExcess`. The player selects the exact ID figures returning to the reserves. die remains Resolving until the end of the mandatory selection. We do not treat the excess as a loss of FP. The battle loss FP goes to casualities; the loss of Shadow returns to the reserve.

<a id="opcje-i-integracja"></a>
## Options and integration

`MusterOptions` transmits nations and legal dice diplomacy and reserve groups: nation, type, specific ID He's not sending out all pairs of deployments. UI-129   `RecruitmentRules.Blocker` / `MusterBlocker` returns the code of the first rule that closes settlement  (`RecruitmentBlock`), a  `CanPlace` / `CanPlaceMuster` check that the code is missing; `RecruitmentAvailabilityProjection` explains from the same codes each settlement each own nation in `PlayerView.Availability`  (BASE  s.  20,  26–27; description of the entry in CONTRACTS Interface of pallets and ghosts. figures Development UI-04;  T11 it does not replace it with numerical fields. For excess options contain the exact subset of units and the number of deletions required.

Verification includes policy boundaries, separate triggers, atomic rejection of illegal recruitment, various nations, the absence of figures, hostile control and siege, as well as the record between paying the Muster and deciding on the excess. The actual results record the T11 story.

`bash scripts/test.sh`: 79/79 .NET, without omission. `MusterPersistenceTests` passes through production endpoints HTTP and PostgreSQL, closes the first host while selecting excess, opens the second, renews the earlier command with the same ID and finishes the selection. Result: one paid die, preserved ID, correct reserves and revision 2. `MusterTests` contains the remaining 24 new cases. Formatting of the solution and document/catalogue validators included; the Pallet interface remains within the scope of UI-04.

Correction of T21-SH-CHAR-04: the above events of the army record the cause, nation and region, and activation and displacement determine at the end of the entire action, also after the final round of battle and excess figures. The set of nations comes from the pre-loss state. Attack and capture continue to give two separate shifts. Declaration of Fellowship in its phase and the public political effects of cards retain its own moment of execution. The usual retreat does not trigger activation from the Army entry or the current Companion. Sources and exceptions of Wormtongue: [specification](T21-SH-CHAR-04.md).
