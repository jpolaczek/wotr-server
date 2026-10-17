<a id="qual-17--niezależne-oczekiwania-reguł-i-jawne-luki-pokrycia"></a>
# QUAL-17 — Independent rule expectations and explicit coverage gaps

<a id="cel"></a>
## Goal

Verification of P2 limits with §4 review: critical tests have the expected result derived from the source, not only from the earlier engine result.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

It requires QUAL-04/06. `BaseCorpusExport` to calculate the engine's expected part; snapshot is useful for regression, but is not an independent proof of the rule. Sources: LOGIC, EXPANSIONS, cards/characters manifestos, instructions and FAQ about pinned hash, rule and acceptance directory. There is no proof of the full correctness of all possible combinations of 135 definition of cards; do not declare such coverage.

<a id="logika-i-zakres"></a>
## Logic and scope

- For the base body, inventory expectations derived from the engine and assign the source of
  independent Assertion to the result: legality, resources, order, next decision, visibility,
  victory. Hash remains an auxiliary regression.
- Prepare a table of eight mandatory families: ( 1 ) pas/Pier cuttings and empty pools; ( 2 )
  selection of both cards and the arm limit; ( 3 ) policy across the board action ; ( 4 )
  interruption/resumption card In battle ; ( 5 ) casualties/retreat/ siege ; ( 6 ) Tile
  Hunt/disclosure/Mordor ; ( 7 ) Council/Ruler/Lords and order of special resources; ( 8 ) four
  victories and influence Fate/New Cities.
- Each family has a positive case, negative/Border and save/restart or retra when applicable.
  Expectations to justify the source and page. Cover eight combinations of expansions and indicate
  which variants/cards has been tested.
- Add sequence non-converters: no cost/RNG when rejected, no double cost after retra, the same
  before/after result reboot, projections of states differing in secret do not reveal it to the
  unauthorized side.
- New conflict with the source save as a specific case and the owner's task; no source goes to the
  gaps according to workflow. Do not change the rule with guesswork or finish the task with an
  unattained FAIL.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- All eight families have independent Assertions, Sources and Result made; expectations are not
  calculated by the tested engine/projection/handler.
- The existing body distinguishes the source oracle and regression snapshot. The matrix indicates
  real coverage and other combinations, without the declaration ‘all card work’ solely on the basis
  of the manifesto.
- Restart, retry and privacy have scenarios required by QUAL-12; revealed bugs have tasks and block
  acceptance dependent when they concern its scope.

<a id="weryfikacja"></a>
## Verification

Source table and fixture overview by contractor, targeted domain testing and HTTP/PostgreSQL, full backend and existing manifesto checkers with current TRX. For each family indicate the full name of the test and independently expected result. Do not treat the number of tests as a measure of completeness of all interactions.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

`docs/TESTING.md` , `docs/rules/LOGIC.md` , `docs/rules/EXPANSIONS.md` , the relevant manifests only when they are actually covered, spec/registry ; create a matrix oracle/ source/test. Pass QUAL-12/18 the limits of proof of correctness.
