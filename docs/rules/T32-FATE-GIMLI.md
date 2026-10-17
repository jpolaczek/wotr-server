<a id="t32-gimli--jedna-figura-dwie-wersje-fate"></a>
# T32-GIMLI — one figure, two Fate versions

Sources: Full of obverses `1/11 FE` and `LM 6/10 FE` of a sheet `FATE-CARD-SHEET-2AE0D186` , re-read visually 2026-10-06 ; [index](../sources/fate-index.json) , WOTR018 the paragraph of Lords, CE p. 45–46 , LORDS p. 30–31 and FAQ 1.2 p. 3 That's two. card characters, without the fictional application of Combat event.

<a id="tożsamość-i-setup"></a>
## Identity and setup

figure retains ID `base.character.gimli`. Fate selects its normal obverses `fate.character.gimli`; only Lords+Council allows private choice `fate.character.gimli-dwarven-lord`. In this configuration, options do not contain replaced obverses base/lords. Omission of optional selection Gimlie preserves the normal version of Fate. Choosing both old version or alternatives without Council is illegal. The choice does not create a second figure.

Normal card: level 2, Leadership 1, icon Dwarves. Alternative: level 1, Leadership 1, no icon, level 2 for traffic only. None adds dice action. Gimli name at the event recognizes both versions, but does not give the ability of the second card. Configuration without Fate retains the base/Lords.

The Council version is private to reveal as Guide, loss or separation. The first decision of the homeland opens independently of this secret. The alternative can be launched in Iron Hills or Ered Luin, also without New Cities. Erebor is not allowed. The ordinary version has no such privilege. Zero/one/multiple quits retains the Shadow token rule. The recording and retrys cover separately versions, homeland and selection of token.

<a id="zdolności-i-legalność"></a>
## Capacity and legality

- Captain of the West regular version: +1 to the Combat Army force FP, only participating in battle,
  before the limit of five. Cancellation of the ability runs in the current round; it is not an
  additional Leadership or a hit modifier.
- Dwarf of Renown: in Iron Hills/Ered Luin release any of its own available die action and move
  Dwarves one legal step. The Reverse FE does not include the requirement of `unconquered`; we do not
  move it from the old obverse. It does not activate the nation or exceed the limitations of an
  inactive nation at the entrance to At War. In the normal setup Fate Dwarnoludes are already
  active.
- Alternative Guide: in the appropriate Hunt window, also with 0 injuries, voluntarily separate
  Gimli and reduce injuries with 1, minimum 0. Not working on CorruptionOnly. The new Guide and
  common movement of all separated against this tile retain the sequence of Council/FAQ. Victim
  instead of ability absorbing only 1 injury and remove figure.
- Dwarven Champion alternatives: +1 to Combat force only when the Army Defender region lies in the
  Dwarves nation, regardless of the attacking party. It uses board Fate: Iron Hills/Ered Luin meet
  the condition, Erebor Saurona does not. Limit five, participation and cancellation as above.
- Messenger of Elrond alternatives: any own available die in Iron Hills/Ered Luin activates Dwarves
  and moves one step. No additional control requirement. With At War and the active nation it does
  not offer an empty action.

Example: an alternative in Iron Hills under the control of Shadow can perform Messenger. However, it does not activate the nation by its presence/movement and does not meet the icon requirement for Kings actions. The usual version has an icon; it does not restore the omitted Dáin sovereign in Kings+Fate configuration.

<a id="implementacja-i-odbiór"></a>
## Implementation and acceptance

The Fate rules specify the explicit version, homeland and policy changes, and the common Council/Hunt/Battle performs the confirmed capabilities. We do not change the identifier figure, transport or sources of RNG. Private options include ID figure and both authorised versions separately.

acceptance Plan: both card and all source sentences in the manifesto, version/statistics/icons tests, both homelands and costs, changed geography, participation/cancellation, Hunt and traffic level, restart/retry every real choice, the base/Lords/Fate. Unity regression uses existing clicked action form and reaction Hunt; the private set UI Council remains in the subsidiary UI-09, the full setup of configurations in T33/T34.

<a id="checkpoint-implementacji--2026-10-06"></a>
## Checkpoint implementation — 2026-10-06

The two obverses are in `content/fate-characters.json`, and `FateGimli` binds them to the constant figure. `CompanionVersion` now distinguishes `FigureId` and `BaseId`: with Fate default card there is no ID figure. Versions/homelands and the Guide response use existing commands and saved frames. Policy rules and Captain/Champion respect these differences and read board Fate.

Focused `scripts/test.sh` (FateGimli/Council/CharacterActionTests filter): **175/175 PASS**, 0 skip, `.artifacts/test-results/t32-gimli-behavior.trx`. Four new HTTP/PostgreSQL methods include 12 re-stamping decisions. Previously, 30/30 setup and ability after correction of the 73/73. fixtures The first attempt of 56/73 capability revealed the initialization of Lords/Kings after the Setup phase and the attempt to download the sixth figure at five Regulars of the Dwarts; the design of the scripts was improved without circumventing the production validation. Manifestivity, full regression and verification of the Units remain open.

<a id="odbiór-obu-awersów"></a>
## acceptance both obverses

[Manifest](../../content/fate-character-implementation-manifest.json) and [report](T32-GIMLI-COVERAGE.md) bind a set of sources from 23 methods / 51 cases; C# also checks actual statistics, capabilities and response recording. Full regression `t32-gimli-full.trx` : **3229/3229 PASS**, 0 skill. Unity `t32-gimli-editor.xml` : **158/158 PASS**; network `t32-gimli-play.xml` : **3/3 PASS**, 16 layouts and reconnect. commands/times and previous trials: [TESTING](../TESTING.md) .

Structure overview: `FateGimli` has 22 lines responsible for its own ID, swaps and reactions; common classes retain validation/projection/execution. Unity has not been added to the domain, new command format or version combination numbering. Folders/Groups are loaded once. `sync_content.py` retains .meta and only overwrites changed public resources.

Messenger trays and separations are readable in 1366×768/125%. `.artifacts/screenshots/fate-gimli-departure-1366-1.25.png` drop reveals a collision of South Dunland spirit signature with the Fords of Isen label; this particular case was passed to review the UI-12 map. The full private Council interface remains the task of UI-09. Next: the collective acceptance T32, then T33.
