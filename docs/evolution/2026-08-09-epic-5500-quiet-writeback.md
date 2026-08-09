# Making write-back stop shouting — Epic 5500

Shipped 2026-08-08 → 2026-08-09 in three slices and one spike, plus two slices that were designed and
then **removed on evidence**. Premium.

The complaint was concrete: every value Lighthouse wrote into Jira emailed everyone watching the work
item, so teams asked their administrator to switch Data Sync off. The fix looks like a one-line query
parameter. It is not, and the spike is the reason this epic has a spine.

## What shipped

| Slice | ADO | What it added |
| --- | --- | --- |
| 01 | 5502 | Write-back intents are collected during an update execution and flushed **once**, instead of one pass per refresh stage. Measured 3 connector calls → 1. |
| 02 | 5503 | **One call per work item** on both connectors — a multi-key `fields` object on Jira, a multi-operation patch on Azure DevOps — with an unbatched retry so one bad mapping cannot take the item down. |
| 03 | — | SPIKE. Nine questions against real Jira Cloud. No ship. |
| 04 | 5505 | `?notifyUsers=false` on every Jira write, a 403 retry that guarantees the write still lands, and one Warning per connection per flush naming the projects that could not be silenced. |
| ~~05~~ | ~~5506~~ | **Removed 2026-08-09** — folded into Epic 5511 (Task Manager). |
| ~~06~~ | ~~5507~~ | **Removed 2026-08-08** — its least-privilege premise was disproved by the spike. |

ADRs **142-145**, of which 145 is Superseded without ever being built.

## The spike inverted the slice it was meant to confirm

Atlassian's own evidence conflicted: the Cloud docs say an under-permissioned `notifyUsers=false` is
**silently ignored** (204, watchers mailed anyway), while a community report says it is a hard error. Those
demand opposite designs, so slice 03 went and measured it.

The docs are wrong. Jira answers **403, and drops the entire field update** — verified on `SPIKEPRM-1`,
whose `duedate` stayed `null`.

That inverted slice 04's premise. Its brief had assumed the worst case was "silent ignore", in which
shipping was "strictly better than today". The real worst case is worse than today: every customer whose
credential lacks `Administer Jira` or `Administer Projects` would have gone from **noisy-but-working**
write-back to **no write-back at all**. Always-on suppression, as designed, was a regression.

The fix is optimistic: send suppressed, and on 403 re-send the identical payload once without the
parameter. Write-back can then never regress — it can only get quieter.

## The distinction that took a reviewer to find

A 403 does not mean "suppression refused". Jira answers exactly the same way when the credential cannot
edit the work item at all, or cannot see it. Reporting that as a notification problem would send an
administrator to grant `Administer Projects` for a permission that was never at fault.

So the **retry's outcome is the diagnosis**, never the 403:

- retry succeeds → the 403 *was* about suppression → `NotSuppressed`, feeds the Warning;
- retry fails → it was not → `Unknown`, a plain write failure, and **nothing** in the Warning.

That rule generalised into the enum both connectors now report on every result: an attempt that landed is
`Suppressed`, one that failed is `Unknown`, one never made is `NotApplicable`. Without fixing it once, each
connector could have satisfied its own tests with a different reading and the rollup would have quietly
mixed them.

## Where the retry lives is the whole design

Inside `TryWriteFields`, not `UpdateIssue`. That single placement makes two degradations compose without
either knowing about the other:

- a **403** drops the suppression and keeps the batch;
- any **other** rejection drops the batch and keeps the suppression;
- a batch that hits **both** still isolates its good fields, because each per-field re-send is itself a
  fresh attempt that asks for silence first.

Neither ADR spells that third case out. It is pinned by two specifications so it cannot rot.

## Three claims the spike killed, one of which nearly reached the release notes

- **"Batching fields means fewer emails."** Void. Jira Cloud batches watcher mail per (recipient, work
  item) over roughly ten minutes, so one call and four calls both produced exactly **one** email. Slice
  02's value is API-call reduction and a measured **4:1** drop in history churn — nothing else. The email
  claim must not appear in docs or release notes.
- **"N+2 write-back passes per refresh round."** Overstated. `UpdateQueueService` coalesces duplicate
  `(Forecasts, portfolioId)` triggers, so N teams yield at most two forecast executions — the real
  portfolio-level count was about four, not N+2.
- **"Cloud bulk edit needs a lower permission."** Disproved: `sendBulkNotification:false` needs
  admin/project-admin exactly like the per-issue path. That was the entire stated point of slice 06, so
  slice 06 died with it.

## Two slices removed, both because a measurement changed the answer

Slice 06 went first, on the spike's Q5. Slice 05 went last, on judgement: it was a capability interface, a
dedicated endpoint, a per-project fan-out with a latency budget and a concurrency cap, and a degraded
"could not check" state — to answer a question slice 04's Warning already answers after the fact, the docs
answer before it, and the team's complaints answer regardless. ADR-142's retry makes the failure benign, so
late discovery costs emails, not data.

Nothing was orphaned: ADR-145's per-project verdict and its project-key derivation both shipped inside
slice 04's Warning. Epic 5511 inherits two constraints — deduplicate by connection, and keep
`NotSuppressed` apart from `Unknown`.

## What the mutation runs found

Scoped kill rate **98.39 %** on slice 04's methods (gate is 80 %), reached in three passes from 59.21 %.
Both jumps were real gaps, not accounting:

- **The project-key derivation was barely tested.** The Warning named `PROJ` whether the derivation worked
  or not — because `unknown project (item PROJ-1)` contains `PROJ` too. Seven reference shapes now pin it,
  including `PROJ-`, `PROJ-1a` and `-1`.
- **A test added in the first pass was vacuous.** `Is.All.EqualTo(...)` over a list the statement mutant
  had emptied passes. It looked green while the mutant survived — exactly the failure mutation testing
  exists to catch.
- **Slice 02's "Azure DevOps cannot usefully be mutated" is wrong.** `UpdateItem` takes the
  `WorkItemTrackingHttpClient` as a parameter, so a mocked client reaches the whole fallback path. Its
  mutants are killed now, not waived.

The Stryker traps hold and are worth not re-deriving: the score's denominator includes `NoCoverage`, which
appears in neither the "will be tested" count nor the survivor list; and Stryker.NET still cannot scope to
a line range, so the whole-file figure (12.44 % across three large files) is a true statement about the
files and a useless one about the slice.

## The test fixture that emailed a real person

The live evidence for the 403 path needs a **second Jira identity** — the CI credential is site admin and
can always suppress, so it can never reach the branch. The first version of that fixture followed the
house create-and-delete pattern, and both ends of it were wrong for this credential:

- it has **no Delete Issues** in `SPIKEPRM`, so teardown failed silently and every run leaked a work item;
- the project auto-assigns to its lead, so every create notified somebody who was not the actor — a
  notification this credential **cannot** suppress, that being the entire reason it exists.

It now reuses one fixed work item, self-assigned in setup, so actor, reporter and sole watcher are the same
account and Jira does not mail you your own changes. The general lesson: a fixture running as a
deliberately under-permissioned identity cannot borrow the conventions written for the privileged one.

## Gates

- CI green on `main` @ `f004eda68` (run 31309637395) — every substantive job including `sonar-gates`;
  the pending jobs are release gates that fire on tag.
- Full offline suite **4429 passed, 0 failed**, zero build warnings. Live: **98** Jira and **104** Azure
  DevOps integration tests, including the restricted-identity fixture.
- Mutation 98.39 % scoped (`mutation/results-5505.md`); slice 01 81.62 %, slice 02 86.96 %.
- Review gates: `nw-acceptance-designer-reviewer` on DISTILL and `nw-software-crafter-reviewer` on
  DELIVER, both approved with zero blockers. The DELIVER review independently concluded the two
  connectors' visibly parallel shapes must **not** be extracted — the structure repeats, the knowledge
  does not.
- ADO 5502 / 5503 / 5505 Closed, 5506 / 5507 Removed.

## Still open

- **Data Center is unverified.** No instance was obtainable before release. Atlassian documents the same
  permission requirement and the retry protects the write under any of the three possible behaviours, but
  the post-release checklist (spike Q1 / Q2 / Q10) stands at the end of
  `slices/slice-03-spike-jira-notification-suppression.md`.
- **String-typed custom-field write-back is unverified** — the test site has no plain-text custom field.
- **Epic 5511** carries the durable surface for suppression refusals.
- `SPIKEPRM` and its work item `SPIKEPRM-4` must survive: it is the only project on the test site whose
  scheme does not grant `Administer Projects` to every licensed user, and therefore the only way to reach
  a 403 at all.
