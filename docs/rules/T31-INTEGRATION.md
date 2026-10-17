<a id="t31--lordskings-i-warianty"></a>
# T31 — Lords/Kings and variants

<a id="źródła-i-kolejność"></a>
## Sources and order

The basis is the local instruction KINGS p. 6, 10–13, 17–18 and LORDS p. 7–11. The text Lore is derived from the obverse of `lords.sh.character.26`, indicated in [index Lords](../sources/lords-card-index.json). The following table separates the throw from the decisions ending the phase; it does not depend on the order of registration of the dealers.

| Step | Rule and decision |
| --- | --- |
| Roll | All authorized dice, including Ruler and Lords; the number of actually dumped dice is saved. LORDS p. 7. |
| Keeper/Lesser | Selected one special die, mandatory Eye if you have. More dice thrown first; FP draw. Selected special Eye goes to Hunt. LORDS p. 7–8. |
| Kings SH | At the end of Action Roll SH rejects standard non-Eye or Ruler, also Eye. Other regular/Ruler Eye goes to Hunt. KINGS p. 6 pt. 3a, p. 17. |
| Kings FP | FP rejects standard or Ruler; Keeper/Lesser is never the cost. KINGS p. 6 pt. 3b, p. 17. |
| After the throw, before action | Dismay allows SH to move the saved die Ruler of any page. die Rejected/usuni is taken outside the available jackpot. Eye's score goes to Hunt, another returns to available. We do not repeat the discard of Kings. KINGS p. 6 pt. 4 and p. 17. |
| Lore and Dismay | Both are voluntary actions of SH before the first action. When both are available, SH selects which to use first; we do not force the priority of one module. Lore is once per throw, each Dismay consumes a separate physical token. |
| Shares | Only after the above windows have been completed, the first action receives FP. |

The placing of the Lords elections before the Kings discard results from the final moment of the Kings discard; the Lords' choice itself must end before the action. The interpretation of this border and the common Lore/Dismay window verify the scenarios, also after the reboot. We do not introduce the Warriors or their dice. The search on the publisher page of the 2026-10-06 did not provide a separate FAQ Kings amending these rules; the basis remains the pinned instruction, not fan summary.

<a id="pochodzenie-kości-i-council"></a>
## Origin of dice and Council

KINGS p. 17: normal and Ruler Eye Shadow and selected Keeper/Lesser Eye count to Corruption. Captain of Despair is not a cost or a limit component. We put the special Eye released to the pool used dice FP, retaining the actual owner, source, selection and symbol Remove to the next Recovery. The place of postponement does not transfer the property of Lesser Minion Free Peoples.

KINGS p. 18: Dark Chieftain input can release any available token Council together with the normally authorized dice. They bypass only the prerequisite leader; region, one-time entry and cost dice is still valid. Both resources are one approval. The limit of one Council token on turn remains (LORDS p. 31). The lack of an icon of the nation on the alternative Boromir/Gimli/Hobbits does not give the right to remove Corruption or corrupt sovereign.

<a id="warianty"></a>
## Options

- Visible Corruption: only tiles already assigned to the sovereign are public. The tiles selected
  remain private. ‘Hidden’ in effect means every tile other than Kings; random removal is still
  random (KINGS p. 18).
- Sequential: After the first regular Kavl SH can stop with it or select it further. Never selects
  an earlier tile. The last allowed tile ends the attempt automatically. Eye/Special/Sméagol forces
  the exchange and does not give the right to return to the previous result. Kings immediately ends
  the attempt with 0. Unchosen ordinary tiles are revealed at the usual end, not earlier; Kings
  retains its own exception p. 12 (p. 11–12, 17–18).
- Dismay: Ruler token is used to transfer the same token SH. Awakened death only passes the yet
  unreleased token. Token cannot return again. The spread consumes the token and draws on the
  server; SH can keep it for the future.
- Renovating Ruler Tokens: Awakening does not create token FP It is only the death of Awakened that
  creates Dismay; death Initial/usuni The Corrupted hook does not do this. The validation of the
  configuration requires Lords + Kings + Dismay as in REQUIREMENTS.

Example Sequential: SH selects 3, continues and selects 1. It can only preserve 1; the 3 tile returns to the pool after disclosure. Example of special cost: Gothmog Eye pays the test and goes to the used FP, but with Recovery it still falls under the Gothmog and selected Remove conditions, rather than the Keeper conditions.

<a id="plan-sprawdzenia-i-granice-odbioru"></a>
## acceptance verification plan and limits

The tests will include all legal flags sets, V12–V14, two projections, previews without RNG, the wrong actor/cost, restarting any new decision and an idmpotent retra by HTTP/PostgreSQL. Separately: both orders Lore/Dismay, Eye in both directions, all detention sites Sequential, Sméagol, Kings, Captain, token Council + die and characters without icons. Results will be added after start. The full interface of expansions and unlocking the lobby still belong to UI-09/T33/T34; T31 does not mean them acceptance.

Checkpoint implementation: targeted regression of existing Kings/Lore/Council **609/609 PASS** (`.artifacts/test-results/t31-initial.trx`). New cases, 84 legal configurations Kings and four HTTP/PostgreSQL tests covering multiple subsequent reboots: **167/167 PASS** (`.artifacts/test-results/t31-rules-third.trx`). The first compilation of new tests required improving import and two xUnit analyzer rules; the first execution of 75/77 detected the lack of mapping of Dismay's presentation of decisions, fixed before repetition. Full regression and Unity are still underway.

<a id="powiązanie-zachowań-z-testami"></a>
## Linking behavior to tests

| Maintenance | Tests |
| --- | --- |
| Full throw, selected special Eye, discards and SH window | `KingsLordsIntegrationTests.ActualRollUsesLordsOrderThenKingsShadowThenFreePeoplesThenDismay`, also a draw and Hunt allocation. |
| Special Cost and Remove location at Recover | `SpecialEyesPayCorruptionAndRemainOwnedButArePlacedWithFreePeoplesUsedDice` — All Five dice Lords. |
| Ruler Eye and the Captain's ban | `RulerEyeCountsButCaptainDiceNeverPayOrIncreaseDrawLimit`. |
| The actual cost of the Council and the lack of an icon | `KingsCouncilIntegrationTests` — three chiefs × two tokens, Ruler, wrong costs/regions and four alternative Companion. |
| Dismay and both Lore's orders | `KingsDismayTests` — 12 Walls of two Ruler, Hunt ↔ Available, no target, no refusal, another token, wear and death. |
| Sequence and privacy | `KingsSequentialTests` — stop on 1/2/3, ban on previous score, Eye/Special/Sméagol, Kings on any site, Visible on/off. |
| All flag allowed | `KingsVariantMatrixTests` — 84 configurations with Kings, save/projections and cost variants; the other and illegal flags include `LobbyTests`. This is not acceptance not yet implemented maps/cards Fate. |
| Corruption from event | `SequentialWestUsesUnspentEyesAndResumesItsEventWithoutAnAdditionalCost`, restarting the last numerical selection. |
| Durability and idempotence | Five `KingsIntegrationPersistenceTests` tests: actual API/PG, three hosts per border, two projections, another's actor, preview without RNG and retras without a re-kosztu/losowania. |
| Text of the variant and undoing | `KingsVariantTests`: six cards changes the description of hidden to non-Kings without the directory mutation, the draft retains both token types. |

Unity EditMode: **153/153 PASS**, 0 skip, 07:16:40–07:16:42 UTC; command `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t31-editor-pass.xml -logFile .artifacts/t31-editor-pass.log`. First build detected no public configuration in `PlayerView` — it was added a detached projection and test of the change of DTO without mutation of the condition. The second detected two incorrect names of API in the new test; it was corrected to call the existing draft.

First full regression: **3028/3029**, 0 skip, 1 min 59 s, `.artifacts/test-results/t31-full.trx`. One error: `BaseCorpusTests` counted the new enum Dismay to the base. It was added to the open list of decisions expansions. No frozen entries or their SHA were changed. A full regression and a check-up of two clients were underway.

Re-full regression**3029/3029 PASS**, 0 a skip, 07:20:30–07:22:31 UTC : `DOTNET_CLI_HOME="$PWD/.artifacts/dotnet-cli" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=t31-full-final.trx' --results-directory .artifacts/test-results` . TRX : `.artifacts/test-results/t31-full-final.trx` , log: `.artifacts/t31-full-final.log` New T31 classes: 35 + 17 + 86 + 21 + 9 + 5 = **173 cases PASS** Manifests bases/Lords/Kings checked with this TRX : 477/148/157 methods, respectively 990/248/313 cases PASS . `source_kings_cards.py` , `source_lords_cards.py` , `check_catalog.py` , `tasks.py validate` and `git diff --check` : PASS The structure review retained the division domain/transport/presentation ; registration lists server/fixture is clearly distributed after one position in the row without changing the order.

After the first PlayMode **25/25 PASS** ( 07:23:01–07:24:01 UTC , `.artifacts/t31-play-first.xml/log` ) review detected a discrepancy of the omitted Dismay flag: view used default Lords + Kings, runtime required `true` . `ConfigurationRules.DismayEnabled` is now a common interpretation of null also in the read record. Three new tests check null/true/false , public configuration, the Metastatic window and the granting of token after death. Re-full regression: **3032/3032 PASS**, 0 a skip, 1 min 58 s, `.artifacts/test-results/t31-accepted.trx` and `.artifacts/t31-accepted.log` ; it's the same as above with a new name TRX All three checkers cover cards again PASS with this file. Total T31 adds 176 cases.

Final PlayMode: **25/25 PASS**, 0 skip, 07:28:40–07:29:40 UTC, `bash scripts/test-client.sh -testFilter KingsCards`; artifacts `.artifacts/t31-play-final.xml/log`. Two clients used the real Kestrel/PostgreSQL, with the restart and control of existing cards panels. This is not the acceptance of new Dismay/Council controls nor the complete UI expansions controls.

Transmission: changes are in `Domain/Kings`, common costs/configuration, `Contracts/Runtime`, projection/DI server and texts/Draft Unity. The responsibilities and the check paths are indicated above and in CODE_QUALITY. No open source gap T31. Manual user tests, standalone Windows build and full setup/client expansions remain in their tasks; gate lobby is still in force. Next ready task: **T32-SETUP**.
