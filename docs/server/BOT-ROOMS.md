# Saved PvE rooms

The host reserves the opposite seat for one of two faction policies. The private
runtime runs inside the game server. [BOT-HOST](BOT-HOST.md) specifies scheduling,
readiness, opaque memory and atomic command recovery. Legacy BOT-16 HTTP credentials
remain disabled by default and are not used by this embedded path.

## Admission

`PUT /api/rooms/{id}/bot` uses the human session and accepts
`{ "requestId": "<32 hexadecimal UUID characters>", "expectedVersion": 1, "thinkingMode": "Fast" }`.

**CPU pace (BOT-37, contracts 0.75.0).** `thinkingMode` is `Fast` (default: about three seconds a
decision, at most five) or `Thorough` (it looks further ahead: about ten, at most fifteen); both
are the same opponent. `PUT /api/rooms/{id}/bot/thinking` with
`{ "expectedVersion": n, "thinkingMode": "Thorough" }` lets the host change it while the room
waits: the room version rises and both confirmations are cleared (the CPU confirms again at once).
After the start it answers `MatchAlreadyStarted`; without a CPU `BotNotSeated`. The pace is saved
in the room state with the game (`bot.thinkingMode`); a room written before it reads as Fast.
`BotSeatView.ThinkingMode` shows it.
Only a seated, nontechnical host can reserve the empty opposite seat before start.
The server selects `shadow-strong` or `free-peoples-strong`; the request contains
no service URL, difficulty or arbitrary policy selector.

Admission defaults to disabled. Embedded admission requires
`Wotr:Bots:RuntimeEnabled=true`, a verified `RuntimeManifest` and
`RuntimeManifestSha256`, a `RuntimeVersion` pin, `RoomsEnabled=true` and an
explicit `AcceptedPolicyVersion`; see [operator packages](BOT-OPERATIONS.md).
Raw `RuntimeAssembly` loading requires the explicit test-only
`AllowUnpackagedRuntime` opt-in. The library must
report gameplay readiness and both accepted faction policies. `Wotr:Bots:Features` names the
operator's accepted coverage, independently constrained by released modules.
These settings do not establish strategic acceptance: current private policies
remain unavailable. Tests use a fixture version; transport checks alone do not
justify enabling admission in production.

Under the existing room row lock, admission validates the request, creates a
non-login technical participant, takes the seat, invalidates both Ready states,
increments the room version and inserts `bot_assignments`. All commit together.
A concurrent human join cannot replace the reservation. An identical request
returns the current room; a changed expected version gives `BotRequestConflict`.
A cancelled request gives `BotReservationCancelled`, even if the seat is free.

Other refusals: `InvalidBotRequest`, `PlayerRequired`, `HostRequired`,
`HumanHostRequired`, `SeatTaken`, `StaleConfiguration`, `MatchAlreadyStarted`,
`ExpansionsUnavailable`, `BotUnavailable`, `BotConfigurationUnsupported`.
Nonparticipants/closed rooms retain not-found behavior. The bot operation
allowlist denies this human-only route.

## Readiness and lifecycle

Reservation creates a durable runtime job, without a network credential. A bounded
host worker claims the job, then checks the room version, configuration, runtime/
policy pins and current lease under room -> job locks. Only this internal path
readies a runtime CPU; an ordinary user or legacy seat session cannot do so.
CPU readiness never refreshes `rooms.seen_at`.

Host Start requires both current-version acceptances and fresh worker health,
checking the database clock after acquiring the job lock. Start creates the
ordinary recorded PvE match and binds the assignment in one transaction. It also
invalidates any captured pre-start evaluation generation. Configuration changes,
Remove, host Leave and moving the host to Observer invalidate earlier work;
removal cancels the unstarted reservation. Started seats remain fixed.

The sweeper deletes abandoned reservations through foreign-key cascades. Match
deletion cascades assignment, runtime job and opaque proposal/memory while the
statistics ledger survives. Restart resumes persisted jobs without a player GET.
Unavailable/failed runtimes preserve the pending decision; no demo fallback or
implicit pass is used. The older grant-based path remains covered by BOT-12/16
regression tests when explicitly configured independently of runtime mode.

## Contracts and compatibility

Contracts **0.15.0** add `AddRoomBotRequest`, `SeatOccupantKind`, `BotSeatStatus`
and `BotSeatView`. `RoomView` exposes occupant kinds and CPU side/profile/policy
version. Joining/Ready/Unavailable reflect configuration acceptance and current runtime
health; Start always checks durable health again under lock. No assignment secrets or private plans are exposed. `MatchSummary.IsPve`
and `.Bot` identify saved PvE. Public content **1.0.16** pins Contracts 0.15.0;
its public data is unchanged. Protocol 2 and save schema 3 are unchanged.

Migration **018_pve_rooms.sql** adds assignments and the `pve` ledger kind.
**019_embedded_bot_runtime.sql** adds pinned runtime versions, leased jobs and
the opaque command/memory journal. Old
rooms deserialize without a CPU reservation; human/test/demo matches keep their
kinds. `AdminSummaryView.PveGames/PveFinished` count PvE separately; human game/win
statistics exclude it. New PvE never invokes the one-demo-per-owner replacement.
Multiple PvE saves coexist with human games and the legacy replaceable demo.

Commands, receipts, own-side views/history, observer authorization and replay
recording/export retain their existing paths. Humans and authorized observers can
export completed PvE replays. Bot credentials still cannot access observer
perspectives or replay exports. CPU appears as CPU in those records.

## Verification and handoff

`PveRoomTests`, `PveSeatRaceTests`, `PveSaveCompatibilityTests` use isolated real
PostgreSQL and actual ASP.NET routes/stores. They cover both sides, retries,
version/release/capability gates, readiness, cancellation, admission/configuration/
start/removal races, fresh-clock expiry/revocation, abandoned rooms, three PvE
saves with a human game and legacy demo, restart, commands/receipts/privacy/history
and completed replay. The replay fixture seeds a verified near-victory position;
it does not demonstrate strategic gameplay. BOT-20 repeats the integration with
the real private policies loaded in the host. BOT-13 implements minimal Add CPU/seat-status client controls, current-version
Ready/Start, idempotent invitation retries and multiple saved PvE games. It reuses
the shared opponent presenters; the main-menu side click opens a waiting room.
A public embedded ABI fixture in the test-only seeder verifies these client paths
without distributing private bot code or asserting strategic readiness.

The implementation at `4b345b6` passed 20 focused PvE/options cases and the final
140-case room, correspondence, replay, authorization, migration, statistics and
release regression, with no skipped cases. Dependency packaging passed seven
Python checks; the client bootstrap passed 16. Contracts/content and the server/
seeder archives were built from that commit and verified from a local channel.
Client commit `8e6d2579` on `codex/bot-room-contracts` pins those exact archives
and records the UI handoff. They have not been published; no Unity gameplay or
real private worker acceptance is claimed by these checks.

BOT-18 additionally uses `BotHostRoomTests`, `BotHostMemoryTests`,
`BotHostInputTests`, `BotHostSchedulingTests` and `BotHostRecoveryTests` with the
public ABI fixture and actual Kestrel restart. Exact current results are recorded
in the local task evidence; earlier BOT-12 counts above are historical regression
evidence, not acceptance of the embedded worker or real policies.
