<a id="qual-07--aktualne-scenariusze-ui-i-stabilna-geometria"></a>
# QUAL-07 — Current UI scenarios and stable geometry

<a id="cel"></a>
## Goal

P2 implementation for six permanent failures R11: tests check the current UX and actually achieve the intended conditions; the production errors revealed by these tests are repaired without reducing requirements.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

Requires QUAL-06 Seventh defeat ( `InGameProfileDriverTests` ) belongs to QUAL-08 Sources: [review R11](../reviews/2026-10-13-code-review.md) , present UX_SPEC , BOARD_INTERACTION_UX , BATTLE_WINDOW_UX , ARMY_MAP_UX , UI-100/UI-124 – UI-131 . The current meal panel is part of the regression; do not play the old UX.

<a id="logika-i-zakres"></a>
## Logic and scope

- `BattleReportNetworkTests.ClosedReportRejectsLateHttpAndFailedReadCanRetry`: after reading the
  report, the icon of the last unread fight disappears according to UI-100. Reread it by legal path
  through history. Keep the ratings of rejecting the late response and re-reading after the error of
  HTTP.
- `ContextHoverTests.ClickedInspectionIsInteractiveStableAndFitsDenseRegionsAtBothScales`: set the
  moment of the stable Layout/animation; check visibility, hit and stability in units with the
  described rounding. The difference around 0,55 px does not automatically entitle to remove the
  Assertion.
- `ExpansionWindowNetworkTests.EvilKnowledgeRetainsTheEliteAndFullBattleResourcesFit` ,
  `KingsCards02NetworkTests.HopeChoosesWhichInitialSovereignAwakensDuringBattle` ,
  `ShadowStrategy01LayoutTests.ServerProjectedMapChoicesSacrificesAndSiegesFitBothSizesAndScales` :
  settle real click area and scale conversion. Common minimum 32 with result 31,86555 fix in
  geometry/zaokr agling according to the contract in force, keeping the rules’ ratings cards and
  resources.
- `MapViewportTests.MovingArmyCannotStopNavigationUnderAStationaryEdgePointerAtFourLayouts`: lead
  the animated figure physically under a motionless indicator at the shore, openly check this
  condition and only the continuity of edge-pan. If the branch reveals a production error, correct
  it in this task.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- All six named tests pass separately and in full packages at both resolutions and scales. They are
  not skip/ignored/quarantine.
- Each test still detects a proper breach: late report, loss of interaction, too small hitbox or
  edge-pan stop under figure.
- Each tolerance is derived from the contract pixels/animation and has a justification; removing
  strategic assertion or general increase in timeouts is not a repair.

<a id="weryfikacja"></a>
## Verification

Aimed PlayMode listed methods in fresh fixture, then full runner QUAL-06. Review of dumps/hitboxes in four layouts, both sides; regression of clicks, pressed, Esc, drag reserves and army movement. Save separately the change of test and change of runtime and proof of why they correspond to the current specification.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

`docs/TESTING.md`, the appropriate `wotr-client:docs/ui/SCENARIOS.md`, `BOARD_INTERACTION_UX.md`, `ARMY_MAP_UX.md` and `BATTLE_WINDOW_UX.md`, spec/rejestr. Forward QUAL-08/10 stable waiting for layout and evidence of geometry.
