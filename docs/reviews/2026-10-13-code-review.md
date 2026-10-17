<a id="niezależny-audyt-techniczny-wotr"></a>
# Independent technical audit WoTR

**Audition completed 14 October 2026, Europe/Madrid. Versions of the code were clipped at the beginning of the audit.**

Order: evaluation as a portfolio project and future open source project for 100–300 active parallel users of 1v1. Audit launched 13 October 2026 UTC, continued 14 October Europe/Madrid. without repair of production code, changes in contracts and publications.

<a id="1-werdykt"></a>
## 1. Verdict

**Value as current portfolio project: 6,5/10.** The greatest value lies in the server model of rules, decision durability and protection of hidden information. This is a foundation worth further development. The biggest shortcomings before opening to the community are the control of resource abuse, completeness of validation, healthy acceptance client and the possibility of independent project launch.

Backend is good architecturally. The client has a reasonable separation of transport and recovery, but his central coordination UI is average in terms of maintenance cost. The efficiency of measured paths is good. Preparation for open source is poor.

| Zone | Evaluation | Certainty and justification |
|---|---:|---|
| Server Architecture | **7,5/10** | High: separated domain, authoritarianity, transactions, projections and public sequels |
| Server code quality | **7/10** | High: readable modules and tests; common payoad and unclosed input lowers the rating |
| Architecture and client quality | **6/10** | High: good boundaries of transport/journalu, large coordinators and manual relations between windows |
| Compliance with the rules | **7/10** | Moderate: strong source tracking and extensive testing, confirmed error pass; no proof of all cards intersections |
| Reliability of tests and acceptance | **6/10** | High: strong backend; client requires data isolation and repair of seven repetitive failures |
| Server performance for the assumed scale | **7,5/10** | High for measured workload; limited for long, complex batches and internet host |
| Client performance | **7/10** | Moderate: Full board/motion meet budgets, legacy/in-game have incomplete drivers |
| Open source readiness | **3/10** | High: anonymously unavailable package, need for public quickstart and CI policy |

Scale: 1–3 requires basic work, 4–6 useful with significant debt, 7–8 strong foundation with specific limitations, 9–10 mature and widely verified solution. The overall assessment is an engineering judgement in the context of the portfolio, not an arithmetic average or a financial valuation.

<a id="2-co-dokładnie-oceniono"></a>
## 2. What exactly was assessed

| Element | Version |
|---|---|
| Server | `63e9e205a79d8c1f3413845a6decee0f3ea2b320` |
| Client | `08eb95678a3275edfadcaffe2034e52808d19d9d` |
| Backend and pinned Seeder by Client | `eac4afe342fa82f136dce34c0e36fe8ab59bdfeb` |
| Contracts / public content | `0.3.0` / `1.0.2`, SHA-256 from lock |
| Environment | Apple M1 Pro, 8 logical CPU , 16 GiB RAM , . NET SDK 10.0.401 Unity 6000.6 .0f1, PostgreSQL 17.11 |

A separate worktree and database on `127.0.0.1:55472` was used. Unsaved changes to the recruitment panel in the main client checkout and changes to task registry remain outside the evaluated version. The difference between the HEAD base and the client package is applied to seven Domain files, mainly the availability of recruitment and script directory; contracts and content have not changed in this comparison.

During the audit, the main checkouts moved to the `75d51b2f6892503131580e3004de1e438758308e` server and the `0d88608c79987fdd571f80340fab5ff7cc00fb8c` client. The comparison confirmed no changes in the `src`, `content` and the server seeder relative to the evaluated commit. The client received a further recruitment implementation at that time (44 files runtime/resource, about 1430 added and 197 deleted lines); these later changes do not include test results or client evaluation. Links to the code use the historical view of GitHub `blame` to ensure that the subsequent pining of repositories did not transfer quotes to another code.

The review included the limits of modules and command paths, recording, projection, randomness, reaction, transport, local recovery, UI, source of rules, packaging and CI. The risk analysis was supplemented by full tests, validation of manifests, independent reproductions and measurements. This is not a manual comparison declaration of each possible course of all cards.

Server production code: 438 C# files, 26 150 lines; testy/scenariusze: 458 files, 51 900 Line. Client Running: 342 files, 35 723 lines; client tests: 362 files, 50 444 lines. The numbers serve the description of the scope, are not the basis for quality assessment.

<a id="3-najważniejsze-ustalenia"></a>
## 3. Key findings

Priorities: **P1** — required before the service/repositories are opened to the community; **P2** — important correctness, reliability or cost of living; **P3** — improvement. Confirmed behaviour and risk of construction are identified separately.

<a id="r01--p1-potwierdzone-odrzucone-komendy-pozwalają-nieograniczenie-zapisywać-dane"></a>
### R01 — P1, confirmed: rejected command allows unlimited data recording

**Code:** [MatchStore.cs:65](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Server/Persistence/MatchStore.cs#L65), [MatchEndpoints.cs](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Server/Endpoints/MatchEndpoints.cs), [AuthenticationRateLimit.cs](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Server/Authentication/AuthenticationRateLimit.cs).

`Execute` blocks and reads the full state of the party, and then saves the receiver also for the rejected command. The limiter includes the login/Registration routes; the command path does not have the corresponding budget or the limit of tho-regime.

**Reproduction:** 150 new `commandId` with incorrect `decisionId`, in one own batch, gave 150 responses HTTP 200 / `DecisionExpired` and 150 permanent record receipt in 0,47 p. There has been no 429. Not Stronghold that HTTP 200 for domain rejection is a mistake; the problem is unlimited cost of handling and writing.

The logged user can charge the database, disk and block his own batch. The shared resources are shared on the common server. The priority are the limits per account/game and the limits of tho-regime, along with the policy of storing rejections. You must maintain the lasting and dempotence of the accepted decisions. It is also worth reviewing the budgets of table creation and opening of sockets. [Traffic Limiting Documentation ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0).

<a id="r02--p2-potwierdzone-poprawny-json-z-null-prowadzi-do-http-500"></a>
### R02 — P2, confirmed: correct JSON with `null` leads to HTTP 500

**Code:** [CommandShape.cs:7](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Domain/CommandShape.cs#L7), [HistoryCapture.cs:39](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Domain/History/HistoryCapture.cs#L39), [ActionDecisionFacts.cs:53](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Domain/History/ActionDecisionFacts.cs#L53).

Validation checks the presence and size of the collection, but does not close the contract of text fields. Initialization of properties on `""` does not protect against the overt JSON `null`. Later history code uses `id.Length` or `Dictionary.ContainsKey`.

**900 trial on 25 saved base states gave 65 exceptions. These are many reproductions of several common causes, not 65 various errors. By the actual Kestrel command `ContinueFellowship` `rulerTokenId: null`, `councilTokenId: null` or `regionId: null` returned HTTP 500. The status review remained unchanged — the transaction worked correctly.

The entire entry structure, including nested elements, needs to be centrally validated before the domain is executed. The rejection should be controlled. Tests should be extended to the public `null` in fields not used by the type of command. Long-wave common extensive `CommandPayload` increases the cost of manual validation.

<a id="r03--p2-potwierdzone-użycie-pierścienia-blokuje-legalny-pas"></a>
### R03 — P2, confirmed: Use of the Ring blocks the legal pass

**Code:** [TurnCommands.cs:26](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Domain/Turns/TurnCommands.cs#L26), [RingRules.cs:37](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Domain/Turns/RingRules.cs#L37), [TurnOptions.cs:71](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Domain/Turns/TurnOptions.cs#L71).

[Instructions BASE 2.4.1, printed p. 21](../sources/materials/base-rulebook-v2.4.1.pdf#page=21), allows omission or pass after using the Ring under normal conditions: ‘He can also skip or pass the action, if egible to do so.’ Meanwhile, `CanPass` additionally requires `!RingActionPending`.

**Reproduction:** FP has two unused dice, SH three. Before changing the result the pass ring is legal. The ring is used correctly, the numbers dice remain 2/3 and pass receives `IllegalChoice`. The projection also disables it. This is a rule error and the decision available to the player.

Repair must cover legality, options and complete the wait for action after the Ring at pass. Both sides and save/restart between the use of the Ring and pass are required. The mere removal of the condition without checking the further life of `RingActionPending` would be incomplete.

<a id="r05--p1-dla-open-source-potwierdzone-anonimowy-bootstrap-zależności-jest-niedostępny"></a>
### R05 — P1 for open source, confirmed: anonymous bootstrap of dependency is unavailable

**Client code:** [wotr-deps.lock.json:3](https://github.com/jpolaczek/wotr-client/blame/08eb95678a3275edfadcaffe2034e52808d19d9d/wotr-deps.lock.json#L3), [scripts/bootstrap_deps.py](https://github.com/jpolaczek/wotr-client/blame/08eb95678a3275edfadcaffe2034e52808d19d9d/scripts/bootstrap_deps.py#L317), [.github/workflows/ci.yml:12](https://github.com/jpolaczek/wotr-client/blame/08eb95678a3275edfadcaffe2034e52808d19d9d/.github/workflows/ci.yml#L12) on the rated client committee.

Lock indicates the `github:jpolaczek/wotr-server` channel. Anonymous reading of the required Contracts release `0.3.0` by GitHub API returned 404. Local audit works thanks to the existing verified cache. The documentation itself describes repositories and channel as private.

Hashing, public versions and refusal to switch packages are good solutions. For the public project you should make the required packages available and/or complete, proven path of building a canal from public sources. acceptance : Clean environment without the author's tokens can run the client and integrate. It is not enough to pass on the machine with `.deps`.

<a id="r06--p2p1-warunkowo-ryzyko-ci-może-pominąć-unity-a-publiczne-pr-wymagają-izolacji-runnera"></a>
### R06 — P2/P1 conditionally, risk: CI may omit Unity and public PR require Runner isolation

**Client code:** [.github/workflows/ci.yml:10–23](https://github.com/jpolaczek/wotr-client/blame/08eb95678a3275edfadcaffe2034e52808d19d9d/.github/workflows/ci.yml#L10).

A simple job performs document and release control. Unity only works when `WOTR_UNITY_CI=enabled` is set, on the runner `self-hosted`. Without this switch, the green workflow does not prove the game's compilation or operation. I did not check remote varibles and branch protection settings; not Stronghold that the job is now always omitted. After turning on the job performs a full `test-client.sh`, that is, the path that on the rated sources in the audience is not green.

In addition, workflow responds to `pull_request`. Before making the repository public, you need to establish secure handling of untrusted PR code on Unity Runner. GitHub indicates a significant risk of a permanent self-hosted environment. You need to isolate the environment and a public policy to run PR, and the required statuses should actually include the game. [GitHub: hardening self-hosted runners](https://docs.github.com/en/actions/reference/security/secure-use#hardening-for-self-hosted-runners).

<a id="r07--p2-potwierdzony-koszt-konstrukcji-pełna-historia-jest-częścią-roboczego-snapshotu"></a>
### R07 — P2, confirmed cost of construction: full history is part of a work snapshot

**Code:** [GameEngine.cs:39](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Domain/GameEngine.cs#L39), [StateCodec.cs](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Domain/State/StateCodec.cs), [MatchState.cs](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Domain/State/MatchState.cs), [MatchStore.cs:117](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Server/Persistence/MatchStore.cs#L117).

Each accepted decision of serializuje/deserializuje a copy of the entire state, then saves the entire JSONB. The condition includes the growing `Events` and `RandomSamples`, despite the existence of a separate event index. Heartbeat private version also reaches JSONB state.

Microbunchmark of an identical simple decision, without cost of base: a new batch of about 0,87 median and 0,23 Mi B allocation; 10 thousand synthetic events about 10,59 ms and 9,39 Mi B. This confirms an increase in cost. It does not prove failure of 300 players — a conducted test of this scale passed.

Direction: limited working status, history/draws in a separate permanent journal, open-ended cursor of new events, light version columns. It requires migration and recording tests during reakcji/bitwy. Priority after entry repairs and publication preparation; a complete change to the model to event source is not a condition for gaining value.

<a id="r08--p2-ryzyko-utrzymania-koordynacja-klienta-ma-duży-zakres-zmian"></a>
### R08 — P2, maintenance risk: client coordination has a large range of changes

**Client code:** [Runtime/Application/TurnInteraction.cs](https://github.com/jpolaczek/wotr-client/blame/08eb95678a3275edfadcaffe2034e52808d19d9d/unity/Wotr.Client/Assets/Wotr/Runtime/Application/TurnInteraction.cs#L612) — 916 line, especially `Render` and `RenderLayout`; [Runtime/UI/BoardScreen.cs](https://github.com/jpolaczek/wotr-client/blame/08eb95678a3275edfadcaffe2034e52808d19d9d/unity/Wotr.Client/Assets/Wotr/Runtime/UI/BoardScreen.cs#L195) — 573 line, including numerous pairs of `Opening`/`CloseTransient` subscriptions; [Runtime/UI/DecisionTray.cs](https://github.com/jpolaczek/wotr-client/blame/08eb95678a3275edfadcaffe2034e52808d19d9d/unity/Wotr.Client/Assets/Wotr/Runtime/UI/DecisionTray.cs#L104) — 539 line subscriptions.

There are separate context controllers, but the central code still knows many types of decisions, panel hosts, selection conditions and transitions. Adding the panel requires adding a relationship with other panels. This is a real coupling that does not remove a large number of small files.

It is worth to separate declarative window coordination and limit the responsibility of the main decision controller. Keep the working sender, the journal, the reversion of views and the production renderer. This does not justify rewriting the client from scratch.

<a id="r09--p2-ryzyko-diagnostyki-wyjątki-prezentacji-mogą-wyglądać-jak-utrata-sieci"></a>
### R09 — P2, diagnostic risk: exceptions to presentations may look like a loss of network

**Client code** [Runtime/Networking/MatchConnection.cs:37–41, 95–99](https://github.com/jpolaczek/wotr-client/blame/08eb95678a3275edfadcaffe2034e52808d19d9d/unity/Wotr.Client/Assets/Wotr/Runtime/Networking/MatchConnection.cs#L37) and [Runtime/Networking/ViewVersion.cs](https://github.com/jpolaczek/wotr-client/blame/08eb95678a3275edfadcaffe2034e52808d19d9d/unity/Wotr.Client/Assets/Wotr/Runtime/Networking/ViewVersion.cs#L19).

The transport loop also includes calling subscribers `Updated`, after which it captures the general `Exception` as a reconnect. The number of the accepted view is already moved before the subscriber is executed. Except for UI, the same view can be rejected as already accepted. There is no distinction between network error and application error and visible diagnostic trace at this location.

This is a risk resulting from the design, unconfirmed scenario of a normal batch. Separate acceptance/acceptance from the view application, provide diagnostics without secrets and check behavior after exception of the subscriber.

<a id="r10--p3-potwierdzone-ciężar-repozytorium-utrudnia-wejście-do-projektu"></a>
### R10 — P3, confirmed: the weight of the repository makes it difficult to enter the project

The client tree has about 691,7 Mi B, of which `graphicalassets` about 671,5 Mi B; the history of Git about 433,7 Mi B in the package. The server has about 74,1 Mi B in the tree. The runtime Unity with resources is a small part of these data.

This cost of cloning and maintaining the repository, not proof of the decline of FPS. It is worth separating active sources of graphics from the iteration archive and using appropriate storage of new large files. Documentation needs a short current entry point; extended workbooks should not be necessary for the first start.

<a id="r11--p2-potwierdzone-bramka-odbioru-klienta-pozostaje-niespójna"></a>
### R11 — P2, confirmed: gate acceptance Client remains inconsistent

Full PlayMode: 520/574 PASS. All 54 failures were repeated in packages of four; 47 over. The remaining seven were restarted individually on fresh data and server; all seven failed again. The aggregate gives **567 PASS / 7 FAIL / 0 missing**. This is the result of several preserved runs, not one pure full PASS.

| Repeated Failure | What has been shown |
|---|---|
| BattleReportNetworkTests.ClosedReportRejectsLateHttpAndFailedReadCanRetry | The test second time clicks the `last-battle` icon after reading. Runtime `BattleReport.RefreshOpen` (98 line) deliberately hides the read icon; the test must use the current path to reopen the report. |
| ContextHoverTests.ClickedInspectionIsInteractiveStableAndFitsDenseRegionsAtBothScales | A tight comparison of rectangles detects a change in y about 0,55 px. You have to settle stability/animation and measurement tolerance; no proof of ingratibility. |
| ExpansionWindowNetworkTests.EvilKnowledgeRetainsTheEliteAndFullBattleResourcesFit | Joint Assertion `FellowshipLayoutTests.Check:351`: button height about 31,8656 against threshold 32. |
| KingsCards02NetworkTests.HopeChoosesWhichInitialSovereignAwakensDuringBattle | The same height assertion; the message itself does not show an error in the waking rule. |
| ShadowStrategy01LayoutTests.ServerProjectedMapChoicesSacrificesAndSiegesFitBothSizesAndScales | Same height Assertion. Check the required area of click and rounding, instead of automatically weakening the threshold. |
| InGameProfileDriverTests.AllEightRealProfileDriversChangeAndRenderTheirExpectedStateInFourLayouts | Driver tries to run `board-menu-victory` with zero balls. Do not close the opening/sync track of the layout. |
| MapViewportTests.MovingArmyCannotStopNavigationUnderAStationaryEdgePointerAtFourLayouts | The script does not reach the condition: the army physically fails to hit the motionless indicator. The intended edge-pan branch remains unverified. |

I do not count these seven failures as seven mechanical errors. They are a specific problem of maintaining and reliability acceptance. The full run further exceeded the 37 login limit times, and some other failures disappeared after data isolation. Repair should ensure the independence of fixture, handling current gestures and a stable wait for Layout while maintaining production limits.

<a id="r12--p2-potwierdzone-w-środowisku-audytu-brak-jawnego-budżetu-połączeń-ujawnia-się-przy-skoku-ruchu"></a>
### R12 — P2, confirmed in the audit environment: no clear call budget is revealed at traffic jump

**[Program.cs:21](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Server/Program.cs#L21), [DatabaseConfiguration.cs](https://github.com/jpolaczek/wotr-server/blame/63e9e205a79d8c1f3413845a6decee0f3ea2b320/src/Wotr.Server/Hosting/DatabaseConfiguration.cs), connection string configuration of the operator.

The simultaneous refresh and the opening of the WebSockets 300 session gave 297 successes and three errors; logs confirmed PostgreSQL `53300: sorry, too many clients already`. The audit cluster had `max_connections=100` and also operated isolated test bases. The default maximum pool of Npgsql is 100, which leaves no reserve for other processes. [Npgsql: pool parameters](https://www.npgsql.org/doc/connection-string-parameters.html).

After the audit only reboot of Kestrel with `Maximum Pool Size=40`, on the same cluster and without changing the code, all 300 session passed. Refresh + WS + first revision message: p95 1271,7 ms, maximum 1272,4 ms.

This is proof of the need for a public budget of the call and controlled overload service. This is not proof that the domain itself will not handle 300 users. implementation should include all instances, health, migration, backup and administrator reserve. The value selection should be confirmed at the target host.

<a id="4-zasady-gry-i-wiarygodność-testów"></a>
## 4. Game rules and reliability tests

Reference point: local instruction BASE 2.4.1, FAQ 1.2, instructions Lords/Kings, pinned obverses and materials Fate and explicit integration principles. Data mapping of sources, programs and test results has been verified. Manifests include 135 definition cards from four directories; no definition should be identified with the number cards used in the deck at the same time.

| Zone | Tested liability / evidence |
|---|---|
| B01–B05: setup, turn, dice, costs, Rings | MatchFactory, TurnFlow, DiceRules, RingRules, TurnTests; independent discrepancy R03 |
| B06: card and Effects | EffectCatalog, EffectRuntime, typed steps, event/combat manifests and results TRX |
| B07–B10: politics, recruitment, movement, characters | PoliticalRules, RecruitmentRules, MovementRules, CharacterCosts; validation of map sources and limitations |
| B11–B14: battle, losses, retreat, siege | BattleRules/Roll/Losses, ArmyLossRules, continuations and record tests |
| B15–B19: Fellowship, Hunt, Mordor | TravelRules, HuntRoll/Pool/Damage, MordorRules; saved decisions and sequels |
| B20: Victory | RingVictory, MilitaryVictory, four results in the regression body |
| Lords / Council | dice selection, private versions, homelands, tokens, public windows; configuration matrix and privacy tests |
| Kings / variants | sovereigns, attempts to corrupt, Ruler, Dismay, variants; configuration and write states tests |
| Fate / integrations | setup, cards, New Cities, Gimli and configuration conflicts; manifests and acceptance |

Atut: Privacy tests compare strict projections of states differing by secret. Atut: PostgreSQL tests, conflicts of commands, loss of response and continuation are real value. Atut: cards programs are a typical code and expansions have clear substitution rules.

Limitation: Snapshot compatibility with previous snapshot checks regression, not the truthfulness of the rule. `BaseCorpusExport` calculates the expected result using the engine; it needs an independent source overview. R03 shows that thousands of tests counted can maintain a misinterpretation. Configurations and manifests do not provide proof of checking all possible cards intersections.

<a id="5-rzeczywiście-wykonane-sprawdzenia"></a>
## 5. Indeed, the checks have been done

| Verification | Result |
|---|---|
| Backend Release ed PostgreSQL | **3842/3842 PASS**, 0 skip; 7 min 40 s |
| Unity EditMode | **688/688 PASS**, 0 skip |
| Full Unity PlayMode, backend pinned | **520/574 PASS, 54 FAIL**, 0 skip; 38 min 41 s |
| Repeat 54 failures in packages | **47/54 PASS**, 7 FAIL; all seven again FAIL in single case isolation |
| PlayMode aggregate after repetitions | **567/574 has PASS**, 7 repetitive FAIL, 0 missing; result of several runs |
| Unity with HEAD backend | **6/6 PASS**, single: two clients and recovery, movement/recruitment, three types of traffic figures, scenarios |
| Python: server packages / release tool | **6/6 and 20/20 PASS** |
| Python: walidatory client profiles / maps | **19/19 and 6/6 PASS** |
| Links of client documentation | PASS, 42 files Markdown |
| Catalogue, sources cards, manifests and acceptance expansions with current TRX | **11 commands PASS** |
| Validation of documentation/registry in the audit branch | PASS, 258 Tasks |
| NuGet vulnerable + transitive | 7 projects, no reported vulnerable packages in the result received |
| Historical Secret Patterns | 7070 + 3638 text blobs, no matching to four patterns |

The secret scan included the GitHub/OpenAI token patterns, the AWS keys and the private key headers in the historical text files for 2 Mi B. The result is not a guarantee of the absence of any secret. The NuGet scan does not include the vulnerability of system image and the entire Unity toolchain.

<a id="6-wydajność"></a>
## 6. Productivity

<a id="serwer--wynik-lokalny"></a>
### Server — local score

The actual Kestrel, PostgreSQL and two WebSockety on the table. On each table one legal decision was made at 5 s and two projection readings. Procedure: 15 s warm-up, 60 s sampling to the stage. Phases were performed, Hunt allocation, dice omission and discards; it is not the benchmark of each card and extended battle. Both clients were simulated network sessions, not 300 Unity processes.

| Players / Tables | Command p50 / p95 / p99 ms | View p50 / p95 / p99 ms | Measured HTTP | Errors |
|---|---|---|---:|---:|
| 2 / 1 | 8,32 / 26,03 / 26,03 | 5,55 / 33,58 / 44,06 | 36 | 0 |
| 100 / 50 | 6,07 / 12,40 / 21,19 | 2,55 / 6,77 / 16,68 | 1800 | 0 |
| 300 / 150 | 6,93 / 19,75 / 82,72 | 2,73 / 9,36 / 36,44 | 5400 | 0 |

In 300 backend sessions reached a maximum of around 164 Mi B RSS and medium 0,37 core CPU Repeating the stage 300 the sampling session of the database also gave 5400 HTTP No errors: maximum observed 29 connections and 2 active queries, without observed expectations on lock and without deadlocks. Sample what 0,5 s does not exclude shorter expectations.

The machine was shared with work in Unity and other applications. This is a useful local measurement, not SLA for the target host. The first stage has too few commands for a stable evaluation of extreme percentiles. TLS, internet, background backup and load of multiple hours are not measured here.

<a id="wzrost-historii--mikrobenchmark"></a>
### Growth in History — Microbenchmark

25 measured after warm-up tests, the same simple decision, synthetic events with 160 text characters, without cost of the base.

| Events Added | JSON state | Execute median ms | Execute average allocation |
|---:|---:|---:|---:|
| 0 | 47 005 B | 0,87 | 0,23 Mi B |
| 500 | 162 505 B | 1,38 | 0,68 Mi B |
| 2000 | 509 005 B | 3,99 | 2,04 Mi B |
| 10 000 | 2 357 005 B | 10,59 | 9,39 Mi B |

Actual events may include richer structures. Cost increases are measured; the exact capacity of the host should not be calculated from this table.

<a id="klient--granice-dowodu"></a>
### Client — Limits of proof

The new Development Player macOS was created from a rated commit. Compliance with 1194 runtime files and resources was checked. A separate identity of the application protected the settings of the correct client.

The first three original profilers interrupted: the log on `hunt-report-previous`, in-game and board on unreachable resolution. These results were preserved. The next build was only added to the record of the results and resolution collected in the existing profiler exception path. The production UI, renderer and measuring loop remained unchanged. [Accurate instrumentation](2026-10-13-evidence/profiler-checkpoint.patch) and hashe are in evidence. The source was changed after the measurement tool was built.

| Set | Scope | Result | Lowest average FPS | Biggest dealer / render-end ms |
|---|---|---|---:|---:|
| board | 40/40, 1366×768 and 1920×1080, the 1 and 1,25 scale | *PASS full validator** | 113,08 | 7,96 / 16,66 |
| figure-motion | 32/32, both resolutions and scales, normal/limited movement | *PASS full validator** | 114,13 | 16,42 / 24,60 |
| legacy | 5/56 completed workloads, 1366×768 | **Incomplete**, interrupt on unrendered card | 115,22 in completed | 6,39 / 12,45 |
| in-game | 4/32, 1366×768 | **incomplete**, invisible `board-menu-victory` | 115,85 in completed | 5,48 / 21,68 |

Each workload: 120 warm-up frames and 360 samples. Full series passed the thresholds of at least 60 medium FPS and feedback to 100 ms. Traffic data comes from subsequent real HTTP/WS projections from the HEAD backend tests. The average allocation to the frame in full board was between about 357 and 5579 B, and in the motion between about 5957 and 9093 B. The GC counters were available.

The P99 frame time reached about 17,15 ms in board and 17,46 ms in motion; the FPS average is not a guarantee of no single cut. The measurement of handler/render-end does not cover the physical input device, display and round trip server. The lack of a complete legacy/in-game does not allow to pass the whole presentation. There is no basis for calling the renderer inefficient based on these results.

Fresh screenshots of synthetic screenplays were viewed. The frame is consistent, basic information readable, and open inspections take up a lot of maps at 1366×768 and the 1,25 scale. This requires manual acceptance ergonomics. Replacement graphics are compliant with the requirements and do not reduce the evaluation of architecture themselves. [Example of a drop](2026-10-13-evidence/characters-1366.png).

<a id="7-co-ma-wartość-i-co-poprawiać"></a>
## 7. What has value and what to improve

** Authoritative domain without Unity/bases/Networks, transactions and receipts, projection policy, public decisions and sequels, typical cards programs, indexed history, open pin and hashe packages, local client journal, cache/indexes renderer and integration tests.

**Refactor with stages:** validation of common payload, server resource control, coordination of windows/rodzajy decision, client diagnostics, state boundary and history archive, tests dependent on common fixture and profiling drivers.

**Evaluation of the cost:** there is no proof of the rewrite of both repositories from scratch. The biggest return will bring the entrance, public launch and reliable acceptance client. Another set of functions UI before these jobs will increase the cost of living.

Proposed order for small team, indicative effort of experienced engineer:

| Order | Result | Input |
|---:|---|---|
| 1 | Controlled rejection of entire payload + independent regression null | 1–2 days |
| 2 | accounts/game/socket budget and secure policy of the receipt | 2–4 days |
| 3 | Correct pass support after Ring with reboot | 0,5–1 day |
| 4 | License, material listing and anonymous bootstrap | 2–5 technical days; issues of allowances depend on missing data |
| 5 | Reliable Insulated PlayMode, Current Drivers and Safe Required CI Unity | 3–6 days |
| 6 | Reduce the scope of the Client Window Coordinator and Decision | 3–7 days, in stages |
| 7 | Limiting history in snapshots, migration and long batch measurements | 4–8 days |

The inputs are planning spaces, not a commitment or valuation of repositories.

<a id="8-dowody-i-powtarzalność"></a>
## 8. Evidence and repeatability

The raw results are included in the `.artifacts/audit` isolated checkouts. Private fixtures with tokens and test passwords will not be copied to the report. Next to the report, [cleared summaries and independent sample code](2026-10-13-evidence/README.md) is saved. [Collective test results](2026-10-13-evidence/test-summary.json) also retain SHA-256 raw reports.

Most important command executions: backend `scripts/dotnet.sh test --configuration Release` with separate `WOTR_TEST_DB` Unity `scripts/unity.sh -runTests -testPlatform EditMode` ; full `scripts/test-client.sh` ; sources and cover of `--results` indicating fresh `backend.trx` ; `dotnet list ... package --vulnerable --include-transitive` ; independent Probe in modes `rules` , `micro` , `seed` , `load` , `reconnect` .

No publication, push or repair of the production code was performed. No games were performed on Windows, Intel Mac or the target web host. Audit ended with overt test failures and measurement limits. This acceptance report, not a game readiness statement.
