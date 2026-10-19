<a id="uruchamianie-projektu"></a>
# Launching the project

Execute commands from the main repository directory. SDK : . NET 10.0.401 ( `global.json` ), Unity 6000.6 .0f1, a client with Mono on desktops. The common Contracts package has a profile . NET Standard 2.1 and is a local Unity package. The engine and server use . NET 10 . .

## .NET

```sh
bash scripts/dotnet.sh restore --locked-mode
bash scripts/dotnet.sh test --configuration Release
bash scripts/dotnet.sh run --project src/Wotr.Server
```

The script uses the local `.artifacts/dotnet` if there is, otherwise SDK with PATH. The installation of SDK in this environment has been made official dotnet-install to the project directory, without replacing the global SDK 9. On the new computer, install the version with `global.json` according to [Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script).

`packages.lock.json` files perpetuate the dissolved dependencies. The Contracts compile output goes beyond the local Unity package so that the editor does not import the sources simultaneously and DLL from `bin/obj`.

## Unity

Unity Client is in a separate `jpolaczek/wotr-client` repository (DIST-03). Building, EditMode/PlayMode tests and packages describes its [DEVELOPMENT](https://github.com/jpolaczek/wotr-client/blob/1232d72e5b3f71c9f8d1e688a4f268e0000a20f2/docs/DEVELOPMENT.md); this link moves `python3 scripts/tasks.py pin-repo wotr-client`.

<a id="środowisko-po-podziale--dist-03"></a>
## Environment after division — DIST-03

`global.json` requires .NET SDK 10.0.401. `scripts/dotnet.sh` uses `.artifacts/dotnet/dotnet` if it exists (e.g. link to local SDK), otherwise the system `dotnet`. Tests with isolated PostgreSQL (`IsolatedPostgres`) require a correct location, e.g. `LC_ALL=en_US.UTF-8`; without it, the postmaster on macOS ends up with a startup "became multithreaded during".

<a id="baza-danych-i-ci"></a>
## Database and CI

Backend uses PostgreSQL 17. In this environment, separate `postgresql@17` (17.11) has been installed, leaving PostgreSQL 14 and global links. T07 adds an isolated cluster of design and transaction tests. No automatic service has been launched on login.

CI `.github/workflows/ci.yml` (Verify server) launches document validator, parcel testing and .NET on Linux. Job Unity is in `wotr-client` and uses pinned server archives.

From BUILD-03 Verify server has two jobs to save minutes GitHub Actions (repositories are private, the plan has a monthly limit):
- `checks` always works, about a minute: `tasks.py validate`, Python tests (`tests/ci`,
  `tests/packages`, `tests/release`), source scripts and cards coverage;
- `backend` works only when the change touches something outside the documentation. It is
  PostgreSQL, `dotnet restore`/`test` and the coverage on the test results, about 12 minutes.

Scope `scripts/ci_scope.py` Documentation is `docs/**` and files `.md` in the root directory, including pinned links `wotr-client` Except for the files read by the tests. NET : Data files in `docs/sources/` (index) cards JSON ) and `docs/TEST_SCENARIOS.md` No comparison database (new branch, tag) means full run. On branches other than `main` `concurrency` Cancels the run replaced by a newer push. On `main` Each push has its own run because each one is just checking its change. First version BUILD-03 cancelled on `main` And the commit with the documentation itself, pushed right after the commut with the code, interrupted his tests. NET When the test. NET starts reading a new file with `docs/` , add it to `READ_BY_TESTS` or generally in `ci_scope.py` .

<a id="stan-odbioru"></a>
## Stan acceptance

The results of the individual tasks are in the local task registry (`docs/tasks/`, ignored by Git). The user postponed the manual tests. Build Windows on macOS confirms the compilation for Windows, not running on the physical Windows computer. Tests of full game and ergonomics belong to later tasks.

<a id="postgresql-i-testy-integracyjne"></a>
## PostgreSQL and integration tests

`bash scripts/test.sh` launches a private development cluster PostgreSQL 17 in `.artifacts/postgres`, on `127.0.0.1:55432`, and all .NET tests. It does not touch the Homebrew system cluster. `bash scripts/postgres.sh stop` stops this cluster. On Linux, set `WOTR_PG_BIN` to the PostgreSQL 17 binary directory or deliver `WOTR_TEST_DB` to the existing test database.

Server variables: `WOTR_DB` — connection PostgreSQL; `ASPNETCORE_ENVIRONMENT=Development` allows local HTTP. Besides Development required HTTPS with Kestrel certificate. Passwords, sessions and certificates do not belong to the repository.

`bash scripts/dotnet.sh format whitespace Wotr.slnx --no-restore` uses `.editorconfig`; [CODE_QUALITY](CODE_QUALITY.md) describes the limits of modules. Unity uses the same public data by pinned package: from DIST-02 `scripts/unity.sh` runs `scripts/bootstrap_deps.py`, and build from the editor checks the copies in Resources against `wotr-deps.lock.json` (section below).

<a id="zależności-serwera-w-kliencie--dist-02"></a>
## Dependency of the server in the client — DIST-02

Client no longer reads `src/Wotr.Contracts` or `content/`. `wotr-deps.lock.json` attaches two packages: `com.wotr.contracts` (UPM `.tgz`, solved by Unity with `.deps/`) and `com.wotr.public-content` (allowlist archive; Unity import only tracked copies in `Assets/Wotr/Resources` with their `.meta`). Lock contains a version, commit server, size and SHA-256 archive and SHA-256 of each public file.

```bash
# Server: packages made of clean commit (default .artifacts/deps-channel)
python3 scripts/package_deps.py --channel <channel folder>
# Client: download and verify (unity.sh does this before each start)
python3 scripts/bootstrap_deps.py
# Client: a public version change, then commit lock, Unity manifesto and Resources
python3 scripts/bootstrap_deps.py pin com.wotr.contracts@0.1.0 com.wotr.public-content@1.0.0
```

The channel from the lock is the public `github:jpolaczek/wotr-server` release channel
(`name-version`, with an archive and manifest). The current coherent set is Contracts
`0.10.0`, public content `1.0.11`, and the server and seeder built from
`a68ceee8bbc7`. A player or contributor does not need a GitHub token. In the client,
`python3 scripts/bootstrap_deps.py fetch --anonymous --test` explicitly ignores token
environment variables and the local `gh` login while downloading and verifying all
four archives. For offline work, set `WOTR_DEPS_SOURCE=file:<channel folder>`.
Private channels may use `WOTR_DEPS_TOKEN`, `GITHUB_TOKEN` or `gh auth token`; the
credential is never written to the lock, manifests or logs. A bad hash, unavailable
version or denied access stops the bootstrap without substituting another version.

The change of Contracts requires the `version` to be conquered in `src/Wotr.Contracts/package.json`, and the change of the file from the allowlist — `version` to the `content/unity-public-content.json`. Packer refuses to replace the published version with other bytes. Unchanged sources also give the identical archive in a later commit; the manifest retains the commit that first produced it.

<a id="lokalny-podgląd-po-t10"></a>
## Local preview of T10

Does not require manual testing to pass current tasks. For later start:

```sh
bash scripts/postgres.sh start
export WOTR_DB='Host=127.0.0.1;Port=55432;Database=wotr;Username=wotr'
ASPNETCORE_ENVIRONMENT=Development Wotr__Email__Provider=log Wotr__PublicUrl=http://127.0.0.1:5088 \
  bash scripts/dotnet.sh run --project src/Wotr.Server -- --urls http://127.0.0.1:5088
```

The account (AUTH-02) is assumed to require `POST /api/auth/register` with `username`, `email` and `password` (`infra/site/account/` pages require nginx of the game, e.g. in `infra/README.md`). The `log` provider prints in Development a confirmation link and link reset to the log. In Unity or build Development type `http://127.0.0.1:5088` and sign in with a name or address. Each table has a separate invitation to the second place. Confirm both accounts configuration, then Start game. The development database is only available on the loopback and uses local trust; it is not an implementation configuration.

`bash scripts/test-client.sh` automates two independent Unity sessions with a real Kestrel/PostgreSQL and all PlayMode. Selects `wotr_test`, creates one-time accounts and closes the Kestrel process after testing. You can specify `WOTR_TEST_DB` for your own Test Database. Self-hosted Job Unity uses this script and requires PostgreSQL 17 (or this DSN) except for .NET and Unity license. The call test uses the local HTTP/WS; the production TLS and the Windows runtime test remain to acceptance release.

<a id="map-17--profil-konturów-i-rozdzielenie-transportu"></a>
## MAP-17 — contour profile and transport separation

`tests/Wotr.Scenarios` saves `map17-profile.json` next to fixture `client-integration.json` during `scripts/test-client.sh`. These are safe server projections for Development-only honesty; do not copy to full private fixture profile with certifications. The new `map-profile-2` requires 36 workloads and 42 PNG, including dense field/garrison and Fate/White Hand changes.

```sh
python3 scripts/review-map-client.py --player ".artifacts/map17/profile/Wotr.app/Contents/MacOS/War of the Ring Online" --input .artifacts/map17-profile.json --output .artifacts/map17-player --timeout 600
python3 scripts/test_map_profile.py
```

Confirm the executable path in Info.plist of a specific build. Start with behaviour/przywr distorting player preferences (helper `clean_profile` In the scripts/smoke-macos.py ); final acceptance It's a wrapper. `.artifacts/map17-build-smoke.py profile` The values from the Development Player are distinct from smoke nonDevelopment and Editor tests. Check all images after automatic PASS . Measurement report without accounts/secret you can keep in docs/ui/profiles ; private exports remain in .artifacts.

`scripts/test-map-tls.py` is a separate test of two script accounts on the local HTTPS/WSS proxy for the Test Kestrel5088: checks the CA certificate and localhost, private hands, notifications/reconnect and the same receipt. It does not change the implemented stack and removes the temporary leaf certificate key. It requires a real test host and fixture; do not run parallel with the batch using the same accounts or login limit. Units `scripts/test-client.sh` still uses HTTP/WS. Final command and results: [MAP17_ACCEPTANCE](https://github.com/jpolaczek/wotr-client/blob/1232d72e5b3f71c9f8d1e688a4f268e0000a20f2/docs/ui/MAP17_ACCEPTANCE.md).

<a id="zgodność-klienta-i-lokalnego-serwera--ui-83"></a>
## Client and local server compatibility — UI-83

New build Unity does not rebuild the image of Docker. Appearance: visible Discard 2, double click without recoil, despite the current client. The old server returns Minimum=2; from the commit cfdebdb allows Minimum=1 and after the first rejection opens the next decision. The UI-83 application confirmed in the image with 2026-10-07 in the morning, older than this change. The `docker compose restart` itself restarts the old image.

Update according to [backend instructions](../infra/README.md): backup along with the image, attempt on the restored base, new WOTR_VERSION, `build server`, then `up -d --no-deps --wait server`. Keep the database, secrets and volumes. Do not use `down -v`. Control the actual image of the container running, the health state and the behavior of the saved batches.

After changing the backend, go back to Games and reopen the game or start a new client. The revision filter of the existing session can ignore the changed options with the identical state review. The draft itself is not a game command; do not remove it or the journal to unlock the hand.

Ready to build after UI-83: `open .artifacts/ui83/macos/Wotr.app`. Connect with `https://localhost:8443` and open the existing batch. Local backend uses the `ui83-7df29a1` tag; Windows build is in `.artifacts/ui83/windows/`. The repository contains a client amendment, so the next regular build master will also include it.

<a id="diagnostyka-natywnej-krawędzi--ui-84"></a>
## Native Edge Diagnosis — UI-84

MapEdgeProbe is only available in the Editor or Development build. It runs the real BoardScreen from a single safe PlayerView exported from the test script. It starts before InterfaceController and ClientFlow: does not create GameApi, does not log in, read session, or send commands. Do not give it `client-integration.json`, statements or write a real batch. Use a built Player to observe boundaries; Game View itself in the editor does not prove the behavior of the macOS window.

The diagnostic build must have its own companyName, productName and Bundle ID. UI-84 prepared it in an isolated checkout; the temporary identity concerned only the artifact and was restored in front of normal builds. Thus, the size/mode windows remembered by Unity do not touch the client preferences of the user. The use of another output directory does not isolate PlayerPrefs or persistentDataPath. Do not run the test under the same identity next to the active batch.

Example for the existing, separate UI-84 artifact, from the repo directory:

```sh
".artifacts/ui84/probe-green/Wotr.app/Contents/MacOS/WoTR UI84 Probe" \
  -logFile "$PWD/.artifacts/ui84/probe-window.log" \
  --wotr-edge-probe "$PWD/.artifacts/ui84/probe-view.json" "$PWD/.artifacts/ui84/probe-window" \
  --wotr-edge-mode window --wotr-edge-width 1366 --wotr-edge-height 768 \
  --wotr-edge-scale 1.25 --wotr-edge-seconds 180
```

Confirm the name of the program in Info.plist of a particular build. `--wotr-edge-mode fullscreen` mode means FullScreenWindow. To test the physical edges, specify the native resolution of the screen being tested; in acceptance UI-84, it was `--wotr-edge-width 3024 --wotr-edge-height 1890`. Extortion of 1366×768 on this screen gave a belt outside the viewport: touching the edge of the monitor did not mean touching the rendered map. `status.json` gives the actual ScreenWidth/ScreenHeight, FullScreenMode and PixelsPerPoint; do not assume that the coordinates of CoreGraphics at the points are the rendering pixels.

Probe does not control the cursor or the game. Supports native input without DesktopProfileInput The first ready condition has `State="ready"` and `AcknowledgedReset=0` . Before the next edge move the cursor to the middle of the window, save it atomicly `request.json` ed `{"Reset":1}` and wait for `AcknowledgedReset=1` and `State="ready"` In the `status.json` . Each next reset has a larger number; it centers and zooms in only the camera. Then move the cursor to the test limit and leave it still for about a second. `{"Quit":true}` ends the sample; the time limit is by default 180 s, argument allows 10–300 p.

`trace.jsonl` saves samples of approximately 20 Hz and Move/Enter/Leave, button events, MouseLeaveWindow and focus. Contains raw Input.mousePosition, inverting Y, ScreenToPanel, boundaries maps/root, capture, Active and Pan. The buffer is emptied every second; when you read a working attempt, skip the finished last row. Limit is 25 000 records, and `Dropped` reveals the excess. A new run should have a separate directory of the report.

The existing `scripts/profile-client.py` and `scripts/review-map-client.py` are profiles of synthetic presentation: they force Windowed, and their guard blocks the physical input. They do not replace the above test. The chapter between the Routing Tests UI, native observations and limitations of platforms is described by TESTING (UI-84 evidence in Git history).


Ready Client with UI-84 Edge Correction:

```sh
open .artifacts/ui84/macos/Wotr.app
```

Build Windows: `.artifacts/ui84/windows/Wotr.exe`. The next build with master also contains an amendment. The server remains `https://localhost:8443`; the repair is for camera input and does not require a backend update.

<a id="wersje-klienta-i-wydania--dist-05"></a>
## Client and release versions — DIST-05

Contract and Decisions: [CONTRACTS](CONTRACTS.md#wersje-klienta-katalog-wydań-i-zgodność--dist-05), [ARCHITECTURE](ARCHITECTURE.md#wydania-klienta-i-zgodność-wersji--dist-05).

- The server with DIST-05 rejects the client in Production without the headers of the version. After
  rebuilding the local Docker image, the existing build of the game (before DIST-05) will get 409
  `ClientUpdateRequired`. Build the client with `wotr-client` or set the
  `WOTR_ACCEPT_UNVERSIONED_CLIENTS=true` transition time in `infra/.env` (see
  [infra/README](../infra/README.md#katalog-wydań-klienta--dist-05)).
- Development and Testing accept clients without headers by default; when headers are, they check
  them as well as Production. `Wotr__Clients__AcceptUnversioned=false` also turns on the strict mode
  locally.
- The release signature key was generated by 2026-10-13 outside the repositories:
  `~/.config/wotr/release-signing/wotr-release-ed25519.key` (0600) and `.pub`, key id
  `95ffc109e1bb9b15`. The client attaches the public key in
  `wotr-client:unity/Wotr.Client/Assets/Wotr/Runtime/Networking/ReleasePublisher.cs`. The private
  key copy is stored offline; the loss requires a new key, a client release with a new key and
  manual installation on the players.
- `dotnet run --project src/Wotr.ReleaseTool -- <polecenie>` launches the tool locally. Commands and
  refusals describe infra/README.

<a id="instalatory-klienta--dist-06"></a>
## Instalatory client — DIST-06

Player packages are Velopack installers instead of ZIP. Description of build, packaging (`scripts/package_velopack.py`), test smokes and workflow Installers: `wotr-client:release/README.md`; acceptance on equipment: `wotr-client:docs/release/INSTALLER-ACCEPTANCE.md`. Release build accepts commit argument `-wotrCommit`; `WOTR_BUILD_COMMIT` still works locally.

<a id="scenariusze-testowe--test-01"></a>
## Test scenarios — TEST-01

The server in Development has the test mode enabled by default, e.g. local `ASPNETCORE_ENVIRONMENT=Development … --urls http://127.0.0.1:5088`. It is disabled by `Wotr__Testing__Scenarios=false`. The directory is in `src/Wotr.Domain/Testing`; the new script is an entry in `TestScenarioCatalog` with state builder. The `EveryScenarioBuildsTheSameStateAndStartsWithItsSide` test checks repeatability and the starting page. The client support is added by TEST-03.

Manual test without client:

```sh
curl -s -H "Authorization: Bearer $TOKEN" http://127.0.0.1:5088/api/test-scenarios
curl -s -X POST -H "Authorization: Bearer $TOKEN" http://127.0.0.1:5088/api/test-scenarios/setup/start
```
