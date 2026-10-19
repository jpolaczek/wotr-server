<a id="t38--prywatne-wdrożenie-i-odtworzenie"></a>
# T38 — Private implementation and restoration

<a id="odbiór-końcowy-2026-10-06"></a>
## acceptance final 2026-10-06

After the release of the seat (7,0 GiB before the start) the full attempt passed. [Machine Report](t38-restore.json) retains the score and ID image.

- `docker compose --env-file infra/.env -f infra/compose.yaml build server` and `up -d --wait`:
  PASS, image `9673073ddf7c`, ready-to-use services.
- `.artifacts/deployment-venv/bin/python scripts/test-deployment.py` : PASS ,
  `.artifacts/deployment-8da8dbc1/result.json` completed 16:17 UTC . HTTPS with validation CA/names
  , WSS , nine tables, both projections, accounts, sessions and receipts according to the copy.
  Retry did not change state nor RNG Continuation battle : revision 5 → 8 , preserved 98 samples RNG
  , six new. The second copy from the active recreated database recovered new decisions; the
  original base remained unchanged. Overwrite refusal verified.
- `docker image load --input .artifacts/deployment-8da8dbc1/backup/server-image.tar.gz`: PASS, read
  the same full ID image as in the manifest (`t38-image-load.log`).
- `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
  --logger 'trx;LogFileName=t38-full-resumed.trx' --results-directory .artifacts/test-results`:
  **3494/3494 PASS**, zero skip, 16:14:32–16:18:33 UTC. Launched with local SDK/.NET CLI home
  described in DEVELOPMENT. Regression also includes migrations and hosting.

The division of Hosting/transport/persistence, infrastructure scripts and tests has been reviewed. No changes to the game rules or UI. T38 is received in the range agreed with the user; the target host, DNS and the public certificate have still not been tested. Next task: T39.

<a id="plan-i-granica-odbioru"></a>
## acceptance plan and boundary

You have commissioned the preparation of configuration/instructions without the target host. acceptance local does not confirm DNS, public certificate or availability from the player network. Windows runtime remains postponed separately in T39.

- Compose: Caddy ends TLS, backend .NET acts as a user without root rights, PostgreSQL does not
  publish a port. Backend trusts one proxy address; the client header itself cannot override HTTPS
  or change the limit rate address.
- PostgreSQL password and connection string in secret files outside Git. Production without HTTP for
  API. Health does not return the game status, identity or connection string; readiness actually
  checks the database.
- Migrations under existing transactional lock. Recreate to new empty base, never erasing an active
  base by the restore script. Copy includes accounts, invitations/sessions, rooms, snapshots,
  intercepts and read progress.
- pinned versions of images and lock files NuGet. Backup retains the metadata of a compatible
  release; the instruction requires an archive of the image with its contents.
- Test: normal account, battle in the middle of the decision, saved receipt/RNG, pg_dump, separate
  empty database, restart and further decision. The verification of TLS/WSS does not disable
  certificate validation.

<a id="źródła-techniczne"></a>
## Technical sources

The Trust Configuration is based on [Forwarded Headers In the ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0). TLS and local certification office: [Caddy automatic HTTPS](https://caddyserver.com/docs/automatic-https). Secret Files: [Docker Compose secrets](https://docs.docker.com/compose/how-tos/use-secrets/). A consistent dump of the running base: [PostgreSQL 17 pg_dump](https://www.postgresql.org/docs/17/app-pgdump.html).

<a id="wcześniejsze-próby-i-diagnostyka"></a>
## Previous tests and diagnostics

The target host is not available; no publication has been made.

- `docker compose --env-file infra/.env -f infra/compose.yaml config --quiet`: PASS. Backend image
  built on Linux ARM64; diggests of four pinned base images, restore NuGet uses lock files.
- Clean Start Compose: PASS, `t38-compose-accepted.log`. Readingness of all services confirmed.
  PostgreSQL: role `wotr` has SUPERUSER=false, CREATEDB=false, CREATEROLE=false, different password
  than administrator.
- Targeted .NET/PostgreSQL: **18/18 PASS**, zero skip, `t38-hosting-pass.trx`; trusted and alien
  proxy, attempt to forge protocol/IP, HTTP production, secret file, login and private errors. The
  first test compilation used withdrawn WebHostBuilder; corrected to HostBuilder.
- `MigrationRecoveryTests`: **1/1 PASS**, zero skip, `t38-migration.trx`; interrupt waiting for
  lock, error after 001 migration, no partial tables/ledger, correct 001–003 repetition.
- Local HTTPS/WSS with Caddy and overt CA has passed the initial steps of the attempt: no default
  trust in CA, correct localhost name, two normal accounts, battle view and revision signal. The
  script does not disable certificate verification.
- **NIE ZALICZONO full backup/restore**: saving the image archive has interrupted the lack of disk
  space. Another attempt is using a stream gzip, but still no space. Folders without the manifest
  are incomplete and are not subject to restoration. The active database has not been switched.
- **NIE ZALICZONO final full regression** `t38-full.log` : PostgreSQL returned `53100: No space
  left on device` ; Runner ended with code 1 It's not a result. PASS No proof of regression of
  rules.

In the first attempt, the dynamic Compose IP backend has taken over the proxy address; both have now open-air addresses. The mounted initialization script on this Docker Desktop reported no execution permission; the initialization replaced SQL, with a correct removal of the end of the line from the secret. Health database performs SELECT 1 as the right role, so it does not consider the unfinished setup to be ready. Each reinitialization concerned only the new test volume created by T38, without the user data.

<a id="miejsce-na-dysku-i-wznowienie"></a>
## Disk space and resume

The 2026-10-06 review found that the original script always copied `wotr`, also after switching the server to the restored base. The amendment introduces `WOTR_DATABASE` in the Compose configuration: the server sets the base from it during the start and the backup reads the same value from a specific running container. The connection secret does not have to be edited on switching. The script binds the base, image and content to one container; the change of the container during the copy prevents the creation of the manifest. acceptance also has to recreate the second copy, performed after the game continued in the first restor. Tests without Docker checked the base selection and refusal to overwrite and inconsistent copy; they do not replace the real dump/restore..

Resume implementation at user's command, with still about 250 Mi B free space:

- `python3 -m unittest discover -s tests/deployment -p 'test_*.py' -v`: **10/10 PASS**. Controlled
  test launches the actual progeny process, forces the ENOSPC write error without filling the disk
  and checks the end of the exporter. Other attempts use the Docker substitutes and are not
  acceptance PostgreSQL.
- `bash scripts/dotnet.sh test tests/Wotr.Tests/Wotr.Tests.csproj --configuration Release
  --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter
  'FullyQualifiedName~DeploymentDatabaseTests|FullyQualifiedName~HostingTests' --logger
  'trx;LogFileName=t38-backup-config.trx' --results-directory .artifacts/test-results`: **19/19
  PASS**, zero skip. Selection of the played base retains password, also with semicolon/council; bad
  name, other host/port or role are rejected.
- `docker compose --env-file infra/.env -f infra/compose.yaml config --quiet`: PASS after change.
  Image with new selection of the database was rebuilt during the final acceptance described at the
  beginning of the report.

During the trial, the free space fell below 200 MB. Completed TRX pre-T37 reports were compressed to `.trx.gz` (286 Mi B retrieved), checking the re-reading of the bytes. Unfinished image archives and previous T36 builds development (retained results and profiles); T39 is expected to generate the current packages again. No data of other projects or Docker's global cache were removed.

Resume procedure recorded at lock — performed in the final acceptance:

1. Release at least 3 GB and check `df -h .`.
2. Start local PostgreSQL (`bash scripts/postgres.sh start`) after checking its log; no space has
   also interrupted the full regression. Rebuild the backend with the database selection: `docker
   compose --env-file infra/.env -f infra/compose.yaml build server`. Re-enable the Test Compose by
   `docker compose --env-file infra/.env -f infra/compose.yaml up -d --wait` — three T38 services
   were stopped after attempts, keeping volumes and secrets.
3. Repeat `scripts/test-deployment.py`: backup, comparison of nine tables in the new base, reboot,
   retry of identical receipt without mutation, further round battle and new RNG by HTTPS/WSS, then
   backup of the active restored base and second restorative keeping new decisions.
4. Repeat the full .NET/PostgreSQL and save the actual result.
5. Only then close T38 and start T39. No target host is postponed at the user's command, but local
   restoration is still the required task criterion.

<a id="budżety-ruchu-zalogowanych-kont--qual-02-2026-10-15"></a>
## Traffic budgets of logged accounts — QUAL-02 (2026-10-15)

Fixing R01 review: the logged player could send unrestrained command rejected. 150 new `commandId` with the wrong `decisionId` saved 150 received in 0,47 p.

Budgets work after authentication, and before the party block, transaction and receipt. Crossing gives 429 with `Retry-After` and `{"error":"TooManyRequests"}`, without receipt and without mutation of the game.

| Variable | Default | Scope | Key of the budget | Applies |
| --- | --- | --- | --- | --- |
| `WOTR_LIMIT_WINDOW_SECONDS` | 10 | 1–3600 | — | The Traffic Budgets Window. |
| `WOTR_LIMIT_COMMANDS` | 60 | 1–10000 | account and batch | `POST /api/matches/{id}/commands` |
| `WOTR_LIMIT_PREVIEWS` | 120 | 1–10000 | account and batch | `POST /api/matches/{id}/preview` |
| `WOTR_LIMIT_READS` | 240 | 1–10000 | account | Batch reading, receive, action log, battle, correspondence. |
| `WOTR_LIMIT_ROOMS` | 60 | 1–10000 | account | `/api/rooms/*` (list, creation, inclusion, start). |
| `WOTR_LIMIT_CONCURRENT` | 8 | 1–256 | account | Concurrent demands HTTP account (no sockets). |
| `WOTR_LIMIT_SOCKETS` | 4 | 1–64 | account | Open sockets `/updates`. |
| `WOTR_LIMIT_SIGNINS` | 20 | 1–10000 | network address, per minute | Sign-ins (`/api/auth/login`, admin panel, rename password check); EXP-00. Client integration raises it. |

- Sign-ins keep their per-address limit of 20 per minute by default (`WOTR_LIMIT_SIGNINS`) and the per-name lock (SEC-02).
- The budget belongs to the account, not to the address. The common NAT does not block the group and
  the account does not use the opponent's budget.
- Two sessions of one account split his budget.
- Heartbeat socket is not a request for HTTP and does not use the command budget.
- Slot socket returns when closing, disconnecting, cancelling and failure.
- Windows and tho-existence are built-in limiters ASP.NET Core. Inactive partitions are released, so
  keys do not grow after short sessions.
- The meters are local to the process. At several instances each counts separately, so the global
  account budget is the sum of the instances; a common counter would require a separate warehouse.
- The bad value stops the server at startup with the variable name.
- The change requires rebooting with a new environment variable.

- The justification for the value.
- The official client sends command to the approved player step (single per second). The preview
  sends draft when changing, and read the view after each revision notification.
- 60 commands and 120 previews on 10 s per batch leaves multiple backups for fast play and automatic
  client testing. The discarded flood holds the highest 6 receive per second per account and
  batch.
- The load measurement of 300 session / 150 batch with the proof review was not repeated. The
  aggregate measurement belongs to QUAL-18.

**Renew.** Client at 429 leaves command as waiting. "Check result" seeks a receipt (404, because 429 does not save it) and sends the same command again with the same `commandId`, without playing a gesture. The client message is "Too many requests. Try again in N seconds."

**Retention of the receipt (QUAL-03).**
- Approved receipts grow with the number of actual batch movements.
- Rejections are limited to 64 per batch and disappear on the next approved move.
- The 008 migration cleans up existing obsolete rejections at the first start of the new version.
  Make a copy before updating with a large database, because cleaning works in migration
  transactions.
- Counters: meter `Wotr.Server.Receipts` (`dotnet-counters monitor
  --counters Wotr.Server.Receipts`).

<a id="budżet-połączeń-postgresql--qual-05-2026-10-15"></a>
## Budget of PostgreSQL — QUAL-05 (2026-10-15)

Fixing R12: 300 simultaneous fresh+WS has given three 53300 (`too_many_connections`) errors, and `Maximum Pool Size=40` has given 300/300 on the same cluster. Pula now has a clear budget for the process:

| Variable | Default | Scope | Meaning |
| --- | --- | --- | --- |
| `WOTR_DB_POOL_MAX` | 40 | 1–1000 | Maximum pool of connections of one server process. |
| `WOTR_DB_POOL_WAIT_SECONDS` | 5 | 1–60 | Time to wait for the free call. Then the response 503. |
| `WOTR_DB_COMMAND_TIMEOUT_SECONDS` | 30 | 1–600 | Time limit for single query. |

- The values replace the corresponding connection chain keys. The wrong value or `Minimum Pool Size`
  greater than the maximum stops the server at startup.
- Cluster budget: sum `WOTR_DB_POOL_MAX` all instances, plus migrations and health check (after 1 ),
  backup ( 1–2 ), other applications and administrator reserve ( `superuser_reserved_connections`
  default 3 ), must be less than `max_connections` (default 100 ).
  - One instance with 40 leaves a spare.
  - Two instances with 40 require checking of other processes.
  - Three instances require a reduction in the pool or an increase in the limit after measurement.
- The transient database error gives 503 `{"error":"ServerBusy"}` with `Retry-After: 2`, without the
  receiver. The transient error is:
  - a full pool after waiting;
  - 53300, restart or broken connection (`NpgsqlException.IsTransient`).

The command result is then unknown, not certain rejection. The client leaves it waiting and recovers the receipt with the same `commandId`.
- The `/updates` open socket closes with the 1013 code (`ServerBusy`) and the client reconnects with
  the increasing delay.
- Logs only state SQL, without the connection chain, session and payoad.
- Meters: `Wotr.Server.Database` (`database.overload.rejected`) and built-in meter `Npgsql`
  (connections in use, waiting, waiting times and timeouts), e.g. `dotnet-counters monitor
  --counters Npgsql,Wotr.Server.Database`.
- Measurement 300 fresh+WS and steamy load 150 of the batch on the real Kestrel belongs to QUAL-18.

