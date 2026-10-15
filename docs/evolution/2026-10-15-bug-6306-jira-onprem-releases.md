# Bug #6306 — Jira Server / Data Center offered no Releases to bind a Delivery to

**Local, not yet pushed (2026-10-15)** · ADO Bug #6306 Active · workspace
`docs/feature/bug-6306-jira-onprem-releases/` (RCA, roadmap, execution log, mutation record) · commits
`b75f103c7..d0c2d1521` · the additional-fields re-sync request from the same report is split out as Story #6307

## What users get

On a self-hosted Jira the Release picker came up empty, so a Delivery could not follow a Jira Release, and
nothing on screen or in the log said why. Two independent users reported it; the maintainer's Jira Cloud site
worked. After the fix, on Jira Server and Data Center (8 or later):

- **The picker lists the unreleased Releases** of every project the credential can see.
- **A bound Release refreshes**: its date and the Work Items that carry it.
- **The forecast is written back** to the Release's description.
- Jira Cloud sends exactly the requests it sent before.

## How it happened

1. **The Release capabilities were built against Jira Cloud only.** The picker, the refresh and the publish
   called `rest/api/3/project/search`, `rest/api/3/project/{key}/version` and `rest/api/3/version/{id}`.
   Data Center serves no `rest/api/3` and no paged project search, even under v2. Confirmed live on an
   affected user's Data Center 10.3 (`deploymentType: "Server"`), which answers the project search with
   Jira's "dead link" page.
2. **The connector already knew which Jira it talked to** and the search already branched on it. But every
   call site wrote its own `rest/api/N` path, so nothing asked a new capability for its Data Center route.
   The same gap had been fixed once before, locally, for search.
3. **The failure was silent.** A refused project list was logged at Information and read as "no projects",
   and every test used a Cloud-shaped stub.
4. **A naive `3`→`2` would not have fixed it.** Data Center lists projects as a bare array, which the page
   reader would have thrown on (a 500 instead of an empty list). Its version list takes no `status` filter.

## The fix

- **Routes by deployment.** Cloud keeps its routes byte for byte. Data Center reads `rest/api/2/project`
  (one bare array) and `rest/api/2/project/{key}/version` (paged, filtered for status by Lighthouse), and
  publishes through `rest/api/2/version/{id}`. A Jira that will not say which kind it is tries
  `rest/api/2/project/search` and falls back to the plain list on a 404.
- **A failed deployment check is asked again** next time instead of being remembered until restart.
- **A refused project list is logged at Warning**, naming the route and the deployment.
- **Found by the adversarial review and fixed in the same change:**
  - An answer Jira sent that cannot be read (valid JSON, wrong shape) no longer counts as "saw everything",
    so it can never make a refresh retire every bound Release. This also closes the same gap in the older
    Cloud reader.
  - One project's network error no longer empties the whole picker.
  - A version read that comes back unreadable leaves the Release's description alone, and the next round
    tries again (maintainer's choice), instead of overwriting the team's text with the forecast block.

## So it cannot be forgotten again

- **`JiraRestRoutes`** owns every Jira REST route. A Cloud and a Data Center implementation sit behind one
  abstract member per capability that differs, so a new call does not compile until it names its Data
  Center route.
- **`JiraRouteLiteralsTest`** fails on `rest/api/`, `api/2/` or `api/3/` written anywhere in backend
  production code outside that file, and on `api/latest/` inside the Jira folder. It proves it is not
  vacuous: a file-count floor, the routes file itself, and planted literals including a split string.
- **`FakeJiraDataCenter`** is a shared test helper built from the Data Center REST docs. It answers 404 to
  every `rest/api/3` route, serves bare arrays, ignores `status`, and is what the release capabilities are
  tested against.
- **`docs/ci-learnings.md`** carries the rule. `docs/concepts/worktrackingsystems/jira.md` names the oldest
  supported version, and `ARCHITECTURE.md` explains the split.

## Gates

- Every behaviour fix had a regression test that failed on the old code for the bug's reason.
- Full filtered backend suite: 10055 passed, 0 failed. Live `JiraIntegration` (Jira Cloud): 110/110 after
  the routes refactor.
- L1–L6 refactor pass. Adversarial review returned NEEDS_REVISION: 2 major, 10 minor. One revision pass
  followed. The first major was real; the second did not reproduce as stated, but its test found a related
  defect.
- Mutation (Stryker.NET, `JiraRestRoutes.cs` and `JiraReleaseVersionReader.cs`, Jira unit tests only, no
  live categories): see `docs/feature/bug-6306-jira-onprem-releases/mutation/results.md`. The connector
  itself was left out of the run by the maintainer's choice.

## Deferred

- The picker still shows an empty list rather than "some Releases could not be read". That is a UI hint, so
  it needs a sketch first; not filed.
- Story #6307: refresh the Teams and Portfolios on a connection when its additional fields change.
