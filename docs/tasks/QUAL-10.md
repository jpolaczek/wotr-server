<a id="qual-10--jeden-koordynator-cyklu-życia-okien"></a>
# QUAL-10 — One window lifecycle coordinator

<a id="cel"></a>
## Goal

Refactoring P2 for R08: a new window requires a declaration of your family, parent and behavior, instead of adding pairs of seals in existing panels.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

It requires QUAL-07/09. Area `BoardScreen`, transient windows, `WindowFamily` and subscriptions `Opening`/`CloseTransient`. Read the current UX_SPEC, BOARD_INTERACTION_UX, ARMY_MAP_UX and the tasks UI-89/UI-92/UI-98/UI-130/UI-131. The new Meal Panel is part of the initial state. Source: R08 review.

<a id="logika-i-zakres"></a>
## Logic and scope

- First save the current matrix: hand, inspection, pinned card, parent/podokno, current decision,
  left slat, Fellowship/Hunt, meals, Menu/Settings and history. Mark coexistence, replacement and
  priority Esc.
- Enter one owner of opening/closing, activity, parent-child relations and Esc order. Existing
  panels report intent, do not close all others by separate subscriptions.
- The parent closure closes the children; the removal/reconnect invalidates only the appropriate
  contexts. pinned card and the new decision retains the rules of UX.
- Keep the exception of interactive bookmarks on the hiver, text tooltips, pressed/release, terminal
  target click, drag reserves and army traffic clicks. Window coordinator does not interpret the
  legality of the rules and does not send commands.
- Eliminate double subscriptions and leaks after mount/dispose.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- The addition of the window in the existing family does not require editing all other panels or new
  pairs of exclusions.
- `BoardScreen` retains the depositing of dependence; the coexistence rules and Esc are in one
  place. No second parallel closing mechanism is created.
- UI-130/UI-131, history reading, reconnecting and privacy preserves behavior. The board profile
  shows no additional reconstruction of the entire UI.

<a id="weryfikacja"></a>
## Verification

PlayMode open and close by matrix in four layouts; repeat mount/dispose, Esc at Settings and the window, change decision at local draft. Full runner QUAL-06, EditMode and target board profile with QUAL-08. In diffie check to remove pair relationships, not only move them to file.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

`docs/ARCHITECTURE.md`, `docs/TESTING.md`, `wotr-client:docs/ui/BOARD_INTERACTION_UX.md`, `ARMY_MAP_UX.md`, spec/rejestr. To transfer QUAL-11 matrix and one open-source window host contract.
