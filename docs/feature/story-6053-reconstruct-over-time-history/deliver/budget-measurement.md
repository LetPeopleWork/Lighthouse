# The wall-clock budget on a reconstruction pass

## The value, and where it comes from

`OverTimeHistoryFiller.LongestOnePassMayRun` is **10 seconds**.

It was not chosen from a throughput figure and does not move with the size of an instance. While a
pass is running, the database maintenance gate refuses a restore, a backup and a clear — so the
budget is the longest an operator who clicks Restore can be left pressing a button that does nothing
before the answer changes. Ten seconds is about as long as anyone waits at a control before deciding
it is broken. That question is about people, not about how many work items an instance holds, which
is why no measurement could have answered it.

A measurement answers a different and smaller question: how much history fits inside the budget. It
is recorded below because it tells us which of the two bounds — the ninety-day cap or the budget —
is the one that actually stops a pass on an instance we can see. It is not the justification for the
number.

## What was measured, and on what

| | |
|---|---|
| Instance | the story-6053 acceptance fixture: real ASP.NET host, real EF, real SQLite **file** |
| **n** | **201 work items, all belonging to one team**, one finished per day across the 200 days before the pinned instance day |
| Build | Debug, run under the NUnit test host |
| Write path | the shipped `PercentileSnapshotWriter`, wrapped by the fixture's latch decorator |
| Method | the budget handed to one pass was set to 1 second and the number of days that pass wrote was read off the filler's own log line |
| Result | **43 days written in 1 second**, i.e. ~23 ms per day for the four cycle-time percentile series |

This is the largest instance genuinely available here, and it is small: **201 work items and one
team**. It is not a production instance, it is not a production build, and it carries a test
decorator on the write path. The figure above must not be read as a production budget, and nothing
here supports extrapolating it to an instance of another size — the per-day cost depends on how many
items fall inside each trailing window, which is exactly what changes with instance size.

Earlier notes in this story's context carried figures of 5–6 s and 20–25 s for a full window. Those
were arithmetic on a ~15 ms/day base, not measurements, and they are superseded by the line above
rather than confirmed by it.

## What the measurement says

On a fixture this size a pass gets through its whole ninety-day allowance in roughly two seconds, so
the **cap** is what stops it and the budget is never reached. The budget exists for the instance we
cannot see: the one where a day costs enough that ninety of them would hold the maintenance gate
shut for a minute.

If a real instance turns out much slower than this, that does **not** change the constant. It changes
how many visits a full window takes, which is the entire reason the walk is resumable: every day a
pass has written stays written, a pass that hands the rest of its window back loses nothing, and the
next chart load asks for whatever is still missing. A large instance therefore fills its charts in
more slowly rather than holding an operator longer — the direction this is meant to give way in.

## Why it is not a setting

Neither the budget nor the ninety-day cap is an `AppSettings` row. A knob invites tuning, and there
is nothing here a user could usefully tune: raising the budget buys nothing except a longer refusal
on the Restore button, and lowering it only spreads the same work over more visits. Both live as
constants in the code that obeys them — the budget in `OverTimeHistoryFiller`, the cap in
`OverTimeGapReconciler`, which is the one place a visit's ask is bounded.
