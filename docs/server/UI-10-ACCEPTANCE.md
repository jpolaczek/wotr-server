<a id="ui-10--granica-autorytatywnego-serwera"></a>
# UI-10 — Authoritative server limit

The range of assistance in the active UI-10: production endpoints `view`, `preview`, `commands`, `receipts`, validation of nested payoads and privacy in the race of two devices. The requirements of R09–R11, R19, R21 and [contracts](../CONTRACTS.md); transaction mechanics in [T07](T07-TRANSACTIONS.md). No alternative API or test endpoint is created. Unity client review and testing have separate owners.

<a id="audyt-istniejącej-ścieżki"></a>
## Audit of the existing path

`MatchEndpoints` sets the account from the session. `MatchStore` reads the role from the match record, blocks it in the transaction and checks the fingerprint command before execution. The receiver is associated with the account and ID; the replay does not call the engine. The preview uses the same `GameEngine` validation, without `Apply` and RNG. Information about the settlement queues and saved draws remain on the server side. `Projection` and `Visibility.SafeError` are a common disclosure limit.

| Family | Existing evidence of actual record and follow-up |
| --- | --- |
| Phases, cost, selection, Ring | `TurnPersistenceTests`, `MusterPersistenceTests` |
| Recruitment, excess, army movement | `MusterPersistenceTests`, `Strategy02PersistenceTests`, `ShadowStrategy03PersistenceTests`, `ShadowStrategy04PersistenceTests` |
| Entry and movement of the character | `CharacterPersistenceTests`, `CardPack03PersistenceTests` |
| Secret card combat, throws, losses, retreat | `BattlePersistenceTests`, `CardPackPersistenceTests`, `AutoDecisionPersistenceTests` |
| Stronghold, out, out, out, extra round | `SiegePersistenceTests`, `CorsairPersistenceTests`, `Shadow04PersistenceTests` |
| Fellowship, route, Guide, separation | `FellowshipPersistenceTests`, `CardPack02PersistenceTests` |
| Hunt, replacement, protection and additional tiles | `HuntPersistenceTests`, `Shadow01PersistenceTests`, `Shadow02PersistenceTests`, `Shadow03PersistenceTests`, `BaseCardHuntInteractionTests` |
| Effects selection, table, randomness and opposing window owner | `EffectPersistenceTests`, 16 parcels cards T21 and their persistence tests |
| End of the party | `VictoryPersistenceTests` |

Full acceptance T21 confirmed 1648 cases, without omissions. The above classes describe the existing evidence, not additionally executed course UI-10 The T07 race test so far uses a test response handler; UI-10 should be additionally passed HTTP on the real event and raw JSON The screening tests also have synthetic secrets; we will check the actual covered combat commitment.

<a id="ustalony-plan-weryfikacji-przed-zmianami"></a>
## Established verification plan before revision

1. Raw JSON with `null` in nested Army route is to give a safe error, without exception server,
   mutations and RNG. New `ArmyMoveChoice.Route` is used in marches and ordinary traffic; when
   reading the code it was found to lack its control at the entrance limit of `GameEngine`. The test
   will first confirm the problem.
2. Invalid identifier, collection elements, too long route and foreign ID go through the same
   endpoints of preview and commit. Preview does not save the receipt; rejected commit saves the
   secure receipt for the sender.
3. The two devices of the same account approve the same real action under various command ID.
   Exactly one commit and a set of samples RNG; the second result `StaleRevision`. The reboot and
   the repeat of the win ID return the same receipt. The changed payoad under this ID gives
   `CommandIdConflict`; the opponent does not read the receipt.
4. The selection preview does not reveal another card, does not change the order of the deck and
   does not perform RNG. The secret combat approval of the opponent does not alter its authorized
   public payload or safe errors.

The results and exact commands will be added after a coordinated seal. The test itself does not include writing or means all UI-10 as completed on the basis of the server audit.

<a id="wznowienie-2026-10-05-i-ograniczenie-runnera"></a>
## Resuming 2026-10-05 and limiting the runner

After release of the seat, saved `UI10PayloadTests` , `UI10ReceiptTests` , `UI10PrivacyTests` and `UI10ProtocolFixture` Tests use a production engine and endpoints, real cards raw JSON and existing PostgreSQL Fixture HTTP use `TestHost` , i.e. transport ASP . NET in the process; is not proof of external action Kestrel/WSS Or Unity's client.

The first attempt of `scripts/test.sh` stopped when the existing PostgreSQL was restarted. The second one indicated the existing base by `WOTR_TEST_DB`, without trying to run a new server:

```sh
WOTR_TEST_DB='Host=127.0.0.1;Port=55432;Username=wotr;Database=wotr_test' \
DOTNET_CLI_HOME="$PWD/.artifacts/dotnet-cli" DOTNET_CLI_TELEMETRY_OPTOUT=1 \
DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true \
bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false \
  --filter 'FullyQualifiedName~UI10PayloadTests' \
  --logger 'trx;LogFileName=ui10-payload-repro.trx' --results-directory .artifacts/test-results
```

**PASS compilation; test performed before the first case.**VSTest received `SocketException (13): Permission denied` at `TcpListener.Bind` at its communication channel. Log `.artifacts/UI10-payload-repro.log`, report `.artifacts/test-results/ui10-payload-repro.trx` means discontinued course, not test result. Validation of documentation, directory and base sources in this command has passed. At this stage, `GameEngine` remains unchanged; the expected reproduction of `null` is not yet counted.

<a id="potwierdzona-reprodukcja-i-zakres-poprawki"></a>
### Confirmed reproduction and scope of amendment

Then the ordinary offline program directly triggered four domain methods from the same compiled test library, without VSTest, mailener and network. `.artifacts/UI10-offline-before.log`: **1/4 PASS, 3 actually recreated exceptions**. `route: null` causes `NullReferenceException` in `MovementRules.Plan` and `ShadowMarchRules.ApplyRoute`; `from: null` causes `ArgumentNullException` in the travel region dictionary. `to: null` already returns safe rejection.

The amendment retains the existing limits and moves shape control to a small `CommandShape`. For each army movement requires blank `From`/`To`, a collection `Route` different from `null` (empty list still legal for normal movement), at most 128 steps and blank identifier steps. The region, composition, cost and order is still setting the appropriate rule module. Validation precedes state copying and access to RNG; it does not change DTO contracts or the semantics of correct commands.

Extended matrix `UI10ShapeTests` includes `null` in `Ids`,  `Moves.UnitIds`,  `Moves.CharacterIds`,  `CompanionMoves.Ids`, both fields `Placements`,  `Losses.UnitId` and fields `DieId` / `RegionId` Each case first checks legal command in the actual decision family (rejection card, movement, movement Companion, recruitment, loss, selection or entry of Saruman), and then changes the single raw field JSON Without proof of exception, we do not add new semantics to these validators.

<a id="końcowy-wynik-części-serwerowej"></a>
### Final result of the server part

- **PASS compilation**, no warnings and errors, 3,25 p. Command:
  `DOTNET_CLI_HOME="$PWD/.artifacts/dotnet-cli" DOTNET_CLI_TELEMETRY_OPTOUT=1
  DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true bash scripts/dotnet.sh build tests/Wotr.Tests -c
  Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false`. Log
  `.artifacts/UI10-shape-build.log`.
- **Offline domain 13/13 PASS**, without missing methods. Four cases routes/region and nine cases
  identifier above. Log `.artifacts/UI10-shape-offline.log`; the previous result after the amendment
  itself is 4/4 in `.artifacts/UI10-offline-after.log`. All additional identifier were already
  safely rejected by the relevant rules, so they did not require an extension of the production
  amendment.
- **VSTest not performed**: ban `Bind` its communication channel. This is not the result of a full
  regression.
- **HTTP/PostgreSQL not counted**: six methods were run by a regular program and actual `TestHost`,
  but each stopped before the HTTP with `NpgsqlException` / `SocketException (13): Permission
  denied` when connected to `127.0.0.1:55432`. `.artifacts/UI10-inprocess-http-third.log` separates
  four domain methods from six unavailable HTTP methods. Seventh, later added the HTTP test with the
  identifier matrix remains unexecuted. No other database, proxy, or other transport route has been
  used.

In total, new files declare **20 cases xUnit: 13 domain and 7 with PostgreSQL**. Only the first 13 has the actual result of the execution after the amendment. Offline launch is not a complete runner of xUnit: it does not perform the discovery, lifecycle or report TRX. It calls directly mentioned methods from the production-compiled test library, with their original Assertions.

The program and compilation list are available in `.artifacts/ui10-offline/Program.cs` and `compile.rsp`. The existing DLL tests and local C# compiler, without restore and VSTest:

```sh
DOTNET_CLI_HOME="$PWD/.artifacts/dotnet-cli" DOTNET_CLI_TELEMETRY_OPTOUT=1 \
bash scripts/dotnet.sh .artifacts/dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll \
  @.artifacts/ui10-offline/compile.rsp
DOTNET_CLI_HOME="$PWD/.artifacts/dotnet-cli" DOTNET_CLI_TELEMETRY_OPTOUT=1 \
bash scripts/dotnet.sh exec \
  --runtimeconfig tests/Wotr.Tests/bin/Release/net10.0/Wotr.Tests.runtimeconfig.json \
  --depsfile tests/Wotr.Tests/bin/Release/net10.0/Wotr.Tests.deps.json \
  tests/Wotr.Tests/bin/Release/net10.0/UI10.Offline.dll
```

The HTTP sample used the same DLL files, the `--http` argument, the `tests/Wotr.Tests/bin/Release/net10.0` working directory (required `MvcTestingAppManifest.json`), the above indicated `WOTR_TEST_DB` and `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false`. Disabled configuration observation allowed you to clearly see the refusal of the connection to the base; it did not unlock network sockets. The earlier suspended restores and the TestHost test were discontinued by Ctrl+C; both processes ended with the 130 code.

Structure overview: one internal entry shape class next to `GameEngine`, separate payoad tests, identifier, receipt and privacy and two small test data classes. The changes concern only `GameEngine.cs` and `CommandShape.cs`; no changes to DTO, transport, write and rules of correct movements. The further step acceptance: run `FullyQualifiedName~UI10` and regression of traffic in the VSTest and PostgreSQL environments, then the actual client/Kestrel according to UI-10. The status of the entire task remains within the remit of the integration contractor.

<a id="odbiór-po-odblokowaniu-portów--2026-10-05-2128-utc"></a>
## acceptance after port unlocking — 2026-10-05, 21:28 UTC

Actual `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=ui10-full.trx' --results-directory .artifacts/test-results` ended **1668/1668 PASS , no omissions** including all ** 20 cases UI10 ( 13 domains and 7 HTTP/PostgreSQL * Used local `DOTNET_CLI_HOME=.artifacts/dotnet-cli` , disabled telemetry and notification of workload updates. The script launched the existing PostgreSQL ; no changes in the transport of tests were made. `.artifacts/test-results/ui10-full.trx` , log `.artifacts/ui10-full.log` .

Previous nest refusals remain a historical result of a limited session. Confirmed by proper raw payoads Runner, safe bugs, privacy, one draw result in the device race and reboot and read the receipt. Independent border review `CommandShape` , `GameEngine` , `MatchStore` And endpoints showed no new faults. acceptance client and final status of the whole UI-10 save [network protocol](https://github.com/jpolaczek/wotr-client/blob/81430f29c5d25742c6be32eb79beee12745e9453/docs/ui/UI-10-NETWORK.md).
