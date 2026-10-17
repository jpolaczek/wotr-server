<a id="t32--odbiór-fate-of-erebor-i-new-cities"></a>
# T32 — acceptance Fate of Erebor and New Cities

Range: Base+Fate, New Cities enabled/disabled and set of both Gimli obverses. Full eight configurations expansions and variants receive T33; Private Council interface belongs to UI-09. Source: full WOTR018, CE p.45–46 and eight obverses FE, pinned at [SRC-07](../sources/SRC-07-AUDIT.md).

<a id="składniki-i-kryteria"></a>
## Ingredients and criteria

| Scope | Implementation and proof of conduct |
| --- | --- |
| Map and initial figure | [T32-SETUP](T32-FATE-SETUP.md), `fate-setup-1`, six final pairs region/nar. Tests compare all 105 regions and all physical figure with the base; change only indicated metadane/rozmieszczenia. |
| Scoring | Erebor is the original Stronghold Saurona: initially SH 0 PZ, the capture of FP 2 PZ. Dale is not a city. The score does not depend on the dynamic White Hand. |
| New Cities | Only Ered Luin and South Rhûn; they also act as cities for rekrutacji/obrony/kart. Without variant remain Town; without Fate configuration is rejected before RNG. |
| Six events | [Rules](T32-FATE-CARDS.md), [manifest](T32-CARD-COVERAGE.md), both applications and appropriate initiatives. Six swaps do not increase 96 cards base game. |
| Gimli | [Rules](T32-FATE-GIMLI.md), [manifest](T32-GIMLI-COVERAGE.md), two obverses of one figure, the right poziomy/ikony/zdolno. The council has two new homelands and a private choice. |
| Recording and projection | Deterministic factory/reload , identical projections after reading, disconnected caps and reboot HTTP/PG each decision class; the retra does not pay/slot again. |

`fate_acceptance.py` combines registered three components, source index, actual data diff and both test manifests. Checking TRX requires all assigned methods to be executed without omissions. Checker also works in CI without local source images. It does not change rules or shuffle records again.

<a id="wykonany-odbiór--2026-10-06"></a>
## acceptance — 2026-10-06

`bash scripts/test.sh --no-build --no-restore --filter 'FullyQualifiedName~Fate|FullyQualifiedName~BaseCorpusTests' --logger 'trx;LogFileName=t32-acceptance.trx' --results-directory .artifacts/test-results` : **300/300 PASS**, no omissions, 08:44:00–08:44:12 UTC . `python3 scripts/fate_acceptance.py --results .artifacts/test-results/t32-acceptance.trx` confirms **79 methods / 192 Completed cases**, accurate setup and set 6 + 2 cards . Separate manifestos confirm 39/102 for events and 23/51 For Gimli. `python3 scripts/source_fate.py --registered` : PASS , six hashs of sources, eight obverses, six end pairs of setups and eight compositions.

Five controlled mutations in memory were rejected: the removed pair figures, the incorrect number of regulars, the altered original owner, the lack of one New City and the lack of an active nation. Source files remained unchanged. `python3 -m py_compile scripts/fate_acceptance.py`, `python3 scripts/tasks.py validate` and `git diff --check`: PASS.

This acceptance does not change the gameplay code. The last full regression of this code is **3229/3229 .NET** and **158/158 Unity EditMode** with T32-GIMLI. Unity PlayMode runs from the packages are six cards scenarios and three Gimli scenarios; their separate results and limitations are recorded in [TESTING](../TESTING.md). Next range: T33, full integration matrix.

<a id="granice"></a>
## Boundaries

Original illustrations are not required for this acceptance. Unity uses public descriptions and a interchangeable map; nine Fate scenarios check capabilities and card by the actual server. This does not mean acceptance full UI expansions or Windows. The specific collision of the South Dunland / Fords of Isen map label at 1366×768/125% is saved in T32-GIMLI to UI-12. Manual user trials remain postponed.
