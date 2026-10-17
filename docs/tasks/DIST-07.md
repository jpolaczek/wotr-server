<a id="dist-07--aktualizacje-w-grze-naprawa-i-bezpieczny-restart"></a>
# DIST-07 — In-game updates, repair and safe restart

<a id="cel"></a>
## Goal

Connect the server directory with UI update, download and switch after the process is completed safely: optional version with consent required (also with deadline) automatically downloads, repair the current version from Settings ([D13](../DECISIONS.md)). Owner: repo client; server enforces compliance with DIST-05/DIST-09.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

After DIST-05, DIST-06 and DIST-09. Range of DS15-DS17 and DS22/DS23. The User has chosen to have no launcher and the possibility to postpone the compatible version. UI in English; notifications only in the application. Do not add automatic strategic decisions.

Stage E; Direct Dependency: DIST-05, DIST-06, DIST-09. Requirements: R06/R07/R10/R13/R21/R23/R25/R29. Source of details and decisions: [DISTRIBUTION_PLAN](../release/DISTRIBUTION_PLAN.md), [REQUIREMENTS](../REQUIREMENTS.md), [ARCHITECTURE](../ARCHITECTURE.md), [CONTRACTS](../CONTRACTS.md) and read_before documents in [Register](tasks.json). This integration product/infrastructure, without establishing new rules of the game.

<a id="logika-i-zakres"></a>
## Logic and scope

- Enter outright states with DISTRIBUTION_PLAN and separate UI, directory communication,
  transfer/verifications, permanent download status and integration of native helper.
- Check small metadata at start/reconnect, every 15 minutes, manually in Settings, after 409
  response and at the time of the planned minimum (with random delay). For optional release, show
  version, size, description and Download update/Later; before consent, do not download the package.
- Priority modes: RequiredNow > RequiredSoon > Repair > Optional; ServerBehind no download.
  RequiredNow and RequiredSoon download `latest` automatically (RequiredSoon delayed 0–10 min);
  Repair collects `installed` offer after confirmation in Settings. Counting down to date according
  to `serverTime`; after the deadline, cancels the new command also offline.
- `Runtime/Updates` module: directory client, pure mode policy, manifest verification (Ed25519 by
  BouncyCastle, pinned key, allowed origin downloads), downloader (without Authorization, without
  redirects, Range, site control, SHA-256), Velopack bridge (inactive in Editor, developer build and
  uninstalled copy), state coordinator, schedule and blockage of one instance.
- 409 response served in lobby, waiting room, `CommandSender` and `TurnInteraction` (today omitted):
  no loop of reruns, with the behavior of the journal and commandId.
- Optional release during a game is announced without blocking. After agreement to download in
  the background with progress/Cancel/Resume; on change of release do not transfer consent to
  another package.
- The old version blocks the new command online, preserving the game, preview and local journal.
  The newer non-compliant client gets a message about the server, without automatic downgrade.
- Verify the manifest signature, address HTTPS, platform, size and hash before the swap. Failure or
  cancellation does not violate the running installation.
- Restart and update first completes existing local records and retains uncertain journal with
  commandId. Error writing shows Retry and leaves process; do not send LeaveRoom/CancelGame.
- After being ready to write check the other processes of this installation and one replacement
  lock; do not kill the other client or start earlier watchdog helper.
- After rebooting use normal login/reconnect and recovery of the receipt. The aborted swap has a
  proven recovery/repairs path signed with the installer, without losing data.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- DS15: transfer test confirms zero downloaded bytes of optional package before consent and after
  Later; compatible version still playing, unobtrusive requires an update.
- DS22: the required and scheduled version is downloaded without question, until the deadline the
  game lasts with the countdown banner, from the deadline new moves are stopped until restart;
  installation only after clicking.
- DS23: Repair downloads and installs the current version; for the withdrawn version it proposes an
  update.
- U8: The required update always offers a “Decline and quit” that ends the local save (error →
  Retry) and closes the game without installation; the next start shows the selection again and uses
  the downloaded package.
- DS16: reboot does not lose draft, settings or commandId; lost receipt is recovered without double
  decision execution in turze/bitwie/Polowaniu.
- DS17: Cancel/Resume, broken link, bad signature/hash, no space, write error, two instances and
  discontinued swaps have handled result without automatically killing clients.
- UI remains responsive and available with keyboard on 1366×768/1920×1080; the optional message does
  not capture the ongoing gesture or strategic decision.

<a id="weryfikacja"></a>
## Verification

- controller/transport updater tests with request counter and bytes, cancellation and compatibility
  matrix; PlayMode checks UI, focus and clicks.
- Indeed installed N → N+1: consent, postponement, background download, verification, reboot and
  update and normal login/reconnect.
- Attempts to fail to write and transfer, interrupt the switch and two processes; confirm no forced
  kill and complete installation after repair.
- Two accounts and a real backend: lost response to command before updating, new client recovers the
  receipt; fingerprint profile przed/po. Layout Tests and Response.
- python3 scripts/tasks.py validate and git diff --check for ko .. documents and changes.

The performer saves actual commands, environment and results. After the repo split, he adjusts the paths to the canonical locations with DIST-03. The above scenarios are a plan for future verification. The absence of the environment or the required criterion requires an open block with the condition of resumption, rather than a fictional PASS.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

Update this description, DISTRIBUTION_PLAN and update_docs documents in the registry. After the separation, use their canonical lokalizacje/odno-based DIST-03-based accretions. Keep START, notes, results, commit and DONE/BLOCKED according to WORKFLOW. The transfer includes versions and artifacts required by subsequent dependent task, modified files, test evidence, limitations and actual readiness.
