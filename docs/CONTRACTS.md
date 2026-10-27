<a id="kontrakty-i-maszyny-stanów"></a>
# Contracts and State machinery

The contract is implemented in stages T05–T07. In T06 there are DTO `PlayerView`, `ActionOption`, `DecisionView`, `ActionPreview`; endpoints adds T07. Each field has a specific visibility and version. The table below also includes target contracts for later tasks.

<a id="główne-komunikaty"></a>
## Main messages

<a id="korespondencja-t35"></a>
### Correspondence T35

`CorrespondenceContracts` does not contain full status or legal options. All these endpoints require a session and the account comes from authentication:

| Endpoint | Answer and Semantics |
| --- | --- |
| `GET /api/matches?status=All&limit=20&cursor=` | `MatchListView`: list of `MatchSummary` and `NextCursor`; limit of 1–50, empty cursor ends list. Filters All/YourDecision/Waiting/Finished; stable order of ID. LOBBY-05: without `status` the list is My games, the unfinished games only (YourDecision and Waiting); `status=All` adds the finished ones. `Name` of a game between two people is `<name> · <Free Peoples username> vs <Shadow username>` (only the players when the stored name is empty), the same for both; a game with a technical account (demo CPU) keeps its stored name. |
| `GET /api/matches/{id}/activity?afterRevision=` | `MatchActivityView` : Current revision, own LastSeenRevision used AfterRevision , 50 last visible entries and HasEarlierEntries The omitting parameter uses the saved LastSeenRevision . . |
| `PUT /api/matches/{id}/seen` | `SeenRevisionRequest.Revision` must be given and belong to 0...current revision. `SeenRevisionView.LastSeenRevision` returns the monotonous maximum of both positions. |
| `DELETE /api/matches/{id}` | MENU-21: Party player removes it for both sides (204 without content); foreign account and no party give 404. Party disappears from the list of both players, and open update sockets close. |
| `POST /api/rooms/{id}/demo` | DEMO-01: A room member in the state of Waiting launches its base game demo against CPU (200 `DemoMatchView`: `matchId`, `you` = the page of its place). A new demo removes the previous demo of this account with the entire save; the room does not change. An alien, unknown or closed room: 404; started: 409 `MatchAlreadyStarted`. |

`MatchSummary` contains ID batch, opponent name, You, Configuration, Turn/Phase , Revision, own LastSeenRevision/HasUnseenChanges , Status, public DecisionOwner , YourDecision only for the owner and Winner/Victory. Not included ID/parameter This private decision, history, cards No ack means - 1 So, a new party for a revision. 0 has new changes. Status depends on pending decision, not on the contractor started action .

GET does not move the read position or the engine. Ack of the old displayed view does not include a newer revision. Refreshing the list and plain GET Projections repair lost notification without replay; do not require open WebSocketu No session gives 401 , foreign/nieistniej of which 404 , wrong range 400 ( `InvalidPage` either `InvalidRevision` ).Summary allows AfterRevision =- 1 ; the history limit applies only after rejecting events invisible to a given page.

Implementation, privacy and plan acceptance: [T35](server/T35-CORRESPONDENCE.md).

<a id="dziennik-akcji-ui-38--kontrakt-wdrożenia"></a>
### Journal action UI-38 — contract implementation

Authorized `GET /api/matches/{id}/actions` returns `ActionLogPage` with steps and their headers action Parameters: `filter=All|You|Opponent` , `limit=1…50` (default 50 ), optional `before` for the older page, or `after` for growth. Do not combine both cursors. Older pages go from the latest, increase from the oldest unset step; `NextCursor` continues the same direction, `LatestCursor` allows you to check new steps. The cursor binds the batch, perspective and filter to the visible ID entry; does not contain hidden step item. New old step action receives new ID And it's going to grow.

`GET /api/matches/{id}/actions/{actionId}?filter=All` returns `ActionLogView`: the header and all the accepted steps of this action. ID outside of the visible story gives 404, incomplete/mistake/foreign cursor 400 `InvalidPage`, no session 401. Reading does not mean see, does not perform command or RNG. Previous activity, seen and limited history in PlayerView retain the existing contract.

`ActionLogStep` has a stable `Id`,  `ActionId` safe `ParentStepId`,  `Revision`,  `Order` (rail of visible events in revision), optional `Turn`,  `Actor`,  `Command`, public `Automatic`,  `Legacy`,  `Kind` / `Text`,  `Context`,  `Costs`,  `Movements` and `Status`.  ID It's not CommandId. `ActionLogSummary` gives the initiator, context, type action,  turn,  `PartialHistory`, status, last visible ID  (`Version`), the number of steps after the filter and the common cost. The additional cost of the reaction remains at its step. The filter performer comes from a step; the machine without the contractor and the former entry without the author belong to All. The headers and expenses of the parent only use the accepted facts, without revealing the omitted steps or counters.

`ActionContext` indicates the host type and the public regions, definition, nation, BattleId or chain Hunt. `ActionCost` distinguishes die, Ring, Token, card, ability and no cost; preserves owner, source, original wall and application. `ActionMovement` saves source, target, actual route and participants: ID/nar/type figure or the then definition of the character, page and garrison. It does not contain a full stanu/payloadu.

Metadata are part of the existing GameEvent and active group part ResolutionState The writing is atomic with decision and receipt. Additional fields are optional in snapshot v3; reading does not add to the old facts. Old texts get stable identity based on the party and position in the journal, without the interpretation of Text. The ongoing old sequence begins a partial description. `match_events` is reproducible with the Event snapshot. Light migration adds columns/indexes without global scan of records. New batch saves full index, old completes it in transaction next accepted command . GET old party reads only her Events with JSONB , without migration or change snapshotu/receipt/regu The two paths keep identical ID . The visibility filter is valid before page, order and headers. UI-38 supply vertical movement/Finish ; full typed coverage will provide UI-39 .

<a id="stan-i-komendy-gry"></a>
### State and command game

The window semantics are defined by [Inventory of decisions](rules/decisions.json). The presentation type describes the selection, not ready widget. Each response saves the owner, ID the decision and return to the previous effect; we do not rely on the current client screen. [Traces](rules/TRACES.md) separates slots and player responses.

| Type | Content / Responsibility |
| --- | --- |
| `MatchConfiguration` | Three expansions flags, variants, rule/content version, configuration version and acceptances of both sides. No secrets to prepare the game. |
| `GameCommand` | `matchId`, `commandId`, `expectedRevision`, `decisionId`, `kind`, typed `payload`. The player results from the session, not from the `actor` client field. |
| `PlayerView` | Review, public board and resources, own hand, visible opponent data, safe history, current context and your own decision. |
| `PendingDecision` | Stable ID, authorized page, type, window, allowed answers, cost already incurred and condition of completion. Private content only for the owner. |
| `ActionOptions` | Available types of activities and further restrictions on the choice of object, unit, purpose and cost. We do not list all combinations of division of armies. |
| `ActionDraft` | Selected objects, steps and intent to pay; local, reversible, linked to the revision. It is not the state of the party. |
| `ActionPreview` | Confirmed revision, known effects, cost, decision-requiring elements and information about outstanding randomness, without drawing or revealing secrets. |
| `DecisionPresentation` | Presentation semantics: e.g. the choice of figures, region, card, dice, loss. The server does not impose pixels or layout Unity. |
| `CommandResult` | Receipt with the result of acceptance/rejection, revision and safe reason; current view or link to download it. |
| `AutoDecisionPolicy` | Owner, battle, range `SkipCombatCards`, hand version, activity; expires with battle or change of hand. |

UI-11 : [description of the implementation](https://github.com/jpolaczek/wotr-client/blob/a7967afe0ca8b2b02d3777503ccc99eec0a25e63/docs/ui/UI-11-AUTO-DECISIONS.md) . `PlayerView.AutoDecision` and the receipt changes contain only their own `Version` , `HandVersion` , `Policy` , `LastSkippedBattleId` and `LastSkippedRound` . `SetCombatCardPolicy` send `Payload.AutoDecision { BattleId, ExpectedVersion, HandVersion, Enabled }` This is a private setting outside the decision queue: instead of `ExpectedRevision/DecisionId` validate current battle and private CAS and on the turn on the hand version. Normal command they retain full validation. Disabled after the saved “No card” does not reverse this response and is not invalidated just because the game revision has changed.

`CommandReceipt.PublicStateChanged` distinguishes a private change from the approved game response. The preference itself does not change public revision, history, RNG nor the projection of the opponent. Each actual hand mutation passes through `HandRules`; increases the hand version and extinguishs the policy. Automatic response arises only with open own `CombatCard`, retains its ID/In order and uses the same solution as manual rejection. It does not create a separate command or second spending.

<a id="przykład-ruch-dwóch-armii"></a>
## Example: The movement of two armies

UI-16 server contract; client gestures integrate UI-17:

1. The player selects a specific die, figure and the target. Safe options come from the server.
2. The final click/drop approves the first step: movement, cost once, used ID and the remaining
   budget are saved atomicly with a receipt.
3. The server opens the continuation of the same action. The second move cannot use the moved ID.
   The action type cannot be switched or the first march undone.
4. The second step or public finish of Finish one is action; only then will the proper final limits
   and postponed policy. The excess remains a separate PendingDecision.
5. Retry recovers the same receipt. Old records and confirmations retain the reading; migration does
   not take steps nor RNG. There are also sequences of recruitment and travel of groups of
   characters.

`CommandPayload.Stepwise=true` limits `MoveArmies` , `Recruit` , `MoveCompanions` and `MoveShadowCharacters` to one movement/deployments/Groups au command The first step is done in `Action` , another in `ActionSequence` ; `DieId` , type and `AbilityId` remain identical, and for the march also `MovementCost` . `RulerTokenId` is only allowed in the first step of the right ability; the continuation does not reissue it. `FinishActionSequence` It has an empty payoad and ends the remaining budget. You can not switch to attack, recruiting or legacy whip in the march started.

`PlayerView.ActionSequence` includes public `Kind`, `Owner`, `DieId`, `Face`, `AbilityId`, `CompletedSteps`, `Remaining`, `UsedIds`, `UsedRegions`. `Remaining` denotes marches, recruitment points or the number of unused figures travel. Initial set of candidates remains internal. Only the owner gets `Decision` and follow-up options. `EligibleDice` continuation already indicates paid die Resolving; preview has an empty new cost. Preview Finish shows excess and deferred policy without performance of the rules on or RNG.

The default `Stepwise=false` keeps the atomic list to date. Events cards and Council chips continue to use their public commands/frames; the flag does not give them the exception of recruitment or additional coverage. The 2 protocol adds a new type of decision/commands and requires a consistent client for new gestures. Snapshot v3 migrates v1/v2 without creating the initial sequence, revision or draw change. Fingerprint v2 omits the default false; the unchanged old payload recovers the old receipt, but adding true changes its identity also to the old hashs.

<a id="przykład-bitwa-i-karty"></a>
## Example: battle and card

`BattleStart → AttackerCardCommit → DefenderCardCommit → Reveal → OrderedEffects → CombatRoll → LeaderReroll → AttackerLosses → DefenderLosses → AttackerContinue → DefenderRetreat → NextRound/End`.

This course is a skeleton. The effects of cards can end battle earlier, force an additional decision or modify the correct window; the record must contain a place of return. We do not skip the stage of the defender just because the attacker has resigned from card.

From the end of the first approval, the opponent sees the allowed reverse; the full card is only revealed in a common step. The failure between approvals keeps the secret and order. The short-cut omission of cards acts as the owner's response at the right time, not as removing phases from the machine.

<a id="przykład-drużyna-i-polowanie"></a>
## Example: Fellowship and Hunt

`MoveFellowship → Progress → HuntRoll → HuntRerolls → DrawTile → InterventionWindows → DamageChoice → ApplyCorruption/CompanionLoss → RevealIfRequired → AdditionalTileIfRequired → CompleteAction`.

The individual instructions cards and expansions specify the exact windows. The engine stops at any real selection. The usual progress of Fellowship does not contain the secretly scheduled path of the regions. The route becomes the choice only with declarations/disclosure when needed to resolve it.

<a id="błędy-i-odświeżanie"></a>
## Errors and Refreshing

T06: `Projection.For(state, side)` building new DTO with a list of permitted fields. The deck order, foreign hands/rejections and frames are never serialized to view. `DecisionView` The only options are the owner's. `Visibility` filter history and error codes. `PreviewService` only cause validation, without port RNG And without Apply. `IActionOptionsProvider` is the point of extensions; T10 adds cycle options turn , T11/T12 further types action . `EligibleIds` , `Targets` and `EligibleDice` represent the selection stages without the numbering of the combination; `DraftSelection` control the subset, duplicates, limit and cost.

`DecisionPresentation` mapping on selection, preview and cost describes [Inventory UI](ui/interaction-inventory.json). No type has a presentation of the general numerical form. `Sending` state blocks a recommit of the same decision, but leaves a map and inspection of cards.

Minimum categories: `NotAuthorized`, `StaleRevision`, `DecisionExpired`, `IllegalChoice`, `InsufficientResources`, `InvalidConfiguration`, `CommandIdConflict`, `UnsupportedSaveVersion`. Reason may only use the data visible to the recipient. We do not respond to "the opponent has card X", to explain the inaccessibility of action.

With the revision error UI downloads the current projection, retains legal parts of the draft and requires re-approval of the modified result. On timeout, it does not create a new `commandId`; first checks the result of the old one. The sent command has one state of waiting and the multi-click does not generate any more spending.

UI-10 Details [versification and recovery](https://github.com/jpolaczek/wotr-client/blob/a7967afe0ca8b2b02d3777503ccc99eec0a25e63/docs/ui/UI-10-TRANSPORT.md) and [_Other Organiser](https://github.com/jpolaczek/wotr-client/blob/a7967afe0ca8b2b02d3777503ccc99eec0a25e63/docs/ui/UI-10-DRAFTS.md) We compare game revision, private version of the policy and hand version; old and identical projections do not undo the view or delete local Undo. New revision invalidates previous preview. Removed cost remains empty, even if another die It's the only way to make it. command , not projection: the older receipt does not reverse board Recovering starts with GET Receipt; only 404 allow the same repetition command The exact payoad is stored as an independent copy. Results acceptance are recorded in [network protocol UI-10](https://github.com/jpolaczek/wotr-client/blob/a7967afe0ca8b2b02d3777503ccc99eec0a25e63/docs/ui/UI-10-NETWORK.md) .

`CommandShape` borders reject `null` on the Army traffic route and lack identifier source/target without calling the right planner. `IllegalChoice` does not change status or download RNG. The empty route list remains the correct shape for normal traffic; geography and cost are still set by the rule module. [Tests of raw payoads](server/UI-10-ACCEPTANCE.md) confirms the domain and seven actual cases of HTTP/PostgreSQL after unlocking the environment.

<a id="wymagana-trwałość"></a>
## Required durability

T37: fingerprint retrieve has a prefix `2:` and canonical SHA-256. It retains the identity of commands/game/decision and the expected revision, organizes the properties of the objects, preserves the order of the arrays. Only the actual default values of the fields DTO are omitted; `null` the list differs from the empty list. Three old, undivided shapes have a clear read-out. The new blank costs of Ruler/Council cannot match the format without these fields. The change of semantics/defaultu the existing field requires a new version.

Snapshot v1 goes to v2 without execution turn changes ID waiting decision. Unsupported record gives HTTP 503 UnsupportedSaveVersion damaged JSON/nag Pen/structure — CorruptSave These are safe codes, without details of the private state. The login limit gives HTTP 429 TooManyRequests and the Retry-After header; the client does not re-enter the login itself.

T07 provides [endpoints and semantics receipt](server/T07-TRANSACTIONS.md). WSS carries a revision signal; after reconnecting HTTP retrieves the view and the earlier receipt. You do not need to play every missed message.

UI-11 adds a private version of the preferences to the WSS signal (only the recipient version). The change of the settings publishes a notification only to the sessions of its owner; the public decision continues to notify both sides. Five-second version reading recovers lost signals. It does not move hand or policy itself by WSS.

T05 implements `GameCommand` , `CommandKind` , `CommandPayload` and receive in Contracts package. Payload has semantic fields ( `DieId` , `RingId` , `Deck` , `Face` , list `Ids` ); does not include an actor. Domain stores separate models of full state; they must not be used as answers HTTP Each frame of the continuation contains ID effect, step and suspended decision; already paid dice remain in `PaidDieIds` .

We do not store pending decisions exclusively in WebSocket, coroutine or process memory. The serialization must recreate an identical set of legal responses, private information from both sides and follow-up effects. Each new type of decision requires a save-restart-reply scenario.

<a id="lokalna-kontynuacja-ui-12"></a>
### Local continuation of UI-12

`LocalMatchFiles` keeps the recorded records under the hash of the server/account/match. range Separate `action`, `policy` and `draft` do not contain sessions, passwords or the whole PlayerView. `CommandJournal` retains a deep copy of command with its original ID before HTTP; incorrect recording or a pending record stops the upload. The nuclear switch preceded by the flash protects the recording, and the removal requires the ID compatibility. Late recapt does not remove the new command.

`SavedTurnDraft` contains payoad, local editing context and minimal MatchId/You/Turn/Phase/DecisionId/Kind/Window/SourceId and window flags. It does not store legal options, hands or maps. When opening draft passes existing CanCarry/Reconcile; the server still calculates legality. Undo does not exceed reboot. Quick choices are combined into the last asynchronous record, and the change of the batch is waiting for the end of the queue. Unsolved command remains separate from draft.

`MatchSession` rejects events after the screen is released. The first projection shows the current decision without replaying. `MatchSeenTracker` sends monotonous PUT seen only after displaying the mounted board. Coalscing and rewriting this auxiliary recording does not stop the game. The list and Changes perform only GET. Private recording is not automatically transferred between devices. Terms and results: [UI-12](https://github.com/jpolaczek/wotr-client/blob/a7967afe0ca8b2b02d3777503ccc99eec0a25e63/docs/ui/UI-12-RETURN.md).

<a id="konta-i-pokoje-t08"></a>
## Accounts and rooms (T08)

[Endpoints, privileges and states of peace](server/T08-LOBBY.md) use separate DTO `LobbyContracts` The configuration version is independent of batch revision. `Dismay=null` at the input means default value dependent on Lords + Kings; write and view contain the settled value. The server accepts 93 of 512 A clear combination of flags. T34 allows the start of all 93 legal configurations after acceptance of the same version by both parties; opening the batch does not change the configuration or replace the setup expansions A base.

<a id="cykl-i-koszty-po-t10"></a>
## Cycle and costs after T10

[Performance specification](rules/T10-TURNS.md) binds CommandKind to the phase, cost and continuation. TurnOptions complements the projection with candidates; PlayerSummary contains public VictoryPoints. AfterDiscard and RingActionPending are only part of the domain record. DeclineReaction does not change the normal performer. CommandSender retains uncertain command to recover the receipt; UI does not create a new ID at the retry.

<a id="wspólne-wejście-ui-03"></a>
## Common input UI-03

`DraftToken` identifies a visual object or target (die, Ring, card, action, deck, tray). `DraftInput` translates the movement of the indicator to `DraftGesture.Drop`; it does not have API commands. Illegal drop leaves the payload unchanged. Click and drag update the same `TurnDraft`, and `TurnInteraction` cancels the outdated preview and activates one commit only after the answer to the current revision.

History of local elections stores copies of payload and the type of action, maximum 32 steps. Esc undos the step, and a new revision removes the story to not reproduce the cancelled cost. The cost adjustment only works when starting a new intent; reconcile does not select another dice. One class of ordinary dice with the same effects can get the default cost; Will of the West and special sources require indication.

Enter is supported as press-release and requires legal preview. The new key events do not trigger a new decision. The second part of the two-click does not activate the control displayed after the server has responded. The tray retains control instances during local selection changes; the network blocks the approval, but not the local selection preparation or map.

Support for pointer capture also tracks the source button, as intercepted events bypass the trickle-down of the ancestors. Behavior checked in PlayMode and in [Unity EventDispatchUtilities code](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/Core/Events/EventDispatchUtilities.cs); the overall order describes [documentation Unity 6000.6](https://docs.unity.com/en-us/engine/6000.6/manual/uitoolkits/uielements/uie-events/dispatching). Callbacks and interceptions are deleted at the end of gestu/ekranu.

<a id="polityka-i-rekrutacja-t11"></a>
## T11 policy and recruitment

`CommandPayload.NationId` indicates the nation action `AdvancePolitics`. `Placements` is a list of `UnitPlacementChoice` from ID figure and the region. The server never accepts from the client flags surrounding At War, siege or the affiliation settlement. `ActionOption.Recruitment` groups reserves according to nation/type, giving legal regions of each group; the choice of two deployments continues to validate the entire set.

`RemoveExcess` is a separate decision with the exact number of figures and the permitted ID. The `RemovedIds` preview only describes the known deletion of its own figures, e.g. the commander of FP recruited without an army. It is not used to predict losses after the future throw. Both new types of action and the selection of excesses are recorded in the production GameEngine and the server projection; the pallet client integrates UI-04.

<a id="ruch-i-pozycje-t12"></a>
## Movement and positions T12

`CommandPayload.Moves` contains `ArmyMoveChoice` ( `From` , `To` , `UnitIds` , `CharacterIds` ), a `MovementCost` It's either Army or Character. figures valid throughout action , also when the first movement is followed by two armies joining. `ActionOption.Armies` describes candidates, commanders and targets with eligible ID ; `MovementCosts` binding die with category, movement limit and requirement Leadership We do not generate a list of all subsets.

`ActionPreview.Placements`, `ControlChanges`, `PoliticsChanges` and `Excess` are the known effects of the same planner that Apply uses. The illegal plan does not mutate the state and does not pay the cost. `PlayerView.Characters` contains only standing characters in the board regions; hidden variants and compositions outside board do not leak through this model.

The movement is atomic command; the possible `RemoveExcess` after both movements remains a permanent separate decision. UI-04/T13 connects these contracts to gestures and two clients. The ability of specific characters and independent movement require further tasks.

UI-04 expand `DraftToken` about Army, Region and Reserve. `BoardDraft.Drop` checks the gesture on the draft copy and takes the result only with the correct target. `ArmyDraft` and `RecruitDraft` have only page view and server options; never read full MatchState . `FigureGrid` , `ArmyChoices` , `MusterChoices` and `MapDraftOverlay` are responsible for the presentation, `TurnInteraction` for preview/commit and canceling outdated queries. `MoveFocus` and `ReserveId` are local editing context, not protocol fields command .

T13 verifies these contracts by two mounted Unity clients, the actual server and PostgreSQL. With the loss of response `CommandSender.Pending` stores the original command also when WSS has already delivered a new revision and the opponent's decision. `Check result` takes a receipt of the same ID; the reconnect does not reset the camera. The script seeder exists only in the test project, without extending the HTTP protocol for the command to modify the test status.

<a id="efekty-t14"></a>
## Effects T14

`PlayEvent` accepts `CardId`, `DieId`, `CardCost` and required `NationId`. `ActionOption.Events` binds each card to acceptable pairs die–used score and targets. The unimplemented program is rejected before hand release and at cost. `ChooseEffect.Ids` selects the required set of current step candidates. The only mandatory candidate is executed automatically; the choice of regular/elite is always a decision. `RespondEffect.ReactionId` indicates the server response definition, not any program.

`DecisionView.SourceId`, `Window` and `Presentation` describe the context to the owner. `PlayerView.Effects` contains only visible source, target, owner and condition/time duration. An alien decision, full frames, definitions of private reactions and future selection are not sent. Decline also remains available in an empty private reaction window. The preview does not take steps, does not select or draws.

The `EffectExecution` saves paid-for use of the dice, cursor, page, source, target, window and return mode. The response cost can use card from the table; it does not repay dice from the master. `DiscardContinuation.ResumeEffect` and the removal of excess returns to the frame instead of ending action prematurely. The attempt to re-approval the old decision is rejected; the retries of the same ID recovers the earlier receipt.

UI-06: `ActionOption.EffectChoices` describes candidates by stable ID, the known `TargetId` target, the source of the `SourceId` reaction and the cost of `Cost`. identifier are still validated in the engine; the cards/figures labels come from a public catalogue and own projection. `EffectChoiceStep.TargetId` defines the established recruitment region without stitching the specific cards names in Unity.

`EffectExecution.Committed` saves the choices with its owner. `PlayerView.EffectTrail` only sends the steps chosen by the recipient, without foreign private candidates. The list is in the master frame and survives nesting/reconnect, and when the effect is completed it disappears along with the frame. This is a context of the ongoing decision, not a complete history. All fields are additional fields of the 1 scheme with empty default values for older entries.

<a id="drużyna-i-towarzysze-t15"></a>
## Fellowship and Companion T15

`DeclareFellowship` and `RevealFellowship` Accept `Route` containing the initial region and the subsequent neighbouring regions. ID means to stay; the client does not indicate the target itself, as routes may have other consequences. `HideFellowship` pays `DieId` ; `SelectGuide` indicates `GuideId` . `SeparateCompanions` contains the group `Ids` and common `Route` ; in action also die , when forced separation, the cost comes from the superior procedure. `MoveCompanions.CompanionMoves` includes independent groups ID/routes paid one dice .

`ActionOption.Travel` submit candidates by `Id` , `Origin` , `Range` , `Targets` ; does not calculate all routes or subsets. The group may benefit from the highest level, but validation concerns the entire route declared. `ActionPreview.Fellowship` shows known region/post Noise/hidden, treatment and regions requiring additional caff; do not select a tile. Character deployments use an existing `Placements` . .

UI-08 adds public restrictions on `TravelOption.StopRegions` and `BlockedRegions`, designated by the server. The client submits the route from the map neighbours: does not exceed the range, does not pass through the blockade and ends the journey after entering the Stop region. `Targets` defines the permitted endings, not all the allowed steps – e.g. the sanctuary can pass during the disclosure. The list does not contain hidden route or hidden information about Companion. The validation of the entire command is still part of the server.

Local `HuntChoice` is nullable and starts unanswered; only a clear choice is set by `CommandPayload.HuntResponse`. `TurnDraft` copies independently the route and all groups at Back, Fork and upload. Reconconnect does not place any other cost, Guide or route. Zero route outside Mordor requires an open Stay here, and an empty selection of spreads means consciously retaining the throw at approval.

`PlayerView.OwnCompanions` is a private list of figures and FP statistics. Public `Fellowship.CompanionIdentities` contains identities, non-secret versions of the future Council. `FellowshipRoute`, `Guide` and `CompanionSeparation` only go to the decision owner. New enum values are added at the end, keeping the existing numbers. `ResolutionFrame.Fellowship` remains the server model; it stores return, route and unaccounted for fortresses. Default `LastDeclarationTurn=-1` allows you to read older writing. UI-08 implements gestures of these contracts.

<a id="polowanie-t16"></a>
## Hunt T16

`MoveFellowship.DieId` launches traffic and Hunt runs without a route. `RerollHunt.Ids` indicates zero or more different failed dices from the current throw, to the server limit; empty choice means resignation. New decision `HuntReroll` belongs to Shadows. `ResolveHuntDamage.HuntResponse` allows Ring, Guide or RandomCompanions; the application IDs from the victim is rejected. `ActionOption.HuntResponses`s and existing Minimum/Maximum/EligibleIdss describe legal choices without a number form.

`PlayerView.Hunt` is a public context of ongoing resolution, also with nested response or route. Shows actual rolls/rerolls, bonus and its public sources, selected tile, its separate throw, other injuries, Reveal/Stop, used loss and pool size. It does not contain the order/ID of future tiles, samples of RNG or the anticipated victim. The preview also indicates an unknown result when refusing the reroll if the earlier success continues to result in random selection of the tile.

`ResolutionFrame.Hunt` stores the stage, origin, return and offered guides. `IResolutionContinuation.CanContinue` selects the top frame performer; the engine continues to decide or end the game regardless of the order of the handlers. `EffectRuntime.BeginPublicWindow` checks the public sources and public abilities conditions; does not check the secret hand when deciding whether the window exists at all. `RequiredGuideId` combines the response to the public version of Guide, `RequiredCharacterId` with the live version of the character on board. Baseability implements T20; the other cards programs belong to T21.

The recording further distinguishes the prepared, selected, rejected and permanently removed tile. die paying motion remains Resolving during all choices, and to Hunt passes once after the end of the entire action. The server uses one injected effect directory for validation, reaction and options. Tests replace catalog/RNG in the test host, without test endpoints or production switches.

<a id="mordor-i-zakończenie-t17"></a>
## Mordor and T17 End

UI-34-MORDOR adds public `FellowshipView.MordorAttemptedThisTurn` (`bool?`). On track, the server compares the saved `LastMordorAttemptTurn` with the current turn: true includes a movement attempt (also Stop) and a covert, also a legal effect of card. The input itself gives false. Beyond the track, the value is null. The absent field in the older JSON means unknown information, not false; the client does not request a report. The field is identical to both sides and does not change the format of domain writing, commands or randomness.

`EnterMordor` is a cost-free command phase Fellowship. After the entry `Fellowship.RegionId=mordor-track`, `MordorStep=0`; the tag is not a graph region. `MoveFellowship` retains the same cost and type command, but the server selects the track procedure. `HuntView.Mordor` and `EyeDamage` explain the lack of throw and the Eye value resulting from the Hunt box.

`FellowshipPreview.MordorStep` shows the current step when you try to move, and `HasUnknownResult=true` remains It's a match./settlements tiles. We do not announce the step of 5 or win before the result. At the input, the known step is 0; the hidden step is maintained. The forced separation on the track has `Ids` and the empty `Route`; the preview shows `RemovedIds`, without fictitious locations on the map. The usual action Character does not share this bypass.

`PlayerView.Victory` / `MatchState.Victory` to nullable `VictoryCondition`: RingCorrupted, RingDestroyed, ShadowMilitary, FreePeoplesMilitary. Once the result is read to both sides; the finished match does not have new opcji/decyzji. `LastMordorAttemptTurn` also saves the stopped attempt to stop the reboot from counting inactivity. These are additional fields with neutral values in the older 1 chart; UI-08 plugs the track and the result message.

<a id="bitwa-t18"></a>
## Battle T18

`AttackArmy` uses exactly one position `Moves` and `MovementCost`   Army/Character. The selected group remains in `From` to the winning progress. `CommitCombatCard.CardId` is ID either by hand or by an empty sign meaning resignation; null and unimplemented programs are rejected. `RerollBattle.Ids` points from zero to maximum server ID failed dice.  `TakeBattleLosses.Losses` is an orderly list `{ UnitId, Kind: Remove|Downgrade }`; preview provides deleted figure (also commanders) and regulars entering the elite. `ContinueBattle` / `StopBattle`,  `HoldBattle` / `RetreatBattle.RegionId` and `AdvanceBattle.Ids` close the round and battle An empty progress list means to stay. Every decision still requires the right side, revision and ID.

`BattleView` contains public forces, positions, participants, strength, Leadership, phase/Round, throws and pending losses. `CardBack` reverse is public after first selection, `CardId` only owns up to the common `CardsRevealed`. This includes `PlayerSummary.VisibleCombatCard` and history. The selection of the `OwnRerollSelection` reroll returns to the owner only; new results appear both sides after the second selection. The public dice have a separate natural score, modifier, threshold, hit and metastatic result. Specific attacks are distinct from the fight throw.

Full `BattleState` and `BattleLossProcedure` belong to the server only. Snapshot stores the queue of programs/initiatives, cursor, cancellations, dedicated to the Leadership points, secret approvals and return to action or effect. The contract models are compatible with .NET Standard 2.1; the enum values are added at the end. The compatibility of the recipt and allowed commands on the update of the old release verifies T37. T19 develops this fort contract; the target battle tray provides UI-07.

<a id="twierdze-t19"></a>
## Fortress T19

`BattleView.Mode` and `ArmyTargetOption.BattleMode` have public values Field, Siege, Sortie, Relief. `ArmyOption.Garrison` distinguishes the group in the middle from the field army; an attack with `From == To` means a storm or exit designated by the server from the participants and location. The usual `MoveArmies` still does not allow to remove the garrison.

`StrongholdDefense` provides `FightFieldBattle` and `RetreatIntoSiege`, both without payoad. Between the rounds, shelter may be one of the options of the existing `BattleRetreat`; `HoldBattle` selects the fight in the field without second confirmation. `DefenderSheltered` means temporary withdrawal of the defender: with `BattleAdvance`, the attacker selects the progress or empty list, and only blank progress enforces the garrison limit. Preview gives an excess with `ArmyExcess.Garrison=true` and an excess after the merger saves.

`SiegeContinuation` belongs to the attacker and makes available `ExtendSiege.Ids` (exactly one participating elite) or `StopBattle`. The preview shows a specific removed elite, entering regular or no replacement and possible loss of character. The recording stores a new composition and round number; the retra does not reduce another figure and does not use another dice action. The absence of participating elite automatically ends the assault. The capture of Stronghold in the storm occurs without fictional progress when the remaining army in the region fulfils the control rule.

<a id="prezentacja-bitwy-ui-07"></a>
## battle UI-07 Presentation

`BattleForceView.Units/Characters` store the public lineup of living participants at the time of the projection, without rear guard. `LastBattleRound` is an optional public report of the last revealed round. Written at the closing of the round before the effects expire; it contains both discovered card, first throw, reroll, special attacks and then lineup. It does not contain a private list of selected reroll. Undisclosed choices do not overwrite the report. The report is copied at the projection, limited to one entry and played after the recording; it does not give the client a reference to the model stored on the server. The absence of a field in the older record means no report, not repeated draw.

The client copies the `Losses` deeply with `RegionId` when sending, withdrawing and moving the draft. Selecting an empty card or empty progress requires an open button. The local result of `Ready` only starts the authoritative preview and does not approve or replace the rules.

<a id="postacie-t20"></a>
## Characters T20

`EnterCharacter` Accepts the target `CharacterId` definitions, `RegionId` and `DieId` . `UseCharacterAbility` Accepts ID figure , `AbilityId` , die and regions in `Ids` . `CharacterActionOption` only available legal places/costs and short title and description. The change does not change ID figure ; the client label is due to the current `DefinitionId` .

`MoveShadowCharacters` uses the `CompanionMoves` list as self-moving groups. The flight group has a `[origin, destination]` route, also a `[origin, origin]` to stay; foot traffic Mouth retains further edges. `TravelOption.Flight` does not require a fictional range. `SmallGroupRange/SmallGroupPartners` describes the Shadowfax exception, and `RequiredGroup` blocks changes in the composition of the already required Hobbit group.

`MovementCostOption.AbilityId` and `EventCostOption.AbilityId` indicate optional use of Messenger; the client uploads them to the same paid command. The server saves the number of used turn. The character preview includes placement, regular returns, activation/shifting policy and excess recruitment.

`EffectChoiceView.Title/Detail` describes the ability without pretending to be card events. Windows `battle.start`, `battle.round.closed`, `event.completed` and `hunt.guide` can use the character response. `BattleView.WhiteRiderActive`, `BattleForceView.CaptainBonus` and `HuntView.GuideReturns` explain the public state; they do not reveal the hand or future draws. Hobbit group data and the response tag offered are part of the server snapshot. Additional fields have default values; historical transcript migration and release protocol describes T37.

<a id="paczka-t21-fp-char-01"></a>
## Pack T21-FP-CHAR-01

`EffectChoiceView.SelectionGroup` binds candidates requiring one choice from each group. `ChooseEffect.Payload.Ids` contains the entire set; the server checks the number, uniqueity, membership and groups in the program step. Mighty Attack has groups Companion/Miss and one commit, without separate command payments. Other steps retain a single selection. Both groups and accurate ID dice play after reboot; local Back only undos unapproved draft.

`hunt.replace` precedes injury determination and `hunt.table`. `HuntView.DamagePending` distinguishes the unset injury from the actual zero; Shelob does not have `TileDie` yet. The selection page is part of the server procedure, not the client's declaration. The tile set aside by Mithril and the tiles used in action have separate server locations, not sent as pool content.

`BattleDieView.ConvertedHit` explains the box's conversion by card, keeping the raw score. `BattleForceView.SpentHits` explains the difference between the hits on dice and the sum of losses after Blade. The information is public after both cards. [Rules and sources](rules/T21-FP-CHAR-01.md).

<a id="paczka-t21-fp-char-02"></a>
## Pack T21-FP-CHAR-02

Borders `ChooseEffect` are now dependent on the current step. `EffectChoiceView.GroupMaximum` specifies the maximum number of selections in the group (default 1 ), `RequiresAny` requires at least one ID from the list given in the same set, and `Exclusive` describes the alternative. This presentation of legal dependencies; the server still validates the rule itself. Heroic Death sends one ID figure And one ID hit options or omission. Andúril sends specific ID one/dw Missed dice Or legal behavior of missing. figure removes the illegal number of hits from the draft, without choosing a replacement.

`hunt.draw` opens before any draw and the tile is available. `TileId` is empty, `DamagePending=true`; Staff can complete this selection. A new window in front of the Mithril replacement tile does not exist. `FellowshipProcedure.SeparationBonus` captures the additional range of card I Will Go Alone; the same boundaries go to `TravelOption` and validation of the path.

The selection steps describe the known removal and unknown traffic result by hook preview of the command support. The preview does not induce randomness or `Apply`. The opponent still does not see local selection, and approved decisions and restarts use the same continuation stack. [Logic and sources](rules/T21-FP-CHAR-02.md).

<a id="paczka-t21-fp-char-03"></a>
## Pack T21-FP-CHAR-03

`ActionOption.MaximumGroups` limits the number of travel groups; zero retains the usual range of command, one means the movement of one event group. In `CompanionSeparation` options may include `SeparateCompanions` and `MoveCompanions` if card allows you to choose either of these methods. Both command are already paid for and do not require another dice. `TravelOption.StopRegions` includes the allowed besieged target; `BlockedRegions` continues to block the exit from the besieged start.

`DecisionView.Presentation=Region` choose ID region from `ChooseEffect.EligibleIds` on the map. `ActionDie` chooses in the same command ID available dice clicked in the pool; does not use the payment box `DieId` Answers only contain the decision-owner's options. During the event FP the owner of another window may become Shadow , as with the withdrawal after Eagles. Property approved `EffectTrail` is separate from the event; this is confirmed by the Gandalf window test after the event paid for.

<a id="paczka-t21-fp-char-04"></a>
## Pack T21-FP-CHAR-04

`TakeEventLosses` uses existing `Payload.Losses` and the decision `Losses`. Public `PlayerView.EventLoss` contains owner, region/garrison, ID attendees, source, hits and throws. `Battle` remains empty. The options figures and the possibility of approval is given only to the owner of losses. The rule and UI degradations are shared with losses battle; the preview takes into account the commanders and possible acquisition of Orthanc.

`Reaction/ents.follow-up` allows `PlayEvent` and `DeclineReaction`; legal `EventPlayOption` contain one cost with an empty `DieId`, without `AbilityId`, with `Face=Character`. The server accepts this cost only in the saved window, not in the usual action. The window exists independently of the hand contents. Sending any dice, card Strategy or failure to meet the event condition is a mistake.

`EffectChoiceView.GroupMinimum` (default 1) allows you to describe an optional group. `CompanionDestination` presentation combines one map target with 0–6 additional Companion in one command `ChooseEffect.Ids`; mandatory Strider/Aragorn is set from the current server state. Separate ID do not allow to join the army. When recruiting Dead Men, the `recruits` group has a minimum 0 and a maximum 3; the empty selection is the actual decision. The three figure reserves are interchangeable copies of the same type.

<a id="paczka-t21-fp-str-01"></a>
## Pack T21-FP-STR-01

`DiscardProtection` Allowed in action Shadow : `Payload.CardId` indicates one source of protection on the table FP , `DieId` one available die SH , `Ids` exactly two different card SH from the hand (Army and Character type). The server sets the types from the directory; the strategy deck itself is not enough. Private `ActionOption.Targets` contains card for disposal, `EligibleDice` dice , `EligibleIds` card cost, `EffectChoices.SelectionGroup` Army/Character. Field `ActionPreview.Costs` contains die and both card only for an eligible client. Public history does not state the identity of the rejected cards .

`ActiveEffectView.Modifier` describes the public presentation of the effect. `PreventShadowArmyEntry` with `TargetId` indicates the protected region; the client does not calculate the legality of the movement. `PreventFellowshipHuntDie` describes Last Battle. The list can have three effects of one source; UI shows one card and three regions.

`ActionOption.AllowEmptySelection` (default false) Allows empty `CompanionMoves` for optional Book movement. Fixed `CompanionJourney` has `AllowSeparation` (default true), `MaximumGroups` (default 1 , 0 means no group limit), `AllowNoMovement` (default false). Book only offers MoveCompanions ; the client cannot smuggle separation, movement figure twice or entering siege .

The Scouts `RegionOrShelter` presentation uses `ChooseEffect.Ids` with one region or `scouts.shelter`. The server offers shelter only in its legal own Stronghold. After the decision, you can get RemoveExcess, a round closure response or BattleAdvance. The choice does not perform battle throws. `EventLossProcedure.Cause` is only the state of the server; the client still sends the same typed losses.

<a id="paczka-t21-fp-str-02"></a>
## Pack T21-FP-STR-02

`DecisionKind.ArmyEvent` is added at the end of the enumu. In MoveArmies events `Moves` has exactly one group, `Route` contains further regions without beginning, and `Moves[0].To` It must be the last step. `DieId` and `AbilityId` are empty. Help shares AttackArmy with one group and empty Route. Trying to use another dice , other command or an illegal group is discarded before mutation and RNG .

The optional `ActionOption.ArmyEvent` includes `MaximumSteps`, `RequiresCompanion`, `AllowLeaderLoss`. The last exception is valid only for `ArmyOption.Garrison`; the remaining armies still cannot leave the commander alone. `ArmyTargetOption.Route` gives a complete short route, and `EligibleIds` figure is entitled throughout the route. The usual action leaves ArmyEvent null and Route empty. `MovementPlan.Describe` returns target deployments, control also along the way, policy, losses left behind commanders and final surplus.

The server saves `EffectWait.Army` and `EffectExecution.ArmyOrder`, does not accept the exception type from the client. After the move, it resumes the event; after Help battle has `Return=Parent`. `BattleForce.CombatDiceReduction` and public `BattleForceView.CombatDiceReduction` are valid for all rounds, after the normal dice limit. The older record has neutral zero/null. Confusion uses existing `CannotReroll` and ExtraHits; it does not hide the result of the natural one or settles losses from the commanders' throw.

T21-FP-STR-03: recruiting multiple figures uses the same `ChooseEffect.Ids`, the `Recruits` group and the exact Minimum/Maximum value, reduced only by the actual lack of reserves. Each figure has known `TargetId` in preview and options. The selection of the region has Presentation=Region and the selection of the ReserveFigures recruits. The server remembers the selected region in the effect frame; the client cannot change it with the command field of recruits. The board directory adds public `RegionDefinition.Coastal`; the recording of the match does not store the duplicate of this feature.

T21-FP-STR-04: Joint recruitment in different regions preserves `ChooseEffect.Ids`; `SelectionGroup` means the target region of an independent reserve and the group limits must be met separately. All selected figure have its own known `TargetId` and go to one preview/commitu. Empty group does not generate an artificial selection. No new DTO or new decision window; write using the cursor and effect frame program.

T21-SH-CHAR-01 : public `HuntView.CorruptionOnly` explains why there is no choice protection/casualties ; does not reveal the pool. `Stop` means the running motion test stop, so the motion is false at the event without movement. `HuntTileRule/HuntDamageRule` have neutral default values All/Normal. Condition of the new additional Stronghold He's not inheriting the injury ban. `ChooseEffect` with Presentation= NazgulLeadership contains two different ID points; `EffectChoiceView.SourceId` indicates figure , a `SelectionGroup=Leadership` has Minimum=Maximum= 2 Both points can come from Witch-king; the client does not send amounts or rule names. `TargetId` it still means a spatial target where appropriate, and it's empty for those points.

T21-SH-CHAR-02 expand NazgulLeadership per solid one/two or variable cost: `ActionOption.Minimum/Maximum` and `EffectChoiceView.GroupMinimum/GroupMaximum` determine the permissible number of points. UI does not propose legality for card Added at the end of the enum `DecisionKind.CharacterEvent` He's got a presentation. PortraitRoute and command `MoveShadowCharacters` , existing payoad `CompanionMoves` , empty EligibleDice and AllowEmptySelection =true. Empty list means ending motion without moving; not to send DieId Only this box allows the Nazgûli movement from card ; ordinary action and Mouth/Saruman they shall not undergo validation. `NazgulStrike` selects one ID public card Character from the table, `nazgul-strike.hunt` or `nazgul-strike.decline` . `LeaderOrCompanion` selects a live participant or `black-breath.decline` Cruel Weather uses the region and a full `FellowshipPreview` by keeping progres/ukrycie.

T21-SH-CHAR-03 uses `ChooseEffect` and existing selection limits for Lidless ( 0 .. 3 own available dice ) and Breaking (exact number Companion Presentation `FellowshipCompanions` contains a clear purpose of placement, also used by preview figures on the map. `CharacterCardDiscard` contains public card from the table and the random option from the hand; the existence of a random option depends only on the public size of the hand. No Character in the private hand ends such a choice without recoil and without drawing. ID rejected card gets exclusively FP . `HuntView.SeparatesCompanions` changes the meaning of the number of tiles in the context of UI ; does not grant the client the right to calculate the result.

`BattleForceView.CancelledAbilities` shows the open-ended effects of Words in the current round. `BattleView.OwnTableCombatCards` gives the owner additional card from table available in this battle , also after its own secret approval. The common numbers include the available Balroga without cheating from which place it was selected card ; card Virtually available for combat does not become card hand for costs or limits. Reactions `hunt.roll` , `hunt.casualty` and `fellowship.declared` use the existing serialized window `Reaction` , with one approval, permanent identifier decisions and simple retras.

The domain save is extended additively by `HuntProcedure.Rolled` (default false), the end value of `HuntDamageRule.RandomCompanionFirst` and `EffectWait.NazgulTravel`. The older movement save is recognized by Movement; no saved gameplay is required. The reboot stores the owner of each window, program cursor, throw, metastatic sources and points dedicated. Do not send a pool of tiles, future results or draft opponent.

The spatial choice of Cruel Weather has `EffectChoiceView.SourceId=fellowship` and TargetId region. The client displays with these metadata the planned location and arrow; the authoritative tag remains in place to confirm. The selection of the region and Undo do not change the public state or calculate the local effects of the rules.

Public `BattleForceView.ForfeitedLeadership` already provides paid points per figure both sides. By default, the empty dictionary; the projection copy it from the round, and the previous round report retains the status after the decision. It does not contain the current local draft. Tooltip force describes the cost at the name figure; the server still counts the available Leadership itself.

T21-SH-CHAR-04 : `ArmyOption.RequiredAnyIds` requires at least one indicated figure in a really selected group. `ArmyEventOption.RouteMovement` distinguishes the route of the event from the manoeuvre, and `ActionOption.Maximum` It's a limited army. `FinishArmyEvent` is only available in the optional manoeuvre Shadow . `DiscardPalantir` indicates `DieId` and optional `RingId` ; `DiceWithoutRing` contains only dice Will of the West. The whole cost is atomic, the Ring goes to Shadow and uses a limit turn . `BattleView.MandatorySiegeRounds` and `CompanionRequiredForCard` are public information. Source/Dreadful target have unique ID Army and separate `TargetId` region; presentations of Deck and CaptainOrder use normal `ChooseEffect` The personal selection of Jerkír does not go into the history of the opponent. New enum values are added at the end; older entries receive neutral values of new fields and an empty line of politics.

T21-SH-STR-01 : Added `CommandKind.DiscardDenethor` requires available `DieId` specified in the option, without the cost of the Ring. `BattleView.DenethorBlocksCards` describes the public prohibition, and `VoluntaryLosses` distinguishes the optional victim from the mandatory hits. `TakeBattleLosses` allows an empty list; `ActionOption.Minimum=0` , Maximum is the hit limit, not the number figures The server calculates the cost Remove/Downgrade from current participants. `DecisionView.Presentation=ArmyFigure` use `EffectChoiceView.TargetId` as a region figure ; the selection of the region is a local filter, `ChooseEffect.Ids` contains the exact selected figure Valinor preserves the regions served in the effect frame and does not draw again after retry. Additive write fields `Voluntary` , `AdditionalAttack` and `AppliedHits` have neutral default values. `AdditionalAttack` distinguishes Onslaught's throw from his voluntary sacrifice from his usual continuation battle Relativeless; is not a parameter command Client.


T21-SH-STR-02: `ArmyMoveChoice.Route` contains a specific army route, and `ArmyEventOption.SeparateRoutes` indicates that each element of `Moves` has its own route. The single marches have been maintained by `CommandPayload.Route`; the server rejects mixing both formats and routes in normal traffic. `Minimum=1` does not guarantee the possibility of ending Lengthens after the first march: the preview returns the safe `MoreArmyMovesRequired` if it remains the legal move of another primary army. This does not reveal hidden data. `BattleView.ArrivalBeforeBattle` is a public fact of the earlier landing; it is not a trusted parameter of command. The battle saves the same tag and returns to the paid event.

`ArmyFigures` can combine the choice of figures from several regions. `EffectChoiceView.SourceId` means source region if the selection moves figure and `TargetId` the destination; without SourceId the previous TargetId remains figure region. The selection group contains the public minimum/maximum and the client retains the selections when changing the region.

T21-SH-STR-03/04: `DecisionView.Presentation=RecruitmentPlan` uses `ChooseEffect.Placements` as real lists `UnitId`/`RegionId`; `Ids` must remain empty. `ActionOption.EventRecruitment` contains `MaximumRegions` and `Limits` with nation, region, allowed types and maximum. Public metadata describes legal options, but the server re-checks the entire plan, current reserves, uniqueity of figures and mandatory maximum. The recruits, the siege page and the amounts are not accepted from command. The approved elections go to the existing history of `EffectSelection`; the reboot and snapshot cover the entire atomic plan and the next excess window.

<a id="t24--specjalne-kości-lords"></a>
## T24 — Special dice Lords

`ChooseSpecialDie.DieId` corresponds to the decision `SpecialDieChoice` Phase ActionRoll . `EligibleDice` contains only permitted dice (same Eye, if any); minimum and maximum is one. Preview does not treat the selection as payment. `AwaitingChoice` is a public place of the outcome awaiting selection, `Discarded` means the unelected result to the next Recovery. `DieView.RemoveSymbol` shows the print, not immediate removal. Result/printing dice Unrolled/Removed is not designed. `CardDraw` and `UseBalrog` are separate walls; CardDraw not paying PlayEvent . `HuntOrigin=keeper-eye/lesser-eye` retains its origin with the unchanged Owner; `keeper-eye` counts as a force Shadow , not the earlier move FP .

In the `MatchState.Lords` record, it stores the number of actually dumped dice and the election queue, and `DieState.Special` choice, printing and permanent removal. Both fields are optional and omitted for the database. [Specification T24](rules/T24-LORDS-DICE-RINGS.md) sets the time of recovery, input and the T25–T27/UI-09. integration limit

`UseNarya` contains specific `RingId`,  `DieId` and `NationId` according to settlement Gandalf Keeper. Options calculates the server, and preview shows the cost and policy change. `KeepActionDie`  /  `KeepDieOrDecline`  /  `action.completed` It's a separate decision. FP All over. action:  `UseVilya` indicates just paid die And the Ring of Vily, `DeclineVilya` ends the normal settlement. Die remains Resolving for approval; no other available dice The preview of the stop only charges the Ring. `Lords.ActionCompletion.FellowshipMove` determines the normal place of cost, and `PaidWillOfWestIds` retains the original score before standardizing Will to the category action After completion, this data is cleaned. Restart does not return to execution action.

Nenya uses `RespondEffect.ReactionId=lords.nenya.replace` in `hunt.replace`. The option has `Cost=TransferNenya`, its own title and description; preview indicates the Ring and unknown exchange result. Galadriel, the property of the Ring and the standard Eye are public window terms. The selected exchange saves transfer, permanent removal of the tile and single selection in one transaction. The new tile can open another legal exchange window: the remaining ability of the player chooses himself. The state of the pool, samples of RNG, the record of continuation and other options do not appear in the projection.

`DieView.SpecialDieChosen` remains a public choice trail also after the release or rejection of dice; `Discarded` itself is not sufficient because it may affect both situations. `DieView.PermanentlyRemoved` distinguishes the permanent removal of the special dice from the source/condition of Narya. Recovery will zero the selection/print, while maintaining a permanent removal. The client does not reproduce this information from a limited event history. The base dice have both false tags; both sides see the same public values.

<a id="t25--postacie-i-decyzje-lords"></a>
## T25 — Lords characters and decisions

`ChooseCompanionVersions` corresponds to the private decision of `CouncilVersions` during the setup, also when the Council is disabled. `Ids` contains the selected version of Grey, `GuideId` stable ID initial Guide. Selection precedes the first Recovery. T26 extends the same procedure to the other versions. `Fellowship.RevealedCompanionVersions` contains only versions already public; `OwnCompanionVersions` remains private. `CharacterState.Id` retains the figure during the conversion and conversion of the version, `DefinitionId` specifies the statistics.

`EnterCharacter.Placements` allows an optional one of Balrog's recruits. The option `CharacterActionOption.Recruitment` indicates the actual reserve and target; it is part of one preview and command. `UseCharacterAbility` supports activate/deactivate Balrog and gothmog-recruit/upgrade. `MovementCostOption.RequiredCharacterId` binds Use Balrog and Great Sorcery to one particular army. The standalone Use Balrog only allows Balrog; none of these costs entitles you to simply play the event.

`UseEvilKnowledge` is a separate option of `SiegeContinuation`, next to Elite and Stop costs. `Lords.EvilKnowledgeUsed` lasts throughout action, `LastArmyConversionTurn` through turn and `Lords.WitchKingReplaced` through the batch. The meters behave after reboot and retra.

The Chief uses `RespondEffect`: `lords.chief.spear-of-terror` in `event.played`, `lords.chief.he-sees-he-knows` in `fellowship.declared`. These are optional reactions with a public source, title and description. Spear saves a single private selection and removes Jerkír's second selection for this card. Follow shows the target on the map and has no cost of dice. Both responses can be rejected.

Fire and Shadow saves the pending Hunt in the location procedure Fellowship, then a separate Hunt with `FireAndShadow=true`. Eliminates Guide/Balroga only after Eye exchange windows and returns by simply choosing the Guide / Hobbit return to the overarching declaration or disclosure. It does not draw again on playback. New flags omitted at false values retain the historical hashe of the base body.

Sméagol has a permanent `SmeagolLeft` / `GollumEntered`; its level in `OwnCompanions` calculates the server from the current configuration without mutating the snapshot. `PlayerSummary.TableCards` transmits a clear table of both sides, including We Shall Get It. It does not cover the hand or deck. Sméagol's departure in preview is the removal, never setting the figurine on the map. Full rules, sources, interactions and boundaries T27/UI-09: [contract T25](rules/T25-LORDS-CHARACTERS.md).

<a id="t26--council-wersje-ojczyzny-i-tokeny"></a>
## T26 — Council: versions, homeland and tokens

`ActionOption.CompanionVersions` groupes the base and alternative version after stable ID figure; `ChooseCompanionVersions.Ids` requires one version of Grey, the remaining characters retain the base version by default. Each figure has a maximum of one entry. Alternatives of the six remaining characters require the Council. The whole is private FP.

`ChooseCouncilHomelands.Placements` contains `UnitId` (ID figure Companion) and `RegionId`; an empty list means to leave everyone in Fellowship. The private option provides legal characters/places, without a combination of subsets. The opponent sees only approved departures. `ChooseCouncilToken.Ids` is a separate choice of SH from one token after exactly one departure. Both new decisions have separate presentations of `PortraitHomelands` / `ShadowTokenChoice`.

`UseCouncilToken.AbilityId` defines the token; `NationId` or `CompanionMoves` determines its effect. Cost `DieId` / `RingId` It's empty. `PlayerView.CouncilTokens` publicly communicate the tokens received, `Spent` and `UsedTokenThisTurn`; the client does not list them from history. Tokens do not appear in any list dice.  `Lords.Council` saves the choice of homelands, awarded/zu rye tokens and the last turn use. Details of the rules and the limit of the further scope: [T26](rules/T26-COUNCIL.md).

Further T26: `DieLocation.FellowshipBox` distinguishes die Stridera from Hunt and the usual issued dice. `Lords.Council.LastFellowshipDieTurn` and `FellowshipBoxDieId` binds its settlement to the first move; `BattleReturns` keeps the pending Hobbits back. The return uses the existing `EffectChoice`/`ChooseEffect` with location preview, retaining the owner and the previous decision. The private version of the second Hobbit filteres its own options, but not the existence of a public window. `EventCondition.CanOpenPublicWindow` serves only the conditions known to both parties; `CanPlay` can additionally check the owner's secret.

T32-GIMLI : `CompanionVersionOption.FigureId` remains `base.character.gimli` but `BaseId` At Fate is `fate.character.gimli` , a `AlternateId` `fate.character.gimli-dwarven-lord` . `Ids` chooses the version, no figure The omission of an optional choice gives the usual obverses of Fate. All command concerning Guide/movement/homelands still indicate stable figure The server rejects the old obverses in Fate configuration and an alternative outside the Council. Two homelands come in the existing `CharacterActions.Targets` ; their choice is private for approval. `fate.gimli-guide` uses existing `RespondEffect` , `SelectGuide` common `SeparateCompanions` . Details: [both versions of Gimli](rules/T32-FATE-GIMLI.md) .

<a id="t27-cards-01--prywatna-ręka-i-odzyskiwanie-pierścieni"></a>
## T27-CARDS-01 — Private Hand and Ring Recovery

[Full rules](rules/T27-LORDS-01.md). `CardCatalog.Base` and `EffectCatalog.Base` retain the 96 cards set; `All` connects modules without depending on the order of the files. Access to card checks configuration, property and registered program. Metadata 14 cards Lords do not mean implementation other packages.

Wind uses `ChooseEffect`: First card table or `wind.inspect-hand` / `wind.finish`, then mandatory discard of a particular card. Only in the second window `EffectExecution.InspectedHand` gives the performer the right to list the averses by `EffectChoices`; `Presentation=InspectedHand`. The opponent does not get a list of options. The other card does not get into history or a trailer. Status and permission exist for approval; restart does not extend access.

`RingRecoveryState` captures three results and number of successes. `ElvenRings` uses the existing `ChooseEffect.Ids` and groups `Out of play` / `Held by Shadow` with exact limits. The server rechecks the origin order of the Rings. Rejects duplicates, excesses and omits the required play.

`BattleRoundState.OrderlyWithdrawal` saves the consequence of voluntary cancellation of a hit. With the mandatory backend there is no `HoldBattle` in command or options, and the validator also refuses this command. The target still selects FP on the map. The field has default falses ignored in old records; new effect fields are omitted when null, so the base body remains compatible.

<a id="t27-cards-02--reakcje-koszt-kości-i-trwałe-karty-cienia"></a>
## T27-CARDS-02 — reactions, cost of dice and durable card Shadow

[Rules and sources](rules/T27-LORDS-02.md). `ChooseEffect` with `DieAndResult` presentation includes `Die` and `Result` groups; `RequiresAny` combines the score with a specific legal dice. `lore.keep-results` is an exclusive refusal. One command approves two ID, no charge. `LoreOfferedTurn` and the continuation of `BeginActions` prevent re-offering. Keeper must show Eye in this throw; assigned to Hunt dice are not candidates. The physical results of Lesser Minion retain a link to the Remove symbol.

`SmeagolCorruption` is a decision of Shadow against Hunt protection, also after the exchange of the tile by Mithril/Nenya. Hand selection is private; the public history does not give it ID. The effect can end the game before further response. `CaptainDice` records only later used Character, and `CompleteCost` transfers it to Hunt throughout action with origin `captain-of-despair` for Kings. The dice kick does not mean use it. The table conditions are rechecked after decisions.

The new `DiscardLore` and `DiscardGates` are ordinary action FP, with `DieId` and optional `RingId` only for Lore. `DiceWithoutRing` distinguishes Will from the other results. `TableRingCost` shares the cost with the base Jerkír; the transfer of the Ring is its use in this turn. The server rejects the wrong result itself, Other/used Ring and the lack of card Table.

In Lords, the `OfferedReactions` recording of the location window removes the executed response, and after it is completed, it reopens only the remaining legal options. Gates and Chief can work in any order; the refusal ends the window. The base reactions and the format of frozen database records have not been changed.

`DoubleSixes` in the state of the round means additional hits of natural sixes Combat/Leader re-roll; does not include special attacks. New round cleans them with costs Leadership . Servant sacrifices Leadership one selected figure, after a five-lime dice FP Balrog has a separate resuming of the journey, and both maneuvers cards The army is returning to the paid event.

`MatchFactory.Create` accepts a copy of configuration before shuffling. `CardCatalog.LordsDeck` consists of 108 cards: 28/26 FP and 28/26 SH; the base Balrog 17 is replaced, the special card Sméagola remains out of the deck. gate room start still requires completion of the remaining Lords; the seed test is a separate program, without endpoint and without server production flag.
<a id="karty-lords--paczka-t27-cards-03"></a>
## card Lords — T27-CARDS-03 package

`EffectExecution.MadnessTarget` is an optional record for the target region and the original siege. We do not set this branch again after `TakeEventLosses`. Losses end siege according to the base, and the parent resumes recruitment without repeating the throw. The selection of the region shows the public `Placements` two regulars in the preview; the physically equivalent figures do not create an additional window.

to `HuntStage` added `ShadowReplacement`; existing values retain numbers. In Lords order is selection/Sméagol →   `hunt.shadow-replace`   Shadow   →   `hunt.replace`   FP   → determination of injuries →   ochrona/Guide. Frame ownership, refusal, table discard and retras use the existing protocol `RespondEffect` The first tile remains `return-after-action` and the view shows only the current tile and `DamagePending`, without future results. card In Shall Get It doesn't go to `Events` / `Combats` or to the waist; the directory records only its reaction. Details: [rules](rules/T27-LORDS-03.md).

<a id="t28--stan-suwerenów-i-prywatny-dobór"></a>
## T28 — State of sovereigns and private selection

`PlayerView.Sovereigns` describes five identities with status Initial/Awakened/ Corrupted/Removed , region, resilience and tiles. Values and physical ID covered tiles only receive SH ; FP gets anonymous identifier the position of the reverse. The openly Kings tile has a zero value in the corruption. `CorruptionAttempt` make the selection counter available to both parties, and `OwnChoices` only Shadow . . `AwakenSovereign` Accepts CharacterId/DieId/Route ; `RemoveCorruptedSovereign` CharacterId/DieId. `AttemptCorruption` indicate the sovereign and entitled Eye; `DrawCorruptionTile` saves the next selection, `ChooseCorruptionTile` one physical tile with private options SH Authorization, revision, legality and RNG remain on the server. [Sources, conditions and continuations](rules/T28-SOVEREIGNS.md) . .

Policy changes can open `ChooseEffect` presentation `CorruptionRemoval` for FP Even in action   SH.  `remove` randomly returns one covered tile, `decline` leaves state; neither choice nor history reveals value. The cost actor and previous decision is preserved. `CombatReplacement` I'm giving Brand `replace` / `keep` After the reveal. `ExtendSiege` may indicate Regular solely with the openly eligible weakness costs; the preview shows removal, not downgrade. `PlayerView.RegionOverrides` announces public change to Helm’s Deep to Isengard/Shadow at Corrupted Théoden; victory continues to benefit from its original affiliation settlement The client does not have to guess the activity of this change on his own.

T32-SETUP extends `RegionOverrides` to the metadata Fate/New Cities. Each view contains a full current set of overwrites with respect to `base-setup-1`, not incremental diff of the previous revision. No region in the next set restores its base definition; exit from the match cleans the cover. `Nation=null` and `OriginalOwner=null` mean neutral Dale. Names/graf remains base and client dictionary and geometry retain identity. Projection copy objects and Neighbours lists; modification of DTO does not change the server directory. The scoring does not use dynamic White Hand but from the original owner of the configured script. Details: [Fate setup](rules/T32-FATE-SETUP.md).

`CharacterActionOption.RulerRecruitment` describes the character region, the permission of the local Elite, the number of sediments and the requirement of another place. `Recruitment` includes groups available figures and targets, and command `UseCharacterAbility.Placements` complete approved plan. Preview shows all figure and the surplus garrison. We do not send a combination of physically equivalent recruits.

`ActionOption.RulerTravel` specify the eligible dice , `AbilityId` , additional coverage with appropriate travel options or reduction of number dice Hunt . `MoveFellowship` , `MoveCompanions` and `SeparateCompanions` use capacity only after the explicit choice of this `AbilityId` empty identifier it's a regular mode. `HuntProcedure.DiceReduction` The test shall be carried out on the basis of the following: `EffectExecution.AnarionDrawPending` save optional selection after event; select deck, refusal and limit of the hand use existing decision effects. New status fields omit null/default , maintaining serialization of the base corps.

<a id="t29--ruler-tokeny-i-wodzowie"></a>
## T29 — Ruler, tokens and chiefs

`DiscardRulerDie.DieId` corresponds to a new decision `RulerDiscard` in ActionRoll.  `KingsState.RulerDiscardOrder` fix the queue SH   →   FP Normal Eye SH and Ruler Eye remain `AwaitingChoice` to discard SH; candidates exclude ordinary Eye and dice Lords. Rejected score has `Discarded`, the other Eye `Hunt` with origin `rolled-eye` The Ruler has a source `ruler` and constant ID   `kings.die.fp.ruler`  /  `kings.die.sh.ruler`, without `Special`  Lords.

The input of three chiefs uses `EnterCharacter` and `CharacterActionOption`. The server provides the right regions/blanket and revalidates them in command. The Ruler can pay the entry any available result outside of Eye; this does not change the usual costs of dice. Statistics share a common directory and a Minion role, with the overt exclusion of the White condition. [Rules and verification](rules/T29-RULER-CHIEFTAINS.md).

`CommandPayload.RulerTokenId` binds token to complete command The validation checks the page, the window, the identity sovereign/ko au, availability, conflict Ring and Legality action After changing the result. Preview shows both costs without state mutations. There is no separate command to change the token result. `PlayerView.RulerTokens` is the open state of the components; `RulerTokenActions` contains only for FP Acceptable action and their existing recruitment options, cards or travel. Hand/costs of events do not reach Shadow. Right `AbilityId`, token and selected die remain part of one draft UI-09.

`KingsState` fix the use tokenu/Pier wall in turn and the identity of the ongoing action tokenu. Denethor after the token requires additional selection without refusal option; nested Event, hand limit and selection Guide They don't release the Ruler cost before. action disappears at the end of the settlement, conflict markers only at the beginning of the new turn Keeper and cost Ring removal card the table uses the same lock, keeping its own base limits.

After the commitment of both cards `BeforeReveal` stops the fight on `EffectChoice` Shadow with the presentation of `CombatReplacement`. This is a choice of one current ability or behavior of card. card in both projections remain covered for the opponent. Red Wrath opens separate `Losses` before the return; Mirkwood devotes its own Leadership. `BattleRoundState.ReplacementSource` saves the replacement, and `PlayerView.CombatReplacements` shows only the open source and page, without premature release of cards/conditional with the bonus.

Reveal builds a replacement queue with the right initiatives; `CardId` and the state played are left. Cancellations skip future effects, does not undo the cost. New round removes the replacement along with the remaining state of the round. Brand takes his later choice in the existing window after the reveal. When his own hit removes the entire army, the usual continuation ends with battle, rejects the obliged card and does not throw it.

`HuntProcedure.RerollBonus` fixes the Uglúk bonus set at the throw; `HuntView.RerollBonus` shows it separately from the previous moves bonus. Natural `First/Result` is not overwritten by the value after modification. `Success` only takes this bonus into account with `Rerolled=true`. The list of metastatic sources and their limit remain independent of the bonus.

<a id="t30-cards-01--karty-i-prywatna-inspekcja-kings"></a>
## T30-CARDS-01 — card and Private Kings Inspection

Six cards uses ordinary `PlayEvent` , `CommitCombatCard` and `ChooseEffect` Presentations `SovereignCardBranch` , `Sovereign` and `InspectedCorruption` determine branching, sovereign selection and tile selection. The last window always requires approval, also with one kaflu. `EffectExecution.InspectedSovereign` preserves the right to view; the lack of value does not change the elder JSON Just... FP in the active decision it receives values/ ID tiles of this sovereign. The end of the decision removes permission and local draft with private controls. Shadow retains its own ordinary privileges, does not receive decision/options FP . `keep` is occurring with optional refusal or prohibition of removal; it is not legal with mandatory Dark return.

Riders use `RecruitmentPlan` and `Placements`: one R/E and Leader figurine in one region, with partial execution in the absence of reserves. Aid uses existing `CompanionJourney` without separation, without group limit and with the possibility of missing movement. Both new Combats use `CharacterLeadership` and a special attack of the additional existing permanent window `Losses`. Each decision has a separate revision, preview and idempotence key; no transport based on card was created.

<a id="t30-cards-02--różne-narody-i-próba-korupcji-z-wydarzenia"></a>
## T30-CARDS-02 — different nations and the Corruption test from the event

`EventRecruitmentOption.DistinctNations > 0` means one figure per nation, to the printed limit. `ActionOption.Minimum/Maximum` gives the maximum feasible result after taking into account the reserves and targets. Default zero retains the current quota model. The client controls the entire prefix of the plan, the server independently checks the `Placements` set, types, regions and different nations. One command is approved; the change of the selection refreshes existing controls.

`DiscardWelcome` is added at the end of `CommandKind`; it does not change the older command numbers. The server returns only the legal dice in `EligibleDice`, re-verifies the tho-presence of figures and atomicly releases die/rejects card. Service saves the target in `EffectExecution.TargetIds`, nation in `NationId` and region in `RegionId`; these data do not open access to the tile value.

West uses existing `CorruptionAttempt`, `DrawCorruptionTile` and `ChooseCorruptionTile`. Its frame has an empty `DieId` and `Return=Parent`; the cost of the event remains at the overriding stack. `OwnChoices` is available only Shadow. UI shows up to three places and the tiles buttons from this projection; clicks draft, the joint approval sends command. `BattleRoundState.LeadershipPenalty` reduces the Army's Leadership to a minimum of zero, leaving available points of individual figures as a separate cost. The field equal to zero is omitted in the record, so the old entries remain identical. The new round will cancel the penalty with the remaining round costs.

<a id="t30-cards-03--koszt-konwersji-i-trwały-limit"></a>
## T30-CARDS-03 — conversion cost and permanent limit

`ActionOption.DrawCosts` contains legal `EventCostOption` for selection card . `DieId` and `AbilityId` hit command `DrawCard` ; simple selection has an empty ability. `kings.palantir.muster-as-event` means the conscious use of Jerkír Barad-dûr and is also found in costs `PlayEvent` The server checks the table, page, result and availability dice and limit turn It doesn't change. `DieState.Face` . `KingsState.PalantirUsedTurn` saves use also after rejection and re-play card ; null is omitted in JSON Older states.

`DiscardBaradPalantir` is added at the end of the command enum. It uses `DieId` and optional `RingId` according to `TableRingCost`, regardless of `DiscardPalantir` for Orthanek. `DiscardDenethor` selects the card version according to configuration. `KingsDenethorRules` shares the legality with the cards battle ban projection.

Stormcrow does not create a private selection from one tile. The server captures its draw along with the event effect and continues with the FP `ArmyFigure` window. Valinor uses `Region` and existing `TakeEventLosses`; King extra `ReserveFigures`, and His Power Waxes `Sovereign` when the targets change the effect. They all use the existing continuation and idempotence frames.


### Work of the Enemy — T30-CARDS-04

[Rules](rules/T30-KINGS-04.md). Without new commands or writing fields. `ChooseEffect.Ids` first indicates ID sovereign with a public region in `EffectChoiceView.TargetId`, then one ID Army `region:field` or `region:garrison`, if both occur. The threshold is public, due to sovereign status; it does not depend on covered tiles. `EventLoss` transfers the results of three throws, hits and participants of one army solely with authoritative resolution. FP corresponds to `TakeEventLosses`.

A joint presentation of Unity `Region` shows separate buttons when several legal ID points the same region. Clicking the region opens its choices; it does not automatically select a field or garrison. The local `RegionId` only defines the context of the map, the server still checks the actual `Ids`.


T30: existing `SovereignView.Tiles` and `Corruption` take into account `MatchConfiguration.VisibleCorruption` : values assigned to tiles are public only in this variant or in the source inspection window. Private `OwnChoices` attempts to corrupt remain a choice Shadow ; are not a pool or a public order of future tiles. Source: KINGS p. 18 Full integration of options and notifications is carried out T31/UI-09.

<a id="t31--dismay-sekwencyjna-korupcja-i-council"></a>
## T31 — Dismay, Sequential Corruption and Council

`PlayerView.Configuration` contains a disconnected public copy of the settled party flags. The client uses it for correct variants descriptions also after opening the entry without prior entry to the lobby; he does not guess the flags from the state of figures or the tiles. He does not include private Council choices.

The `Dismay=null` value also in the read snapshot means the default Lords + Kings. The projection and runtime use the common `ConfigurationRules.DismayEnabled`, so omitting the flag does not create the difference between the available rules and the configuration description.

New Decision `Dismay` presentation `DismayRerollOrKeep` belongs to SH After the Lords election and both Kings discards. `UseDismay` indicates one physical token in `Ids` and preserved die Ruler in `DieId` ; die is not at the expense of wear. `DeclineDismay` saves tokens for future turn . `UseLoreBeforeActions` allows SH select the order available Lore and Dismay. No tokens/targets skip the window and save the saved `DismayDeclinedTurn` prevents you from returning to Lore. Each reroll records token, result, sample RNG And a new decision in atomic form. `RulerTokenView.Location=dismay` means the reverse of the same component, `removed` — its final wear. In Removing Ruler Tokens token is only created after the death of Awakened; waking does not leave the token FP .

`CorruptionStage.SequentialChoice` allows `DrawCorruptionTile` or `ChooseCorruptionTile` only with the last numeric tile in `Ids`. Limit and Kings end the sequential attempt automatically. Ignored tile forces replacement selection. `CorruptionAttemptView.Sequential` explains the mode; `OwnChoices` only provides the current legal tile SH. With Visible Corruption, the privacy of selection does not change; public is only assigned to tiles. The texts of options, previews, history and inspector card distinguish the tile covered from a random tile other than Kings. The common directory of texts remains unchanged.

`DieState.SpentWith` / `DieView.SpentWith` distinguishes the place of laying down of the issued Keeper/Lesser Eye from its owner. In such a cost is FP; `Owner`, source and selected Remove do not change. Recovery removes the tag and applies the original rules of the given dice. Normal/Ruler Eye does not change the pool. The status field is optional and omitted at null; old entries retain the default jackpot.

`EnterCharacter` has an optional `CommandPayload.CouncilTokenId`. `CharacterActionOption.CouncilTokens` shows legal tokens, `RequiresCouncilToken` distinguishes the input without normal prerequisite. The server validates both resources, region and no previous entry; token bypasses only prerequisite Dark Chieftain. The preview lists both costs, execution spends both in one transaction. The other command rejects smuggled token Council. Details of sources, order and acceptance: [T31](rules/T31-INTEGRATION.md). UI new controls remain in UI-09.

T33 adds source diagnostics of configuration and content composition before RNG. `ConfigurationConflict` indicates only public option, requirement, rule and source; `ContentConflictException` public slot card. Both mechanisms are independent of snapshot, hands and private decisions. The API configuration error code remains `InvalidConfiguration`. No Dismay value runs identically before and after `Normalize`, also for legal Removing Ruler Tokens at Lords+Kings. The saved deck order and the current snapshot format remain unchanged; the reading does not start folding the content. [Matrix T33](rules/T33-COMPOSITION.md) checks the preparation, full composition and both projections after reboot.
<a id="ui-09--opcje-prezentacji-dodatków"></a>
## UI-09 — presentation options expansions

`AwakenSovereign.Travel` contains a separate option for the sovereign: `Id`, current `Origin`, printed `Range`, legal targets, `StopRegions` (fortress Shadow) and `BlockedRegions` (siege Free Peoples). The client sketches the route, the server continues to check all the way through `SovereignAwakening.Valid`.

`CharacterActionOption.RulerRecruitment.Settlements` gives the nation a free settlement. `LocalRegion` can be a normal region and it is not a settlement; UI can not treat the local place as an additional place from the `SettlementCount` pool. Example: Théoden in Eastemnet — one local figure and the choices from the settlement, never two figure posing as two settlement Eastemnet. Checks: `ExpansionPresentationOptionsTests` and `ExpansionPresentationTests`. These fields supplement the K01/K02 projection; they do not change the rules or condition written.

Client `DraftOptions` choose `RulerTokenActions` after indicated token; does not modify `PlayerView.Options` , `Dice.Face` or configuration. Token and die they stay in one command Several options for waking the same type are combined exclusively for the sovereign selector. Council Travel draws identifier moving groups with received options `Travel`.

UI-31 — `DiscardCards` : `Minimum=1` , `Maximum=remaining excess` , `EligibleIds` contains only its own. Each accepted command removes the various indicated ID , increment the revision and save the receipt. Overtime opens a new decision about the new ID for the same owner and saves `AfterDiscard` - Six. cards does not allow other activities. Older clients can still send the entire remaining set. Empty list, duplicates, foreign ID and overshooting is not allowed. Public history only gives a number cards ; single recoil is covered. Snapshot schema and type command they remain unchanged.

<a id="publiczny-raport-polowania--ui-33"></a>
## Public report Hunt — UI-33

UI-50 Adds optional `HuntView.MovementBonus` and `EffectBonus` . `Bonus` remains the sum used by the rules; components separate the previous dice Movement FP of the premium card (e.g. Flocks of Crebain). The current cost of Resolving does not enter the traffic bonus. `RerollBonus` remains a separate bonus used exclusively for reroll, without creating a right to them. Before the throw of the ingredients are not given; the older report without fields has null and shows the total itself instead of guessing its origin. Public snapshots report save the same ingredients for both parties; no changes command , RNG Or protocol.

`PlayerView.Hunt` remains the current projection of the active frame. Optional `HuntReport` stores the entire current or last string after the missing frame. `Id` is durable and independent of randomness, `StartedRevision` identifies the beginning, `Completed` close. Entries are ordered, have a revision, `HuntId`, type `HuntReportKind`, public source, public snapshot Hunt and the public result of Fellowship. Entries of effects may not have a snapshot Hunt. `HuntView.Healing` separates treatment and injury; `DamagePending=true` denotes the outstanding value, also in the historical entry of the mentioned tile.

`Initial`/`Outcome`: known region, progress, Mordor step, hiding, Corruption and public Guide. `CompanionLeft`, `CompanionPlaced` and `CharacterEliminated` contain only the already revealed identity and location. The report does not contain secret versions of Companion or potential future victim. It does not send order of pool/decks or future samples of RNG. Both sides receive the same copy of the report; legal options still only the decision owner.

The report is part of the normal snapshotu/transakcji/receipt. Re-do command does not add or draw again. Reading an older snapshot without a field gives `null`; the client shows no available report. No text history or track is parsed. New optional fields do not change existing payoads or fingerprints of commands.

UI-34 does not add a new command recovery. When unsure, the client opens once a context containing existing `Check result` for the preserved CommandId. The change of the decision owner does not change the receipt, its cost or the repeat semantics. The window is only local.

UI-35 checks the compatibility of the historical body without changing the pattern of the rules. Continuation of the old disclosure in Moria can begin **new** report of the additional tile; its revision/ the source and identical projection of both sides are checked separately. It does not match the historical projection. acceptance does not change DTO , payloads, fingerprints or protocol versions.


<a id="skład-puli-polowania-i-lico-kafla--ui-141"></a>
## Pool composition Hunt and lico kafla — UI-141

Contracts 0.3.1 (public content 1.0.3 requires this version) adds `PlayerView.HuntPool` (`HuntPoolView`) and optional `HuntView.Face` (`HuntTileFaceView`). The fields are additional: the protocol 2, command, payodays and fingerprints unchanged; the older snapshot report without `Face` gives `null`.

- `HuntTileFaceView` : printed lico tile — `Pool` ( `standard` , `special-fp` , `special-sh` ,
  `smeagol` ), `Value` (number, `eye` , `die` ), `Reveal` , `Stop` , `Source` ( `base` , `lords` ,
  `kings` ) and `Count` , the number of tiles of the letter about this lice.
- `HuntPoolView.Tiles`: pool composition grouped after the July in a fixed order (pula, source, Eye,
  decreasing value), never in the order of draw. `Count` is the number of tiles in the pool.
  `Prepared` is the special tiles in the game that will enter the pool after entering Fellowship
  into Mordor.
- Privacy: the composition is public (the selected tiles are discovered, special ones come out), but
  the tile Corruption on the sovereign that the viewer cannot read, and the undisclosed tile of the
  ongoing attempt Corruption (for Free Peoples) remain in `Tiles` on the viewer; `Unseen` tells how
  many of them are no longer in the pool. The subtraction of the letter does not reveal their
  identity. The tiles Kings are always public. Tests: `HuntPoolProjectionTests`.
- `HuntView.Face` describes the `TileId` of the current Hunt and each report snapshot to show the
  tile graphics without its own expansions directory.

<a id="typowane-fakty-kroków--ui-39"></a>
## Typical facts of the steps — UI-39

`ActionLogStep.Facts` complements the existing contract without changes to payoads, fingerprints or snapshot versions. It includes public SourceId/Targets, changes to figures (the then definition, nation, kind, garrison and locations), nations (Active/StepsToWar before and after), controls, dice, resources and cards. `Cards.Use` distinguishes selection, recoil, play, commitment/reveal and entry/ removal from the table. Covered discard has a public Count without Id, and a separate step of the owner can contain identity. The choice of effect itself has private OwnEffectChoice, without revealing the rejected card on the other side.

`LossDetails` keeps the ordered operations, figure from loss, number of hits and ReplacementId/ReplacementFrom; `Losses` keeps the approved selection. `Budget` has actually completed steps and rest after command, also the last one. `ActionCostKind.Figure` describes e.g. the elite for the extra round siege. The hidden cost of cards gives Count and the public Source, without Id. The additional cost of the response goes to her step, not to the cost of the parent.

`Battle` is a public snapshot of the moment of entry. Earlier commitment does not obtain the identity of card after a reveal, and OwnRerollSelection remains empty. BattleId combines facts with the future report UI-40. `Hunt` retains ChainId, a copy of the existing entry HuntReport and a public completed; another string does not replace the former story. `FellowshipBefore/After` and the public FellowshipRoute do not create a route of normal hidden traffic. `Rolls`, HitTarget/Hits, Phase and Outcome describe the actual performance of the manufacturers. Sovereigns only contains public data of the variant.

The new HistoryId have separate public/FreePeoples/Shadow. counters The public list, details, numbers and cursors do not change after the private selection of the reroll itself. HistoryId and parents do not encode the hidden index. SetCombatCardPolicy without public change does not add an entry; when performing a commit, the story describes the commit as an automatic. Preview, rejection and retry still do not produce new steps.


<a id="ui-40--raport-bitwy"></a>
## UI-40 — battle report

`GET /api/matches/{id}/battles/{battleId}` requires session and place in the party. Unknown report, other party or BattleId from another party: 404; no session: 401. BattleId is opaque and requires URL encoding. costs/ActionId indicates action parent, `Context.BattleRound` specific step round. `Rounds` contains only revealed/played round, in order of numbers, with identifier public steps. Empty rounds means e.g. shelter without reveal/rzutu.

`Initial` and `Latest` are historical projections of participating combat forces. After partial entry they are not used to calculate all survivors: `Place.Units/Characters` real figure in the region battle , origin and retreat, from Garrison/Location. First BattleStarted contains the same snapshot, so you can distinguish between garrison and rear guard. `Place` also contains the name region/nation/type settlement And control. `Outcome` preserve points and the actual score of the game without an alleged winner battle Facts of the final checkpoint command have priority over the indirect event of the same revision.

Status: InProgress , AwaitingSettlement (the fight is closed, action parent waiting), Completed or Unknown for legacy. Two battle one action have separate steps/Rounds ; settlement of this action ends both reports. Version indicates the last public fact shown or public settlement action Private commit/reroll does not disclose content or change the public version. Legacy/PartialHistory describe shortcomings; no sources are zero cost or fictional turn Public copies do not include hand, reroll selection or RNG .

<a id="ui-45--podgląd-operacji-strat"></a>
## UI-45 — preview of loss operations

The optional `ActionPreview.Losses` is `ArmyLossPreview`: RequiredHits, AppliedHits, Voluntary and Ordered Operations (`ActionLossFact`). For legal TakeBattleLosses/TakeEventLosses data come from the same ArmyLossPlan as validation and recording. Hits means the cost of a particular operation; Downgrade without ReplacementId removes the elite for one hit. ReplacementFrom distinguishes casualties/reserve; the earlier operation can release figure. RemovedIds also continues to give the actual lost commanders/post by reason. Preview does not perform movement, draw or history.

The field is additive and optional; old answers are not supplemented by conjecture. It does not change payoad command, receipt, protocol or snapshot. Illegal or incomplete draft does not receive an apparent legal preview.

<a id="granica-raportu-otwartego-z-alertu--ui-47"></a>
## Limit of open report from alert — UI-47

The specified `ActionLogStep.Command` represents the approved decision with automatic effects of the same `Revision`. The client `BattleReportVersion.Through` includes these public steps (e.g. `AdvanceBattle` and the following `BattleEnded`). It does not transfer another revision from the late GET to the previously clicked alert. For a single event without Command, the exact limit of ID/In order is valid. The data still comes exclusively from the server public report; the reading does not run command, cost or RNG. Unit and two client regressions are described by UI-47.

<a id="ui-56--efekty-dostępność-i-punkty"></a>
## UI-56 — effects, availability and points

`PlayerView.Availability` It's your own. `ActionAvailabilityView` : SubjectId , ActionId , Title, Source (rule lodger), Cost, Timing, ConditionsMet , WindowOpen , CanAct , EligibleDice , Targets and Conditions. Each condition has Category ( Cost/Target/Condition/Limit ), stable Code, English Description and Met. ConditionsMet describes the conditions independently of the actor of the current decision. WindowOpen requires its own decision at the right time; CanAct combines both conditions and excludes the game ending. This is a description of the available choice, not the power to send any payload. The server still validizes a specific command , die , route, target, token and decision version. No new fields leave empty lists/descriptions And false, never a fictional consent.

UI-129 Adds entry `recruit` for each individual nation of the viewer ( `SubjectId` = id nation) without changing Contracts. `Targets` This settlement to which any figure from the reserve can now hit the regular Muster (in an ongoing sequence without the used sediments and figures ); `EligibleDice` This dice Muster or die sequences. `Conditions` : `at-war` (description of steps and activity), `reserves` , `settlement:<region>` for each settlement nation with a note `:<reason>` closed ( `at-war` , `captured` , `besieged` , `enemy-army` , `used-this-action` , `nazgul-stronghold` , `kings-weakness` , `reserves` ) and `elite-budget` at one other point. Exceptionally `ConditionsMet` ‘is open settlement “because the conditions of the settlement are an explanation, not a conundrum; `CanAct` It connects it to the decision window. The opponent does not get other people's entries.

| ActionId | SubjectId/use |
| --- | --- |
| change-die | A specific own Ring, a simple change in the score. |
| keeper | Specific Narya/Nenya/Vilya, separate cost and proper window. |
| council | Own token Council and its printed action. |
| council-chieftain | Token Council together with dice chief entrance; a separate cost. |
| ruler-token | Token sovereign associated with a particular Ruler and its full capacity. |
| dismay | Own Dismay, authorized to preserve the Ruler of both sides. |
| keep-special | Own die Keeper/Lesser, compliance with the Eye selection obligation. |
| discard-after-ruler | Own die allowed to discard after throwing out the Ruler. |
| awaken / remove-corrupted / cleanse | Sovereign designated by FP; cleansse requires a real window after the policy and random return of the tile. |
| attempt-corruption | Sovereign named by SH, limit on turn and authorized Eye. |

`ActiveEffectView` preserves the existing fields and adds SourceKind ( Card/Ability/Effect ), Title, Description, SourceReference , ExpiryDescription and Targets ( Kind/Id/Label The objectives distinguish the Region, Nation, NationRegions , Politics, Fellowship, Global and Character. Do not extend the general objective to all regions. Each effect has its own Id and term; SourceId allows you to group together card "ability. card table without separate state of the modifier has a stable entry `table:<cardId>` Corrupted Sovereign uses public ID versions and separate coverage recruitment/second weakness. Source counter count after SourceId/In the The owner, not after the number of effects.

`PlayerView.MilitaryVictory` contains both sides: Points, Threshold, ThresholdReached , CheckedAt , Source and Regions ( RegionId/Name/Settlement/ Points). The total of the regions is identical with PlayerSummary. VictoryPoints ; the threshold does not set Winner/Victory. Regions come from board setup of a given configuration, before the dynamic change of the nationality of Kings.

`DieView.SourceTitle` and RecoveryDescription describe the public source and conditional recovery. Rune does not promise to be removed before checking the correct White/Witch-kinga in the next turn. Identity, owner, SpentWith and HuntOrigin remain in the previous fields.

New fields do not read the hidden hand of the other side, the Council version or the values of the selected tiles. SovereignView .Corruption still remains null when the sum is unknown. Visible changes the assigned tiles and public wording cards , without revealing OwnChoices The trials of the opponent.

Targets means the actual range of the modifier; historical TargetId can only mean the object of the expiration condition (Saruman). The ruler-action condition is only present after the actual launch of the planner; the cost, limit or source may prevent the object from being evaluated. The untested target is not described as Met=false.

<a id="ui-65--cykl-życia-i-przyszłe-wersje-postaci"></a>
## UI-65 — life cycle and future versions of characters

PlayerView . OwnCharacterRoster : List CharacterRosterView , empty by default. The entry contains FigureId , Identity, Side, CurrentDefinitionId , Title, CharacterRosterLifecycle , RegionId/Garrison , nullable Level/MovementLevel/ CombatLevel/Leadership , NationIcon , UnlimitedMovement and FutureVersions No current version before the first entry gives empty CurrentDefinitionId and null statistics, not apparent figure Maps. RegionId exists only for OnBoard ; Fellowship and figure Waiting for a return do not create a fictional destination of the Place.

Lifecycle: Unknown (neutral value), NotEntered, OnBoard, InFellowship, AwaitingPlacement, AwaitingReturn, Eliminated and EliminatedWithReplacement. The last state is related to Grey/Keeper Grey, which can still enter as White. The unfulfilled current condition White does not mean definitive death of figure. AwaitingReturn reads both the Council queue and the active council.art-of-disappearing program; the self casualties without exception is Eliminated.

CharacterEntryVersionView : DefinitionId , Title, Mode ( Unknown/FirstEntry/ Transformation/ReturnAsNewVersion ), ExtraDiceNextRecovery , Entry ( ActionAvailabilityView ), optional CouncilEntry and specific available CouncilTokens This is a description of the terms; not a new command Or the right to exercise. EligibleDice/Targets they come from the same planners/costs as validation. ConditionsMet separate the cost and the cumulative entry conditions from WindowOpen ; CanAct requires both and own Action/EnterCharacter without the game being finished. CouncilEntry keeps its own token and cost dice ; when the planner ends up with a lack of token, it does not publish a false diagnosis of the untested region.

The full location and version are exclusively from its own list. SH does not receive FP roster or private versions of the Council. The previous Characters, OwnCompanions, OwnCompanionVersions and the public inspection of the opponent retain responsibility. Older answers have an empty roster, Unknown, null statistics and false accessibility; we do not request elimination or consent. The version of the database and the existing command has not changed.

<a id="menu-i-publiczne-pokoje--menu"></a>
## Menu and public rooms — MENU

Contracts for new screens, paginations, Ready, invitations and exits are described by [MAIN_MENU_UX](https://github.com/jpolaczek/wotr-client/blob/a7967afe0ca8b2b02d3777503ccc99eec0a25e63/docs/ui/MAIN_MENU_UX.md). Public DTO is never RoomView.

<a id="paczka-comwotrcontracts--dist-02"></a>
## Pack com.wotr.contracts — DIST-02

Contracts remain a project. NET Standard 2.1 without reference to projects; DTO the server compiles them from sources, and the client gets the package UPM `com.wotr.contracts-<version>.tgz` ed `scripts/package_deps.py` : `package.json` , `Runtime/**` and theirs `.meta` (solid GUID -y asmdef and scripts), without `Wotr.Contracts.csproj` and `packages.lock.json` Packer rejects sources with an appeal to `Wotr.Domain` , `Wotr.Server` or `Wotr.Persistence` . .

The package version is `version` ed `src/Wotr.Contracts/package.json` (from DIST-05 : 0.2.0 The change of contract requires it to be conquered; the published version is not replaced by other bytes. The manifesto of the package saves the commit, SHA-256 and `Protocol.Version` , `RulesVersion` and `ContentVersion` . A package of public content saves the version of Contracts with which it was created; the client bootstrap rejects the lock in which these versions do not agree. HTTP/WebSocket describes the section DIST-05 below.

Contracts 0.11.0 (LOBBY-01) add `LobbyMessage` (`Sequence`, `Author`, `Text`, `At`), `LobbyMessageRequest` (`Text`),
`OnlinePlayer` (`Name`, `You`), `ChallengeStatus` (`Pending`, `Accepted`, `Declined`, `Cancelled`, `Expired`),
`ChallengeView` (`Id`, `From`, `To`, `Status`, `ExpiresAt`, `RoomId`), `ChallengeRequest` (`Name`), `LobbyView`
(`Messages`, `Latest`, `Online`, `Incoming`, `Outgoing`) and `LobbyLimits`. They only add types; existing DTOs,
the protocol and commands are unchanged. Public content 1.0.12 has the same files as 1.0.11 and requires Contracts
0.11.0. Endpoints: [T08-LOBBY](server/T08-LOBBY.md#lobby-chat-players-online-and-challenges--lobby-01-2026-10-18).

On the expansions line, Contracts 0.12.0–0.14.0 and public content 1.0.13–1.0.15 below were local builds that
were never published; `main` published its own Contracts 0.12.0 and content 1.0.13 (UI-361, after them).

Contracts 0.12.0 (EXP-01, release per module) add `ExpansionModules` (the ids `Fate` = `fate`, `Lords` = `lords`,
`Kings` = `kings`, `All` in release order, `IsKnown`, and `Of(MatchConfiguration)`, the modules a configuration
enables), `RoomView.OfferedModules` (`offeredModules` in JSON: the modules this server releases for new tables,
lower-case ids in release order; empty when it offers only the base game) and `PublicRoomSummary.Configuration`
(`configuration`: the room's current configuration, so the Open games list can show its expansions).
`RoomView.ExpansionsAvailable` stays and is true when any module is released, so clients from before 0.12.0 keep
working; such a client shows every expansion switch while only some modules are released, and the server refuses the
others with 409 `ExpansionsUnavailable`. The protocol and commands are unchanged. Public content 1.0.13 has the same
files as 1.0.12 and requires Contracts 0.12.0. Rules: [T08-LOBBY](server/T08-LOBBY.md#release-per-module--exp-01-2026-10-19).

Contracts 0.13.0 (TEST-10) add `TestScenarioView.Modules` (`modules` in JSON: the expansion modules a test scenario
plays with, lower-case `ExpansionModules` ids; empty for the base game). The server lists and starts a scenario only
when all its modules are released; otherwise it is absent from `GET /api/test-scenarios` and its start answers 404
`ScenarioNotFound`. The protocol and commands are unchanged. Public content 1.0.14 has the same files as 1.0.13 and
requires Contracts 0.13.0. Scenarios: [TEST_SCENARIOS](TEST_SCENARIOS.md).

Contracts 0.14.0 (DEMO-06) add `OfferedModulesView` (`modules`: the expansion modules this server releases for new
tables), answered by `GET /api/rooms/offered-modules` so the menu's demo page can show the rules before any room
exists. A demo (`POST /api/rooms/{id}/demo`) now plays with its room's configuration and is refused with 409
`ExpansionsUnavailable` while a module of it is not released. The protocol and commands are unchanged. Public content
1.0.15 has the same files as 1.0.14 and requires Contracts 0.14.0. Rules: [DEMO-MODE](server/DEMO-MODE.md).

Contracts 0.12.0 on `main` (UI-361) add `PlayerSummary.HandDeckCounts`: each player's hand by deck (`character`, `strategy`, the
keys of `DeckCounts`), from the same cards as `HandCount` (a battle's Balrog table card included). It is public in both
views, as the decks' backs differ; the cards themselves stay hidden
(`ProjectionTests.EachHandsSplitByDeckIsPublicAndItsCardsStayHidden`). The field only adds data: an older client ignores
it and the protocol is unchanged. Public content 1.0.13 has the same files as 1.0.12 and requires Contracts 0.12.0.

Contracts 0.15.0 (EXP-16, the expansions line after `main`'s 0.12.0) hold both lines: the expansions' 0.12.0–0.14.0
above and `main`'s `PlayerSummary.HandDeckCounts`. Public content 1.0.16 has the same files as 1.0.15 and requires
Contracts 0.15.0.

<a id="wersje-klienta-katalog-wydań-i-zgodność--dist-05"></a>
## Client versions, release directory and compliance — DIST-05

Source: DIST-05, [DISTRIBUTION_PLAN](release/DISTRIBUTION_PLAN.md). Contracts 0.2.0 add only new types; existing DTO have not changed. Public content 1.0.1 has the same files as 1.0.0 and saves dependence on Contracts 0.2.0.

<a id="rozdzielone-identyfikatory-ds10"></a>
### Separated identifier (DS10)

| Identifier | Source | He's changing it. |
| --- | --- | --- |
| Client version (SemVer without metadata) | `wotr-client:release/version.json` | client release |
| Protocol | `Protocol.Version` (2) | incompatible change of API/WS |
| Rules and content of the party | `Protocol.RulesVersion`, `Protocol.ContentVersion`, saved in batch | migration of recording |
| Public content | version of the package from the client lock and SHA-256 files `Resources` | `package_deps.py` |
| Save diagram | `StateCodec.CurrentVersion` | server migration |
| Server protocol range | `ProtocolSupport.Current` (`src/Wotr.Releases`) | server release |

`ClientBuildInfo` (scheme) 1 ) is one identity of the build: `version` , `sourceCommit` , `platform` ( `windows-x64`, `macos-universal` ), `development` , `protocolVersion` , `rulesVersion` , `contentVersion` , `publicContentVersion` , `publicContentSha256` , `unityVersion` Generator in the client ( `ClientBuildIdentity` ) saves the same text JSON (camelCase, `\n` ) in the player and in the `build-info.json` . . `development: true` means local build, editor or developer build; the signature tool rejects such build. `ClientVersion` compare versions by SemVer 2.0 : issue is newer than its pre-release, and labels are compared to the field by field (number, text by order ASCII Versions contain only lower letters and cannot have metadata. `+build` . .

<a id="nagłówki-i-odmowy-ds11"></a>
### Headers and refusals (DS11)

Each request `/api/**` and handshake WebSocket sends `X-Wotr-Client-Version` and `X-Wotr-Protocol-Version`. The server checks them before authentication, login limit, upgrade WebSocket and command:

| Headers | Response |
| --- | --- |
| compatible: server protocol, version ≥ minimum directory | further processing |
| protocol above `Maximum` | 409 `{"error":"ServerUpdateRequired"}` |
| protocol below `Minimum` or version < minimum | 409 `{"error":"ClientUpdateRequired"}` |
| both missing | 409 `ClientUpdateRequired`; during the transitional period (`Wotr:Clients:AcceptUnversioned=true`) adopted |
| one missing, bad format, repeated header | 400 `{"error":"InvalidClientVersion"}` |

Refusals have `Cache-Control: no-store`. Without control remain `/health`, `/health/ready`, `/api/client/releases`, `/api/client/downloads` (DIST-09) and `/api/auth/logout`. By default Production rejects clients without headers, and other environments do not reject them. Refusals command does not come into the engine: it does not change the revision, the receiver or RNG.

<a id="publiczny-katalog-ds12"></a>
### Public Directory (DS12)

`GET /api/client/releases?platform=<p>[&version=<v>&protocol=<n>]` works without a session and returns `ClientReleaseView` : `channel` ( `stable` ), `platform` , `minimumVersion` (empty without minimum), `minimumProtocolVersion` , `maximumProtocolVersion` , `status` ( `Unknown` without a version, `Compatible` , `ClientUpdateRequired` , `ServerUpdateRequired` ) and `latest` : the latest announced edition in accordance with server protocols ( `version` , `protocolVersion` , `manifestUrl` , `signatureUrl` , `publishedAt` ) or `null` Unknown platform gives 400 `UnknownPlatform` , bad version or protocol 400 `InvalidClientVersion` The answer is `no-store` and does not contain account, session or party data; it is identical to and without token.

<a id="manifest-i-podpis"></a>
### Manifesto and signature

`releases/<version>/<platform>/manifest.json` (scheme) 1 , channel `stable` ): `version` , `platform` , `protocolVersion` , `sourceCommit` , `package` ( `fileName` , `url` , `size` , `sha256` ), `notes` ( 1–4000 signs), `createdAt` . . `package.url` must be accurate `<downloads>/releases/<version>/<platform>/<file>` at an approved address HTTPS Unknown fields reject the manifesto. `manifest.json.sig` contains `algorithm` ( `Ed25519` ), `keyId` ( 16 hex characters SHA-256 public key) and `signature` (base64) over exact manifesto bytes. Verification uses BouncyCastle .Cryptography 2.7.0 ( RFC 8032 ).

`catalog.json` (1 scheme) contains `revision`, `minimumVersion` and `releases` with `version`, `protocolVersion`, `publishedAt`, `withdrawn` and SHA-256 with the manifest of each platform. `minimumVersion` is empty or equal to the announced, unrepeated release. Files and commands of the operator describe [infra/README](../infra/README.md#katalog-wydań-klienta--dist-05).

<a id="minimum-z-terminem-naprawa-i-instalatory--dist-09"></a>
## Minimum deadline, repair and installers — DIST-09

Source: DIST-09, [D13](DECISIONS.md), [plan](release/DISTRIBUTION_PLAN.md#d13--zmiany-z-2026-10-14). Contracts 0.4.0 are additive: new fields `ClientReleaseView` and new types; enum `ClientCompatibilityStatus` unchanged.

<a id="rozszerzony-katalog-dla-klienta"></a>
### Extended directory for client

`ClientReleaseView` gets:

| Pole | Meaning |
| --- | --- |
| `serverTime` | server time (UTC) at the time of reply; the client counts the countdown from it |
| `scheduledMinimum` | `{version, effectiveAt}` minimum to be applied in the future; `null` when missing or due date |
| `installed` | offer (like `latest`) for the requested version to repair the installation; `null` for the withdrawn version, unknown or without the `version` parameter |

`minimumVersion` and `status` always describe the minimum in force now. From `effectiveAt` gate rejects older versions (409 `ClientUpdateRequired`) without reloading the directory file; the open WebSockety remain, subsequent requests HTTP and handshake get 409.

<a id="instalatory-dla-strony-gry"></a>
### Installers for Party game

`GET /api/client/downloads` works without session and without version headers, with `no-store` , and returns `ClientDownloadsView` : `latest` ed `version` , `publishedAt` and list `installers` ( `platform` , `fileName` , `url` , `size` , `sha256` ) — the latest announced edition, which has installers of both platforms; `null` When there is no such thing. DIST-09 (manifest scheme 1 ) have no installers and are omitted.

<a id="schematy-plików"></a>
### File diagrams

The Manifesto schema 2 adds the required `installer` (`fileName`, `url`, `size`, `sha256`; a different file than `package`, the same release directory). The release folder contains a manifest, signature, package and installer. The manifesto of the 1 without `installer` remains valid. The client reads the manifesto type `ClientReleaseManifest` (Contracts) with the same fields as the server type.

`catalog.json` schema 2 adds `scheduledMinimum` (`version`, `effectiveAt` in UTC). The scheduled version is announced, unretired and higher than `minimumVersion`; at most one at a time. The 1 schema file is read unchanged, and each tool recording has 2 schema. The operator's commands are described by [infra/README](../infra/README.md#minimum-z-terminem-i-instalatory--dist-09).

<a id="scenariusze-testowe--test-01"></a>
## Test scenarios — TEST-01

Contracts 0.3.0 adds DTO test mode (R31); public content 1.0.2 requires this version. `PlayerView` and command games are unchanged.

| Route | Response |
| --- | --- |
| `GET /api/test-scenarios` | `TestScenarioCatalogView` with `TestScenarioView` list (`id`, `group`, `title`, `description`, `startingSide`). |
| `POST /api/test-scenarios/{id}/start` | `TestSessionView`: `matchId`, `scenario`, `ownerSide` and `seat` (`SessionView` on the other hand). 404 `ScenarioNotFound` for unknown id. |
| `DELETE /api/test-scenarios/session` | 204; removes the owner's test batch. |

- Routes require a session and only exist when mode is on; without it they correspond to 404.
- Owner game Free Peoples with his token and his token `seat` ♪ give ♪ Shadow in the same batch. The
  game uses regular routes `/api/matches/{id}`.
- The `seat` session gets 403 on each route outside `/api/matches`, and in lots other than its own
  test batch sees 404.
- Start and restart invalidate the previous token `seat` (401).

<a id="tryb-demo-z-cpu--demo-01"></a>
## Demo mode with CPU — DEMO-01

Contracts 0.5.0 add additive `DemoMatchView` ( `MatchId` , `You` ), and public content 1.0.7 requires this version. `PlayerView` , command and the list of parties are unchanged: demo is a simple party with a technical account `demo-cpu` (name ‘ CPU ) on the other side and the name ‘Demo · {page} vs CPU Project: ” [DEMO-MODE](server/DEMO-MODE.md) .

<a id="usuwanie-gracza-i-czat-poczekalni--menu-24"></a>
## Remove player and chat waiting room — MENU-24

Contracts 0.6.0 add add additive `RoomChatMessage` (`Sequence`, `Author`, `Side`, `Text`), `RoomChatView` (`Messages`, `Latest`) and `RoomChatRequest` (`Text`). Public content 1.0.8 requires this version.

| Route | Response |
| --- | --- |
| `POST /api/rooms/{id}/remove` | Host removes guest: 200 `RoomView`. 404 foreign or closed room; 409 `HostRequired`, `MatchAlreadyStarted`, `SeatEmpty`. |
| `GET /api/rooms/{id}` | No change for members; deleted account gets 410 `RemovedFromRoom`. |
| `POST /api/rooms/join`, `POST /api/rooms/{id}/join` | Deleted account: 409 `RemovedFromRoom`. |
| `GET /api/rooms/{id}/chat?after=N` | `RoomChatView` with messages with a number greater than N (all when N > `Latest`). 404 outside members. |
| `POST /api/rooms/{id}/chat` | `RoomChatRequest`. The text after the cut has 1–200 characters, no controls; otherwise 400 `InvalidMessage`. Answer: `RoomChatView` with last messages. |

Chat is only in process memory (at most 50 messages per room), without a database and history. Details: [T08-LOBBY](server/T08-LOBBY.md).

<a id="konta-z-e-mailem--auth-02"></a>
## Email accounts — AUTH-02

Contracts 0.7.0 they are not retroactively altered ( [D16](DECISIONS.md) ): `RegisterRequest` has `Username` , `Email` , `Password` (without `Invitation` ), `InvitationView` does not exist, and `AccountInput` instead of `ValidInvitation` and `InvitationLength` has `ValidEmail` , `NormalizeEmail` , `MaximumEmailLength` and `MaximumEmailLocalLength` . . `LoginRequest.Username` accepts the name or address. 0.7.0 In the AUTH-03 with removal of registration from the game; until then the client on 0.6.0 log in with a name and his registration form with an invitation gets 400 `InvalidRegistration` Endpointy `/api/account/*` serve the parties WWW games and have DTO only on the server side ( `token` , `login` , `password` ); gate the client's version does not include them, as does `/api/auth/register` . .

<a id="obserwatorzy-pokoju--obs-01"></a>
## Peace Observers — OBS-01

Contracts 0.8.0 add additive:
- `RoomRole` (`Player`, `Observer`);
- `JoinRoomRequest.Role`;
- `RoomRoleRequest` (`Role`, `Side`);
- `RemoveParticipantRequest` (`Name`);
- `RoomObserverView` (`Name`, `IsHost`);
- In the `RoomView`: `Role`, `Observers`, `ObserverLimit`;
- In the `PublicRoomSummary`: `Status`, `Players`, `Observers`, `CanPlay`,
  `CanObserve`;
- In the `RoomChatMessage`: `Role`.

Public content 1.0.9 requires this version. `You` viewer has the value of Free Peoples, that is, its default perspective.

| Route | Response |
| --- | --- |
| `POST /api/rooms/join` | `Role = Observer` joins the audience, also after takeoff. 409 `ObserversFull`, `MatchFinished`, `RemovedFromRoom`; 400 `InvalidRole`. |
| `POST /api/rooms/{id}/join?role=Observer` | As above for the public room. |
| `POST /api/rooms/{id}/role` | `RoomRoleRequest`, only in Waiting: 200 `RoomView`; 409 `RolesLocked`, `SeatTaken`, `ObserversFull`; 400 `InvalidRole`. |
| `POST /api/rooms/{id}/presence` | Refurbishment of the viewer's lease: 204; 404 after expiry, removal or closure of the room. |
| `POST /api/rooms/{id}/remove` | Optional `RemoveParticipantRequest`: player name or viewer; 409 `ParticipantNotFound`. |
| `PUT /api/rooms/{id}/ready`, `POST /api/rooms/{id}/confirm` | The viewer gets 409 `PlayerRequired`. |
| `GET /api/rooms/public` | LOBBY-05: only rooms whose game has not started (also with both seats taken), so `Status` is always `Waiting`; a game in progress is watched by its code. |
| `GET /api/rooms` | LOBBY-05: without the rooms whose game is finished or removed. |

Details: [T08-LOBBY](server/T08-LOBBY.md).

<a id="odczyty-obserwatora--obs-02"></a>
## Observer readings — OBS-02

Without new types: the observer gets existing `PlayerView`, `ActionLogPage`, `ActionLogView` and `BattleReportView`. The `perspective` parameter (`FreePeoples` by default or `Shadow`) selects the page; another value gives 400 `InvalidPerspective`. Party player and alien get 404 on each of these routes.

| Route | Response |
| --- | --- |
| `GET /api/observe/{id}?perspective=` | `PlayerView` selected page with her hand; `Options`, `Availability`, `RulerTokenActions` empty, `AutoDecision` default, `Decision` without `Id` and `Choices` (type, source, window, presentation remain). |
| `GET /api/observe/{id}/actions?perspective=&filter=&limit=&before=&after=` | `ActionLogPage` as for the player of this page. |
| `GET /api/observe/{id}/actions/{actionId}?perspective=&filter=` | `ActionLogView` as for the player of this page. |
| `GET /api/observe/{id}/battles/{battleId}?perspective=` | `BattleReportView`. |
| `GET /api/observe/{id}/updates` (WebSocket) | `{ matchId, revision, privateVersion: 0 }`; closes after loss of access. |

Details: [T07-TRANSACTIONS](server/T07-TRANSACTIONS.md).

<a id="plik-replaya--replay-02"></a>
## File replaya — REPLAY-02

Contracts 0.9.0 add additively `ReplayFormat` (permanent entries and limits names), `ReplayManifest`, `ReplayPlayer`, `ReplayFile`, `ReplayStep`, `ReplayTurn`, `ReplayFrame` and `ReplayBattleReport`. Public content 1.0.10 requires this version.

| Route | Response |
| --- | --- |
| `GET /api/matches/{id}/replay` | Player of finished, recorded game: 200 `application/zip` (`.wotrreplay`). |
| `GET /api/observe/{id}/replay` | The same for the viewer with the important lease. |

- 404: aliens, expired viewer or removed party.
- 409 `MatchNotFinished`: batch in progress.
- 409 `ReplayUnavailable`: party without recording.

Format: [REPLAY-FORMAT](server/REPLAY-FORMAT.md).

<a id="zgodność-obserwatorów-i-replayów--replay-07"></a>
## Compatibility of Observers and Replays — REPLAY-07

A server with migrations 012 and 013 requires Contracts 0.9.0 and public content 1.0.10 in a client with observers and replays. The changes Contracts 0.8.0 and 0.9.0 are additional, so the older client is still playing. The format of the replay file has its own version (`formatVersion` 1, [REPLAY-FORMAT](server/REPLAY-FORMAT.md)) independent of the version of Contracts.

## Linux client platform — LNX-01

Contracts 0.10.0 add `ClientPlatforms.Linux` (`linux-x64`) to `All` and `IsKnown` (D18). The catalog,
`/api/client/releases?platform=linux-x64`, the release tool and `/api/client/downloads` handle it
like the other platforms: a release is announced only with the package, installer
(`WotrOnline-<version>-linux.AppImage`) and signed manifest of all three platforms. The Steam Deck
runs the Linux build and reports `linux-x64`.
