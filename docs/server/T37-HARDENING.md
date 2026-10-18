<a id="t37--odporność-i-zgodność-zapisu"></a>
# T37 — Resistance and Conformity of Recording

<a id="plan-przed-implementacją"></a>
## Plan before implementation

The range of R09/R10/R11/R13/R14/R25. We do not change the rules or privacy of the projection. `graphicalassets/maps` data appeared while working outside of this task and are not part of it.

1. Play command, receive and snapshot with a `6dcc0b6` commit engine (full database/early dice Lords,
   before the cost fields Lords/Kings). Fixture has a fixed ID and test RNG, without the real
   kont/sesji. Writing before and after command will prove one cost and the same saved draws after
   the retras.
2. New fingerprints have a clear prefix of the version. Canonization will sort out the properties of
   objects, preserve the order of the arrays and skip only values equal to the declared default DTO
   field. `null` list is not an empty list; the swapped cost or route order changes hash. New
   neutral fields do not change hash. The change of meaning/defaultu field requires a new version
   and open migration, not silently ignoring data.
3. The existing unverted SHA-256 compare with the actual historical formats. The contract history
   includes two added fields `RulerTokenId` and `CouncilTokenId`; the format without the field is
   only allowed to be played with the exact default value. We do not match any subsets JSON. Retry
   does not start the engine or RNG.
4. Play the list of ordinary decision commands and its suspended return from the common directory of
   the family and configuration, keeping ID decision, owner, cost, source and stack. Special windows
   retain their own lists; reading cannot move the phase or re-effect.
5. Separate the login limit (429) from the server failure. The answer has a secure code and
   Retry-After; the client shows a understandable reason. Tests do not disable or increase
   production limit.
6. Complete specific audit gaps of authorizations and logs, then run full .NET tests with PostgreSQL
   and targeted client tests for loss of response, two devices and process reboot. The existing
   durability tests include each database decision family and expansions.

<a id="wyniki"></a>
## Results

The historic exporter has actually compiled and launched against the `6dcc0b6` code. He returned the accepted command, the 3 revision and one new RNG score. [Fixture and reproduction](../../tests/fixtures/contract-legacy/README.md) distinguishes the full base course and mechanical scenario of the old Lords letters.

First targeted run: **55/108 PASS**. New hash tests and migrations passed; 52 the frozen body's ratings were still waiting for the version header 1 and comparison PostgreSQL assumed the order of the dictionary keys JSON The body test normalizes only the header version, and the comparison JSONB preserves the full content and order of the boards. Repeat: **108/108 PASS**, zero skip, `t37-legacy-second.trx` .

`t37-boundaries.trx` : **9/9 PASS**, zero skip, HTTP/PostgreSQL. Includes existing races, correspondence confidentiality, 429 Retry-After, no passwords in logs and safe errors damaged/sp No record. `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t37-full.trx' --results-directory .artifacts/test-results` : **3473/3473 PASS**, zero skip, 3 min 47 s, PostgreSQL 17.11 . After the constant canonical pattern has been achieved SHA-256 repeated CommandFingerprint/SaveMigration/LegacyReceipt/HardeningBoundary: **26/26 PASS**, zero skip, `t37-final-boundaries.trx` .

`python3 scripts/check-legacy-fixtures.py`: **PASS**. The script archives the specified commit, compiles its engine and compares all export bytes with fixture. It does not rewrite the current engine pattern.

Unity EditMode : **196/196 PASS**, zero skip, `t37-editor.xml` . After stopping the automatic reconnect for a permanent write error: `ApiFailureTests` **6/6 PASS**, zero skip, `t37-errors-editor.xml` . PlayMode ed `bash scripts/test-client.sh -testFilter 'SaveFailureLayoutTests;TwoIndependentSessionsUseRealServerAndRecoverLobbyAndMap; UI10NetworkTests;ReturnRecoveryTests;ReturnDraftTests;ReturnListTests; MovementNetworkTests;BattlePolicyNetworkTests'` : **12/14 PASS**, zero skip, 14:48:55–14:49:37 UTC , `t37-play-first.xml/log` All of them. 12 the actual network scenarios have passed. Two new Layout tests required a fixture to be improved: MountedMatchClient does not mount navigation ClientFlow inactive draft need not have a Confirm button. After the correction the entire class was repeated SaveFailureLayoutTests : **2/2 PASS**, zero skip, 14:51:10–14:51:12 UTC , `t37-save-layout.xml/log` It's a regression with a targeted rerun, not one run. 14/14. Overviewed Drop CorruptSave 1366 × 768/125% shows a clear reason and access to manual check and game list. Layout uses a controlled error response; tests HTTP/PostgreSQL separate proof of origin. The first runner was interrupted before acceptance too wide filter `NetworkTests` ; is not the result of the tests.

<a id="macierz-zamkniętych-ryzyk"></a>
## Closed risk matrix

| Risk | Proof in full regression .NET / client test |
| --- | --- |
| Two devices, old revision, lost response, one cost/RNG | PostgresTests, UI10ReceiptTests, LegacyReceiptPersistenceTests; UI10/Movement/ReturnRecovery In the Unity. |
| Illegal actor, ID, changed receiver, wrong shape command | TransportTests, UI10PayloadTests, UI10ShapeTests, HardeningBoundaryTests. |
| Restart of the decision measure | BaseCorpusPersistenceTests and the Persistence Tests of individual families; ExpansionPersistenceTests and KingsIntegrationPersistenceTests include expansions and variants. The T33 body retains 212 scenarios. |
| Foreign hands, decks, covered card, Council, Kings and private politics | ProjectionTests, ExpansionPrivacyTests (93 configuring), UI10PrivacyTests, CorrespondencePrivacyTests, AutoDecisionPersistenceTests: raw DTO, options, preview, errors, history, summaries and management of notifications. |
| Ended/revocationed session | LobbyTests; actual login, logout and WSS in NetworkTests client. MatchSocket checks the session before the signal and what 5 is idle. |
| Damaged/sp • Recording, secrets in logs | HardeningBoundaryTests captures the actual logs along with the exception text; private sentinels do not get to logs or answers. |
| Change of contracts between issues | Historical exporter, fixed hash v2, SaveMigrationTests, CommandFingerprintTests, LegacyReceiptPersistenceTests on PostgreSQL. |

<a id="przegląd-struktury-i-ograniczenia"></a>
## Structure review and limitations

The code of compatibility for the receiver remains in Persistence; Snapshot migration and common command directory belong to the pure domain. The rate limit is in Authentication and safe mapping of exceptions in Endpoints. The client separates the transport, connection loop and message UI. The metadata of DTO have cache per type; the receiver does not call the engine or re-engine RNG. No new domain dependencies from Unity, HTTP or PostgreSQL.

The validation of the recording structure includes the header and basic collections, it does not replace the full audit of any possible manual data damage. Compatibility concerns three actual historical shapes of DTO and snapshot v1. Future change of semantics requires another overt migration. Tests are not an external penetration audit. TLS/backup go to T38; Windows runtime and target host remain postponed as ordered.

<a id="decyzje-implementacyjne"></a>
## Decisions implementacyjne

- Snapshot passes from the 1 version to 2. Migration is only in memory when reading; fixation is
  done with the next approved command. The usual Action/FellowshipPhase uses the same directory as
  the new decisions. Special windows are not extended. The new snapshot v2 is not migrated with each
  Copy, which retains purposeful command limitations.
- Fingerprint `2:` sorts properties names and removes their actual default values. DTO metadata are
  indexed once per type. Old hashe retains the frozen order of fields of three published shapes; the
  incompatible new cost cannot be removed during comparison.
- Unknown schema/rules/content returns UnsupportedSaveVersion, incorrect JSON, no header and no
  basic structure returns CorruptSave. HTTP 503 contains only a secure code. Log does not save the
  exception or the status.
- ADMIN-01 (2026-10-18): the admin panel ([ADMIN-PANEL](ADMIN-PANEL.md)) uses the same login limit for its sign-in,
  short admin sessions that a game session cannot replace, administrators named only by `WOTR_ADMIN_ACCOUNTS`, and an
  empty 404 for everyone else.
- The login limit remains 20 trial/min/IP, without queue. The server returns TooManyRequests and
  Retry-After 1–60 p. GameApi preserves the code and delay; it does not repeat the passwords or
  commands themselves. UI shows the right reason.

<a id="sign-in-lock"></a>
## Sign-in lock — SEC-02 (2026-10-18)

On top of the per-address budget (20 sign-ins per minute per network address, `AuthenticationRateLimit.Login`), each
account name has a temporary lock (`Authentication/SignInGuard.cs`), at the owner's request after external security
feedback:

- **Rule:** 10 failed sign-ins of one name within 15 minutes lock that name for 15 minutes, from every address. A new
  15-minute window starts when the lock ends; a successful sign-in clears the name's failures.
- **During the lock** every sign-in of the name answers `429 {"error":"SignInLocked"}` with `Retry-After` (seconds
  left), even with the right password, and the password is not checked (no PBKDF2 work for a locked name).
- **What counts:** a wrong password, a name with no account, and an admin-panel sign-in of an account that is not an
  administrator. Malformed names and passwords are refused before the count. Names are compared in any case.
- **Shared:** game sign-in (`/api/auth/login`), the admin panel (`/api/admin/session`) and the password check of the
  account rename (`/api/account/name`, keyed by the account's current name) use the same lock.
- **Privacy:** an unknown name locks exactly like an account, so the lock says nothing about which names exist. The
  log records that a name was locked, never which.
- **Storage:** in memory of the single server process, at most 10 000 names; when full, names whose window and lock
  are over are forgotten first, then the one active longest ago. A restart clears all counts.
- **Configuration:** `Authentication:SignInFailures` (10), `Authentication:SignInWindowSeconds` (900),
  `Authentication:SignInLockSeconds` (900); a value that is not a positive whole number stops the server at start.
- **Client:** the game and the account-name dialog say "Too many failed sign-ins for this name. Try again in N
  minutes."; the admin page says the same.
- **Trade-off:** someone who knows a player's name can keep that name locked by failing on purpose. The lock is short and
  the per-address budget slows this down; the player can still use an open session.

Tests: `tests/Wotr.Tests/SignInLockTests.cs` (counter, window, bound, configuration, game, unknown name, admin panel and
rename over HTTP); `HardeningBoundaryTests` keeps the per-address limit on distinct names. Client:
`wotr-client:unity/Wotr.Client/Assets/Wotr/Tests/Editor/SignInLockTests.cs`.
