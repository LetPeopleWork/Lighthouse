---
description: AFK mode — drive a feature autonomously through DESIGN → DEVOPS → DISTILL → DELIVER, with refactor + adversarial review (and, if chosen on entry, mutation testing and a push with CI watched) after each slice and a 5-minute review window, then a final refactor → review → mutation, and HOLD. Stops only for a decision that genuinely needs the maintainer. Pushes only if chosen on entry. Stays on until explicitly switched off.
argument-hint: "[feature-id] | off | status"
---

# /afk — carry on without me

The maintainer is away from the keyboard. Drive the named feature forward on your own, as far as it
can safely go, and leave a clean trail. **Stopping is the exception, not the default**; **pushing is
done only when the maintainer chose per-slice pushes on entry** (see "Session options").

## Arguments

Arguments for this run: `$ARGUMENTS`

- `/afk <feature-id>` — enter AFK mode for `docs/feature/<feature-id>/` and start (or resume) the run.
- `/afk` with no id — resume the feature named in the marker; if there is none, detect the single
  in-flight feature the way `nw-continue` does. If that is ambiguous, that is a stop (see below).
- `/afk status` — print the marker and the next action. Changes nothing.
- `/afk off` — leave AFK mode (see "Leaving AFK mode").

## The marker — AFK mode is state, not a mood

On entry write `.nwave/afk.json` in the project root (the `.nwave/` directory is gitignored, so it can
never be committed):

```json
{ "feature": "<feature-id>", "entered": "<ISO-8601>", "status": "running",
  "position": "<wave or step>", "hold_after": "mutation",
  "slice_wrapup": "refactor-review" | "refactor-review-mutation",
  "push": "never" | "per-slice",
  "blocked_on": null, "log": [] }
```

## Session options — ask on entry, before the first dispatch

The maintainer is still at the keyboard when AFK mode starts, so settle these two questions then, in
one `AskUserQuestion` call, and write the answers into the marker. When resuming a run whose marker
already holds them, do not ask again.

1. **How does each slice wrap up?**
   - *Refactor + review* (`slice_wrapup: "refactor-review"`): mutation testing runs once, in the final
     gates, on frozen code.
   - *Refactor + review + mutation* (`slice_wrapup: "refactor-review-mutation"`): each slice also gets
     its own mutation run (see the slice wrap-up below), and the final gate re-runs mutation only on
     files that changed after their slice's run.
2. **Is each slice pushed on its own?**
   - *No, hold everything locally* (`push: "never"`): nothing is pushed; the hold lists the commits
     ahead of origin.
   - *Yes, push each slice and watch CI on the side* (`push: "per-slice"`): after the slice wrap-up
     (see "Push the slice" below), push and keep working on the next slice while CI runs.

If the maintainer answers neither (for example, they launched `/afk` and walked away), default to
`refactor-review` and `never`, and log that the defaults were taken.

Update `position` at every wave or step boundary and append one line to `log` per meaningful event
(wave done, gate result, decision taken, commit). Read the marker at the start of **every** turn while
it exists: if it exists and says `running`, `blocked` or `paused`, you are in AFK mode, whatever the new message
says.

## Leaving AFK mode — only on an explicit word

The maintainer often answers from a phone while still busy. **A reply is not a return.** Stay in AFK
mode after any message except an explicit exit: `/afk off`, "exit afk", "leave afk", "stop afk",
"I'm back". Anything else — "ok", "yes", an answer to a question, a new instruction — is remote input:
apply it, log it, and keep going. When genuinely unsure whether a message means "I'm back", assume it
does not and say in one line that AFK mode is still on and how to leave it.

On exit: set `status` to `off`, summarise where things stand, and delete nothing.

## The sequence

Resume at the first incomplete item. Skip nothing silently; a wave whose artifacts already exist and
are reviewed is complete.

1. **Precondition — DISCUSS must be done.** AFK mode starts at DESIGN. DISCUSS is the maintainer's
   conversation (framing, stories, what the user sees); if the feature has no DISCUSS artifacts, stop.
2. **DESIGN** (`nw-design`), in *propose* mode: take the recommended option unless it changes what a
   user sees or is told. Record every choice in `feature-delta.md` as the wave expects.
3. **DEVOPS** (`nw-devops`), including the project's own checklist items (e.g. the usage-data event
   question in Lighthouse's `CLAUDE.md`).
4. **DISTILL** (`nw-distill`), including its mandatory multi-reviewer gate.
5. **DELIVER** (`nw-deliver`): roadmap → roadmap review → every step through DES → integrity check.
   Stop at the end of the steps — **do not** run DELIVER's own finalize or push.
   **At the end of every slice** (the last step of a roadmap phase is committed), before the next
   slice's first step:
   1. **Refactor** the slice's code (L1–L6, `nw-refactor`), scoped to what the slice touched. End it
      with the static checks (`pnpm build` for the frontend and E2E projects, `dotnet build` for the
      backend) and fix what they report before the review.
   2. **Adversarial review** of the slice, with the same rules as the final review below (the rigor
      profile's reviewer model, verbatim quotes, one revision pass, blockers fixed and proven).
   3. **Mutation testing** — only when `slice_wrapup` is `"refactor-review-mutation"`. Same rules as
      the final mutation gate below (≥ 80 % per stack the slice touched, cheap re-runs, one kill round,
      survivors recorded), scoped to the files the slice changed, on the slice's now-frozen code.
      Record each run under the feature's `mutation/` folder named after the slice.
   4. **Push the slice** — only when `push` is `"per-slice"`:
      1. Run the full local gates first (the filtered `dotnet test`, `pnpm test`, `pnpm build`,
         `dotnet build`); never push red.
      2. `git fetch` and check `origin/main`. If it moved and the push would need a rebase or merge,
         that is a stop ("Trunk moved"): never rebase or merge on your own.
      3. Push, move the slice's Story to Resolved (`/ado-sync`), and start watching the CI run in the
         background (Bash `run_in_background`, e.g. `gh run watch <id> --exit-status`) the way
         `/clean-ci` does. Carry on with the next slice meanwhile; do not wait for CI.
      4. When the watch fires: green → move the Story to Closed. Red → diagnose it the `/clean-ci` way
         between two steps (not in the middle of one), fix it as its own commit, and push the fix with
         its gates green. A CI failure still red after one fix is a stop.
   5. **Review window — 5 minutes.** Post a slice summary (what the slice delivered, its commits, the
      refactor and review outcome, anything worth a look) ending with: *"Review window open for 5 min:
      say **hold** to pause before the next slice; otherwise I carry on at HH:MM."* Start a background
      timer (`sleep 300` via Bash `run_in_background`) and end the turn. Dispatch nothing while the
      window is open.
      - If the timer fires with no reply, continue with the next slice.
      - If the maintainer says **hold** (or "wait", "pause"), set `status: "paused"` in the marker and
        wait, still in AFK mode, until they say "go on" / "continue" / "resume". Any other reply in the
        window is remote input: apply it, then continue as soon as the window closes.
      - Delete or ignore the timer's notification once the window is already settled.
6. **Final quality gates, after the last slice**, in this order:
   1. **Refactor** (L1–L6, `nw-refactor`) across the whole feature, ending with the same static checks.
   2. **Adversarial review** of the whole feature, on the model the agreed rigor profile names for
      reviewers (pass it explicitly; never pick a model of your own); demand a
      verbatim quote for every cited line and spot-check any citation before acting on it. One revision
      pass for its findings; blockers must be fixed and proven (a test that fails before the fix and
      passes after).
   3. **Mutation testing** (`/mutation-testing`) — ≥ 80 % kill rate on each stack the feature touched.
      Kill real survivors with tests; justify equivalent ones in writing. It runs **only here, once**, on
      frozen code: any later edit shifts the mutated line ranges and makes earlier scores stale.
      Configure it for cheap re-runs from the first run (`coverageAnalysis: "perTest"` plus incremental
      mode — see `/mutation-testing`, "Make re-runs cheap"), and stop iterating once each stack is
      ≥ 80 % with the remaining survivors recorded; tell the agent so in its prompt. With per-slice
      mutation, mutate only the files changed since their slice's run (the final refactor and review
      fixes); if none changed, record that and skip the run.
7. **HOLD.** Write the summary (what shipped, locally or pushed, gate numbers, decisions taken, open
   questions, commits ahead of origin), set `status: "holding"`, send one push notification, and do
   nothing more. Do not finalize, do not start the next feature. With `push: "per-slice"`, the final
   gates' commits are pushed the same way as a slice's; otherwise nothing is pushed.

Wave-defined reviewers and gates run without asking — they are part of the wave, not an extra.

## Ask first, with a 15-minute default, for choices that cost a lot of time

Some choices are not product decisions, but they cost a lot of wall clock. Examples: widening a mutation
run to slow acceptance suites, re-running a long gate, picking a much heavier approach than the default,
or anything you expect to add **20 minutes or more** to the run. Do not simply take your recommendation
on these. Raise them first:

1. Post a short message with the choice, the options, the time each one costs, and your recommendation.
   End it with: *"Going with <recommendation> at HH:MM unless you say otherwise."*
2. Start a 15-minute background timer (`sleep 900` via Bash `run_in_background`). Keep working on anything
   that does not depend on the answer.
3. If a reply arrives, follow it. If the timer fires with no reply, go with the recommendation and log it.

This is not a stop: the marker stays `running`, and no push notification is needed beyond the status
update.

## When to stop — and then really stop

Stop only when continuing would mean guessing something that is the maintainer's to decide:

- A **product decision with no safe default** — it changes what users see, are told, or can do, and
  the artifacts do not settle it. (Engineering choices with a clear recommended option are not this:
  take the recommendation, record it, carry on.)
- A **contradiction between waves** the artifacts cannot resolve, or a DISTILL reconciliation gate
  that returns CLARIFICATION_NEEDED on a product question.
- A **gate still red after one revision cycle** (two for DISTILL's review), or the same failure twice.
- **Outward or irreversible action needed**: a push not covered by `push: "per-slice"`, a force/rebase, creating/removing/closing tracker
  items beyond the `/ado-sync` auto-transition rules (Active on start, Resolved on push, Closed on CI green), touching shared infrastructure.
- **Trunk moved underneath you** in a way that needs a rebase or merge — report, never resolve it
  yourself.
- Missing environment: credentials, a service that will not start, a tool that is not installed.
- Scope is ambiguous: no feature named and more than one in flight.

To stop: write the question as a short, self-contained message (the options, your recommendation, and
what happens under each), set `status: "blocked"` and `blocked_on` in the marker, send one push
notification, and **end the turn**. No further writes, dispatches or "while I wait" work. When the
answer arrives, record it and resume from `position` — still in AFK mode.

## Hard rules for the whole run

- **Follow the nWave rigor profile on every dispatch.** Pass `model` explicitly: the profile's
  `agent_model` for crafters and designers, its `reviewer_model` for every reviewer, gates included. AFK
  mode never picks a stronger or weaker model of its own.
- **Push only as the session options allow** (`push: "per-slice"`, at a slice's end with its gates
  green); otherwise never. Never `git pull --rebase`, never force anything. Say how many commits are
  ahead of origin at the hold.
- Commit at every step boundary through the project's conventions (DES `des-commit` inside DELIVER;
  conventional commits; the project's trailers). Write multi-line commit messages to a file and use
  `git commit -F` — inline `-m` can be rewritten by hooks.
- **Wait for each agent's completion notification** before dispatching the next agent that touches
  the same files or runs the same build — a hand-back message is not completion. Run in parallel only
  when the work is on separate stacks and builds cannot collide.
- After every agent: verify its claims that matter (`git log`, `git status`, the test numbers it
  quotes against a re-run when in doubt). Never report a result you have not seen.
- Follow `CLAUDE.md`, `docs/ci-learnings.md` and the project memory; they override this command
  where they are more specific.
- Keep the user-visible output lean: the status update below at each step, not per tool call.

## Status updates — every step, and every 15 minutes

The maintainer checks in from a phone, so every update reads on its own. Post one **whenever a step
completes** (a DELIVER step, a wave, a gate, a fix round) **and every 15 minutes when nothing else has
been posted**. On entry, schedule the heartbeat with `CronCreate` (`*/15 * * * *`, recurring, prompt:
"AFK heartbeat: post the AFK status update"); on a heartbeat, skip it if an update went out in the last
10 minutes. Delete the job (`CronDelete`) at the hold, on a stop, and on exit; recreate it on resume.

The update is only this, nothing more:

```
🌙 AFK MODE · <feature-id> · <HH:MM>
Now:  <slice / Story> · <step id> — <what it does, in a few words> (<agent running | gate running | …>)
Next: <the next step>, then <what follows it>
```

`<HH:MM>` is the local time the update is sent — run `date +%H:%M` for it, never estimate it. A stop
message and the hold summary carry the same time stamp in their first line.

Add one `Done:` line only when a step just finished (its commit and key numbers). A stop or the hold
replaces the update with its own message.

## At the hold — what the summary contains

1. Waves and gates completed, with the key numbers (tests, mutation score per file, review verdict).
2. Decisions taken autonomously that the maintainer may want to revisit, each in one line.
3. Anything deferred or found in passing (flaky tests, stale docs, follow-up tickets worth raising).
4. `N commits ahead of origin, not pushed` (with per-slice pushes, which slices were pushed and their
   CI results) — and the exact next action on their word ("push", or "finalize").
