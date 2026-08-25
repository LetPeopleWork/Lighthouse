# RESUME — Epic 5565, slices 04 and 05

Written 2026-08-25, at the point slice 03 (#5831) was finished. Read this before starting; it is the
short version of what the last three slices established and what those two slices inherit.

## Where the Epic stands

| Slice | Story | State |
|---|---|---|
| 00 SPIKE — Jira Release reality check | #5827 | Closed |
| 01a — See what a Release would give a Delivery | #5828 | Closed |
| 01b — Create a Delivery from a Release | #5829 | Closed |
| 02 — Keep the date in step | #5830 | Closed |
| 03 — Say so when the Release is gone | #5831 | see git log; finished 2026-08-25 |
| **04 — Publish the forecast to the Release** | **#4463** | **next** |
| **05 — Say so when Jira refuses the write** | **#5832** | after 04 |

The Epic (#5565) stays `Active` and must **never** be closed by an agent — `Closed` on an Epic means
released, and that is the maintainer's call at release time. Stop at `Resolved`.

## Slice 04 — the decision that was revised

**Publishing is switched on per Delivery, not per Portfolio.** See `D8a` in `feature-delta.md` for the
full reasoning. The one-line version: *"may Lighthouse write to this Jira"* is a credential question that
already lives on the connection, and *"do I want this forecast broadcast"* is an editorial one that does
not belong beside it. Slice 04's scope line and AC-05.1 are already updated.

Consequences to honour:

- The flag is a property of the **binding**. The aggregate must refuse it on a Delivery that follows
  nothing, and `Unbind` must clear it — the same pattern slice 03 established for `SourceLastSyncedOn`
  and `SourceUnavailableReason`. That invariant is free here and unrepresentable on a Portfolio.
- Ship a `bool`, but name it so the anticipated third mode (write the description **or** overwrite the
  target Release date) becomes an enum on the same field without a second migration.
- Do **not** build a Portfolio-level default with per-Delivery override. The discoverability cost is
  accepted and written down in D8a; the remedy is easy to add later and awkward to unpick.

## What slice 04 inherits from slice 03

- **The archived exclusion is unblocked.** The spec still says it is HELD on #5698 (S15 claims `Delivery`
  has no archive field). That is stale: `ArchivedOn`, `Archive`, `Unarchive` exist, and
  `RecordableDeliveries` already throws if an archived Delivery reaches a background pass. Fix the stale
  line while you are there.
- **`SourceUnavailableReason == null` is NOT a sufficient liveness test** for AC-05.5's "only Deliveries
  bound to a live Release publish". It is also null for a Delivery that has never synced, and the
  transient reason is deliberately never persisted. Decide liveness explicitly.
- **Seven renderer specifications already exist and are `[Ignore]`d** in
  `DeliveryForecastBlockRendererTest`. Three of them pin ADR-179's append-never-guess rule. Un-ignore
  them as the slice lands rather than writing new ones.
- The block marker is `🔮`, anchored on its opening line, measured in slice 00 to survive both the Jira
  API and a hand edit. Detection matches the opening **line**, never the bare emoji.

## Known debt this Epic is carrying

Decide deliberately whether each belongs to slice 04, slice 05, or a bug of its own.

1. **The Jira adapter contract test is still owed** (DISTILL "Adapter coverage" table). It has been
   deferred twice and is the direct cause of item 2.
2. **AC-04.4 is met against the port, not against Jira.** `JiraWorkTrackingConnector.AvailableSources`
   ignores its `connection` argument and returns a static list, so `OffersSource` cannot go false for a
   Jira connection. A real credential downgrade instead resolves to the transient reason and raises
   nothing — the safe direction, but the capability-withdrawn state is only reachable in production for a
   connection that genuinely offers no sources.
3. **`WalkCloudSearchPages` cannot tell an exhausted walk from a truncated one.** It returns the same
   value whether it ran out of pages or hit `MaxCloudSearchPages`, so a Portfolio with enough bound
   Deliveries can silently lose Features from a Delivery while the source reports healthy. Pre-existing
   (slice 02 code), and it makes AC-04.1's "nothing cleared" false through an arm nobody guards. Probably
   wants its own bug.
4. **The delivery wire is unvalidated.** `DeliveryService` parses with `z.custom<IDelivery>()`, which
   validates nothing, and `DeliverySourceUnavailableReason` is a bare TS union rather than a `z.enum`
   like its two neighbours.
5. **AC-04.3's "editable" is proved only by the mode returning to Manual**, not by a rename or reschedule
   succeeding afterwards.

## Working rules that cost time when skipped

- **Move the ADO Story to `Active` as the FIRST action of the slice**, before any code.
- Backend tests: always exclude the live-connector categories. A bare `FullyQualifiedName~Delivery`
  filter is **not** enough — it matches `JiraWriteBackTest`'s `"Delivery Date"` *parameter* and will run
  a live Jira test.
- Never run `pnpm vitest` from inside a component directory: it writes `sonar-report.xml` and
  `test-results.json` to its cwd. The ignore patterns are de-anchored now, but the files still land.
- Mutation: Stryker.NET ignores line ranges, so a whole file gets mutated — scope the **test filter** to
  match, or the score describes other slices' code. A "survived" mutant that does not compile is not a
  gap; check before writing a test for it.
- Adversarial review has found something real on every slice so far, twice by running experiments rather
  than reasoning. Budget for it.
