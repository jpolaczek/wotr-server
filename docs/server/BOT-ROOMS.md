# Saved PvE rooms

BOT-12 adds server-owned CPU reservations to ordinary rooms. Claim delivery and
the private worker remain BOT-17/BOT-18. No strategy, hypothetical position,
memory or command journal is stored here.

## Admission

`PUT /api/rooms/{id}/bot` uses the human session and accepts
`{ "requestId": "<32 hexadecimal UUID characters>", "expectedVersion": 1 }`.
Only a seated, nontechnical host can reserve the empty opposite seat before start.
The server selects `shadow-strong` or `free-peoples-strong`; the request contains
no service URL, difficulty or arbitrary policy selector.

Admission defaults to disabled. It requires `Wotr:Bots:Enabled=true`, the
[BOT-16 identity settings](BOT-AUTHORIZATION.md), `Wotr:Bots:RoomsEnabled=true`
and an explicit `Wotr:Bots:AcceptedPolicyVersion`. `Wotr:Bots:Features` names the
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

Reservation does not mint a credential or acquire a lease. BOT-17 must claim a
pending assignment under the room lock, verify its service/profile/version and
current configuration, register the BOT-16 grant using the assignment ID, and
issue a scoped credential. Its participant is
`BotCredentialStore.Participant(assignmentId)`; registration recognizes that
existing technical account. Claim must reject cancelled/closed/moved seats.
BOT-12 introduces no public claim endpoint or production worker.

The scoped seat uses ordinary `PUT /api/rooms/{id}/ready`. The server requires
the matching grant, service and policy/configuration and saves the accepted
generation. An ordinary session cannot ready a CPU reservation. Configuration
changes clear acceptance; unsupported bot configurations are refused.
Host Start requires both current-version acceptances, a supported policy version
and live matching grant. It checks the database clock after waiting for the grant
lock. Revoke/reassign/expiry cannot slip through a queued start. Start creates the
normal recorded match, binds assignment/grant to it and records kind `pve` in one
transaction. Captured pre-start authority fails the later BOT-16 scope recheck.

Existing Remove, host Leave and moving the host to Observer cancel an unstarted
CPU reservation and revoke its grant atomically. Started seats stay fixed. The
room sweeper removes abandoned reservations through foreign-key cascades; bot
reads/readiness do not refresh human presence. Match deletion removes assignment
and grant, while the statistics ledger survives. BOT-17 must use room -> grant
locking for lifecycle changes and owns lease renewal, live health and delayed
claims. Tests use a trusted claim stub and then the actual scoped readiness route.

## Contracts and compatibility

Contracts **0.15.0** add `AddRoomBotRequest`, `SeatOccupantKind`, `BotSeatStatus`
and `BotSeatView`. `RoomView` exposes occupant kinds and CPU side/profile/policy
version. Joining/Ready describe configuration acceptance; live worker health is
BOT-17. No assignment secrets or private plans are exposed. `MatchSummary.IsPve`
and `.Bot` identify saved PvE. Public content **1.0.16** pins Contracts 0.15.0;
its public data is unchanged. Protocol 2 and save schema 3 are unchanged.

Migration **018_pve_rooms.sql** adds assignments and the `pve` ledger kind. Old
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
the real private process. BOT-13 owns minimal Add CPU/seat-status client controls.
