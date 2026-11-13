---
name: guided-review
description: Walk the maintainer through checking delivered slices by hand, one check at a time, in the running app. Each check is a question they answer with OK, "could not check", or what they see instead; mismatches and new wishes are collected and turned into decisions or follow-ups. Use when the maintainer says "guided review", "walk me through what to verify", "what should I check for S1/S2", or wants to try a delivered slice before the next one starts (also during an AFK hold).
---

# Guided review

The maintainer tries what was delivered in their own running instance. The point is to see it the way a user does,
not to re-run the tests. Keep it short, concrete and in their words.

## 1. Build the check list

For each slice in scope, read what it promised and decided, not the code:

- the slice file (`docs/feature/<feature>/slices/…`) IN/OUT, UAT scenarios and acceptance criteria;
- the maintainer-decision blocks and sketches in `feature-delta.md` (the latest block wins);
- the slice's GREEN commit message, for what actually shipped and what is deferred to later slices.

Turn each into one observable check: **where to go** (menu path or exact URL to paste), **what to do** (click,
toggle, throttle), and **what they should see** (exact copy in quotes, icons, order). One behaviour per check.

Group them by slice, then by place in the app (header, page, old links, in-app links, loading and failure looks).
Mark checks the local setup cannot show (RBAC on, a premium licence, a tracker connection, an empty instance) so the
maintainer can skip them. Loading and failure looks get a check with the DevTools step to provoke them (Network →
Slow 3G, or block a request URL).

Before starting, say what the instance needs (which build, which backend, RBAC on or off), and make sure nothing of
yours still holds the ports (`ss -ltnp | grep -E ':5169|:7246'`).

## 2. Walk through it

Use `AskUserQuestion`, up to four related checks per call, in the order a user would meet them. Each question names
the slice, the place and the expected result on its own, because the dialog may hide earlier text. Options:

- **OK**;
- **Not as expected**, with a description of what they see, via "Other" or the notes;
- **Could not check** or **Skip**, where the local setup may not allow the check.

Do not explain or defend between rounds. Collect each answer as given. A reply that says "yes, but …" is an OK
plus a new wish: record both.

## 3. Close the loop

After the last round:

1. Sum up in one short list: checks met, checks not run (and what covers them instead: a test, CI, a later
   slice), mismatches, new wishes.
2. For each **mismatch**: is it a defect (the build differs from what was decided) or a changed mind (the decision
   was wrong)? A defect becomes a fix in the slice or a bug. A changed mind is a new decision.
3. For each **new wish**: offer where it belongs (inside an open slice, a follow-up item, or out of scope), with a
   recommendation. UI changes get a sketch in the question's `preview` first. Never create an ADO item without a
   yes.
4. Record the outcome in `feature-delta.md` as a "Maintainer guided review" block: what was checked, what was not,
   and every new decision with the next free D-number. When the feature runs on nwdes, add each new decision with
   `nwdes product decision add` and tell the roles through the root context.
5. In AFK mode, the review happens while the run is paused; it does not resume the run. Wait for "go on".
