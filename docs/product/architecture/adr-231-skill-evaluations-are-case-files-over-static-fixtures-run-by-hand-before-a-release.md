# ADR-231: Skill evaluations are case files over static fixtures, run by hand before a release

- **Status**: Proposed (2026-10-08, DESIGN wave for ADO User Stories #6245, #6217, #6246). Interaction mode =
  **propose**. Run-by-hand confirmed by the maintainer (DISCUSS Q3).
- **Feature**: `story-6217-lighthouse-skills` — repo `lighthouse-clients` (`skills/*/evals/`,
  `scripts/eval-fixture.mjs`, `skills/README.md`)
- **Relies on**: ADR-229 (evals live in each skill folder and are not zipped).

## Context

The acceptance criteria of the three skills are about what an assistant does with them: which tools it calls,
which it must not call (a vote before the person confirms), and what its answer must and must not say. Only a
model can show that, and model output varies between runs. The maintainer decided the runs happen by hand before
a release, with no model key in CI. Each case needs a Lighthouse in a known state, and a real instance's ages,
risks and dates move every day.

Quality attributes: repeatability (a case means the same thing next month), cost (no CI secret, no paid runs per
commit), and evidence a guardrail held (which writes reached Lighthouse).

## Decision

1. Cases live in `skills/<name>/evals/cases.json`: `id`, `story`, `kind` (positive / neighbour / negative),
   `guardrail`, `surface` (MCP or `lh`), `fixture`, `prompt`, optional `followUps`, required and forbidden tool
   calls (and a tool that must not come before a given turn), required and forbidden `lh` commands, and answer
   properties: literal `mustContain` / `mustNotContain` (case-insensitive) and plain-language `checks` the
   maintainer judges.
2. Fixtures are static route maps, `skills/<name>/evals/fixtures/<fixture>.json` (`version` + method-and-path →
   body), served by `scripts/eval-fixture.mjs`, a dependency-free Node server that answers by method and path,
   returns 404 for anything unlisted, and logs every request.
3. The procedure is written in `skills/README.md`: start the fixture, point MCP stdio or `lh` at it, install the
   skill(s) under test, run each case three times in fresh conversations, score from the transcript and the
   request log. Guardrail cases pass 3 of 3; the rest at least 90 % per skill. Results go to the feature
   workspace.

## Alternatives considered

- **Run evals in CI with a model key.** Automatic and repeatable per commit. Rejected by the maintainer (cost,
  nondeterministic red builds, a secret in CI).
- **An eval framework** (promptfoo or similar) with YAML cases. Ready-made scoring. Rejected: a new dependency
  and a model key for a run done by hand a few times a year; the scoring that matters (guardrails, tone) is a
  judgement the maintainer makes anyway.
- **Demo data on a real Lighthouse as the fixture.** No fixture authoring. Rejected: ages and risks change
  daily, the demo has no Team Gravity, and an older-Lighthouse case needs a version the demo cannot report.
- **The in-process `test-support/fakeLighthouse.ts` as the server.** Already holds Gravity's Refinement.
  Rejected as the server (it is TypeScript bound to Vitest's aliases; a manual run should need only `node`);
  kept as the source DISTILL copies fixture bodies from.

## Consequences

- Positive: a case is data; fixtures do not age; writes are visible in the request log, so "no vote before
  confirmation" is checked from evidence, not from the answer text.
- Positive: no CI secret, no new dependency.
- Negative: KPI-2 depends on the maintainer running the cases; a release without a recorded run has no eval
  evidence. The release checklist names the run.
- Negative: fixture bodies copy server shapes by hand and can go stale when the API grows. A route a case needs
  but the fixture lacks answers 404 and shows in the log.
