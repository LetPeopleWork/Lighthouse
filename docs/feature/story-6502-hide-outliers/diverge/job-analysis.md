# Job analysis — Story 6502 "Hide Outliers"

Source: the maintainer's story text only. There is no DISCOVER artifact and no customer evidence behind it; where a
number below is a judgement it is marked **(estimate)**. The maintainer was not available during this run.

## Raw request (a solution, not a job)

"Visually hide the outliers on some charts. Show an indicator that there are outliers, on click show their numbers and
how much bigger they are. They still appear in View Data."

That names a solution (hide + indicator + popover). The job sits underneath.

## Five whys

1. Why hide outliers? A very old item in the cycle time scatterplot makes the chart hard to read.
2. Why is it hard to read? The axis stretches to the oldest item (`getMaxYAxisHeight` = 1.1 x the largest value), so the
   other dots and the percentile lines are squeezed into the bottom of the plot.
3. Why does that matter? The reader uses the chart to see where typical work lands against the percentile lines and the
   SLE, and has to explain that to someone else.
4. Why not simply drop the outliers? Because they are real: "while we want to keep outliers". They are the items to
   investigate, and the percentiles are computed with them.
5. Why does it need to be on the chart at all? Because a reader who cannot see that something was left out will trust a
   picture that is not the whole picture.

## Job statements

**Strategic job.** When I read a flow chart to judge how my work is going, I want the picture of normal work to stay
legible while the exceptional items stay known and one step away, so that I can act on both and explain the chart to
someone else without being misled by either.

- **Functional:** read where most Work Items land, and still know which ones did not and how far out they are.
- **Emotional:** feel confident that the chart is neither hiding a problem nor drowned by one item.
- **Social:** be able to put the chart in front of a stakeholder and not be asked "what is that dot at 600 days, and why
  is everything else flat?" or "what did you leave out?".

Level check: strategic (no chart type, no popover, no scale in the statement).

## Who

Flow coach, team lead, delivery lead (existing personas, `job-flow-coach-spot-pace-outliers` is the nearest existing
job in `docs/product/jobs.yaml`, about spotting in-flight outliers; this job is its counterpart for charts of finished
work and for chart legibility). No `jobs.yaml` edit was made in this run (shared doc); proposed entry text is the
strategic statement above.

## ODI outcome statements

Format: direction + metric + object + context. Importance/satisfaction are **(estimate)** on 1-5, from the story
wording and the code as it is today, not from users.

| # | Outcome | Imp | Sat | Gap |
|---|---|---|---|---|
| O1 | Minimise the time it takes to see where most closed Work Items fall against the percentile lines | 5 | 2 | 3 |
| O2 | Minimise the likelihood that an exceptional item goes unnoticed because it is off the plotted range | 5 | 3 | 2 |
| O3 | Minimise the likelihood that a reader takes a chart statistic (percentile, SLE share) to describe only what is drawn | 5 | 4 | 1 (a risk the fix must not create) |
| O4 | Minimise the time to find which items are outliers and how far beyond normal they are | 4 | 2 | 2 |
| O5 | Minimise the effort to explain to a stakeholder why an item is or is not on the chart | 4 | 2 | 2 |
| O6 | Minimise the likelihood that a hiding rule surprises the reader (an item vanishes with no sign) | 5 | 3 | 2 |

Under-served: O1 (the stated pain), then O4, O5, O6. O3 is a guard rail rather than a gap: the current chart satisfies
it, and any direction must keep it.

## Gate G1

- Job at strategic level, no feature reference in the statement: pass.
- At least 3 ODI statements: pass (6).
