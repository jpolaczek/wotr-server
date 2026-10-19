<a id="tryb-demonstracyjny-z-prostym-cpu--demo-00demo-05"></a>
# Demo mode with straight CPU — DEMO-00-DEMO-05

<a id="zlecenie-i-granice"></a>
## Order and boundaries

User order with 2026-10-15:

> add a special game mode, possible to run from the room where you wait for a second player to be
> purely demonstration mode, in which the player will be able to perform moves and familiarize with
> the game, and the moves normally performed by the second player will be performed by CPU. This is
> supposed to be a purely demonstration mode, a bot can be simple as a cepa design, the idea is to
> be able to turn on the game and scroll on another turn. The games fired in this mode will be
> overwritten. Each demonstration will overwrite another, unlike normal games that you can have
> many.

The user's decision is narrow except for the exclusion of the “AI” from MVP (REQUIREMENTS, R32). Bot plays exclusively in the demo party. It is not an opponent of the usual batches, it does not evaluate the position, there are no levels of difficulty or learning. It chooses among the legal options of its site according to simple preferences. Rules, Contracts commands and projections of players remain unchanged.

Default settings adopted (to be corrected by the user):

- demo launches any room player in the state of Waiting. Plays side of its room space and CPU side
  of its room;
- the room remains unchanged and is still waiting for the second player while someone is in it; the
  empty room disappears after 30 s (MENU-27);
- demo is always base game, regardless of the room configuration saved (MVP: only base game);
- one demo per account: a new start removes the previous demo with the entire save, also during the
  game. Normal parties do not change;
- demo is shown in the Games list as "Demo · Free Peoples vs CPU" (or "Shadow vs CPU") with the
  opponent "CPU". They can be resumed and removed (MENU-21/22);
- CPU performs motions at a rate of approximately 0,8 s per step. The client shows them like the
  moves of the opponent: flags, sounds, windows battle and Hunt.

<a id="dane"></a>
## Data

- `009_demo_matches.sql` migration: table `demo_matches`:
  - `match_id` — key, reference to `matches` with cascade;
  - `owner_account`, with a unique index: one demo per account;
  - `cpu_side`;
  - `created_at`.
- Technical account CPU `demo-cpu`, common to all demo parties:
  - `technical = true`, name ‘CPU’, `normalized_name` `#demo-cpu`;
  - password unknown (`!` + random token), so the account cannot log in or get a session;
  - created at the first start of the demo (`ON CONFLICT DO NOTHING`).
- The demo party is a simple line `matches` (player and `demo-cpu` on site sites) with
  `display_name` "Demo · {player page} vs CPU". Receipts, events, log action, battle reports and
  read position work unchanged.

<a id="start-i-nadpisywanie-demo-01"></a>
## Start and Overwrite (DEMO-01)

- `POST /api/rooms/{id}/demo` (logged in room member) → 200 `DemoMatchView { MatchId, You }`.
  - Room unknown, closed or alien → 404.
  - Room with the party started → 409 `MatchAlreadyStarted`.
- One transaction:
  1. block the owner's account line (as in TEST-01), so two clicks will not leave two dem;
  2. remove the previous owner demo by `MatchRemoval.Remove`;
  3. account `demo-cpu`;
  4. `MatchFactory.Create` with configuration of base game and `TurnFlow.Start`;
  5. `MatchStore.Insert` with accounts on site and line `demo_matches`.
- After approval, the server wakes up CPU, which may have the first decision: when the player plays
  Shadow, CPU begins with the Fellowship phase.
- The new contract `DemoMatchView` requires a new version of the Contracts package
  (`scripts/package_deps.py`) and pinned in the client.

<a id="wdrożenie-demo-01-2026-10-15"></a>
### implementation DEMO-01 (2026-10-15)

- `src/Wotr.Server/Demo`: `DemoSessionStore` (start, `CpuSide`) and `DemoEndpoints`; migration
  `009_demo_matches.sql`.
- The room checks `RoomStore.Read` before the transaction. Demo does not change the room, so the
  simultaneous start of the regular batch in this room is independent.
- Contracts 0.5.0 (`DemoMatchView`) and public content 1.0.7, which requires this version.

## Bot (DEMO-02, `Wotr.Domain/Demo`)

- `DemoBot` is pure C# (no database, network and Unity). Entry: `MatchState` and CPU page. Output:
  command or no movement.
- Candidates are created from the projection of the CPU page (`Projection.For`): from its legal
  options and decisions, that is, from the same knowledge that the client gets from this page. Each
  candidate checks the `GameEngine.Validate`. Bot returns the first correct in the order of
  preference, so it does not duplicate the engine rules.
- Simple CPU preferences:
  - action: types of action in the order shuffled with grain with identifier party and revision.
    Select: move Fellowship (Free Peoples), Army movement or attack, recruitment, politics, choosing
    card, character input. The last board is rejection dice (`DiscardDie`) or Pass;
  - phase Fellowship: Next (`ContinueFellowship`);
  - the allocation of Hunt: minimum, and when the minimum is 0, one die;
  - cards recoil and excess figures: the number required, from the first;
  - battle: without card combat, losses from the first legal ones. Attackers continue as long as
    they have more units; defender does not withdraw;
  - Hunt: injuries as Corruption, without reroll;
  - reactions and optional effects: refusal when available;
  - other decisions: first correct option.
- The selection is deterministic for a given batch and revision (`System.Random` with seeds). Draw
  rules continues to perform the server (`IRandomSource`), so the bot does not affect the throws.
- The lack of a valid candidate means no movement. Runner saves the warning and does not loop. The
  “bot vs. bot” tests ensure that this does not occur in the stand.

<a id="wdrożenie-demo-02-2026-10-15"></a>
### implementation DEMO-02 (2026-10-15)

- `src/Wotr.Domain/Demo`:
  - `DemoBot` — selection and validation;
  - `DemoChoice` — FNV grain with identifier batch and revision, weighted order;
  - `DemoCandidates` — a chapter by type of decision and general fallback: each option enabled
    without payload and with each of its dice, then each allowed command decision;
  - candidates action (`DemoActionCandidates`), Fellowship and Hunt, battle and losses, choices,
    recruitment with card and routes (BFS by the neighborhood).
- Details that came out in the "bot versus bot" games:
  - recruitment with card (`RecruitmentPlan`) requires `Placements` in exact numbers,
    nation/region/kind limits and number of regions. Bot tries random order limits;
  - group elections (`SelectionGroup`, e.g. one figure per region);
  - Shadow marches with card: route in motion (`Route` in `ArmyMoveChoice`). Shadow Lengthens
    requires a second army when this one can move, so the bot tries first the most army;
  - Rohan Passage and march with Companion: route in `CommandPayload.Route` at one move.
- Limitation: the Free Peoples bot always takes Hunt injuries to the Ring and often marches, so in
  the gameplay two bots win Shadow by failure, usually in turn 10–22. This is consistent with the
  goal of the "cepa": CPU is to move the game, not play well.

### Demo with released expansions — DEMO-06 (2026-10-19)

- `POST /api/rooms/{id}/demo` plays with the room's configuration, normalized; a room set to a module the release no
  longer offers is refused with 409 `ExpansionsUnavailable` (as its own game would be), and a client older than one of
  its modules gets 409 `ClientUpdateRequired` first (EXP-02 `ModuleCompatibility.ForRoom`).
- `GET /api/rooms/offered-modules` (`OfferedModulesView`) names the released modules, so the menu's demo page shows
  the same rules as the waiting room before it makes its private room; the client configures that room and starts
  the demo from it.
- The CPU answers every decision of the released modules (EXP-03). `DemoAcceptanceTests.AWholeExpansionDemoReachesTheEnd`
  plays one demo per module over HTTP to the end (2026-10-19: Fate 15 turns, Lords 8, Kings 16; Shadow wins each by
  corruption, as in the base game).

### Expansions — EXP-03 (2026-10-19)

The CPU answers every expansion decision, simply and legally (`DemoExpansionCandidates`):

- Companion versions and the starting Guide: a version of each Companion and a Guide at random, then all base
  versions;
- the Council's homelands: one Companion in a homeland of its own about a third of the time (the Shadow then chooses
  a Council token), otherwise all stay with the Fellowship; the Council token at random;
- Dismay: half the time a token re-rolls one of the offered Ruler dice, otherwise the tokens are kept;
- a Corruption tile: draw again now and then while allowed, otherwise keep a drawn tile at random;
- the special die, the Ruler discard and Vilya need only the plain fallback over the options' dice;
- with Kings the Shadow's actions include a corruption attempt (weight 3): a random sovereign with a Hunt Box Eye.

The CPU never spends a Ruler token (a sovereign's token action needs a full recruitment plan), so its games never
grant Dismay; `ExpansionBotGameTests.TheBotAnswersADismayWindowBothWays` gives it the decision directly. Choices follow
the match's id and revision (`DemoChoice`), so setup decisions differ only between matches with different ids.

<a id="wykonanie-ruchów-cpu-demo-03"></a>
## Making CPU movements (DEMO-03)

- `DemoBotRunner` (background service with a wake channel) wakes up after:
  - accepted command player in the demo party;
  - start demo;
  - read the demo batch (`GET /api/matches/{id}`), which resumes CPU after server reboot.
- One loop per batch at a time.
  1. state reading;
  2. bot selection;
  3. `MatchStore.Execute` as a `demo-cpu` account, with an expected current revision and new
     identifier command account;
  4. revision publication in `RevisionHub`.

Steps divide the pause `DemoOptions.StepDelay` (default 0,8 s, in the 0 tests). The loop ends with player decision, game end, no movement or after 500 steps of one wake.
- A conflict with command player settles the line lock and revision (`StaleRevision`). Removed
  during the game ends the loop without error.
- command CPU has a simple receipt, history and log entry action, so the player sees it exactly like
  the human movement.

<a id="wdrożenie-demo-03-2026-10-15"></a>
### implementation DEMO-03 (2026-10-15)

- `DemoBotRunner` (`src/Wotr.Server/Demo`): background service with a wake channel.
  - One loop per batch; waking during the loop tells her to check the state again.
  - The CPU page is remembered for the process (`demo_matches`), so waking up a regular batch costs
    one query per process.
- Wake Up:
  - `POST /api/rooms/{id}/demo` After takeoff;
  - `POST /api/matches/{id}/commands` after the public state change has been adopted;
  - `GET /api/matches/{id}`, or opening of the batch. This resumes CPU after rebooting the server,
    also in the middle of its turn.
- Step CPU:
  1. read as `demo-cpu`;
  2. `DemoBot.Next`;
  3. pause `Wotr:Demo:StepDelayMilliseconds` (800 ms);
  4. `MatchStore.Execute` with an identifier `demo-cpu-<guid>`;
  5. `RevisionHub.Publish`.

Rejected command, e.g. out of date by simultaneous change, ends step and loop reads state again. Removed batch ends loop. No movement on its own decision CPU gives warning in the log. Limit is 500 wake-up steps.

<a id="klient-demo-04"></a>
## Client (DEMO-04)

Details: [DEMO_UX](https://github.com/jpolaczek/wotr-client/blob/dcbc68abd6986917b5747eaf7ed722f876342c36/docs/ui/DEMO_UX.md). In short:

- in the waiting room, the fixed button “Play demo vs CPU” in the row Ready/Start. is greyed when
  the room is not waiting or another operation is taking place. Tooltip explains that the previous
  demo will be replaced;
- click launches the demo and opens board the same way as the start of the batch;
- CPU moves come like the moves of the opponent. The opponent is called ‘CPU’ and the party ‘Demo ·
  ...’ on the Games list.

<a id="scenariusze"></a>
## Scenarios

| ID | Scenario | Task |
| --- | --- | --- |
| DM01 | Start from the waiting room: base game demo, player on the site of your place, CPU on the second; room is still waiting. | DEMO-01 |
| DM02 | Another start removes the previous demo with the receivers, events and journal; the usual parties and other people's demos unchanged. | DEMO-01 |
| DM03 | Alien, closed or started room: 404/409 without effect; two simultaneous starts leave one demo. | DEMO-01 |
| DM04 | Bot selects only command adopted by the engine from the knowledge of its site; each type of decision base game has a candidate. | DEMO-02 |
| DM05 | Bot vs bot since the start of base game (a few grains) comes to the end of the game or limit of the turn without motion. | DEMO-02 |
| DM11 | Bot vs bot for every expansion family and variant (14 configurations, two seeds) finishes; saves read back and projections hide the other side's information at every expansion decision; every expansion decision but Dismay comes up, and a Dismay window gets both answers; one all-expansions game finishes over HTTP and PostgreSQL across five restarts. | EXP-03 |
| DM12 | A demo from a room set to a released module plays with it; an unreleased one is refused; one demo per module reaches the end over HTTP. | DEMO-06 |
| DM06 | CPU plays as long as the decision is his, with pause; the player Shadow sees the Fellowship CPU phase before his own allocation. | DEMO-03 |
| DM07 | CPU moves have receipts, history and log action; the player projection does not reveal more than usual. | DEMO-03 |
| DM08 | Restart the server during turn CPU: read the batch resumes CPU; remove the demo in progress ends the loop. | DEMO-03 |
| DM09 | Client: Room button, start and open board, demo on the Games list, restart replaces demo. | DEMO-04 |
| DM10 | acceptance: several turn-sides against CPU on the real server, game end, no change of the usual parties. | DEMO-05 |

<a id="odbiór-demo-05-2026-10-15"></a>
## acceptance DEMO-05 (2026-10-15)

| ID | Proof | Result |
| --- | --- | --- |
| DM01 | `DemoSessionTests` (page from place, base game, list, room further Waiting) | PASS |
| DM02 | `DemoSessionTests` (second demo removes first, rest stays); `DemoRoomNetworkTests` (list in client) | PASS |
| DM03 | `DemoSessionTests` (404/409, two starts at once) | PASS |
| DM04 | `DemoBotTests` (only your own decision, same move, engine accepts) | PASS |
| DM05 | `DemoBotTests` grain 1–10 and one-time review 11–60: each batch to the end | PASS |
| DM06 | `DemoRunnerTests` (Fellowship CPU phase before player allocation; change pages to the second turn) | PASS |
| DM07 | `DemoRunnerTests` (receipts `demo-cpu`, step in the log action) | PASS |
| DM08 | `DemoRunnerTests` (re-reading, plain batch without CPU, removal in progress) | PASS |
| DM09 | `DemoRoomNetworkTests` (button, board, CPU, replacement) | PASS |
| DM10 | `DemoAcceptanceTests`: whole batch by HTTP on both sides until the end of the game; full `dotnet test` 3931/3931 and the regression of the client menu | PASS |

Restrictions:
- simple CPU Free Peoples loses by failure (according to purpose);
- the manual test at Unity Editor is the user's (optional);
- Contracts 0.5.0 packages, content 1.0.7 and backend `489e5be31221` are only in the local channel.
  The client push requires publication of releases and the host server – implementation; both are
  waiting for the user's consent.

<a id="zadania"></a>
## Tasks

| Order | Task | Dependencies | Result |
| --- | --- | --- | --- |
| Documentation | DEMO-00 | — | This document, DEMO_UX, R32, ROADMAP, WORKFLOW, tasks. |
| 1 | DEMO-01 | DEMO-00 | Table, CPU account, room start and overwrite. |
| 2 | DEMO-02 | DEMO-00 | Bot base game with candidates and “bot vs. bot” tests. |
| 3 | DEMO-03 | DEMO-01, DEMO-02 | CPU mover, resume, test backend. |
| 4 | DEMO-04 | DEMO-03, MENU-22 | Button in the room and opening a demo in the client. |
| Acceptance | DEMO-05 | DEMO-01–DEMO-04 | The game will be both sides and regression. |
