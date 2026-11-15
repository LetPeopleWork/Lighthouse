# S3 — RED observed before craft

Observed on unchanged production and the unchanged `RefinementView.comments.test.tsx`, before any crafter edit.

| Command (from the worktree root) | Exit | Gate line | Observed by |
|---|---|---|---|
| `bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/measure-slice.sh S3` | 1 | `gate: slice=S3 observed=96 assertions_failed=1 setup_failed=0 file_alone_ratio=1.038 combined_ratio=1.251 valid_pairs=5 required_pairs=3 limit_file_alone=0.60 limit_combined=0.80 verdict=time-ratio-exceeded` | acceptance designer, 2026-11-15, 5 valid pairs, 1 discarded as loaded |
| `bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/check-slice.sh S3` | 1 | `gate: slice=S3 observed=96 assertions_failed=17 setup_failed=0 verdict=failed` | acceptance designer, then the oracle reviewer |
| `bash docs/feature/story-6648-frontend-clean-as-we-go/measurement/deliberate-breaks.sh S3` | 0 | `gate: slice=S3 observed=43 assertions_failed=0 setup_failed=0 went_red=43 verdict=pass` | oracle reviewer on the revised oracle (`0e4f25b47`) |

The timing and mapping checks are RED as intended: the page file still holds its 24 tests. Break 41 (the re-read after a
comment is sent) goes red today only through the old "reads the log again" test; after the rewrite the kept "lets anybody
add a comment without voting" test must catch it.

The shared mapping check reads the before commit's ids from a Vitest run of that file since `7a8e46971`; S1's oracle was
re-approved for that change.
