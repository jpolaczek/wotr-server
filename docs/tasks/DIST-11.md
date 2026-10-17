<a id="dist-11--polecenie-wydawcy-runbook-i-instrukcja-pierwszej-instalacji"></a>
# DIST-11 — Release command, runbook and first-installation instructions

<a id="cel"></a>
## Goal

One command on Mac publisher to transfer the release from draft GitHub Release to players: verification, signature Ed25519, upload, publication and check. Add the operator runbook and instructions for the first installation of the unsigned application. Owner: wotr-server (tool and runbook) and votr-client (player instruction).

<a id="kontekst-i-zależności"></a>
## Context and dependencies

After DIST-09 (new commands ReleaseTool, installer in the manifest) and DIST-10 (artificries with CI). The range of DS25/DS26 by decision [D13](../DECISIONS.md). The private key remains on the Mac (U6). The key with DIST-05 (key id `95ffc109e1bb9b15`) was not played after migration of the host to Omarchy — if it cannot be safely recovered, generate a new one on Mac and attach a new public key in the client and in the server configuration before the first release with the updateer. Public release requires a working host with DIST-04.

Stage E; direct dependencies: DIST-09, DIST-10. Requirements: R06/R13/R25/R28/R29/R30. Source of details: [DISTRIBUTION_PLAN](../release/DISTRIBUTION_PLAN.md#d13--zmiany-z-2026-10-14), [infra/README](../../infra/README.md) and read_before documents in [Register](tasks.json).

<a id="logika-i-zakres"></a>
## Logic and scope

- `scripts/release_client.py v<version> [--required-at ISO | --required-now] [--dry-run]` : `gh
  release download` ed draft ; control `SHA256SUMS` ; `build-info.json` (version = tag, commit =
  commit tag, `development=false` , common protocol); version in full package; `ReleaseTool sign`
  for both platforms with notes and installer; `verify-upload` ; a public confirmation; `rsync
  --partial` ed `incoming/<version>/` on the host; `ssh` → `publish` [+] `minimum --at` ]; check
  `/api/client/releases` , `/api/client/downloads` answers 206 for the Range and signature of the
  downloaded manifesto; entry of the history of releases.
- Runbook: cancellation of deadline, withdrawal (minimum photo first), repair only higher version,
  encrypted offline key copy, key rotation as client release fastening two keys, unavailable
  host/Internet.
- Player's Manual (`wotr-client:docs/release/PLAYER-GUIDE.md`) and Game Page: buttons with
  `/api/client/downloads`; Windows — browser warning, SmartScreen “More info → Run anyway”, Smart
  App Control limitation; macOS 15/26 — “Open Anyway”, “only for me” installation; one-time
  transition with ZIP.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- DS25: On Mac, publishers publish a complete release with CI and confirm public addresses;
  `--dry-run` does not send or change anything.
- Refuse: Evil SHA artifact, incompatible commit/version, build developer, no platform, no
  confirmation — no change on host.
- DS26: Windows external tester and publisher on macOS ARM64 install the game only according to
  instructions.

<a id="weryfikacja"></a>
## Verification

- Dry-run and actual publication of two test versions in the environment acceptance; `minimum --at`
  with deadline behind 10 minutes and client observation with DIST-07.
- Negative attempts from the denial list on copies of artifacts.
- python3 scripts/tasks.py validate and git diff --check.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

Update this description, DISTRIBUTION_PLAN, infra/README, PLAYER-GUIDE and `wotr-client:release/README.md`. Transfer to DIST-08: actual commands, release history, key copy location (without its value). START, notes and DONE/BLOCKED according to WORKFLOW.

<a id="stan-2026-10-15"></a>
## State (2026-10-15)

Ready tools ( `3ab753e` ): `scripts/release_client.py` (Mac publisher) and `scripts/release_host.py` (host), tests `tests/release` ( 18 , also Python 3.9 Systemic macOS) in CI , runbook in [infra/README](../../infra/README.md#wydanie-klienta-jednym-poleceniem--dist-11) Key from DIST-05 is on Mac publisher: pasted `.pub` is equal to the key attached in `ReleasePublisher.cs` (key id `95ffc109e1bb9b15` ), so the new key is not needed. `ReleaseTool` It went in a container. NET 10 on files in the layout draft DIST-10 : `keygen` , `sign` both platforms the same arguments as `release_client.py` , `verify-upload` , `publish --minimum --at` , `status` ; `release_host.py state` reads created `catalog.json` .

to acceptance lack of environment: host with `downloads` and API  (DIST-04) for the publication of two test versions and clients with the updater (DIST-07) for observation `minimum --at` On Mac, publishers are still needed. NET   10   SDK (is 9.0),  `ssh` / `rsync` to host and `~/.config/wotr/release-client.json`.  DS26: installation according to instructions on macOS ARM64 confirmed acceptance   DS14 in DIST-06; Windows tester belongs to DIST-13  (D14).
