# S2 — RED observed before craft

Observed after the `refactor(reports)` move (before commit subject "refactor(reports): move the failure rules of a
combined PDF into a pure module") and on the unchanged `ReportsPage.combinedPdf.test.tsx`, before any page rewrite.

| Command (from the worktree root) | Exit | Gate line | Observed by |
|---|---|---|---|
| `MEASURE_PAIRS=3 MEASURE_WAIT_SECONDS=300 bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/measure-slice.sh S2` | 1 | `gate: slice=S2 observed=34 assertions_failed=1 setup_failed=0 file_alone_ratio=0.989 combined_ratio=1.014 valid_pairs=3 required_pairs=3 limit_file_alone=0.75 limit_combined=0.80 verdict=time-ratio-exceeded` | acceptance designer, 2026-11-15 (3 pairs, not the default 5; the GREEN run uses the default) |
| `bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/check-slice.sh S2` | 1 | `gate: slice=S2 observed=34 assertions_failed=5 setup_failed=0 verdict=failed` | oracle reviewer on `a56744191` |
| `bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/deliberate-breaks.sh S2` | 0 | `gate: slice=S2 observed=29 assertions_failed=0 setup_failed=0 went_red=29 verdict=pass` | oracle reviewer |

The move itself was proven behaviour-preserving before the oracle: the same 51 tests of the Reports page, its usage data
and the selection actions passed unchanged before and after it.

The slice checks resolve the before commit by its subject since `a56744191`, after the branch was rebased onto main and
pushed; S1, S3 and S2 oracles were re-approved for that change.
