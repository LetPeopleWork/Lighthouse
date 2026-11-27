## Per page (median of runs)

| Page | Requests | Distinct endpoints | Repeated calls | Shell calls | Page calls | Chain depth (slow 4G) | Data done fast (ms) | Data done slow 4G (ms) | Data done slow 3G (ms) | JS boot slow 4G (ms) | API KB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| team-features | 8 | 6 | 2 | 4 | 4 | 2 | 396 | 5723 | 0 | 5254 | 21 |
| team-forecasts | 8 | 6 | 2 | 4 | 4 | 2 | 208 | 5900 | 0 | 5290 | 40 |
| team-metrics | 32 | 28 | 3 | 4 | 28 | 2 | 799 | 6696 | 0 | 5252 | 151 |
| team-refinement | 8 | 6 | 2 | 4 | 4 | 2 | 179 | 5858 | 0 | 5251 | 40 |
| team-reports | 6 | 5 | 1 | 2 | 4 | 2 | 74 | 5610 | 0 | 5253 | 5 |
| team-settings | 14 | 9 | 5 | 4 | 10 | 2 | 212 | 6196 | 0 | 5252 | 42 |
| portfolio-features | 11 | 8 | 2 | 4 | 7 | 4 | 151 | 8472 | 0 | 7395 | 83 |
| portfolio-deliveries | 11 | 8 | 2 | 4 | 7 | 4 | 110 | 8284 | 0 | 7392 | 50 |
| portfolio-metrics | 31 | 26 | 3 | 4 | 27 | 4 | 215 | 9039 | 0 | 7393 | 134 |
| portfolio-reports | 9 | 7 | 1 | 2 | 7 | 3 | 97 | 8446 | 0 | 7401 | 47 |
| portfolio-settings | 21 | 11 | 9 | 7 | 14 | 5 | 265 | 8890 | 0 | 7393 | 61 |
| features | 4 | 4 | 0 | 2 | 2 | 1 | 71 | 879 | 0 | 457 | 52 |
| reports | 4 | 4 | 0 | 2 | 2 | 1 | 32 | 1125 | 0 | 939 | 3 |
| settings-organisation | 5 | 4 | 1 | 2 | 3 | 1 | 354 | 166 | 0 | 11 | 7 |
| settings-system | 5 | 4 | 1 | 2 | 3 | 1 | 360 | 168 | 0 | 9 | 7 |

## App-shell endpoints (called on every page)

- `GET /api/portfolios`
- `GET /api/teams`

## Repeated calls (same URL more than once in one load, fast profile, first run)

| Endpoint | Extra calls summed over all pages |
|---|---|
| `GET /api/teams` | 10 |
| `GET /api/portfolios` | 10 |
| `GET /api/authorization/my-summary` | 8 |
| `GET /api/teams/{id}/settings` | 6 |
| `GET /api/portfolios/{id}/settings` | 2 |
| `GET /api/teams/{id}/metrics/workItemAgePercentiles` | 1 |
| `GET /api/teams/{id}/forecast-filter/schema` | 1 |
| `GET /api/portfolios/{id}/metrics/workItemAgePercentiles` | 1 |
| `GET /api/suggestions/states/projects` | 1 |

## Longest chain per page (slow 4G, first run)

- **team-features** (2): `GET /api/teams` → `GET /api/portfolios`
- **team-forecasts** (2): `GET /api/teams` → `GET /api/portfolios`
- **team-metrics** (1): `GET /api/teams`
- **team-refinement** (2): `GET /api/teams` → `GET /api/portfolios`
- **team-reports** (2): `GET /api/teams` → `GET /api/authorization/my-summary`
- **team-settings** (2): `GET /api/authorization/my-summary` → `GET /api/teams/{id}/settings`
- **portfolio-features** (4): `GET /api/authorization/my-summary` → `GET /api/portfolios/{id}/settings` → `GET /api/teams/{id}/settings` → `GET /api/teams`
- **portfolio-deliveries** (4): `GET /api/authorization/my-summary` → `GET /api/portfolios/{id}/settings` → `GET /api/teams/{id}/settings` → `GET /api/teams`
- **portfolio-metrics** (4): `GET /api/authorization/my-summary` → `GET /api/portfolios/{id}/settings` → `GET /api/teams/{id}/settings` → `GET /api/teams`
- **portfolio-reports** (3): `GET /api/authorization/my-summary` → `GET /api/portfolios/{id}/settings` → `GET /api/teams/{id}/settings`
- **portfolio-settings** (5): `GET /api/authorization/my-summary` → `GET /api/portfolios/{id}/settings` → `GET /api/teams/{id}/settings` → `GET /api/teams` → `GET /api/teams`
- **features** (1): `GET /api/teams`
- **reports** (1): `GET /api/authorization/my-summary`
- **settings-organisation** (1): `GET /api/authorization/my-summary`
- **settings-system** (1): `GET /api/authorization/my-summary`
