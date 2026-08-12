# Slice 05 — A setting costs a refetch only when it changes what is fetched

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5728 · **Story**: US-05 (+ US-08 precursor)
**Estimate**: ~6h · **Reference class**: `PrepareQuery` on both connectors — the fingerprint is, almost
exactly, the argument list of that method plus the connection it runs against.

## Goal

Lighthouse decides for itself whether an edit changed what gets fetched: a query, type, state, cutoff,
additional-field, parent-override or connection change forces the next cycle to be a full fetch;
everything else provokes no remote fetch at all.

## IN scope

- **Precursor commit (US-08, `@infrastructure`)**: a fetch-fingerprint column on Team and Portfolio,
  additive expand-only migration via `CreateMigration`.
- Fingerprint computed from exactly: `DataRetrievalValue`, `WorkItemTypes`, `AllStates`,
  `DoneItemsCutoffDays`, additional field definitions, parent-override field,
  `WorkTrackingSystemConnectionId`. Order-insensitive for the collections, so re-saving the same states
  in a different order is not a change.
- Mismatch → next cycle is `mode=full`, and the summary line names configuration as the reason.
- Everything outside the list — wait states, blocked rules, staleness threshold, named cycle times,
  ordering policy, terminology — leaves the fingerprint alone and provokes nothing.
- **A guard test** that enumerates the properties reachable from `PrepareQuery` and the connector call
  sites and fails when one is neither in the fingerprint nor on an explicit, commented exclusion list.
- An instance upgrading into this feature has no stored fingerprint, so its first cycle is full (D8).
- **One property set, two consumers (added 2026-08-09 — see the amendment below).** The fingerprint and
  the existing save-time decision `WorkItemRelatedSettingsChanged` are folded onto a single list, and the
  guard test covers **both** call sites. This closes the live `DoneItemsCutoffDays` gap — it shapes the
  remote query today but triggers no purge — and resolves the team/portfolio asymmetry, both as
  acceptance criteria of this slice rather than as separate work items.

## OUT of scope

- Triggering an update on save. Settings saves do not fetch today (`TriggerUpdate` is called only from
  the manual controller endpoints and the periodic loop) and this slice does not change that — it only
  decides what *mode* the next scheduled cycle runs in.
- A manual "Force full refresh" action. D3 makes it unnecessary and its presence would imply the
  automatic path is not trusted.
- Per-field granularity ("only the states changed, so refetch only the newly included states"). A
  fingerprint is a boolean, deliberately.
- Any UI.

## Known limitation this slice does not close (found in DISTILL, 2026-08-11)

**A blocked-rule edit does not re-open or close spells for records that did not move.** The slice's
secondary learning hypothesis asked whether blocked rules belong in the fingerprint. By the hypothesis's
own criterion they do not: `blockedItemService.IsBlocked(workItem, team)` reads the **stored** item, so
re-deriving after a rule change needs no remote call at all — and AC-5.3 is right to leave the edit
free.

But the delta loop only visits *downloaded* items, so after a rule edit a quiet item's blocked spell is
never re-evaluated until it next moves. The read path recomputes `IsBlocked` per request, so the UI is
correct immediately; the `WorkItemBlockedTransition` / `FeatureBlockedTransition` history is not.

**Its home is the ADR-141 / DDD-4 derivation pass** — the second pass over the stored set that already
took staleness out of the fetch loop — **not the fingerprint.** Putting the rule in the fingerprint
would buy a local derivation with a full remote download, which is the wrong instrument and would spend
exactly the win AC-5.3 exists to protect. Recorded here so a reader of this brief does not have to find
it in the feature-delta's upstream issues.

## Behaviour change this slice ships (found in DISTILL, 2026-08-11)

**Today, editing a Team's query, work item types, states or state mappings deletes every stored work
item for that team and its whole transition history** (`TeamController.UpdateTeam:178` →
`RemoveWorkItemsForTeam`, with a cascade on `WorkItemStateTransition.WorkItemId`). **After this slice it
does not** — only a connection change does.

This is user-visible and nobody wrote the current behaviour down, so nobody will notice it changed. What
an admin gains: editing a query no longer costs the team's recorded flow history, so cycle-time and
time-in-state numbers survive a settings correction that today silently resets them. What replaces the
purge: `removed = stored − fetched` on the next full cycle, which is how the portfolio side has always
reconciled the same edit without a purge.

A connection change keeps the purge, and gains one on the portfolio side it never had — because the same
reference id on a different tracker is a different item, and that is the one case set difference cannot
reconcile.

**Wants a release-note line at slice close.** Not written here: the note belongs to whoever cuts the
release, and this is the flag that it is owed.

## Amendment 2026-08-09 — there is already a second answer to this question, and it disagrees

Found while preparing slice 02. Read this before writing the fingerprint: **the "does this edit change
what gets fetched?" decision already exists in the codebase**, under a different name, with a different
property list, and only for one of the two entities.

### What is there today

`TeamController.UpdateTeam` (`:178`) calls `team.WorkItemRelatedSettingsChanged(teamSetting)`
(`API/Helpers/TeamExtensions.cs:65`). On true it runs `workItemRepository.RemoveWorkItemsForTeam(team.Id)`
— **it deletes every stored work item for the team** — and the next scheduled cycle refetches from
nothing. On false, nothing is deleted and nothing is fetched.

So the promise this slice is named after — *a setting costs a refetch only when it changes what is
fetched* — is **already half-kept**. Editing wait states, blocked rules, staleness thresholds, the SLE or
cycle-time definitions on a team is genuinely free today. That is worth knowing before writing a brief
that implies none of it exists.

### Three defects in what is there

1. **Portfolio has no equivalent at all.** `PortfolioController.UpdatePortfolio` (`:96`) syncs and saves;
   there is no change detection and no purge. Its Features are reconciled only by the fetch's own
   removal rule on the next cycle. Two entities, two different answers to one question.

2. **The team path is destructive, and possibly redundantly so.** It answers "the query changed" by
   deleting stored work items *and their history*, then refetching. But `removed = stored − fetched`
   already reconciles exactly that on any full cycle — which is precisely how the portfolio side copes
   without a purge. The purge looks like belt-and-braces bought with transition history. **Verify before
   removing it**: the plausible reason it exists is a case the removal rule does not cover, and the fact
   that nobody wrote that reason down is not evidence there isn't one.

3. **The property lists differ, and `DoneItemsCutoffDays` is a live gap.**

   | Property | `WorkItemRelatedSettingsChanged` | this slice's fingerprint |
   |---|---|---|
   | `DataRetrievalValue`, connection id, `WorkItemTypes`, all states | ✅ | ✅ |
   | `StateMappings` | ✅ | ❌ |
   | `DoneItemsCutoffDays` | ❌ | ✅ |
   | additional field definitions | ❌ | ✅ |
   | parent-override field | ❌ | ✅ |

   `DoneItemsCutoffDays` is part of the remote query's resolved-cutoff clause — it demonstrably shapes
   the result set — and changing it triggers no purge today.

### What this slice must therefore do

**One property set, two consumers.** The fingerprint and the save-time decision are the same question
asked twice; they must not ship as two lists. Fold `WorkItemRelatedSettingsChanged` onto the fingerprint's
property set, and extend the guard test (already an acceptance criterion here) to cover **both** call
sites rather than the fingerprint alone.

This is the difference between the slice's stated hypothesis holding and quietly failing. As briefed, the
guard test protects the fingerprint from drift while an older, shorter list sits in a file the guard does
not look at — which is the very drift the test exists to prevent, reintroduced one directory away.

While doing it, resolve the portfolio asymmetry: whatever the answer is, both entities give it.

### Two things this amendment is not

- **Not gated by the opt-in flag** (slice 02's amendment). This is true whether the next cycle is full or
  delta, so it ships plain.
- **Not blocked on delta either — but not independent of it.** With delta off, the fingerprint's only
  output ("the next cycle is `mode=full`") is what already happens, so it is *inert*, not inactive: the
  column fills, the guard test runs, and no behaviour changes until someone opts in. That makes it safe
  to ship early and cheap to ship ungated, and it is why this slice needs no flag of its own.

**Decided 2026-08-09 (maintainer): the `DoneItemsCutoffDays` gap is fixed here, not pulled forward as its
own bug.** It is the same defect as the rest of this amendment — one property set, asked twice, with the
two copies disagreeing — so splitting it out would mean touching the same list in two changes and
carrying a bug that closes the moment this slice lands. It becomes an acceptance criterion of this slice
rather than a separate work item.

## Learning hypothesis

**Disproves "the fetch-shaping property set can be enumerated in one place."** If it cannot — if a
property reaches the remote query through a path the guard test cannot see — then the fingerprint will
drift the first time someone adds a connector option, and delta will serve a stale result set with every
test green. That failure mode is wrong numbers with no error, which is why the guard test is an
acceptance criterion and not a nice-to-have.

If it fails, D3 collapses back to "any settings save invalidates", which is safe and wastes the win — a
worse product, but an honest one, and better learned in six hours than after three connectors ship.

Secondary: **disproves "local-only edits are actually local."** Blocked rules are the interesting case —
they are evaluated at sync time and drive `FeatureBlockedTransition` writes. If changing a blocked rule
requires re-deriving from remote data rather than from stored data, it belongs in the fingerprint after
all, and the epic's second promise shrinks.

## Acceptance criteria

AC-5.1 … AC-5.6 from `feature-delta.md` (US-05). AC-5.4 (the guard test) is the one that makes the rest
survive contact with the next connector option someone adds.

## Dependencies

- Slices 02-04. This is a correctness gate over everything delta has shipped so far: until it lands, a
  query edit is knowingly unprotected on three connectors, which is why it runs before the remaining
  three rather than after them.

## Effort

~6h. Migration ~0.5h, fingerprint + storage ~1.5h, mode/reason plumbing ~1h, guard test ~1.5h, remaining
tests ~1.5h.

## Production data / dogfood moment

On `:5169` with real recorded history: change a team's query, watch the next cycle log
`mode=full | reason=configuration-changed`; then add a wait state, watch the next cycle log
`mode=delta | fetched=0`. Two log lines, same day, and they are the acceptance for KPI-4.

## Pre-slice SPIKE

Not needed. The uncertainty is enumerability, and the guard test answers it inside the slice.

## Verdict

**Confirmed, and the hypothesis was wrong in the direction that mattered.** Recorded 2026-08-12 at slice
close, shipped as `5f2ac86c5`.

The hypothesis was *"the set of properties that shape a fetch can be enumerated in one place, and a guard
can keep it enumerated"*. Both halves hold. `FetchFingerprint.RegisteredProperties` is that place,
`FetchShapingPropertyGuardTest` walks every public settable property of the four types an operator can
edit and forces a decision on each, and the two consumers — the sync's mode decision and the save-time
purge — now read the same list through one function.

What the slice got wrong going in was the **size** of the set. AC-5.1 said seven properties, drawn from
what `PrepareQuery` is handed. The real membership is *what the query asks for* **union** *how the answer
is read into the stored record*, because a cheap cycle skips an unchanged record's whole derivation and
not merely its download. That is thirteen. A state mapping, a parent-override field, a portfolio's
feature-owner and size-estimate fields, and the connection's own field definitions and system all decide
what ends up stored without changing a byte of the query text. AC-5.1, AC-5.4 and ADR-140 all carried the
narrow wording and were all restated.

Two findings worth keeping:

- **The old purge was masking the whole feature.** A query, type, state or mapping edit tripped
  `WorkItemRelatedSettingsChanged`, emptied storage, and the resolver then answered `Full` on its "nothing
  stored" branch. Six of scenario 3's eight cases and the walking skeleton itself would have gone green
  against a fingerprint that did nothing at all. The narrowing had to land *before* anything measured the
  widened set, which is why the roadmap reordered the DISTILL handoff.
- **Narrowing the purge is the user-visible win, and it was never the headline.** The slice was scoped as
  "stop re-downloading for free", but what an operator actually notices is that editing a team's query no
  longer destroys that team's stored work items and their transition history. Only a connection change
  does — the one edit `removed = stored − fetched` cannot reconcile, because the same reference id on a
  different tracker is a different item.

**Verified manually** on a live Jira Cloud instance 2026-08-12: a fetch-shaping edit produces
`mode=Full | reason=configuration-changed`, a non-shaping edit stays `mode=Delta`, recorded history
survives a query edit, a connection change still starts from nothing, and an upgrading instance takes one
full cycle without being told configuration caused it.

**Owed at epic close, not here**: the release-note line for the narrowed purge. Release notes are drafted
at feature close from the epic, so this is the flag that it is owed, not a task for this slice.
