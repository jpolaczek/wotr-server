<a id="t28--suwereni-kings"></a>
# T28 — Sovereign Kings

Sources: KINGS 1.0 ( 2023.08.10 ), p. 3 , 7–13 ; [the obverses](../sources/kings-character-index.json) assigned to T28 and [audit SRC-06](../sources/SRC-06-AUDIT.md) . Before the implementation, the full text of the instructions was reread and all five pairs of obverses were viewed. T29 integrates the pool Ruler/tokens and chiefs, T30 events, T31 variants, T33 setup all configurations, UI-09/T34 full client service.

<a id="tożsamość-i-cykl"></a>
## Identity and cycle

Five additional commanders start with Dale/Erebor/Minas Tirith/Edoras/Woodland Realm (p.3). This additional figure, not replacing base commanders. Initially they are subject to normal Leader movement and elimination; they are not Companion. `SovereignStatus` distinguishes Initial, Awakened, Corrupted and Removed. One identity has at most one active representation: Unit for Initial, Character for Awakened, a separate state without combat classification for Corrupted. The initial elimination of figure returns her tiles to Hunt and does not create a recruit.

Awakening requires its own phase of action FP, the right active nation, Muster (including Army/Muster or Will) or any result of available Ruler and legal route of the level 1/1/1/2/2. Brand selects unconquered City/Stronghold FP; the remaining home of its own designated home. Thranduil requires Elves At War. Remaining in place is a legal route of one region; entering the besieged Stronghold from the outside remains not allowed (p.10). Awakening returns all the tiles, grants the identity of the token and turns on the capabilities; die Ruler recovers only from the next turn (t29 integration). Awakened is not subject to attempts to corruption.

Corrupted is created after a total of at least 2/4/3/3/4, in the place indicated on the obverse, without the route. All tiles are then revealed; the highest standard is rejected, with the draw priority is Reveal, the others return to the pool. Kings counts as zero in this procedure and always returns. Corrupted is not Leader/Companion/Character/Minion, moves nor dies with the army (p.12–13). FP removes dice Character with the current Companion with the right icon of the nation or Free Peoples; ability does not belong to the corrupt figure itself. The removal function is also a point of integration of future events T30.

<a id="próba-zepsucia"></a>
## Attempt to Break

Instead of action dice Shadow selects the initial sovereign and the specific authorized Eye in Hunt. FP must have at least one unused die and Hunt more than one authorized Eye. No ordinary available dice SH does not block this alternative itself. One sovereign may choose once on turn, except for the public exception of the event (p.11–12). T31 adds detailed sources of the Lords Eye.

The cost comes out of Hunt before selection. The number of appropriate tiles is `min(3, remaining Eyes)`. The selection has a permanent cursor and record each next tile; it does not run HuntRuntime or ochrony/przewodnika/Sméagol. The usual Eye and Special are public, omitted and replaced; they do not return before the end of this attempt to not draw them again. Kings immediately completes the selection and ends openly on the sovereign as zero. At the normal end of Shadow selects one of the selected covered tiles, the remaining ones reveal and returns. The eye goes back to released, then returns to the usual interlacing of action.

The tile assigned to the sovereign, the tile retained in the procedure and the tile in the pool are separate places. Physical selection of the pool is shared with the normal Hunt, but not its reactions. Two Kings tiles have a value of 1/2 for Hunt and the effects of events (p.7); identification of their zeros only concerns the corruption of sovereigns.

<a id="polityka-i-usuwanie-kafla"></a>
## Tile Policy and Removal

The actual activation or progress of the nation with the right Companion in the region of the initial sovereign gives FP an optional random return of one covered tile (p.13). It is not selected and its value cannot be removed so openly Kings. The window retains the previous decision, cost and return; the refusal does not repeat the trigger after the reboot. No policy change does not create such an offer.

<a id="awersy-i-słabości"></a>
## Averses and Weaknesses

- Brand: once per battle , after the reveal, with the participating Brand and North unit, replacing
  the effect of their own legal card cancellation card SH on the initiative 0 Corrupted Brand gives
  a similar substitution SH in each battle from North, regardless of Brand's location (p. 7 and both
  sides).
- Théoden: Leadership 3 only in the first round of each battle, usually 1 later; does not require
  the sacrifice of card.
- Dáin/Denethor/Thranduil: upgrade your own Regular in your army and home at At War, also in siege,
  behind Muster; Regular returns to reserve, available Elite is necessary (p.11).
- The second capacity is available only from the corresponding Ruler/Will: Brand and Théoden result
  — two different free settlements and a free character region; Dáin — Regular/Elite at home, then
  Regular/Leader at another free settlement; Denethor — optional choice of additional deck after the
  end of draw/play Event; Thranduil — one mode of travel, a range bonus or a reduction of the number
  of dice Hunt by 1 to the minimum of 1, outside Mordor Track.
- Corrupted Brand blocks Leader/Elite North everywhere, Théoden Leader/Elite in Rohan regions;
  Dáin/Denethor only in Erebor/Minas Tirith; Thranduil only Elite in Woodland Realm. Exception of
  the event does not entitle to ordinary recruitment.
- Corrupted Dáin/Denethor/Thranduil allow once for the right battle siege remove the participating
  SH Regular instead of the Elite reduction. Selection of figure and limit must survive reboot.
- Corrupted Théoden changes controlled by SH Helm’s Deep in Stronghold Isengard, but retains its
  points gained by SH; removing the sovereign immediately disables this weakness.

<a id="zapis-widoczność-i-architektura"></a>
## Record, visibility and architecture

The Kings State is an optional pure domain module; null does not change the old database snapshots. command describe the sovereign, die and route or selected tile, without actor and without result value. The server rechecks each command Full tiles are only in the state of the server and the projection Shadow . FP received status/position s/number and public Kings and required disclosures. identifier the reverse does not encode the hidden tile values; history, options, errors and preview are subject to the same policy. Preview does not download RNG .

The `Kings` module separates the directory, the sovereign cycle, the planning of awakening, corruption, policy, capabilities, combat and projection. The physical pool, movement, loss, recruitment, stack and transport; it does not play these rules in Unity. Options return candidates and groups, without a combination of all routes or figures.

<a id="plan-weryfikacji"></a>
## Verification plan

[Manifesto of the This one. Averses](../../content/kings-sovereign-implementation-manifest.json) binds entry, every ability, exit and all `verificationCases` with real methods. Tests include V14: 1/2/4 Eye, pre-choice cost, secret choice, re-take turn , Eye/Special/redraw , Kings in every place, tie-break Reveal, death, awakening, proper icon, roles figures , the reach of weakness, battle Restart HTTP/PostgreSQL before/after the decisions compare the reference state, both severe projections, none RNG in the preview and the idmpotent receipt. The full regression must keep the unchanged base body and previous expansions .

<a id="wyniki-i-przekazanie"></a>
## Results and transmission

Checkpoint foundation: `KingsSovereignTests`, `KingsCorruptionTests` and `BaseCorpusTests`: **104/104 PASS**, without omissions. Started `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~KingsSovereignTests|FullyQualifiedName~KingsCorruptionTests|FullyQualifiedName~BaseCorpusTests' --logger 'trx;LogFileName=t28-foundation.trx' --results-directory .artifacts/test-results`. Result: `.artifacts/test-results/t28-foundation.trx`. Includes additional figure, five changes, route and siege, death, separation of loss planning from their approval, selection/interruptment and anonymous reverses. The base body has not changed. This is not acceptance T28: capabilities, manifesto and reboot HTTP/PG remain in progress; full regression or Unity client on this checkpoint has not been launched.

Checkpoint policy: `Kings*|BaseCorpusTests` by `scripts/test.sh` with `--logger 'trx;LogFileName=t28-politics.trx' --results-directory .artifacts/test-results`: **138/138 PASS, 0 skip**, including three reboots HTTP/PostgreSQL (choice, secret choice, random return by policy). Cost remains Resolving to decision FP, also in action SH. Each actual activation and every progress is a separate trigger.13; reexecution of the continuation does not generate the offer again. The result precedes implementation of new combat capabilities and upgrade.

Overlay Théodena is calculated for Helm’s Deep on each state check: SH controls settlement and Théoden has Corrupted status. Then the region is Stronghold Isengard (recruitment, Voice, movement, Hunt and conditions cards). The victory points benefit from the original property of settlement; FP recovering Helm’s Deep does not get points for its own Stronghold. The traffic planning copy carries separately active Corrupted as a map context, without sharing the mutant state of sovereigns. The tests compare the lack of preview mutations.

Checkpoint Fights/s swans/upgrades: `scripts/test.sh` with filter `FullyQualifiedName~Kings|FullyQualifiedName~BaseCorpusTests|FullyQualifiedName~SiegeTests` , `--logger 'trx;LogFileName=t28-weaknesses.trx' --results-directory .artifacts/test-results` : **195/195 PASS , 0 Skip**. Includes four reboots HTTP/PG , full cost Regular in assault, three upgrades also in siege Brand vs Swarm at the initiative 0 , Thoden/Daring Defiance, recruitment restrictions, Event as an exception, Helm’s Deep (Muster, Voice, Hunt , VP , movement/preview recovery by FP The two earlier fixture failures resulted from the lack of Elven Leader in the reserve in the standard setup; the test openly transfers existing Leader to the reserve. Full acceptance still requires the second abilities of Ruler and a complete manifesto.

Full checkpoint set ( `t28-checkpoint-combat.trx` ) revealed an error in the order of validation of the empty region in `CanPlaceMuster` : 2364 PASS , 2 FAIL , 0 skip. Fixed target check before reading Kings modifier. Recheck `Kings*|BaseCorpusTests|SiegeTests|UI10ShapeTests` (including HTTP ) with `--logger 'trx;LogFileName=t28-checkpoint-fix.trx'` : **216/216 PASS , 0 skip**. Independent cases were also added null/empty/unknown region, removing five Corrupted, tie-break Reveal and comparison of the full JSON FP for different secrets. A full regression will be repeated at acceptance other T28 capabilities.

Checkpoint second ability: `Kings*|BaseCorpusTests`, `--logger 'trx;LogFileName=t28-ruler-abilities.trx' --results-directory .artifacts/test-results`: **236/236 PASS, 0 skip**, including eight reboots HTTP/PostgreSQL. `UseCharacterAbility` adopts a full recruitment plan Branda/Dáina/Théodena: the server checks the type, reserve, free regions and sediment separation before cost. Additional recruit Branda/Théodena can reach one of the two selected settlements; Dáin requires another settlement for the second recruit. No resources limit the number but does not select for the player of the region.

Jerkír Anárion retains the permission on the event frame and offers an additional deck after the entire effect, also after nested elections. Denethor's exit before this window takes the ability. Optional selection and discard of the excess hand precede the end of the cost. Far-sighted requires an open `AbilityId`; the usual use of Ruler does not automatically trigger the bonus. The route range and reduction Hunt use common validators; they do not change the printed level, throw bonus or the sources of the metastatic. With the zero number of dice Hunt no additional throw occurs. The minimum one applies to the positive pool.

Fixture of this range openly provides die source `ruler`; its actual recovery, number and tokens are covered by T29. Contracts describe modes and limitations for UI-09. This checkpoint does not count full service in Unity.

<a id="odbiór-2026-10-06"></a>
### acceptance 2026-10-06

- Full `bash scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
  --logger 'trx;LogFileName=t28-full.trx' --results-directory .artifacts/test-results`: **2431/2431
  PASS**, 0 skip, 1 min 25 s; actual PostgreSQL, without the regeneration of the base body.
- This one. obverses, 20 ability, 47 SRC-06: manifesto test compares statistics, full text of cases and
  availability of methods without Skip. Control of his appeals in TRX: **45 methods, 113 runs
  PASS**, including eight methods of rebooting HTTP/PG. The full set also includes tests not
  assigned to single obverses (trespekts of corruption, policy, privacy).
- `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t28-editor.xml
  -logFile .artifacts/t28-editor.log`: **129/129 PASS**, compilation of shared contracts at Unity
  6000.6.0f1.
- `python3 scripts/source_kings_characters.py` : 13 sources and groups 10 + 3 PASS ; `python3
  scripts/check_catalog.py` , `python3 scripts/tasks.py validate` and `git diff --check` : PASS.

The initial set of new boundaries had 242 PASS / 5 FAIL: fixture did not include an additional dice Stridera, a refrill of a single element pool or a legal entrance Companion to the hostile, unrestrained Stronghold. The revised scripts according to the base; then the **11/11 PASS** in `t28-boundaries-fix.trx` and the full regression above. The first build of the manifesto test detected a comparison of enum/string; the revised test compiled in the final acceptance. The rules for matching the incorrect fixture expectations were not changed.

The Kings structure, common effects and planning have been reviewed: small classes by responsibility, no transport in the domain, limited search for slots, no changes in the serialization of the existing body. Documentation includes Contracts, EXPANSIONS, rule directory, source index, TESTING and CODE_QUALITY. The next task **T29** integrates the real Ruler, tokens and three chiefs; T30 adds events, T31 variants. Normal start expansions, full click on new decisions in Unity and configuration parties belong to T33/UI-09/T34. W T28 has not performed PlayMode new interactions, Windows or hand tests.
