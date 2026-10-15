# Competitive Research — story-6307-refresh-on-additional-fields-change

DIVERGE wave · lightweight depth · 2026-10-15. Done by Flux directly with WebSearch/WebFetch (the research
question is narrow; no `nw-researcher` dispatch). Job under study: *after a field is added to how a tool reads
a source system, get the already-synced records to carry it — or say they do not yet.*

Confidence labels: **Doc** = vendor documentation read; **Search** = vendor material surfaced via search
summary, not read in full; **Dated** = source older than two years.

## Comparable tools (same category: flow analytics on top of a work tracker)

### 1. ActionableAgile Analytics (55 Degrees) — manual "Reload"

- **Behaviour.** Opening a project from *Recent Projects* uses cached browser data, "to reduce server load and
  decrease the wait time". New projects, boards or filters require the user to "hit the 'Reload' button at the
  top right"; structural changes call for a fresh load from *All Projects*. Additional Jira fields are chosen in
  an "Additional fields" step of the load dialog. (Doc — FAQ "Do I have to reload my data every time?";
  Search — "Loading Data in Jira Cloud".) A 2024-11-27 Cloud release note fixes "adding a field into a shared
  data set did not persist when reloading" (Search).
- **Serves the job well.** Field choice and data load are the same dialog, so a newly picked field is loaded in
  the same act.
- **Fails the job.** Outside that dialog, freshness is the user's problem; the FAQ is literally a page
  explaining when you must remember to reload.
- **Assumption about the user.** The user knows when their configuration invalidated the cache.

### 2. Screenful — next scheduled sync, or "sync now"

- **Behaviour.** "Once you add a custom field to a board or project, it is imported to your dashboard during the
  next data sync (the data is synced once per hour and you can trigger sync manually anytime at the
  integration settings)." (Search, Dated — Screenful blog, 2018.)
- **Serves the job well.** A short default interval (1 h) bounds the gap; a manual sync is one click, at the
  place the field was configured.
- **Fails the job.** Nothing tells the user that the gap exists for *this* field right now; they rely on the
  sentence in the blog.
- **Assumption.** An hour is short enough not to matter. Lighthouse's equivalent today is up to ~4 h (E7 in
  job-analysis.md).

### 3. Lighthouse itself (internal prior art)

- Team/Portfolio settings save → immediate refresh of that one owner (frontend calls the update endpoint).
- ADR-030: cheap dependent reload → automatic; expensive → one in-place click; never "go elsewhere and refresh".
- ADR-140: fingerprint makes the next cycle full; connection-level invalidation is passive, by design.
- "Update All" header button: premium + SystemAdmin, all owners, no connection scope.

## Non-obvious alternatives (different category, same job)

### 4. Airbyte (ELT data pipelines) — opt-in automatic backfill of the stream

- **Behaviour.** When a new column is detected, the column is created and "values for the field will be filled
  in for the updated rows"; rows that were not updated stay empty unless the user runs a manual refresh or
  enables **"Backfill new or renamed columns"**, which re-syncs the whole stream when a new column appears.
  Propagation is a per-connection policy: *propagate field changes only*, *propagate all*, *approve all changes
  myself* (detected, not applied, until approved) or *stop future syncs*. Pending changes are flagged with a
  blue (non-breaking) or red (breaking) "i" icon on the Schema tab, with a *Review changes* dialog. The
  page also says Cloud does not bill a sync that backfills a new or renamed column (read through a WebFetch
  summary, not verbatim; not load-bearing for the decision). (Doc — docs.airbyte.com schema change management.)
- **Serves the job well.** It names the exact failure Lighthouse has (old rows stay blank) and offers an
  automatic, whole-stream backfill as a first-class, policy-controlled behaviour. It also makes "pending"
  visible.
- **Fails the job.** The safe behaviour is opt-in; the default leaves old rows blank. A community thread reports
  old records left null even with propagation on (Search).
- **Assumption.** Cost of a full re-sync is the user's to accept, so it is a setting rather than the default.

### 5. Fivetran (ELT data pipelines) — manual historical re-sync

- **Behaviour.** After enabling columns, "run a historical re-sync if you want to backfill the data that was
  missed while the columns were disabled"; for a new PostgreSQL column, "re-sync the table to ensure the new
  column is populated for existing records". (Doc/Search — Fivetran troubleshooting pages.)
- **Serves the job well.** Granular, per-table re-sync.
- **Fails the job.** Entirely manual and documented as troubleshooting — the user finds out from nulls.
- **Assumption.** Data engineers read the troubleshooting pages.

### 6. Jira Data Center — the re-index banner

- **Behaviour.** Some configuration changes ("Add a custom field: Yes", changing a field's context or searcher)
  require a re-index; Jira shows an administrator a banner that the index is out of date, and the admin
  starts a background re-index from *Administration > System > Advanced > Indexing*, which keeps the instance
  usable but slower, advised for low-usage periods. (Doc — Atlassian KB "Reindexing in Jira Server and Data
  Center after configuring an instance"; Search — community article on the banner.)
- **Serves the job well.** A persistent, admin-visible *pending* state that survives until the work is done,
  plus a one-click remedy. This is the same user Lighthouse targets on DC instances.
- **Fails the job.** The admin chooses the time because the job is expensive for the shared instance — so the
  gap stays open until someone acts, and banners get ignored.
- **Assumption.** The expensive step must be scheduled by a human who knows the load profile.

## Synthesis — the option space the market already demonstrates

| Pattern | Who | Gap left open |
|---|---|---|
| Manual reload, user must know when | ActionableAgile, Fivetran | O3, O4 fully open |
| Wait for the next scheduled sync | Screenful, Lighthouse today | O1 bounded by interval; O4 open |
| Automatic backfill on detection (policy) | Airbyte (opt-in) | Closed when on; cost accepted |
| Persistent "pending" marker + admin-run job | Jira DC re-index, Airbyte "approve myself" | O1 open until acted on; O4 closed |

None of the five tools surveyed here (lightweight scope: their public docs and release notes on field and
schema changes) addresses O2 (rules silently evaluated against unfetched values) — the rule layer is
Lighthouse's own, and that is where its silent failure lives. This is an absence within that scope, not a
market-wide claim.

Distinctive fact for Lighthouse: unlike Airbyte, **the expensive full fetch is already committed** the moment
the connection is saved (ADR-140 fingerprint). The open choice is *when* it happens and *whether anybody can
see it pending*, not *whether* to pay for it.

## Sources

- [ActionableAgile — Do I have to reload my data every time?](https://55degrees.atlassian.net/wiki/spaces/ActionableAgile/pages/259260512)
- [ActionableAgile — Loading Data in Jira Cloud](https://55degrees.atlassian.net/wiki/spaces/ActionableAgile/pages/1823965231/Loading+Data+in+Jira+Cloud)
- [ActionableAgile — Release Notes](https://55degrees.atlassian.net/wiki/x/TIEBHg)
- [Screenful — Custom fields now supported for Jira, Trello, and Asana users](https://screenful.com/blog/custom-fields-now-available-for-jira-trello-and-asana-users)
- [Airbyte — Schema change management](https://docs.airbyte.com/platform/using-airbyte/schema-change-management)
- [Airbyte community — schema changes not propagated for old records](https://discuss.airbyte.io/t/schema-changes-not-propagated-for-old-records/6738)
- [Fivetran — Why aren't new columns syncing after I update the schema change handling setting?](https://fivetran.com/docs/connectors/troubleshooting/new-columns-not-syncing-after-schema-change)
- [Fivetran — New column contains null values in the destination](https://fivetran.com/docs/connectors/databases/postgresql/troubleshooting/new-column-null-values)
- [Atlassian KB — Reindexing in Jira Server and Data Center after configuring an instance](https://support.atlassian.com/jira/kb/reindexing-in-jira-server-and-data-center-after-configuring-an-instance/)
- [Atlassian Community — The anatomy of Jira's reindex banner](https://community.atlassian.com/forums/Jira-Cloud-Admins-articles/The-Anatomy-of-Jira-s-Reindex-Banner/ba-p/3264516)

## Gate G2

- [x] 3+ real products named with cited behaviour (ActionableAgile, Screenful, Airbyte, Fivetran, Jira DC).
- [x] Non-obvious alternatives from a different category (ELT pipelines; Jira's own re-index).
- [x] No generic market claims; each claim carries its source and confidence label.
