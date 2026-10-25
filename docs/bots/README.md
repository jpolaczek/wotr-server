# Bot inventory and implementation handoff

The [service design](../server/STRATEGIC-BOTS.md) is the product contract.
[coverage.json](coverage.json) is the public inventory, not proof of bot strength.
It currently contains 1,399 mechanics and 2,798 **planned** scenario obligations.
Every legal/purposeful evidence field is pending. Existing `ruleTests` refer to
game-rule tests and cannot establish bot behavior.

The inventory includes each manifest card's implemented event/combat half,
character entry/exit/ability, source clause and token; documented and runtime
decisions; recursive command payload types and enums; phases, costs and victory;
and each legal normalized configuration. An ability's `choice-and-consequence`
mode requires candidate generation only when a player option exists, plus
valuation of passive effects. Never manufacture a command for a passive effect.
Scenario intents distinguish useful use, retention/decline/illegal alternatives,
automatic consequences and absent triggers. Private implementation tasks must
provide concrete paired positions and assertions for these obligations.

[runtime-catalog.json](runtime-catalog.json) is exported by
`tools/Wotr.BotCatalog` from the actual Contracts and Domain assemblies. It tests
all 768 input combinations, including nullable Dismay's default: 606 are rejected;
the valid inputs collapse to 93 normalized configurations. `ConfigurationRules`
supplies validity and `CardCatalog.Enabled` supplies replacements and exclusions.
Character availability additionally requires the current roster, chosen version
and ordinary player options; configuration flags alone do not grant entry.

```sh
python3 scripts/bot_coverage.py --generate
python3 scripts/bot_coverage.py
python3 -m unittest discover -s tests/bots -v
```

Regeneration intentionally resets evidence to pending. Review the diff when rule,
contract, manifest or content inputs change. Public CI detects stale compiled
inputs and regenerates the catalog using .NET; it needs no private credentials.
Unknown content/rules, omissions, duplicates, dangling scenarios and fabricated
passes fail validation. Acceptance-report import is a later BOT-14/BOT-20
deliverable: it must bind private build/rules pins, scenario IDs, commands, seeds,
counts and results without exposing weights, plans or private source. Until that
verifier exists the public schema accepts **only pending** behavioral evidence.

## Frozen handoff contracts (version 1)

These are implementation requirements. BOT-01 implements the pure observation/
memory boundary; BOT-16 implements [restricted server authority and player API
access](../server/BOT-AUTHORIZATION.md). BOT-12 implements [room admission](../server/BOT-ROOMS.md);
assignment delivery and the private worker/journal remain pending.

| Contract | Required data and responsibility | Owner |
| --- | --- | --- |
| `BotObservation` | Schema version, assigned match/side, rules/content compatibility and a copied ordinary `PlayerView`. Include only that seat's hand, knowledge and already received public history/reports. All supplemental reads use the seat identity. No `MatchState`, DB, observer, RNG/deck order or other hand. | BOT-01 |
| `BotMemory` | Schema and policy version, accepted revision, one main/up to two supporting plans, known facts, soft resource reservations and acknowledged progress. Reject unsupported versions explicitly. Memory has no authority to make an illegal option legal. | BOT-01/BOT-05 |
| `DecisionBudget` | 3-second target, 5-second hard compute deadline, cancellation and reproducible private sampling seed. Propagate remaining budget into candidate generation, simulations and lookahead. A deadline is never permission to invent a pass. | BOT-02/BOT-04/BOT-05 |
| `IBotPolicy.Decide` | Observation + memory + budget -> one proposed ordinary `GameCommand`, proposed next memory and private diagnostics, or explicit `UnsupportedDecision`/`NoLegalCandidate`/cancelled outcome. The policy has no network or persistence dependency. Command remains uncommitted. | BOT-15/BOT-01/BOT-03 |
| Simulation | Build hypothetical worlds from the observation and sampled unknowns; clone them; call the pinned pure engine. Each simulated actor gets its own projection. Expose scores/results to policy, never a live-state supplier. | BOT-02 |
| Assignment control | Assignment ID, generation/fence, room/match/side, service/profile/version, renewable lease and readiness. Service identity can claim/renew/cancel; it cannot read or play a match. | BOT-16/BOT-17 |
| Seat transport | Separate scoped seat credential; ordinary player view, options/previews, history/reports, updates, commands and receipts. Authenticate compatibility; enforce generation inside the mutation lock. Never reuse the host's session. | BOT-16/BOT-18 |
| Private journal | Before sending: exact command bytes/ID, observation revision, assignment generation and proposed memory. After accepted receipt: acknowledge and advance memory once in the private store. Unknown outcome: read receipt, then resend identical bytes/ID if necessary. Rejected/stale: refresh without advancing progress. | BOT-18 |

The game database owns state and receipts; the private service owns memory and
journal. There is no distributed transaction. Reassignment/restart recovers a
pending command before choosing another one. Unsupported decisions and empty
candidate sets produce a private diagnostic with a mechanic ID, refresh the
ordinary player observation once, and then leave the assignment pending/unready
with bounded retry. They cannot consume a die, choose an arbitrary decline or
silently substitute the legacy demo policy.

Integration seams are `GameRuntime`/`GameEngine` for hypothetical execution,
`Projection.For` for information boundaries, `MatchStore` and ordinary endpoints
for authoritative mutations/receipts, and the client's host room for CPU
invitation/readiness. `DemoBot` remains a legacy opponent and a frozen benchmark,
not the implementation of either new strategic policy.
