# Player rule facts at an action boundary

Contracts 0.17.0 provide optional `PlayerView.ActionBoundary` version 3. It is ordinary
own-side player information, also passed through the existing bot input projection.
It does not grant command permission and requires no new UI controls. Protocol 2,
save schema 3 and game rules are unchanged. Older responses omit it; consumers
must treat absence or an unknown version as unsupported, never as empty state.

Version 3 describes base/Fate ordinary actions, paid army/recruitment/character
steps and excess removal after a paid action. These windows have no suspended
frames or discard continuation. Lords, Kings, setup, battles, Hunt procedures,
pending discards and suspended reactions still omit the boundary. Never serialize
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
| Hunt tile zones grouped by printed face | Public pool/prepared/discard/removed/set-aside composition, B18/B19. Public special-tile IDs retain their card associations even for identical faces. No physical ordering, random samples or unrevealed Kings assignments. |

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
`ActionSequencePersistenceTests`; hypothetical reconstruction belongs only in the
private runtime and must still sample unknown cards independently.

Version 1 omitted special-tile associations. Version 2 described ordinary action
roots only and has no paid-action provenance. Consumers must not interpret absent
v3 fields in an older response as empty obligations. Public content 1.0.19 and
pure rules 0.3.0 pin this contract; game rules, protocol and save schema are unchanged.
