---
description: Continuous improvement of Lighthouse's architecture, tech debt, code structure and test infrastructure — one improvement at a time, interleaved with feature work. Builds one board across every ADO Epic tagged "Continuous Improvement" (in backlog order), resumes the in-flight item or lets the maintainer pick the next one, runs its waves under behaviour-free refactoring discipline, and feeds what was learned back into the plan. Also answers which improvement items a new feature Epic should take on first.
argument-hint: "[status] | [item-ids…] | focus | before <epic-id>"
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
- `/continuous-improvement focus` — list the improvement Epics in their order, with open-item counts.
- `/continuous-improvement before <epic-id>` — for a feature Epic about to start (at its DISCUSS or
  DESIGN): which open improvement items touch the same code and are worth doing before or during it
  (see "Before a feature Epic").

## The improvement Epics

Improvement work is grouped into **improvement Epics**, each with a catalogue of independent items
rather than a slice plan. They are found in ADO, not listed here: every Epic tagged
**`Continuous Improvement`** that is not Closed or Removed.

```
SELECT [System.Id], [System.Title], [System.State], [Microsoft.VSTS.Common.StackRank]
FROM WorkItems WHERE [System.TeamProject] = 'Lighthouse' AND [System.WorkItemType] = 'Epic'
AND [System.Tags] CONTAINS 'Continuous Improvement' AND [System.State] NOT IN ('Closed', 'Removed')
ORDER BY [Microsoft.VSTS.Common.StackRank]
```

**Their order is the Epic backlog order in ADO** (StackRank, lowest first), which the maintainer sets by
dragging on the board. The board covers all of them; recommendations prefer the first Epic in that order
that has Ready items, and say when a later Epic's item is the better pick. A new improvement Epic joins
by being tagged; nothing here needs editing. Adding the tag, removing it, reordering and every Epic state
move are the maintainer's; when no tagged Epic has open items left, say so, list any open ADO items
titled as tech debt or refactoring that you come across, and ask what comes next. Do not invent an Epic.

**Catalogues.** Several Epics can share one catalogue: the six Epics split out of Epic 6264 on
2026-11-03 (fast tests #6506, persistence by role #6507, thin services #6508, frontend by module #6509,
modules #6510, aggregates and data ownership #6511) all keep their briefs in
`docs/feature/epic-6264-modular-monolith/`. Find an item's brief by its ADO id in the "ADO Stories"
table of any `docs/feature/epic-*/feature-delta.md`, not by assuming one workspace per Epic. An item
with no brief anywhere (for example T5 #6469 at the split) needs its DISCUSS before it can be Ready.

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

For each catalogue the tagged Epics use, read the feature-delta's `[REF]` sections every run, because they change as items
close: especially **How to pick up a Story**, **Wave Decisions**, **Risks / Open questions** and
**Improvement log** (once it exists). Also read any ADR an earlier item produced that the log or the
decisions name (6264: the bounded-context and layout ADR from S01, with its context map and glossary).

Then collect three sources and reconcile them:

1. **ADO** — the tagged Epics (query above), then one WIQL over their children (`/ado-sync` has the
   `az boards` cookbook): `SELECT [System.Id], [System.Title], [System.State], [System.Parent] FROM
   WorkItems WHERE [System.TeamProject] = 'Lighthouse' AND [System.Parent] IN (<epic-ids>)`. If `az` fails, say so and continue on local evidence
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

Dependencies cross Epics: a brief may name an item now in another tagged Epic. Resolve it the same way,
and show the Epic next to the item.

Print the board grouped by Epic (in backlog order), then by pick-up group, one line per item:
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

Finish with the refreshed board, so the next decision starts from it. When an improvement Epic has no
open items left, say so and leave its ADO state and its tag to the maintainer.

## Before a feature Epic

When a feature Epic starts (its DISCUSS, or DESIGN at the latest; CLAUDE.md asks for this), check which
open improvement items it should take on first or alongside. `before <epic-id>`:

1. Build the board as in step 1, `status` only.
2. From the feature's DISCUSS (or its ADO description if DISCUSS has not run): the contexts it touches
   (ADR-270's context map), and the code areas, services, controllers and frontend units it will change.
3. For each open improvement item, read its brief and mark it **Before** (the feature would build on the
   structure the item replaces, so doing it first saves rework, or the feature would grow a thing the
   item ratchets down), **During** (small, same area, can ride along as its own commits under the
   behaviour-free rules), or **Unrelated**.
4. Print the Before and During items with one line of why each, and their Ready/Blocked state. The
   maintainer decides; a chosen item runs as its own improvement item (steps 3 to 5), never folded into
   a feature Story's commits.

## Never

- Start an item whose hard dependency or gate is not met.
- Change behaviour inside an improvement item.
- Loosen a ratchet, or ship a ratchet-less move.
- Trust a brief's numbers without re-counting.
- Copy the Epic's [REF] sections into an item workspace (only the checklist is copied).
- Tag or untag an Epic, reorder the Epic backlog, move an Epic's ADO state, push without a go, or rebase
  on your own.
