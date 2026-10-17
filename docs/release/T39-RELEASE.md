<a id="t39--prywatny-release-candidate"></a>
# T39 — private release candidate

<a id="zakres-odbioru"></a>
## Scope of receipt

`0.1.0-rc1` version: Windows x64 and macOS Universal, without Development Build. Each package contains the version number, commit sources, Unity/minutes version, rules/contents and fingerprint public Resources. A common manifest links client archives with the exact image of the backend proven by T38 procedure. Secrets, test accounts, database copies and audit materials do not go into the packages.

acceptance requires full EditMode and PlayMode with real backend, content manifests control/expansions, builds, archive integrity and launch of the unpacked player macOS. The start-up attempt does not replace full automatic batches or a login test by the production certificate.

The user commissioned Windows build with no Windows test saved and server configuration without host. Windows runtime, Windows game ↔ macOS, target DNS/certificate and manual acceptance the user remains deferred. We do not publish packages. Distribution signatures and notation are not available in this local package; the instruction describes the limitation of the startup.

acceptance available range completed: **3499/3499 .NET, 198/198 EditMode and 270/270 PlayMode PASS**, all over the page. Both builds, package integrity, launch of unpacked macOS and re-restore of the backend have passed. [Results and totals of reports](t39-validation.json) attachs evidence to the release code; below are also the reasons for previous failures.

<a id="polecenia-i-wyniki"></a>
## Commands and Results

Full backend after T38: **3494/3494 PASS**, zero skip; [restore TLS/WSS](../server/T38-DEPLOYMENT.md) received separately. `python3 scripts/expansion_acceptance.py --results .artifacts/test-results/t38-full-resumed.trx`: PASS, 1107 required methods, 2560 counted cases and full 93 matrix configuration.

`bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t39-editor.xml -logFile .artifacts/t39-editor.log` : **198/198 PASS**, zero skip, 16:24:18–16:24:24 UTC . After adding restore `bundleVersion` and control the development of flags in the build script final EditMode also **198/198 PASS**, zero skip, 16:46:58–16:47:05 UTC ( `t39-editor-final.xml` Build retains the release version in the application and returns to previous project settings after completion. `bash scripts/test-client.sh` : **269/270 PASS**, zero skip, 16:25:06–16:40:55 UTC ( `t39-play-first.xml` All scripts except full party expansions past. After the settlement of the Reveal tile in Mordor, the server approved the revision 92 but projection Shadow She threw `KeyNotFoundException` In `KingsChieftains.Regions` Repair limits the regional entrance points of Uglúka to Fellowship Off track; still retains entry to the Isengard army. Sources and example: [K03/B19 intersection](../rules/T29-RULER-CHIEFTAINS.md) .

New Test HTTP/PostgreSQL Recreated the exception before the amendment. In the same attempt, four new fixture cases had a reserve preparation error; the fixture was corrected while retaining figure Instead of removing them from the state. 58/62 PASS , including a new reboot; four projections used a sidekick without `CharacterOptions` The tests now indicate the right provider. The production server records it from the beginning. Regression `KingsChieftainEntryTests|KingsCharacterManifestTests|MordorTests` : **62/62 PASS**, zero skip ( `t39-mordor-pass.trx` Full regression after repair: **3499/3499 PASS**, zero skip ( `t39-full.trx` ), 16:46:54–16:51:02 UTC Used `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t39-full.trx' --results-directory .artifacts/test-results` Checker expansions on this TRX : **1109 methods / 2565 cases PASS**, including matrix 93 configuration.

Final `bash scripts/test-client.sh`: **270/270   PASS**, no skip, 16:58:39–17:15:24   UTC  (`t39-play-final.xml`  /  `.log` Production backend, HTTP/WebSocket/PostgreSQL, without substitution RNG Full parties from the normal lobby checked both accounts, reconnect every three turn, in accordance with the projections and the refusal of further action results:

| Configuration | UI | Decisions / turn | Result |
| --- | --- | --- | --- |
| Base | 100% | 172 / 13 | FP, destruction of the Ring |
| Base | 125%, Reduced motion | 96 / 7 | SH, Corruption |
| Lords + Kings + Fate, Council, Dismay, New Cities | 125%, Reduced motion | 217 / 15 | SH, Corruption |

The tracks are in `.artifacts/t39-games/`; configurations and SHA-256 keeps the validation report. The other variants have the 93 array configuration; we do not declare 93 full batches or run these attempts on Windows.

After the amendment, the backend was rebuilt: `docker compose --env-file infra/.env -f infra/compose.yaml build server`, then `up -d --wait` and `.artifacts/deployment-venv/bin/python scripts/test-deployment.py`. [New acceptance](t39-deployment.json): **PASS**, TLS/WSS, nine tables, two restoratives, continuation battle 5 → 8, 98 preserved and six new samples RNG. The archive comes from `.artifacts/deployment-eb88c5e3/backup`. The older T38 report retains the score for the previous image.

`python3 scripts/release.py build` prepared both builds without Development from the commit `20bbd2425bef39dc6a256f67e98c72c2d46be72b` . `file` confirmed Mach-O x86_ 64 + arm64 and PE32 + x86- 64 Logs: `t39-release-macos.log` , `t39-release-windows.log` , both of them `WOTR_BUILD_SUCCESS` Packing by `python3 scripts/release.py package --server-backup .artifacts/deployment-eb88c5e3/backup --server-report docs/release/t39-deployment.json` , `release.py verify` and `shasum -a 256 -c SHA256SUMS` past. (From DIST-06 `release.py` only build; packages ZIP replaced Velopack installers with `scripts/package_velopack.py` .) `docker image load --input .artifacts/releases/0.1.0-rc1/wotr-server-0.1.0-rc1-linux-arm64.tar.gz` read the archive; ID The image is consistent with the restorative report.

The first two attempts of dragons mistakenly required window width ≥ 1000 points. CoreGraphics returned correct 683 × 412 points for Retina window with content 1366 × 768 pixels. Adjusted test to a visible window of positive dimensions; test `t39-smoke-368b12fd` **PASS**: Unpacked Player, clean data, visible window, no error in the log and restored earlier profile. Systemic capture of the window returned failure; no drop was obtained and no visual count was counted acceptance this Player or online login.

The final `python3 scripts/smoke-macos.py` for the package with `20bbd24`: **PASS** (`t39-smoke-fcad512f`, 16:57:59 UTC). In this attempt, [screenshot](t39-macos-login.png) was also saved, which the performer watched: normal login screen, clear fields and buttons, no cuts. [Report](t39-macos.json) retains the start/evaluation separation from the unexecuted player login to the target host and manual user test.

<a id="artefakty-lokalne"></a>
## Local artifacts

Catalog: `.artifacts/releases/0.1.0-rc1/`. Not published.

| File | Content | Size |
| --- | --- | --- |
| `Wotr-0.1.0-rc1-macos.zip` | Universal app, build-info and player manual | 43 831 052 B |
| `Wotr-0.1.0-rc1-windows.zip` | Windows x64, data/libraries set, build-info and instructions | 36 349 198 B |
| `wotr-server-0.1.0-rc1-linux-arm64.tar.gz` | Received image of a backend; without bases/sekrets | 90 952 598 B |

[Manifest Issues](0.1.0-rc1-manifest.json) attaches commits, versions, total archives, backend image, snapshot v2 and migration 001–003. The archives do not include an assemblies test, test login or development material. `SHA256SUMS` in the release directory also includes a manifesto. The instructions for players are inside both ZIP, the administrator uses [Compose configurations and procedures](../../infra/README.md).

Transmission: completed range T00–T39, UI, content packages and source audits according to the register. In stage E there is no next ready task. Separate work MAP-04 over the variant of the illustration is not part of the acceptance of this build. Subsequent attempts requiring the environment are Windows runtime/cross-platform, Intel macOS, target host and manual acceptance of the user.

The way Player starts is based on [arguments Unity 6.6](https://docs.unity3d.com/6000.6/Documentation/Manual/PlayerCommandLineArguments.html). The clean data sample temporarily postpones only the profile of this application and finally restores it. Locations: [PlayerPrefs](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/PlayerPrefs.html) and [persistentDataPath](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Application-persistentDataPath.html).
