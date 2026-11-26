# DIVERGE request: story-6564-report-notes

ADO Story #6564 "Add notes to reports". No parent Epic.

## The requester's words

source: requester-verbatim (maintainer, chat turn 2026-11-26)

> Idea: similar to deliveries, I'd like to bea ble to add notes to reports. if auth is on, we should identify who wrote it. Notes should be visible in the "live report" in lighthouse, i think (for nwo) we keep them out of the print to pdf

source: requester-verbatim (maintainer, ADO Story #6564 description)

> Numbers in a report can be misread without context. Add notes to reports, the same way we have them on Deliveries (5639). This should be a generic concept for all report types, not something built for Then & Now only.
>
> Use cases from the workshop
> - Jessie: show how large the Team is, because a Team of three and a Team of ten have very different throughput.
> - Peter: record what happened, for example a change that started or a training that took place on a date.
>
> Open
> - Should notes be part of the print and PDF version? Probably yes, as those readers have the least context.
>
> Source: Evri Lighthouse workshop, 2026-11-06. Part 1, 2:06:41 to 2:07:31.

## DISCOVER result

source: host-summary

- Job: `docs/product/jobs.yaml`, `job-flow-coach-keep-the-context-with-the-numbers`. VALIDATED at low-to-medium
  confidence: two informants from one organisation, at one workshop.
- C1: Jessie's case (Team size) may be better served by a structured value than by free text. Lighthouse holds
  no Team size today (`Lighthouse.Backend/Lighthouse.Backend/Models/Team.cs`).
- C2: Team size is context that always applies; Peter's case belongs to a date. One note concept may or may not
  serve both.
- Prior art: Delivery notes (#5639), `Lighthouse.Backend/Lighthouse.Backend/Models/DeliveryNote.cs`. Plain text,
  newest first, dated, author nullable when authentication is off, only the author edits, notes are frozen
  when the Delivery is archived.
- Scope from the requester: keep it short. 3 to 5 directions, scored, one recommendation.
