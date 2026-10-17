<a id="dowody-audytu-wotr--2026-10-1314"></a>
# Evidence of audit WoTR — 2026-10-13/14

Rated sources: `63e9e205a79d8c1f3413845a6decee0f3ea2b320` server, `08eb95678a3275edfadcaffe2034e52808d19d9d` client; backend of the client from `eac4afe342fa82f136dce34c0e36fe8ab59bdfeb` lock.

<a id="zawartość"></a>
## Content

- `test-summary.json`: full backend and EditMode, first full PlayMode, open repetitions, final
  567/574 aggregate, and integrations HEAD 6/6. aggregate is not a single clean run. SHA-256
  indicate the maintained raw TRX/XML.
- `rules-probes.json`, `http-probes.json`: independent pass after Ring, 900 null mutations in 25
  states, confirmed HTTP500 and write 150 rejections.
- `history-micro.json`: same decision with growing synthetic history, off base.
- `load-benchmark.json` , `load-300-db.json` , `db-load-300-samples.json` : real HTTP/WS , 1/50/150
  table, decision every 5s; 15s warm-up + 60s measurement. Second file is repetition 150 Tables with
  sampling of the base what 0 ,5s.
- `reconnect-burst*.json`: 300 simultaneous refresh+WS, before and after setting up the only audit
  pool on 40. PostgreSQL had max_connections=100 and was shared with test bases.
- `profile-*.json`, `client-profile-summary.json`: full board/motion and openly partial
  legacy/in-game. Incomplete reports are not PASS. `profile-*-status.json` retain the reasons for
  interruption.
- `profiler-checkpoint.patch` , `checkpoint-instrumentation.json` : only change of runtime in the
  measuring tool — record of completed samples on the existing exception path; no change of the
  measuring and production loop UI The source after the construction was restored.
  `build-provenance.json` document earlier build without this instrumentation and compatibility 1194
  files runtime/resource Both builds had a separate identity `com.wotr.review20261013`.
- `content-checks.json` : 11 actual checks on sources, manifestos and acceptance on fresh TRX and
  anonymous HTTP404 Contracts release.
- `secret-pattern-scan.json`, `dependency-vulnerabilities.json`: limited scope of scans; they are
  not a guarantee of the absence of any secrets/reliability.
- `characters-1366.png` : fresh synthetic presentation scenario, not manual acceptance the whole
  party.

Private fixtures with tokens and test passwords do not enter this directory. Raw reports remain locally in `.artifacts/audit` checkouts `audit-server`, `audit-client` and `audit-client-build`. The JSON data in this directory are cleared results from dedicated audit data.

<a id="ponowienie-prób-domeny"></a>
## Re-testing domain

From the main server repository directory, to the attached source version, with the appropriate .NET SDK:

```sh
mkdir -p .artifacts/audit
bash scripts/dotnet.sh run --project docs/reviews/2026-10-13-evidence/AuditProbe.csproj --configuration Release -- rules .artifacts/audit/rules.json
bash scripts/dotnet.sh run --project docs/reviews/2026-10-13-evidence/AuditProbe.csproj --configuration Release -- micro .artifacts/audit/micro.json
```

`rules` uses frozen condition of body and current production GameRuntime; does not use the database. `micro` measures a simple decision ContinueFellowship and synthetic events with text 160 characters; 10 warm-up attempts and 25 measured for operation.

<a id="obciążenie--wyłącznie-dedykowane-środowisko"></a>
## Load — dedicated environment only

The `seed` mode requires `WOTR_AUDIT_DB` to indicate a separate database **audit_load_test**. It saves 150 of the batch and 300 of the session and the private file `.artifacts/audit/load-private.json`; hashe test accounts are not used for login. Do not use this data at the normal instance of the game.

`load` and `reconnect` modes deliberately use only `127.0.0.1:50973` and private fixture created by the seed. For `load` set `WOTR_AUDIT_SERVER_PID` to PID own audit Kestrel; `WOTR_AUDIT_TABLES` defaults to `1,50,150`. Backend in the audit worked in Development without TLS. The operator must recreate the isolation of the base and port before performing these modes; no production base is part of these attempts.

The test code is an additional audit tool, outside the production solution. The results concern the machine described and the load, do not guarantee the capacity of any host.
