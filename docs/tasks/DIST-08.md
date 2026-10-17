<a id="dist-08--odbiór-i-kontrolowana-publikacja-dystrybucji"></a>
# DIST-08 — Distribution acceptance and controlled publication

<a id="cel"></a>
## Goal

Pick up the complete download, installation and update path over the Internet and prepare the first controlled edition for players. Owner: integration of releases of both repositories.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

Congregate job after all DIST-01 – DIST-07 and DIST-09 – DIST-11 without children's dependence on this acceptance System signature ( DIST-12 ) is postponed by decision D13 and does not block acceptance Scope DS18 – DS20 and completeness check DS01 – DS17 Past postponements T38/T39 they do not include new criteria. The actual publication is only made in the scope of the subsequent execution order.

Stage E; direct dependencies: DIST-01 , DIST-02 , DIST-03 , DIST-04 , DIST-05 , DIST-06 , DIST-07 , DIST-09 , DIST-10 , DIST-11 Requirements: R04/R05/R06/R07/R09/R10/R11/R13/R24/R25/R26/R27/R28/R29/R30. Source of details and decisions: [DISTRIBUTION_PLAN](../release/DISTRIBUTION_PLAN.md) , [REQUIREMENTS](../REQUIREMENTS.md) , [ARCHITECTURE](../ARCHITECTURE.md) , [CONTRACTS](../CONTRACTS.md) and read_before documents in [Register](tasks.json) It's integration. product/infrastructure , without establishing new rules of the game.

<a id="logika-i-zakres"></a>
## Logic and scope

- Collect commit client/server, locki, digest backend, data and protocol versions, installers,
  Ed25519 manifests signatures, and actual system reports.
- Download the signed release of N from the external network, install it on the clean Windows/macOS
  Intel/ARM64 and play two accounts on the target PC.
- Release candidate N+ 1 a controlled tool in the environment acceptance : consent/Later/Required
  update, transfer/interruption/resumption , safe reboot, login and return to writing.
- Perform a common full Windows ↔ macOS game and play the saved decision after the client/server
  and backup/restore reboot to a separate database. Check the receiver in the middle of the
  bitwy/Polowania.
- Confirm atomic publication, no announcement of incomplete version, raising the minimum only after
  both platforms and withdrawing defective announcement. Do not test the destruction on an active
  player base.
- Prepare the player instructions without development tools and operator runbook: build, signatures,
  upload, announcement, backup, repair, withdrawal and unavailable PC/Internet.
- When attaching to the publication, announce the first approved table; without it save the actual
  lack and condition of completion, without marking distribution as issued.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- DS18: Windows x64 matrix (tester)/MACOS ARM64 (sender)/MACOS Intel (at least Rosetta, no hardware
  saved) has PASS for the downloaded installer and actually installed N → N+1, in
  Optional/RequiredSoon/RequiredNow/Repair modes, also refusal and failure.
- DS19: two real clients over the Internet end a common batch and recover save/receipt after
  breaks and restoration. Ready build or Editor does not count this attempt.
- DS20: complete catalog and artifacts indicate the same versions; the operator can
  publish/withdrawn the ad and fix the installation according to the saved commands.
- DS01-DS26 have references to evidence, no omitted scenarios and fictional PASS. All
  environmental/publication locks are public in the register.

<a id="weryfikacja"></a>
## Verification

- Run the full required CI of both repositories, public catalog and signature tests and the runner
  of the installed upgrade on all three targets.
- External acceptance: page, HTTPS downloads, large transfer/Range, WSS, two accounts, full party,
  reboot PC and backup/restore.
- Negative release attempts: aborted upload, lack of platform, bad signature, old cache, raised
  minimum, space, connection atrophy and two processes.
- Save the acceptance report with real commands, systems/architekturami, SHA-256, URL and
  diggests; review packages and logs for secrets/tests, check the player's instructions.
- python3 scripts/tasks.py validate and git diff --check for ko .. documents and changes.

The performer saves actual commands, environment and results. After the repo split, he adjusts the paths to the canonical locations with DIST-03. The above scenarios are a plan for future verification. The absence of the environment or the required criterion requires an open block with the condition of resumption, rather than a fictional PASS.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

Update this description, DISTRIBUTION_PLAN and update_docs documents in the registry. After the separation, use their canonical lokalizacje/odno-based DIST-03-based accretions. Keep START, notes, results, commit and DONE/BLOCKED according to WORKFLOW. The transfer includes versions and artifacts required by subsequent dependent task, modified files, test evidence, limitations and actual readiness.
