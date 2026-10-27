---
description: Continuous improvement of Lighthouse's architecture, tech debt and code structure — one improvement at a time, interleaved with feature work. Builds a board of what is done, in flight and ready in the current improvement focus (today Epic 6264, the path to a modular monolith), resumes the in-flight item or lets the maintainer pick the next one, runs its waves under behaviour-free refactoring discipline, and feeds what was learned back into the plan.
argument-hint: "[status] | [item-ids…] | focus [<epic-id>]"
---

# /continuous-improvement — the next step in making the code better

Architecture, tech-debt and refactoring work in Lighthouse is not a project that ends. It is a steady
stream of small, behaviour-free improvements, picked up one at a time between features. This command is
the entry point to every one of them: **first look at what is still to be done, then decide case by
case what to tackle next**, never a sequential run through a plan made months ago.

Arguments for this run: `$ARGUMENTS`

- `/continuous-improvement` — full run: board → decide → waves → feed back.
- `/continuous-improvement status` — build and print the board only. Changes nothing.
- `/continuous-improvement <item> [<item>…]` — skip the recommendation and propose those items (ADO ids
  like `6367`, or catalogue ids like `S04a`). The board and the dependency check still run; a blocked
  pick is refused with what blocks it named.
- `/continuous-improvement focus [<epic-id>]` — show or change the current focus (see below).

## The current focus

Improvement work is grouped into **improvement Epics**: DISCUSS ran once for the whole Epic and left a
catalogue of independent items rather than a slice plan. The Epics listed here are the focus, in
order; the first one with open items is the one this command works from.

| Epic | Theme | Workspace |
|---|---|---|
| #6264 | Architecture cleanup — path to a modular monolith | `docs/feature/epic-6264-modular-monolith/` |

Changing the focus — adding the next improvement Epic once its DISCUSS has produced a catalogue, or
removing one that is finished — is the maintainer's call. `focus <epic-id>` proposes the edit to this
table and applies it on their yes. When every Epic here is finished, say so, list any open ADO items
tagged or titled as tech debt or refactoring that you come across, and ask what the next focus is. Do
not invent one.

A catalogue is usable when its `feature-delta.md` has a story map with pick-up groups, a dependency
graph, a "How to pick up a Story" section, an ADO Stories table, and one brief per item under
`slices/`. If any is missing, stop and say which: that Epic needs its DISCUSS finished first.

## What makes improvement work different

These rules apply to every item, whatever its Epic says, because they are what keeps a long refactoring
stream safe. An Epic may add stricter ones (6264 does, in its per-Story checklist); it never relaxes
these.

- **Behaviour-free.** Routes, DTO shapes, serialized responses, UI pixels and topologies are unchanged.
  The existing API, E2E and response tests stay green *unmodified*, apart from tests that move along with
  the code they cover. An item that has to change behaviour is not an improvement item; split the
  behaviour change out as its own Story.
- **Ratchet in the same change.** The ArchUnit, import-boundary or analyzer rule an item makes pass is
  added or tightened in the same commit range that makes it pass. Baselines are inline file lists, not
  counts, so a failure names the file. A rule is never loosened.
- **Moves separate from edits.** A pure move or rename commit (`refactor(<module>): move …`) and a
  separate edit commit, so review and `git log --follow` stay readable.
- **Re-count before designing.** Briefs go stale over months. Every number in a brief is re-measured
  against today's code at pick-up, old → new.
- **Mutation last, on frozen code.** Moves invalidate Stryker line ranges. The touched area's score must
  not drop.
- **Architecture docs in the same change.** `ARCHITECTURE.md` and any ADR whose decision changed are
  updated with the code, not later.
- **Prove the payoff where one is claimed.** An item justified by performance is measured before and
  after on the restored production-like dev database.
- **No user-facing release notes by default.** A behaviour-free item ships nothing a user sees. Only tag
  it for release notes if the maintainer says it does.

## 1. Build the board

For the focus Epic, read the feature-delta's `[REF]` sections every run, because they change as items
close: especially **How to pick up a Story**, **Wave Decisions**, **Risks / Open questions** and
**Improvement log** (once it exists). Also read any ADR an earlier item produced that the log or the
decisions name (6264: the bounded-context and layout ADR from S01, with its context map and glossary).

Then collect three sources and reconcile them:

1. **ADO** — one WIQL over the Epic's children (`/ado-sync` has the `az boards` cookbook):
   `SELECT [System.Id], [System.Title], [System.State] FROM WorkItems WHERE [System.TeamProject] =
   'Lighthouse' AND [System.Parent] = <epic-id>`. If `az` fails, say so and continue on local evidence
   alone, marking the board "ADO not checked".
2. **Workspaces** — item workspaces `docs/feature/story-<ado-id>-*/` and
   `docs/feature/stories-*<ado-id>*/`, plus archives in `docs/evolution/` naming the id. For an open
   workspace, detect its wave the way `nw-continue` does.
3. **Git** — `git log --oneline --grep "<ado-id>"` on `main`, to catch work without a workspace and to
   tell pushed from local.

Map each brief to its ADO id through the ADO Stories table (`| #6357 | S01 … |` ↔
`slice-01-s01-….md`). Take dependencies from the **dependency graph** (`==>` hard, `-->` soft) together
with each brief's **Dependencies** section. Briefs are free text: `Hard:` / `Soft:` lines, a bare
"None.", or no section at all, and they sometimes disagree with the graph (6264: the S05b brief says
"Soft: S05a", the graph says `S05a ==> S05b`). Take the stricter of the two, and list each disagreement
under the board so the maintainer can fix the brief or the graph.

Classify every item:

| State | Meaning |
|---|---|
| **Done** | ADO Closed/Resolved, or archived in `docs/evolution/` |
| **In flight** | ADO Active, or an open workspace — show its wave and next step |
| **Ready** | every hard dependency Done, and no gate says stop |
| **Blocked** | name each unmet hard dependency or gate |
| **Removed** | ADO Removed — show the Improvement-log line that explains it |

**Gates.** Refactoring streams carry a "churn without payoff" risk, so catalogues name go/no-go items: a
pilot whose lessons decide whether the items behind it go ahead (6264: S02, the Metrics pilot, gates the
S03* packaging items). Items behind a gate stay Blocked until its recorded decision is go or
go-with-changes. Read the decision from the Improvement log or the ADR, never from memory.

Flag drift: an item ADO calls Closed with no commits, an Active one with no workspace, an open
workspace whose ADO item is still New.

Print the board grouped by pick-up group, one line per item:
`S04a #6367  Ready   — Close the Models ↛ Services gap (S)`, then the dependency disagreements, then
the counts (done / in flight / ready / blocked), and the progress against the Epic's outcome KPIs where
the log has re-measured them.

For `status`, stop here.

## 2. Decide what to tackle

- **Something in flight → resume it first.** Propose carrying on at its next wave. The maintainer may
  still start another; then say this makes two in flight (merge pain from long-lived refactors is the
  usual risk) and go on when they confirm.
- **Nothing in flight → recommend.** Offer 2–3 Ready items with `AskUserQuestion`, the recommended one
  first, each with one line of why from the catalogue's priority rationale: its group's order, its
  size, what it unblocks (count the items whose last hard dependency it is), and what the Improvement
  log has since said about it. Prefer, other things equal, a ratchet that stops drift today over a
  large move. Offer a **small batch** as one option when two or three Ready items are small, on the same
  track and in the same area. Never batch a gate item with anything.
- Re-check every pick against the board: an unmet hard dependency refuses it, an unmet soft dependency
  gets one line of warning.

## 3. Pick up

1. **Workspace.** Create `docs/feature/story-<ado-id>-<catalogue-id>-<slug>/` (a batch:
   `stories-<id>-<id>[-<id>]-<slug>/`) with a `feature-delta.md` opening with:
   - a back-link to the Epic workspace and the brief(s), and the line *DISCUSS: covered at Epic level
     (see the Epic's Wave Decisions); waves start at DESIGN*, but only if the Epic's Wave Decisions say
     so. If they do not, this item runs DISCUSS first, per CLAUDE.md's wave sequence.
   - **Inherited [REF]**: the Epic sections and ADRs that apply, by heading. Link, do not copy; the Epic
     is the single source of truth.
   - **DoR re-run**: the Epic's Definition of Ready, answered for this item.
   - **Baseline re-count**: every number in the brief, old → new.
   - **Checklist**: the rules in *What makes improvement work different* plus the Epic's own per-Story
     checklist, as unticked boxes, ticked through DELIVER. This is the one thing that is copied, because
     it is ticked per item.
2. **ADO**: activate the item (`/ado-sync` Op 3). Never move the Epic.
3. Commit the workspace locally (`docs(<scope>): story <ado-id> workspace — pick-up`).

## 4. Run the item's waves

Run DESIGN → DEVOPS → DISTILL → DELIVER for this item only, through the `nw-*` skills, on its
workspace. Everything in CLAUDE.md applies; the points that matter most for improvement work:

- **Item-specific openers come from the files.** Read the brief's "Notes for DESIGN" and the Epic's Wave
  Decisions for anything that changes how a wave opens or who runs it. 6264: S01 opens with a Big
  Picture Event Storming session run *together with the maintainer* (seed a draft board from the code,
  walk it in chat, publish it as a visual board), and S08 with design-level storming per context, both
  with `nw-ddd-architect`. Never run those sessions alone.
- **DESIGN** cuts the item into ≤ 1-day carpaccio slices and the roadmap, and plans the move/edit commit
  split and the ratchet. It ends with the architecture summary for the maintainer; for improvement work,
  make its "before → after" explicit (what depends on what today, and after).
- **DEVOPS** answers the usage-data question: almost always "N/A, because it is behaviour-free",
  stated. **DISTILL** pins the behaviour-free guarantee: the existing tests that must stay green
  unmodified, plus the new or tightened ratchet rule as a test. UI sketches are N/A, stated, unless
  something visible changes, and then see the behaviour-free rule.
- **DELIVER** opens by agreeing the rigor profile per slice; pass `model` on every dispatch.
- In a worktree, give subagents **absolute** paths.
- **ADR numbers come from `origin/main`**, not the local checkout: `git fetch` and take the highest
  `adr-NNN` across both + 1, right before writing. Other sessions add ADRs while an item is in flight.

**Holds**: stop and wait for the maintainer at each.

1. After DESIGN's summary, before DEVOPS: they review slices and roadmap.
2. At DELIVER start: rigor profile.
3. After the last slice: they review the delivered slices before close-out.
4. Before any push (`/ado-sync` Op 4). Never push without their go; never rebase on your own.

A run may end at any hold. The next `/continuous-improvement` finds the item in flight and resumes it.

## 5. Close out and feed back

After push and green CI: `/ado-sync` Op 5 (item Closed, Epic roll-up reported, Epic state untouched),
then `nw-finalize` on the workspace (mutation results recorded, workspace archived).

Then write back to the plan. This is what makes the next pick-up better than this one. Append to a
`## Wave: DISCUSS / [REF] Improvement log` section at the end of the Epic's `feature-delta.md`, creating
it the first time:

```markdown
### <YYYY-MM-DD> — <catalogue-id> #<ado-id> <title> — <Done | Removed | Gate: go / go-with-changes / stop>
- Commits: <first>..<last> · archive: docs/evolution/<file>
- Ratchet: <rule added or tightened, and its baseline size>
- KPIs re-measured: <name old → new> — or "none touched".
- Changes the plan: <absorbed items, re-ordering, new or dropped dependencies, ADR amended> — or "none".
```

When a lesson changes another item (a new dependency, an item absorbed, a scope moved), show the edit
to that brief and the ADO change (`/ado-sync` Op 2: create, retitle or Remove) and apply them only on
the maintainer's yes. Commit the log and brief edits as
`docs(<scope>): improvement log — <catalogue-id> closed`.

Finish with the refreshed board, so the next decision starts from it. When the focus Epic has no open
items left, say so, leave its ADO state to the maintainer, and ask whether to drop it from the focus
table and what comes next.

## Never

- Start an item whose hard dependency or gate is not met.
- Change behaviour inside an improvement item.
- Loosen a ratchet, or ship a ratchet-less move.
- Trust a brief's numbers without re-counting.
- Copy the Epic's [REF] sections into an item workspace (only the checklist is copied).
- Pick a new focus Epic, move an Epic's ADO state, push without a go, or rebase on your own.
