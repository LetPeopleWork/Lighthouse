# Mutation testing — 6456 (Team/Portfolio query saves when the field is left)

Run 2026-10-31 against `main` @ `e7fb48f1a`. Gate is 80 % kill rate on each stack touched.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend | N/A | | | | | | |
| Frontend (StrykerJS 10.1.1), first run | **91.11 %** | 45 | 41 | 3 | 1 | 0 | 5 m 9 s |
| Frontend, after one kill round (incremental) | **95.56 %** | 45 | 43 | 2 | 0 | 0 | 2 m 55 s |

**Backend N/A:** the story changed no backend file.

Configs: `stryker.6456.frontend.json` and `vitest.stryker.mutation.ts`. Copy the vitest config into
`Lighthouse.Frontend/` for the run; it is gitignored there. `coverageAnalysis: perTest` with an
incremental file.

## Frontend

| file | mutated lines | tested | survived |
| --- | --- | --- | --- |
| `GeneralSettingsComponent.tsx` | 132-145, 190-192 (query draft and commit on blur) | 15 | 1 |
| `useModifySettings.ts` | 305-345 (unmount flush and the autosave effect it hooks into) | 30 | 1 |

### Closed by this pass

- `useModifySettings.ts:314`: `if (isSavingRef.current)` was not covered (NoCoverage, plus the
  `false` conditional survived). The page is left while an earlier save is still on its way. New
  test "saves the latest edit after the save already on its way, when the page is left meanwhile" in
  `ModifyTeamSettings.queryCommit.test.tsx`. It pins that the latest edit is held until the first
  save finishes, and then sent once.

### Accepted survivors

- `GeneralSettingsComponent.tsx:132`: `?? ""` → `?? "Stryker was here!"`. The fallback applies only
  when `settings` is null. Neither the Team nor the Portfolio page renders the form before settings
  have loaded, so no user can reach it.
- `useModifySettings.ts:323`: the unmount effect's dependency list `[]` → `["Stryker was here"]`. A
  constant dependency never changes, so the effect still runs once and cleans up once: an equivalent
  mutant.
