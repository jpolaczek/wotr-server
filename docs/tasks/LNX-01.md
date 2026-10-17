# LNX-01 — Linux platform in Contracts, the release catalog and the publisher tools

## Goal

Implementation in `wotr-server`: `linux-x64` is a known client platform (D18) everywhere the server
and the publisher handle platforms, so a release carries Linux packages and the game page offers the
Linux installer.

## Context and dependencies

D18. Server half of the Linux platform; the client half is LNX-02. Contracts change additively
(new constant, `All` with three platforms) and get a new version that the client pins in LNX-02.

## Logic and scope

- `ClientPlatforms.Linux = "linux-x64"`, part of `All` and `IsKnown`; the catalog, compatibility
  policy and `/api/client/releases?platform=linux-x64` accept it.
- `Wotr.ReleaseTool` (sign, verify-upload, publish) and the catalog's completeness rule require all
  three platforms; manifests, installers (`.AppImage`) and downloads work for Linux like the others.
- `scripts/release_client.py` and `release_host.py` expect the Linux files from the draft release.
- The game page (`infra/site`) offers the Linux AppImage with short instructions and marks the
  Steam Deck experimental (D18).
- Contracts and public content are packaged and published; DISTRIBUTION_PLAN, CONTRACTS and the
  release runbook describe three platforms.

## Acceptance criteria

- Server tests cover `linux-x64` in the catalog, the compatibility checks and the release tool,
  including a release refused without the Linux files.
- The publisher's dry run accepts a draft with Linux files and rejects one without them.
- The game page lists the Linux installer when the catalog has one.

## Verification

`dotnet test` (release catalog, release tool, endpoints), `tests/release` unit tests, the game page
checked against a local catalog, `python3 scripts/tasks.py validate`.

## Documentation and handoff

Update this file, DISTRIBUTION_PLAN, CONTRACTS, `infra/README.md` and the player guide link. Hand
over the Contracts version and the platform identifier to LNX-02.
