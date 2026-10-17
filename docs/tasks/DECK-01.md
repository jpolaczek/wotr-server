# DECK-01 — Steam Deck playability of the Linux build

## Goal

Implementation and acceptance: the Linux build is comfortable on the Steam Deck (D18) — its
1280×800 screen, trackpads, touch screen and on-screen keyboard — and the game page drops the
"experimental" label once a device check passes.

## Context and dependencies

After LNX-02. The owner has no Steam Deck now; the device check waits for a tester with one.

## Logic and scope

- Layout at 1280×800 (narrower than the 1366×768 minimum): a fitting interface scale and windows
  that stay on screen; checked at that resolution on the Linux PC first.
- Trackpad as mouse and touch taps reach every control; hover-only information stays reachable.
- Sign-in and table codes with the Steam on-screen keyboard; instructions for adding the AppImage
  to Steam as a non-Steam game.

## Acceptance criteria

- PlayMode layout checks at 1280×800 pass without hidden or off-screen controls.
- A tester completes sign-in and a game turn on a Steam Deck in gaming mode.

## Verification

PlayMode layout tests at 1280×800; a device session recorded by the tester.

## Documentation and handoff

Update this file, `wotr-client:docs/ui/UX_SPEC.md` and `wotr-client:docs/release/PLAYER-GUIDE.md`.
