<a id="audio-10--niższe-szeleszczące-brzmienie-efektów"></a>
# AUDIO-10 — Lower, softer rustling sound effects

<a id="cel"></a>
## Goal

Implementation: Re-synthesis clips with AUDIO-01 after user comments with 2026-10-15: "They are too high, a lot sounds like bamboo; they are to be more rustling, less stinging." Only WAV files and the generator change; the clip names, directory, channels and sound logic remain.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

Depends on AUDIO-01 (generator `graphicalassets/audio` Measurement before change (spectral centre, energy above 4 kHz, share 20 strongest straps as a measure of tonality:
- `ui-click` and `action-accepted` are almost pure tones (99,9% and 100%), as they are formed from
  several suppressed sines, which gives the sound of a ‘bomb’;
- `card-inspect` has 51,8% energy above 4 kHz (crushing crackles);
- `dice-roll` She's got hard fashion. 2–7 kHz.

<a id="logika-i-zakres"></a>
## Logic and scope

- Effects (click, preview and move card, dice, reception, steps): The source is noise shaped with
  envelope — paper, fabric, felt — instead of ringing fashions. Band below, without cracks above ~4
  kHz, soft attack.
- Signals (battle, Ring, Decision, Error, Game End) retain recognizable tone, but are lower and
  darker, without pricking components: battle iron and decision bell tinted, bug notes below.
- Save lengths from the directory, adjust volume (BS.1770) and limit true peak −1 dBTP and
  deterministic grains. No change in Unity code and filenames.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- Effects: tonality below 50%, energy above 4 kHz below 10%, in the measurement of the same script
  before and after.
- `AudioCatalogTests` and full EditMode pass (lengths, import, edges).
- The user listening on the listening page. Without listening, the hearing result remains open, not
  counted.

<a id="weryfikacja"></a>
## Verification

`python graphicalassets/audio/generate.py`, pre- and post-measurement script, full EditMode client, check_docs and tasks.py validate. Listen to the listening page with new clips.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

This document, wotr-client:docs/ui/AUDIO_UX.md (receptures and measurements), docs/TESTING.md, ROADMAP. No push without order.
