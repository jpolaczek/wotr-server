<a id="qual-11--węższe-odpowiedzialności-kontrolera-decyzji-klienta"></a>
# QUAL-11 — Narrower responsibilities for the client decision controller

<a id="cel"></a>
## Goal

P2 repapactorization for R08: separate host selection, decision presentation, local draft and send intent, reducing the scope of changes in central controller.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

It requires QUAL-10 and QUAL-04. Area `TurnInteraction.Render/RenderLayout`, `DecisionTray`, existing controllers and new `ReinforcementsInteraction` and recruitment models UI-130/UI-131. Sources: review, current architecture, `docs/ui/interaction-inventory.json`, UX_SPEC and ARMY_MAP_UX.

<a id="logika-i-zakres"></a>
## Logic and scope

- Create a matrix type of decision → host → presentation model → draft → commit. Use existing
  modules; do not create one handler with another great switch or abstraction for each button.
- The central controller is responsible and selects the appropriate module. Details battle,
  Fellowship/Hunt, characters and meals belong to their modules. The system refreshes on the basis
  of significant changes, it does not play everything the frame.
- The window host uses QUAL-10 The draft has decisionId/wersj The Commit goes through one
  sender/journal.
- Legality and explanations come from the server projection; without copying the rules to Unity. No
  selection of the non-equivalent dice, route and cost per player.
- Keep complete clicks, ✓ for collective choices, discards from the hand of two clicks and the
  operation of new meals in Muster/cards/zdolno Scissors.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- All inventory positions have one owner and a way of doing it. The new variant in the existing
  family does not require duplication of conditions in `Render`, `RenderLayout` and `DecisionTray`.
- There is no second sender, no record of the authoritative condition in the client, no divergent
  copies of kosztu/legalnosci. The approved step does not disappear after Esc.
- Full behavior tests and profiles confirm the lack of gestures, privacy, budgets, liquidity and the
  new UX. The number of small files alone does not count the task.

<a id="weryfikacja"></a>
## Verification

EditMode models and PlayMode all decision families, costs/retry/change decision during draft, both sides and expansions. Full runner QUAL-06, QUAL-08 profile sets and comparison of the inventory manifest. Overview of the diff for the dependencies and number of places required when adding the decision variant.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

`docs/ARCHITECTURE.md` , `docs/ui/interaction-inventory.json` , `docs/TESTING.md` , `wotr-client:docs/ui/UX_SPEC.md` , `ARMY_MAP_UX.md` , spec/rejestr. Pass QUAL-18 the final responsibility matrix and proof of the cost of rendering.
