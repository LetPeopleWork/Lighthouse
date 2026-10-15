# Recommendation — story-6307-refresh-on-additional-fields-change

DIVERGE wave · Flux · 2026-10-15 · revision 2 (after peer review round 1) · hands off to DISCUSS
(nw-product-owner). Supporting artifacts: `diverge/job-analysis.md`, `diverge/competitive-research.md`,
`diverge/options-raw.md`, `diverge/taste-evaluation.md`, `diverge/review.yaml`.

**Job.** When I add or change a field on a work tracking connection, I want every Team and Portfolio that reads
through it to hold that field's values before I build on it — or to be told plainly that they do not yet — so I
can put a rule on the field and trust what it matches.

**The fact that shapes everything.** Saving the connection already commits every Team and Portfolio on it to one
full fetch: the connection's field definitions are part of the fetch fingerprint (`FetchFingerprint.cs:26-47`),
so each owner's *next* cycle is full. With default settings that next cycle is up to ~4 hours away
(`AppSettingSeeder.cs:28-32`, `UpdateServiceBase.cs:305-337`), and nothing says it is pending. So the choice is
not *whether* to pay for the fetch, only **when** it happens, **who picks the moment**, and **whether anyone can
see it waiting**.

**The two accepted decisions in the way.** ADR-030 rules that an expensive dependent reload "never auto-fires …
the user controls when the heavy recompute runs", and the operation it ruled on is a Team update (the forecast
filter's "Reload throughput now", `ForecastSettingsComponent.tsx:81-82`). Yet every other Team and Portfolio
settings save fires that same update automatically (`useModifySettings.ts:252-254`). ADR-140 kept the connection
save passive so that it carries no fan-out write and no partial-failure question. ADR-030 also rejected "Always auto-refresh" because a
recompute on every debounced keystroke repeats heavy work; that reason does not transfer to one explicit Save of an
already-owed fetch, but its "no surprise long operation" does, and a whole connection's owners is a larger single
operation than one Team (`diverge/taste-evaluation.md` §0). What would be amended is ADR-030's Decision, "Split by
recompute cost". Lighthouse already lives with both rulings on Team updates; which one governs a connection save is a maintainer decision, and this
recommendation is conditional on it.

## Top 3

Ranked by weighted score. "Option N" everywhere in this file is the curated number from `options-raw.md`
(1 update on save, 2 waiting marker, 3 rules tell the truth, 4 the rule asks for its field).

### Rank 1 — Option 1: Update the connection's Teams and Portfolios on save — 4.25 (gate: ADR-030)

- **Why it scores well.** No new concept (it is what Team and Portfolio saves already do); one action; it removes
  the "next refresh" Alert (`ModifyConnectionSettings.tsx:674-680`) instead of adding to it; its parts exist
  (`TriggerUpdate`, owners by connection id, the pure fingerprint, the coalescing queue). It is the only option that
  fully closes O1 (time to populated).
- **Core trade-off.** The admin does not pick the moment. N full fetches start straight after the save, one after
  another on the sequential queue, instead of spread over the next hours.
- **Partial failure (ADR-140's question).** No N-owner write happens at save; the connection save commits alone
  and each owner is then its own queue item. A failed update is reported and recorded for that owner
  (`UpdateServiceBase.cs:25-49`); its fingerprint is written only after its own fetch completes
  (`WorkItemService.cs:61-62`, `:91-92`), so it stays mismatched and its next scheduled cycle is full again.
  A partial failure leaves the failed owners where they are today, each visible as its own failed row.
- **Key risk.** On a large on-premise Jira, an admin editing a field at 10:00 sends every owner on that connection
  through a full fetch in business hours — the "surprise long operation" ADR-030 names. The total load is unchanged
  (each owner owed it), the queue never hits the tracker in parallel, and each row can be stopped from the Task
  Manager; none of that gives the admin the choice ADR-030 promised.
- **Timing variants** (sub-decisions, same mechanism): *now* (as described); *ask first* (a dialog at save — ADR-140
  rejected asking about the fetch *mode*, this asks about *timing*); *next tick* (treat a changed owner as due at
  the next scheduler pass, ≤ 60 min, no UI).
- **Hire criteria.** The admin wants the field to work, not a decision about load — the case in Bug #6306.

### Rank 2 — Option 4: The rule that needs the field asks for it — 3.73 (gate: ADR-030 on the forecast filter)

- **Why it scores well.** No new screen; only owners that actually read the field pay for a fetch now.
- **Core trade-off.** Everything that reads the field outside a rule (a Portfolio's owner, size or dependency field
  reference, write-back mappings, columns) still waits for the schedule, silently.
- **Key risk.** It auto-fires a Team update on the forecast filter surface, the very surface ADR-030 kept one-click.
- **Hire criteria.** Instances where additional fields exist mostly to drive rules.

### Rank 3 — Option 2: "Waiting for an update" marker with one-click remedy — 3.40 (no gate)

- **Why it scores well.** The highest-scoring option consistent with both ADR-030 and ADR-140. It makes the
  pending state visible wherever it matters (O4), derived from the fingerprint with no new storage, and the admin
  picks the moment with one in-place click — including "Update all of them" on the connection page itself.
- **Core trade-off.** Two new concepts and three surfaces to design, each with a loading and a failure look.
- **Key risk.** Markers get ignored, as Jira's re-index banner does; the gap stays open until someone acts.
- **Hire criteria.** A large-instance operator who schedules heavy fetches outside business hours.

## Recommended option

**Option 1 — Update the connection's Teams and Portfolios on save, conditional on the maintainer ruling at
DISCUSS that a connection save follows the Team-settings precedent (save ⇒ update) rather than ADR-030's
"expensive ⇒ intentional" rule, recorded as an amendment to ADR-030.** It leads the matrix by 0.52 and stays first
under the default preset (4.40) and with its Desirability, Feasibility and Viability all downgraded (4.00). It closes the time
gap and the forgotten step without a new concept and with machinery that exists, and it keeps the two cost
outcomes already served: one full fetch per owner per edit (O5), and nothing queued for an edit that moves no
fingerprint, such as a rename of the connection or a credential rotation (O6), because the trigger is "this
owner's fingerprint moved", not "the connection was saved". Its partial-failure behaviour is defined by existing
machinery (above).

**If the maintainer upholds ADR-030 for this case, proceed with Option 2** (the waiting marker with one click). It
is the highest-scoring option that needs no accepted decision overturned; Option 4 is skipped because it carries
the same ADR-030 conflict on the forecast filter with less coverage than Option 1.

**Flagged weakness (both paths).** Until the updates finish, a rule built on the new field still reads empty. Option
1 shrinks the window from hours to minutes; Option 2 leaves it to the admin. Neither closes O2. If DISCUSS finds the
window harmful, "rules tell the truth about unfetched fields" (3.23) is the complementary follow-up.

## Dissenting case

**By score: Option 4, the rule that needs the field asks for it (3.73, 0.52 behind).** It spends less tracker load
up front by fetching only for owners that read the field through a rule. It loses on coverage: every other reader
of the field still waits silently, and it carries the same ADR-030 conflict as Option 1.

**By principle: Option 2, the waiting marker (3.40).** The live alternative, not a footnote: if "the admin chooses
when an expensive update runs" is a product principle rather than a rule for one settings surface, Option 2 is the
answer and Options 1 and 4 are out.

## Decision for DISCUSS

**Proceed with Option 1 — when a connection's additional fields change, queue an update of every Team and Portfolio
on that connection whose fetch fingerprint moved, and say so in place of today's "next refresh" Alert — assuming
(a) the maintainer explicitly sets ADR-030's "expensive ⇒ intentional" aside for connection saves and records that
as an ADR-030 amendment, and (b) starting those already-owed full fetches immediately, one after another on the
existing queue, is an acceptable load on a large on-premise tracker. If (a) is refused, proceed with Option 2.**

## Open questions DISCUSS must decide

1. **ADR-030 vs the Team-settings precedent** (gate for Option 1). Rule which governs a connection save; record it.
2. **ADR-140's passive invalidation.** Accept a save-time fan-out of enqueues (not writes), with partial failure
   handled per owner as described. Amend ADR-140's "A connection change fans out" consequence accordingly.
3. **Timing variant** if Option 1: now, ask first, or next tick.
4. **Trigger scope.** Only additional-field edits, or every fingerprint-moving connection edit (the field set, the
   work tracking system)? Recommended: "any owner whose fingerprint moved", which excludes credentials and names by
   construction. Wider still — every fetch-shaping save on Team and Portfolio, moved server-side — is separate.
5. **Display-name-only edits.** The fingerprint renders a field's `DisplayName` (`FetchFingerprint.cs:117-131`), so
   renaming a field's label already costs every owner a full fetch on its next cycle, and under Option 1 would cost
   it immediately. Keep, or narrow the fingerprint to what changes the fetch (`Reference`, id)? The latter touches
   ADR-140's guard test.
6. **Predefined-field auto-registration.** The connection GET can add or update a predefined field and save
   (`WorkTrackingSystemConnectionController.cs:34-73`). Must that path fan out too, or only the admin's PUT?
7. **Licensing.** "Update All" is premium (`TeamsController.cs:72`, `PortfoliosController.cs:62`); a single owner's
   update is not. Is a connection-scoped fan-out (or Option 2's "Update all of them") premium-gated? Recommended: no —
   it follows through on a save the community edition can make (one additional field).
8. **RBAC.** Editing a connection is SystemAdmin only (`WorkTrackingSystemConnectionController.cs:76`), and a
   SystemAdmin may already update every owner. Confirm no new permission. For Option 2, who may press "Update now"
   on a single owner's marker (today `TeamWrite` on the Team endpoint, `TeamController.cs:75-76`)?
9. **Large instances.** Tens of owners per connection is the stated scale (ADR-140). Is a threshold wanted above
   which Option 1 asks instead, or is the Task Manager's per-row stop enough?
10. **Feedback copy and placement.** What replaces the Alert at `ModifyConnectionSettings.tsx:674-680`, and what the
    admin sees after Save (the page navigates to `/` today). UI: needs a sketch and the maintainer's yes before
    DELIVER, in Terminology (`Teams`, `Portfolios`, `Work Items`).
11. **Documentation.** `docs/settings/configuration.md:81` and the Additional Fields sections of
    `docs/concepts/worktrackingsystems/jira.md` and `azuredevops.md` must say what now happens on save.
12. **Usage data.** For DEVOPS to answer (event or "N/A, because …"); not designed here.
13. **The remaining window (O2).** Whether "rules tell the truth about unfetched fields" is wanted as a follow-up Story.
