# Story 5612 — ServiceNow pre-close bucket

**ADO**: User Story [#5612](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5612),
parent Epic [#5513](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5513).
**Delivered**: 2026-08-01, one slice.
**Workspace**: `docs/feature/servicenow-pre-close-bucket/` — retained; this document is the summary,
that directory is the history.

## What it was for

A holding pen. Seven small findings had accumulated from dogfooding the earlier ServiceNow slices,
each too small to justify its own story and none of them safe to close without a decision. The story
existed to force those decisions before the epic closed, not to ship all seven.

DISCUSS produced a verdict per item, and only two became code.

| # | Item | Verdict |
|---|---|---|
| 1 | A work item's id is not clickable — ServiceNow is the only connector leaving `WorkItemBase.Url` unset | **Shipped** |
| 7 | `Type` reads `change_request`, and a coach must *type* `change_request` into Work Item Types | **Shipped** (added during DISCUSS, not in the original bucket) |
| 2 | A declared per-connector capability set instead of ad-hoc advisories | **NOT-NOW**, with a rule-of-three trigger. Two connectors is not a pattern |
| 3 | Query-authoring guidance | **Answered by #5610**, already in flight |
| 5, 6 | Two findings about when a stale-item rule can fire | **Folded into #5627**, whose scope as written could not fire on the case that created it |
| 4 | — | **No-op by decision** |

## The shape of the answer

**Item 1** cost one string. `Url` already ships on `WorkItemBase`, flows through `WorkItemDto`, and is
already rendered as a link by `WorkItemsDialog` when non-null; `sys_id` has been read since #5577 and
`sys_class_name` since #5611. So the deep link is `{instanceUrl}/{sys_class_name}.do?sys_id={sys_id}`,
built from the **record's own class** rather than the team's — the `.do` path is class-specific, so
taking the team's first kind of work would 404 every other row.

**Item 7** is the interesting one, and its design changed twice under contact with reality.

The first instinct was to persist a label alongside the type, or to have the frontend format it. Both
were rejected in favour of **a hardcoded bidirectional map owned by the connector** (ADR-128): 50
class↔label pairs read from `sys_db_object` on the PDI, applied at the connector's own boundary, with
**passthrough on anything it does not know**. No new field, no migration, no frontend change, and
nothing outside the ServiceNow connector knows a mapping happened.

Then two amendments, both from questions asked mid-delivery rather than from review:

- **`KindOfWork` returns the words *this team* used**, not a globally-chosen label. The design had
  specified a save-time normalisation step to keep config and data in step; that step had no home
  (`SyncTeamWithTeamSettings` is connector-agnostic), was bypassable by the API, the CLI and the MCP
  server, and mutated what the coach typed. Reporting each record in its own team's vocabulary makes
  config and data agree **by construction** instead. The normalisation decision was deleted outright.
- **A class name is recognised in any case.** See the dogfood section — this one was a live silent
  zero.

## Decisions worth keeping

| | Decision | Why |
|---|---|---|
| ADR-128 | The class↔label map is connector-local and passes unknown names through unchanged | A free `display_value` would give a custom class a pretty label while its config kept the class name, and `GetCreatedItemsForTeam` compares the two. A field that desynchronises is worse than no field |
| ADR-128 am. 1 | `KindOfWork` returns `scope.AsTyped(...)`, the team's own wording | Agreement by construction beats a save-time step with no home and three ways around it |
| ADR-128 am. 2 | `ClassFor` folds the case of a class name | `IN` matches case-insensitively, so a wrong-case class name syncs *successfully* and then diverges |
| — | The deep link is built from the record's class, not the team's | One team reads several classes; the `.do` path differs per row |
| ADR-123 D10 | A working connection says it works and stops | Withdrawn advisory: true, but unactionable where no team exists yet |
| — | The advisory channel is deleted, not parked | Zero callers on both stacks. Re-adding two nullable fields later is cheaper than carrying a dead cross-stack contract |

ADRs amended in place: **ADR-128** (new, then amended twice), **ADR-123** (decision 10's advisory
withdrawn), **ADR-118** (the D5 contract change withdrawn with it).

## What the dogfood measured

Run against PDI `dev191338` on 2026-08-01, with the app built from this branch. Do not re-derive:

- **`sysparm_query`'s `IN` matches a value case-insensitively.** `sys_class_nameINChange_Request` and
  `sys_class_nameINchange_request` return identical rows, and every row says `change_request`. This is
  why `ClassFor` folds case: a team configured `Change_Request` queried fine, got rows, and stored
  `change_request` against a config that said otherwise — a silent zero reached by a typo. Settles
  **OC-8**, raised as review finding F2 and accepted pending exactly this measurement.
- **`{class}.do` is universal across the ITSM classes.** A team reading `Incident, Change Request,
  Problem, Catalog Task` opened a record of each from the Work Items dialog. No 404. Settles **OC-2**,
  which had been measured for `incident` only.
- **Either form works in Work Item Types.** `Change Request` and `change_request` both sync, and the
  Type column reads back whichever was typed.

The dogfood also produced two findings the gates had all missed: the connection-validation advisory
(removed, above) and **Bug #5628**.

## Bug #5628 — found here, not fixed here

Team settings **auto-save bypasses connector validation entirely.** `useModifySettings.ts` validates
in `handleSave` (:336) but the autosave effect (:249-264) debounces straight into `saveSettings`;
`ModifyTeamSettings.tsx` enables autosave and never destructures `handleSave`, so
`validateTeamSettings` is passed into the hook and is unreachable. Connector validation therefore only
runs in the **create** wizards.

For ServiceNow that means the whole ADR-124 probe ladder — built precisely to stop a mistyped work
item type from silently selecting zero rows — is bypassed on the page where a coach is most likely to
mistype. It affects every connector and both settings pages. Filed standalone, outside this epic,
because the fix is not obvious: validating per debounce tick would fire a live probe per named kind of
work.

## Work completed

18 commits, `8f30994fe..f64a8bba9`. Full chain DISCUSS → DESIGN → DISTILL → four-reviewer gate →
DELIVER → dogfood → review → refactor → mutation in one day.

The post-implementation review is worth noting because the four-reviewer gate ran at DISTILL, before
any implementation existed. It found six things; three were behaviour-preserving and became a refactor
commit, and three needed decisions:

- **F1** — the blank-class fallback bypassed ADR-128, so a team that typed `Task` would get `task` on
  exactly those rows. Fixed; verified red-before-green.
- **F2** — became the `IN` measurement above.
- **F3** — `LabelFor` had no production caller once amendment 1 landed. Deleted rather than kept for
  the picker that might one day want it.

## Quality gates

| Gate | Result |
|---|---|
| Backend mutation | **85.91 %** (367 killed / 58 survived / 5 timeout, 430 tested, 10 m 20 s) |
| Frontend mutation | **N/A** — zero frontend files changed by the slice (DD-6) |
| `dotnet build` | clean, zero warnings |
| Backend tests | 328 ServiceNow green; 710 green across connector + validation after the advisory removal |
| `pnpm build` | clean (implies Biome clean via `prebuild`) |
| Frontend tests | 3815 / 3815 across 286 files |
| SonarCloud | **not yet run** — nothing pushed |

Mutation record and configs: `docs/feature/servicenow-pre-close-bucket/mutation/`.

Two hypotheses about a *failed* first mutation run were disproved and are recorded there so they are
not re-derived: the acceptance test does slip past `!~IntegrationTest`, but it predates a clean run
with the identical filter; and the `mutate` globs do bind — `13 707 mutants created` is a pre-filter
count, `430 will be tested` is the scope.

## Definition of Done — item by item

| # | Item | Status |
|---|---|---|
| 1 | Every bucket item has an explicit verdict, including "no" | Met — table above; no silent N/A |
| 2 | Items 1 and 7 green end to end | Met |
| 3 | Mutation ≥ 80 % on each changed stack | Met — 85.91 % backend; frontend N/A and declared |
| 4 | Both builds warning-free; no new Sonar issues | **Partly** — builds clean, Sonar unrun until push |
| 5 | Verified against a real instance | Met — PDI `dev191338`, all four checks above |
| 6 | Docs updated | **NOT met — carried to #5578**, same reason as #5611: there is no public ServiceNow page. What this story owes it: the deep link, that either form is accepted, and that #5611's "class names not labels" guidance is now wrong |
| 7 | A dogfood moment the same day | Met — produced the advisory removal, F2's answer and Bug #5628 |
| 8 | ADO transitioned; Release Notes tag decided | **Open** — maintainer's call |

## Finalization checklist

- **Docs prose** — deferred to #5578 by the epic's plan. See DoD 6.
- **Screenshots** — none. The Work Items dialog now renders the id as a link, which is a visible
  change, but there is no page to host a shot. Travels with the docs to #5578.
- **Demo data** — untouched. The demo seeder already produces the classes this slice reads.
- **Website marketing surface** — untouched. No asset under `docs/assets/` added, renamed or deleted,
  so nothing letpeople.work hot-links via jsDelivr is affected.
- **Lighthouse-Clients (CLI / MCP)** — no client-facing contract changed. `Url` was already on the
  work item payload and `Type` keeps its type; the advisory fields removed from
  `ConnectionValidationResult` were never surfaced by a client. No version bump prepared — worth a
  maintainer confirmation before release, since it asserts something about another repo.
- **RBAC** — unchanged. No endpoint, permission or gating surface added.

## Known follow-ups

- **Bug #5628** — the auto-save validation bypass, above. Needs its own DISCUSS.
- **OC-4** — #5610's board picker should pre-fill the **label**. Held open deliberately; trigger is
  #5610 finishing DELIVER on `main`. Sharper since the normalisation step was deleted: nothing
  canonicalises a class name afterwards, so a picker-configured team reads `change_request` in its
  Type column permanently.
- **ADR-127** still asserts the premise DISCUSS disproved about #5627's scope. Belongs to whoever
  picks up #5627.
- **A custom class typed in the wrong case still diverges** — passthrough cannot canonicalise a name
  the map has never seen. Inherent, recorded in ADR-128.
- **The `Link`-header walk** at `ServiceNowWorkTrackingConnector.cs:838-927` is the densest untested
  cluster left in the connector and has never been anyone's slice.
- **Item 2** (declared capability set) is NOT-NOW with a rule-of-three trigger, and the advisory
  channel it would have used is now deleted — a deliberate trade recorded in ADR-118.

## Related

- `docs/evolution/2026-08-01-story-5611-servicenow-record-classes.md` — whose "document class names,
  not labels" conclusion this story reverses
- `docs/evolution/2026-08-01-bug-5621-servicenow-span-dates-and-sort.md`
- ADR-118, ADR-123, ADR-124, ADR-128 under `docs/product/architecture/`
