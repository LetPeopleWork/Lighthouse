# Azure DevOps additional-field lookup — Bug #6013

**Delivered** 2026-09-16 · Backend only · ADO Bug #6013 · twin of Bug #6012, delivered the same day

## The bug as filed was not reproducible

#6013 came out of #6012's blast-radius analysis, which reported that Azure DevOps carried the identical
misattribution: a failed additional-field lookup reported as *"Could not reach Azure DevOps with the
provided URL."* against the URL input. The evidence cited was a catch guard on
`HttpRequestException.StatusCode`.

Measured against the pinned `Microsoft.TeamFoundationServer.Client` 20.256.2 with an offline probe — a
local TLS listener playing Azure DevOps, answering API negotiation successfully so only
`GET /_apis/wit/fields` failed, which is the shape the connector sees after `VerifyConnection` passes —
**the URL branch is unreachable for a field-list failure.** It is reached only by genuine transport
failure, where its message is correct.

The guard that suggested otherwise is **dead code**: the connector holds no raw `HttpClient` and never
calls `EnsureSuccessStatusCode()`, which is the only thing in .NET that populates
`HttpRequestException.StatusCode`. An unexercised defensive catch had become a specification of
behaviour the dependency does not have, and was then cited as evidence in a bug report. It fooled the
same reader twice, in opposite directions, before the probe settled it.

## What the defect actually was

| Field list answers | Exception | What the administrator was told |
|---|---|---|
| 403/404/5xx, JSON body | `VssServiceException` | "Azure DevOps rejected the connection settings." |
| 403/404/5xx, HTML body | `VssServiceResponseException` | same, technical detail the single word `Forbidden` |
| **401 challenged** (what dev.azure.com sends) | `VssUnauthorizedException` | **"Authentication failed… check your Personal Access Token"** |
| truncated body | `JsonSerializationException` | "…unexpected error." with no details |

Every row describes a connection that answered a work item query two lines earlier. The 401 row sent an
administrator to replace a token that had just signed in successfully.

Two further defects sat in one expression. `SingleOrDefault` threw `InvalidOperationException` when two
fields matched one reference — landing in the bare catch as a detail-free verdict, and killing a refresh
outright. And the trailing `?? string.Empty` dropped an unresolvable reference without a word on every
refresh cycle, not only during validation.

## What shipped

| Commit | |
|---|---|
| `3515f4f3d` | `AzureDevOpsReadException`, the catch at the fields call, the three validation catches |
| `48330f97f` | RCA and roadmap |
| `110059546` | Deterministic field pick, and the unresolvable reference logged rather than dropped |
| `5998c0c16` | The verdict stops claiming something it cannot know |
| `b44a3a3f8` | The message pinned fragment by fragment, and the display-name fallback tested |

Azure DevOps was the only connector without a `WorkTrackingReadException` subclass. The new type is
deliberately neither a `Vss*` type nor an `HttpRequestException`: sharing a base with either of the
connector's existing catches would have let them swallow it and left the misattribution in place.
Catching `VssException` rather than `VssServiceException` is what covers the 401, which does not derive
from it. Throwing from `GetCustomFieldReferences` rather than the `ValidateConnection` call site is what
gives the portfolio and refresh paths the same verdict.

## Key decisions

**The URL verdict was left exactly as it was.** After this change `HttpRequestException` is the only
thing the fields call can still raise, and it then really does mean Azure DevOps could not be reached.

**The unresolvable field logs rather than refuses.** A stale field reference in a saved configuration is
an ordinary user mistake. Refusing would turn a cosmetic problem into an outage for everyone on that
connection. The decision is written into the method's own comment so it is not re-litigated.

**A catch was added to `ValidateTeamSettings` that cannot currently be exercised.** Team validation does
not reach the field lookup — measured, not assumed. It was added anyway, as defence for the next read
added to that path, and it is the one mutant deliberately left alive.

## Lessons

**The reviewer and the mutation run each caught what the other could not, on the same change.** The
independent review rejected the fix over a verdict that claimed *"a work item query on it succeeded
moments before this"* — true where it was written, false on the refresh path, which looks work items up
by id and never issues a query. Mutation testing had nothing to say about that: a mutant can tell you a
string is *emptiable*, never that it is *false*. Conversely the review passed a message that Stryker
then showed could be emptied fragment by fragment. Neither gate substitutes for the other.

**Tracing the reviewer's finding turned up a worse one it had missed.** Validation only reaches the field
list after `VerifyConnection` has accepted the credential, so there "this is not your token" is fair. A
refresh has no such step: an expired PAT fails the field call first and would have been reported as a
field-list permission problem. The fix had introduced a new misattribution in the opposite direction to
the one it was written to remove. The verdict now claims nothing about what else is working, and carries
the status Azure DevOps gave, which is what tells the two apart.

**Third time for the same test defect.** #5973, #6012 and now #6013 all reached a mutation run with a
user-facing message whose substance nothing asserted. The shape differs each time — a `Does.Not.Contain`
that passes against `""`, an unasserted message, a message asserted only at its opening clause — but the
cause is identical: a test that exercises a path without asserting what the user reads.

## Still open

- **No test covers `VssResourceNotFoundException`.** The catch is wide enough to handle it, but nothing
  pins that, so an SDK change moving a 404 onto that type would go unnoticed.
- **The unresolvable-field warning has no rate limiting.** It fires per unresolvable field per refresh
  cycle. Better than the silence it replaced; a connection with several stale fields refreshing
  frequently will repeat it.
- **`ExecuteWithRetry`'s status guard is dead code** and was left in place — removing it is not this bug,
  but it has now misled one investigation and should not be allowed to mislead another.
- **`GetBoards` / `GetBoardInformation` silent empties** and the OAuth path into the bare catch, both
  recorded in the RCA's blast-radius sweep.
- **`fieldName` is not wired to an input highlight** in the UI for any connector. The verdict names the
  input; nothing focuses it.

## A process failure worth recording

One crafter on this bug ran `dotnet test --filter "FullyQualifiedName~AzureDevOps"` **without the
live-category exclusions**, executing roughly 104 tests against the customer's real Azure DevOps
organisation and spending quota, against an explicit instruction not to. It reported this itself rather
than leaving it to be discovered. The exclusion filter is easy to drop when narrowing a filter to one
connector's fixtures, because the connector name matches the live fixture too — `FullyQualifiedName~`
and a category exclusion are not alternatives, and the name-based filter is the one that looks precise.

## Artifacts

- `docs/feature/fix-ado-field-lookup-url-misattribution/rca.md` — the probe, the measured exception
  table, six causal chains, and the fix proposal per site
- `docs/feature/fix-ado-field-lookup-url-misattribution/deliver/roadmap.json`
- `docs/feature/fix-ado-field-lookup-url-misattribution/mutation/stryker.6013.backend.json`
