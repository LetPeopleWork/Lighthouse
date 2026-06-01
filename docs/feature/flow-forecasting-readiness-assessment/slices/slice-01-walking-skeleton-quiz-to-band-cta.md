# Slice 01 — Walking Skeleton: 6 questions → score → band → CTA (no gate, no persistence)

**Goal**: A visitor at `/assessment` answers all six 0-3 questions one-at-a-time, gets a 0-100 score, a named band, and the band-specific CTA set — fully client-side, no email gate, no Supabase.

## IN scope

- Route `/assessment` registered in `App.tsx` above the catch-all.
- The six questions with their exact 0-3 answer ladders (faithful to the epic).
- One-question-at-a-time flow with a "Question N of 6" progress indicator and back navigation.
- Scoring module: `rawSum` (0-18) → `score = round(rawSum/18*100)` → `band` mapping (4 bands, exact thresholds).
- Results page showing `score`/100, `band` name, band explanation speaking to BOTH pillars, and the band-specific CTA set (Community in every band; consulting on low bands; paid on top band).
- The credibility anchor line ("Based on Kanban flow metrics and probabilistic forecasting principles (Vacanti / ProKanban)").
- Mobile-friendly single-column layout.

## OUT scope

- Email gate (the breakdown is shown directly here — gating is Slice 03).
- Any Supabase write / persistence (Slice 02).
- Admin dashboard (Slice 04).
- Scroll-reveal / restyled-Navigation visual adoption (Slice 05).
- Per-pillar sub-scores, benchmark line.

## Learning hypothesis

"The six questions and four bands produce a read that visitors recognize as honest and worth ~5 minutes."
Disproved if: internal dogfooding (team takes it) yields scores/bands that feel wrong for known teams, or the questions read as a sales quiz rather than a diagnostic.

## Acceptance criteria

- [ ] Visiting `/assessment` shows the intro then Q1; answering advances one question at a time to Q6.
- [ ] Six answers of 0,0,0,0,0,0 → rawSum 0 → score 0 → band "Flying blind".
- [ ] Six answers of 3,3,3,3,3,3 → rawSum 18 → score 100 → band "Predictable".
- [ ] Boundary: rawSum yielding score 25 → "Flying blind"; 26 → "Drifting"; 50 → "Drifting"; 51 → "Flow-aware"; 75 → "Flow-aware"; 76 → "Predictable".
- [ ] Each band renders its band-specific CTA set; the free Community CTA is present in ALL four bands.
- [ ] Layout is usable at 375px width; full run completes in ~5 minutes.

## Dependencies

- None (greenfield pages on `main`; uses existing react-router and shadcn primitives).

## Effort estimate

~6h (one crafter dispatch). **Reference class**: a self-contained multi-step quiz with a pure scoring function — comparable to prior single-page interactive components in the website repo.
