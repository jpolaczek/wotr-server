<a id="qual-12--ograniczony-snapshot-i-trwałe-archiwum-historii"></a>
# QUAL-12 — Bounded snapshots and a persistent history archive

<a id="cel"></a>
## Goal

P2 implementation for R07: cost copying/recording working status and version checking does not grow linearly from archival Events/RandomSamples. Full history and reproducibility remain preserved.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

Requires QUAL-01/03/05/06/17. Sources: review and `history-micro.json` , `GameEngine` , `StateCodec` , `MatchState` , `MatchStore` , event index, history reading and WS Microbunchmark 0 → 10 thousands of events: about 0,87 → 10,59 ms and 0,23 → 9,39 Mi B for a simple accepted decision without DB . Steady load 300 the session passed; refactoring is performed after corrections and stabilisation acceptance .

<a id="logika-i-zakres"></a>
## Logic and scope

- Separate the minimum condition needed for the next decision from archival events/lots. The domain
  remains pure C#, it does not read SQL or network.
- The new events/RNG has a clear cursor and permanent ID; the state, the growth of the archive
  and the receipt are atomicly validated. The recovery of the response stack does not require
  loading the entire history. Do not delete the data required by the active rule.
- Identify all Events/RandomSamples readings before changing; bring the necessary summaries to the
  working state. Reuse the existing history index, without the second competitive source of truth.
- Add lightweight fixed versions for heartbeat/ReadVersion, including a private auto-decision
  version; without reading the entire JSONB every five seconds. Keep detecting changes between
  processes that RevisionHub does not provide itself.
- Migration of existing 1/2/3 schema and newer supported versions is re-newable, detects damage and
  does not reset games. The archive has indexes, pagination and access policy per page. This is not
  a full event source.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- Save/restart in battle, Hunt, card and private reaction retains legality, RNG, costs, next ID and
  a full history of both sides.
- The repetition/conflict of command and the migration error do not duplicate events or form a
  partially approved batch. The old receipts are still running.
- With identical active status and 0/500/2000/10000 archival events, the working memory and a copy
  of snapshot are limited in size independent of the length of the archive. Benchmark confirms the
  removal of the former czasu/alokacji. growth
- Heartbeat does not deserialize the full state; history continues to return all authorized entries,
  without gaps, replays and disclosures of secrets.

<a id="weryfikacja"></a>
## Verification

Complete backend; migration matrix with active continuations, migration interrupt and reboot; transactions PostgreSQL, retra and history privacy. Compare microbenchmark and legal workload SQL before/after on the same machine, with warmup/dispersal; report bytes, allocations, p50/p95/p99 and heartbeat queries. Integration of client history and 300 session in QUAL-18.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

`docs/ARCHITECTURE.md`, `docs/server/T07-TRANSACTIONS.md`, `docs/server/T38-DEPLOYMENT.md`, `docs/TESTING.md`, spec/rejestr. Pass the diagram, the instructions migration/recovery and the comparison measurements QUAL-18.
