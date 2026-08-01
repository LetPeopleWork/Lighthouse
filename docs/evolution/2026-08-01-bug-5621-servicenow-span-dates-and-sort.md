# Bug 5621 — ServiceNow span-to-date dates, and a sort that was not total

**ADO**: Bug [#5621](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5621), parent Epic
[#5513](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5513).
**Delivered**: 2026-08-01. **Commits**: `989e5539a` … `94a4498dc` on `main` (11).
**Workspace**: `docs/feature/fix-servicenow-span-dates-and-sort/` — retained; this document is the
summary, that directory holds the RCA and the mutation evidence.

## What it was for

A code review of Story #5611 at `67637ce76`, not a field report. Nothing ServiceNow has ever been
released, so every defect below was on `main` and in nobody's hands — the whole point of fixing them
now rather than after the connector launches.

Five findings were filed. Four were fixed, one parked, and a sixth was found during triage.

## The findings, and what each turned out to be

**F1 — a record whose only spans do not measure state lost both dates, silently.** The connector asked
"does this record have *any* span?"; the mapper asked "does it have a *state* span?". Those are
different questions, and a record between them got `null` for `StartedDate` and `ClosedDate` while its
`opened_at` and `closed_at` sat in the answer already in hand. Not exotic:
`ServiceNowHistoryQuery.DefinitionQueryFor` filters on table and type only — it cannot do better,
because the state field is named differently on every record class — and the stock `incident` table
answers with **four** `field_value_duration` definitions, of which three measure `assigned_to`,
`assignment_group` and `active`. So an incident closed before the state definition was activated,
whose group somebody changed during a later queue cleanup, had exactly one span and no state span. It
was Done with a null `ClosedDate` and dropped out of Throughput and Cycle Time — while the identical
record nobody re-assigned synced correctly, because it had *no* spans and reached the fallback.
**Whether an item carried dates depended on whether an unrelated field had changed.**

Fixed by filtering to team-recognised state labels where spans enter the by-record dictionary, so
absence means what every caller already read it as.

**F2 — dates came from individual spans, not from category crossings.** Filed as "`WhenWorkFinished`
uses `FindLast`". Triage found the defect was broader and the fix already existed twice in the
codebase: Jira (`IssueFactory`) and Azure DevOps both date work from a crossing *into* a category
*from outside it*, taking the latest such crossing. ServiceNow had reimplemented the idea from
scratch and diverged three ways — the filed one, plus a start rule that counted from an abandoned
attempt after work returned to the queue, plus two missing rules (the queue-return guard, and
`started = closed` when a record finished without ever being seen in Doing). That last one is why F2
was more than a date being off: a desk that resolves straight out of the queue produced a null start,
and the item left Cycle Time entirely where Jira and ADO keep it with a zero-length cycle.

**F3 — the `sys_id` tie-breaker was skipped whenever a team wrote its own `ORDERBY`.** `InAStableOrder`
early-returned, so `95e8a9d39`'s fix for non-total sorts never reached those teams: pages overlap,
`GuardAgainstRepeatedRecords` throws, and the whole team's sync aborts once it outgrows one page. The
population is larger than "a coach who hand-writes an ORDERBY" — **ServiceNow's own *Copy query*
emits the list's current sort**, and that is the path #5610 D2 assigns to #5578's docs. One line: the
early return is gone, because encoded queries chain `ORDERBY` terms, so appending keeps the coach's
order primary *and* makes it total.

**F4 — a 200 carrying no record set threw instead of downgrading.** Both history reads pass
`WhenRefused.Downgrade` precisely so a degraded instance degrades the sync; the escape hatch tested
the status code alone, so the SSO sign-in page ADR-114 exists for walked past it into `RecordsFrom`
and took the team's whole sync down. Now conditioned on "did this answer carry a readable record set".

**F5 — the paging slack is frozen from page 1. Parked, deliberately.** The failure is loud
(`PagingDidNotTerminate`), the limit is documented, and the honest next step is a live probe — does a
real instance emit `rel=next` past its own reported count? Guessing at a larger constant is not a fix.

**F6 — found during triage, not in the original filing: the availability verdict was an aggregate
count.** Definitions attach to concrete record classes and never to a base table (ADR-123 D9), so a
team naming `incident` and `change_request` needs one on each — and a single count above zero reported
`Available` for a team where only the first was configured, leaving every change_request with no
dates and no warning. This needs no misconfiguration at all.

## Decisions worth keeping

**The category-crossing rule now has one home.** It had been written twice, the copies agreed, and
nobody noticed it was duplicated *knowledge* until a third connector diverged on three points at once
— none catchable, because each connector only ever tested its own copy. `WorkItemCategoryCrossing`
holds it; Jira and Azure DevOps were re-pointed at it with behaviour verified unchanged, and their
differing input shapes were deliberately left alone (ADO's revision walk seeds an empty previous state
and its first revision must keep counting as an arrival, which `GetAllStateTransitionsThrottled` would
have dropped).

**"Available" cannot be answered from the definition rows.** Whether a `field_value_duration`
definition measures *state* is unknowable from the row — `incident_state` on incident, `state` on
change_request — so a `field=` filter would drop the stock definition rather than the noise. It is
settled by what came back instead: a span read that succeeds while returning nothing the team
recognises has not measured state, whatever the definitions claimed.

**The verdict must not suppress the read.** The first version of F6's fix skipped the span read
entirely when coverage was partial, which cost the *measured* class its true dates — trading a silent
half-team defect for a loud whole-team accuracy regression. Separating "what the administrator is
told" from "whether we bother reading" is safe because `WithSyncDeltaTransition` **appends** a
synthetic transition rather than replacing the real ones.

**Absence of evidence is not evidence of absence — twice.** Once per record (F1's fallback), and once
per team: an empty span read is what a correctly configured instance answers when nothing has moved
since the definition was switched on, which is exactly the state an administrator is in on the sync
right after they act on Lighthouse's own warning. Downgrading on it would tell them to activate a
definition they had just activated.

**A deliberate divergence from Jira and ADO, recorded because the commit subject does not convey it.**
ServiceNow drops unmapped spans *before* pairing, so a detour through a state the team never mapped
joins the spans either side and re-dates nothing. Jira and ADO see the raw label and would re-date.
The ServiceNow answer is the one consistent with "an unmapped state does not exist" — the occupancy
either side reads as continuous — but the rule is the same; only what the connectors are shown
differs.

## Evidence

- **Tests**: 4260 green, 0 warnings. 32 tests added.
- **Mutation** (`docs/feature/fix-servicenow-span-dates-and-sort/mutation/results.md`): backend
  **88.11 %** over the six changed files against an 80 % gate. Frontend **N/A — zero frontend files
  changed**, independently confirmed by CI's paths filter skipping both the frontend and E2E jobs.
  `AzureDevOpsWorkTrackingConnector` is outside the mutate set (27 changed lines in 1111, and
  Stryker.NET cannot scope to a line range); its change is a pure delegation to
  `WorkItemCategoryCrossing`, which is mutated and directly tested.
- **Review**: returned NEEDS_REVISION with two blocking issues, both real — the verdict suppressing
  the span read, and the evidence downgrade conflating "no rows" with "no state rows". Both fixed in
  `87035ad18` before the push.
- **CI**: run `30691199133` green, both Sonar gates OK.

## Lessons

**The first mutation run scored 86.05 % and still hid a rule with no coverage at all.** The block that
drops the start date on a queue return came back `NoCoverage` — the rule was reachable only through
the pure mapper's tests and never through a sync. A passing gate is not the signal; the survivor list
is. Six survivors sat in logic this fix wrote, and closing them took the score to 88.11 %.

**CA1859 recurred through a pure refactor for the first time** (`docs/ci-learnings.md`, Recurrence 7).
Extracting a private helper out of a public method carried the public method's interface return type
with it — right for the API, wrong for the extractee. Local build was clean at 0 warnings; the rule is
INFO locally and only ever fails the Sonar gate.

**Two of the three defects were pre-existing patterns that #5611 extended rather than introduced.**
The `StartedDate` half of F1 dates to slice 04; #5611 extended it to `ClosedDate`. Reviewing a story
that extends an existing shape means reviewing the shape.

## Related

- [ADR-117](../product/architecture/adr-117-servicenow-started-and-closed-dates-without-itil.md) and
  [ADR-118](../product/architecture/adr-118-servicenow-transition-history-from-metric-instance-spans.md)
  — both amended 2026-08-01 by this fix.
- [Story 5611 evolution](./2026-08-01-story-5611-servicenow-record-classes.md) — the story this review
  came out of.
- `/mutation-testing` — written during this fix, capturing the Stryker traps it rediscovered.
