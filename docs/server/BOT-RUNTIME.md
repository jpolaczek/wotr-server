# Embedded runtime ABI — version 1

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
`gameplayReady`, `profiles` and `modules`. Profiles/modules declare the shape of
the runtime; they do not establish accepted policy coverage. The current runtime
reports `gameplayReady: false` and refuses to fabricate a playing policy.

The decision request contains:

- `abiVersion`, unique `invocationId`, `runtimeVersion`, `protocolVersion`,
  `rulesVersion`, `contentVersion`;
- `matchId` and assigned `side`, checked against `player`;
- `player`: the ordinary, copied own-side PlayerView, including its current
  decision/options and MatchConfiguration;
- `history`: own-side ActionLogStep array, `reports`: own-side BattleReportView
  array, and `previews`: ordinary ActionPreview array for the current revision;
- `memory`: optional opaque runtime-owned JSON string, `sampleSeed` and
  `maximumWork` (1–100,000).

The host must create every surface using ordinary player visibility at a
consistent revision. DTO identity checks are not authorization. History/report
steps may not be newer than the view, and previews must match its revision.
The input is bounded to 16 MiB UTF-8, history to 4,096 steps, reports to 128 and
previews to 256. Larger histories require host recovery/compaction of already
observed facts, never secret state. Explicit `[]` is valid; missing arrays are not.

The result contains `abiVersion`, `invocationId`, `status` (`Proposal`,
`NoDecision` or `Refused`), fixed `reason`, selected `modules`, `policyVersion`,
optional ordinary `command` and optional opaque `proposedMemory`. Output is
bounded to 4 MiB UTF-8. The host verifies correlation, seat/decision/revision and
normal command legality before committing. It saves proposed memory only for an
accepted command. No response can consume resources by itself.

Runtime computation observes cooperative cancellation and the existing five-second
budget. Host scheduling/timeouts, readiness, provenance and durable accepted-memory
recovery belong to BOT-18. No library method starts a worker, HTTP server or thread
that watches games. Missing/incompatible/failed libraries keep CPU unavailable.

Module selection uses the shared rules' configuration dependencies, including
nullable Dismay's default. One base module plus configured expansion/variant
modules serves both faction profiles. BOT-09/10/11 add strategic module behavior;
selection alone does not establish mechanic acceptance.

## Optional server loading

`Wotr:Bots:RuntimeAssembly` is an absolute operator-only path; an unset path
returns `NotConfigured`. Optional `Wotr:Bots:RuntimeVersion` pins the required
build version. Invalid files, entrypoints and incompatible metadata return a safe
unavailable status. The server resolves the loader at startup but does not start
a game loop. An instance serializes evaluation and disposal; BOT-18 owns bounded
scheduling outside game locks. A future scheduler must check accepted policy
coverage before readiness, even when the DLL loads successfully.

The host validates response correlation, own decision/revision, available command
kind and response shape. This is a proposal gate, not full rule validation: BOT-18
must still invoke the ordinary authoritative command path under its normal locks.
Exceptions are reduced to fixed refusal codes; private exception text is not
returned. Framework-only ABI values permit the runtime's own pinned rule assembly
to coexist with the host's game engine without reference sharing.

## Verified foundation — 2026-10-27

The private full verification passes 679 C# and nine Python cases. Fifteen of the
C# cases cover the runtime ABI/context/module selector, including all 768 raw /
93 normalized configurations. Public loader tests pass ten cases; the selected
server startup/compatibility/PvE/room regression passes 55, with no skips.

A local operator-only smoke harness loads the actual published private DLL through
`EmbeddedBotRuntime`: both factions with base and combined expansions pass four
ABI cases. Its Contracts and Domain assemblies are distinct from the host's;
the real runtime correctly stays unready and returns `PolicyUnavailable`.
Published runtime output contains six runtime/rules libraries and both licenses,
without an App, transport, persistence, test or server assembly. Public server
publish output contains no private runtime or test fixture. No deployment or
real-policy gameplay acceptance is claimed. Evidence commands/hashes remain in
the ignored local task/testing record and operator artifacts.
