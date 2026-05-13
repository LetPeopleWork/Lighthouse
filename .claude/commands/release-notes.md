---
description: Draft release notes for the next Lighthouse release by aggregating ADO work items tagged "Release Notes", cross-checking commits since the last tag, attributing community reporters, and adding first-time contributors.
allowed-tools: Bash, Read, Edit, Write, Glob, Grep, AskUserQuestion, mcp__azure-devops__wit_query_by_wiql, mcp__azure-devops__wit_get_work_items_batch_by_ids
---

# /release-notes — draft the next release section

You are drafting the next release-notes block at the top of `docs/releasenotes/releasenotes.md`. Goal: a complete first draft the user can polish, with community reporters correctly attributed and any first-time contributor added to `CONTRIBUTORS.md`.

## Fixed context (do NOT ask)

- **Release notes file**: `docs/releasenotes/releasenotes.md` — new block goes at the top, ABOVE the current top section.
- **Contributors file**: `CONTRIBUTORS.md` — append first-timers under `## Individual Contributors`.
- **GitHub repo**: `LetPeopleWork/Lighthouse` — used for the `compare/<old>...<new>` changelog link.
- **Azure DevOps**:
  - Org: `dev.azure.com/letpeoplework`
  - Project name: `Lighthouse` (ID `7971c18a-f115-43c0-b56c-ca2fe4569606`) — *not* `Lighthouse Demo`.
  - "Release Notes" tag = the curated set to draw from.
  - **Shipped state**: `Closed` only. `Removed` = cancelled/won't-ship, must be excluded. `New`/`Active` = not yet shipped, must be excluded.
  - **Shipped-date field**: `Microsoft.VSTS.Common.ClosedDate`.
  - **Community reporter field**: `Custom.ReportedBy` — *plain string with HTML/markdown*, often wrapped in `<div>...</div>`, sometimes comma-separated, sometimes absent. Not an identity object.
- **Placeholder version**: keep `# Lighthouse vNext` and `compare/<lastTag>...HEAD` until the user supplies a real version. Don't invent one; ask once and accept "vNext" / "not yet" as a valid answer.

`$ARGUMENTS` may optionally be a tag name to anchor "since" (e.g. `v26.5.3.5`). If empty, use the latest tag on `main`.

## Step 0 — load the canvas

Read in parallel:
1. `docs/releasenotes/releasenotes.md` (at least the top ~250 lines — enough to learn the structure: `# Lighthouse vX.Y.Z.W` heading, `## <Feature Name>` sections with prose & images, a `## Bugfixes and Improvements` bullet list, a `## Contributions ❤️` section listing reporters with LinkedIn URLs, and a `**Full Changelog**` compare link).
2. `CONTRIBUTORS.md` — collect every name + URL already listed under `## Individual Contributors`. You will reuse those URLs verbatim if the same person appears again, and only ask the user for a URL when the reporter is genuinely new.

Also harvest reporter-name → LinkedIn-URL pairs from prior `## Contributions ❤️` sections in the release notes — many community members are listed there but not in `CONTRIBUTORS.md`, and the URL is the same.

## Step 1 — anchor "since"

```bash
git tag --sort=-creatordate | head -5
git log -1 --format=%cI <latest-tag>   # the tag's commit date, ISO 8601 with offset
```

If `$ARGUMENTS` names a tag, use it. Otherwise use the latest. Report to the user, in one line, what you anchored to (tag + date).

## Step 2 — pull the ADO release-notes items

Use `mcp__azure-devops__wit_query_by_wiql` against project `Lighthouse`. The ClosedDate cutoff is the tag's commit date from Step 1, in `YYYY-MM-DDTHH:mm:ssZ` form (WIQL accepts ISO 8601):

```sql
SELECT [System.Id], [System.Title], [System.WorkItemType], [System.Tags], [System.State]
FROM WorkItems
WHERE [System.TeamProject] = 'Lighthouse'
  AND [System.Tags] CONTAINS 'Release Notes'
  AND [System.State] = 'Closed'
  AND [Microsoft.VSTS.Common.ClosedDate] >= '<cutoff>'
ORDER BY [Microsoft.VSTS.Common.ClosedDate] ASC
```

Then fetch full bodies via `mcp__azure-devops__wit_get_work_items_batch_by_ids` with these fields:

```
["System.Id", "System.Title", "System.WorkItemType", "System.State",
 "System.Tags", "System.Description", "Microsoft.VSTS.TCM.ReproSteps",
 "Microsoft.VSTS.Common.ClosedDate", "Custom.ReportedBy"]
```

(Bugs put their body in `Microsoft.VSTS.TCM.ReproSteps`, Stories/Epics in `System.Description`.)

If the result set is empty: tell the user, and skip to Step 4 anyway — there are usually dependency bumps + commit-only items worth mentioning.

## Step 3 — normalize community reporters

For every item with a non-empty `Custom.ReportedBy`:

1. Strip HTML tags (`<div>`, `<br>`, `<p>`, `&nbsp;`, etc.) and trim whitespace.
2. Split on commas → one name per token, trim each.
3. Drop empties and de-duplicate (case-insensitive) within the release.

Build the dedup'd list `reporters = [name1, name2, ...]` preserving first-seen order.

For each reporter, resolve their LinkedIn URL in this priority:
1. Exact name match in `CONTRIBUTORS.md` → reuse that URL.
2. Exact name match in any prior `## Contributions ❤️` block in `releasenotes.md` → reuse that URL.
3. Otherwise → mark as **first-timer**, no URL yet. Carry them into Step 6.

Name-match is case-insensitive on the full token. Don't fuzzy-match — "Chris" and "Chris Graves" are different people unless the user says otherwise.

## Step 4 — commits since the last tag, for the gap-check

```bash
git log <tag>..HEAD --no-merges --pretty='%h %s'
```

Bucket by conventional-commit prefix:
- `feat(...)` / `feat:` → candidate headline or note-worthy item
- `fix(...)` / `fix:` → candidate Bugfixes-and-Improvements bullet
- `refactor`, `test`, `docs`, `style`, `chore`, `deps`, `ci` → usually skip, except: bundle dependency bumps into a single "Updated various third-party libraries" line (that's the established phrasing — see prior releases).

Compare the buckets against the ADO items you have. Anything that looks user-visible but doesn't trace to an ADO item is a **gap candidate**. Examples that matter: a `feat(...)` not represented in any item, a `fix(...)` for a user-visible behaviour bug, a security or auth fix the user shipped without an item.

Anything you'd surface to a user reading the release notes belongs in this list. Anything purely internal (test refactors, infra, dependency bumps beyond the catch-all line, the release-notes drafting itself) does not.

## Step 5 — ask the user about gaps & version (one combined prompt)

Use `AskUserQuestion` with up to 3 questions in a single call:

1. **Gap list** (if non-empty): "I found N commits since `<tag>` that aren't covered by an ADO item. Should any of these be in the notes?" — present the top candidates with their hash and subject. Multi-select. If the user adds any, treat them as additional items in Step 6 (no `Custom.ReportedBy`, body comes from the commit message — ask a follow-up only if the message is too thin to summarize).
2. **Version number**: "What version is this release? (`vX.Y.Z.W`, or leave as `vNext` for now.)" Single-select with `vNext (keep placeholder)` recommended first, and `Other` for the user to type a value.
3. **First-timer LinkedIn URLs**: if Step 3 produced first-timers, list them with `Other` (free text) so the user can paste each URL. If there are 0 first-timers, omit this question.

Don't ask anything you can answer yourself. Don't ask whether to write — that's the whole point of the command.

## Step 6 — draft the release-notes block

Insert a new block at the very top of `docs/releasenotes/releasenotes.md`, ABOVE the current top section (do not overwrite the existing `# Lighthouse vNext` block — replace it only if the user gave a real version *and* the existing top block IS the `vNext` placeholder; otherwise insert above).

Structure (match prior releases exactly):

```markdown
# Lighthouse <vNext-or-real-version>

## <Headline Feature 1 — only if there's a story-sized item or feat>
<2–4 sentences of prose. If the ADO item description has usable copy, lift the essence — do not paste raw HTML. If the feature has a docs page, link it.>

## <Headline Feature 2 — same rules>
...

## Bugfixes and Improvements
- **<short title>** — one-sentence description of the user-visible change.
- **<short title>** — ...
- Updated various third-party libraries.   <!-- only if there were dep bumps -->

## Contributions ❤️

Special thanks to everyone who contributed feedback for this release:
- [<Name>](<LinkedIn URL>)
- ...

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/<old-tag>...<new-tag-or-HEAD>)
```

Drafting rules:
- One `## <Feature>` section per genuinely user-visible Story/Epic. Multiple small Bug/Story items collapse into bullets under `## Bugfixes and Improvements`.
- If `Custom.ReportedBy` is set on a Story whose feature is a headline, still credit them by putting their name(s) in the `## Contributions ❤️` list — don't sprinkle attributions inline in the section body (the past releases keep this clean).
- Omit `## Contributions ❤️` entirely if the reporter list is empty after Step 3. Do not write the heading with an empty list.
- Compare link target: `v<old>...<new>` if a real version was given, else `v<old>...HEAD`.
- Don't fabricate image links. Reuse images already in `docs/releasenotes/` only when the ADO description or commit clearly references that asset; otherwise leave images out and let the user add them.
- Tone matches prior releases: pragmatic, mildly enthusiastic, no marketing fluff.

## Step 7 — update CONTRIBUTORS.md for first-timers

For each first-timer the user gave a LinkedIn URL for in Step 5, append to `CONTRIBUTORS.md` under `## Individual Contributors`, matching the existing list style:

```markdown
- [**<Name>**](<LinkedIn URL>)
```

Keep the trailing horizontal-rule + Supporting Companies sections untouched. If the user did NOT provide a URL for a first-timer (left it blank or skipped), still add them to the release-notes `## Contributions ❤️` block as plain text `- <Name>` (no link) but do NOT add them to `CONTRIBUTORS.md` — that file is link-only.

## Step 8 — final report

End with a 4-line summary:
1. Anchor tag + ADO item count + commit count since tag.
2. Headline sections drafted vs. bullets drafted.
3. Reporters credited (count) + first-timers added to `CONTRIBUTORS.md` (count + names).
4. Files written. Suggest the user review the draft and tell you the version when they have one — offer to do a follow-up `/release-notes` pass to swap `vNext` → real version + `HEAD` → real tag in both the heading and the compare URL.

## Guardrails

- Never invent a version number. `vNext` stays `vNext` until the user types a real one.
- Never invent a LinkedIn URL. If the name isn't in any prior list and the user didn't supply one, the entry stays unlinked.
- Never delete or rewrite prior release-notes sections. Only insert at the top (or replace the existing `vNext` placeholder when the user supplies a real version for it).
- Don't include `Removed`-state items, no matter how interesting the title looks — those are explicitly cancelled.
- Don't bundle bumps into the catch-all line if a dependency upgrade was actually a *behaviour change worth calling out* (rare, but happens — e.g. a major React or .NET runtime bump). Surface as its own bullet then.
- Don't commit or push. Leave the diff for the user to review.
- If the ADO MCP fails or returns 0 items, say so plainly — don't paper over it by inferring everything from commit messages alone.
