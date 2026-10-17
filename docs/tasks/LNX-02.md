# LNX-02 — Linux client build, AppImage installer and CI

## Goal

Implementation in `wotr-client`: a Linux x64 release build of the client, packed by Velopack as an
AppImage, smoke-tested in CI and published with every release tag next to macOS and Windows (D18).

## Context and dependencies

After LNX-01 (Contracts with `linux-x64`). Builds on DIST-06/DIST-10 (GameCI, Velopack, release
workflow) and DIST-07 (the in-game updater, which must work for the AppImage too).

## Logic and scope

- `BuildTasks.BuildReleaseLinux` (Mono, x86_64), build identity `linux-x64`, settings that keep the
  player data path stable.
- Velopack on Linux: startup hooks, installed-copy detection and staging an update for the AppImage;
  the self-check argument works for the Linux player.
- `scripts/package_velopack.py pack --platform linux` builds `WotrOnline-<version>-linux.AppImage`
  and the full package; sums and verification include Linux.
- `installers.yml`: GameCI `StandaloneLinux64`, a Linux pack job and a smoke test on ubuntu
  (start the AppImage's self-check, apply N+1, data kept). `release.yml` adds the Linux assets.
- Player guide: download, `chmod +x`, start; FUSE note (`fusermount3` or `fusermount`); Steam Deck desktop mode and adding it to
  Steam as a non-Steam game (experimental until DECK-01).

## Acceptance criteria

- A tag builds a draft release with the Linux AppImage and package, and the CI smoke test passes.
- On the owner's Linux PC the AppImage installs, signs in to the production server and updates
  from N to N+1.
- EditMode tests pass; the in-game updater identifies `linux-x64`.

## Verification

CI runs of `installers.yml` and `release.yml`; local install and update on the owner's Linux PC
(Omarchy); EditMode in CI.

## Documentation and handoff

Update this file, `wotr-client:release/README.md`, `wotr-client:docs/release/PLAYER-GUIDE.md` and
`wotr-client:docs/release/INSTALLER-ACCEPTANCE.md`. Hand over the asset names to DIST-11.
