<a id="t10--wdrożony-cykl-tury"></a>
# T10 — Implementation of the turn cycle

Sources: BASE p. 18–22 (phases, dice , pass , Rings, Picking), p. 44 (militarisation phase control 6 ) and FAQ 1.2 p. 1 (first the whole selection procedure, then discard). Files and totals of sources are in [manifest.json](../sources/manifest.json) Code: `src/Wotr.Domain/Turns` ; tests: `TurnTests` , `TurnPersistenceTests` .

<a id="przejścia-i-utrwalone-decyzje"></a>
## Transitions and established decisions

1. The start of the accepted room shuffles the decks by MatchFactory and calls TurnFlow. Start.
   Setup does not select cards itself. The 1 phase recovers dice and selects each page on one card
   from both decks. The deck remains empty, we do not shuffle.
2. Only after the entire selection we check the limit of 6. With excess FP receives its own decision
   DiscardCards; then Shadow, if it has exceeded the limit. In the hand it can be at the moment 8
   cards. The alien client sees the number, not the choice cards or their ID.
   `AfterDiscard=RecoverDraw` restores the same continuation after writing.
3. The Fellowship phase is an open decision of FP. T10 makes the continuous phase available, i.e.
   the completion of the phase without declarations/changes Guide. The full actions of this phase are
   added by T15; they are not automatically executed by the client.
4. Shadow selects specific standard dice allocation. Minimum 1 only after recovery of dice FP with
   origin `fellowship-move`; Eye Keeper does not meet this condition. Maximum `min(available pula,
   max(1, number of companions))`. Trustees do not increase the limit.
5. The other dice are thrown on the server, the results are recorded. Each Eye Shadow goes to Hunt.
   The action phase starts with FP; later it changes the player only completed akcja/pas. Page
   without dice is omitted. The reaction reproduces the saved window, without replacing the normal
   performer action.
6. After both pools have been exhausted, the 6 phase checks the current base control of the
   sediment: Shadow 10 points, then FP 4. Without victory, the next turn begins. The movement and
   battle changing the control and the full victory system are later in range.

Automatic transitions do not require an unnecessary click “further”, but do not jump over the mandatory selection. Each command ends with another real window and is saved by the T07 transaction.

<a id="komendy"></a>
## Commands

| Command | Legality and effect |
| --- | --- |
| ContinueFellowship | FP, 2 phase. Opens Shadow allocation. |
| AllocateHunt | Shadow, 3 phase, various ID from its own pool and correct min/max. Allocation → throw the rest → first available action. |
| DiscardCards | Owner of the current window, exactly excess, different card your own hand. Covered discard; reproduces the written sequel. |
| DrawCard | action phase, the own available Event or Will of the West, selected deck empty. die transitions to Resolving; selection and possible discard; then Spent and change of page. |
| DiscardDie | Own available die; Discarded without effect, end of action. This is not free pass. |
| Pass | Closer to the available dice than the opponent; without the use of dice. It does not replace the normal action immediately after using the Ring. |
| UseRing | The own Ring, at most once by a given page in turn, the own available die and other allowed result. FP transmits the Ring Shadow, Shadow removes it. Normal action stays with the Ring user and can release another die. |

Empty decks are omitted in the selection phase. The purpose of the action DrawCard from the empty deck is unavailable (BASE p.22: no further selection from it is possible); rejection of dice without effect remains a separate legal option.

FP does not create the Ring Will of the West or Eye Shadow. Shadow can create an Eye; die can immediately go to Hunt with origin `elven-ring`. You can not change the Eye located in Hunt. When you change the Eye there is no longer your own dice action, the transition skips the empty jackpot. Use the Ring and the next action have separate receipts; `RingActionPending` restarts.

<a id="koszt-i-dalsza-integracja"></a>
## Cost and further integration

`DiceRules.Pay` checks and records ID dice in PaidDieIds. Will of the West can pay the normal cost and Army/Muster the right one category. We do not treat each dice as any resource. The settled cost remains Resolving on an additional decision, for example, reject excess after selection.

`CompleteCost(..., fellowshipMove: true)` is a T15/T16: integration point only after the entire Hunt moves paid die FP to Hunt with the right origin. The test returns the record between pay and ending. T10 does not issue a false command for the traffic bypassing Hunt.

`ExtraDiceNextRecovery` is the target number of additional normals dice maintained by living characters (0–2 FP, 0–3 Shadow), not an increase of each turn. This change does not affect the current pool; it only works on the next recovery. The input/transition and death of the character will connect T20. Special dice expansions requires their separate tasks.

<a id="klient-i-weryfikacja"></a>
## Client and verification

Server options transmit ID candidates, acceptable decks/ The tray shows a visual choice, cost preview and one commit. Pass/Continue are a direct commit. There is no field to enter the number dice / cards or scrolling decisions. Local selection is immediate; the previous request preview is cancelled and the late result ignored.

The server does not return the future card or the result of the preview throw. The client does not choose another dice alone after the change of status. Recon activating still legal parts of the selection; the illegal cost is cleaned and requires a choice. The map remains available when sent. The uncertain response retains a specific commandId; Check result recovers the receipt or repeats exactly the same command.

Performed: scripts V01/V02, both limits Hunt with 0/2   Companion, both discards after full selection, empty deck, phases, side without dice, cost after rejection, reaction without change of contractor, additional dice upon recovery, transmission/usuni ring ring, forbidden walls, second ring and real PostgreSQL played with a new connection before another action Unity checks two sessions and the actual commit with a deliberately lost response HTTP, then receipt recovery, Hunt And the first pitch. 10   dice  /  8   cards  /  3 The rings are located in 1366×768 and 1920×1080.

Restrictions: full text cards They still have gate source; current labels cards is a type and printed number. The implemented range does not represent a full game. Manual user and runtime Windows tests are postponed. The exact commands and end results save task registry.

<a id="wyniki-zamknięcia-t10"></a>
## T10 closure results

- `bash scripts/test.sh` — 54/54 tests .NET, without being omitted; integration uses PostgreSQL
  17.11. It also includes military thresholds 9/10 and 3/4 and priority Shadow in the 6 phase; the
  victory Rings remain in T17.
- `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t10-edit.xml
  -logFile .artifacts/t10-edit.log` — 6/6 tests, without being missed.
- `bash scripts/test-client.sh` — 4/4 tests PlayMode, without being omitted. Two independent
  sessions work in one Unity test process, with a real server and a database; it is not a sample of
  two physical computers.
- `dotnet format whitespace` with `--verify-no-changes` for the Assetts solution and directory — no
  formatting changes. Overview of module boundaries and costly paths describes
  [CODE_QUALITY](../CODE_QUALITY.md).
- `bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildMac -logFile
  .artifacts/t10-mac-build.log` and `BuildWindows` respectively with
  `.artifacts/t10-windows-build.log` — both builds completed successfully.
- The launched binary macOS client remained active by 8 seconds, registered the start and reported
  no exceptions or missing data ICU/panelu. The process was stopped after checking; this is an
  application start attempt, not full batch. Log: `.artifacts/t10-mac-smoke.log`.

Automatic UI trial dumps are in `.artifacts/screenshots`: map view, siege, login, configuration and selection The Ring, Hunt and discard. They were visually tested; neither the FPS nor the manual acceptance ergonomics measurements were performed. The macOS application run attempt detected a lack of packaged text resources, repaired by the persistent WotrPanel resource. The end build result and application starter saves the history of T10.

Transfer: the rule code is in `src/Wotr.Domain/Turns`, transactions in `src/Wotr.Server/Persistence`, and client election support in Unity `Application/TurnInteraction`, `Presentation/TurnDraft` and `UI/DecisionTray`. The next range starts with UI-03, then action T11/T12 according to dependencies; it is not part of the order to T10. Keep the local selection and authoritative validation chapter, the privacy of the discard, and identifier cost and command on re-repeated.
