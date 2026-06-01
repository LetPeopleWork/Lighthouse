# Slice 05 — Adopt the redesign visual language + Navigation entry

**Goal**: The assessment pages adopt the alt/redesign-2026 visual language — scroll-reveal idiom, restyled section styling — and a "Forecasting Readiness" entry is added to the restyled Navigation, so the feature does not look stale when the redesign merges.

## IN scope

- Add a Navigation entry (in `navItems`) linking to `/assessment`.
- Apply the `useScrollReveal` scroll-animation idiom and consistent restyled section styling to the intro, question, teaser, and breakdown surfaces.
- Visual consistency with Hero/HowItWorks/FAQ section styling so the assessment reads as part of the redesigned site.

## OUT scope

- Any change to scoring, gate, persistence, or dashboard behavior (Slices 01-04 own those).
- Editing redesign-branch files (style-only branch is the colleague's; this slice consumes the shared tokens/hook, does not modify them).

## Learning hypothesis

"The assessment can adopt the redesign idiom by consuming the shared tokens and useScrollReveal hook, with zero collision with the alt/redesign-2026 branch."
Disproved if: adopting the idiom requires touching files the redesign branch also edits, creating a merge collision (contradicts D7's no-collision premise).

## Acceptance criteria

- [ ] The restyled Navigation shows a "Forecasting Readiness" entry that routes to `/assessment`.
- [ ] Intro, question, teaser, and breakdown surfaces use the scroll-reveal idiom and consistent section styling.
- [ ] No file edited here is also edited on alt/redesign-2026 (no merge collision) — verified against the branch's touched set.
- [ ] Behavior from Slices 01-04 is unchanged (regression check).

## Dependencies

- Slices 01-03 (the surfaces to style); coordinate timing with the redesign branch merge (styling-only, non-blocking).

## Effort estimate

~4h. **Reference class**: a styling-adoption pass consuming an existing hook + tokens — comparable to applying the redesign idiom to one new page.
