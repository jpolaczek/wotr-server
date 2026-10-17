<a id="t27--odbiór-kart-lords"></a>
# T27 — acceptance cards Lords

Scope: 13 common cards Event/Combat and special We Shall Get It after finishing three packages 6 + 6 + 2 Sources: LORDS p. 2–3 (Composition), p. 22 (Sméagol), FAQ 1.2 p. 3 (reaction sequence) and [audit of obverses](../sources/SRC-03-AUDIT.md) Detailed interpretations, examples and evidence are included in the reports [FP](T27-LORDS-01.md) , [Shadow](T27-LORDS-02.md) and [last kart](T27-LORDS-03.md) .

<a id="kontrakt-odbioru"></a>
## Contract of receipt

The Manifesto must contain exactly all the 14 ID source and three completed, separate packages. Each rule sentence and each source case indicates existing behavior tests. The usual card have both applications, and the card special only automatic input and reaction from the table. We do not match its artificial Combat program or the cost of dice.

`LordsCards02BoundaryTests.LordsDeckReplacesBaseBalrogAndKeepsSpecialCardOutsideShuffledDecks` verify actual `MatchFactory` : decks FP 28/26 , SH 28/26 , 108 different cards , the replacement of the slot itself SH Character 17 and special card outside decks. Configuration is copied before shuffling. The base remains with 96 card and the original Balrog; the validators reject both applications cards Lords, when the module is off, the frozen base body also checks the continuity of behavior.

The `lords_card_coverage.py` control compares the sources, programs, assignments and the full test list. The report is generated; `--check` checks the currentity, `--results <TRX>` requires the actual results of all references. Full run without filter confirms the performance of all Theory; the very presence of the method in TRX does not guarantee this.

<a id="macierz-interakcji"></a>
## Interaction matrix

| Intersection | Checked behaviour | Tests |
| --- | --- | --- |
| Guide and Sméagol | Additional tiles do not duplicate the setup; the departure ends with WWGB, the special card is waiting for Gollum; the last Sméagol replaced by Gollum does not give card. | `LordsCards01EventTests`, `LordsWeWontGoBackTests`, `LordsWeShallGetItTests` |
| Exchanges of tiles and protection | Shadow selects before Nenya/Mithril; the new tile is protected. Refusal preserves card, the return of the old tile awaits the end of action. | `LordsWeShallGetItTests`, `LordsWeShallGetItInteractionTests` |
| Sméagol’s actual departure | SeparateCompanions introduces card; the later MoveFellowship gives Shadow a choice of use or refusal, visible to both clients. | `LordsFinalCardsNetworkTests`, `lords-special-replace` and `lords-special-decline` scenarios |
| Special dice and Lore | Both sides finish the special selection of dice before Lore; only the cast Eye Keeper launches the effect, the Lesser score retains the print Remove. | `LordsLoreTests` |
| Captain and nested battle | die the event itself does not go to Hunt; the later Character only after all the decisions have been completed. | `LordsCaptainTests` |
| Rings and hand | The identity of the retrieved Rings is a choice; Wind gives temporary hand access, card special on the table blocks this alternative. | `LordsRingRecoveryTests`, `LordsWindTests`, `LordsCards01InteractionTests` |
| Council and Combat Skills | Words cancels Leadership and the ability of the selected version; the participation of figure itself remains. | `LordsShadowCombatInteractionTests` |
| Swarm and Orderly Withdrawal | Canceled card does not create a retreat obligation; when the 0 draw the first defender works; ineffective card does not give a bonus for the absence of card. | `LordsCards01InteractionTests`, `LordsSwarmTests` |
| Fear / Servant and White Rider | Already cancelled Leadership does not pay the cost; natural sixes operate under their own clause; Servant retains a minimum of one dice. | `LordsShadowCombatTests`, `LordsShadowCombatInteractionTests` |
| Madness and Stronghold | Losses do not change the saved recruitment branch; neighborhood counted from the FP target; possible excess ends with the same paid action. | `LordsMadnessTests`, `LordsMadnessRecruitTests` |
| Record all packages | Restart before and after the decision, projections of both sides, preview without RNG and retry receive without re-kosztu/losowania. | `LordsCards01PersistenceTests`, `LordsCards02PersistenceTests`, `LordsCards03PersistenceTests` |

<a id="granice-i-struktura"></a>
## Borders and structure

T27 receiving card according to the to-do document. The full setup plan of the eight configurations and their gameplay sequences belong to T33; the new client decisions to UI-09 , acceptance to T34. Earlier mention EXPANSIONS o acceptance The whole T27 launch was wider than its criteria. gate The lobby remains until this integration is completed. The transfer does not mean that it is credited.

Special rules are in `Domain/Lords/Cards` and the execution uses common modules Effects/Battles/Hunt/Fellowship/Armies. Reporting works outside the gameplay path. Catalogs and static neighbourhoods are indexed once; no second engine is created in Unity. Documentation and CI control completeness after subsequent changes. Manual tests of the user remain postponed.

<a id="wynik-weryfikacji"></a>
## Result of the verification

2026-10-06, macOS 15.6.1, .NET 10.0.401, local PostgreSQL 17:

```sh
DOTNET_CLI_HOME="$PWD/.artifacts/dotnet-cli" DOTNET_CLI_TELEMETRY_OPTOUT=1 \
DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true bash scripts/test.sh \
  --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false \
  --logger 'trx;LogFileName=t27-aggregate.trx' --results-directory .artifacts/test-results
python3 scripts/lords_card_coverage.py --check --results .artifacts/test-results/t27-aggregate.trx
python3 scripts/card_coverage.py --check --results .artifacts/test-results/t27-aggregate.trx
python3 scripts/source_lords_cards.py
python3 scripts/source_cards.py
python3 scripts/check_catalog.py
python3 scripts/tasks.py validate
git diff --check
```

Full regression**2287/2287 PASS**, 0 omissions, 1 min 21 p. Log `.artifacts/t27-aggregate.log` the result `.artifacts/test-results/t27-aggregate.trx` Manifesto Lords: **14 cards , 148 methods, 248 counted cases**. Database Manifesto: **96 cards , 477 methods, 990 Accounted cases**. Source checks: 14 components Lords/108 cards decks/6+6+2 and 96 cards and 14 database form; directory: 30 families/34 windows; documents: 88 tasks — PASS .

Four experiments in memory with `unittest.mock.patch` function `read` coverage script confirmed rejection: missing card, duplicate, `in_progress` package and no test clause. No source manifest has been modified. CI now saves TRX and checks both manifests against the tests performed; remote workflow has not been run.

T27 does not change the production code or Unity. Proof of legal entry card special and both responses comes from four network tests of the 03 parcel on the unchanged code (`.artifacts/t27-03-play-labels.xml`, 4/4 PASS). In this aggregation no new Unity run, desktop builds or manual user tests were performed. The full range of normal start-up remains openly to T33/UI-09/T34. Next ready task according to ROADMAP: T28.
