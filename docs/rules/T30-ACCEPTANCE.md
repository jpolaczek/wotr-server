<a id="t30--odbiór-19-kart-kings"></a>
# T30 — acceptance 19 cards Kings

Sources: KINGS 1.0 s.3/19 (11 replaces and eight new ones), p.18 (except Fate), [awersy SRC-05](../sources/kings-card-index.json). Four completed packages: [01](T30-KINGS-01.md), [02](T30-KINGS-02.md), [03](T30-KINGS-03.md), [04](T30-KINGS-04.md). Each card has Event/Combat; Seeing Stones promos remain out of range.

<a id="skład-i-wykonanie"></a>
## Composition and implementation

`CardCatalog.ConfiguredDeck` select active expansions and replacement before shuffling in `MatchFactory` Without Kings, it does not change the composition or the input order bazy/Lords. With Kings, four decks have a uc 26 cards , 104 including; with Lords have 30/28/30/28 , 116 - I'm not sure. card Sméagola is not shuffled. Setup does not choose cards ( BASE p. 14 ); only a phase does this 1 .

Fate deactivates `kings.fp.strategy.19` and `.22` exactly. Other Kings replacements are still in operation. Fate replacements and map itself belong to T32/T33; this aggregation test checks the Kings shutdown parameter, does not yet declare acceptance complete Fate configuration.

The performance manifesto stores the public `setup` for each ID and binds all source sentences to the tests. Checker compares 19 with different ID, four completed packages 6+6+6+1, 11+8, both programs, initiatives and metadata of the client. TRX must include actual reference runs; the table itself is not proof of performance. CI checks the set before the tests and the result after the tests.

The Kings draws in preparation grow from the base 92 to 100 (four decks after 26, after 25 swap). Fixtures use an open constant setup; the effects tests still separately check the RNG increment. The recording and selection do not shuffle the deck again.

KINGS p.18: Visible Corruption makes available to both sides the tiles already assigned to the sovereign. `KingsProjection` now uses the common `Visibility.CanReadCorruption` policy. So far, only the old player's counter projection has been included in the flag. Six cards tests confirm that the phrase "hidden" draws a simple tile also with open values and never the Kings' tile. Example: Dáin has a simple 1 and Kings 0 tiles; Ever at Your Service returns a simple tile in both settings. We do not reveal by this change the pool, the order of future tiles or private choices in the attempt to corrupt. Full integration of variants and sequential draws are still T31.

<a id="plan-odbioru"></a>
## Plan acceptance

Check real MatchFactory for eight expansions flags, uniqueness cards, both halves, exclusions and full recording/private deck projection. Regresion of all cards, sovereigns, dice and chiefs; cases of hidden tiles in a normal and open variant. Joint battle programs and table exceptions must maintain the same order regardless of the order of registration. Negative checker attempts reject none/duplicate, an unfinished package, no clause test and lost Fate exception. The actual results are given below.

The full variants of Lords+Kings are the T31 range, all T33 configurations, full UI expansions UI-09, acceptance T34. gate normal lobby until then remains public; T30 finishes acceptance cards and their decks, not the whole stage D.


<a id="macierz-integracji"></a>
## Integration Matrix

| Intersection | Result required from source | Tests |
| --- | --- | --- |
| Setup bases/Lords/Kings | 11+8, 104/116 cards, two swaps disabled from Fate; no change in order at Kings off | `KingsDeckTests`, `KingsCardManifestTests` |
| Ruler and effect directory | All 19 events pay the correct Ruler result; reversed registration does not change the state or RNG | `KingsCardIntegrationTests` |
| Normal/disclosed tiles | Six random returns exclude Kings, transparency does not give you a choice of tile | `KingsCardIntegrationTests`, `KingsCards01EventTests`, `KingsCards02EventTests`, `KingsServiceTests` |
| Inspection | The visdom may refuse, Dark requires a possible return; insight does not become a permanent disclosure | `KingsInspectionTests`, `KingsCards01PersistenceTests` |
| Table Restrictions | Folly and Welcome individually restrict awakening/removal; Threeats retains a separate alternative Ruler and expires according to printed causes | `KingsFollyThreatsTests`, `KingsCards02EventTests` |
| Two Jerksíry | Convert Muster/Event once to turn, the correct cost starts Orthanc, removing one card retains the other | `KingsPalantirTests`, `KingsCards03PersistenceTests` |
| Chiefs and commanders | Cost/Strength bathing before reveal, card remains played; Daring Defiance cancels the modifier | `KingsChieftainCombatTests`, `KingsCards03CombatTests`, `KingsEnemyCombatTests` |
| Event and loss | Separate army/garrison, loss of the right page, cause Event protects Companion | `KingsValinorTests`, `KingsEnemyTests`, `KingsEnemyPersistenceTests` |
| Four Packs at Unity | Two clients, decision ownership, reconnect, visible costs and targets, no scrolla/writing numbers | `KingsCards01NetworkTests`–`KingsCards04NetworkTests` |

Five negative `kings_card_coverage.verified_cards` attempts with the switched `read` in `unittest.mock.patch` were done in memory: no card, duplicate, `in_progress` package, no test clause and no Fate shutdown were rejected. No source files were modified during these attempts. CI received a background check, report and results TRX; remote workflow was not started. The directory and replacements are counted once when creating a batch, without additional cost in the client's frame or while reading the write.


<a id="wyniki"></a>
## Results

2026-10-06, macOS 15.6.1/M1 Pro, .NET 10.0.401, PostgreSQL 17.11:

```sh
bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t30-aggregate-full.trx' --results-directory .artifacts/test-results
python3 scripts/kings_card_coverage.py --check --results .artifacts/test-results/t30-aggregate-full.trx
python3 scripts/lords_card_coverage.py --check --results .artifacts/test-results/t30-aggregate-full.trx
python3 scripts/card_coverage.py --check --results .artifacts/test-results/t30-aggregate-full.trx
python3 scripts/source_kings_cards.py
python3 scripts/source_kings_characters.py
python3 scripts/source_lords_cards.py
python3 scripts/check_catalog.py
python3 scripts/tasks.py validate
git diff --check
```

Performed with local DOTNET_CLI_HOME And the telemetry is off. 2856/2856 PASS **, 0 a skip, 1 m 52 s; `.artifacts/t30-aggregate-full.log` Manifest Kings:** 19 cards , 131 sentences/accidents, 157 methods, 313 PASS Lords: 148 methods/248 PASS **; Base:** 477 methods/990 PASS **. Checks of sources, graph 88 tasks, 30 families/34 windows and diff — PASS .

After the setup integration, the first target run had 554/560 PASS: four fixtures required accounting for an additional eight shuffles, two saved deck before taking the played card. New samples of the open variant detected the described missing in SovereignView (585/591 PASS); after the amendment all new and existing cases went in full run. The CardCost error in the new test was corrected after the actual compilation error.

`bash scripts/test-client.sh -testFilter KingsCards` after changing the setup: **25/25   PASS**,  0  skip,  2026-10-06   06:51: 31Z  —  2026-10-06   06:52: 33Z. Result `.artifacts/t30-aggregate-play.xml` / `.log`: two actual Unity clients, Kestrel/PostgreSQL, reconnect, layout control of four packages. Drops and layout updated in scripts; new code does not change UI Unity has not been changed into T30; last EditMode of the pack 04 has 146/146   PASS Windows and user manual tests remain postponed.


The overview of the structure and data flow has been completed: one selection of active cards, common effect and confidentiality modules, no second engine on Unity side. No open source gaps. Next task according to ROADMAP: T31.
