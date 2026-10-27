# Mutation testing — 6425 (Export PDF does nothing in the Standalone app on macOS)

Run 2026-10-27 against `worktree-jaunty-gathering-wren` @ `56c6c76ac`. Gate is 80 % kill rate on each touched stack.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS) | **100.00 %** | 10 | 10 | 0 | 0 | 1 m 34 s |
| Backend | N/A — no backend file changed | | | | | |

Configs: `stryker.6425.frontend.json`, `vitest.stryker.6425.ts` (copy it into `Lighthouse.Frontend/` to re-run).

## Frontend

Scoped to the changed lines only:

| file | lines | killed | survived |
| --- | --- | --- | --- |
| `src/utils/tauri.ts` | 9-10 (`isTauriOnMac`) | 4 | 0 |
| `src/pages/Common/Reports/ReportList.tsx` | 51-64 (macOS branch of the list's PDF icon) | 4 | 0 |
| `src/pages/Common/Reports/ThenAndNowHeader.tsx` | 44-51 (macOS branch of the header button) | 2 | 0 |

Specs: `utils/tauri.test.ts`, `Reports/ReportsTab.exportPdf.test.tsx`, `Reports/ReportView.exportPdf.test.tsx`.

### Accepted survivors
None.

### Not mutated
`pdfExportCopy.ts` (a single string constant, pinned verbatim by the UI tests) and the test kit.
