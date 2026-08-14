# Updates that download only what moved — Epic 5687

Shipped 2026-08-08 → 2026-08-13 in six slices, with two more designed and then dropped on evidence.
Free, behind a preview toggle with a defined end.

The complaint behind it is one number: on the maintainer's own on-premise Jira, a single team refresh of
1457 work items took **7 minutes 49 seconds**, and almost all of it was spent downloading issues nobody
had touched in weeks. The tracker on the other side absorbed the same load.

## What shipped

| Slice | ADO | What it added |
| --- | --- | --- |
| 01 | 5724 | One Information-level line per completed update — `mode`, `scanned`, `fetched`, duration — and everything the update *iterated over* demoted to Debug. The measuring instrument, shipped first. |
| 02 | 5725 | The two-phase contract on the Jira Cloud **team** path: sweep the whole query for `(referenceId, changedAt)`, download payloads only for the records whose stamp moved. `LastChangedRemote` on the work item; `SupportsIncrementalSync(connection)` on the port. |
| 03 | 5726 | The same contract for **portfolios** and their parent Features — and the survivor rule that keeps a portfolio from deleting the Features it did not refetch. |
| 05 | 5728 | The **fetch fingerprint**: thirteen properties that decide what a cycle asks for and how it reads the answer. A change to any of them costs one full cycle. Also narrowed the settings purge to a connection change. |
| 04 | 5727 | **Jira Data Center** — an offset walk with its own stable ordering, after a probe proved DC pagination returns a stable id set. Found and fixed Bug #5755. |
| 06 | 5729 | **Azure DevOps** — revisions are read only for the records that moved. Found and fixed Bug #5756. |
| ~~07~~ | ~~5730~~ | **Removed 2026-08-13** — ServiceNow. Nobody could show enough instances running on it to earn the work. |
| ~~08~~ | ~~5731~~ | **Removed 2026-08-13** — Linear, same reason. |

ADRs **138** (two-phase sync), **139** (per-connection capability probe), **140** (fetch fingerprint on the
config aggregate) and **141** (time-driven derivations over the stored set).

## The delete rule was the constraint, not the cost

The obvious design — ask the tracker for `updated >= lastSync` — is wrong here, and wrong in a way that
destroys data rather than merely underperforming. `RefreshWorkItems` removes any stored record the fetch
did not return. Under a naive delta query every unchanged record stops being returned, so either every
record becomes immortal or the ones that went quiet get deleted.

So phase one runs **the same full query** the download always ran, asking only for identity and change
stamp. `removed = stored − swept` keeps exactly the meaning it had before delta existed. That is why the
dogfood numbers report `scanned` next to `fetched`: the speed is the headline, but `scanned` being
identical across both modes is the correctness signal.

Comparison is **per record**, never against a global watermark — the sweep returns a timestamp for every
id, so clock skew and `lastSync` semantics never enter the design.

## Three traps that were found by reading code, not by tests

**Staleness would have silently stopped.** `AddStalenessEventIfThresholdCrossed` ran inside the loop over
*fetched* records. Under delta, the record that goes stale is exactly the record that stops being fetched
— so nothing would ever have gone stale again, with every test green. Time-driven derivations now run over
the *stored* set every cycle regardless of mode (ADR-141).

**The portfolio half is not the team half with a different noun.** A team refresh removes what the query
stopped returning; a portfolio refresh *rebuilds itself from what was fetched* — `UpdateFeatures` is a
Clear + AddRange — and `OrphanedFeatureCleanupService` then deletes any Feature left claimed by nobody.
This was demonstrated during slice 03, not theorised: a step that fed the fetched list straight back in
reported two Features MISSING, deleted. Sequencing the survivor rule first, while every cycle was still
full, is what kept that out of a commit.

**The old purge masked the fingerprint, so slice 05 had to jump the queue.** Editing a query used to
discard the entity's stored records, which meant the resolver answered `Full` on its "nothing stored"
branch — and six of one scenario's eight cases would have gone green with a fingerprint that did nothing.
The narrowing had to land before anything measured the widened set.

## The user-visible behaviour change worth naming

Editing a team's or portfolio's query, work item types, states, state mapping or Done cutoff **no longer
throws away the stored work items and their transition history**. Only pointing the entity at a different
work tracking connection does that — the one edit where the same reference id genuinely means a different
item, and therefore the one thing `removed = stored − fetched` cannot reconcile on the next full cycle.

This holds whether the toggle is on or off, and it is the line the release notes owe.

## Two live bugs the epic found

**Bug #5755 — a query ending in `ORDER BY` was sent as invalid JQL.** `PrepareQuery` parenthesises the
operator's own query before combining it with the type and state filters, so an ordering clause landed in
the middle of an expression. Pre-existing, on Cloud as much as Data Center, on the full fetch as much as
the sweep — and self-written JQL is exactly who runs a Data Center deployment. The house rule already
existed (`RemoveOrderByClause` had been stripping saved-filter JQL for years); it had simply never been
applied to the team query. The strip needed hardening on the way in — it matched `ORDER BY` anywhere, so
`summary ~ "reorder by priority"` truncated mid-value — and is now token-boundary and quote-aware.
Backslash-escaped quotes are still not handled.

**Bug #5756 — a fetch that could not ask was read as a query that matched nothing.** Both Azure DevOps
fetches, and both Linear ones, caught their own transport failures and returned an empty list. The caller
cannot tell that from a legitimate empty result — and removal is `stored − fetched`, so it deletes every
stored work item of that team. Found by a mutation run on the sweep. A related hazard is knowingly still
open: Linear answers with no records when the configured team name cannot be resolved, and ServiceNow does
the same for a missing query, so a renamed Linear team still loses its stored work items quietly.

## What it measures

| Instance | Records | Full | Delta | Factor |
| --- | --- | --- | --- | --- |
| Jira Cloud (slice 03) | 4 | 3 555 ms | 1 092 ms, then 1 944 ms after one state change | — |
| Jira Data Center, on-premise | 1 457 | 468 856 ms | 2 087 ms | **225×** |
| Azure DevOps, Lighthouse's own board | 374 | 20 106 ms | 947 ms | **21×** |

On Azure DevOps the product claim mattered more than the duration: per-item transition counts, dumped
before and after, came back byte-identical. A transition there is reconstructed by walking an item's
revisions, so a quiet item that is never fetched must keep the transitions it already has — and it does.

## What was deliberately not built

- **No UI.** The observable surface is the log line. Any view over update runs belongs to Epic #5511.
- **ServiceNow, Linear and CSV keep running full updates.** For the first two that is the adoption-evidence
  decision; for CSV it is permanent — there is no remote to ask.
- **The toggle has an end.** Once enough real instances have run on it, delta becomes the normal behaviour
  and the `DeltaSync` optional feature goes away.

## Lessons

- **Two false-green generators were met live and are now in the CI ledger.** `dotnet test --no-build`
  after a *failed* build silently runs the previous binary and prints `Passed!` — and `&&` does not save
  you, because a piped build exits with the pipe's status. And Stryker's reported mutation text does not
  always compile if applied literally, so hand-verifying a survivor means inversions and value swaps only.
- **Mutation testing earned its keep as design feedback, not as a score.** It killed the whole
  parent-inversion rule, which had no test at all; it found `AddProjectToFeature` to be dead code; it found
  the paging mutant that would have turned a 1457-issue sweep into 1457 requests; and it surfaced Bug
  #5756. One slice missed the 80 % gate by a single mutant, and that was recorded rather than padded, with
  every survivor verified equivalent by application.
- **A guard that forbids behaviour which does not exist yet proves nothing until its positive control is
  green.** Several slices shipped such guards knowingly, each with the re-run recorded in its handoff.
- **Stryker.NET whole-file scores are noise on a slice-shaped change.** It ignores line spans, so the
  verdict is always the changed-line figure, recovered from the report by intersecting mutant locations
  with the diff.
