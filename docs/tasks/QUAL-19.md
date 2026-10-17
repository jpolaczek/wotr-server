# QUAL-19 — Base-game regression failures without an owner

## Goal

Implementation: the base-game PlayMode failures found in the UI-164 regression (2026-10-16) that no task owns pass
again, each fixed in the runtime or in its test according to the current UX, without lowering what the test protects.

## Context and dependencies

The UI-164 acceptance compared every remaining failure with baseline `453c88ea` (before UI-210): all of these occur
identically there, so they predate the card-choice work. Owned elsewhere and not part of this task: BattleReport,
ContextHover and MapViewport (QUAL-07, after QUAL-06), login limits and shared-scenario contamination such as
`ViewApplicationNetworkTests` in a pack with `T22TurnNetworkTests` (QUAL-06), and expansion tests (outside the MVP
regression by the user's decision). User request 2026-10-17: diagnose and fix the regression failures.

## Logic and scope

For each test, find whether the runtime or the test departs from the current specification, fix that side and
record the reason:

- `AutoOpenPolicyTests.AnOrdinaryOpponentActionOpensNothingAndTheFellowshipPhaseOnlyForItsPlayer` (Hunt candidate die
  border 3 instead of 2).
- `BoardCompletionLayoutTests.FullRosterVersionsAndHomelandsKeepBothViewsReadableInFourLayouts` and
  `MenuAndSettingsPreserveBattleAndHuntDraftsAcrossFourLayouts` (text taller than its box by 1–2 pt).
- `CardServerCompatibilityTests` ×2 (an element expected visible is hidden).
- `DraftInputTests.HeldEnterCannotSubmitNextSetWhileDelayedPreviewIsPending` (no tray ✓ for the Hunt allocation since
  UI-172).
- `EffectsLayoutTests.MultipleSourcesAbilityEmptyAndExpiredViewsFitFourLayoutsWithoutScrolling` (panel 492 > 475.4).
- `RightColumnLayoutTests.IllustratedFellowshipAndHuntWasteNoRowsInFourLayouts` (compact panel 192 > 84).
- `VictoryPanelLayoutTests.BothScoresThresholdPagesAndFinalBannerFitAllFourLayouts` (shortcut not a pointer target).
- `PairedDecisionPanelNetworkTests` ×2 (window offset 354 and 594 instead of < 1).
- `ShadowStrategy02NetworkTests.CorsairsRequiresAnotherRoundWhenDefenderStays` (its assertion races the automatic No
  card of UI-151).

## Acceptance criteria

- Every listed test passes alone and in its pack; none is skipped, ignored or relaxed without a recorded reason from
  the current specification.
- Each fix keeps the test able to detect the defect it guards (geometry, visibility, single commit, window placement).

## Verification

- Targeted PlayMode packs for the listed classes, then the affected neighbours; full EditMode.

## Documentation and handoff

Update: this file, `docs/TESTING.md`, `docs/ROADMAP.md`, and the client UX documents whose behaviour changes.

## Result (2026-10-17)

Each failure was traced to the side that departed from the current specification:

- Runtime, `DecisionTray.Notice` (TURN_CONSOLE_UX): problems and notices reported through the tray's preview never reached
  the console since UI-160, so "Server update required", offline, refused commands and preview errors were invisible
  while the tray sat in the closed decision window. `CardServerCompatibilityTests` now reads the message in the console's
  hint and its tooltip (the console holds two lines and keeps the full text in the tooltip).
- Runtime, TYPO-01 line heights: Cinzel and Alegreya lines are taller than the old font's, so fixed text boxes clipped:
  the Characters section titles (14 points → 20, short viewport 12 → 18), the roster title (17) and detail (14), and
  the Settings title stretched by its 36-point close button with its own bottom margin (`BoardCompletionLayoutTests`).
- Runtime, Effects panel: the compact mode measured the whole stage, not the space below the docked panel's top, and
  was decided before the docked panel had its place; it now uses the space below the panel and re-evaluates on the
  panel's own geometry, and a compact panel spends less vertical spacing (rows keep their 32-point buttons).
  `EffectsLayoutTests.TextFits` measures at the physical-pixel boundary, as `BoardCompletionLayoutChecks` already did.
- Tests behind the current UX: `AutoOpenPolicyTests` (UI-205's 3-point ring before the first die),
  `DraftInputTests` (UI-172: the Hunt plaque, not a tray ✓, confirms the allocation), `RightColumnLayoutTests`
  (UI-209: the plaque opens the pool list, folded before measuring the compact panel), `VictoryPanelLayoutTests`
  (UI-169: the end-of-game screen opens first; View board returns), `PairedDecisionPanelNetworkTests` (UI-162 removed the
  shared Fellowship–Hunt column; both stay readable side by side), `ShadowStrategy02NetworkTests` Corsairs (UI-151's
  automatic No card; the test guards the second round). `ShadowStrategy03LayoutTests` checks the lit regions instead of a
  hint text, since notices may now lead the hint.

