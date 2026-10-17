<a id="dist-12--podpis-systemowy-instalatorów-odroczone"></a>
# DIST-12 — Operating-system signatures for installers (deferred)

<a id="cel"></a>
## Goal

When the user decides to cost certificates: sign installers and executable files Authenticode (Windows) and Developer ID with notarization and stapling (macOS), removing warnings of the first installation. [D13](../DECISIONS.md).

<a id="kontekst-i-zależności"></a>
## Context and dependencies

After DIST-06. Takes over the old signature requirements with DS13/DS14. It does not block DIST-08 or unsigned releases. Start requires a user order and a Windows certificate (e.g. Artificant Signing or OV on token) and Apple Developer Program account.

Stage E; Direct Dependency: DIST-06. Requirements: R06/R25/R28. Source: [DISTRIBUTION_PLAN](../release/DISTRIBUTION_PLAN.md) and Read_before documents in [Register](tasks.json).

<a id="logika-i-zakres"></a>
## Logic and scope

- Windows: Authenticode signature with time stamp for Setup.exe, `Wotr.exe`, Velopack helper and
  packages; configuration in CI with DIST-10.
- macOS: Developer ID Application/Installer, Hardened Runtime, elements Unity, notarization and
  stapling .pkg/.app, including a helper; check both architectures.
- Updated player instructions: remove the warning step.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- DS13/DS14 in the original version: on a clean Windows and macOS Intel/ARM64 downloaded installer
  starts without warning about an unknown publisher.
- Update from the version not signed to the signed retains the data and identity.

<a id="weryfikacja"></a>
## Verification

- signtool verify; codesign --verify --strict, spctl, stapler validate; installation from downloaded
  file on real systems.
- python3 scripts/tasks.py validate and git diff --check.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

Update this description, DISTRIBUTION_PLAN, PLAYER-GUIDE and `wotr-client:release/README.md`. START, notes and DONE/BLOCKED according to WORKFLOW.
