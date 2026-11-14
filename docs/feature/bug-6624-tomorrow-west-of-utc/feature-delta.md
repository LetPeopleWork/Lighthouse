# Bug #6624 — Delivery dialog refuses tomorrow as a launch date west of UTC

## Maintainer decisions

- **Rigor profile (2026-11-14):** S1 `standard`. Sonnet for the acceptance designer and crafter; Haiku for the reviewers and the examiner.
- **UI sketch:** not needed. No visible change: same date field, same message. Only which day the rule accepts changes.

## Root cause

`isValidFutureDate` (`DeliveryGrid/deliveryFormRules.ts`) builds the picked day with `new Date("YYYY-MM-DD")`. That reads the
string as UTC midnight. West of UTC, this is the evening before in local time, so after `setHours(0, 0, 0, 0)` tomorrow
equals today and is refused. Confirmed on 2026-11-14: the existing rule tests pass under `TZ=Europe/Zurich` and their
"tomorrow" rows fail under `TZ=America/New_York`.

## Test design

The frontend suite pins `TZ=Europe/Zurich` (east of UTC), where the defect cannot show. The regression oracle runs in
its own Vitest project under `America/New_York`, in a process of its own, so the zone cannot leak into the Zurich-pinned
tests. Then `pnpm test` covers it as well.

## Checklist answers

- DEVOPS usage-data event: N/A, because this restores existing behaviour; no new feature to count.
- Context map: N/A. Delivery planning in the portfolio context; no new term, concept or event.
