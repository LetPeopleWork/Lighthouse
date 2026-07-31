# SPIKE Decisions — servicenow-multi-table-work-item-types

**Run**: 2026-07-31, against PDI `dev191338.service-now.com`. Evidence: `findings.md`.

## Assumption Tested

Can one read of a hierarchy-rooted ServiceNow table be restricted to a named set of
`sys_class_name` values — **correctly** (OC-1: does the filter bind as intended alongside the team's
own encoded query?) and **visibly** (OC-2: can a coach tell a correct read from an ACL-truncated
one?). OC-3 (names vs labels) rode along at zero cost.

## Probe Verdict

**WORKS.** D2's model holds: `sys_class_nameIN…` on `task` returns exactly the reference answer —
identical `sys_id` sets, zero extra, zero missing, across four team queries including one carrying
its own `^OR` and one carrying the connector's `ORDERBY`. Both the `^OR` chain and the `IN` form are
correct on this instance; `IN` is chosen on other grounds. D3 confirmed: unfiltered, the same team
reads 579 records of 13 classes instead of 159 of 2.

## Promotion Decision

**DISCARD** — maintainer, 2026-07-31. The findings are the deliverable.

Slice 01 already carries AC-B1..AC-B6 from DISCUSS, so a walking skeleton would be slice 01 minus its
acceptance tests, built outside DISTILL where the tests belong. Probe scripts deleted; the two
behaviours worth keeping become standing guards in
`ServiceNowWorkTrackingConnectorIntegrationTest` during DELIVER (the fixture slice 02 extended rather
than duplicated), not throwaway scripts:

- the OC-2 verdict ladder (`X-Total-Count` > 0 with an empty body = ACL denial),
- metric definitions scoped by class rather than by table.

## Design Implications

1. **Emit `sys_class_nameIN…`, never the `^OR` chain.** Both are correct here; `IN` is one condition
   instead of *2n−1* against the 8192-byte URL cliff `ServiceNowHistoryQuery.RecordsPerBatch` already
   measured, and its correctness does not rest on a grouping rule observed on one instance version.
   Prepend it to the team's query, ahead of `InAStableOrder`'s `ORDERBY`.
2. **AC-B6 is a per-class probe at validation time**, one `sysparm_limit=1` request per named class.
   Header > 0 with an empty body is an ACL denial and the *only* signal there is; header = 0 is
   "empty or misspelt" and cannot be split further by any account that matters. Do not build on
   `sys_db_object`.
3. **Slice 01 must also scope `ServiceNowHistoryQuery.DefinitionQueryFor` to the classes.** A
   `task`-rooted team finds **0** metric definitions and loses slice 04's transition history
   outright. Not in the slice brief's IN scope as written — added below.
4. **The widening detector needs a decision, not an inheritance.** `X-Total-Count` is ACL-blind, and
   for a `task`-rooted team its "everything" probe counts the whole hierarchy. DESIGN says whether
   that probe carries the class filter.
5. **State mapping is the real usability risk, not the class list.** Four classes = 14 labels to map
   by hand; unmapped work silently does not exist.

## Constraints Discovered

- `X-Total-Count` ignores ACLs — it reports what the instance holds, never what the account can read.
  (Pre-existing defect in `ValidateTeamSettings`'s comparison; out of scope here, recorded so it is
  not re-derived.)
- Metric definitions attach to concrete classes only; the base table never has any. `change_request`
  on a stock PDI has no state-tracking definition at all — an instance fact, not a Lighthouse bug.
- A bogus `sys_class_name` narrows to zero rows; it never widens, unlike a bogus *field* name.
- `sys_class_name=task` matches the 30 base-class records, not the 725 in the hierarchy — exact
  match is not hierarchy-inclusive.
- Class **names** (`change_request`), not labels (`Change Request`).

## Next Wave

DESIGN (`nw-solution-architect`), reading `findings.md`. OC-1, OC-2 and OC-3 are closed, so slice 01
is buildable — with implication 3 folded into its scope.
