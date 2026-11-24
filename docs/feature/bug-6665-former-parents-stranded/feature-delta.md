# Bug 6665 — Former parent Features never cleaned up

ADO Bug #6665. Route: nwdes bugfix (`nw-bugfix`), AFK mode. Working assessment **M**: data clean-up on a Portfolio
refresh, touching the Feature model, the Portfolio refresh, the orphaned-Feature clean-up and the Portfolio
delete, with forecasting as the affected reader. Rigor profile: **thorough** (opus for designer, crafter,
architect; sonnet for reviewers and the examiner), as for Bug 6656.

## Diagnosis (troubleshooter, 2026-11-24, citations spot-checked by the host)

Verdict **partly confirmed**: the symptom is real and permanent; the report's repro as written does not reach it.

- "Is a parent" is a one-way stored flag (`Feature.IsParentFeature`). It is set only when the Portfolio refresh's
  parent half **creates** the row (`WorkItemService.cs` around 1333-1337 and 1372). `Feature.Update` copies only
  `EstimatedSize` and `OwningTeam`, so the flag is never set on an existing row and never cleared. Nothing in
  production sets it to false.
- The orphaned-Feature clean-up deletes only `!f.IsParentFeature && !f.Portfolios.Any()`
  (`OrphanedFeatureCleanupService.cs:17`), so a flagged row is never deleted.
- When a Feature leaves its last Portfolio, `Portfolio.UpdateFeatures` drops the relation, but the remaining-work
  pass loops only over the Portfolio's current Features, so the departed Feature keeps its last FeatureWork.
- A Team's Feature list (`FeatureRepository.FeaturesOfTeam`) and the forecast's Feature set
  (`FeatureRepository` `TheFeaturesWorkedOnBy`, `ForecastService`) select by FeatureWork alone. A stranded Feature
  with remaining work therefore shows on the Team's Features tab with no Portfolio and takes that Team's
  throughput in the instance-wide order, ahead of real Features ranked below it; its forecasts are recomputed
  from frozen remaining work.
- Real paths: (a) one Portfolio, item first stored as a parent, later its type added to the Portfolio, then
  removed again; (b) two Portfolios, one storing the item as a parent, the other holding it as a Feature, which
  it then leaves.
- Side defect, same cause: the report's repro instead **churns** a parent: stored unflagged (the parent half skips
  what the Feature half downloaded), deleted by the clean-up while still a parent, re-created flagged on the next
  refresh; each deletion cascades its transitions and forecasts.
- Portfolio delete runs no clean-up (`PortfolioController` delete), so its Features stay on Team pages until the
  next Portfolio update or a restart.

Existing tests that pin today's behaviour and need a decision: `OrphanedFeatureCleanupServiceTests`
`CleanupAsync_ParentFeature_PreservesIt` (exactly the stranded shape); `WorkItemServiceTest`
`..._FeatureAlreadyExists_UpdatesParentFeature` (the mock's stored row is the downloaded object, which hides the
`Update` gap); acceptance specs that assert `parent.IsParentFeature` after quiet or delta cycles.

## Maintainer decisions

- **2026-11-24, option A (all four):**
  1. The clean-up keeps a Feature only while it is in a Portfolio, or while it is the parent (`ParentReferenceId`)
     of a Feature that is in a Portfolio. Everything else is deleted. The stored flag no longer decides.
  2. When a Feature leaves its **last** Portfolio, its remaining work (FeatureWork) is cleared. A parent outside
     every Portfolio is kept only as a name for its children: not on Team pages, not in forecasts.
  3. Deleting a Portfolio runs the clean-up at once.
  4. Existing instances are cleaned by the new rule on the next refresh or start. No data migration.
- Feature ↔ Portfolio stays many-to-many for this fix. Going forward the levels work maps an item to exactly one
  unit of the level above (recorded on Epic 6455, `cbf4b089d`); then "last Portfolio" becomes "its unit".
- No user-visible UI is added or changed (items simply stop showing), so no sketch review is needed.
- Usage data: N/A, because a bug fix of data clean-up adds nothing to count.
