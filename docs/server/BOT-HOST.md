# Embedded bot host

BOT-18 implements host infrastructure, not acceptance of a playing policy. The
operator enables `RuntimeEnabled` and `RoomsEnabled`, pins `RuntimeVersion` and
`AcceptedPolicyVersion`, and supplies an absolute `RuntimeAssembly` path. Runtime
ABI 2 advertises accepted policy/module coverage separately from available module
definitions. Both faction profiles must be accepted. The real private library
continues to report no accepted policies until gameplay acceptance.

The host persists CPU assignments and polls them with a fixed worker count.
Each worker owns a library instance, takes a short database lease and increments
the assignment generation. Room changes and starts invalidate older generations.
No player read or external service is needed to recover after restart. Admission,
readiness and start recheck configuration, policy/runtime pins and worker health.
CPU heartbeats do not renew human waiting-room presence.

Runtime input is assembled in a repeatable-read transaction using the ordinary
own-side Projection, ActionLogQuery/Projection and BattleReportStore. History
starts after the last accepted memory's delivered cursor; excess context causes
an explicit refusal, never silent truncation. Preview requests are bounded
continuations: the host supplies ordinary PreviewService results at the same
revision, with their commands, and retries the pure runtime. Neither authoritative
state nor database/network handles cross the ABI. Invocation IDs/seeds are
independent of hidden state. Human decisions and finished games are not evaluated.

The host journals an exact command, invocation ID and proposed opaque memory
before submission. A recovered pending proposal is submitted unchanged. Under
the match lock it rechecks the assignment, generation, lease, seat, memory version
and command fingerprint, then uses the normal engine and receipt path. The game
state, receipt, accepted memory/version and proposal result commit in one database
transaction. Rejection never advances memory. A crash before commit rolls all of
them back; a crash after commit cannot repeat either effect. Final moves use the
same path. Private memory stays outside domain state, events, replays and player
DTOs. Only the latest proposal is retained for each assignment.

Lock order is room or match, then runtime job; claiming and journaling lock only
the job. No runtime computation holds any database lock. Work is bounded per
invocation and globally. A timed-out library that ignores cancellation occupies
its worker until it returns; its late answer is discarded. The host does not
spawn replacement computations without bound and does not block shutdown on that
library. This is trusted in-process code, not a process sandbox.

Tests must cover real PostgreSQL, the public policy-free ABI fixture, both seats,
nested/final decisions, hidden-information parity, ordinary legality, stale work,
removal/configuration/readiness races, failure recovery and actual Kestrel restart
on both sides of the atomic commit. Fixture acceptance is not gameplay acceptance
and authorizes no deployment. See [runtime ABI](BOT-RUNTIME.md) and
[strategic architecture](STRATEGIC-BOTS.md).


## Verification — 2026-10-28

The PostgreSQL/server regression passed 253 cases; final boundary and coexistence
batches passed 16 and 17 respectively (258 distinct cases across those overlapping
batches, no skips). This includes 40 embedded host/loader cases: eight actual
Kestrel kill/restart boundaries with ordinary and game-ending moves, two missing/
throwing-library recoveries, both factions, ordinary information parity, nested
reactions, stale/expired work, removal and human/save isolation. Additional
checks cover automatic readiness, cooperative host shutdown with an uncooperative
library, bounded worker slots, and the legacy/runtime room coexistence gate.

Private verification passed 682 C# and nine Python cases. The actual 0.2.0 private
DLL passed four faction/configuration ABI cases and remains unready with no
accepted policies. Public server publish excludes private DLLs and the test
fixture; private publish contains the four bot libraries, its two pinned rule
libraries and both licenses. No client package/schema change, publication or
real-strategy gameplay acceptance is claimed. BOT-19 owns packaging/upgrades;
BOT-20 owns real-policy integration.
