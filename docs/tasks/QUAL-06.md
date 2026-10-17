<a id="qual-06--izolowane-playmode-i-kompletny-inventory-testów"></a>
# QUAL-06 — Isolated PlayMode runs and complete test inventory

<a id="cel"></a>
## Goal

Infrastructure implementation acceptance, P1 in the plan: the full current PlayMode set starts in isolated packages, and the result reveals each FAIL, missing, skip, additional case and source change.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

R11 review; `wotr-client:scripts/test-client.sh`, `test-client-batches.py`, network fixture, seeder and login limits. No new dependencies. Audit: 520/574 PASS in full run, 37 login-related failures; aggregation of isolated 567/574, seven permanent FAIL. These numbers apply to the old client. Current inventory must also include UI-130/UI-131. tests

<a id="logika-i-zakres"></a>
## Logic and scope

- Set up the current inventory from the discovery of the correct Unity version and sources. Old XML
  cannot define the full set by itself after adding new tests.
- Each package has its own server process, database/scheme, accounts, sessions, result directories
  and UI status. Removing resources only includes your own fixture.
- Keep production login limits. Select the size of the packages/cycle server life to budget; do not add
  universal limit bypass for tests.
- Check the contamination in the package, order of classes and repeat in a fresh environment. The
  full test path along with the parameters identifies the result.
- The manifest gives SHA both repositories, lock dependencies, build sources, XML/logs and all
  reruns. The first FAIL remains visible; the automatic rerun does not convert the unstable
  test into a normal PASS.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- Runner detects a lack of one case, skip, incorrect filter and source change; returns non-zero code
  instead of accepting incomplete result.
- Each current case is executed without 429 login and sharing of account status. Two full series
  with altered order do not show new failure dependent on order; permanent FAIL have the same
  owners.
- QUAL-06 can be picked up with confirmed FAIL belonging to QUAL-07/08; it is not acceptance game
  nor green gate CI. New FAIL does not hide the allowlist.

<a id="weryfikacja"></a>
## Verification

Expand existing runner tests with only significant completeness and isolation errors. Complete full inventory in packages and a second series in a different order on unchanged sources. Use `python3 scripts/test-client-batches.py --inventory <current-discovery.xml> --output <new-directory> --batching mixed` with size resulting from budget. Save actual commands and results.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

`docs/TESTING.md`, `docs/DEVELOPMENT.md`, spec and register; client manual `wotr-client:docs/DEVELOPMENT.md`. Transfer the inventory and causes of FAIL to QUAL-07/08/15. Do not overwrite historical audit results.
