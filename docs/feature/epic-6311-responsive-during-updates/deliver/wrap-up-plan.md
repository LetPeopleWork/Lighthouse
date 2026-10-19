# Epic #6311 wrap-up plan: slice 05 and finalize

Written 2026-10-19 and **not started**. It waits for the maintainer's go, which comes once the Epic #6303
work in this checkout is committed and pushed.

State at writing: slices 01–04 (Bug #6308 and Story #6309, both Closed) are on `origin/main` up to
`c792e7dee`, and their mutation gate passed (`mutation/results.md`). Story #6322 (slice 05, Cancel all) is
in **Next**. Epic #6311 is Active, and its state stays the maintainer's to move.

## 0. Entry checks (before any code)

- [ ] The maintainer has said go.
- [ ] `git status` is clean, and local `main` equals `origin/main` (the #6303 work is pushed). If the trunk
      moved or diverged, **stop and report**. Never rebase without being asked.
- [ ] Docker is running, because 05-03 uses Testcontainers Redis.
- [ ] Re-read `docs/ci-learnings.md` and slice 05's notes (`slices/slice-05-cancel-all.md`, NMD-11/15 in
      `feature-delta.md`).
- [ ] ADO: Story #6322 goes Next → Active.
- [ ] UI: the Cancel all sketch was settled at DISTILL (NMD-11: a text button at the right end of the
      Activity heading, no confirmation, "Cancelling…" while in flight, "Could not cancel. Try again." on
      failure). Nothing was left open, so there is **no new sketch round** unless 05-04 has to depart from
      NMD-11. A departure is asked first.

## 1. Slice 05 DELIVER: four steps through DES (`/nw-execute`, nw-software-crafter)

Each step's commit carries `- step 05-0N` and the Task-Id trailer. The pending ATs already exist, so each
step turns its pending tests on, makes them green and commits.

| Step | What | Test file | Watch for |
|---|---|---|---|
| 05-01 | `POST update/tasks/cancel` (latest + v1, SystemAdmin, 204, idempotent), `CancelAllAsync`, notifier port `PublishCancelAllAsync`/`SubscribeToCancelAll` (in-process; Redis delivers locally only) | `Slice05CancelAllScenarios.cs` | no S107 growth on `UpdateController`; one Information log line with counts only; set the budget in `Scripts/test-timings/test-time-budget.json` |
| 05-02 | Apply order: follow-ups, then holds, then per-key stops; `cancellations.Admit` moved before `statusStore.TryAdmit` | + `CancelAllWhileWorkIsAdmittedTest.cs` | capture every `Try*` result (CA1806); ArchUnit hold guards stay green |
| 05-03 | Redis cancel-all channel, separate from `lighthouse:update-cancelled` | `CancelAllMultiReplicaTests.cs` | proof-of-concept only: the minimum change plus one real-Redis test |
| 05-04 | `cancelAllTasks`, an action slot on `SectionHeading`, the control in `ActivitySection`/`useTaskManagerPopover` | `TaskManagerIcon.cancelAll.test.tsx` | gate on `useRbac()` only; frontend preflight (S6754, S7735, readonly props, `error_`); grep `Lighthouse.EndToEndTests/` before changing any Task Manager label; **delete `PendingEpic6311.cs`** once no pending marker remains (backend and frontend) |

## 2. Slice-end refactor and adversarial review

1. `nw-refactor` (L1–L6), scoped to the files slice 05 touched.
2. Adversarial review of slice 05 on a strong model (not Haiku). Every cited line needs a verbatim quote,
   and citations are spot-checked. One revision pass; a blocker is fixed with a test that fails before the
   fix and passes after.
3. Comments follow the CLAUDE.md rule: no NMD/DDD/AC references in code comments.

## 3. Local gates (CI parity)

- `dotnet build` with zero warnings, and `dotnet test` with the non-connector filter. Also run
  `CancelAllMultiReplicaTests` explicitly with Docker up, and check which category it carries.
- Frontend: `pnpm test` and `pnpm build`, clean. `pnpm build` runs `biome --write`, so watch the
  docs-symlink hazard.
- Playwright: run the Task Manager spec(s) locally if any visible label changed. No new E2E spec.
- **Manual production-data gate** on the dev instance restored with `Restore-DbBackup.ps1` (:5169):
  AC-05.7 (Update All, then Cancel all: only removals remain, and nothing comes back before the next
  periodic tick). **Also owed from slices 01–04:** the slice notes record no readings for AC-01.5,
  AC-02.7, AC-03.8 or AC-04.4 (compare AC-04.4 against the 1,766 ms KPI-3 baseline). Record each reading
  and pass or fail in its slice file.

## 4. Mutation testing (on frozen code, once)

- `/mutation-testing`, scoped to **slice 05's changed lines only** on both stacks. Slices 01–04 are
  already gated on pushed, unchanged code. Re-scope only if the refactor in step 2 touched their files.
- Use `coverageAnalysis: perTest` and incremental mode from the first run, and exclude the acceptance
  suite from Stryker.NET.
- Make ≥ 80 % on each stack, do one kill round, then one cheap re-run. Record the remaining survivors and
  stop. Add a "Slice 05" section to `mutation/results.md` and the configs as `stryker.6322.*`.

## 5. Push (pause first)

- Post a summary (commits, gate numbers, review outcome, manual readings) and **wait for the maintainer's
  OK to push**.
- Push straight to `origin main`, trunk-based. ADO: Story #6322 → Resolved on push.

## 6. `/clean-ci`

- Watch "Build And Deploy Lighthouse" on that push, fix any failure or Sonar issue, and add the lessons
  to `docs/ci-learnings.md`.
- A run sitting in `waiting` is a deploy approval, and the maintainer approves Release gates.
- When CI is green, Story #6322 → Closed.

## 7. Finalize the whole Epic (DoD 6–10 and the DISCUSS checklist)

Docs go only after the maintainer confirms that the shipped code behaves as intended. Write the
configurable terms (Feature, Work Item…).

- [ ] **Docs.** `docs/settings/taskmanager.md` (Cancel all; Stop now ends a forecast) and
      `docs/concepts/howlighthouseforecasts.md` (half the cores, the 5-year horizon, what "More than 5
      years" means, and that write-back writes nothing for a beyond percentile, which per NMD-4 renders as
      "85%: more than 5 years" in the Jira Release block). Grep `docs/` for every other page about forecast
      dates and Delivery likelihood. Check for website hot-links to any changed asset.
- [ ] **Screenshot** (`@screenshot`, one per theme, premium licence present, `rm` the old PNG before
      regenerating): the Task Manager with Cancel all only. "More than 5 years" is described in the docs
      as text, with no screenshot and no low-throughput demo Team (maintainer, 2026-10-19).
- [ ] **Architecture.** Update `ARCHITECTURE.md` and `brief.md` for bounded simulation parallelism, the
      beyond-horizon marker and the cancel-all channel (ADRs 248–254).
- [ ] **Release work is out of this Epic** (maintainer, 2026-10-19). It is done later, at release time,
      and is listed here only so nothing gets lost:
      - The Lighthouse-Clients minor release, which renders `beyondHorizon`/`horizonYears` keyed on
        whether the field is present.
      - The "Release Notes" tag on #6308, #6309 and #6322, and the release-notes copy (pain first, the
        reporter credited, no customer identifiers).
      - The rollback note (NMD-16): after a rollback, run a forecast update for every Portfolio.
- [ ] **Epic #6298** (PDF export, not started) must render beyond-horizon. Add a note to its workspace.
- [ ] **N/A items with their reasons.** Website: a fix, not a capability. Usage data: no event (NMD-7),
      so `usagedata.md` is untouched.
- [ ] **Archive.** `/nw-finalize` moves lasting artifacts and archives the workspace to `docs/evolution/`.
      Commit the workspace and the docs, then push. A docs-only push starts no CI run.
- [ ] Final summary to the maintainer. The Epic's state is left to them.

## Stop points that need the maintainer

1. The go to start (§0). 2. A departure from NMD-11 (§0). 3. Push (§5). 4. Release gate approvals (§6).
5. Confirming the shipped behaviour before docs (§7).
