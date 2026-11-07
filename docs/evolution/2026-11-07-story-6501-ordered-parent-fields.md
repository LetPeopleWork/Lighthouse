# Story #6501: ordered parent sources

**Pushed to main 2026-11-07, not yet released** · ADO Story #6501 · workspace
`docs/feature/story-6501-ordered-parent-fields/` (feature delta, slices, roadmap, execution log, mutation record) ·
commits `0755fad65..f500fa921`, pushed at `d15aefc48` · decision record
[ADR-279](../product/architecture/adr-279-a-team-or-portfolio-reads-its-parent-from-an-ordered-list-of-typed-sources.md)

## Why

Some setups keep parents in more than one place. The Parent Override Field held one Additional Field and always fell
back to the tracker's own parent, so a setup with two non-native places, or whose own parent is not a Feature, could not
be expressed.

## What users get

- **Parent sources replaces the Parent Override Field** on Team and Portfolio settings and on the connection page: an
  ordered list of one to five sources, and the first that yields a parent wins.
- **The tracker's own parent is a row, not a hidden fallback.** It reads `Parent (default)`, it can be moved or removed,
  and the page warns when it is left out.
- **On Jira, a link that leads to no stored Feature hands over to the next source** on a Team, a single link included. A
  Portfolio does not narrow.
- **The connection default is the same list.** An owner follows it whole or picks `Override`, which starts from a copy.
- **Refresh History names every source that could not be read**, one sentence each, for Teams as well as Portfolios.
- **The list shows on Jira and Azure DevOps only.** It is hidden on Linear, CSV and ServiceNow, which build their parents
  themselves.
- **Usage data** gains one name-only event, `ParentSourcesSavedWithSeveralEntries`, number 25.

Upgrading turns each setting into a list and re-reads once: every Jira owner, and every Azure DevOps owner whose list
holds a field. Three kinds of item move once, so Feature sizes and forecasts can change once: an item of an override owner
with an empty parent key now reads the instance's parent field, an Azure DevOps item of an override owner with no value
hangs under its native parent, and a Team's item whose only link is no stored Feature falls through. Azure DevOps owners
with the own parent in the list read relations for every record they download, override owners included. All of this
belongs in the release note, which is written at `/release`.

## What changed

- `ParentSources` and `ParentSourceEntry`: a validated value, stored as JSON text in three new columns, with a
  migration on both providers that writes today's shapes explicitly.
- Connectors read each entry into a reading on the record. `ParentSourceSelector` alone orders them, `HangUnder` alone
  assigns the parent, and the refresh settles what needs the stored Features.
- The fetch fingerprint renders the ordered list, and a Jira reading version moves every Jira fingerprint once.
- The API gained `parentSources`, `parentSourcesDefault`, `parentSourcesForTeams` and `parentSourcesForPortfolios`. The
  old integer members stay for one release as projections, and the old columns are written through.
- Five architecture rules, read from the compiled code, pin who may decide, assign and read.
- `ParentSourcesList`, `ParentSourcesSetting` and the connection page's list, with the rules mirrored in
  `reduceParentSources`.

21 steps in three slices: Jira with the shared rule (17), Azure DevOps (2), the connection default (2).

## Decisions that moved during delivery

- **The old field on a PUT.** The design counted it only when no list was sent. Today's page and scripts echo the whole
  GET, so that rule would answer 200 and silently drop an edit of the old field. Now a list equal to what the owner reads,
  beside an old field that differs, means the old field is the edit.
- **The connection bridge stayed.** The old number, and a field added in the same save, still reach the default through
  the old setters. The page sends neither, and offers saved fields only.
- **A relation type reads nothing on Azure DevOps** and raises one finding. The design had said otherwise.
- **The upgrade's list of items that move shrank.** A check against the code showed a Jira item with an empty override
  already fell back to the parent key, so that class does not move.
- **Drag and the Playwright check were dropped**, and so were the screenshots and demo data for a Team with two sources:
  the demo data reads one CSV connection, where the control is hidden.
- **Copy, in the maintainer's hand review on 2026-11-07:** the row label became `Parent (default)` on every tracker, the
  `Set here` radio became `Override`, and the rule sentence left the screen. A list sent with the source
  `ConnectionDefault` is ignored, so an API client that means the list sends `Own` too.

## Lessons

- **A page that tidies a list hides the refusal that protects it.** The connection page pruned a removed field from a
  default's list, so the save was not refused and followers moved silently. Lists now reach the server as they stand.
- **Check an upgrade claim against the code before writing it down.** The DESIGN text named a class of moved items that
  the code showed never moved, and that claim had already reached the ADR draft.
- **An echoed GET is a client, even when no client knows about the new field.** Designing the deprecated member for a
  client that leaves the list out missed the page and the scripts that send everything back.
- **A mutation score needs its denominator.** The first backend run read 76.69 % and the final one 96.32 %, but 183 mutants
  did not compile and were never tested. Counted as survivors the score is 70.09 %. Both figures stay on record.
- **A cheap refresh makes "repaired on the next read" a weaker promise than it sounds.** See the known limits.

## Known limits

- A Jira Team item whose link names a Feature stored later stays where the next source put it until the item changes or
  a full update runs, and nothing schedules a full update. Azure DevOps has no link entry, so it cannot arise there. The
  maintainer accepted this for the release; the Team settings page says so.
- A refresh whose query returns nothing reports no parent findings.
- `ParentSourceMatchedNothing` says "link type" on Azure DevOps, which calls it a relation type.
- Rollback limits are listed in ADR-279.
- Survivors worth a look: `JiraWorkTrackingConnector.cs:2031`, `ModifyConnectionSettings.tsx:206`.

## Verification

As reported on the last commit before the push, before the maintainer's copy changes:

- Backend: 12,796 passed, 2 skipped, 0 failed. Frontend: 9,493 passed. Builds clean.
- Live Jira 117 of 117 and live Azure DevOps 100 of 100, run before the last connector-neutral edits.
- Mutation: backend 96.32 % (489 tested), frontend 92.67 % (464 tested), both above the 80 % gate. See
  `docs/feature/story-6501-ordered-parent-fields/mutation/results.md`.
- E2E was not run for this feature before the push. CI was still running when this was written.
