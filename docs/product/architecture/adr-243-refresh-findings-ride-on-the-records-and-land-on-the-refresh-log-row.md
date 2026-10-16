# ADR-243: Refresh findings ride on the records and land on the refresh log row

**Status**: Accepted (maintainer, 2026-10-16, DESIGN pass 2; both the transport and the store as recommended).
**Feature**: `epic-6303-configure-once-per-connection` (Stories #6300 slice 02, #6312 slices 03–04)
**Extends**: [ADR-238](./adr-238-several-link-candidates-are-placed-by-the-oldest-link-in-the-work-item-service.md)'s
path from the record to `WorkItemService` to `SyncOutcome` to `RefreshLog`.

## Context

The maintainer fixed Refresh History (Settings → System Info) as the surface for a refresh's dependency findings (D5a):
a named link nothing carries (with the link names seen), a name that points both ways (with both phrases or ends), an
ADO relation type not yet read (slice 03 alone), a relation type named where it cannot be read. Each is at most one per
refresh, whole-query fetch only, and detail also goes to the log as one line.

Today these warnings exist only as log lines inside the Jira connector (`JiraWorkTrackingConnector.cs:1275-1309`).
`RefreshLog` carries only numbers (`RefreshLog.cs`), and Refresh History shows per-entity figures over a window
(`RefreshHistorySection.tsx:176-276`). Story 6301 already carries per-refresh facts out of a connector by putting the
evidence on the records it returns and deciding in `WorkItemService` (`WorkItemService.cs:183-228`). Connectors are
leaf adapters; ADR-183's interface header warns against making them reach into the pipeline's ambient state.

## Decision

1. **Evidence on the records.** The connector puts, on each Feature it returns, a transient value (not mapped, carried
   by the copy constructor): what the Dependency Field resolved to, and the link names or `Rel` values the record
   carries.
2. **Decided once per fetch in `WorkItemService`**, beside the 6301 placement step: at most one finding per kind per
   whole-query fetch, the same "silent when anything matched, silent when nothing is linked" rules as today, and one
   aggregated log line.
3. **Carried by `SyncOutcome`** as an `init` member (the 6301 precedent), written by the updaters.
4. **Stored as one nullable text column on `RefreshLog`**: a list of findings, each with a closed `kind`, the setting,
   what was named and what was seen. Tracker values stay literal; the browser builds the sentence in the configured
   terms.
5. **Shown as the findings of the selected entity's latest whole-query run** under its figures in Refresh History,
   labelled with that run's time. A delta run (`RefreshLog.Mode == SyncMode.Delta`) reads only moved records, so it
   assesses nothing and writes no findings; the read takes the latest row with `Mode == Full`, so a delta refresh can
   never blank a standing warning. The backend picks that row (per entity, whatever window the section shows, so a
   full run older than the window still counts) and sends its findings with its time; the browser does not search
   the rows itself. The run's time tells the reader how old the finding is. The frontend model's member
   is optional, so an older backend's payload still renders. (Recommended option, taken while the maintainer was AFK.)

## Alternatives considered

- **A richer return type from the connector**: five connectors and both fetch overloads change for a value only two
  produce.
- **An injected per-refresh recorder** the connector writes to: an ambient side channel into a leaf adapter.
- **Typed columns per finding kind**: a column per kind, and AC-3.10 can raise several findings in one refresh.
- **A child table**: one more read path for a section that shows the latest run.
- **The latest run, whatever its mode, with delta runs writing "not assessed"**: the section would show "not assessed"
  after most refreshes and hide a warning that still holds.
- **The latest run, whatever its mode** (pass 1): a delta refresh blanks the warnings.
- **Counts only**: cannot name the phrase that was looked for or the ones that were seen, which is what the admin acts
  on (AC-1.3, AC-2.1).

## Consequences

**Positive**
- One path for refresh findings, reused by the next feature that has one.
- No connector interface change.

**Negative / accepted**
- Free text from the tracker (link names) is stored on the row. It is the tracker's own configuration, already logged;
  it is never sent as usage data.
- A cheap refresh reads only moved records, so its findings say nothing; the section reads the latest *whole-query*
  run, which can be older than the latest refresh. A fix made in the tracker shows once the next full download runs.

## Enforcement

- Logger-capturing and `RefreshLog` tests: one finding per kind per fetch however many Features; none on a delta fetch;
  none when anything matched.
- Frontend: the section renders findings only when present; the model's member is optional.
- Acceptance: a full run with a finding, then a delta run → Refresh History still shows the full run's finding with the
  full run's time; a later full run with none → the finding is gone.

## Cross-reference

- [ADR-238](./adr-238-several-link-candidates-are-placed-by-the-oldest-link-in-the-work-item-service.md).
- [ADR-242](./adr-242-a-dependency-field-may-name-a-link-type-read-as-a-third-source-of-the-dependency-selector.md).
