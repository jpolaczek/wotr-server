<a id="transakcje-i-protokół-t07"></a>
# Transactions and Protocol T07

`MatchStore` blocks one match record by `SELECT … FOR UPDATE`, sets the page from the account and checks the receiver in the same transaction. Idempotence uses the `(match, account, commandId)` key. Fingerprint comes from a standard DTO. Another payoad under the same ID returns `CommandIdConflict`.

The new state, events and receipt are recorded atomicly. Snapshot contains a decision, frames, paid resources, versions and samples RNG. Production randomness is `RandomNumberGenerator.GetInt32`. Unsaved result does not go to the client; the retra after the commit does not draw again.

| Method | Path | Result |
| --- | --- | --- |
| GET | `/api/matches/{id}` | `PlayerView` session accounts |
| POST | `/api/matches/{id}/commands` | Persistent `CommandReceipt` |
| GET | `/api/matches/{id}/receipts/{commandId}` | command own result |
| POST | `/api/matches/{id}/preview` | validation without Apply/RNG |
| WSS | `/api/matches/{id}/updates` | matchId and revision; client downloads projection |
| DELETE | `/api/matches/{id}` | MENU-21: player removal of the game, 204 |

All paths require a session. Alien and non-existent matches return 404. WSS does not send secrets. Subscription before rereading the revision; reading every 5 seconds covers lost notification. The validity of the session is checked also during the connection. WSS does not store the decision.

In addition to `Development`, the server rejects HTTP. Kestrel accepts HTTPS by standard certificate configuration; TestServer with HTTPS scheme does not test the production certificate. The local PostgreSQL 17 runs on the port of 55432 and loopback; `trust` with this cluster serves only local work.

Tests on PostgreSQL: race (one commit), retry after new instance store/pool (without RNG), payload conflict, foreign role, rollback by failure before commit, read both projections and HTTP/WebSocket by TestServer. The failure is injected at the transaction limit; it is not a physical killing process. T09 will check full client–server flow, T37 will expand the failure and playback.

`jsonb` can change the order of the object keys. Comparisons test semantic equivalence with maintaining the order of arrays, especially decks.

<a id="kontrakt-struktury-komendy--qual-01-2026-10-15"></a>
## Command structure contract — QUAL-01 (2026-10-15)

Setting R02 review with 2026-10-13: correct JSON with open `null` gave HTTP 500, e.g. `ContinueFellowship` with `rulerTokenId`, `councilTokenId` or `regionId` equal to `null`. The revision remained unchanged, but the history code read `.Length` with `null`.

**Border.**
- `CommandShape` (Domain, public) checks the entire entry structure as the first step of
  `GameEngine.Validate`. It does so before the party identity, revision, rules, history and record
  of the receipt. Preview passes through the same `Validate`.
- Elements of the structure and conditions:

  | Element | Condition |
  | --- | --- |
  | Fennel | `CommandId` blanks, at most 100 characters; `MatchId` and `DecisionId` not `null`; `Kind` is a defined value. |
  | Payload | Not `null`; `Stepwise` only for types of steps support. |
  | 12 text fields | `dieId` , `ringId` , `rulerTokenId` , `councilTokenId` , `deck` , `nationId` , `cardId` , `reactionId` , `guideId` , `regionId` , `characterId` , `abilityId` : no `null` , at most 128 signs. |
  | Enumy | `face` (optional), `movementCost` , `cardCost` , `huntResponse` , `kind` losses: defined values. |
  | Collections | So far, the size limits. Now also any element: identifier not `null` and at most 128 characters, track steps blank. |
  | Nestled objects | Army movements (`from`, `to` impermeable), deployments, losses, Companion movements, `autoDecision.battleId` movements. |

- The blank text remains legal “unchosen”. Nothing is quietly normalized, so the receipt
  (`CommandFingerprint`) prints retain the meaning. The modification of the fingerprint
  compatibility is not there; it concerns QUAL-03 and QUAL-09.

**Codes.**
- The bad structure gives `MalformedCommand`. The code is safe in `Visibility.SafeError` and does
  not reveal details.
- Correctly built but illegal choice so far, the `IllegalChoice`, e.g. unknown region on the road or
  foreign figure.
- HTTP retains the UI-10 protocol: 200 response with the written rejection receipt, without changing
  revision, decision, cost, history and RNG.
- Exception: missing or excessive length of `commandId`, because such command cannot be linked to
  the receiver. Then the endpoint returns 400 from `MalformedCommand`. The incompatible `matchId`
  route gives 400 from `IllegalChoice`.
- The client does not distinguish the rejection codes, so the change does not require a Contracts
  package or a new pin.

<a id="cykl-życia-receipt--qual-03-2026-10-15"></a>
## Life cycle receipt — QUAL-03 (2026-10-15)

Determination of R01: each domain rejection recorded a permanent receipt, so the flood of the new `commandId` grew without borders (the budgets of QUAL-02 only limit the pace).

* Types of replies and guarantees of re-renewal.**

| Kind | Persistence | Renewing the same `commandId` |
| --- | --- | --- |
| Approved Command | The Receipt continued, in one transaction with the state and RNG, never removed (archive: QUAL-12). | This one. sam receipt. Other payload ♪ give ♪ `CommandIdConflict`. |
| Domain rejection (e.g. `IllegalChoice`, `DecisionExpired`, `StaleRevision`) | Received with `accepted=false`, `revision` and `stored_at` columns while the batch is in this revision. At most 32 to the party account (`MatchStore.RejectionsKept`). | As long as the receiver exists, the same receipt. After the validation is removed on the current revision. |
| Bad structure (`MalformedCommand`, QUAL-01) | No receipt, no lock and no transaction. | Same answer. |
| 429 (QUAL-02) and 503 (write failure) | Without the receipt, the transaction was either not created or withdrawn. | command will be performed normally when the budget or base allows. |

Why the deleted rejection is safe.
- Each command carries `ExpectedRevision` and `DecisionId`.
- When approved command moves the batch further, all rejections responded to an older revision are
  removed.
- Their renewal is performed by simple validation on a new revision and ends with `StaleRevision`,
  `DecisionExpired` or `NotAuthorized`. It will not execute the old intent in the new decision nor
  become the new paid action.
- The rejection code may then change, but the rejected command remains rejected.
- `SetCombatCardPolicy` bypasses the review check, but has a private CAS version (UI-11), so it
  works the same.

**Removing.**
- On each approval: `DELETE … WHERE match_id AND NOT accepted AND revision < nowa rewizja`.
- On each rejection: Delete the rejection of the account in the outside party 32 The latest.
- Both queries use the partial `command_receipts_rejections (match_id, account_id, revision) WHERE
  NOT accepted` index, cover one batch and perform in the same transaction. There is no background
  or block of the entire table.
- Border: 32 tops rejection to the party account, i.e. 64 per party.

**Migration 008.**
- Adds and fills columns with JSON receiver.
- He's setting up an index.
- One time it removes the rejections obsolete from the current batch revision, with the portions
  after 5000 rows.
- Approved receipt does not move.
- Second migration doesn't change anything.

** Meter `Wotr.Server.Receipts` with `receipts.accepted.stored`, `receipts.rejected.stored`, `receipts.rejected.removed` and `commands.malformed` meters, readable by `dotnet-counters`. It does not contain payloads, identifier nor accounts.

<a id="usunięcie-partii--menu-21-2026-10-15"></a>
## Batch Removal — MENU-21 (2026-10-15)

`MatchRemoval.Delete` blocks the batch line (`FOR UPDATE`, player's seat condition) and removes:

1. `command_receipts` and `match_events` game;
2. the room from which the party was created (`rooms.match_id`);
3. row `matches`; `match_reads` and `test_matches` disappear with a cascade.

The same order is used by `TestSessionStore` at the end of the script. command waiting for the lock after removal does not find the batch (404) and does not record the status or the receipt. After approval, `RevisionHub.Publish` wakes the batch sockets. The socket does not find the version, closes, and the client gets 404 after reconnection.

<a id="odczyty-obserwatora--obs-02-2026-10-16"></a>
## Observer readings — OBS-02 (2026-10-16)

The Observer reads the party only via `/api/observe/{id}`. One access rule (`MatchObservers.Watching`) is valid board, history, battle reports and update slot: the account watches the room of this party with valid lease (OBS-01), the room is not closed, and the account does not play the party on any side. The player and the alien get 404, so the `perspective` parameter does not give the player another hand.

- board: `Projection.Observe(state, perspective)` builds the view of the selected page with its hand
  and knowledge. Without option action, token and decision choices (remains type, source, window and
  presentation), without preferences of automatic decisions, action master chips and action
  availability.
- History and reports: `MatchReader.Observer(konto, strona)` transmits the selected page to the same
  queries as for the player of this page. The cursor contains a page, so it does not cross-prospect.
- The socket: `/api/observe/{id}/updates` sends `revision` and `privateVersion = 0`. What refresh
  (highest 5 s) checks the session and access; after the lease, exit or removal of the loop batch
  ends and the socket closes.
- The observer's readings do not record anything: they do not awaken CPU demo, they don't change
  `match_reads` private version or lease. Rent renews `POST /api/rooms/{id}/presence`.
- Player routes (`/api/matches/...`: read, command, preview, retrieve, `seen`, activity, log,
  delete) still require player seat; the viewer gets 404 without changing the status, receive and
  draw.

<a id="nagrywanie-kroków-partii--replay-01-2026-10-16"></a>
## Recording the steps of the batch — REPLAY-01 (2026-10-16)

New batch (`matches.recorded = true`) saves frames in `match_frames`:

1. `MatchStore.Insert` saves the 0 step in the batch creation transaction;
2. `MatchStore.Execute` after the accepted command with `PublicStateChanged` saves the next step in
   the same transaction as the state, events and receipt. The batch line is blocked, so the step
   number (`max(step) + 1`) is unambiguous, and `UNIQUE (match_id, revision)` guards one frame for
   revision.

The command is rewritten to receive a receipt before the engine, so it does not match the frame. The error before the commit also withdraws the frame. Rejection and private preference (`SetCombatCardPolicy` without public change) do not change the revision and do not add a step. The frame contains views of both pages with `Projection.Observe` without history. The story stays in `match_events`. The removal of the batch removes the frames with a cascade (`ON DELETE CASCADE`).
