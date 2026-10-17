<a id="t32-cards--sześć-wydarzeń-fate"></a>
# T32-CARDS — Six Fate Events

Source: full English obverses `FATE-CARD-SHEET-2AE0D186`, visually read 2026-10-06, `FE` numbers and boundaries in [index](../sources/fate-index.json). The standalone instruction WOTR018 confirms slots; CE p. 45–46 summarizes changes. Rules recruitment/Common s. BASE p. 22–23; combat effects and order: BASE p. 28–30. Integration principle: KINGS p. 18–19.

<a id="sloty-koszt-i-widoczność"></a>
## Slots, cost and visibility

Each card has ID `fate.*` and one overt `replaces=base.*`. Six replacements do not increase the deck. With Kings+Fate slots FP Strategy 19/22 belong to Fate, and corresponding card Kings and base ones are disabled. Other Kings/Lords replacements retain the range. Self-insistence of the old card in the test snapshot does not bypass the validation of `CardCatalog.Enabled`.

Each event is unconditional, pays the right icon or Event according to the usual rules of dice. Character only applies to There and Back Again, the other five uses Muster. Public becomes played obverses; future card selected from Strategy and the rest of the hand remain private. die remains Resolving by all nested decisions, selection/rejection and final army surplus.

<a id="wydarzenia-i-decyzje"></a>
## Events and decisions

| ID (prefix `fate.`) | Order |
| --- | --- |
| `fp.character.17` There and Back Again | One group separated from Fellowship, +1 to normal range. Then Gimli or Legolas' presence in Carrock/Iron Hills/Woodland Realm activates **only North** and moves Dwarves/Elves/North one step at a time. Present earlier Companion also meets the condition. Erebor and Dale do not meet it. |
| `fp.strategy.04` Book of Mazarbul | Any subset of Companion outside Fellowship moves normally, also empty. After the Companion movement in Iron Hills/Ered Luin moves Dwarves to At War. Then select one eligible region from Companion and one Regular/Elite Dwarves. |
| `fp.strategy.19` Bardings | One Regular/Elite North and one Leader North together in the Dale neighborhood or in the Dale area with the FP army. Then one card Strategy. |
| `fp.strategy.22` Dwarves of the Iron Hills | Like Bardings, but figure Dwarves. Also, he chooses one Strategy. |
| `sh.strategy.20` Orcs Multiplying Again | Three Regular Saurons in Dol Guldur and three in one selected Mount Gundabad/Erebor. Selection of the other region precedes the joint recruitment plan. |
| `sh.strategy.22` Monsters Roused | Choice of the Western variant: after Regular in Angmar/Ettenmoors/Weather Hills and Elite in Trollshaws; or Eastern: after Regular in Western Mirkwood/Northern Mirkwood/Withered Heath and Elite in one of these three regions. |

Full obverses There/Book do not require control FP For the political part itself; we do not transfer this additional restriction from existing base programs. Recruitment continues to respect control, the presence of the enemy and garrison. Example: Companion in the acquired Iron Hills allows Book to move politics, but without its own garrison it does not allow to recruit figure under the army Shadow.

The absence of Mobile/oddzielanego Companion does not remove the second part of the event. No units in the reserve does not reverse the policy or block the final selection. In Bardings/Dwarves, all available recruitment groups are selected in one plan with `MaximumRegions=1`; Leader or Companion itself do not constitute an army allowing recruiting in Dale. There is no need to enter the number.

Orcs does not recruit from one reserve consecutively: with four available Regulars the player can divide them 1+3, 2+2 or 3+1 between two legal locations. He cannot divide the other three between Mount Gundabad and Erebor. The blocked alternative region is not a legal target, but the absence of both does not remove recruitment in the Guldur Dol. The excess stack is checked throughout the chart.

Monsters preserves the choice of the variant program, then the Elite target (if available), then the joint recruitment plan. The variants do not mix figures. Each variant remains conscious of the choice also at the disadvantage; the database does not force the maximum effect between the two printed alternatives. The absence of a legal Elite target does not remove Regular recruitment.

## Combat

| Card | Initiative / Condition / Effect |
| --- | --- |
| There | 3, participating Companion; Shadow throws one die Combat less for each Companion FP, after a five-limit minimum of one. |
| Book | 3, the region of the defender inside the nation FP; Combat Shadow −1, without penalty to Leader re-roll, natural 6 hits. Dale/Erebor in Fate does not qualify. |
| Bardings | 6; at least two regular Combat+Leader re-rolls of the opponent → canceling one, before losses. |
| Dwarves | 3, participating Elite FP; Combat FP +1, without a bonus for Leader re-roll. |
| Orcs | 7, after normal losses; voluntary 0–4 hits on own units, so many separate dice 4+, without metastas/modifiers of the regular Combat. |
| Monsters | 3; both sides +1 to Combat and Leader re-roll, with natural 1/6. preservation |

These are the same read Combat programs as the corresponding printed effects of the database. Tests use the real ID Fate, investigate conditions, cancel and return after losses. The results end with the round, none of the six cards introduces a new permanent card table or private reaction window.

<a id="zapis-i-odbiór"></a>
## Writing and acceptance

All new choices use existing commands `ChooseEffect` , `MoveCompanions` , `SeparateCompanions` The record includes a program, cursor, selected region, costs and approved trail; it does not save delegates. The manifest binds every sentence of the index to the program and executed testem/TRX. acceptance includes eight deck configurations, restart/retry all actual windows and privacy selection, and Unity uses the controls to date mapy/figur. Gimli remains within the scope T32-GIMLI .

[Coverage report](T32-CARD-COVERAGE.md) and [manifest](../../content/fate-card-implementation-manifest.json) are checked by `fate_card_coverage.py --check --results <TRX>`; the reflex test compares them with the actual C# directory and active test methods. CI performs this deterministic check without reproduction. The local source audit `source_fate.py --registered` additionally checks pinned SHA-256 images in the ignored `.artifacts/source-audit/fate` directory.

Six `FateCardsNetworkTests` leads the actual Unity clients by separation/Guide , movement/region/recruitment , both plans at Dale, alternative to Orcs and variant/target/plan Monsters. Reconconnect retains paid decisions; private selection remains on the owner's side. Each tray is checked in 1366×768 and 1920×1080 , on a scale 1 and 1,25 , without ScrollView/TextField. Monsters discharge review ( 1366×768 , 1,25 ) and Orcs ( 1920×1080 , 1 ) confirmed the visible election and approval. The map continues to use interchangeable replacement graphics; further common acceptance UX belongs to UI-09/UI-12.
