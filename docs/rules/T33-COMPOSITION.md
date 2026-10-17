<a id="t33--skład-i-macierz-integracji"></a>
# T33 — composition and integration matrix

<a id="plan-przed-implementacją"></a>
## Plan before implementation

The input is agreed, public configuration. First we decide the default Dismay and the dependencies of variants; the incorrect input does not create a batch and does not download RNG. Domain diagnostics indicate the option, rule and source page, without access to the party status. The error code API remains `InvalidConfiguration`. Public start expansions still requires acceptance UI-09/T34.

| Modules | Legal open configurations | Character / Strategy to page | Characters and board |
| --- | ---: | --- | --- |
| Base | 1 | 24 / 24 | Setup base, seven Companion and Gollum by normal rules. |
| Lords | 2 | 28 / 26 | Private Gandalf; Council optional, named Rings, five special dice. |
| Kings | 4 | 26 / 26 | Five sovereigns, two Ruler, two Kings tiles; Visible and Sequential independent. |
| Fate | 2 | 24 / 24 | Diff six pairs of figures and three regions; New Cities adds two cities, a regular Gimli FE. |
| Lords + Kings | 24 | 30 / 28 | Both modules, the open order dice, Dismay and Renovating Ruler Tokens. |
| Lords + Fate | 4 | 28 / 26 | The council selects Gimli FE versions, the homeland of Iron Hills/Ered Luin. |
| Kings + Fate | 8 | 26 / 26 | Three sovereigns: without Brand and Dáin, their events slots replace Fate. |
| Lords + Kings + Fate | 48 | 30 / 28 | All above exceptions applied together. |

93 open configurations are derived from 512 combination of nine flags. We individually check Dismay no value: it is only active with Lords+Kings by default. The configuration version after starting is not the UI preference.

<a id="kolejność-i-tożsamości"></a>
## Order and identity

1. Validating the configuration and manifest of four decks before shuffling. Each of the 96 base
   slots has exactly one version; 12 expansions Lords and eight Kings have separate ID. Special We
   Shall Get It is off the deck. Slot conflict stops the setup with the source locator, never
   resolves it by the order of loading.
2. `MatchFactory` copies the base figures/politics, sets the control with `SetupRegions` and applies
   the final numbers of Fate. Reserves retain the physical ID. Score starts from zero, also in
   Erebore.
3. The factory prepares the base tiles and physical normal dice (4/7 active), shuffles four final
   decks. It does not yet select cards.
4. `TurnFlow.Start` adds Kings only once: three/pi The sovereigns are created as Leader, two
   initially unavailable Ruler and two tiles in the pool. Tokens are created only according to the
   passages suwerena/wariantu.
5. Lords prepares five initially unavailable special dice, three named Rings and four Sméagol tiles
   (two in the pool, two put aside for the event). Opens a private selection of the version before
   selection. With the Council seven groups, without the Council exclusively Gandalf; Fate changes
   two Gimli obverses, retaining one ID figure.
6. After the versions starts with Recover/Draw: after one card of both decks, then the first home of
   the Council, possible selection of the Shadow token, the Fellowship phase, the Hunt allocation
   and the throws. Each decision is individually approved and saved. Reading never performs the
   setup again.

Sources: BASE-2 . 4.1 p. 14–19 ; LORDS p. 3,7–15,30–31 ; KINGS p. 6–7,17–19 ; WOTR018 and FE obverses from [SRC-07](../sources/SRC-07-AUDIT.md) Full programs and sources cards : [Lords](T27-ACCEPTANCE.md) , [Kings](T30-ACCEPTANCE.md) , [Fate](T32-ACCEPTANCE.md) Response order and costs: [T31](T31-INTEGRATION.md) .

<a id="weryfikacja-i-prywatność"></a>
## Verification and privacy

The matrix also starts the actual factory and setup decisions for all legal flags, checking the full composition, resources and both views after reading. Each configuration has a reboot of HTTP/PostgreSQL and a retra approved decision. The negative matrix checks the remaining flags before RNG. `expansion_acceptance.py` compares the conversion metadata with source indices, Unity copies, a set of event/post manifestos and the actual TRX. It requires 93 performed separately for the setup and privacy.

Targeted sequences include V12–V16: Lords election before Ruler, eye origin, Captain, Sméagol during Corruption, Council and the cost of the chief entry, tiles/Dismay variants, lack of Dáin/Brand and kart/region Fate rules. `ExpansionInteractionTests` and `ExpansionDiceIntegrationTests` check these intersections after the actual set of the full set. Comparison of states differing only with secrets must preserve JSON opponent; visibility of assigned tiles still depends on Visible. The frozen base body does not change the expected hashs.

<a id="checkpoint-implementacji"></a>
## Checkpoint Implementation

`DeckComposition` controls the set and uniqueness of slots before RNG; `MatchFactory` separates the copying of the setup, players and shuffling. In the Lords data, the `replaces` Balroga has been supplemented according to SRC-03; previously the same substitution had only the condition in C#. The order of existing decks does not change. `ConfigurationRules.Conflict` returns the public rule locator and coherently supports the default Dismay also without previous Normalize.

2026-10-06 : `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~Expansion' --logger 'trx;LogFileName=t33-matrix.trx' --results-directory .artifacts/test-results` : **192/192 PASS**, no omissions, 1 m 6 s. Includes 93 full setup, 93 privacy comparison and **186 reboot HTTP/PostgreSQL**: first decision of preparation and first expense of each configuration. Each reboot checks both views, preview without RNG/mutations , rejection of a foreign page and idmpotent retras after another server launch. Prior to that, a focused setup/thai regression: 118/118 PASS Checking flags 512 open entrances ( 93 Legal) and 256 Dismay=null ( 69 The first build detected a collision of the name. `Source` with the basic exception property; corrected for `SourceReference` before the tests.

Targeted regression: the same `scripts/test.sh` filter with `FullyQualifiedName~ExpansionInteraction|FullyQualifiedName~ExpansionDiceIntegration|FullyQualifiedName~ExpansionPersistenceTests.FateCouncil|FullyQualifiedName~ExpansionPersistenceTests.Full` and TRX `t33-interactions-pass.trx`: **13/13 PASS**, zero omissions. The first 8/10 found the error of additional click of the region in the Book test; the only region was already automatically settled, remained the figure selection. The test was corrected without changing the rule or strategic selection.

| Intersection of the full set | Proof of conduct |
| --- | --- |
| Council → Gimli FE → Book → token and Chief Kings | Gimli starts in Iron Hills without the icon of the nation, allows At War and the chosen elite by Book. Token and Muster introduce Serpent; Ruler only appears in Recovery. Dáin/Brand do not return. New Cities on/off. |
| Lords Eye + Captain + corruption + Sméagol | Nenya pays the test, Captain does not increase the limit, Sméagol forces the next selection without entering the character. Privacy before assigning also at Visible; plain/sequential selection, correct return of the tiles and the cost pool of FP. |
| Lords → Kings → Dismay at Fate | Four special dice and two Rulers; the SH/FP election, the SH/FP discards, only the Token Théodena. Both token variants and the New Cities; no Branda/Dáin absent. |

Required subsequent states characters/cards/pool in the last two scenarios are clearly prepared after the normal setup. They test sequences of interactions, they do not pretend to be the whole game played. **26 further reboots HTTP/PostgreSQL** includes movement/recruit Book, double cost of the chief, all the elections dice Dismay and selection/attribution in four variants Corruption Total T33 tests are performed 212 restart scenarios, each with a play before and after the commission.

<a id="odbiór-końcowy--2026-10-06"></a>
## acceptance final — 2026-10-06

- `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
  --logger 'trx;LogFileName=t33-full.trx' --results-directory .artifacts/test-results`: **3434/3434
  PASS**, no omissions, 09:06:51–09:10:19 UTC (3 m 26 s). Includes 205 new T33, V12–V16 cases,
  character manifests and unchanged base body.
- `python3 scripts/expansion_acceptance.py --results .artifacts/test-results/t33-full.trx`: **1107
  methods / 2560 cases PASS**, both complete sets after 93. Separate checkers of
  bazy/Lords/Kings/Fate: manifestos PASS.
- `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t33-editor.xml
  -logFile .artifacts/t33-editor.log`: **158/158 PASS**, no omissions, 09:06:55–09:06:57 UTC. Also
  checks the readout of synchronized public metadata Lords.
- Three controlled metadata mutations (no Lords substitution, wrong Fate page, no card Kings) and
  removed case of the TRX matrix are rejected. Trials were done in memory/ interim file, no change
  in content or original TRX. `py_compile`, `tasks.py validate`, `check_catalog.py` and `git diff
  --check`: PASS.

Structure preserves boundaries: configuration policy in `Configuration` , the composition of public data in `Content` , setup in factory and modules, durability in existing server. Guard performs linear transitions at most 116 card once when creating a batch; does not work during refresh UI read the writing. Does not change the client's contracts, order RNG nor the expected hash of the base corps.

No outstanding source conflict in the MVP range was found. No new PlayMode, Windows builds, manual user trials, or measurements of FPS in T33 were performed. The task takes the domain and server; full Unity interactions and start-up opening expansions remain in UI-09/T34, migrations of released versions in T37. Next ready to do: UI-09.
