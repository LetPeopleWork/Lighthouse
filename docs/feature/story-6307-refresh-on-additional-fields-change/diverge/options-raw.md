# Options (raw, unevaluated) — story-6307-refresh-on-additional-fields-change

DIVERGE wave · 2026-10-15 · revision 2 (after peer review round 1). Generation only; nothing here is scored or
ranked (see `taste-evaluation.md`). Statements under "Key assumption" are what must be true, not judgements.

## HMW question

**How might we make a change to how a connection reads the work tracking system be in force — or plainly not
yet in force — for every Team and Portfolio before anyone builds on it?**

(No embedded mechanism: the answer may be faster data, a visible state, a different rule behaviour, a
different trigger, or no fetch at all.)

## SCAMPER

### S — Substitute: Field-only backfill

**Core idea**: After the save, Lighthouse fetches only the new field's values for the Work Items and Features it
already stores, instead of running a whole update of each Team and Portfolio.
**Key mechanism**: A new connector capability "read field set F for stored reference ids", run per owner on the
update queue, writing into `AdditionalFieldValues` without touching anything else.
**Key assumption**: Each connector can read one field for a known id list, and a partial write leaves every other
stored value and transition consistent.
**SCAMPER origin**: Substitute (replace the whole update with a narrow one).
**Closest competitor**: Fivetran per-table historical re-sync, narrowed to a column.

### C — Combine: Update the connection's Teams and Portfolios on save

**Core idea**: Saving a connection whose field set changed queues an update of every Team and Portfolio that uses
it; the admin sees "Updating 4 Teams and 3 Portfolios" and the rows appear in the Task Manager.
**Key mechanism**: After the connection PUT commits, each owner on that connection whose fetch fingerprint no
longer matches gets `TriggerUpdate`. An edit that moves no fingerprint queues nothing.
**Key assumption**: Each owner owes one full fetch anyway (fingerprint); the sequential queue is the pacing.
**SCAMPER origin**: Combine (connection save + update queue).
**Closest competitor**: Airbyte with "Backfill new or renamed columns" switched on.

### A — Adapt: "Waiting for an update" marker with one-click remedy

**Core idea**: Every Team and Portfolio whose stored data was read under an older field set shows a small
"Waiting for an update — fields changed on the connection" marker, on its page and in the lists, with an
"Update now" action; the connection page lists them and offers "Update all of them".
**Key mechanism**: Derived state: `owner.FetchFingerprint != FetchFingerprint.For(owner)`. No new stored column.
**Key assumption**: An admin who sees the marker acts on it, or lets the schedule clear it.
**SCAMPER origin**: Adapt (Jira DC's re-index banner; Airbyte's pending-schema icon).
**Closest competitor**: Jira Data Center re-index banner.

### M — Magnify: Rules tell the truth about unfetched fields

**Core idea**: Wherever a rule names an additional field (Delivery rule, forecast filter, blocked rule), the rule
editor and the rule's result say "not loaded yet for this Team/Portfolio" until the field has been fetched, and
the rule does not produce an empty match in the meantime.
**Key mechanism**: The rule surfaces ask whether the owner's last completed fetch included the field and render
that state; evaluation treats "not loaded" as its own outcome.
**Key assumption**: The harm is the silent wrong answer more than the delay.
**SCAMPER origin**: Magnify (the rule layer, where the failure is felt).
**Closest competitor**: none among the surveyed tools; nearest is Airbyte's schema icon.

### P — Put to other use: Every fetch-shaping save takes effect at once

**Core idea**: Any save that moves a fetch fingerprint — Team, Portfolio or connection — queues the affected
owners server-side.
**Key mechanism**: A domain event raised on configuration save; a handler compares fingerprints and enqueues.
**Key assumption**: One server-side rule for every configuration surface is wanted, replacing the frontend calls
after Team/Portfolio saves.
**SCAMPER origin**: Put to other use.
**Closest competitor**: Airbyte "propagate all" policy.

### E — Eliminate: Re-derive from what is already stored

**Core idea**: No fetch at all: Lighthouse keeps each item's raw tracker payload, so a newly added field is read
out of the stored payloads the moment it is saved.
**Key mechanism**: Persist the raw response per Work Item and Feature at sync time; on a field-set change,
re-run the field extraction locally over stored payloads.
**Key assumption**: Storing the full raw payload per item is affordable, and the tracker returned every field
(not only the requested ones) in the first place.
**SCAMPER origin**: Eliminate (remove the fetch from the path).
**Closest competitor**: ELT "load raw, transform later" (Airbyte/Fivetran raw tables).

### R — Reverse: The field is not offered until it has data

**Core idea**: The rule builder lists a newly added field as "loading — available after the next update" and does
not let it be picked until the owner has fetched it.
**Key mechanism**: The rule schema for an owner disables additional fields its last completed fetch did not include.
**Key assumption**: Holding the rule author back for the gap is accepted.
**SCAMPER origin**: Reverse (the rule waits for data instead of data being pushed to the rule).
**Closest competitor**: none found.

## Crazy 8s supplements

### X1 — Ask on save

**Core idea**: Save opens a dialog: "This changes what 4 Teams and 3 Portfolios read. [Save and update them now]
[Save only — they update on their schedule]".
**Key mechanism**: Pre-save diff of the field set in the browser plus the owner list; the button decides.
**Key assumption**: The admin wants to choose the moment at save time.
**SCAMPER origin**: Crazy 8s.
**Closest competitor**: Airbyte "approve all changes myself".

### X2 — The schedule treats a changed owner as due now

**Core idea**: The periodic loop counts an owner whose fingerprint moved as due, so the next tick (≤ 60 min) picks
it up instead of after the 180-minute "refresh after".
**Key mechanism**: `ShouldUpdateEntity` also returns true on fingerprint mismatch.
**Key assumption**: Up to an hour is short enough, and nobody needs to see the wait.
**SCAMPER origin**: Crazy 8s.
**Closest competitor**: Screenful's hourly sync.

### X3 — The rule that needs the field asks for it

**Core idea**: Saving (or opening) a rule that names a field its Team or Portfolio has not fetched yet queues an
update of that one owner; owners with no rule on the field update on their schedule.
**Key mechanism**: The three rule save paths check the named additional fields against the owner's last
completed fetch and call that owner's `TriggerUpdate`.
**Key assumption**: The field only needs to be present where something reads it.
**SCAMPER origin**: Crazy 8s.
**Closest competitor**: ActionableAgile's load-when-used model.

### X4 — Update on first view

**Core idea**: The first time anyone opens a Team or Portfolio still on the old field set, that owner is queued.
**Key mechanism**: The owner read path checks fingerprint mismatch and calls `TriggerUpdate` (coalesced).
**Key assumption**: An owner nobody looks at does not need to be current.
**SCAMPER origin**: Crazy 8s.
**Closest competitor**: ActionableAgile's cache-until-opened model.

## Grouping by mechanism family

Peer review round 1 found that "update on save", "ask on save" and "the schedule treats it as due" share one
mechanism (the same full fetch of every owner whose fingerprint moved) and differ only in who decides the moment
and when. They are therefore **one** curated option; the moment is a sub-decision inside it.

| Family | Members | Shared mechanism |
|---|---|---|
| Fan-out on the connection | C, X1, X2, P | full fetch of every owner whose fingerprint moved; variants differ only in timing, decider or surface scope |
| Visible pending state | A | derived owner state + human remedy |
| Rule layer, display | M, R | rule surfaces render "not loaded"; R is the blocking form of M |
| Consumer-triggered refresh | X3, X4 | the reader of the field triggers its own owner only |
| Narrow fetch | S | new per-field connector capability |
| No fetch | E | local re-derivation from stored raw payloads |

## Curated 6

| # | Curated option | Mechanism | Assumption about the user | Cost profile |
|---|---|---|---|---|
| 1 | **Update the connection's Teams and Portfolios on save** (timing variants: now / ask first / next tick) | server enqueues every owner whose fingerprint moved | the admin wants the effect, not the decision | small backend change, one line of copy |
| 2 | **"Waiting for an update" marker with one-click remedy** | derived pending state on owner and connection surfaces; a human triggers | the admin will see it and choose the moment | read-side computation + UI on several surfaces |
| 3 | **Rules tell the truth about unfetched fields** | rule surfaces and evaluation render "not loaded yet" | the harm is silence, not delay | three rule surfaces + evaluation semantics |
| 4 | **The rule that needs the field asks for it** | rule save path enqueues its own owner only | the field matters only where it is read | three rule save paths + "fetched?" check; fewer full fetches up front |
| 5 | **Field-only backfill** | new narrow connector capability | the field matters, the rest of the record does not | new capability in every remote connector |
| 6 | **Re-derive from what is already stored** | local extraction from stored raw payloads | storage is cheap and the tracker sent every field | new persistence of raw payloads for every item |

### Diversity test — all 15 pairs

M = different mechanism, A = different assumption about the user, C = different cost profile.

| Pair | M | A | C | Note |
|---|---|---|---|---|
| 1–2 | yes | yes | yes | system queues now vs human acts on a visible state |
| 1–3 | yes | yes | yes | fetch earlier vs label the gap |
| 1–4 | partly | yes | yes | both end in `TriggerUpdate` of an owner; the difference in mechanism is the trigger predicate and scope (every owner on the connection, at the save, vs only owners whose rule reads the field, at the rule save). The pair passes on assumption and cost |
| 1–5 | yes | yes | yes | full update vs narrow per-field read |
| 1–6 | yes | yes | yes | fetch vs no fetch |
| 2–3 | yes | yes | yes | owner-level state vs rule-level state |
| 2–4 | yes | yes | yes | human remedy vs automatic per-consumer trigger |
| 2–5 | yes | yes | yes | display + manual vs new connector capability |
| 2–6 | yes | yes | yes | display + manual vs local re-derivation |
| 3–4 | yes | yes | yes | display only vs trigger |
| 3–5 | yes | yes | yes | display vs narrow fetch |
| 3–6 | yes | yes | yes | display vs local re-derivation |
| 4–5 | yes | yes | yes | full update of one owner vs narrow read of all |
| 4–6 | yes | yes | yes | fetch vs no fetch |
| 5–6 | yes | yes | yes | remote narrow read vs local re-read |

### Merged / removed

- **X1 (ask on save)** and **X2 (schedule treats it as due)** merged into Option 1 as timing variants: same fetch,
  same owners, same predicate.
- **P (every fetch-shaping save)** merged into Option 1 as a scope variant (wider surface, same mechanism).
- **R** merged into Option 3 (blocking form of the same rule-layer state).
- **X4 (update on first view)** merged into Option 4 (in both, the reader triggers its own owner).

## Gate G3

- [x] HMW without an embedded solution.
- [x] All 7 SCAMPER lenses applied; 4 Crazy 8s supplements.
- [x] 6 curated options from 6 distinct mechanism families; all 15 pairs pass the 3-point test (pair 1–4 on
  assumption and cost, with a partial mechanism difference).
- [x] No evaluation language in the generation sections.
