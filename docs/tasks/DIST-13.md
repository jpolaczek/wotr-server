<a id="dist-13--odbiór-windows-i-udostępnienie-instalatora-graczom"></a>
# DIST-13 — Windows acceptance and installer availability

<a id="cel"></a>
## Goal

Take the Windows installer on real hardware (DS13 criterion moved from DIST-06) and only then show it to players on the game side and in Guide. Received task with any installer fixes, decision [D14](../DECISIONS.md).

<a id="kontekst-i-zależności"></a>
## Context and dependencies

After DIST-06, which ended with acceptance macOS (DS14), the CI smokes both systems and EditMode. For D14 MVP 0.9 goes to macOS players; pipeline builds, tests in CI and publishes Windows, but the game page and Guide do not show it. Start requires an external tester from Windows 10 or 11 x64 (a regular account, without development tools) and installer from the current release or run Installers.

Stage E; Direct Dependency: DIST-06. Requirements: R06/R10/R13/R25/R28. Source: [DISTRIBUTION_PLAN](../release/DISTRIBUTION_PLAN.md#d14--mvp-09-najpierw-na-macos-2026-10-15), DIST-06 and the instructions acceptance `wotr-client:docs/release/INSTALLER-ACCEPTANCE.md`.

<a id="logika-i-zakres"></a>
## Logic and scope

- Tester installs `WotrOnline-<version>-win-Setup.exe` downloaded by a browser, passes SmartScreen ,
  starts the game with a shortcut, uninstalls it and reinstalls it by section DS13 instructions
  acceptance ; sends back the Windows version, the Smart App Control status, warnings drop and
  result.
- The error reported by the tester (e.g. black screen, no shortcut, start of the game on
  installation) is repaired in the client and confirms with the new Installers run and re-tester.
- After PASS: Guide player without reservation "for now only macOS", game page (DIST-04) with
  Windows button with `/api/client/downloads`, entry in TESTING.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- DS13: On Windows clean, the standard user installs the game (when warned by SmartScreen), launches
  and uninstalls it; development tools are not required; installation does not open the game window;
  installation works again.
- The game data in `AppData\LocalLow` are uninstalled.
- The players are shown to the Windows installer only after this acceptance.

<a id="weryfikacja"></a>
## Verification

- Tester report (Windows version, Smart App Control, warnings, steps 1–4) and the installer track
  number, his SHA-256 with `SHA256SUMS`.
- On the amendment: green track of Installers after change.
- python3 scripts/tasks.py validate and git diff --check.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

Update this description, DISTRIBUTION_PLAN (D14), TESTING, `wotr-client:docs/release/PLAYER-GUIDE.md` and the game side with DIST-04. Save the result in the history of the task with the running number and the installer sum.
