<a id="qual-15--wymagana-bramka-unity-i-izolacja-publicznych-pr"></a>
# QUAL-15 — Required Unity CI gate and public pull-request isolation

<a id="cel"></a>
## Goal

Integration of CI, P1 for public project: green required status means complete validation of relevant sources, and the code with PR does not have access to permanent host, publication secrets and trusted cache.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

It requires QUAL-06/07/08/14. R06 review: workflow client has optional job Unity for `vars.WOTR_UNITY_CI`, self-hosted runner and token private parcels. Audit did not check remote varibles or branch protection; do not claim that the current remote configuration is already known. Server and client retain separate CI.

<a id="logika-i-zakres"></a>
## Logic and scope

- Enter the unambiguously required aggregate result EditMode, complete PlayMode, desktop building
  and validation of dependencies/documents. Skipped, missing, cancelled or unexecuted required job
  does not give the green acceptance.
- Integration uses pinned package and Runner QUAL-06. Separately validate the sources of the HEAD
  base in CI server; report openly what backend was actually used by the client. Historical archive
  does not include a new HEAD.
- For untrusted PR provide ephemeral isolated runner/VM or controlled gate start in isolation. Do
  not start a checkout PR with secrets on a permanent host; `pull_request_target` is not a shortcut
  to such a start.
- Separate test/build from publication and signing. Minimum token privileges, separate trust cache,
  cleanup and XML/log artifacts with retention.
- Prepare workflow and the settings/required checks before remote changes. None
  runner/licence/dost the slop is public; local YAML does not prove the required operation gate au
  GitHub.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- The actual PR with the omitted Unity, one failure of PlayMode or the missing one does not meet the
  required check. The correct PR performs the full set.
- Public/fork PR does not receive secrets or status of previous performance; after completion,
  there remains a permanent runner controlled by PR.
- Both CI have source/package evidence, artifacts and proven required statuses. Build Windows is
  described as compilation unless runtime is also performed.

<a id="weryfikacja"></a>
## Verification

Local validation of workflow and actual controlled PR: correct, intentionally red, omitted job, incomplete inventory and untrusted fork. Verify the configuration of branches protection/required checks, permissions, isolation and removal of the runner. Do not send secrets in logs. If remote launch is not possible, do not mark the full task as done.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

`docs/TESTING.md`, DEVELOPMENT both repo, `docs/release/DISTRIBUTION_PLAN.md`, spec/rejestr. Save the required status names, trust limits, infrastructure owner and evidence PR to QUAL-18.
