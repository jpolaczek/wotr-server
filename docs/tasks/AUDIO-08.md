<a id="audio-08--odbiór-dyskretnych-dźwięków-i-sekwencji-torów"></a>
# AUDIO-08 — Acceptance of subtle sounds and track animation sequences

<a id="cel"></a>
## Goal

Multi-purpose task: to collect AUDIO-01–AUDIO-07 and the entire AU01-AU18 matrix from [AUDIO_UX](https://github.com/jpolaczek/wotr-client/blob/0424bc994afe16632d12c3933a18d36e7ada4e61/docs/ui/AUDIO_UX.md). acceptance is to demonstrate the correct triggering, ease of operation and order of both track animations.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

Depends on all AUDIO-01-AUDIO-07; the component tasks do not depend on this acceptance. Currently planned, without starting implementation. Sources: latest user agreement, AUDIO_UX, signal documents, Fellowship, battle, Hunt and menu, R06/R07/R09/R10/R11/R14/R21/R23/R25.

<a id="logika-i-zakres"></a>
## Logic and scope

- Collect evidence of all AU01-AU18 and check the cooperation of actual clips, settings, gestures,
  server and audio policy.
- Play a quiet map browsing, fast cards support, longer battle, traffic Fellowship with Hunt,
  Mordor, treatment and termination.
- Especially check the joint track effect: two animations consecutively, both cue exactly once,
  without applying; Journey: shoes → metal, Mordor: metal → shoes. No fictional step at Stop.
- Integrate the Mute, Reduced Motion, invisible panel, two renderers of the same state, a newer view
  during sequence and reconnect.
- Check for loss of response, recovery command, first view, history and return to finished game
  without old effects.
- Real Windows and macOS tests: Focus loss and minimization, only decision/End in the background,
  no reminders and catch up on return.
- Listen to the color, level, recognition and discretion in a quiet and intense sequence. Automatic
  number of calls does not replace the result of the listening.
- The muted game remains fully playable. Do not add music, external notifications, rules or put down
  expansions.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- All component tasks meet their own criteria, and the AU01-AU18 matrix has a specific tests/evidence
  without known FAIL or omission presented as a success.
- When changing tracks together, the previous variant of playing only the Ring never returns. Two
  clips do not overlap.
- The settings are durable, important signals recognisable at low volume, and the hover and the
  usual browsing remain silent.
- The Windows/macOS trials and listening sessions are documented separately. The absence of
  environment or listening remains an open limit to acceptance.

<a id="weryfikacja"></a>
## Verification

- Full EditMode and targeted collective regression PlayMode base game with real server: gestures/cards,
  dice, battle, Hunt, Fellowship/Mordor, turn signals, game end, Settings and recovery.
- The cue recorder and animation stages give proof of exact order; separate results of the clips
  check and the actual listening.
- Real fokusu/minimizing test on both platforms. According to WORKFLOW without mandatory drop and
  local builds; availability of artifacts/environment save, deficiencies do not count.
- check_docs client, tasks.py validate server and update the cover matrix. Do not change status to
  done with the unexecuted required criterion.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

Update this document, AUDIO_UX (mater AU01-AU18 and limitations), wotr-client:docs/ui/UX_SPEC.md, docs/TESTING.md and ROADMAP. The transfer contains commits, actual commands/results, listening and system tests results, and open-ended other limitations. Without push/publikacji.
