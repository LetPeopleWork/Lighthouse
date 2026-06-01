# Slice 04 — Minimal internal results / admin dashboard

**Goal**: A protected internal page lets the LetPeopleWork team see captured assessment results — count, band distribution, and the lead list (email + score + band + date) — establishing the dashboard the sibling survey epic (5124) will reuse.

## IN scope

- Protected route (Supabase auth / gated route in the website repo) — not publicly reachable.
- Read view: total responses, completion vs email-capture counts, band distribution, and a table of leads (email, score, band, created_at), filterable by source/kind so survey rows (5124) slot in later.
- Generalized over the source/kind discriminator so 5124 can add a tab/filter without restructuring.

## OUT scope

- Per-pillar sub-score analytics, benchmark/compare views.
- CRM export / email automation (out of scope for v1; leads are read here, acted on manually).
- Building any 5124-specific survey view now (just don't preclude it).

## Learning hypothesis

"A single generalized dashboard over the discriminator is enough for the team to act on leads and will absorb 5124's survey responses without a redesign."
Disproved if: the team cannot find/act on a lead, or the survey-response shape forces a separate dashboard.

## Acceptance criteria

- [ ] The dashboard route is NOT reachable without authentication (verified).
- [ ] It shows total responses, email-capture count, and band distribution computed from real captured rows.
- [ ] It lists leads (email, score, band, created_at) filtered to source/kind = readiness-assessment.
- [ ] The view is parameterized by the discriminator (changing it to another kind shows that kind's rows, none yet).

## Dependencies

- Slice 02 (rows to read), Slice 03 (email on the rows).

## Effort estimate

~6h. **Reference class**: a read-only authenticated table/summary page over one Supabase table — comparable to a basic admin list view.
