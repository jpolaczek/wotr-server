# Expansion saves — EXP-03

Eleven frozen states from games of two demo CPUs (`DemoBot`), one at the first time each expansion decision comes up
within the first four turns: the Companion versions and the starting Guide, the special die, Vilya's keep, the
Council's homelands and token, the Ruler discard, a Corruption tile under the standard, Visible and Sequential rules,
and an action of a Fate + New Cities game and of a game with every expansion and variant. Of the seeds that reach a
case, the smallest save was kept. The manifest gives each file's SHA-256, schema, configuration, decision and owner,
the CPU's answer and the decision that follows it with a zero random source.

`ExpansionCorpusTests` reads these files from the build output: each reads back to itself, the CPU's answer is
accepted and leads to the recorded decision, and neither side's projection changes when only the other side's hidden
information does. Dismay is not here: the CPU never spends the Ruler token that grants it; its saves are kept by
`KingsIntegrationPersistenceTests`.

Export to an empty work directory, then compare before replacing (do not regenerate to make a failing test pass):

```sh
bash scripts/dotnet.sh run --project tests/Wotr.Scenarios -c Release -- \
  --export-expansion-corpus .artifacts/expansion-corpus-candidate
```
