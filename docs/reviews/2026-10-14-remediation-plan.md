<a id="plan-poprawy-jakości-po-review--2026-10-14"></a>
# Post review quality improvement plan — 2026-10-14

The biggest return gives the server input closure, resource consumption control and reliable acceptance client. The reps are intended to reduce the cost of subsequent changes, and history optimization remove the cost confirmed by measurement.

The source is [review](2026-10-13-code-review.md) and its [evidence](2026-10-13-evidence/README.md). The plan includes 17 with the corrective tasks QUAL-01-QUAL-17 and acceptance QUAL-18; QUAL-00 documents the preparation of the plan. Status, history and readiness are determined only by [registry](../tasks/INDEX.md). The current order concerns the assignment of tasks. Their implementation has not been initiated.

<a id="punkt-odniesienia-i-granice"></a>
## Reference point and boundaries

- Audit: `63e9e205a79d8c1f3413845a6decee0f3ea2b320` server,
  `08eb95678a3275edfadcaffe2034e52808d19d9d` client.
- Plan: `75d51b2f6892503131580e3004de1e438758308e` server,
  `0d88608c79987fdd571f80340fab5ff7cc00fb8c` client. The source of the server is the same as in the
  audit; the client has also UI-130/UI-131, including the meal panel. We do not assign the results
  to the newer commit.
- Target scenario: multiple independent 1v1 parties, 100–300 simultaneous users. Modular monolith,
  pure domain, PostgreSQL and Unity remain an appropriate base; the audit does not justify the
  system being re-written from scratch.
- UI-132 remains a separate acceptance function of army/Food. UI-133 retains publication block.
  This plan does not remove the dependencies or conditions of these tasks.
- P1 means a significant risk of operation or a condition of a public project; P2 error or
  measurable cost of maintenance; P3 streamlining entry to the project. These are the priorities of
  the plan, not automatic change in the classification of audit.

<a id="zadania-i-kolejność"></a>
## Tasks and order

The printout is an indicative number of working days of experienced engineer, with tests and documentation. It does not include waiting for the owner's decisions, access to CI, Unity licenses, host licenses, or publication consent. The aggregation of the compartments is not a project valuation. Technical dependencies are in `tasks.json`; the order below determines priority within this plan when the tasks are ready.

| Order | Task | Priority | Result / circulation |
|---:|---|---|---|
| 1 | QUAL-01 | P2, first amendment | Full validation of commands without HTTP 500; 1–2 days |
| 2 | QUAL-02 | P1 | Limits of movement, work and sockets; 1–2 days |
| 3 | QUAL-03 | P1 | Limited write of rejections with idempotence; 1–3 days |
| 4 | QUAL-04 | P2 | Legal pass after the Ring, also after the reboot; 0,5–1 day |
| 5 | QUAL-05 | P2 | Public base budget and controlled overload; 1–2 days |
| 6 | [QUAL-06](../tasks/QUAL-06.md) | P1 for acceptance | Isolated tests and complete current inventory; 1–2 days |
| 7 | QUAL-09 | P2 | Secure use of view and error diagnosis UI/Networks; 1–2 days |
| 9 | [QUAL-14](../tasks/QUAL-14.md) | P1 for open source | Anonymous bootstrap from empty cache; 1–2 days |
| 10 | [QUAL-07](../tasks/QUAL-07.md) | P2 | Current and stable UI scenarios, six permanent failures; 1–2 days |
| 11 | [QUAL-08](../tasks/QUAL-08.md) | P2 | Full drivery/profiling, seventh sustained defeat; 1–2 days |
| 12 | [QUAL-15](../tasks/QUAL-15.md) | P1 for public PR | Required complete and isolated gate Unity; 1–3 days |
| 13 | [QUAL-17](../tasks/QUAL-17.md) | P2 | Independent expectations of risk rules and matrix; 2–4 days |
| 14 | [QUAL-10](../tasks/QUAL-10.md) | P2 | One window life-cycle coordinator; 1–3 days |
| 15 | [QUAL-11](../tasks/QUAL-11.md) | P2 | Split rendering and decision choices; 2–4 days |
| 16 | [QUAL-12](../tasks/QUAL-12.md) | P2 | Limited Snapshot, Archive and Migration; 4–8 days |
| 17 | [QUAL-16](../tasks/QUAL-16.md) | P3 | Readable onboarding and heavy resource policy; 1–2 days |
| 18 | [QUAL-18](../tasks/QUAL-18.md) | acceptance | Joint regression and measurements of both repositories; 1–3 days |

You can run independently server correcting track, acceptance client track and licencji/bootstrapu. QUAL-15 track requires running tests and dependencies; QUAL-10/11 requires stable UI scenarios; QUAL-12 requires correct transactions and independent regression of rules. The component tasks do not depend on QUAL-18; it is QUAL-18 that depends on all components.

<a id="pokrycie-ustaleń-audytu"></a>
## Coverage of audit findings

The following R01–R12 is identifier **review findings**, not identifier product requirements with REQUIREMENTS.

| Determination | Owner's job | acceptance supplement |
|---|---|---|
| R01: unlimited rejected command / receivers | QUAL-02, QUAL-03 | QUAL-05, QUAL-18 |
| R02: public `null` and HTTP 500 | QUAL-01 | QUAL-18 |
| R03: pass after the Ring | QUAL-04 | QUAL-17, QUAL-18 |
| R05: Unable to be anonymous bootstrap | QUAL-14 | QUAL-15, QUAL-18 |
| R06: ominous goal Unity / runner PR | QUAL-15 | QUAL-18 |
| R07: growing snapshot and heartbeat JSONB | QUAL-12 | QUAL-18 |
| R08: too broad client coordination | QUAL-10, QUAL-11 | QUAL-18 |
| R09: Presentation errors masked as network | QUAL-09 | QUAL-18 |
| R10: Repo size and difficult onboarding | QUAL-16 | QUAL-18 |
| R11: Incoherent acceptance / Seven Permanent Failures | QUAL-06, QUAL-07, QUAL-08 | QUAL-15, QUAL-18 |
| R12: no base connection budget | QUAL-05 | QUAL-09, QUAL-18 |
| Limitation of proof of compliance (§4 review) | QUAL-17 | QUAL-04, QUAL-12, QUAL-18 |

<a id="co-oznacza-rzeczywista-poprawa"></a>
## What a real improvement means

- The incorrect input is rejected; it does not change the state, RNG nor the cost, and does not
  cause unlimited write. The retry of the approved command after loss of response still returns the
  same result.
- The overload has a limited cost and a recognizable response. Normal 300 session / 150 batch does
  not fall into limits. Reconnect test does not end with HTTP 500 or PostgreSQL 53300 error.
- The acceptance client includes current, full inventory. In the audit was 574 PlayMode, in the
  aggregation of 567 PASS and seven FAIL; it was not one green full run. The new UI-130/UI-131 tests
  increase the inventory. We do not lower the geometry requirements and login limits to hide
  failures.
- All workloads of profiles are made. Historically, the 40/40 board and motion 32/32; the 5/56 and
  in-game 4/32 are incomplete. Measurement after the changes includes both sizes, the 100/125%
  scale, traffic restriction, current build sources and R23: average ≥60 FPS and feedback ≤100 ms.
- The expected rule test result comes from instructions/FAQ, not only from the same engine that is
  being tested. The missing cover is public.
- The change in architecture has a measurable effect: fewer window relationships, narrower
  controller responsibility, no linear increase in the copy of the state from the archive history.
  The number of new files and the decrease in the number of lines alone do not constitute a
  completion criterion.
- The new contributor builds repositories without the private cache/token author.

<a id="warunki-zewnętrzne-i-zachowane-ograniczenia"></a>
## External conditions and retained restrictions

QUAL-14 and QUAL-15 must distinguish between the preparation of local packages/workflow from the actual anonymous availability and required status on GitHub. Approval of publication, repository settings and runners is only necessary after the preparation of specific changes to the evaluation if it has not already been granted. The creation of a plan does not give such a binding.

We do not add microservices, new renderer, final illustrations, matchmaking, AI or Warriors. We keep secrets in projections, records in the middle of decisions, journal, piny and hashe packages, manual strategic choices, and the current model of clicks and windows. We do not prescribe history of Git. Measurement of macOS ARM does not include Windows runtime, Intel Mac or target web host. Unexecuted checks remain marked as such.
