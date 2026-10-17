<a id="t15--pozycja-drużyny-i-towarzysze"></a>
# T15 — Position Fellowship and Companion

Sources: BASE p. 24 (form movement), 34 (activation), 36–39 (Fellowship); FAQ 1.2 p. 1 (disclosure target, original affiliation settlement), p. 2 (each Stronghold Shadow, the input Companion activates the nation); statistics from SRC-02. cards character develops T20. Full Hunt, including the execution of additional tiles after disclosure, is T16.

<a id="wejścia-i-legalność"></a>
## Entry and legality

Fellowship keeps the last known position, abstract progress, hiding, level Corruption , Guide The usual increase in progress does not choose or record a secret route. command location includes the entire route, including the initial region; one-piece list means stay. Each pair must be a real neighborhood. We do not automatically choose the route from the different variants with different consequences. Stronghold is one source of extra tiles.

Declaration: only hidden Fellowship except Mordor, in his phase, at most once per turn . Reach does not exceed progress. After approval, progress returns to zero, hiding remains. city/Stronghold FP Controlled by FP It's a one-point-to-nothing- a declaration in an unattractive city. Stronghold The nation's own activating it. T21-FP-CHAR-03 corrects earlier interpretation: acquired settlement does not serve activation either ( BASE p. 32 Chapman, [clarification of policy constraints](https://boardgamegeek.com/thread/1291849/article/35958552#35958552) ; Almanac v98, entry Ring-bearers, p. 7 ). Fellowship can pass through a besieged friend Stronghold .

Disclosure: Forced by a rule, not free command Shadow A permanent decision is opened FP with the preserved back. The exposed position cannot end in the original city/ Stronghold FP Controlled by FP The route you have travelled resets progress; it does not heal and activate the nation. Behind each different primary Stronghold Shadow still controlled by Shadow on the route (including beginning and end) an additional obligation is recorded Hunt The frame remains open to settlement of these tiles in T16; we do not consume dice We're not going back to normal. action Before they were performed. Before T16 normal action Movement Fellowship and card the disclosure is not made available as ready-to-use programs.

Hidden: in action FP, only at Revealed, the cost of Character (or Will of the West as Character). It does not change region/Progress, die goes to Spend, not Hunt.

<a id="przewodnik-i-oddzielenie"></a>
## Guide and separation

Guide has the highest level in the current lineup; draws belong to FP. In the Fellowship phase, the Guide selection ends this phase. Continue retains the current legal Guide. Each change of the lineup reopens the selection if there is more than one candidate, also when the previous Guide still meets the condition. With one candidate the score is forced. With the empty Guide composition, Gollum is left; we do not create Companion or figure from the Army.

Separation: one blank group, cost of Character in normal action, prohibited on the Mordor track. Reach is progress plus the highest level in the group. All selected characters are found in the common goal. If the last known position is friendly Stronghold under siege, the group appears in the garrison and does not leave the region. figures/statistics we do not guess: we use the SRC-02 directory. The character cannot return to the composition. For forced separation by effect there is a saved selection point back to the overriding procedure; it does not download the dice twice.

<a id="ruch-poza-drużyną"></a>
## Traffic outside Fellowship

One die Character pays any independent group Companion on the map. The group starts in one region and one part siege, has a common route of length up to the highest level. You can separate one initial group into several, but the same character cannot be used twice or raised by joining after the first movement. The enemy army does not block. After entering any Stronghold controlled by Shadow the group must finish the movement; this is not only a limitation of the original Shadow fortresses. You cannot enter or exit the friendly Stronghold under siege. Passing through the black borders is illegal. At the end, the Companion authorized icon activates the cities/Stronghold nation, also at the entrance (FAQ).

<a id="zapis-widoczność-i-weryfikacja"></a>
## Record, visibility and verification

Location frames, Guide and forced separation store source, return mode, approved route and uncounted fortresses. The entire cost is charged once; the Guide selection does not switch turn before the completion of the action. The inside frame does not enter the view. Candidates/routes is owned by the authorized FP; the preview reveals only known effects, without drawing the tiles. The public composition uses characters' identity, private version definitions remain separate from the identity for the Council.

`FellowshipTests` and `CompanionTests` check V09, legal and forbidden boundaries, remain, declaration/disclosure/hiding, Moria–Dol Guldur route, primary affiliation/control, treatment once on turn, guides/draws/Golluma, common group coverage, siege and reuse figure. `FellowshipPersistenceTests` destroys the host HTTP when selecting the route, forced group and Guide, plays the state with PostgreSQL and renews the receipts without second cost. Shadow does not get private options.

Final regression `bash scripts/test.sh --no-restore` : **155/155 , zero skip**; includes a re-test after refactoring the projection. `bash scripts/test-client.sh` : **15/15 PlayMode**, 2026-10-05 02:19:43 UTC . EditMode In `.artifacts/T15-edit.xml` check the compatibility of the extended DTO And the current drafts. It's a client regression; new interactions Fellowship Will answer UI-08 - The tiles. Hunt/zdolno • are assigned to the T16/T20.
