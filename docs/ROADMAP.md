<a id="plan-implementacji"></a>
# Implementation roadmap

This roadmap lists remaining work. The [task index](tasks/INDEX.md) is generated from the active registry; dependencies and source gates determine readiness, not authorization to start.

## Current documentation and artwork work

The shared desktop icon is configured in the client. English documentation and tracked notes replace the previous Polish-language policy. At the user's request, completed task specifications and full records are removed, along with superseded documentation. Technical completion identifiers and card assignments remain in [completions.json](tasks/completions.json); previous descriptions and evidence remain in Git history. Commits and push are authorized for this work.

## Remaining work

| Task | Scope | State |
| --- | --- | --- |
| [QUAL-06](tasks/QUAL-06.md) | Isolated PlayMode runs and complete test inventory | `planned` |
| [DIST-07](tasks/DIST-07.md) | In-game updates, repair and safe restart | `in_progress` |
| [DIST-08](tasks/DIST-08.md) | Distribution acceptance and controlled publication | `planned` |
| [QUAL-14](tasks/QUAL-14.md) | Anonymous bootstrap of pinned dependencies | `planned` |
| [QUAL-07](tasks/QUAL-07.md) | Current UI scenarios and stable geometry | `planned` |
| [DIST-11](tasks/DIST-11.md) | Release command, runbook and first-installation instructions | `blocked` |
| [QUAL-08](tasks/QUAL-08.md) | Complete test drivers and reproducible client profiling | `planned` |
| [DIST-12](tasks/DIST-12.md) | Operating-system signatures for installers (deferred) | `planned` |
| [QUAL-15](tasks/QUAL-15.md) | Required Unity CI gate and public pull-request isolation | `planned` |
| [DIST-13](tasks/DIST-13.md) | Windows acceptance and installer availability | `planned` |
| [QUAL-17](tasks/QUAL-17.md) | Independent rule expectations and explicit coverage gaps | `planned` |
| [QUAL-10](tasks/QUAL-10.md) | One window lifecycle coordinator | `planned` |
| [QUAL-11](tasks/QUAL-11.md) | Narrower responsibilities for the client decision controller | `planned` |
| [QUAL-12](tasks/QUAL-12.md) | Bounded snapshots and a persistent history archive | `planned` |
| [QUAL-16](tasks/QUAL-16.md) | Clear onboarding and large-asset policy | `planned` |
| [QUAL-18](tasks/QUAL-18.md) | Combined quality acceptance for both repositories | `planned` |
| [UI-163](tasks/UI-163.md) | Acceptance of the turn console, Hunt window and Rings | `blocked` |
| [AUDIO-08](tasks/AUDIO-08.md) | Acceptance of subtle sounds and track animation sequences | `blocked` |
| [AUDIO-10](tasks/AUDIO-10.md) | Lower, softer rustling sound effects | `blocked` |
| [LNX-01](tasks/LNX-01.md) | Linux platform in Contracts, the release catalog and the publisher tools | `planned` |
| [LNX-02](tasks/LNX-02.md) | Linux client build, AppImage installer and CI | `planned` |
| [DECK-01](tasks/DECK-01.md) | Steam Deck playability of the Linux build | `planned` |
| [UI-217](tasks/UI-217.md) | Readable card names in the hand tiles | `planned` |
| [AUDIO-11](tasks/AUDIO-11.md) | New battle-start sound | `blocked` |

## Order and acceptance

Continue work only within the commissioned scope. Existing work on DIST-07 and all recorded blockers remain unchanged. The quality remediation plan is in [the review follow-up](reviews/2026-10-14-remediation-plan.md); QUAL-18 depends on the individual fixes and complete acceptance evidence. UI-163 depends on UI-164 and the already completed turn-console work. Audio acceptance and device checks remain separate tasks.

Completed dependency identifiers remain valid prerequisites, so deleting their descriptions does not make unfinished dependencies ready. Card coverage still checks every source-to-content assignment. Open source gates must have an active owner.

A task is complete only after its acceptance criteria and required checks pass. Preserve failures, skipped checks and hardware limitations explicitly. Do not equate a generated report or a build with runtime acceptance.

## Current specifications

- [Observers and replays](server/OBSERVERS-REPLAYS.md), [replay format](server/REPLAY-FORMAT.md).
- [Distribution and updates](release/DISTRIBUTION_PLAN.md).
- [Requirements](REQUIREMENTS.md), [workflow](WORKFLOW.md), [testing](TESTING.md).
- [Documentation language and cleanup](DOCUMENTATION_LANGUAGE.md).
