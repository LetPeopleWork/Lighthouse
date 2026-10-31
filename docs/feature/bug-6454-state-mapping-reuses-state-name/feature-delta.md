# Bug 6454 — A state mapping that reuses the name of an existing state confuses Lighthouse

## Wave: DELIVER / [REF] Maintainer decisions

- Root cause in `rca.md` confirmed (2026-10-31).
- Fix direction approved: F1 (picker offers a mapping its own name), F2 (reconciliation only undoes an
  applied mapping and never applies an empty or shadowing one), F3 (a real collision check that blocks
  autosave for Teams and Portfolios), and the optional F4 (remove the unreachable backend branch).
- Validation copy and placement approved as sketched, reusing the existing mapping-error display in
  `StateMappingsEditor`:
  `Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.`
  Autosave pauses while it is shown.
- Rigor profile `standard`: crafter Sonnet, reviewer Haiku.
- After the slice review (2026-10-31): a mapping name that equals a state grouped by another mapping is
  rejected (`Mapping "In Arbeit" has the same name as a state grouped by mapping "Group". Choose another name.`);
  the name clash check covers To Do and Done too (`Mapping "In Arbeit" has the same name as the To Do state
  "In Arbeit". Choose another name.`, Done alike); both pause autosave. Accepted known limitation: removing a
  mapping's other states from Doing by hand while the clash shows makes the setup look like a mapping in
  effect, so the error clears and the raw state is lost on save. Only typed category entries can tell the two
  apart; raise that as its own ADO item.
- After the third review round (2026-10-31): one last fix, then ship. A mapping whose source state another
  mapping also uses is neither taken into Doing nor treated as in effect (no new copy), plus the four low
  findings. Any further finding of the same kind (the frontend guessing from plain strings whether a mapping
  is in effect) goes on the typed-category-entries ADO item instead of being fixed here.
- At the push (2026-10-31): typed category entries, the two remaining hand-edit paths and the empty-mapping
  autosave bad request are folded into one Bug, #6458 (Related to #6454).
- Out of scope, possible follow-ups: a warning when items land in Unknown; typed category entries
  (raw state vs mapping name) in the model.
