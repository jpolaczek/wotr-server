# Player rule facts at an action boundary

Contracts 0.20.0 provide optional `PlayerView.ActionBoundary` version 6. It is ordinary
own-side player information, also passed through the existing bot input projection.
It does not grant command permission and requires no new UI controls. Protocol 2,
save schema 3 and game rules are unchanged. Older responses omit it; consumers
must treat absence or an unknown version as unsupported, never as empty state.

Version 6 describes base/Fate ordinary actions, paid army/recruitment/character
steps, excess removal, and reviewed choices in a Hunt caused by the paid
Fellowship movement action. Ordinary/paid army windows have no suspended frames.
The Hunt context supports rerolls, damage, public reaction windows, guide choice,
initial reveal-route choice, returning Hobbit placement and additional Stronghold
Hunts after that move's committed reveal route. Paid Orc Patrol, Isildur's
Bane, Foul Thing and the committed Nazgûl Strike Hunt have their own CardHunt context with the same reviewed
choice/reveal/Stronghold windows. Their active Balrog reaction after revelation
uses the same context. Other card-origin/declaration Hunts,
reactions already executing a nested effect,
pending discards, battles and Lords/Kings still omit the boundary. Never serialize
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
| Companion figure IDs and character lifecycle, including eliminated figures | Public in base/Fate, including the public Fate replacement. Private Council versions are excluded by the module gate. |
| Used army conversion and extra dice at next recovery | Public use of character abilities and their recovery consequences, B10/B02. |
| Ring action still pending | Public paid conversion; preserves its obligation through the action window, B05. |
| Known discards | Own discarded cards and opposing cards visibly played/revealed/discarded; `PublicDiscards` separately identifies what both sides know. Never derive an opposing discard from an earlier draw/inspection or hidden discard. Base/Fate discards do not recycle, B06. |
| Paid dice | Public resolving payments; retained until the whole action and any excess removal finish, BASE pp. 20–21 / B04. |
| Pending politics | Ordered public entry/attack/arrival/capture causes, nation and region; their consequences wait until action completion, B07. These are known past events, not predicted secret effects. |
| Fellowship movement progress | Previously offered guide windows, pending casualty consequences, voluntary revelation and the public origin/progress/companions of a returning Hobbit group. These are past public choices and obligations (B15–B19, BASE pp. 37–42), not private selections or executable effect steps. |
| Additional Stronghold Hunts | Committed public reveal route, ordered remaining Strongholds and the resolved movement Hunt's ordinary view. Current guide/casualty/departure facts describe the active extra Hunt. The original movement tile and current tile retain their distinct public identities. B18 / T16-HUNT; no effect program, cursor or private choice is transferred. |
| Hunt tile zones grouped by printed face | Public pool/prepared/drawn/spent/return-after-action/discard/removed/set-aside composition, B18/B19. Public special-tile IDs retain their card associations even for identical faces. No physical ordering, random samples or unrevealed Kings assignments. |

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
Hunt context. Public content 1.0.22 and pure rules 0.6.2 pin this contract; game rules, protocol and save schema are unchanged.

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
Balrog during declaration, the earlier uncommitted revelation reaction offer and
other currently executing nested programs remain unsupported. Tests:
`BalrogHuntProjectionTests`, `BalrogHuntPersistenceTests`; private reconstruction
and strength acceptance remain separate.
