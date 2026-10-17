<a id="qual-14--anonimowy-bootstrap-przypiętych-zależności"></a>
# QUAL-14 — Anonymous bootstrap of pinned dependencies

<a id="cel"></a>
## Goal

Integration P1 for R05: a new tho-writer without cache and tokens of the author can recreate client relationships and run the actual integration of both repositories.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

Sources: `package_deps.py`, `wotr-client:scripts/bootstrap_deps.py`, `wotr-deps.lock.json`, DIST-02/03 and UI-133. Anonymous APIThe required Contracts 0.3.0 returned in the audience 404; local cache of the author worked. Hashination and public versions should be kept. Do not remove the UI-133 lock or declare that the public parcel already exists.

<a id="logika-i-zakres"></a>
## Logic and scope

- Deliver a public channel of the versified archives or complete reproducible path to build a local
  channel from public sources. The client still does not compile the backend in his build and does
  not require his checkout as a hidden step; local packaging is an open separate path.
- Cover Contracts, allowed public content and backend/seeder test. Prepare a consistent set with the
  current server, including the reasons for the UI-129 recruitment. If the release replaces the
  UI-133 range, save the actual proof of compliance with its criteria in the separate acceptance; do
  not change the status for the intent itself.
- Keep SHA-256, commitu/version pins and file swap refusal. No package or bad hash gives a clear
  error, without silent use of any wersji/cache.
- The instructions separate the player's download from the developer bootstrap. Prepare the archives
  and local tests before possible publication; check the channel operation anonymously after proper
  publication.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- Clean isolated environment, without `.deps`, global cache and tokens GitHub, download/play
  exactly pinned set and launches the client and PlayMode with a real server. The instructions
  contain all steps.
- Attempting to lack a package, a faulty SHA and an incompatible version ends with control. Local
  random cache does not count anonymous availability.
- The new backend/seeder corresponds to the sources; the meal panel receives the reasons for
  recruitment. The pins of both repositories and documentation indicate a consistent commit.

<a id="weryfikacja"></a>
## Verification

`package_deps.py` Tests and existing release/bootstrap Tests; pure tool profile directory without inherited credentials, new cache and log steps. Bootstrap, EditMode and targeted PlayMode Networks/recruitment; inspection of archive content, hashe and both documentation validators.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

README and DEVELOPMENT both repo, `docs/release/DISTRIBUTION_PLAN.md`, `docs/TESTING.md`, spec/rejestr. Forward QUAL-15/16 accurate command, environmental requirements and proof of anonymous launch.
