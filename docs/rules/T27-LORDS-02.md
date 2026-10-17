<a id="t27-cards-02--karty-cienia-lords-17-2528-i-strategy-25"></a>
# T27-CARDS-02 — card Shadow Lords 17, 25–28 and Strategy 25

<a id="źródła-i-zakres"></a>
## Sources and scope

Six obverses SRC-03 , positions 7–12 In [index](../sources/lords-card-index.json) , watched before implementation. LORDS p. 3 defines the base swap card 17 , p. 7–11 selection dice Special, p. 21–22 Sméagola tiles, p. 23–25 Balroga. FAQ 1.2 p. 3 place Sméagol and Corruption We Won’t Go Back Against Protection Hunt . [Audit](../sources/SRC-03-AUDIT.md) separate sources from acceptance code. The following description is an implementation contract; the results of the checks will be recorded after they have been executed.

<a id="wejście-koszt-kolejność-i-decyzje"></a>
## Entry, cost, order and decisions

| Card | Event | Fight |
| --- | --- | --- |
| `lords.sh.character.17` A Balrog is Come! | Character, Balrog in the game. Inactive becomes active (2 level), motionless. Already active allows you to move the character itself or move/attack an army containing Balrog. One fee of the entire event. | Durin’s Bane, initiative 2: Defender to two regions from Moria **orbo** Balrog participates. Three additional dice, hits 4+, immediate losses before Combat Roll. |
| `lords.sh.character.25` We Won’t Go Back | Character, Sméagol Guide, on the table. Selecting Sméagol tiles while Hunt opens the optional choice Shadow: Reject card from the Character deck from your hand or this card from the table. Cost and two direct points Corruption approved together, against protection; you can refuse. | Fear of Their Masters, initiative 3, participating Minion. The entire Leadership behind +2 Combat Roll; any natural 6 FP in Combat Roll or Leader Re-roll gives an additional hit against Shadow. |
| `lords.sh.character.26` The Lore of the Elven Rings | Character, on the table. Before Action Resolution, when Keeper threw Eye, optional change of one dice from its own Action Roll. Selection of dice and its result is public and perpetuated. FP removes card by Will or any available die and selected its own Ring. | Fear of Their Masters, identical rules for both copies. |
| `lords.sh.character.27` The Gates are Closed | Character, on the table. Declaration/disclosure by the inconvenient Stronghold Shadow (also the beginning and the remaining) gives the option to discard this card for an additional Hunt. Eye without effect, Reveal omitted. FP removes Will; Character suffices with Grey or Gollum as Guide. | Words of Power, initiative 1, Nazgûl participates. Selected participating Companion loses Leadership and ability to finish the round, also version of Lords. |
| `lords.sh.character.28` Captain of Despair | Character, on the table, WK in the game, Guide other than Gollum. Only another used Character results pass to Hunt as Eye after all paid action. The rejection at the declaration in the unaccounted Stronghold FP or Gollum entry as Guide; the departure of WK also removes card (BASE p. 22). | Servant of the Shadow, the 3 initiative, participating Minion. Sacrifice of the whole Leadership of one selected Past reduces the number of dice Combat Roll FP after a five-limit minimum of one. |
| `lords.sh.strategy.25` Shadow of Dol Guldur | Army, Nazgûl in Dol Guldur. Leadership Nazgûli sets the number of regular Sauron, at most five, from the reserve and in the stack limit already before the maneuver. movement/attack This army, no second dice. | Mûmakil, 3 and 5 initiatives, participating Southrons elite. +1 Combat Roll; after reroll, the advantage of hits gives you an additional one. The comparison takes into account special hits of FP before the fight. |

<a id="stan-bezpieczeństwo-i-interfejs"></a>
## Status, security and interface

Fear explanation: [Kevin Chapman, 23.04.2022, response 39935225](https://boardgamegeek.com/thread/2854715/article/39935225#39935225) confirms that White Rider can prevent sacrifice** Leadership, even when Leadership elit/Miniona. card is still legal, but does not give +2; extra hits for FP sixes work independently. Example: WK, Nazgûl and Isengard elite against White Rider → no bonus, FP six will double-check. The test will also include a stand-alone Gothmog and rearguard. Source read by public API BGG, author of `34645`; local proof `.artifacts/source-audit/SRC03/thread-2854715-articles.json`.

The audit correction of SRC-03: **BASE 2.4.1 p.22** orders to immediately remove card from the table if its playing condition ceases to be met. The Captain therefore expires also after the departure of WK; WWGB expires after the loss of Sméagol as Guide. The absence of repetition of this rule on the obverse does not create an exception. The previous notation of the unprinted death of WK was misleading; the index and the required tests corrected.

Lore can change die Lesser Minion and cast Eye: [Chapman 30587888](https://boardgamegeek.com/thread/2103097/article/30587888#30587888), [Chapman 33763703](https://boardgamegeek.com/thread/2103097/article/33763703#33763703). It does not include dice assigned to Hunt before the throw. The change does not remove card from the table; both details confirm the same thread, downloaded to `.artifacts/source-audit/SRC03/thread-2103097-articles.json` (author `34645`).

The cards programs belong to `Lords/Cards`; shared operations battle, travel, cost and Hunt retain existing resume points. The server sets the condition, cost and set of options again at approval. Each strategic branch has a clear window with resignation if it is allowed by text. UI uses objects, existing decision trays and one preview; it does not add numerical fields.

Permanent effects do not read a secret hand at the decision whether to show a public window. In We Won’t Go Back only Shadow sees a list of its own cards at cost; the other side sees the discard of one card and the change of Corruption. Future tiles and draws remain on the server. When changing dice, it is not allowed to lose its source and print the removal; the origin of `captain-of-despair` remains separate according to KINGS p. 17.

The event is finished with battle, losses, reactions and stacks. The recording includes the paying die, the chosen army/form, the Hunt stage and optional reactions. The repetition of the identical command after the reboot returns the saved receipt.

<a id="przykłady-i-plan-weryfikacji"></a>
## Examples and verification plan

- Activation of the inactive Balrog ends the event; the active can move alone or with the chosen
  army. The base card 17 is replaced, not added.
- WWGB: refusal, both costs, empty hand, wrong deck, Mithril/Nenya, Sméagol input and selection at
  his/after he/she leaves; Corruption 12 ends the game.
- Lore: The usual Eye does not trigger, several Keeper Eyes gives one change, the choice of dice
  special and regular, the choice of the Ring, the absence of cost/illegal phase.
- Gates: entrance, exit, route, will remain, won Stronghold, both location types, both acceptable
  Guide, Eye/Reveal, entry to Mordor only after the declaration has been closed.
- Captain: Own die events excluded, another nested battle before transfer, cost other than
  Character, death WK, change of the Guide and declaration.
- Dol Guldur: nine units and Leadership three gives one recruit; reserve empty/au A mantle,
  garrison, optional maneuver and not allowed second army.
- Combat: presence of required figure, cancelled Command/zdolno, both roles, natural 6 in both
  throws, minimum of one dice, Advantage/draw after a special attack, cancel card and reset the next
  round.

Each real selection window: serialization, HTTP/PostgreSQL reboot, reboot, rejection command opponent and random preview. Manifest will link all the rows of obverses and cases of SRC-03 to the actual tests. Full regression of the database is to preserve the frozen body. acceptance whole Lords/UI-09/T34 and manual user tests remain distinct.

<a id="checkpoint-trwałych-efektów"></a>
## Checkpoint lasting effects

Four table effects were implemented, the private cost of WWGB against protection, one joint decision die + Lore score, the cost of removing both cards, the origin of dice Captain and the resumption of other reactions to the location. The response offer is not dependent on hand; the selected reaction disappears from the established list, the other conditions are rechecked.

`test.sh` with filter LordsGates/TableRemoval/Chief/WeWont/Captain/Lore/Shadow04Palantir: 64/64 PASS ( `t27-02-tables-fixed.trx` New frontiers of six cards and 9 scenarios HTTP/PostgreSQL with nooses after decisions: 22/22 PASS ( `t27-02-persistence-fixed.trx` . Extended series of effects: 79 PASS , 2 FAIL from misprepared tiles in the test; after the initialization of fixture whole `LordsWeWontGoBackTests` : 8/8 PASS ( `t27-02-replacement.trx` Unity. EditMode : 125/125 PASS ( `t27-02-editor.xml` ), after improving nullable CommandKind When choosing the Ring. It's checkpoint, no acceptance packages; further cuts remain, actual network client, source coverage manifesto and full regression.

<a id="odbiór-paczki-02"></a>
## acceptance packages 02

2026-10-06 : all six events and six combat applications are active under the Lords configuration. Replacement of base Balrog, decks 28/26/28/26 , ownership, phase, costs, expiration and reset of the round have behavior tests. Manifest packages 01–02 covers all rows of sources: 12 cards , 102 methods and 179 the cases performed. Intersections include White Rider/Fear the cancellation of six Combats, Words/Council , minimum Servant, Mûmakil with special hits, Gates/Chief/ Fire/stronghold and entering Mordor, Mithril/Nenya/WWGB and nested battle Balroga paid dice Captain. 12 method of persistence performs 48 scenarios HTTP/PostgreSQL each with reboot before and after command , two views, preview without RNG , rejecting your opponent and retra without re-charge.

Commands executed:

- `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
  --logger 'trx;LogFileName=t27-02-full.trx' --results-directory .artifacts/test-results` —
  **2226/2226 PASS**, 0 omitted, 1m19s; local .NET 10.0.401 and PostgreSQL 17.11 variables
  `DOTNET_CLI_HOME`, `DOTNET_CLI_TELEMETRY_OPTOUT=1`,
  `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true`. Frozen base body unchanged.
- `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults
  .artifacts/t27-02-editor-final.xml -logFile .artifacts/t27-02-editor-final.log` — **125/125
  PASS**, 0 omitted, Unity 6000.6.0f1/macOS.
- `bash scripts/test-client.sh -testFilter
  'Wotr.PlayTests.LordsShadowNetworkTests;Wotr.PlayTests.NetworkTests;Wotr.PlayTests.T23FullGameNetworkTests'`
  — **6/6 PASS**, 0 omitted, write `t27-02-play.xml`. Two clients, Kestrel and PostgreSQL; four
  new windows cards Lords and the base reconnect/full playthrough.
- After visual correction of the name of the tile Sméagol and summaries kosztu/wyniku: `bash
  scripts/test-client.sh -testFilter 'Wotr.PlayTests.LordsShadowNetworkTests'` — **4/4 PASS**,
  `t27-02-ui-final.xml` All new windows checked on 1366×768 and 1920×1080 , scale 1.0/1.25 ; 16
  discharges `lords02-*` , border control, lack of ScrollView/TextField , watched drop of Lore and
  Sméagol at 1366/1.25.
- `python3 scripts/lords_card_coverage.py --results .artifacts/test-results/t27-02-full.trx` —
  **PASS: 12 cards, 102 method, 179 cases**.
- `python3 scripts/tasks.py validate`, `python3 scripts/check_catalog.py`,
  `python3 scripts/source_lords_cards.py`, `git diff --check` — **PASS**.

The first series detected fixture errors (ID characters/region, initialization and Hunt steps, and number of dice after loss) and nullable CommandKind at Unity. All corrected, and the complete final run has no errors or omissions. The module structure has been reviewed; the longest new production file has 67 rows, the solutions remain small steps without the dependencies of Unity/Networks in the domain.

Amendments: `Lords/Cards` common effects/Hunt/dice/MatchFactory contracts and RoomStore , manifest, client descriptions and tray, separate tests domains/persistence and seed `LordsShadowScenario` Checkpointy: `4b1c8a0` , `3222594` ; commit acceptance follows the status entry. Open restrictions: T27-CARDS-03 and acceptance T27, full start Lords/UI-09/T34 , separate Windows build and postponed manual testing of the user. Next ready to do as planned: **T27-CARDS-03**. No push nor implementation .
