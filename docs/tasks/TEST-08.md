# TEST-08 — Test scenarios for every account on production

## Goal

Integration and operation: the R31 test scenarios (the base-game catalog, TEST-01–TEST-07) are offered to
every signed-in account on the production server, with a limit on how often a scenario can be started.

## Context and dependencies

The owner's request of 2026-10-17: the scenarios they used on the Mac test server, for now available to all
accounts. The mode is in the production image and the released clients; only `WOTR_TEST_SCENARIOS` keeps it
off. Starting a scenario creates a match and a technical seat account, and the start endpoint has no rate
limit.

## Logic and scope

- `POST /api/test-scenarios/{id}/start` gets a per-account rate limit like the demo start.
- Production sets `WOTR_TEST_SCENARIOS=true`; the examples and docs describe the decision.
- Expansion scenarios stay off `main` (TEST-05).

## Acceptance criteria

- On production, a signed-in account sees "Test scenarios" and can start, switch sides, restart and leave one.
- Starting faster than the limit is refused with 429; other accounts are not affected.

## Verification

- `dotnet test` (TestScenarioSessionTests and a new rate-limit test), deploy with a backup, a check of the
  catalog endpoint on production.

## Documentation and handoff

Update: this file, `docs/DECISIONS.md`, `docs/ARCHITECTURE.md`, `infra/README.md`, `infra/.env.example`,
`infra/production.env.example`, `docs/ROADMAP.md`.
