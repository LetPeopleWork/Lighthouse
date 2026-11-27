## Per page (median of runs)

| Page | Requests | Distinct endpoints | Repeated calls | Shell calls | Page calls | Chain depth (slow 4G) | Data done fast (ms) | Data done slow 4G (ms) | Data done slow 3G (ms) | JS boot slow 4G (ms) | API KB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| overview | 34 | 23 | 10 | 29 | 5 | 5 | 950 | 12839 | 50625 | 5854 | 380 |
| team-features | 29 | 22 | 7 | 26 | 3 | 5 | 519 | 17428 | 52127 | 5869 | 387 |
| team-forecasts | 29 | 22 | 7 | 26 | 3 | 3 | 653 | 10243 | 27394 | 5879 | 406 |
| team-metrics | 53 | 44 | 8 | 26 | 27 | 3 | 732 | 11922 | 31671 | 5860 | 515 |
| team-refinement | 29 | 22 | 7 | 26 | 3 | 3 | 411 | 7708 | 34690 | 5874 | 406 |
| team-reports | 27 | 21 | 6 | 25 | 2 | 5 | 915 | 9691 | 69696 | 5865 | 371 |
| team-settings | 35 | 25 | 10 | 28 | 7 | 5 | 619 | 17573 | 70637 | 5882 | 408 |
| portfolio-features | 32 | 24 | 7 | 26 | 6 | 5 | 856 | 19921 | 80127 | 5879 | 450 |
| portfolio-deliveries | 32 | 24 | 7 | 26 | 6 | 5 | 425 | 19790 | 79773 | 5908 | 416 |
| portfolio-metrics | 52 | 42 | 8 | 26 | 26 | 5 | 651 | 20443 | 82206 | 5911 | 500 |
| portfolio-reports | 30 | 23 | 6 | 25 | 5 | 5 | 477 | 19741 | 79425 | 5881 | 413 |
| portfolio-settings | 42 | 27 | 14 | 31 | 11 | 3 | 1209 | 9656 | 36007 | 5918 | 427 |
| features | 25 | 21 | 4 | 23 | 2 | 4 | 391 | 14667 | 27101 | 5864 | 418 |
| reports | 27 | 20 | 7 | 26 | 1 | 5 | 478 | 12990 | 51650 | 5853 | 373 |
| settings-organisation | 28 | 19 | 9 | 28 | 0 | 5 | 494 | 14990 | 59240 | 5897 | 377 |
| settings-system | 28 | 20 | 8 | 27 | 1 | 5 | 936 | 15092 | 59442 | 5883 | 376 |

## App-shell endpoints (called on every page)

- `GET /api/auth/mode`
- `GET /api/authorization/my-summary`
- `GET /api/connectionhealth`
- `GET /api/license`
- `GET /api/logs/problems`
- `GET /api/portfolios`
- `GET /api/surveynudge`
- `GET /api/systeminfo`
- `GET /api/teams`
- `GET /api/terminology/all`
- `GET /api/update/status`
- `GET /api/update/tasks`
- `GET /api/usagedata/state`
- `GET /api/version/current`
- `GET /api/version/distribution`
- `GET /api/version/hasupdate`
- `GET /api/version/new`
- `GET /api/version/updateSupported`
- `POST /api/updateNotificationHub/negotiate`

## Repeated calls (same URL more than once in one load, fast profile, first run)

| Endpoint | Extra calls summed over all pages |
|---|---|
| `GET /api/authorization/my-summary` | 72 |
| `GET /api/teams` | 17 |
| `GET /api/license` | 16 |
| `GET /api/portfolios` | 16 |
| `GET /api/teams/{id}/settings` | 6 |
| `GET /api/portfolios/{id}/settings` | 2 |
| `GET /api/teams/{id}/reports` | 1 |
| `GET /api/teams/{id}/metrics/workItemAgePercentiles` | 1 |
| `GET /api/teams/{id}/forecast-filter/schema` | 1 |
| `GET /api/portfolios/{id}/metrics/workItemAgePercentiles` | 1 |
| `GET /api/suggestions/states/projects` | 1 |
| `GET /api/terminology/all` | 1 |

## Longest chain per page (slow 4G, first run)

- **overview** (5): `GET /api/license` → `GET /api/portfolios` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **team-features** (5): `GET /api/version/current` → `GET /api/version/distribution` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **team-forecasts** (3): `GET /api/authorization/my-summary` → `GET /api/connectionhealth` → `GET /api/logs/problems`
- **team-metrics** (5): `GET /api/version/current` → `GET /api/version/distribution` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **team-refinement** (3): `GET /api/update/tasks` → `GET /api/connectionhealth` → `GET /api/logs/problems`
- **team-reports** (3): `GET /api/authorization/my-summary` → `GET /api/connectionhealth` → `GET /api/logs/problems`
- **team-settings** (5): `GET /api/version/current` → `GET /api/version/distribution` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **portfolio-features** (5): `GET /api/license` → `GET /api/teams` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **portfolio-deliveries** (5): `GET /api/license` → `GET /api/teams` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **portfolio-metrics** (5): `GET /api/license` → `GET /api/teams` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **portfolio-reports** (5): `GET /api/license` → `GET /api/teams` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **portfolio-settings** (3): `GET /api/authorization/my-summary` → `GET /api/connectionhealth` → `GET /api/logs/problems`
- **features** (4): `GET /api/auth/mode` → `GET /api/authorization/my-summary` → `GET /api/portfolios` → `GET /api/version/hasupdate`
- **reports** (5): `GET /api/authorization/my-summary` → `GET /api/portfolios` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **settings-organisation** (6): `GET /api/auth/mode` → `GET /api/authorization/my-summary` → `GET /api/version/distribution` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
- **settings-system** (6): `GET /api/auth/mode` → `GET /api/authorization/my-summary` → `GET /api/version/distribution` → `GET /api/version/hasupdate` → `GET /api/version/new` → `GET /api/version/updateSupported`
