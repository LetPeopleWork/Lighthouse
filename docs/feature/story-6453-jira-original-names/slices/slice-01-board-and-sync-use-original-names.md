# Slice 01 — Load from board and sync use the original names

**Goal:** a German-token Jira Team loads, saves and refreshes with the same original status and type names as an
English one.

**IN:** board load sorted on `statusCategory.key`, `untranslatedName` (fallback `name`), de-duplicated; board work
item types as original names (Cloud and unidentified deployments read the type list, an identified Data Center keeps the
payload name); the status/type ID → original name lookup used on refresh for current state and type, with one re-read
of both lists on an unknown id and a visible failure after that (MD-14, DD-12); the lists also read when `serverInfo`
fails (unidentified deployment); the unknown-id and Team-only kept-state copy (MD-13, MD-14); the JQL Lighthouse builds
is unchanged and checked live to select the same items under both languages; live German-account test.

**OUT:** suggestions (slice 03); the one-full-refresh-after-upgrade step (slice 02, DD-11, separately droppable);
migration of stored names (MD-3); link types (MD-4).

**Learning hypothesis:** disproves "Jira resolves original names in JQL and by ID for a translated token" if the
German account selects fewer items or other states than the English one. Confirms the lookup approach if identical.

**Acceptance criteria:** AC-1.1 … AC-1.11 in `feature-delta.md`.

**Dependencies:** maintainer switches `benjamin@letpeople.work` to German (done). JUSTATEST workflow change made by the
maintainer 2026-10-31 (status 10155 in the workflow and on board 1, JUSTATEST-14 in it): prerequisite of the live test.

**Effort:** about 2-3 days (was ≤1 day at DISCUSS). DESIGN and DISTILL grew it: connector lookup with memo and one
re-read, four verdict factories, board load changes, Unknown-deployment routing, and a fixture blast radius across about
a dozen Jira test classes. Slice 02 (DD-11, about half a day) is not included. **Reference class:** story-6398
connector slices.

**Pre-slice spike:** done. DESIGN live probes (2026-10-31): JQL by original name for both `status` and `issuetype`
(equal counts under both accounts), list-route scope and `untranslatedName` on `rest/api/latest/status` and `/issuetype`.
