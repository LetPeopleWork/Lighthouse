# Slice 52 — T11 Test-writing guidance that the DISTILL and DELIVER agents follow

Epic #6506 · ADO #6538 · `@infrastructure` · job_id: infrastructure-only · **Size S** (estimate: two slices of at most a day) · DRAFT
infrastructure_rationale: FE7, T2 and T5 fix tests after the fact; without guidance the same slow shapes return with every feature.

## Goal and why

A project skill says how tests are written here, and every DISTILL and DELIVER dispatch loads it, so new tests start fast. ADO #6538 lists
the lessons: logic asserted on functions, one render per wiring, small fixtures, a fake clock instead of a real wait, a saves-nothing
check that can still fail, T5's service-port convention. Guards already say a test is wrong afterwards (frontend per-test limit, T2's
host-per-fixture guard, T5's host-use guard, T6's tier guard, T9's budgets); the skill says how to write it right the first time.

## Settled elsewhere (not reopened)

T5 writes the convention into CLAUDE.md (rule, skeleton list, `Contracts`) and the DISTILL text; T11 builds the longer guidance on it and
comes after T5. Fast is decided by time. No nWave scenario tags in test code.

## Proposals (each with its why)

1. **Where it lives: `.claude/skills/test-writing/SKILL.md`**, examples under `references/`. Why: `.claude/skills/visualization/` already has
   that layout, and a skill in the repository is versioned with the code it describes. Two parts, backend and frontend, so a dispatch
   reads only its half.
2. **What it says: rules first, one example each, each citing the measured finding, not the Story number (those archive).**
   Frontend: logic on the function, one render per wiring (FE7), small fixtures, fake clock, no `testTimeout` or `retry` as the fix and no
   order dependence (T12). Backend: scenario at the port, host test only as a named skeleton or in `Contracts` (T5), rule pinned by a unit
   test and wiring by one host test (T10), no shared state across fixtures (T2). Both: a check that cannot fail is not a check: delete the
   production line and the test must go red (FE7's deliberate break). It also shows the guards' failure messages, so a reader knows them.
3. **How dispatches load it.** The dispatch names the file and tells the agent to `ctx_read` it first: nWave subagents have no skill loader,
   only `ctx_*` tools. One line in CLAUDE.md's Wave section covers DISTILL and DELIVER, so every dispatch carries it; reviewer dispatches are
   told the skill is the standard. Nothing is placed in the nWave package (a reinstall reverts it).
4. **Split with CLAUDE.md.** The skill is the only home of how-to and examples. CLAUDE.md keeps three lines: T5's hard rule (new acceptance
   scenarios go to the port), the pointer to the skill, the load instruction. T11 shortens T5's bullet to that form, so no rule is written
   twice. Why: a rule in two places drifts; guard messages point at the skill.
5. **"The next feature's tests follow it", checked as far as an unattended run can.** Proxies now, the real check deferred:
   - a reviewer pass (`nw-skill-reviewer`) on structure and scope;
   - a trial: one acceptance-designer dispatch on an already-delivered small feature in a scratch workspace, output read against the skill's
     checklist, result recorded (cost: one dispatch, an estimate);
   - the mechanical stand-in: the guards and T9's budgets run on the next feature, so a test that breaks a rule fails the build;
   - deferred, read by hand: the next feature's DISTILL records "skill loaded: yes/no, deviations" in its `feature-delta.md`, and the
     maintainer reads it after that feature's DELIVER. T11 closes with this recorded as open, not as passed.

IN: skill, CLAUDE.md lines, dispatch wording, trial, deferred check. OUT: new guards, rewriting tests, product docs.
Dependencies: hard T5 #6469; soft T10 #6537 and T12 #6539 (lessons), T9 #6536 (numbers to cite). Slice-composition gate waived (D-6264-5).

## Acceptance criteria

- [ ] The skill exists at the path above, with both parts and a checklist; a reviewer pass leaves no open critical point.
- [ ] CLAUDE.md names the skill and the load instruction, and the convention appears once.
- [ ] The trial's output is read against the checklist and the result written down; the deferred check is recorded as open, with where it is read.

## Cross-cutting (no silent N/A)

RBAC: N/A, no authorization code changes. Lighthouse-Clients: N/A, no route or DTO changes. Website: N/A, nothing user-visible. Usage-data
event: N/A, nobody uses test guidance in the product. UI sketch: N/A, no screen. Terminology: N/A, no configurable term. Context map: N/A, no change.

## Open for the maintainer

1. **Split with CLAUDE.md.** (a) Skill holds everything, CLAUDE.md three lines; (b) CLAUDE.md keeps T5's full bullet and the skill repeats it. Recommend (a).
2. **Closing with the criterion deferred.** (a) Close on the proxies, note to read the next feature; (b) keep open until one ships. Recommend (a).
3. **The trial dispatch.** (a) Run it; (b) skip it and rely on the reviewer pass. Recommend (a): it is the only test of whether an agent follows the text.

## Maintainer decisions (2026-11-07)

These override the proposals above where they differ.

- **Split:** the skill holds everything; `CLAUDE.md` keeps three lines.
- **Close:** on the proxies, noting when to read the next feature. One trial dispatch on a delivered feature runs.

## Test pyramid vision (maintainer, 2026-11-07)

Direction, not a gate: about 90 % fast, 9.5 % integration, 0.5 % E2E, by test count, as upper limits on the two upper layers.

- **Layers:** fast is the fast tier (`pnpm test:fast`, `Test-Fast.ps1`). Integration is everything in the middle, including frontend `.test.tsx`. E2E is everything in Playwright.
- **Moving down beats the limit:** a test moved from E2E to integration is good, even if the integration share goes over for a while.
- **The hard rule stays time:** the time ceilings gate the build; the shares are only reported, because a share can be met by adding trivial tests.
- **Where it stood on 2026-11-07:** frontend 3,648 fast and 5,857 `.test.tsx` of 9,505; about 56 Playwright tests, 0.25 % of about 22,600. The backend split is known only once T6 has labelled it.

T11 puts this in the test-writing guidance, so a new test lands in the lowest layer that can prove it.

**Component test audit (maintainer, 2026-11-07).** T11 samples about 50 `.test.tsx` files and sorts their tests into "really logic" and "really UI", then estimates the share that could move to a `.test.ts` over an extracted function. It touches no tests. The number decides whether 9.5 % is reachable or only a direction, and it goes to the maintainer before any rewrite is planned.

**Fast cycle in DELIVER (maintainer, 2026-11-07).** T6's step 06-05 puts the rule in CLAUDE.md: red, green and refactor inside a step use the fast loop plus the step's own named test, and the full gates run once at slice end and before every push. T11's skill and every DELIVER dispatch carry it. The fast loop does not see `.test.tsx` or integration tests, so a step whose behaviour lives there runs that named test too.

**Change from T5's DEVOPS (2026-11-07).** A Stryker config must name `Tests.Acceptance.<Feature>`, because new scenarios sit outside its `!~API.Integration` filter.
