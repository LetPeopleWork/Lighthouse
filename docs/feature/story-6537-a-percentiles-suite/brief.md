# Product brief: story\-6537\-a\-percentiles\-suite

## Request

```text
Story 6537 (T10), suite 1: the PercentilesOverTime family. Move each rule its host tests prove to the lowest layer that can prove it (a unit test of the computing class, else the service port), or rewrite it there on a smaller input where the host row is slow in its history fill (the maintainer said yes to rewrites on 2026-11-13). Start with the pilot class Slice07WhereTheLimitsActuallyMovedTest. Behaviour-free: no production file changes.
```

## Work

M

```text
The backend Integration tier spends far less time re-proving percentile-history rules through a web host, and every rule still has a test that goes red when its production line breaks
```

### Rework

None.

## Promises

### P1

Outcome

```text
Each rule a PercentilesOverTime host test proves is proven by a test at the lowest layer that can prove it (a unit test of the computing class, else the service port), so the backend Integration tier takes less time
```

### P2

Outcome

```text
No rule loses its test: every removed host row has an id-table row naming the test that now proves its rule, and one deliberate break of that rule's production line turns that test red
```

### P3

Outcome

```text
No production file changes, the test-host guard lists do not grow, and at least one host row per route still proves the wiring
```

## Scope

None.

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
Pilot: the rules of Slice07WhereTheLimitsActuallyMovedTest (which limit families a team and a portfolio fill, the not-ready and zero-band gates, the retention edge as of a day, a refilled day reading as the watched day) are proven by unit tests of ProcessBehaviorSnapshotWriter and RetentionEdge; one host row stays for the limits route
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.

### S2

```text
The rules of Slice06EveryPercentileTabSpansTheSamePeriodTest (every day of the period filled, a longer look-back carrying an older slow stretch, an item counted at the age it had that day, a refilled age day matching the watched day, a portfolio filling cycle time and age, tabs not refilled on a switch, the age tab stopping at the last refresh, a blank day with nothing in flight) are proven by unit tests of the classes that compute them; one host row stays for the tab route
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.

### S3

```text
The rules of Slice05ReconstructCycleTimeHistoryTest (the filler walks every asked day within what the stored items support, caps a pass at 90 days and at its time budget, leaves quiet days blank on fill and on the daily recording, never rewrites a watched or backdated day, asks for nothing when the period is held, stands down for a restore and blocks a restore while it runs, and two copies filling one day leave one row) are proven by unit tests and one service-port scenario; three host rows stay for the read route, the read-fill-read loop and a refresh landing mid-fill
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.

### S4

```text
The rules of Slice10OnlyWhatTheTeamStillKeepsTest (the fill reaches back no further than the done items the team still keeps, for each family, for a team and a portfolio) and of Slice08NothingToShowTest (a team or portfolio with nothing finished, nothing in flight or no history gets no rows and the chart reads empty rather than zero) are proven by unit tests of the classes that compute them; neither class starts a web host any more
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.
