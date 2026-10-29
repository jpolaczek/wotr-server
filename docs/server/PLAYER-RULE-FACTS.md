# Player rule facts at an action boundary

Contracts 0.16.0 add optional `PlayerView.ActionBoundary` version 1. It is ordinary
own-side player information, also passed through the existing bot input projection.
It does not grant command permission and requires no new UI controls. Protocol 2,
save schema 3 and game rules are unchanged. Older responses omit it; consumers
must treat absence or an unknown version as unsupported, never as empty state.

Version 1 describes uninterrupted base/Fate action windows only. Lords, Kings,
setup, battles, Hunt procedures, paid multi-step actions, pending discards and
suspended reactions do not receive this boundary. Future support needs explicit
semantic contracts for those procedures; never serialize a resolution stack or
redacted save as player data.

| Fact | Visibility and purpose |
| --- | --- |
| Fellowship declaration this turn, previous Minion entry, mandatory Hunt allocation | Public consequences affecting later eligibility; BASE turn/Fellowship/character rules, B02/B10/B15/B17. |
| Companion figure IDs and character lifecycle, including eliminated figures | Public in base/Fate, including the public Fate replacement. Private Council versions are excluded by the module gate. |
| Used army conversion and extra dice at next recovery | Public use of character abilities and their recovery consequences, B10/B02. |
| Ring action still pending | Public paid conversion; preserves its obligation through the action window, B05. |
| Known discards | Own discarded cards and opposing cards visibly played/revealed/discarded; `PublicDiscards` separately identifies what both sides know. Never derive an opposing discard from an earlier draw/inspection or hidden discard. Base/Fate discards do not recycle, B06. |
| Hunt tile zones grouped by printed face | Public pool/prepared/discard/removed/set-aside composition, B18/B19. No physical ordering, random samples or unrevealed Kings assignments. |

`ActiveEffectView.Lifetime` additionally gives the already visible effect's applied
turn, scope, required companions and whether it is private to its owner. The same
visibility filter as the existing effect applies. Table-card descriptions without
a separate lasting modifier have no lifetime. These fields describe expiry, not
executable program counters or future effects.

The projector scans visible structured card facts, not prose, and never treats
knowledge of a card's identity as proof of its current hidden location. Projection
does not modify state or draw entropy. Hidden-hand/deck/discard permutations and
Hunt tile ordering must leave the opposing response unchanged. Tests are in
`ActionBoundaryProjectionTests`; hypothetical reconstruction belongs only in the
private runtime and must still sample unknown cards independently.
