# Slice 01: Jira reads the parent sources in order (US-01, ADO #6501)

**Hypothesis it tests:** the first source that yields a parent is the parent the customer means, and an install can do
without the tracker's own Parent. Disproved if, on the reporter's own Team, a meaningful share of items carrying
several sources want a different winner per item type and no single order fits (then the answer is a per-type rule, not
an order).

**Scope**
- An ordered list of one to five parent sources on the shared owner base (Team and Portfolio), set-here only;
  "Parent (Jira's own)" is an optional, movable entry, read only when listed.
- Storage and migration: the list is one text column of JSON per owner table (serialiser options pinned in DESIGN);
  override X becomes `[X, own Parent]`, no override becomes `[own Parent]`, followers hold none. The legacy integer
  columns are written through for one release; the rollback limit (a list of any other shape resolves as its first field
  and then the own Parent on the previous build) is stated and tested. What the upgrade does to parents that are read today
  is the maintainer's decision **UPGRADE-1** (see Done when). Architecture rule 5 lands here, with the two
  `ConnectionFieldDefaults.cs` registry rows allow-listed until slice 03 migrates them.
- The record keeps the ordered readings for the refresh, so a link entry whose candidates hold no stored Feature lets
  the next entry be read; the placement step runs for every record with a link reading ahead of its winner.
- Jira resolution: field entries, link-type entries (narrowing per Story 6301; a single candidate is judged by
  it too (a legacy-mode owner under UPGRADE-1 (B) excepted), and no Feature held means the next entry), the tracker's own Parent
  only when listed: the parent key, and on Data Center the instance's own parent field after it. One helper alone writes
  the stored parent (an empty choice clears it); the placement function judges a single candidate like several; the
  record's copy constructor carries the readings across the wrap. The helper's clear runs after the Jira connector's
  populate step for the Additional Field values, so a cleared value is not written back. The count and warning lines of
  the placement step cover ambiguous links only.
- UPGRADE-1 (what the upgrade does where today's own-parent fallback is decided at read time: Azure DevOps, Data
  Center, a single non-Feature link candidate) is settled by the maintainer before this slice's migration step; neither
  branch is assumed here.
- Settings list control (see sketch in feature-delta.md) with the "not in the list" consequence copy; save refuses a
  sixth entry and a duplicate.
- Fetch fingerprint (legacy shapes render as before), connection-save guard (loads the connection's owners and scans
  every list), refresh findings per entry.

**Out:** connection default (slice 03), Azure DevOps (slice 02), levels, multi-parent.

**Done when:** US-01 UAT passes (seven scenarios); Refresh History names an unmatched entry; the migration, round trip
and write-through tests are green on SQLite and Postgres; and the migration scenario holds in the form of the maintainer's
decision **UPGRADE-1** (two branches, neither is the default; full text in the design's DD-4):

- **If (A), accept the change:** every override owner is `[X, TrackerParent]`, every other owner `[TrackerParent]`; the
  fall-through rule applies everywhere; a one-time re-read follows (every Jira owner through the Jira reading version,
  with `JiraReadingVersionFingerprintTest` goldens regenerated; ADO override owners through the new fingerprint form).
  Some items whose override names nothing, or only a non-Feature, move under their own Parent; a release note is
  required; the DISCUSS "identical results" criterion is amended.
- **If (B), preserve until edited:** a legacy-mode flag per owner reproduces today's read-time behaviour until the owner
  saves the list once; parents are identical for every owner; nothing is queued at the upgrade; the fingerprint is
  byte-identical.

**Size:** 2-3 days (the same figure as in feature-delta.md; total for the Story 4-5 days). Split Team from Portfolio if
it overruns.

**Release order:** first. Slices 02 and 03 depend on this one and are released only after it.

**Learning to take to slice 02:** whether the record's ordered readings (kept for the second pass) are enough, or the
placement step needs more. Azure DevOps has no links, so it never needs the second pass.
