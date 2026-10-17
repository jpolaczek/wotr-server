<a id="praca-zadanie-po-zadaniu"></a>
# Task-by-task workflow

Documentation, tracked notes, task descriptions and generated reports use English.
Conversation may follow the user's language. The active task registry is
`docs/tasks/tasks.json`; `INDEX.md` is generated. Read AGENTS, the task specification,
requirements, dependencies and source gates before starting work.

## Documentation maintenance

At the user's request (2026-10-16), delete completed task descriptions and full
records instead of translating or extending them. Keep only completion identifiers
and technical coverage assignments in `docs/tasks/completions.json`, so dependency
checks and card/source validation remain meaningful. Full previous evidence is
available in Git history. Keep current rules and product specifications even when
the task that implemented them is complete. Delete superseded documentation and
replace its links with the current specification; old acceptance results must not
be presented as evidence for a newer build.

The current documentation/icon work includes local commits and push to both
repositories, as explicitly requested. It does not authorize a game release,
production deployment or rewriting Git history.

<a id="stany"></a>
## States

| State | Meaning |
| --- | --- |
| `planned` | The task has not started. |
| `ready` | Computed readiness: dependencies are `done` and no source gates remain unresolved. This is not a separately stored status. |
| `in_progress` | The start and assignee are recorded; work or handoff is in progress. |
| `blocked` | A specific obstacle and resumption condition are recorded. |
| `done` | Criteria are met and checks and handoff documents are recorded. |

An aggregate task (`aggregate`) finishes only after all component tasks and
completeness checks. It depends on the component tasks; component tasks do not
depend on their parent. This prevents cycles.

<a id="rozpoczęcie"></a>
## Starting

After reading the context and checking changes, use:

```sh
python3 scripts/tasks.py ready --stage E
python3 scripts/tasks.py start TASK-ID --agent codex --note "Goal and actual verification plan"
```

The script refuses to start with unfinished dependencies or open gates. Resume
`blocked` work through `start` after removing its cause; the note must describe
the change. By default, one agent handles one task; edit shared registry files
sequentially.

The agent also reports `START ID` in the conversation. For work longer than one
step, record `note` after a significant decision, finding an exception or before
handing over context. Keep the user informed of findings and remaining uncertainty.

As requested by the user, regularly save coherent implementation and documentation
changes in local commits. Before committing, review the diff, appropriate check
results and exclusion of secrets, caches and builds. A checkpoint does not grant
`done` status; incomplete acceptance remains explicit in the registry. Include
the commit hash in the handoff. Pushing and publishing require separate authorization.

<a id="dokumentacja-przed-kodem-i-razem-z-kodem"></a>
## Documentation before and alongside code

Before implementation, define inputs, legality, costs, effect order, player
decisions, data visibility and outcomes. Record the rule source. When changing
behavior, first reconcile its description with existing requirements, then
maintain the description and code in the same task.

Record discrepancies in `gaps.json` or DECISIONS. Do not change a game rule as
a “UI improvement”. UI documentation cannot override the rulebook.

Source readiness is separate from audit completion: T01 may finish with an honest
gap registry; tasks needing missing material still cannot start. Before such
completion, create one or more concrete follow-up source-acquisition tasks
(e.g. `SRC-01`) with unblocking conditions and assign them as `owner_task` for
open gaps. Update ROADMAP and the registry. An open gap cannot belong solely to
an already completed task. Close a gap only with verification evidence and a date
in `gaps.json`.

<a id="zakończenie"></a>
## Completion

Before closing, review file structure, dependencies, method length and
responsibilities, and repeated hot-path operations. The user expects senior
software developer standards, readability and justified performance. Avoid
excessive abstraction without combining independent layers in one file.

```sh
python3 scripts/tasks.py validate
python3 scripts/tasks.py finish TASK-ID --summary "Sources checked; missing items recorded" --check "PASS: actual check and result" --docs docs/sources/README.md
```

`finish` requires an active task, completed dependencies, closed gates, at least
one check result and an existing document. This is a record, not automatic proof
of game correctness: the implementer is responsible for matching the entry to
the actual check. Do not use the example command as evidence without performing
the described check.

The final note records the result, deviations, known limitations and the next
task. In the conversation: `DONE ID`. For an unmet criterion, use `block` rather
than `finish`.

```sh
python3 scripts/tasks.py block TASK-ID --reason "Map edge unconfirmed; GAP-MAP describes the required material"
python3 scripts/tasks.py note TASK-ID --note "Other data verified; resume with region comparison"
```

<a id="zakres-autonomii"></a>
## Scope of autonomy

Select the next ready task in the commissioned stage according to ROADMAP.
Do not ask again about routine, reversible actions within the request. A new
implementation stage, work outside the MVP or irreversible publication is not
authorized by a `ready` entry alone.

Record missing deployment information or sources in the relevant task and continue independent work within the current request. Do not claim acceptance requiring an unavailable environment.

<a id="walidacja-dokumentacji"></a>
## Documentation validation

`python3 scripts/tasks.py validate` checks identifiers, the dependency graph,
source gates, required task sections, requirement/rule references, local links,
index consistency, status history and local rulebook checksums. It does not
verify card text, Unity behavior or rule correctness in the running engine.

## Verification scope

Use checks appropriate to the changed behavior. Since the user's 2026-10-14 instruction, routine changes do not require rebuilding and installing the game unless that is needed to verify the task or specifically requested. Full acceptance tasks retain their stated requirements. The current MVP implementation focus is the base game; expansion rules remain documented for their own scope. Demo CPU behavior is confined to demo mode.
