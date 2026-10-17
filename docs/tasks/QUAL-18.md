<a id="qual-18--zbiorczy-odbiór-poprawy-jakości-obu-repozytoriów"></a>
# QUAL-18 — Combined quality acceptance for both repositories

<a id="cel"></a>
## Goal

Multitask: verify integrated QUAL-01-QUAL-17 amendments on specific client/server commits, without transferring historical PASS to new sources. The ending means acceptance from this plan, not publishing the game.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

Depends on all QUAL-01–QUAL-17. Parts do not depend on this acceptance. Sources: [plan](../reviews/2026-10-14-remediation-plan.md), Part specifications, review/evidence, REQUIREMENTS, Current UX and existing UI-132/UI-133. It does not replace the required signatures, Windows/Intel and implementation DIST tests.

<a id="logika-i-zakres"></a>
## Logic and scope

- Set up a manifesto of the version: commits, Dirty diff, lock, packages and build sources. Matrix
  R01–R12 indicates change, test, result and limitation of any arrangement.
- Complete the full backend, EditMode and the current complete PlayMode in the runner QUAL-06; zero
  FAIL/missing/skip. Tests of new meals and reasons for the backend are part of the inventory. The
  re-openings have visible first results; the guts are not PASS.
- Recreate negative commands/null, legal pass after Ring with reboot, and the immobility after loss
  of response, privacy, migration and history.
- Measure 1/50/150 lots and 2/100/300 Sockets, normal traffic and burst reconnect. Minimum 15 s
  warmup + 60 s measurement for comparison with audit; add 30 -minute soak 150 lot with movement,
  battle , Hunt and card Control mistakes, 429/503 , DB , lock waits, CPU , RSS and data growth.
- Repeat full legacy/in-game/board/motion profiles with QUAL-08 and benchmark long history QUAL-12.
  Fresh builds macOS/Windows, overview of dense scenes and panel boundaries in four layouts. Runtime
  platforms mark according to fact.
- Check anonymous onboarding, licenses/packages and actual required gate CI. No decision/dostThe tooth
  cannot be hidden as a documentary credit.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- Each finding has evidence of closure; all components have met their own criteria. The new problem
  detected has a task and blocks acceptance the appropriate scope.
- Full tests are green without omitting and relaxing requirements. All workloads of profiles made,
  R23 preserved. The incomplete series is FAIL.
- Normal 300 session: no unexpected errors and rejects limiter; Burst: 300 recovered sessions
  without 500/53300. The intentionally overloaded scenario has controlled rejection, limited cost
  and correct recovery.
- Soak shows no unlimited increase in rejected receivers, connection leaks, or blocked batches. The
  latency/CPU/RSS report provides equipment and limitations; local score is not a declaration of SLA
  target host.
- client/server run according to instructions without the author's endorsement; the public CI does
  not allow you to bypass Unity and has confirmed isolation PR.

<a id="weryfikacja"></a>
## Verification

Actual command of the basend and Unity according to DEVELOPMENT and QUAL-06/08, source/manifest checkers with new TRX, anonymous bootstrap, CI, load/reconnect/soak and microbenchmark tests. Both documentation validators, hash control and purified origin results. Raw tokens, passwords and private fixtures do not go to the repository.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

New report acceptance In `docs/reviews` , `docs/TESTING.md` , `docs/ROADMAP.md` , `docs/ARCHITECTURE.md` , `wotr-client:docs/ui/SCENARIOS.md` , spec/rejestr. Transmission indicates commits, checks performed, open limits and other independent release tasks. Do not add manually acceptance user.
