# Private runtime operations

BOT-19 packages the optional operator library and controls its host lifecycle.
There is no bot server, container or database. Public builds and player installers
must remain independent of private source, DLLs, symbols and packages. The real
faction policies remain unready; operations tests use a public ABI fixture.

## Package and compatibility contract

The private build produces a runtime archive, a separate private symbols archive,
and SHA-256 inventories. Runtime files are installed into a new versioned directory;
never overwrite a loaded library. A pinned manifest identifies every file and the
describe metadata. The host verifies the manifest digest, bounded flat file list,
individual sizes/hashes, entry assembly and actual Describe result before loading.
Format 1 permits at most 32 flat files / 64 MiB, a manifest up to 128 KiB and no
symbols, links, directories or unlisted files. The private installer additionally
allowlists exactly six runtime libraries, dependency/configuration JSON and both
license notices. Checksums require a trusted operator distribution channel;
they are not publisher signatures. Keep installed files and their parent directory
writable only by the operator and read-only to the running host.
Private rule dependencies retain their provenance and licenses. Unpackaged loading
requires an explicit development setting; operator upgrade requires a package.

ABI 2 gains an optional opaque `memoryFormat` identifier. New CPU assignments pin
ABI/protocol/rules/content/memory-format identities as host metadata, without
interpreting memory contents. Admission requires a declared memory format.
Supported upgrades and rollbacks keep those identities and the faction policy
version unchanged; only the runtime build/version changes. Unknown legacy
metadata, changed policy/schema/rules or unsupported configured modules refuse
the entire transition without resetting memory, changing a decision or consuming
the pending proposal. Semantic memory migrations require separately reviewed work.
Assignments created before migration 020 have unknown compatibility metadata;
they cannot be evaluated or upgraded automatically. Preserve their saves and use
an explicitly reviewed metadata/memory migration before resuming them. Replacing
a saved CPU seat or deleting its memory is not an upgrade procedure.

## Drain and change version

Operator commands run locally through the existing server executable, never a
player HTTP endpoint. `status` returns aggregate operational counts only.
`drain` persistently closes the admission/claim gate and fences every generation.
Room readiness/start, proposal staging and authoritative command application take
a shared control lock before the job lock. Drain takes the exclusive control lock;
when it returns, earlier commits have finished and no older evaluation can commit.
Ordinary human operations remain available. Restart does not clear drain.

While drained, install/inspect the target package, stop old host processes, and
run the compatible upgrade command with explicit source and target runtime pins.
It validates every affected saved assignment and updates pins transactionally.
Accepted memory, pending command bytes/IDs and game state stay unchanged. Start
the host with the new manifest/hash/version, then explicitly resume the matching
control epoch. Rollback uses the same procedure and compatibility checks. Keep
the prior artifact and database backup. No production deployment is authorized.

Configure these environment variables through the existing operator secret/config
mechanism (the database connection remains the ordinary `WOTR_DB`):

```sh
export Wotr__Bots__RuntimeEnabled=true
export Wotr__Bots__RuntimeManifest=/operator/runtimes/VERSION-HASH/runtime-manifest.json
export Wotr__Bots__RuntimeManifestSha256=MANIFEST_SHA256
export Wotr__Bots__RuntimeVersion=TARGET_VERSION
dotnet Wotr.Server.dll --bot-runtime inspect
dotnet Wotr.Server.dll --bot-runtime status
dotnet Wotr.Server.dll --bot-runtime drain
# Record the returned epoch; stop all previous game-host processes.
dotnet Wotr.Server.dll --bot-runtime upgrade EPOCH SOURCE_VERSION
# Start the normal game host with the target settings while it remains drained.
dotnet Wotr.Server.dll --bot-runtime resume EPOCH
```

`inspect` needs no database connection. Other operations use normal migrations and
database access, start no HTTP listener and never start scheduler workers.
`upgrade` requires a distinct source version and the current drain epoch. `resume`
requires the installed version and manifest hash recorded by the transition;
stale epochs, old hosts and another build using the same version cannot bypass it.
For the initial install, `resume` pins the package if all existing assignments
already use that version. Commands emit fixed error codes and return nonzero on
refusal. `status` includes epoch, drain state, runtime version and aggregate
assignment/pending-proposal counts, never private memory or player identifiers.

Install the private archive with `wotr-bot/scripts/runtime_package.py install
ARCHIVE --sha256 ARCHIVE_SHA256 --root ABSOLUTE_OPERATOR_DIRECTORY`; use its
printed manifest values above. Mount the installed directory read-only into the
existing game-server container if applicable. There is no bot Compose service,
image, database or default public Docker layer. `RoomsEnabled`,
`AcceptedPolicyVersion` and `Features` retain their existing gameplay gates.

## Resource and diagnostic boundaries

Configure 1–8 workers, cooperative decision time (at most five seconds) and total
work (at most 100,000 units across preview continuations). A cancelled call that
does not return retains its slot; no replacement tasks accumulate. A configurable
host managed-memory pressure threshold pauses new evaluations; it is not an OS
sandbox or a per-library allocation guarantee. Container/OS limits apply to the
whole game host. Do not promise safe forced abortion of arbitrary managed code.

All settings below are under `Wotr:Bots` (environment prefix `Wotr__Bots__`):

| Setting | Default | Accepted range / behavior |
| --- | --- | --- |
| `Workers` | 2 | 1–8 concurrent loaded instances per host process |
| `MaximumWork` | 100000 | 3–100000 total; floor(limit/3) per evaluation, at most three evaluations |
| `DecisionTimeoutMilliseconds` | 5000 | 50–5000 private compute time across continuations |
| `ManagedMemoryLimitMiB` | 512 | 64–16384; sampled host managed-memory gate before new evaluations |
| `RuntimeLeaseSeconds` | 30 | 5–300; every commit checks fresh database time |
| `PollMilliseconds` | 500 | 50–5000; durable work polling interval |
| `RetrySeconds` | 5 | 1–60; deferred work/failure/memory-pressure delay |

Stop previous processes before starting replacement workers. These concurrency
bounds apply per process; an uncooperative old evaluation is not forcibly killed
by a database drain. Its generation/package pins prevent future commits, while
stopping the old process releases its compute/memory resources.

The operator meter records bounded aggregate outcomes, active/peak evaluations,
queue wait, compute latency and host memory samples. No command, hand, memory,
match/account/assignment identifier or arbitrary runtime reason becomes a label
or log value. Tests exercise eight concurrent matches with real PostgreSQL and
an installed fixture DLL, collect queue/compute/memory evidence, and rehearse
drain, restart, compatible upgrade/rollback and incompatible/tampered artifacts.
The .NET meter is `Wotr.Server.Bots`: `bot.work.outcomes`, `bot.queue.seconds`,
`bot.compute.seconds`, `bot.evaluations.active`, `bot.evaluations.peak`,
`bot.host.managed.peak` and `bot.host.working_set.peak`. Connect the operator's
existing .NET metrics collector; no player metrics endpoint is added. Outcomes
are fixed host codes, including package/refusal/timeout/drain/memory-pressure
failures. In-process diagnostic snapshots retain at most 2048 latency samples;
their p95 and sampled host memory are operational evidence, not allocation quotas.

See [host persistence](BOT-HOST.md), [runtime ABI](BOT-RUNTIME.md) and
[product boundaries](STRATEGIC-BOTS.md) and [verification](../TESTING.md#private-runtime-operations).
Operations acceptance does not authorize deployment or establish strategic gameplay.
