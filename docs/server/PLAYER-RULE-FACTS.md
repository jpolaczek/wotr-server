# Player rule facts at an action boundary

## Direct ordinary battle losses — boundary v14

Boundary v14 extends the existing direct paid-battle marker to ordinary,
non-voluntary combat loss decision. The only accepted shape is a top-level
`FinishAction` battle frame with one plain `BattleLoss` frame above it, source
`combat`, a positive hit count and the matching loss stage in any battle round. The
existing BattleView and DecisionView already expose the public loss owner,
source, hit count and legal choice; `BattleContextView` still adds only
`CompletesPaidAction`. Voluntary losses, event losses, nested battles, parent
returns and extra frames keep the boundary absent. Both seats receive the same
semantic fact; it does not expose the frame or any private choice.

`ActionBoundaryBattleTests` checks both attacker sides, both loss owners, first
and later rounds, both player projections, the minimal serialized fact and
refusal of voluntary, unrelated and nested loss contexts. This gives the private
runtime enough provenance to review normal combat-loss roots without inferring
the action source from battle presentation.

## Direct paid battle return — boundary v12

Boundary v12 adds one semantic fact for a direct, paid army attack: the active
battle will complete the paid action. It is emitted only for a single top-level
battle frame returning to `FinishAction`, with its resolving die owned by the
attacker. The ordinary `PlayerView.Battle` remains the source for public combat
state and face-down card backs. No saved frame, return token, executable cursor,
or hidden card identity is transferred.

This is not a general battle continuation contract. Relief-mode/event battles, nested
or suspended procedures, battle effects, and frames returning to a parent remain
unsupported. The private runtime reconstructs first-round card and any-round
ordinary reroll windows. A revealed card identity is used only after it becomes
public; a hidden opposing selection is sampled from public facts. Boundary v13
marked only first-round loss decisions; boundary v14 adds later-round provenance.
Sources: T18/T19 and BASE pp. 28–32. Public projection coverage is in
`ActionBoundaryBattleTests`; this does not by itself establish private runtime
reconstruction or BOT-02 acceptance.

## Configured Hunt extension — boundary v11

Boundary v11 composes reviewed movement, paid-card and declaration Hunt contexts
with Lords/Kings facts. Only these explicit semantic contexts are eligible;
arbitrary expansion/card programs, battle returns and nested program cursors remain
unsupported. See [T27-LORDS-03](../rules/T27-LORDS-03.md),
[T28-SOVEREIGNS](../rules/T28-SOVEREIGNS.md),
[T29-RULER-CHIEFTAINS](../rules/T29-RULER-CHIEFTAINS.md) and T31 integration.

`HuntView.DiceReduction` preserves Thranduil's paid Far-sighted movement effect;
the projection accepts it only with an awakened Thranduil and a resolving Ruler
Character die. Uglúk's reroll bonus is checked against his public location and
appears on rolled Fellowship movement and Nazgûl Strike Hunts, applying only to
rerolled dice. A Sméagol tile consumed on entry retains its known physical identity
in the removed zone. The bare We Shall Get It replacement offer uses its public
Hunt reaction window. These facts compose with private companion versions and
covered Kings assignments without reading hidden tile values. Nested card
effects still need separate semantic boundaries.

`FellowshipMoveProjectionTests` and `KingsChieftainHuntTests` verify the public
movement modifiers and both player views. Private reconstruction/parity and full
expansion composition remain separate BOT-02 acceptance checks.

## Active corruption fields introduced in v10

Boundary v10 also describes the ordinary Eye-paid corruption attempt while
it draws, replaces a public ignored tile, or awaits the final/sequential choice
(T28/T31, KINGS pp. 11–12, 19). The existing attempt's public sovereign, exact Eye,
draw limit/count and revealed ignored tiles describe the procedure. Shadow also
receives all its already drawn numeric tiles in draw order, separately from its
currently selectable tiles. Free Peoples never receives these identities, even
with Visible Corruption. Their unknown faces remain in the joint pool/attachment/
private-draw multiset. No frame, return token or program cursor is transmitted.

Only a single ordinary paid attempt is covered; event-origin and nested attempts
still omit the boundary. Reconstruction must retain the already paid Eye, current
private draw and ignored tiles without charging/drawing again. The pinned rules
regenerate the current choice and finish the action once. Required checks cover
both variants, hidden permutations, invalid/stale choices, known tile identities,
Lords Eye payment, real restart/replay, budget limits and complete continuations.
`CorruptionBoundaryProjectionTests` passed 94 cases across six module compositions,
both visibility variants and seven draw/choice windows. Fourteen real HTTP/restart/
receipt-replay cases passed for both visibility modes. The selected public run
passed 2,246 cases without skips on 2026-11-05; its owned database was removed.
Private reconstruction, bounds and strategy acceptance remain separate.

## Kings boundary v9

Boundary version 9 adds frame-free Kings phases/actions/paid steps and Ruler-die /
Dismay choices (T28/T29/T31, KINGS pp. 6–13, 17–19). Existing SovereignView and
RulerTokenView supply lifecycle, attempted-this-turn flags, visible tile identities
and public resources. New semantic facts retain Palantir/Dismay/Ruler-token/Ring
uses, the pending Ruler discard order, the token paying an ongoing action and
pending public political-removal obligations. No executable stack is transferred.

Hunt tile zones must not reveal hidden corruption assignments. For a viewer with
unseen attached tiles, base numeric tiles in the pool and those hidden attachments
form one unassigned face multiset. Each affected zone publishes only its hidden
count alongside its known tiles. The combined information is already available
from HuntPool and sovereign backs. Known Kings/visible tiles keep their existing
identities. A hypothetical assignment must conserve the entire configured pool,
keep known identities, and leave every initial sovereign below its resistance.
Hidden-pool/attachment permutations must preserve the entire FP response. Visible
Corruption still reveals assigned tiles only, never a pending private draw.
Event/nested corruption and suspended card/Hunt/battle procedures need separate reviewed contexts.
`KingsBoundaryProjectionTests` verifies both seats across six compositions and
hidden/visible assignments (71 cases); `KingsBoundaryPersistenceTests` verifies
six ordinary HTTP/restart/receipt-replay scenarios. All 2,109 selected Kings,
Lords, Council, Hunt, projection and privacy cases passed without skips on
2026-11-05, using an owned disposable database. Private reconstruction still
requires bounded sampling, impossible-world refusal, legality and continuation
parity; public projection checks alone do not establish BOT-02 acceptance.

## Lords and Council boundary v8

Boundary version 8 describes frame-free Lords/Council setup, ordinary
phases/actions, reviewed paid steps, special-die selection and the Vilya completion
offer. Sources and existing rule procedures are T23–T26, LORDS pp. 8–19, 30–31
and their character/card manifests. It adds public uses of replacement/one-use
abilities, the rolled dice counts and remaining special-die choice order, paid
Will of the West/Captain dice and Council's first Fellowship-die obligation.
Council tokens and special-die faces/removal flags remain in their existing views.
No resolution frames, effect cursor, saved return decision or opposing private
companion selection is transferred. Suspended Lords Hunts/card/battle procedures require separate reviewed contexts.

Shadow's character lifecycle facts omit unrevealed selectable companions in the
Fellowship. Their stable figure identities remain public. Own chosen versions and
already revealed definitions use the existing player/Fellowship fields; hidden
versions must be sampled independently from configured alternatives. A revealed
definition constrains any hypothesis. Setup before the version choice carries no
invented committed selection. Sméagol tiles keep exact public identities because
the two additional tiles are specifically targeted by You Know the Way; identical faces
alone would lose that distinction. The special We Shall Get It table/discard card
is separate from the shuffled event decks.

`LordsBoundaryProjectionTests` covers both seats, private version/card permutations,
no projection mutation, configured setup/homeland/token/dice/paid/Vilya roots and
explicit unsupported contexts (24 cases). `LordsBoundaryPersistenceTests` covers
nine ordinary HTTP/restart/receipt-replay cases. Both passed; package/client pins
are recorded in the [current checkpoint](STRATEGIC-BOTS.md). This is metadata
verification, not BOT-02 or strategic acceptance.

Contracts 0.35.0 add optional `ActionBoundaryView.FellowshipGuide` at boundary
version 19. It marks only the direct, paid Free Peoples companion-separation
continuation that leaves a Guide choice before the action can finish. The
marker is absent for card, Hunt, declaration, and other effect continuations.
It carries no frame, source history, private choice, or extra command authority.
Public content 1.0.35 and rules 0.15.15 pin this context.

Contracts 0.36.0 add declaration-boundary facts at version 20: public source IDs
already committed in a `fellowship.declared` reaction window, plus whether the
route's Fire and Shadow effect is still pending. Rules 0.15.17 uses them to
reconstruct public re-offers, an active `Gates Are Closed` Hunt and its deferred
route effect from either seat's observation; no private choice or resolution
frame is exposed. Public content 1.0.36 pins the contract.

Contracts 0.37.0 add `ActionBoundaryView.LurePlacement` at version 21. It marks
only the chooser-visible returnable Companion after Lure's elimination choice,
while its placement or resulting Guide decision is pending. The selected target
is sent to Free Peoples only, where that player already sees it in their own
effect choice; Shadow receives no target identity from this marker. Rules 0.15.18
rebuilds the two reviewed stages from the public card, resolving die, own choice
and current Companion location. Public content 1.0.37 pins the contract.

Rules 0.15.16 admits the public `fellowship.declared` reaction offer and its
Lords re-offer when earlier public responses are represented by the exact
remaining reaction IDs. It continues to omit unsupported active Hunt/card
continuations and carries no selected opposing choice.

Contracts 0.34.0 add optional `ActionBoundaryView.CompanionJourney` at boundary
version 18. It marks only the Free Peoples' paid Gwaihir or Swifter event while
its pinned companion journey or resulting Guide choice is pending. The ordinary
fact carries the source card, stage and public journey limits so a client can
rebuild the legal choice from the same player information; it contains no
resolution frames or selected movement. The Guide stage may carry queued public
political reactions from the journey; the initial Separation stage requires
none. Public content 1.0.34 and rules 0.15.13 first shipped this fact.

Rules 0.15.14 extends the same version-18 fact to the `OutsideCompanionJourneyStep`
in Book of Mazarbul (base), Fear! Fire! Foes! and Book of Mazarbul (Fate). It
appears only for the card's optional outside-Companion movement, with no
separation, zero-or-more groups, no fixed level or bonus, and no besieged
destination. The paid face is Muster or Event. The marker describes the
printed journey limits, not a frame, route, or private selection; the ordinary
Free Peoples options still determine the exact legal move. Other card programs
and journey steps remain unmarked. Source card rules: T21-FP-STR-01,
T21-FP-STR-02 and T32-FATE-CARDS.

Contracts 0.33.0 also exposes `BattleForceView.VoluntaryLossHits` to both sides
after a voluntary battle-loss choice resolves. Null means the choice has not
resolved; zero is a public decision to take no losses. The hit count lets
observation-only simulation validate the card's later combat modifier without
reading a saved effect frame or guessing from hidden history. Contracts 0.32.0
exposes `BattleForceView.OrderlyWithdrawal` after the public Lords combat choice;
the flag is set only for the Free Peoples force when its player accepted the hit
cancellation. It lets consumers restore the mandatory stop/retreat from public
facts without exposing a card identity or choice before reveal.

Contracts 0.31.0 provide optional `PlayerView.ActionBoundary` version 17. This is
ordinary own-side player information, also passed through the existing bot input
projection.
It does not grant command permission and requires no new UI controls. Protocol 2,
save schema 3 and game rules are unchanged. Older responses omit it; consumers
must treat absence or an unknown version as unsupported, never as empty state.

Version 14 retains the reviewed base/Fate ordinary phases/actions, hand limits,
paid army/recruitment/character
steps, excess removal, and reviewed choices in a Hunt caused by the paid
Fellowship movement action. Ordinary/paid army windows have no suspended frames.
The Hunt context supports rerolls, damage, public reaction windows, guide choice,
initial reveal-route choice, returning Hobbit placement and additional Stronghold
Hunts after that move's committed reveal route. Paid Orc Patrol, Isildur's
Bane, Foul Thing, Breaking Fellowship and the committed Nazgûl Strike Hunt have
their own CardHunt context with the same reviewed choice/reveal/Stronghold
windows. Breaking Fellowship uses the existing public
`HuntView.SeparatesCompanions` marker and exposes its direct companion-selection
continuation only when the card, tile, damage, owner and pinned effect step agree;
it adds no fields to the contract. Both the uncommitted revelation offer and the
active Balrog reaction after revelation use the same context. Declaration has its
own unpaid return context. Reviewed Hunts also support the nested Worn choice.
Version 13 marked an ordinary non-voluntary `combat` loss choice only in the
first round. Version 14 extended this to later rounds when it is the single
`BattleLoss` frame directly above a paid battle that returns to the action.
Version 15 also marks ordinary combat-card commitment windows in later rounds of
that same top-level paid battle. Existing BattleView and DecisionView provide
the public stage and choice; the boundary adds only the semantic paid-action
return. Version 16 adds a reviewed paid Lure of the Ring choice context. It
carries the public card ID and payment face only. The randomly selected Companion
stays in the active Free Peoples player's existing legal effect options; it is
not copied to the shared boundary. The projection admits only the pinned Lure
program's direct choice with a valid target, exact payment, non-Gollum path and
no pending Palantir draw. Other Lure continuations remain unsupported.
Voluntary/event losses, nested battles, other battle contexts, arbitrary
effect-discard returns, suspended Lords procedures and event/nested Kings
procedures still omit the boundary. Never serialize
a resolution stack or redacted save as player data.

The existing `ActionSequenceView` additionally exposes `InitialFigureIds`: public
on-board figures eligible at the start of a paid journey. Their identities are
already public; this list preserves eligibility after groups merge or move. It
contains no Fellowship/Council hidden versions. Ordinary sequence fields still
identify the owner, kind, exact payment, completed/remaining budget and used
figures/regions. Both players receive the context; only the decision owner gets
options and a decision token.

| Fact | Visibility and purpose |
| --- | --- |
| Fellowship declaration this turn, previous Minion entry, mandatory Hunt allocation | Public consequences affecting later eligibility; BASE turn/Fellowship/character rules, B02/B10/B15/B17. |
| Companion figure IDs and character lifecycle, including eliminated figures | Public in base/Fate, including the public Fate replacement. Unrevealed selectable companions are omitted for Shadow; own versions and public revelations retain their existing visibility. |
| Used army conversion and extra dice at next recovery | Public use of character abilities and their recovery consequences, B10/B02. |
| Ring action still pending | Public paid conversion; preserves its obligation through the action window, B05. |
| Known discards | Own discarded cards and opposing cards visibly played/revealed/discarded; `PublicDiscards` separately identifies what both sides know. Never derive an opposing discard from an earlier draw/inspection or hidden discard. Base/Fate discards do not recycle, B06. |
| Paid dice | Public resolving payments; retained until the whole action and any excess removal finish, BASE pp. 20–21 / B04. |
| Pending politics | Ordered public entry/attack/arrival/capture causes, nation and region; their consequences wait until action completion, B07. These are known past events, not predicted secret effects. |
| Fellowship movement progress | Previously offered guide windows, pending casualty consequences, voluntary revelation and the public origin/progress/companions of a returning Hobbit group. These are past public choices and obligations (B15–B19, BASE pp. 37–42), not private selections or executable effect steps. |
| Additional Stronghold Hunts | Committed public reveal route, ordered remaining Strongholds and the resolved movement Hunt's ordinary view. Current guide/casualty/departure facts describe the active extra Hunt. The original movement tile and current tile retain their distinct public identities. B18 / T16-HUNT; no effect program, cursor or private choice is transferred. |
| Hunt tile zones grouped by printed face | Public pool/prepared/drawn/spent/return-after-action/discard/removed/set-aside composition, B18/B19. Public special-tile IDs retain their card associations even for identical faces. No physical ordering, random samples or unrevealed Kings assignments. |
| Lure choice | Public card ID and payment face only; the random Companion target remains in the chooser's existing `EffectChoices`, not shared boundary facts. BASE pp. 22–23, 28–29; T21-SH-CHAR-03. |

`ActiveEffectView.Lifetime` additionally gives the already visible effect's applied
turn, scope, required companions and whether it is private to its owner. The same
visibility filter as the existing effect applies. Table-card descriptions without
a separate lasting modifier have no lifetime. These fields describe expiry, not
executable program counters or future effects.

The projector scans visible structured card facts, not prose, and never treats
knowledge of a card's identity as proof of its current hidden location. Projection
does not modify state or draw entropy. Hidden-hand/deck/discard permutations and
Hunt tile ordering must leave the opposing response unchanged. Tests are in
`ActionBoundaryProjectionTests`, `ActionBoundaryContinuationTests` and
`ActionSequencePersistenceTests`, `FellowshipMoveProjectionTests` and
`StrongholdHuntProjectionTests`, `CardHuntProjectionTests`, `CardHuntPersistenceTests`
and `HuntPersistenceTests`; hypothetical reconstruction belongs only in the
private runtime and must still sample unknown cards independently.

Version 1 omitted special-tile associations. Version 2 described ordinary action
roots only and has no paid-action provenance. Consumers must not interpret absent
new fields in an older response as empty obligations. Version 3 has no
Fellowship movement continuation context. Version 4 omits the committed reveal
route and additional Stronghold obligations. Version 5 has no direct event-card
Hunt context. Version 6 omits declaration continuations. Version 7 omits Lords/Council facts. Version 8 omits Kings and unassigned tile facts. Version 9 omits active ordinary corruption draws. Version 10 has no configured Lords/Kings Hunt composition; version 11 omits the direct paid battle return; version 12 has no battle-loss provenance; version 13 marks first-round losses only; version 14 extends loss provenance to later rounds; version 15 includes later-round card windows and the reviewed Breaking Fellowship card Hunt; version 16 includes the reviewed Lure choice; version 17 adds reviewed battle-loss and special-attack facts; version 18 marks paid Gwaihir/Swifter journeys, follow-up Guide choices, and the three reviewed OutsideCompanionJourneyStep event cards; version 19 marks direct paid companion separation followed by Guide selection; version 20 carries public committed reaction sources and the deferred Fire and Shadow obligation for reviewed Fellowship declaration continuations. Version 21 marks the Free Peoples’ visible Lure return-placement and Guide stages. Public content 1.0.37 and rules 0.15.18 pin the current contract; protocol and save schema remain unchanged.

`FellowshipMove` denotes only a paid ordinary move whose cost completes after
the Hunt. It must never be inferred merely because `HuntView` is present. Existing
Hunt/decision/report fields supply the current roll, tile, damage and window. The
new fields describe which guide windows have happened and returning companions'
public obligations; they contain no serialized resolution stack or return target.
A currently visible tile ID remains authoritative public identity, including when
an indistinguishable tile exists in another zone. Pool ordering stays hidden.

The `StrongholdHunts` context describes additional Stronghold Hunts after that paid
move's reveal route (B18, BASE pp. 40–42; FAQ 1.2 pp. 1–2; T16-HUNT). The public
route and remaining Strongholds retain their committed order. The already resolved
movement Hunt remains public context until the whole action finishes. Each extra
Hunt completes its own guide/casualty/reaction choices before the next tile; an Eye
has zero damage here and another reveal icon does not repeat revelation. The
original movement die stays resolving until completion; corruption victory ends
the chain immediately. These facts are shared with both players, while pending
tokens/options retain their existing ownership. Card-origin/nested effect and
declaration continuations require separate reviewed contexts. No rules change is
authorized by this extension.

## Paid event-card Hunt continuation

This reviewed boundary covers paid Orc Patrol, Isildur's Bane, Foul Thing
and the committed Nazgûl Strike Hunt (T21-SH-CHAR-01/02, B18). Shared facts identify the publicly played card,
the selected Character/Event payment, pending Palantir draw and current public
guide/casualty/departure progress. After revelation they also retain the resolved
card Hunt, committed route and ordered remaining Strongholds. The immutable card
definition supplies its tile/damage rules; no effect program or saved cursor
is transmitted. Card costs finish once for Shadow, never as Fellowship movement.

Existing own-side EffectTrail remains own-side information. Hypotheses may retain
that copied presentation trail but must not invent opposing private selections.
The Palantir obligation records its public presence when the event was paid;
the rules still check whether it remains on the table before drawing. Declaration
Hunts, other event programs and currently executing nested effects remain outside
this reviewed context. This is reconstruction work, not strategic acceptance.

The Nazgûl Strike continuation uses the same v6 fields and Contracts 0.20.0;
there is no wire-format, public-content or client change. Its public card identity
selects the pinned roll procedure instead of a direct tile draw. Existing HuntView
fields preserve rolled dice, movement/effect bonuses, successes and reroll sources.
The original event resumes only after the Hunt and any Stronghold consequences;
there is no second payment, Fellowship progress or added Free Peoples Hunt die.
Rules source: [T21-SH-CHAR-02](../rules/T21-SH-CHAR-02.md), card 11 and BASE p. 41.
The earlier Nazgûl travel/Strike choice and active nested programs still omit the
boundary. `StrikeHuntProjectionTests` checks those refusals and unsupported rule
flags; `CardHuntPersistenceTests` also crosses Shadow/Free Peoples decisions,
failed rolls, Staff windows and the final Palantir draw through host restarts.


## Active Balrog Hunt after revelation

Pure rules 0.6.2 also describes the already chosen Balrog table-card Hunt after
an ordinary paid move or one of the four reviewed paid event Hunts. Existing
HuntView.SourceId identifies Balrog; the movement/card revelation context retains
the resolved source Hunt, committed route and the entire still-unconsumed
Stronghold queue. The queue may be empty when the route's Moria is no longer
Shadow-controlled. There is no new DTO field, cursor, Contracts/content version,
client change or bot-only channel.

The reviewed projector requires the completed public reaction/program, discarded
Balrog card, normal damage with ignored Eyes, and a revelation return to the
original paid Hunt. Bare damage, guide/Hobbit and public Hunt reaction choices
are supported. Balrog completes before the Stronghold queue without another
revelation or payment. The original FP move eventually puts its die in the Hunt;
a paid Shadow event instead spends its original die after its remaining effects.
Public card provenance and each seat's existing EffectTrail keep the visibility
boundary. No opposing private selections are inferred.

Source: [T21-SH-CHAR-03](../rules/T21-SH-CHAR-03.md), card 17 and BASE pp. 37–43.
Balrog during declaration and other currently executing nested programs remain unsupported. Tests:
`BalrogHuntProjectionTests`, `BalrogHuntPersistenceTests`; private reconstruction
and strength acceptance remain separate.

## Uncommitted revelation reaction

Pure rules 0.6.3 additionally describes the ordinary Shadow `fellowship.revealed`
offer before choosing the Balrog table card, after the same paid movement/four
paid event sources. The existing resolved source Hunt and current HuntView are
identical descriptions of one resolved Hunt and one spent tile. The complete
public route and all unconsumed Strongholds remain in `StrongholdHunts`, even
when that queue is empty. Current guide/casualty/departure facts belong to the
source Hunt at this point. No completed Balrog selection or discard exists yet.

Both ordinary Respond and Decline remain available to Shadow. Respond pays the
table-card cost and starts Balrog before the entire queue; Decline retains the
card and starts the first Stronghold, or completes the source action when there
are none. Neither response reapplies the source tile or reveal route. The same
original payment remains resolving until all consequences and any Palantir draw
finish. No decision is automated and no new controls, DTO fields, Contracts or
public-content version are required. Each seat's existing EffectTrail remains
private to that seat.

Sources: B18 / BASE pp. 40–42 and [T21-SH-CHAR-03](../rules/T21-SH-CHAR-03.md),
card 17; for example, a route through FP-controlled Moria can offer Balrog with
no Stronghold Hunt, and declining then draws no tile. No rule interpretation
changes. `RevelationHuntProjectionTests` covers copied public context, hidden
permutations and refusal of unsupported continuations; `RevelationHuntPersistenceTests`
checks both answers across real host restarts and idempotent receipt replay.
Declaration, other executing nested programs and Lords/Kings remain outside
this reconstruction boundary.


## Ordinary phases, hand limits and nested Worn choice

Pure rules 0.6.4 reuses v6 facts for frame-free base/Fate Fellowship-phase and
Hunt-allocation decisions, recovery hand limits and ordinary post-draw hand
limits. Phase and exact resolving payment determine the return. Recovery discards
continue FP then Shadow before Fellowship; post-draw discards complete the same
action cost. Resuming an arbitrary suspended event is not implied by a hand-limit
window. Sources: B02/B06 and the existing TurnFlow/CardFlow rules.

The current `EffectChoice` source also identifies Worn with Sorrow and Toil
inside a reviewed movement/card/Stronghold/Balrog Hunt. Its public casualty, table
card and current Hunt context determine the pinned choice and parent return.
Worn keeps its table card, offers a public table Character card, random opposing
hand Character (if any) or keeping cards. The random-hand option depends only on
the public hand count; it never reveals whether a Character card is actually
present. Its nested response adds no Hunt, movement or payment. Own EffectTrail
stays own-side information. Source: T21-SH-CHAR-03 card 15 / B18; the existing
WornDiscardStep remains authoritative.

PhaseBoundaryProjectionTests, WornHuntProjectionTests and PhaseWornPersistenceTests
exercise the shared player boundary, both-seat visibility and restart/replay.
No new DTO fields, Contracts/content/client source or game-rule change is required.


## Declaration continuation — boundary v7

Version 7 adds `FellowshipDeclaration`: the committed public route, remaining
Stronghold Hunts and the already public progress of the current Hunt. This is an
unpaid declaration in the Fellowship phase. The ordinary public Balrog offer,
executing Balrog Hunt, casualty/guide/placement/replacement choices (including Worn)
and subsequent Stronghold Hunts all return to that phase. The original declaration,
healing and expiry have already occurred and must not be replayed.

No saved decision token, program counter or resolution frame is exposed. Future
hypothetical decision tokens belong to the hypothetical world; they are not claims
about unseen authoritative counters. Root proposals still use the exact observed
decision token/revision. The host persists and resumes its actual suspended token.

Sources: B18 / BASE pp. 40–42 and T21-SH-CHAR-03 card 17, as implemented by
FellowshipFlow, BalrogHuntStep and HuntRuntime. Balrog ignores Eyes; after declaration
it ignores a printed reveal icon at an originally FP City/Stronghold, including
a captured one. Otherwise revelation uses the entire declared route before any
additional Stronghold tile. Declining Balrog does not reveal or draw a tile.
No game rule or save schema changes. Contracts 0.21.0, public content 1.0.23 and
pure rules 0.7.0 pin v7; older v6 inputs do not describe declaration continuations.

DeclarationHuntProjectionTests verify ordinary own-side projections, secret/token
isolation and inconsistent contexts; DeclarationHuntPersistenceTests verify both
views, original return tokens and idempotent commands across real host restarts.
