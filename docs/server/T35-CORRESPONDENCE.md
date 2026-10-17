<a id="t35--trwała-korespondencja"></a>
# T35 — Permanent correspondence

<a id="cel-i-granice"></a>
## Purpose and boundaries

R05/R07/R11/R13/R21. product contract No game rules added. The server retains the current snapshot, decision stack, receivers and RNG. T35 provides multiple batch reading and browsing progress; the list client and local durability draft belong to UI-12. The status of the task is only in the register.

<a id="plan-przed-implementacją"></a>
## Plan before implementation

- The account list returns a limited metadata page: ID, own page, configuration, turn/phase,
  revision, decision owner, type of your decision only and public score. It does not calculate legal
  options or send snapshots for all parties. Filters: All, YourDecision, Waiting, Finished.
- Order according to stable ID and cursor of the last item, default 20, maximum 50. The batch change
  does not move it while downloading the following pages. Refreshing from the beginning finds the
  newly added batches and filter changes.
- `match_reads` stores `(match_id, account_id, revision)`. No entry means -1, or a game not yet
  opened. Ack has only a revision of the actual view, never the default "all up to now". It is
  monotonous, idmpotent, rejects future/negative revision and foreign account. It does not change
  game revision, decision, RNG, receivers or enemy preferences.
- GET lists/state/Summary does not save ack. Summary filters events by the same visibility
  policy as the player's history, before limiting the number of entries. Maximum 50 the latest
  visible entries after the selected revision (default the default saved ack); clear older entries
  tag.
- We do not publish `updated_at`, as it also changes with private cards combat policy. The list does
  not reveal its time or version. It does not send other people's choices, sources of private
  window, hands, decks and covered tiles.
- Each reading re-establishes the account from the session. Lost WebSocket/notification does not
  affect the recovery of the list and current projection from PostgreSQL. No process per batch,
  animation play or client memory dependencies.

<a id="weryfikacja"></a>
## Verification

1. Several batches saved to the real PostgreSQL in battle, Hunt, Council and Kings. Stop host
   without clients, open new, read both letters and continue any decision without repeating
   kosztu/RNG.
2. Change only your opponent's personal data; his private secrets and acks cannot change the payoad
   of the other side. Filter before the limit of history.
3. Empty account, foreign ID, incorrect Limits/cursor, pagination and filters; no mutations after
   reading. Ack out of range, retries, older ack and parallel new decision cannot delete newer
   notification.
4. server regression, common compilation DTO in Unity, structure overview, `tasks.py validate` and
   `git diff --check`. Save actual results.

<a id="implementacja-i-wyniki-cząstkowe"></a>
## Implementation and sub-results

`MatchIndexStore` reads the headers by SQL, `CorrespondenceStore` captures the ack and reads the summary of one recording, and `CorrespondenceEndpoints` validates the requests. `HistoryProjection` shares the visibility policy with board. Migration 003 adds only the table of readings and pagination indices. A transaction ack uses the block of the shared party record and `GREATEST` at the upsert; the command still uses its own lock and receipt. There is no loop, to-do in background or additional object in memory for each saved batch.

Server Build Release: **0 errors 0 warnings**, `.artifacts/t35-build.log` The first reboot test compared the order of the keys JSON which PostgreSQL can change; it has been changed to a structural comparison of the full state. Then **5/5 PASS**, `.artifacts/test-results/t35-first.trx` . Final filter `Correspondence|ProjectionTests|TransportTests|PostgresTests` : **16/16 PASS**, no omissions, `.artifacts/test-results/t35-focused.trx` Command: `scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~Correspondence|FullyQualifiedName~ProjectionTests|FullyQualifiedName~TransportTests|FullyQualifiedName~PostgresTests'` .

Seven new PostgreSQL/HTTP tests include rebooting four real windows and adminent continuation, 55 game/pages/filters, all 30 types of protocol decisions, both sides of privacy, limit only after the visibility filter, no mutations after GET, durability of separate ack, 12 parallel ack and a race with a new decision. Sessions and accounts are real, WebSocket is not opened. Full regression is recorded after its completion.

Unity `scripts/unity.sh -runTests -testPlatform EditMode`: **175/175 PASS**, no omissions, `.artifacts/t35-editor.xml`, 11:48:40–11:48:42 UTC. New attempt reads the actual shape of JSON list by Newtonsoft: own Dismay, no other decision, no reading record, and cursor. Common DTO remain without Unity.

Reading track check: `EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)` exact SQL   `MatchIndexStore`, account with 55 lots, limit 21  (20 + detection of the downstream side), PostgreSQL   17 on localhost/M1 Pro during full tests. The plan used indexes `matches_fp` / `matches_sh`; Planning **0,342 ms**, execution **4,367 ms**. Small fixture does not prove performance on any scale; the result justifies limited header reading without projection of legal options. The article contains only metrics: `.artifacts/t35-list-plan.json`. Indexes with ID are also available to other parties; the selection of the plan is up to PostgreSQL.

Structure overview: endpoints 40 line, warehouses 60/65, common projection of the history of 12. Layers remain separated, without domain dependencies from HTTP/bazy. The recording itself ack does not scan events or generate options.

<a id="odbiór"></a>
## Acceptance

Full `scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t35-full.trx' --results-directory .artifacts/test-results` : **3448/3448 PASS**, no omissions, 11:50:10–11:53:51 UTC , test time 3 min 39 p. Report `.artifacts/test-results/t35-full.trx` Checker `expansion_acceptance.py --results .artifacts/test-results/t35-full.trx` : **1107 methods / 2560 cases PASS**, a set of manifestos expansions and configuration. `tasks.py validate` : 88 tasks PASS ; `git diff --check` : PASS .

acceptance T35 is about API, durability in PostgreSQL and client contracts. It does not yet include list view, local write draft or interrupt Unity process after sending command. This is the next ready-to-use task of UI-12. The rules, external communication channels or configuration of implementation have not been changed.
