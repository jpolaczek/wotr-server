<a id="qual-16--czytelny-onboarding-i-polityka-ciężkich-zasobów"></a>
# QUAL-16 — Clear onboarding and large-asset policy

<a id="cel"></a>
## Goal

Improvement P3 for R10: the new contributor quickly distinguishes the current architecture and the running instructions from the historical acceptance; repo has a policy of growth of binary materials without losing data.

<a id="kontekst-i-zależności"></a>
## Context and dependencies

Requires QUAL-14. Audit: client about 692 Mi B of which approximately 672 Mi B This `graphicalassets` , pack Git around 434 Mi B It's a cost. clone/CI , not proof of low FPS . ARCHITECTURE , TESTING , WORKFLOW and UX_SPEC have extensive historical additions. Sources: review, README/DEVELOPMENT and bootstrap QUAL-14.

<a id="logika-i-zakres"></a>
## Logic and scope

- Prepare a short path: tool requirements, bootstrap, launch, tests, module map and first safe
  contribution. Documentation in Polish, code/UI in English, according to AGENTS.
- In the main documents, distinguish current contracts and requirements. Historical acceptance
  transfer to a readable archive with links, keeping dates, evidence, sources and limitations; do
  not create a second source of status next to the register.
- Separate runtime assembles from inactive iterations and references based on actual appeals. Measure clean clone/checkout and sizes before/after; the transfer of the
  directory itself in Git does not reduce history.
- Set rules for new large files, thresholds, LFS/archive and reproducible download of the needed
  materials. Selecting the mechanism justifying the measurement of cost/requirements, without
  indiscriminate dependence on private storage.
- Without rewriting Git history, losing archival materials or removing required source rules. If a
  real reduction of clone requires history migration, prepare a separate specific plan, do not
  declare its execution.

<a id="kryteria-odbioru"></a>
## Acceptance criteria

- The transition to clean environment leads to a working client and a test with a real server
  without the author's knowledge and undescribed steps.
- Current architecture, workflow, statuses and old evidence are clearly separated and linked by
  links. The agreed exceptions of UX have not been lost.
- The size/clone report describes the actual effect; policy prevents unjustified addition of
  further great iterations to the primary checkout.
- References Unity, GUID/.meta, builds and source validators remain valid.

<a id="weryfikacja"></a>
## Verification

Clean clone/checkout and transition quickstart in isolation; control of references to resources and manifests, build the client and appropriate tests. Both documentation validators, control of links to archive and compare sizes/collected bytes. Do not expect FPS profit from the very sorting of materials.

<a id="dokumentacja-i-przekazanie"></a>
## Documentation and handoff

README, DEVELOPMENT and the principles of input of both repo; `docs/ARCHITECTURE.md`, `docs/WORKFLOW.md`, `docs/TESTING.md`, spec/rejestr. Create archive and resource policy where needed. Forward QUAL-18 to the onboard test.
