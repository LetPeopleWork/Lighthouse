# Slice 02 — People are actually asked, once, and told the truth about when they will be asked again

> **Position confirmed — 2026-09-12.** Unchanged in content and unchanged in sequence: this runs
> **after** `slice-01c-the-event-pipe.md` and **before** slice 03 and slice 04, exactly as the original
> order had it. DESIGN briefly proposed moving slice 04 ahead of this one; the maintainer kept the
> original order, and the reasoning that put it there still holds — the event vocabulary should be
> chosen once there is a consenting population to spend it on, and this is the slice that creates one.
>
> **This slice now carries more weight than it did.** Slice 01c ships a real product event to a
> population that is only the dogfood instance plus whoever went looking for the footer icon, because
> the unprompted ask does not exist until here. **This is the slice that turns the pipe from a
> demonstrated mechanism into a measurement.** An empty dashboard between 01c and this slice is the
> expected state, not a fault.
>
> One thing also gets easier: AC-05.8 ("with the admin switch off, the dialog never appears") used to
> be asserted against the setting's absence. The gate that reads the optional feature exists from 01c
> onward, so it is now asserted against a real reader rather than a placeholder.
>
> **New in scope — `PruneStaleAsync` gets its owner here.** It has shipped with zero production callers
> (verified: interface, implementation and tests only), so the consent table currently grows one row
> per browser ever asked, without bound. **This is the slice that makes that growth real**, because
> before the unprompted ask rows accrue only from people who went hunting for a footer icon, and after
> it every browser that is shown the dialog writes one — including every refusal. Scheduling the prune
> belongs to the slice that creates the problem, not to the one that happened to introduce the method.

## Goal

The Usage Data dialog arrives unprompted at a sensible moment, once per browser, with an honest
statement of whether it will return — so that there is a consenting population to measure at all.

## IN scope

- Unprompted dialog on install age (2–3 days), per browser (US-05).
- Cadence: Yes → never asked again, on any tier. Premium No → never asked again. Community No →
  eligible again in ~3 months.
- The dialog says which of those applies to the reader, before they choose (AC-02.9).
- No "remind me later". Closing without choosing leaves the browser undecided and eligible next
  session.
- Session-level coordination with `SurveyNudge` so the two never appear together (D9).

## OUT of scope

- Bundling with the survey into one dialog. Explicitly refused — GDPR Art 7(4), consent bundled with
  an unrelated ask is not freely given (D9).
- Sharing state with `SurveyNudge`. Its state is instance-wide (S6); this is per browser. The
  cadence *arithmetic* is copied, the storage is not.
- The admin switch — slice 03. This slice must nonetheless honour it if it exists (AC-05.8), which
  it does not yet, so that AC is asserted against the setting's absence defaulting to "may ask".

## Learning hypothesis

**Disproves "people will opt in" if** uptake after 60 days is materially below 20%. That is the
number the entire Epic rests on: below it, the consenting population is too small to answer any
product question and slice 04's event vocabulary is not worth choosing.

**Disproves "a delayed, once-only ask is not experienced as a nag" if** community reports of a
repeated or unstoppable prompt appear within 90 days.

## Acceptance criteria

Per US-05 (AC-05.1…5.8) in `feature-delta.md`. The two that carry the slice:

- **AC-05.2** — once per *browser*, not once per instance. Two people on the same instance are each
  asked. This is what S6 does not give us for free.
- **AC-05.5** — a Community browser that declined is eligible again after ~3 months, **and the
  dialog said so at the time**. The promise and the behaviour are one acceptance criterion on
  purpose; they cannot be allowed to drift apart.

## Production-data acceptance

Uptake measured at the collector from real instances, not from the vendor's own. The vendor instance
is excluded from the uptake denominator — one instance that always consents would flatter it.

## Dogfood moment

Same day, on a second browser profile against the vendor instance: fast-forward the install age,
decline as Community, confirm the dialog said "we will ask again in a few months", confirm it does
not return in the same session, confirm the survey nudge does not appear alongside it.

## Dependencies

- Slices 01a and 01b shipped (consent record, dialog, indicator, gate and heartbeat all exist).
- Nothing new external.

## Effort

≤ 1 day. The dialog and the consent record already exist; this slice adds eligibility arithmetic, a
per-browser "asked at" record, and one coordination rule.

## Reference class

`SurveyNudge` itself — `nudgeEligibility.ts` plus the cadence arithmetic in `AppSettingService` is
almost exactly this problem, solved once already at instance scope. The delta is the scope change to
per-browser, which is where the estimate risk sits.

## Watch

Two Community-only prompts on different clocks (survey at 14 days / 6 months, usage data at 2–3 days
/ 3 months) means collision is the normal case, not the edge case. If the coordination rule turns out
to need more than "not in the same session", that is a signal the two should share one scheduler —
raise it rather than adding a second ad-hoc rule.
