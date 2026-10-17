# Bug #6315 — Jira Cloud refused key-batched searches with 414 Request-URI Too Long

**Pushed to main 2026-10-16, not yet released** · Backend only · ADO Bug #6315 · workspace
`docs/feature/fix-jira-414-key-batched-queries/` (RCA, roadmap, execution log, mutation record) · commits
`cc864bc53..5c642f03c`

## What users get

On an affected user's Jira Cloud instance, four Teams failed on every refresh, on v26.10.10.1 and again on
v26.10.16.6. Each refresh ended with "The work tracking system refused this refresh", and Jira's answer
was `414 RequestUriTooLong`. The project key was long, so a batch of 200 issue keys made a search too long
to fit in a URL. After the fix:

- **Every Jira Cloud search is sent as a POST with a JSON body**, so the query is no longer part of the
  request line and the length of the key list stops mattering.
- **Those Teams refresh again**, and so do Portfolios whose Feature or parent-Feature downloads list as
  many long keys. Those paths use the same search and hit the same limit.
- Jira Server / Data Center sends exactly the requests it sent before. It already searched by POST.

## How it happened

1. **Cloud search put its JQL in the query string.** The delta download asks for the Work Items whose
   stamp moved, 200 keys per query, as `key = "X" OR key = "Y" OR …`. Escaping turns every space, `=`
   and `"` into three characters, so 200 keys of 14 characters make about 5,200 characters of JQL and a
   request URI of about 8.1 KB. That is right at the 8 KB limit many servers and proxies enforce. The
   regression test measured 9,099 bytes for its 200 long keys on the old code.
2. **The chunk bounded a count, and the limit is a length.** The 200-key chunk was copied from the
   Azure DevOps connector, whose ids are short integers sent in a request body. Jira keys vary in length,
   so a count says nothing about bytes.
3. **The same defect had been fixed for Data Center a month earlier, and Cloud was ruled out.** That fix
   named the problem exactly (a count standing in for a length), then decided Cloud was not exposed
   because it pages by token. Paging has nothing to do with where the query travels. The route
   documentation also said, as a fact, that Cloud's search is read with a query string.
4. **The tests could not produce a 414.** The Jira test stub answers any URI of any length, the only
   length assertion ran on Data Center, and every Cloud test used six-character keys such as `PROJ-1`.
5. **One refusal became a permanent outage.** The sweep that finds what moved uses the Team's own short
   query and succeeds. The keyed download then fails, nothing is stored, the stamps never advance, and
   the next refresh asks for the same keys again. Only a failed sweep falls back to the full download, a
   failed keyed download does not. Fixing the transport removes the trigger. The missing fallback stays
   (see Follow-ups).

Where the 414 came from, Atlassian's edge or a proxy in front of it, was never settled. It did not need
to be: taking the query out of the request line fixes both.

## Key decisions

**Fix the one method every Cloud search goes through, not the call sites.** The delta download, the
Feature download, the parent details, both identity sweeps, the validation preview and the Delivery
release-membership walk all reach Jira Cloud through one search walk. Changing that walk fixes all of them
and leaves no second Cloud transport to drift.

**POST, not a shorter query or a byte budget.** `key in ("A", "B", …)` cuts the URI by about a third,
but it is still bounded by a count, so longer keys or a longer proxy path hit the same cliff. Sizing
chunks by encoded length would mean guessing at an undocumented limit and making more round trips. Jira
Cloud's `POST rest/api/3/search/jql` returns the same response as the GET, pages by the same token and
carries the changelog inline the same way.

**The body follows Cloud's schema, not Data Center's.** Cloud reads `expand` as one comma-separated
string, where Data Center reads a list. Copying Data Center's `["changelog"]` would have been ignored or
refused, and every status transition would have silently disappeared. `fields` is always sent, because
Cloud returns only the id when it is left out. A first page carries no `nextPageToken` at all rather than
a null one. The adversarial review checked the body against Atlassian's published OpenAPI for the
operation.

**The chunk stays at 200 keys, in its `OR` form.** It still bounds the response size and the cost of one
round trip, and that is all it now claims to bound. The comments that said it protected a URL length were
rewritten.

## What shipped

| Commit | Change |
|---|---|
| `cc864bc53` | `fix(jira)`: Cloud search over POST with a JSON body; the long-key test now runs on Cloud and Data Center; a Cloud-only test pins the method, route and body shape; test helpers read the query from the body |
| `abf57cda7` | `refactor(jira)`: one place in the tests reads a Jira search body |
| `5776973e7` | `test(jira)`: assertion messages that still said key batches were split for the URL |
| `80adaaf6a` | `test(jira)`: mutation record |
| `5c642f03c` | `docs(jira)`: RCA and delivery record |

## Gates

- **RED first, on the bug's reason.** On the old code the Cloud case of
  `GetWorkItemsForTeamByReferenceId_AsksForAQueryNoRequestLineCouldCarry` failed with a 9,099-byte URI.
  Its Data Center case passed, which shows the test detects exactly the Cloud defect. The new body-shape
  assertions were each confirmed red on the old code before the fix.
- **Filtered backend suite after the rebase onto `main`:** 10,314 passed, 0 failed, 1 skipped (the skip
  predates this change).
- **Roadmap review:** NEEDS_REVISION once, then APPROVED. The blocker is described under Lessons.
- **Adversarial review:** APPROVED. It verified the request body against Jira Cloud's OpenAPI and found
  2 LOW issues, both stale assertion messages, which `5776973e7` fixed.
- **Mutation (Stryker.NET, `JiraWorkTrackingConnector.cs`, Jira unit tests only):** 100% on the changed
  lines (2/2), against an 80% floor. The whole file scored 52.04%, which describes about 2,800 lines this
  fix never touched. Most of the change is a constant, field initialisers and an anonymous object, which
  Stryker does not mutate. Tests that assert the method, route and body directly cover that behaviour.
  Three survivors in the same search walk (the cancellation check, the page counter and the page cap)
  predate this fix. They are recorded, not chased. See
  `docs/feature/fix-jira-414-key-batched-queries/mutation/results.md`.

## Lessons

**An assertion that passes on the old code proves nothing.** The first roadmap had the new tests read the
page token and the expansion through the test helper's `Value()`. That helper reads the URL first and
falls back to the body only if the URL does not have the value. On the old code the values were in the
URL, so every body assertion would have passed before the fix. The roadmap review caught this. The
assertions now parse the body directly, and each one was run red against the old code before the fix
went in.

**Ruling a sibling out needs the same evidence as ruling it in.** The Data Center fix identified the
mechanism correctly and then excused Cloud for a reason unrelated to it. A month later Cloud failed in
the same way. When a fix names a mechanism, check every path that shares it before declaring one
unexposed.

**A stub with no limits hides a whole class of failure.** No test could have produced this 414, because
the stub answers any request. The guard now is an assertion on the request itself, with a positive
control that proves the query alone would not fit in a URL.

**Always add the live-connector exclusions to an ad-hoc test filter.** During review a narrow
`dotnet test --filter` without the `TestCategory!=…` exclusions ran the live Jira integration tests once.
They passed, but they talk to a real instance, and repeated live runs can rate-limit the next CI build. Any filter,
however narrow, carries the exclusions from `CLAUDE.md`.

## Follow-ups

- **The write-back failures ("0 succeeded, 28 failed") from the same report.** Not this defect: write-back
  sends no JQL, only a field-list read and one PUT per issue. The Error or Debug lines from the write-back
  service will name the reason. Deferred by the maintainer.
- **A refused keyed download never falls back to the full download.** Any deterministic refusal on the
  second phase of a refresh still locks a Team out, because the same keys fail again on every refresh.
  Falling back to the full download once, and logging it loudly, would end that. Not done. Why about 200
  long keys counted as moved in the first place is also unconfirmed.
- **A custom API token with only granular scopes.** The documented scope, classic `read:jira-work`, covers
  both GET and POST search. The beta granular scopes Atlassian lists for the two operations differ, so a
  token built only from granular scopes could be refused on POST. The risk is low: the live tests that use
  a scoped token passed. Mention it in the release notes if a user reports it.

## Artifacts

- `docs/feature/fix-jira-414-key-batched-queries/rca.md` — five-whys analysis: the transport, the
  lockout, and why write-back is a separate problem
- `docs/feature/fix-jira-414-key-batched-queries/deliver/roadmap.json` and `execution-log.json` — the
  one-step plan and its RED, GREEN and COMMIT trace
- `docs/feature/fix-jira-414-key-batched-queries/mutation/results.md` — scores, scope and the three
  pre-existing survivors
