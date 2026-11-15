# Product brief: story\-6648\-frontend\-clean\-as\-we\-go

## Request

```text
Story 6648: move the slowest frontend page and dialog tests down to functions, hooks and single components. Scope chosen by the maintainer on 2026-11-15: ReportsPage.removal.test.tsx, ReportsPage.combinedPdf.test.tsx and RefinementView.comments.test.tsx. Behaviour-free: tests change, production behaviour and pixels do not.
```

## Work

M

```text
The three slowest frontend page tests run faster while every rule they prove keeps a test that can fail
```

### Rework

None.

## Promises

### P1

Outcome

```text
Each of the three test files takes less per-test time than before, judged by the median of at least 4 main runs measured the same way as the baseline
```

### P2

Outcome

```text
No rule loses its test: every removed or moved test is mapped to the test that now proves it, and every moved rule is shown to fail when its production line is broken
```

### P3

Outcome

```text
Lighthouse behaves and looks exactly as before: no production behaviour or pixel changes, and the renders that remain keep the loading and failure looks the views have
```

## Scope

```text
Test files ReportsPage.removal.test.tsx, ReportsPage.combinedPdf.test.tsx, RefinementView.comments.test.tsx, the shared Reports test kit they import, and new function, hook or single-component test files their moved rules land in
```

```text
OverviewDashboard.test.tsx, ReportsTab.test.tsx, BaseMetricsView.loading.test.tsx and every other file on the offender table
```

```text
Any production behaviour or pixel change; a production file changes only through one named refactor commit the maintainer has said yes to
```

```text
testTimeout, retries, a longer budget, a new Vitest integration tier, real waits, or deleting a test without mapping its rule
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The removal of picked reports on the Reports page is proven mostly on the confirmation dialog and the removal hooks, with one page render per wiring left, and its test file runs faster
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
The combined PDF export of picked reports is proven with fewer page renders, the failure dialog's buttons on the dialog alone, and its test file runs faster
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
Which Work Item's comment log the refinement view shows, and how often it reads the log, is proven on a hook or function, with one render per wiring left, and its test file runs faster
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.
