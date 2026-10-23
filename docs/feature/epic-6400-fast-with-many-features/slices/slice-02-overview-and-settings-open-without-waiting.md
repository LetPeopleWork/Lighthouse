# Slice 02: The Overview and Settings open without waiting on every Feature (US-02)

**Goal:** the Overview lists Portfolios and Teams, and Settings opens, in under 1 s on an instance with ~3,000
Features, because the lists read names and counts, one Portfolio's page reads one Portfolio, and the licence count
is asked once per page.

**Job:** `job-reach-and-read-what-i-came-for-on-a-large-instance`. **Moves:** K1 idle; the Settings half only if Task
01a showed the server held Settings back (A12).

## IN scope

- `GET /portfolios` and `GET /teams` read what their bodies show, behind **unchanged** bodies (D20, C6).
- Reading one Portfolio filters in the database (`GetById` and its callers on detail pages); new reader methods per
  D28.
- One licence-count answer per page, shared by every `useLicenseRestrictions()` consumer; decisions unchanged with
  RBAC on and off (D30).

## OUT of scope

- A cached or staggered shell (Option 2), stale-while-revalidate beyond the shared count.
- The forecast gather's own Portfolio read (slice 05).
- Changing 6264's planned ports (S05d stays 6264's).

## Learning hypothesis

- **Disproves** "scaffold reads hold the Overview and Settings back" if Overview → Settings moves less than 30% from
  the 01a baseline.
- **Confirms** it if both K1 readings drop under 1 s idle, and a Portfolio's own read takes the same time with or
  without the other five Portfolios (± 15%).

## Acceptance criteria

US-02 AC-2.1 to AC-2.7 in `feature-delta.md`: lists < 1 s; Settings < 1 s; one-Portfolio read scale-invariant;
licence decisions unchanged and asked once per page; a failed count never enables a gated action; bodies unchanged;
dev backup identical.

## Production data (D25)

Dev DB backup: Overview, Teams and Portfolio pages identical before and after; licence decisions identical.

## Looks and sketch

- Changes a fetch: **yes**, the licence count. DESIGN states its loading look (the gated action waits, never shown
  as allowed early) and failure look (D30). The Overview keeps its body spinner while its lists are on their way.
- UI sketch at DISTILL: **only if** DESIGN gives the count a look that does not exist today; else N/A, because the
  pages look as they do now.

## Dependencies

Task 01a (baseline). Swaps freely with 03 and 05 (D18, D27).

## Effort

About 1.5 days. Reference class: `FeaturePositionMap`'s projection, plus moving a hook onto TanStack Query
(`useFeatureChildItems`). If DESIGN finds it over 2 days, split the licence count into its own slice.

## Dogfood

Scaled instance: open the Overview, then System Settings, and compare with the baseline; dev backup: click through
every Portfolio and Team.
