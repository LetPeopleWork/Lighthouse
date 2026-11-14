# Slice 16 — One setting says how fresh the data must be

Epic #6474 · **visible** (Settings → Organization → Refresh & ordering) · ≤ 1 day · depends on: 12 · delivered
right after 12 · ADO Story #6627

Added by the maintainer on 2026-11-14. The slice number is new; the delivery order puts it after slice 12.

## Problem

Settings → Organization → Refresh & ordering has two refreshes, "Team" and "Feature", each with three numbers in
minutes: Interval (60), Refresh After (180) and Start Delay (10 and 15). Six numbers express one wish: "keep my data
fresh". Interval and Refresh After interact in a way few people can predict: with 60 and 180, data can be about 240
minutes old. Start Delay is tuning for whoever runs the server, not an organisation decision.

After slice 12 there is one refresh per unit, with steps chosen by level. The two refresh types the settings name no
longer match the code beneath them, and #6455 would need a third set of numbers for a third level.

## Learning hypothesis

One value ("data is at most N minutes old") is enough for every organisation, at any number of levels, because a
forecast is now cheap when nothing changed. **Disproved if** an organisation needs different freshness per level
(then a per-level value comes with #6455, and this slice still removes Start Delay and the two types).

## IN

- One organisation setting: the oldest the data of any unit may be, in minutes. Every unit on every level is
  refreshed so that its data never gets older than that.
- Upgrade: the new value is taken from today's settings so that no unit refreshes less often than before.
  The exact rule (for example the smaller of the two "Interval + Refresh After" sums) is fixed at DESIGN.
- Start Delay leaves the Organization page. It moves to System or to appsettings; DESIGN decides where.

## OUT

- A value per level (only with #6455, and only if the hypothesis is disproved).
- Changes to the update queue or to what a refresh does (slice 12 owns that).

## UI sketch (draft, for the walk-through at the start of DELIVER)

```
Settings → Organization → Refresh & ordering

  Keep data fresh
  ┌───────────────────────────────────────────────────────────┐
  │ Refresh data that is older than   [ 180 ] minutes         │
  │ Every Team and Portfolio is refreshed before its data     │
  │ gets older than this.                                     │
  └───────────────────────────────────────────────────────────┘
  (Feature ordering policy unchanged below)
```

Loading and failure looks: reuse the page's existing ones.

## Acceptance criteria

- [ ] Organization shows one freshness value instead of two rows of three numbers.
- [ ] After the upgrade, no Team or Portfolio refreshes less often than before.
- [ ] No unit's data gets older than the set value while the server runs.
- [ ] Start Delay is no longer an organisation setting, and the server still waits before its first refresh.
- [ ] CLI and MCP (if they read or write these settings) follow the new shape.
- [ ] Slice 01 check green.

## Open for the start of DELIVER

- Usage data: is there an event that would show this is used? Likely N/A, because it is a setting, not a flow.
- Where Start Delay goes (System or appsettings).
- Docs: `docs/` pages that describe the six numbers change at finalize.
