# Bug #6705 — Total Work Item Age Over Time shows NaN

## Maintainer decisions (2026-11-26)

- **Fix shape:** the server sends `StartedDate ?? CreatedDate` in `WorkItemDto` for an item in a Doing
  state (the rule `WorkItemAge` already uses); the frontend should not need to care. To Do items keep no
  start date; Done items always have one from the connector.
- **Frontend safety net:** where a start date can still be missing, the Total Work Item Age Over Time chart
  skips that item: it leaves it out of the day's total and item count, still totals the others and draws a
  proper line. Never NaN.
- **UI sketch:** none. The chart looks the same as before once its line is drawn; no layout or copy changes.
- **Rigor profile:** `standard` for S1 and S2 (Sonnet for acceptance designer and crafter; Haiku for
  reviewers and EXAMINE).
- **Loading/failure looks:** unchanged; no new view or fetch.
- **Usage data:** N/A, because this is a defect fix with no new feature to count.
