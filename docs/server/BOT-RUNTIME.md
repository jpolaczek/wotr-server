# Embedded runtime ABI — version 2

Owner decision 2026-10-27 replaces external bot hosting. The operator supplies a
private `Wotr.Bot.Runtime.dll`; public builds do not reference it. The host loads
`Wotr.Bot.Runtime.RuntimeEntryPoint` with a parameterless constructor and exactly
two public instance methods: `string Describe()` and
`string Decide(string requestJson, CancellationToken cancellation)`.

Only strings and framework cancellation cross the assembly boundary. The runtime
loads its pinned Contracts/Domain dependencies in its own assembly context. It
receives no live game object, service provider, database or transport callback.
This is trusted in-process code, not a security sandbox.

JSON uses camelCase property names and string enums. `Describe` returns
`abiVersion`, `runtimeVersion`, `protocolVersion`, `rulesVersion`, `contentVersion`,
`gameplayReady`, `profiles`, `modules` and `policies`. Module definitions alone
do not establish accepted coverage. Each policy entry declares `profileId`,
`policyVersion` and accepted `features`; gameplay readiness requires both faction
entries, the operator policy/runtime pins and coverage of the normalized configuration. The current runtime
reports `gameplayReady: false`, `policies: []` and refuses to fabricate a playing policy.

The decision request contains:

- `abiVersion`, unique `invocationId`, `runtimeVersion`, `protocolVersion`,
  `rulesVersion`, `contentVersion`;
- `matchId` and assigned `side`, checked against `player`;
- `player`: the ordinary, copied own-side PlayerView, including its current
  decision/options and MatchConfiguration;
- `history`: own-side ActionLogStep array, `reports`: own-side BattleReportView
  array, and `previews`: ordinary ActionPreview array for the current revision,
  paired by index with `previewCommands`;
- `memory`: optional opaque runtime-owned JSON string, `sampleSeed` and
  `maximumWork` (1–100,000).

The host must create every surface using ordinary player visibility at a
consistent revision. DTO identity checks are not authorization. History/report
steps may not be newer than the view, and previews must match its revision.
The input is bounded to 16 MiB UTF-8, history to 4,096 steps, reports to 128 and
previews to 16. History is incremental from the last accepted-memory cursor.
Overflow refuses the evaluation explicitly; the host never silently drops facts. Explicit `[]` is valid; missing arrays are not.

The result contains `abiVersion`, `invocationId`, `status` (`Proposal`,
`NeedPreviews`, `NoDecision` or `Refused`), fixed `reason`, selected `modules`, `policyVersion`,
optional ordinary `command` and optional opaque `proposedMemory`. Output is
bounded to 4 MiB UTF-8. The host verifies correlation, seat/decision/revision and
normal command legality before committing. It saves proposed memory only for an
accepted command in the same transaction as game state and receipt. The command
ID must equal the host invocation ID. No response can consume resources by itself.

`NeedPreviews` carries 1–8 ordinary `previewRequests` and no command or memory.
The host computes previews using the same service as the player API, checks the
revision/decision again and retries with paired commands/results. At most two
preview rounds (16 results) and three evaluations share a five-second computation
budget and 99,999 work units (33,333 each). The invocation/seed/prior memory remain
unchanged. A changed observation requires a fresh invocation, without a move.

Runtime computation observes cooperative cancellation and the existing five-second
budget. Host scheduling/timeouts, readiness, provenance and durable accepted-memory
recovery are implemented by [the embedded host](BOT-HOST.md). No library method starts a worker, HTTP server or thread
that watches games. Missing/incompatible/failed libraries keep CPU unavailable.

Module selection uses the shared rules' configuration dependencies, including
nullable Dismay's default. One base module plus configured expansion/variant
modules serves both faction profiles. BOT-09/10/11 add strategic module behavior;
selection alone does not establish mechanic acceptance.

## Optional server loading

`Wotr:Bots:RuntimeAssembly` is an absolute operator-only path; an unset path
returns `NotConfigured`. Optional `Wotr:Bots:RuntimeVersion` pins the required
build version. Invalid files, entrypoints and incompatible metadata return a safe
unavailable status. Loading alone starts no gameplay. `RuntimeEnabled=true`
enables the bounded host scheduler and requires a runtime version pin;
`RoomsEnabled` and accepted policy/module settings gate admission. Each worker
uses its own loaded instance. All computation happens outside mutation locks.
A library ignoring cancellation retains its slot until completion, and its late
result is discarded; host shutdown does not wait for that uncooperative call.

The host validates response correlation, own decision/revision, available command
kind and response shape. This is a proposal gate, not full rule validation: the host
invokes the ordinary authoritative command path under its normal locks.
Exceptions are reduced to fixed refusal codes; private exception text is not
returned. Framework-only ABI values permit the runtime's own pinned rule assembly
to coexist with the host's game engine without reference sharing.

## Verification boundary

`EmbeddedBotRuntimeTests` tests loading, copying, cancellation and malformed
responses. `BotHost*Tests` exercise real PostgreSQL and a public policy-free
fixture DLL, including actual Kestrel termination/restart at journal and commit
boundaries. The private full verification and actual private-DLL smoke check are
recorded separately. These checks do not accept strategic strength or authorize
production CPU admission. No private artifact is a public build dependency.
