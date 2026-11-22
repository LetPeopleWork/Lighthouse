# Story 6363 — S03e Package Access and Licensing: the sixth and seventh contexts in their own modules
**Finished:** 2026-11-22 · ADO User Story #6363 · Epic #6510 "Modules: package the bounded contexts"

## The problem

Access (who may do what) and Licensing (whether Premium is available) were still one lump, RBAC/Identity, spread over
`Services/{Interfaces,Implementation}/{Auth,Authorization,Licensing}`, plus four value types in `Models` and two ASP.NET
attribute families (the guards). Four architecture rule files named these by the old namespaces, and the module map
carried one key, `RbacIdentityPattern`, whose only job was to say the old RBAC area was not empty. No rule said who may
ask Access or Licensing, what either may reach, or where the guards, markers and authentication handlers belong. The
licence public key was an embedded resource beside its verifier, found by the end of its resource name, so a move had to
carry two project-file paths with it.

## What changed

Three nwdes features on `main`, behaviour-free, no schema change, no route or DTO change. Design:
`docs/feature/story-6363-s03e-package-access-licensing/`.

- **Feature A, rules first** (`docs/feature/story-6363-s03e-a-rules-first/`, 4 slices). No production code changed.
  - A1: every architecture rule that named Access or Licensing by the old namespaces also sees `Modules/Access` and
    `Modules/Licensing`, each proven with a made-up type (`AccessAndLicensingRulesSeeTheModulesArchUnitTest`).
  - A2: the membership and layer of each type were measured from the compiled code and fixed in
    `AccessAndLicensingMembershipArchUnitTest`, by short name so it held before and after the move. It admits four
    `Models` types. Its review found that the layer check could be fooled by a namesake in another layer; a follow-up
    commit closed that.
  - A3: the new examine command `access-answers-compare` compares the Access and Licensing answers of two revisions,
    with a licence import between two licence reads. It says DIFFERENT with exit 1 for one changed answer and IDENTICAL
    with exit 0 for none.
  - A4: the new examine command `ci-auth-suites-show` shows, for the newest finished CI run at or before a revision, the
    Verify Authentication job and the result of its authentication, RBAC and proxy-auth steps.
- **Feature B, the move** (`docs/feature/story-6363-s03e-b-move/`, 4 slices). Namespace lines follow the folders; the
  unit tests moved with their production code.
  - B1 Licensing: `Application` (4 files) and `Adapters/Http` (1), with `public_key.pem` beside the verifier and the two
    project-file paths edited in the same commit.
  - B2 Access `Domain` (8) and `Application` (16): four `Models` value types moved in, and `EmbedHandshakeOutcome` became
    a file of its own. A layout pin fixes the public surface of `IRbacAdministrationService`.
  - B3 Access `Adapters/Http` (8): the guard and marker attributes, the API key and disabled-authentication handlers, the
    scheme selector and the blocked-mode filter. Pins fix the attributes' shape and the count of guarded actions, and the
    development log key for the disabled-authentication handler follows its new namespace.
  - B4 the Licensing test data (`TestLicenseData`, the three licence files and `LicensingIntegrationTest`) moved to
    `Tests/Modules/Licensing`, so the test tree mirrors the module tree. The paths in `.gitignore`, both CI workflows,
    the test project file, `LicenseControllerTest` and the examine builder follow.
- **Feature C, edges and rules** (`docs/feature/story-6363-s03e-c-edges-and-rules/`, 2 slices).
  - C1: `AccessModuleBoundariesArchUnitTest` and `LicensingModuleBoundariesArchUnitTest` on the shared `ModuleRules`,
    with the lists measured from the compiled code (R-A1 to R-A7 and R-L1 to R-L5, in ADR-270), and one new shared
    rule, `ModuleRules.HttpAdaptersAreReachedFromOutsideOnlyBy`, that guards who may reach a module's `Adapters/Http`,
    attributes included. `ModuleWording` now writes "Access's" for a name ending in "ss".
  - C2 (the design's third slice): `AccessAndLicensingUnitTestPlacementTest` keeps every Access and Licensing unit test
    in its module's test folder, checks that no file of either context is left in the old folders, and checks that no
    module-map key still takes in an old name. The module map's `RbacIdentityPattern` was retired.
- **Docs, at finalize:** ADR-270 (header "Moved so far", the Access and Licensing rows of Decision 1, "Every edge" rows
  for Access, the OIDC identity provider and Licensing, the new block "Edges measured by packaging Access and
  Licensing", a note on ratchet candidate 2, Decision 6 with the new `Http` adapter folder and the two "today" counts);
  `ARCHITECTURE.md` §4 (row 6, the diagram node and its note, the Access module rules, Licensing module rules and tests
  bullets, the shared-rules bullet, and pointers in the two paragraphs on authorization and licensing);
  `docs/product/architecture/brief.md` (new section "story-6363 s03e package access and licensing" and the module table
  row); the Epic catalogue (`docs/feature/epic-6264-modular-monolith/feature-delta.md`: the S03e table row, the S03*
  notes, KPIs 1 to 3, the Improvement log); the slice-07 brief (criteria ticked, update note). No new term enters the
  glossary: the folders carry the ADR-270 names, and the code keeps `License*` and `Rbac*`.

## Key commits

Range: `git log --oneline --grep="6363\|(access)\|(licensing)" e6ad93130~1..HEAD`.

| Commit | What |
|---|---|
| `e6ad93130`, `c0144301c` | DESIGN and DEVOPS, proposed in AFK mode; the maintainer's answers |
| `ab583c4a0`, `9120798a1` | A1: the architecture rules see `Modules/Access` and `Modules/Licensing` |
| `3fc9b0b1a`, `7b5abd43c` | A2: the membership and layers, measured from the compiled code; the namesake gap closed |
| `7e4c2faa5`, `fabb0ff4d` | A3: the Access and Licensing answers compare |
| `bb96413c7`, `5b508961e` | A4: the CI authentication suites probe |
| `452eb3380` | feature A's whole-candidate review accepted |
| `78ea0da92`, `3f8467a60` | B1: Licensing in `Modules/Licensing`, with its key |
| `905a32d8e`, `58b342439` | B2: Access `Domain` and `Application` |
| `1369855bd`, `834c18d96` | B3: the guards and authentication handlers in `Access/Adapters/Http` |
| `c4e6b5dd3`, `c00d223c7` | B4: the licence test data under `Tests/Modules/Licensing` |
| `19d131189` | feature B's whole-candidate review accepted (pushed) |
| `e72e8f1ec` | follow-up fix: six Playwright specs still built the old licence path |
| `f9ea8c246`, `c74381ada`, `d236efabe` | C1: the Access and Licensing module rules, the shared HTTP adapter rule |
| `40d561073`, `cc3d7def2` | C2: test placement and "no file left", the old module-map key retired |
| `df9e227cc` | feature C's whole-candidate review accepted (local, not yet pushed) |

Pushes: feature A at `cf8d1e33e`, feature B at `19d131189` (the licence-path fix `e72e8f1ec` followed it), and feature C's
first slice in `d236efabe`. Feature C's second slice and its candidate review are committed locally.

## What it achieved

- **Behaviour unchanged by construction.** No route, DTO, scheme name, cookie name, configuration key or entity changed.
  Layout pins fix the public surface of `IRbacAdministrationService`, the guard attributes' shape and the count of
  guarded actions. Existing tests changed only by moves, `using` lines and rule-file strings.
- **Contexts packaged with namespace rules:** 5 → 7 of 11. Files under `Services/Interfaces` + `Services/Implementation`
  320 → 288 (110 + 210 → 100 + 188). `Models` files 262 → 258.
- **Access and Licensing each reach no other context**, except that Access asks Licensing, which is legal by namespace.
  Outside callers are listed by name: Access's Domain 1, Application 5, Http adapters 1 (`Program`); Licensing's
  Application 11 (the readers of ratchet candidate 2), Http adapters none.
- **Three Access services still use the database context directly** (21 listed edges in all, repositories and EF Core
  types included); that list shrinks with the repositories-by-role work.
- **Two new examine commands** exist for the next module move that touches authentication: `access-answers-compare` and
  `ci-auth-suites-show`.

## Quality

| Gate | Result |
|---|---|
| Feature A | Oracle and slice reviews per slice, whole-candidate review accepted (`452eb3380`). Its architecture-test run showed 1 failure of 1346, the path classifier on Windows (see "Knowingly left open"). EXAMINE not run: nwdes refuses it on Windows (it needs no-follow file opens); recorded in the candidate-review commit for the maintainer |
| Feature B | Oracle and slice reviews per slice (B3's first review was rejected for missing gate evidence and accepted on the second; B4's oracle was approved again on its committed bytes, see below), whole-candidate review accepted (`19d131189`). EXAMINE waived on Windows, as for A |
| Feature C | Oracle and slice reviews per slice, whole-candidate review accepted (`df9e227cc`); its recorded test run passed 219 of 219 (the reviewer's selection, not the full suite). EXAMINE waived on Windows, as for A and B |
| Mutation | Backend mutation (Stryker.NET 5.0.0, x64 SDK, sandbox): run 1 with the unit-only filter 72.83 % (748 killed of 1027); run 2 with the two port-level acceptance suites added 79.26 %; after one kill round (13 mutants, 4 new test classes and 4 rows in `LicenseGuardAttributeTest`) **80.53 %**, so the 80 % bar is met. Added by the move: none, since 35 of the 36 moved files are identical to their pre-move version outside `namespace` and `using` lines and the 36th only lost a branch-free record. 90 survivors and 110 uncovered mutants remain: log and message text, ties, and `RbacAdministrationService` / `EmbedSessionTokenService` paths covered only by the excluded API and embed suites. Frontend N/A. Full triage: `docs/feature/story-6363-s03e-package-access-licensing/mutation/results.md` |

No frontend file changed apart from the Playwright specs that built the licence path, so no frontend gate ran.

## Decisions taken without the maintainer, open to revisit

- **D-0 to D-13 at DESIGN** (AFK, propose mode), all on the recommended option, then confirmed by the maintainer's
  answers. Among them: the entities, repositories, seeders and controllers stay (D-1, D-4, D-5); the ASP.NET guards and
  handlers go to `Adapters/Http` of each module (D-3); Access may ask Licensing by namespace (D-7); the licence key moves
  with its verifier (D-6); the rules are by namespace through `ModuleRules` (D-8); the move pushes together, so the live
  suites run once (D-11).
- **The Licensing rules slice was folded into the first slice of feature C** (host, AFK). It had no failing test of its
  own: its adapter rule needs the shared rule that slice adds, and feature B had already renamed the neighbours' lists.
- **`ModuleWording` now writes "'s" after names ending in "ss"** (host, AFK), so a failing rule reads "Access's". The
  crafter was blocked on the old wording; the oracle's own slice was allowed to fix it.
- **The S3 supports were named as files** (host, AFK), so the placement check names the files it judges and not a
  pattern.
- **The oracle of B4 was approved again** (host, AFK). The first approval was recorded while the working copy held CRLF
  line endings; the committed oracle was unchanged.
- **EXAMINE was not run for features A, B and C** (host). nwdes refuses EXAMINE on Windows because it needs no-follow
  file opens (the same refusal as for Story 6650). Feature A recorded it in its candidate-review commit; B and C follow it.
  What EXAMINE would have shown is covered by the whole-candidate reviews, the filtered backend suite and the layout pins.
- **The measured lists differ from the design's guesses**, each difference an edge that exists today. The design
  expected the report commands and queries and the usage-data consent service among Access's Application callers; they
  do not reach Access. The design expected the Domain to hold 7 `Models` candidates; four passed the four conditions.

## Knowingly left open

- **The ServiceNow live instance serves a sign-in page in CI**, so its live suite is not a trustworthy signal on a push.
- **The follow-up Bug for decision 8 of Story 6650** is still to be filed.
- **Windows-only test defects**, not caused by this Story and not fixed in it: the path classifier test
  (`ACodeChangeToTheFetchFingerprint_StillSelectsTheLiveJiraTests…`, a bash path problem), `licenseStatusOneAsker`
  (it builds backslash paths), and `rbacAccess` (it reads an unreadable folder).
- **Non-blocking findings of the oracle and slice reviews, for the next touch:**
  - four rule keys repeat the same composition of the Access and Licensing namespaces (`ModuleRules.AccessModule` and
    `LicensingModule`); one shared constant would end it, and the old alternatives are now retired;
  - the usage text of `access-answers-compare` is pinned by loose patterns, which cannot show the capture reads those
    answers; its eight host environment lines repeat set-up lines; the full two-revision mode fails on Windows because
    the shared compare stage passes a `/tmp` path (the same defect as the Delivery-answers compare);
  - `AccessMarkerAttribute` declares no `AttributeUsage` of its own, so the pin on it reads an inherited framework value;
  - the Licensing placement oracle holds one unused list (`CommittedLicenceFixtures`), and an `AccessAndLicensingFixtures`
    helper namespace still carries the old Licensing name;
  - the "entry deleted from a list" tests of Licensing's Application rule re-scan the IL once per entry and took 18 s in
    one run, against 0.5 to 4 s for their peers;
  - the module-rule key reader keeps only string fields, so a key held as a string array is not read, and one comment
    says a field is read "without its static set-up" though reading it runs the static constructor.
- **Repositories by role (Epic 6507):** the six repositories and the three direct database context users stay with the
  platform (no item filed).
- **Epic 6474 and #6657:** the 11 outside readers of `ILicenseService` (R-L3).
- **S03g, S09a, S12, S04b and S12c:** `VoterIdentityResolver`; the two seeders and `Program.cs` registrations; the
  controllers and DTOs; the entities and wire types.
- **`IlDependencies` still misses catch-clause types, events and named attribute arguments**; no current code hits it.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A for user docs, because nothing a user sees changes. Developer docs updated: ADR-270, `ARCHITECTURE.md` §4, `docs/product/architecture/brief.md`, the Epic catalogue and slice-07 brief |
| Screenshots | N/A, because no screen changed |
| Demo data | N/A, because no data shape or seeded value changed |
| Website assets | N/A, because nothing on the marketing surface changed |
| Clients (CLI, MCP) | N/A, because no route or DTO changed |
| Context map / ARCHITECTURE.md | Done (this change): ADR-270 header, Decision 1 Access and Licensing rows, "Every edge" rows, the new measured-edges block, ratchet candidate 2, Decision 6; `ARCHITECTURE.md` §4 row 6, the diagram node and note, the Access and Licensing rule bullets and the shared-rules bullet. No new term and no new domain event: the three licence events stay target only. UI surface: none |
| Usage-data event | N/A, because nothing a user sees or does changes (DEVOPS, 2026-11-17, in the design record) |
| Mutation | Done, 80.53 % after one kill round, none added by the move: see the Mutation row above and `mutation/results.md` |
| Release notes | N/A; written at /release |
