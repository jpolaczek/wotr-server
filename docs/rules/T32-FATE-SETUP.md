<a id="t32-setup--plansza-i-rozmieszczenie-fate"></a>
# T32-SETUP — board and Fate deployment

Source: `FATE-WOTR018-RULES`, internal page of the independent instruction WOTR018; comparison with `FATE-RULES-45`, CE p. 45. Both images read on implementation; control register and sum: [SRC-07](../sources/SRC-07-AUDIT.md).

<a id="dane-i-granice-modułu"></a>
## Module data and limits

`content/fate-setup.json` is a Veditioned Diff `fate-setup-1` with respect to `base-setup-1`. It changes the metadata of three regions, two more at New Cities, six final pairs of region/nar and Dwarf activity. It does not change the graph, names, boundaries, physical numbers of figures or the policy track. `null` value means the region without the nation and the original owner.

| Region | Nation / original owner | Settlement | PZ |
| --- | --- | --- | --- |
| Erebor | Sauron / Shadow | Stronghold | 2 |
| Dale | None/None | Fortification | 0 |
| Iron Hills | Dwarves / FP | City | 1 |
| Ered Luin, only New Cities | Dwarves / FP | City | 1 |
| South Rhûn, only New Cities | Southrons / Shadow | City | 1 |

Final deployment: Erebor has 2 Regular Sauron and no Dwarfs; Dale has no North; Carrock has 2 Regular and 1 Leader North; Ered Luin has 2 Regular Dwarfs; Iron Hills has 2 Regular and 1 Leader Dwarfs. In these pairs there are no Elite or other Leaders. First all figure of these couples return to reserve, then the stable ID normal figures is selected. The remaining deployment remains identical. Removed figure FP are not losses. Dwarves are active but do not move politics.

<a id="odczyt-mapy"></a>
## Map Reading

`BoardRules.Regions` contains only the base graph, ID and the names. Rules reading the nation, settlement or the affiliation use `Region(state, id)`/`CurrentRegions`. Fixed Fate cartridges are indexed once. Kings White Hand covers the configured board without mutation of shared definition. The copy for traffic planning retains configuration.

Scoring and initial control use **board setup**, before the dynamic White Hand. Example: Erebor launches as Sauron and gives Shadow   0   PZ; acquisition by FP ♪ give ♪ 2   PZ White Hand's still not picking up. Shadow points for winning Helm's Deep (KINGS,  card  Théodena and  acceptance   T28 Dale's not. settlement It's recruitment./point Iron Hills and New Cities give 6 + Attacker in the first round battle The field.

The projection returns the complete current `RegionOverrides` set, with the Neighbours lists disconnected. Unity uses it for its own metadata and removes old overwrites at the next view; it does not recognize the Fate flag or region names. We do not send an additional secret state. Return to the database removes the overwrites.

Audit includes movement/recruitment/politics, shelter and hit thresholds, retreat and preview battle, declaration/Hunt, character input and card choosing regions by nation. Pits, Madness and Rangers may not use frozen lists for the database when registering programs.

<a id="weryfikacja"></a>
## Verification

Tests check full Diff figures and metadata, graph and reserve behaviour, deterministic and read-through, four combinations Fate/New Cities (last illegal), both sides of projection, traffic with preview, recruitment, combat, travel Fellowship and the zdobycia/odbicia. score The Kings regression retains White Hand; Unity test checks the application and removal of overwriting without conditions dependent on expansions. Actual results save the task history.

Six substitutes cards receiving T32-CARDS Both versions of Gimli T32-GIMLI , full setup with Kings/Lords T33. Previous `KingsSetup` already skips Dáin/Brand at Fate. We are not opening yet gate take-off expansions.

<a id="odbiór-2026-10-06"></a>
## acceptance 2026-10-06

- `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
  --logger 'trx;LogFileName=t32-setup-full.trx' --results-directory .artifacts/test-results`:
  **3071/3071 PASS**, 0 omitted, including 39 new Fate cases and restart HTTP/PostgreSQL. Output
  file `.artifacts/test-results/t32-setup-full.trx`; test time 2 min 3 p.
- `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults
  .artifacts/t32-setup-editor.xml -logFile .artifacts/t32-setup-editor.log`: **156/156 PASS**, 0
  omitted; three new overlay cases.
- `bash scripts/test-client.sh -testFilter T22BoardNetworkTests` : **5/5 PASS**, 0 omitted; actual
  Kestrel/PostgreSQL and two Unity clients, regression movement/recruitment and preview. A copy of the
  results `.artifacts/t32-setup-play.xml` , 07:46:18–07:46:25 UTC These are the core network
  scenarios, no. acceptance full UI expansions .
- `python3 scripts/source_fate.py --registered` : PASS , six pinned sources; checkers
  `card_coverage.py` , `lords_card_coverage.py` , `kings_card_coverage.py --check --results
  .artifacts/test-results/t32-setup-full.trx` : PASS ( 990/248/313 the cases attributed).
- `python3 scripts/tasks.py validate`, `git diff --check`: PASS.

The first build test improved the call of `Count()`. The first focused regression of 61/62 revealed an invalid base fixture Rangers with neutral control Dale; the script was fixed, the final full regression counts both configurations. Structure overview: data in `content`, charging in `Content`, setup/nak Reloads in `Fate`, common reading in `BoardRules`, public DTO in `Views`; Unity does not contain any Fate rule. No hand tests or Windows were performed. The next ready task by priority: **T32-CARDS**.
