<!-- DES-ENFORCEMENT : exempt -->

# Evolution Archive — epic-5698-deliveries-as-durable-records (Finalize)

**Feature ID**: `epic-5698-deliveries-as-durable-records`
**Epic**: ADO #5698 (https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5698)
**Stories**: #4309 (01 export, Closed) · #5639 (02+03 notes, Closed) · #5640 (04+05 archive)
**Customer**: LetPeopleWork
**Waves shipped**: DISCUSS → DESIGN → DISTILL (2026-08-21) → DELIVER (2026-08-21/22)
**HEAD at finalize**: `5d57fa0fe`
**Status**: All five slices shipped. Slices 01–03 verified by the maintainer on main; 04 and 05
pushed and CI-verified. Backend mutation above the gate, frontend below it — see *Open at finalize*.

---

## Feature summary

A Delivery answered *where do we stand today*, and only that. The moment it shipped, the record
dissolved: its Features moved on, were re-estimated, closed or removed, and the Delivery quietly
rewrote itself behind them. The one review worth having — *what did we say, and what actually
happened* — was impossible, because there was nothing left to compare against.

Five slices made a Delivery something you can keep:

1. **Export** — headline numbers and Feature grid out as one CSV or one paste.
2. **Notes** — dated, attributed, on the Delivery itself.
3. **Authorship** — only the author may correct a note.
4. **Archive** — the Delivery leaves the active list and its numbers are pinned.
5. **Read the archive** — it reads the same afterwards, no matter what the Portfolio does.

## Key decisions

- **The pin is its own table keyed by `DeliveryId` alone** (ADR-160). The daily snapshot's
  `(DeliveryId, RecordedDay)` unique key was the source of every collision path DISCUSS worried
  about; a record that never carries a day never meets it. Archive → un-archive → archive within one
  day became an ordinary upsert rather than a special case.
- **The archived read is structurally unable to recompute** (ADR-161). `ArchivedDeliveryProjection`
  is handed an identity and a closure record — never a `Delivery`, never `today`, never blackout
  periods — so `CalculateMetrics` is uncallable on that path. An ArchUnit test enforces it, and was
  proven non-vacuous by adding a probe method and watching the rule fail.
- **No global EF query filter** (ADR-163). Both background writers read through a narrowed
  `RecordableDeliveries` port instead. `RecomputeRuleBasedDeliveries` takes that type, so an archived
  Delivery cannot enter the re-match loop — the six old call sites failed to *compile*, which is the
  evidence the narrowing works. A filter would have silently emptied the Archived section too.
- **Archiving is premium; un-archiving is not.** Gating the way in but not the way out is a
  capability you sell; gating both traps people in a state.
- **The confirmation may not promise protection from deletion**, because deleting an archived
  Delivery still destroys it and its closure record by cascade. The wording is tested by asserting
  the *absence* of "safe", "protected", "permanent", "forever" — not just the presence of the right
  words.

## What the hypotheses actually proved

Two slices carried falsifiable claims. Both came back confirmed, and one of them found something the
design had not named.

**Slice 04 — can a Delivery be frozen while a background refresh already holds a copy?** Confirmed:
the concurrency token stops the stale write. But the *recovery* turned out to be load-bearing, and
the intuitive recovery defeats the guard entirely: `entry.ReloadAsync()` — which the design
originally prescribed — re-reads the version the database just refused against, so the same pending
change then matches and the next save writes it through. **The archive is undone by the attempt to
recover from it.** Only detaching the entry is a genuine quiet no-op. Recorded in ADR-164.

**Slice 05 — can a Delivery be a durable record at all?** Confirmed, and this is the Epic's whole
premise. `ArchivedDeliveryReadStabilityIntegrationTest` archives a Delivery, then refreshes the
Portfolio in a way that deletes one Feature, renames another, changes a third's counts and adds a
fourth — and both readings come back character-for-character identical. Proven capable of failing by
rewiring the read to the live projector, which produced exactly the drift it was written to catch.

## Lessons

- **A single-team fixture hides a whole class of bug.** Mutation testing found `Sum` → `Max` on a
  Feature's work surviving, because every fixture in the suite gave a Feature exactly one team, and
  for one team the two are the same number. Reporting the busier team's share as the whole makes a
  Feature look smaller and further along than it is.
- **Scraping a grid is not reading it.** The first export built its file from
  `apiRef.getCellValue(...)`, which only works for a column with a plain backing field. Five derived
  columns shipped wrong — a raw JSON array where dates belonged, a sort-order count where names
  belonged, blanks where values belonged — and it took a human opening the file to notice. Nothing
  asserted what a *rendered* cell exports as. Slice 01b replaced the scrape with a settled table and
  added that scenario.
- **A column can exist for a number nothing computes.** `ForecastHowMany` sat on the snapshot table
  across several releases, was never written by anything, and was carried onto the closure record out
  of symmetry. Removed in both places.
- **CI catches what local gates structurally cannot.** A Playwright spec still read the deliveries
  endpoint as a bare array after it became `{active, archived}`; it runs against a live build, so no
  local gate was ever going to see it.

## Work completed

34 commits between `248b5fecb` and `5d57fa0fe`. Two EF migration pairs (archive + closure record;
drop of the unused column), both verified against real SQLite and Postgres databases carrying rows
rather than trusted to the suite — EF InMemory skips migrations entirely.

## Migrated artifacts

- Acceptance specs → `docs/scenarios/epic-5698-deliveries-as-durable-records/` (7 `.feature` files,
  90 scenarios)
- ADR-160 … ADR-165 were authored directly in `docs/product/architecture/` and needed no migration
- Mutation configs and results → kept in the workspace under `mutation/`

## Open at finalize

- **Frontend mutation is 50.50 %, below the 80 % gate.** Reported as it stands rather than tuned:
  `DeliverySection.tsx` at 10.74 % with 198 survivors dominates it, and that is a large pre-existing
  component this Epic only added a button to. Excluding it would have put the headline in the
  seventies while describing less. `ArchivedDeliveriesSection.tsx` at 44.77 % is the genuine gap in
  new code. Backend is 81.58 %.
- ~~ADR-162 describes a mechanism that no longer exists.~~ **Closed 2026-08-22**: ADR-162 is marked
  SUPERSEDED with a note saying what the file it produced actually looked like, and
  [ADR-172](../product/architecture/adr-172-delivery-export-is-one-settled-table-the-caller-builds.md)
  records what replaced it.
- **`TrySaveRecomputedDeliveries` is coarser than intended.** One conflicting Delivery drops that
  cycle's recomputes for its siblings, because the unit of work is the whole session and
  `deliveryRepository.Save()` currently doubles as the save persisting the feature refresh. It
  self-heals on the next refresh.
- **Two recorder assertions are narrower than they read.** Slice 04's "stops recording after
  closure" and slice 05's mirror both run their refreshes on one calendar day, where the snapshot day
  key would keep the count flat regardless. The non-vacuous proof is the recorder unit test that
  advances the clock.
- **A zero-Feature Delivery reads `0 %` live and empty once archived.** The archived side is the
  honest one; changing the live path is a behaviour change beyond this Epic.
- Release notes drafted under a placeholder heading; ADO transitions and the website pricing line are
  the maintainer's.
