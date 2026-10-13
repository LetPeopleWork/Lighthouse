# Slice 02: Feature Size Percentiles on the Team

**Goal:** a Team's Flow Overview shows Feature Size Percentiles of the Team's share, with the trend and the
70th/85th status.

## IN scope
- The Team route `featureSizePercentilesInfo` in the Team Feature Size controller (DDD-7), mirroring the Portfolio's
  and not cached (DDD-5), inside the query-count budget test, and 404 without `TeamRead` (AFK-1): 50th/70th/85th/95th
  of the share (`Feature.SizeFor`) in Features closed in the range, and the trend against the previous period of the
  same length (D6). It reuses `BuildPercentiles` and `BuildFeatureSizePercentilesInfoDto`. `sizePercentiles` already
  shipped in slice 01.
- `getFeatureSizePercentilesInfo` joins `IFeatureSizeMetricsService`. Lift the `featureSizePercentiles` portfolio-only
  gate (DDD-8, DDD-9). The status rule (`computeFeatureSizeRag`) reads the active Features' shares from slice 01's data.
- The card is unchanged. Only its info (ⓘ) description differs on a Team: `Size distribution percentiles of this
  {Team}'s {Work Items} in completed {Features}, with trend comparison to the previous period.` (UI-3, D5). The
  existing no-data state when nothing closed in the range (AC-2.5). Loading and could-not-load looks come from
  `WidgetShell`, as on every Flow Overview widget. (amended: was "same wording as slice 01", superseded by UI-3, 2026-10-13)

## OUT of scope
- Feature Size PBC and Percentiles Over Time for Feature size on the Team.
- Any Team-level size target or Default Feature Size.

## Learning hypothesis
- **Disproves** "the Portfolio's 70th/85th rule fits a Team's share" if, on demo data and on the dev instance,
  most Teams sit at Act permanently because a few large shares stretch the tail.
- **Confirms** it if the status mostly reads Sustain and flags only the Features a coach would name anyway.

## Acceptance criteria
AC-2.1 … AC-2.6 in `../feature-delta.md` (US-02).

(amended: spread over two stories after the DISCUSS split, review C-01, 2026-10-13)
- US-02: AC-2.1, AC-2.2, AC-2.4, AC-2.6
- US-07: AC-2.3, AC-2.5

## Dependencies
Slice 01 (active-Feature shares and `sizePercentiles` feed the status rule). Slice 00 is ordered earlier but is not a
dependency. (amended 2026-10-13)

## Effort
About 3–4 hours: one endpoint on the existing owner-agnostic builders, the gate lift and the wording.
Reference class: the Team's Work Item Age percentiles widget.

## Dogfood
The dev instance (`:5169`, real history, one Team): read the Team's Feature Size Percentiles and its status
against the Features its coach would call large. Also check one demo Team.
