# Phase 03-04 — the scenarios meant to prove the as-of anchor cannot fail as seeded

Step 03-04 was to thread an as-of day through the baseline check so that reconstructing a past
day judges a pinned reference stretch as of that day rather than as of today. The step named two
scenarios as its acceptance cover and said plainly that they are the only tests in the fixture
that can fail on the anchor. They cannot. Unskipped against unmodified production code, all three
scenarios pass:

    Failed: 0, Passed: 8, Skipped: 2, Total: 10

No production code was written. The tree is unchanged.

## Why they cannot fail

The cutoff check inside the baseline validation reads

    cutoffDate = anchor - doneItemsCutoffDays
    invalid when baselineStart < cutoffDate

The two scenarios pin the reference stretch to 240..150 days back and ask about the window 60..30
days back. The base fixture seeds owners with a 365-day retention window. So:

- as of today, the cutoff falls 365 days back, and the stretch starting 240 days back is inside it
- as of any reconstructed day in the window, the cutoff falls 425 to 395 days back, and the stretch
  is inside that too

Both anchors return the same verdict, so no arrangement of the anchor changes the outcome. The
end-date check is never in play either: the stretch ends 150 days back, before both anchors. This
is the shape the step itself warned about — a test in which today's anchor and the reconstructed
day's anchor agree cannot fail — and it turned out to describe the two tests nominated as evidence.

## The scenarios are well formed; only the seeded retention window is too wide

Re-seeding the fixture at 220 days and changing nothing else makes both pinned-baseline scenarios
fail, for the right reason and with the wording the step predicted:

    A fixed reference stretch still describes a process. Reporting nothing at all reads as
    "your data did not support it", which is a different and false statement.
    Expected: not <empty>  But was: <empty>

The retention windows that give this step real cover, holding the dates the scenarios already use:

- invalid as of today needs a window shorter than 240 days
- valid as of every reconstructed day, worst case the one 30 days back, needs at least 210

so anything from 210 up to but not including 240. The seeded history already reaches 250 days back,
so the reference stretch holds finished work and the refusal is genuinely the retention window
rather than an honestly empty stretch.

Choosing that number is a statement about what kind of owner the scenario describes, so it belongs
to whoever owns the scenarios rather than being adjusted in passing here.

## Why the seeded 365 is easy to miss

It is the product default: both a team and a portfolio start at 365 days. A reader checking the
fixture against the product will find it faithful and move on. The hazard belongs to owners who
narrow their retention window — the settings screen's own test uses 180 — or who pin a reference
stretch further back than their retention reaches. A default-configured owner with a stretch pinned
240 days back genuinely cannot hit it, which is worth saying in the scenario's own words so the
next reader does not repeat this investigation.

## What the threading would take, so it need not be worked out twice

The validation already accepts the anchor as a parameter; that part of the step is done. The four
places that hard-code today are the three chart builders on the shared metrics base and the
delivery-size chart on the portfolio service.

An added anchor has to default to today, and a nullable default is the only option because today
is not a compile-time constant. It travels from the two public chart methods on each metrics port
down into the builders, and the family descriptor the snapshot writer holds has to widen to carry
it. The day-filling entry point needs no new parameter — it already knows the day it is
reconstructing — so the structural rule that pins its signature stays satisfied.

The live recorder should leave the anchor unset rather than passing today explicitly. That is not
an evasion: it records today, so the default is exactly what it means. It also matters in practice,
because the existing recorder tests match the chart call with the anchor absent, and passing today
explicitly would quietly stop matching them.

One trap is not in the plan. Every one of these chart methods caches on a key built only from the
window start and end. Once the anchor can vary independently of the window, a live read and a
reconstruction that happen to share a window would serve each other's chart. The anchor has to join
the key.

## One neighbouring scenario is misnamed

The scenario asserting that a period with nothing to draw limits from reports no limits refuses
through the retention-window branch, not through having nothing to draw from. Its stretch is pinned
900 days back, well outside the 365-day window, so it is rejected before any data is read. It still
passes and its assertion is sound, and threading the anchor would not change it — 900 days back is
outside the reconstructed day's window too. Only the name describes a mechanism other than the one
it exercises.
