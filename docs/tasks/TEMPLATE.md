<a id="id--tytuł"></a>
# ID — title

<a id="cel"></a>
## Goal

State a concrete result for acceptance. Specify whether this is a prototype,
implementation, integration or aggregate task.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

The `tasks.json` record is the source of status, dependencies, requirements,
rules, gates and the reading list. List prerequisites and explain significant
scope boundaries.

<a id="logika-i-zakres"></a>
## Logic and scope

Describe inputs, legality, costs, order, choices, effects, hidden information
and save points. For UI work, specify click interactions, previews, commit and
a difficult case. For infrastructure, specify the result and a failure scenario.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- An observable completion condition, with a result consistent with the source.
- A verified boundary or illegal case.

<a id="weryfikacja"></a>
## Verification

Give meaningful scenarios and how to run them. At completion, record actual
commands/results in history. Do not write “tests pass” without evidence that
they were executed.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

Identify files to update and information needed by the next task. Record START,
checkpoints and DONE/BLOCKED as required by WORKFLOW. New gaps need an ID and
an unblocking condition.
