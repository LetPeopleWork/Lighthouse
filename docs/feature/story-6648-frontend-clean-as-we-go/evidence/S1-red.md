# S1 — RED observed before craft

Observed on unchanged production and the unchanged page file, before any crafter edit.

| Command (from the worktree root) | Exit | Gate line | Observed by |
|---|---|---|---|
| `bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/measure-slice.sh S1` | 1 | `gate: slice=S1 observed=43 assertions_failed=1 setup_failed=0 file_alone_ratio=1.052 combined_ratio=1.123 valid_pairs=5 required_pairs=3 limit_file_alone=0.60 limit_combined=0.80 verdict=time-ratio-exceeded` | acceptance designer, 2026-11-15, all 5 pairs at load at most 6 |
| `bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/check-slice.sh S1` | 1 | `gate: slice=S1 observed=43 assertions_failed=15 setup_failed=0 verdict=failed` | acceptance designer, then the oracle reviewer twice |
| `bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/deliberate-breaks.sh S1` | 0 | `gate: slice=S1 observed=17 assertions_failed=0 setup_failed=0 went_red=17 verdict=pass` | oracle reviewer on the revised oracle (`9b5bbb107`) |

The timing and mapping checks are RED as intended: the page file still holds its 21 tests. The deliberate breaks are
already green, because each moved rule's direct test exists; the Cancel and Done breaks name kept page tests and must
stay red after the rewrite.

The measure run was reported by the acceptance designer and was not re-run by the reviewer (the machine is shared); the
crafter's GREEN run re-measures with the same script.
