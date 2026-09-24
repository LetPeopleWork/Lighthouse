# Dogfood, phase 03 — process-behaviour limits across every family, NOT YET PERFORMED

This check has **not** been run. It is left to the user's own live check, which follows the next
steps of this story. Nothing below was observed; it describes what the check is, so that whoever
runs it reads the same thing off the chart that the acceptance scenarios promise.

The development instance on <http://localhost:5169> is the user's live instance, so the step that
closed phase 03 neither started a dev server nor touched the development database.

## What the check is

On Predictability - PBC Over Time, toggle every process-behaviour type the chart offers, at both
scopes:

- **Team** — five families: Throughput, Work Item Age, WIP, Cycle Time, Arrivals.
- **Portfolio** — six families: the same five, plus Feature Size, which only a portfolio has.

For each family, read the three dated limit lines (upper limit, average, lower limit) and record the
dated span before and after the chart has filled in: the first and last day that carries limits,
and how many days in between are missing. The expectation is that the gaps close up to the owner's
last refresh and no further, that no day reads a band of zero width at zero, and that opening the
same chart a second time changes nothing.

## What limits the coverage

The development database holds exactly one Team. The portfolio half of the check can therefore only
be read against portfolios whose work all comes from that one team, and a portfolio spread across
several teams is not represented there at all. Portfolio behaviour over more than one team is covered by the acceptance
scenarios, not by this check.
