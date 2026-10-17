<a id="qual-08--kompletne-drivery-i-powtarzalny-pomiar-klienta"></a>
# QUAL-08 — Complete test drivers and reproducible client profiling

<a id="cel"></a>
## Goal

P2 implementation for R11 and incomplete measurements: full legacy/in-game and board/motion perform all required workloads on fresh build.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

It requires QUAL-06/07. Sources: Evidence Review, `DesktopProfile`, `InGameProfilePanels`, `InGameProfileDriverTests`, `scripts/profile-client.py` and Validators. Historically, the board 40/40 and motion 32/32 have been included, the legs 5/56, in-game 4/32 incomplete. This does not prove poor performance of the renderr.

<a id="logika-i-zakres"></a>
## Logic and scope

- Fix the legal sequence of opening/download data/Layout before gesture on `board-menu-victory`,
  with open hand and on navigation of the previous report Polowania/karty. Driver awaits the
  observed condition and available bounces.
- The driver test must demonstrate a change in the expected condition and the render effect, not
  only the existence of the element. Use real projections and actual gestures, including
  UI-130/UI-131; do not add a bypass shortcut to the tested UI.
- Write results/checkpoint at error to implement openly in the diagnostic tool, with the
  identification of FAIL. Do not hide interruptions and do not use undocumented source modification
  between build and its origin designation.
- Separate the time of the handler, render-end and frame from sieci/input/display. Save sources, hash
  build, machine configuration, actual size, warm-up, samples, FPS, p99, allocations and
  availability of meters.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- `InGameProfileDriverTests.AllEightRealProfileDriversChangeAndRenderTheirExpectedStateInFourLayouts`
  passes in isolation and full inventory.
- All four sets workloads end with result and pass validator. Inventory changes consciously updated;
  do not reduce the number of cases to bypass the previous failure. Both sizes, scales of 100/125%,
  normal and limited movement are covered according to the set's purpose.
- R23: average ≥60 FPS and local feedback ≤100 ms on the stored equipment; keep the p99/GC. report
  No counter or platform available is public.

<a id="weryfikacja"></a>
## Verification

Fresh Development build with source manifesto; `scripts/profile-client.py --suite legacy`, `in-game`, `board`, `figure-motion` with own views/report/player. Confirm the package names in the current CLI before start; use existing validators and Python profilers tests. Overview of dumps and full PlayMode in the QUAL-06 runner. Do not count macOS ARM as Windows/Intel runtime.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

`docs/TESTING.md`, `wotr-client:docs/DEVELOPMENT.md`, `wotr-client:docs/ui/SCENARIOS.md`, spec and register. New cleaned reports saved next to historical, without private fixture. Forward QUAL-15/18 complete inventory and measurement environment requirements.
