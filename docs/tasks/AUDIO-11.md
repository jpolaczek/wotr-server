# AUDIO-11 — New battle-start sound

## Goal

Replace `battle-started` with a serious sound of armies clashing.

## Context and dependencies

The user reported on 2026-10-16 that the current sound resembles a 1950s comedy.
The AUDIO-10 version uses a synthetic 98 Hz war horn, with a fifth jump in the
second variant, over iron clatter. It depends on AUDIO-01 (generator) and AUDIO-06
(battle signal), but does not wait for AUDIO-10 listening acceptance. This task
replaces `BattleStarted`; AUDIO-10 listening covers the other effects.

## Logic and scope

- Replace the horn and melody with two deep war-drum hits: low-pass noise and
  a falling undertone around 60→40 Hz.
- Add a rising army rumble below about 1 kHz and muted iron contact below 2.4 kHz,
  following AUDIO-10's requirement to avoid piercing high frequencies.
- Provide two variants with different hit spacing and texture, BS.1770 loudness
  normalization as in AUDIO-10, and duration up to about 1 s.
- Keep the other effects, volume policy and playback triggers unchanged.

## Acceptance criteria

- New `battle-started-*` files, a manifest and deterministic seeds.
- User listening confirms a serious sound without the comic horn.

## Verification

Run `python3 graphicalassets/audio/generate.py`, loudness and peak measurements,
EditMode audio tests and the listening preview. Record the user's listening result.

## Documentation and handoff

This document, `wotr-client:docs/ui/AUDIO_UX.md`, `docs/TESTING.md` and ROADMAP.
