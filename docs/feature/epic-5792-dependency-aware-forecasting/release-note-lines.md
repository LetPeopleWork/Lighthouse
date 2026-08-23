# Release note lines — Epic #5792 "Dependency-Aware Forecasting"

Draft for the next release. Slices 00, 01 and 02 are all on `main`; ADO Stories #5826/#5784/#5785 are
Closed and Epic #5792 is Resolved. #5792 carries the `Release Notes` and `Premium` tags.

Ships in the same release as Epic #4365, which is what makes dependencies visible in the first place.
**#4365's copy goes first** — a reader has to know the column exists before being told the dates now
use it.

Terminology: *Feature*, *Work Item*, *Portfolio* and *Team* render as whatever the instance calls
them. Keep the seeded defaults in this copy.

---

## Your Forecast Now Waits For What Your Work Waits For

You could already see that a Feature was waiting on another one. The date sitting next to it still
pretended otherwise — it was built from your Team's throughput alone, as though the work could start
tomorrow. So the number everybody actually plans with was the one number that ignored the thing
everybody was worried about, and the gap only showed up in the meeting where the date slipped.

That is fixed. A Feature is no longer forecast to be worked on until everything it waits on has
finished. Not adjusted afterwards, not flagged for you to correct by hand — the wait happens inside
every one of the ten thousand simulated runs the forecast is built from.

**Across Teams, too.** Every Team now advances on one shared clock inside a run, so a Feature your
platform Team delivers can sit behind a Feature your product Team has not finished yet. That was the
hard part, and it is the case that actually hurts.

### Two Features move, not one

The Feature that is waiting moves out. That much you would expect. The one you would not expect:

> Three Features, one Team, Feature WIP of 2. Nothing waiting: **17, 13, 22** working days.
> Record that the second waits on the first: **16, 22, 20**.

The Feature below the waiting one came **in by two days**. Your Team works on the top *Feature WIP*
Features in parallel; a Feature that cannot be started yet stops occupying one of those places, and the
next Feature that *can* be started moves up into it. Waiting is not only a cost — knowing about it
frees capacity that was being modelled as spent.

### What it does not do

**A shared clock shares time, never capacity.** Each Team still draws its own throughput from its own
history. Sitting on the same clock as a faster Team has never made a Team faster and does not now.

**Lighthouse still will not let you author a dependency**, and it never will. It reads what your
tracker already says.

**Some waits are still left out**, and every one of them says so on the Feature rather than going
quiet: the Feature it waits on sits in no Portfolio they share, the two are waiting on each other,
the Feature it waits on has no measured delivery to forecast from, or the Portfolio is set to ignore
dependencies. That last one is now a genuinely useful question to ask — flip it and the dates read as
they would if none of those links existed, which is the closest thing to *what would this plan look
like if we broke the dependencies* that a forecast can give you.

### What you need

Dependency-aware dates need a **premium licence**. Reading dependencies and showing them stays on every
instance, community edition included.

Without a licence the column and its warnings behave exactly as they do with one, and the dates are the
ones you would get if no dependency had been recorded. Where a wait would have moved a date and only
the licence is in the way, the Feature says so — and only there. A wait a licensed instance would leave
out anyway is never dressed up as one a licence would buy you.

See [How Lighthouse Forecasts](https://docs.lighthouse.letpeople.work/concepts/howlighthouseforecasts.html#dependencies)
for the full picture.

## One Forecast Per Portfolio Per Refresh

A round of *Refresh everything* used to forecast a Portfolio once for every Team that delivers into it,
then write the result back to your tracker each time. On a Portfolio with six Teams that is six
forecasts and six writes to land on one delivery date.

It is now one forecast and one write per Portfolio per round, and the delivery date is announced once.
Refreshes finish sooner, your tracker sees a fraction of the traffic, and a Portfolio refresh that
overlaps a Team refresh settles on a single date instead of racing to the last one written.

---

## Open before this ships

- [ ] Re-run the manual verification walkthrough after slice 02 — case D's expectation inverts. See
      `docs/feature/epic-5792-dependency-aware-forecasting/verification/manual-verification-walkthrough.md`.
- [ ] Confirm the three-Feature example above against the shipped build. The numbers are the KPI-2
      measurement taken during slice 01; slice 02 is meant to leave a single-Team case exactly where it
      was, but the copy quotes them as current.
- [ ] Screenshots: the Features view's Dependencies and Warnings columns on a premium instance.
      Deliberately not done in this pass.
