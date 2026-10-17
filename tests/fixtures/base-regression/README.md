<a id="zapis-bazowy-t23"></a>
# Baseline T23

28 fixed snapshots of the internal state of the server. Manifesto attaches files through SHA-256, scheme, rules, content, decision owner, response and expected result. Files contain only artificial test and fictional positions of identifier. They do not go to Unity or to API response.

Coverage: 22 type of base decision, status after transfer Ring with still available revised dice, secret card attacking before choice defender and four results. Sources and acceptance: [T23](../../../docs/T23-ACCEPTANCE.md), [B01–B20](../../../docs/rules/LOGIC.md), [manifest kart](../../../content/implementation-manifest.json).

`BaseCorpusTests` loads these files from the result directory. It does not run the generator. For 24, unfinished items are saved by command, using the open test RNG returner zero. The result hash includes all continuation, cost and saved samples; it does not replace rules control in dedicated tests. `BaseCorpusPersistenceTests` uses the same files when restarting the real HTTP/PostgreSQL and retry. This is separate evidence from the round-trip JSON itself.

Export to an empty work directory:

```sh
bash scripts/dotnet.sh run --project tests/Wotr.Scenarios -c Release -- \
  --export-base-corpus .artifacts/base-corpus-candidate
```

Export does not require a database and does not overwrite the existing directory. Before knowingly updating, compare all changed fields, rules sources, examples and test results. Do not regenerate the body as a way to remove the test failure. Migrations should retain historical inputs; a new version may need a separate directory and open expectations after migration.

Controlled positions do not pretend to record the full gameplay. A full T23 batch is created separately by the production lobby RNG Transitional stacks above limit remain only in the required window `RemoveExcess` ; armies of two pages/garrison in one region counts separately. `no-regular` all 15 regular Gondor was disassembled after 10 and 5 In Pelargir/Dol Amroth to Miss figure to degradation, he did not introduce an independent, illegal pile.
