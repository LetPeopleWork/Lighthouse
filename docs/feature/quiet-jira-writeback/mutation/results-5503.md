# Mutation testing — 5503 (Batch write-back fields per issue)

Run 2026-08-09 against the slice-02 working tree. Gate is 80 % kill rate on every stack with changed
files.

| stack | scope | score | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **the methods slice 02 rewrote** | **86.96 %** | 20 | 3 | 0 | ~4 m |
| Backend (Stryker.NET) | `JiraWorkTrackingConnector.cs` whole file | 11.14 % | 49 | 100 | 288 | — |
| Backend (Stryker.NET) | `AzureDevOpsWorkTrackingConnector.cs` | **not mutated** | — | — | — | — |
| Frontend (StrykerJS) | — | **N/A** | — | — | — | — |

**Frontend is N/A, not skipped**: slice 02 changes no file under `Lighthouse.Frontend/`.

Config: `stryker.5503.backend.json`.

## Read the 86.96 %, not the 11.14 %

Both figures are from the same run and both are honest; they measure different things.

`JiraWorkTrackingConnector.cs` is **1440 lines**, of which slice 02 rewrote about 110 — the write-back
path. The rest is issue search, board discovery, changelog parsing and field mapping, none of which the
write-back test filter exercises, producing **288 `NoCoverage` mutants** that sit in the denominator.
That is what drags the whole-file number to 11 %. It is a true statement about the file's overall test
coverage and a useless one about this slice.

**Stryker.NET cannot scope to a line range** — the ledger records this and it still holds — so the
per-slice figure is computed from `mutation-report.json` by filtering mutants to the rewritten methods
(`UpdateItems` … `Refused`, lines 276-385). That is the number the gate is judged on: **20 killed of 23,
86.96 %**.

Two further traps worth carrying forward, both confirmed again here:

- The score's **denominator includes `NoCoverage`**, which is absent from both the "will be tested"
  count and the cleartext survivor list. Reading only survivors will mislead you about why a score is low.
- The **"N mutants created"** line is project-wide, before scoping. Read "N total mutants will be tested".

## Azure DevOps is not mutated, and cannot usefully be

`AzureDevOpsWorkTrackingConnector` talks to Azure DevOps through the concrete SDK types
`VssConnection` → `WorkItemTrackingHttpClient`. There is **no transport seam** — unlike the Jira
connector, which takes an optional `HttpMessageHandler` for exactly this purpose. Every mutant in its
write-back path would therefore be `NoCoverage` against unit tests, and introducing a seam purely to
raise a mutation score is a design change ADR-143 did not ask for and that nothing else needs.

Its batching and fallback are instead asserted **against the real instance**, which is stronger evidence
for the specific claim at issue — see below.

## Closed by this pass

Three rounds of triage on the scoped set: 73.91 % → **86.96 %**. Three tests, each closing a path that
**no test reached at all** (`NoCoverage`, invisible in the survivor list):

| Mutant | What it exposed | Test |
| --- | --- | --- |
| block removal on the `updates.Count == 1` guard | A single field that fails was never exercised — the guard that stops a pointless retry was unverified | `WriteFieldsToWorkItems_TheOnlyFieldIsRefused_DoesNotRetryIt` |
| boolean mutation on `return (false, ex.Message)` | Nothing made the transport **throw**; only HTTP-level rejection was covered | `WriteFieldsToWorkItems_TheTransportThrows_ReportsTheFailureRatherThanPropagating` |
| string mutation on the Jira error message | The failure message was free to change; it is the only diagnostic distinguishing a rejected field from an unreachable Jira | `WriteFieldsToWorkItems_JiraRefuses_ReportsTheStatusItRefusedWith` |

## Accepted survivors (3)

- **`return (true, string.Empty)`** — the error string on the success path. `Written()` never copies it
  into the result, so no caller can observe it. Equivalent mutant.
- **`ResolveFieldReference`'s lookup** — covered by the **live** fixture, which writes through a field's
  *display name* (`WriteDate_IsoFormat_WritesDateFieldAndReadBackMatches("Delivery Date")`) and so
  exercises the name→reference resolution end to end. The stubbed unit tests address fields by reference,
  which is why it survives there. Not a coverage gap; a filter artefact.
- **`NumberStyles.Float | NumberStyles.AllowThousands` → `&`** — only observable for a value carrying a
  thousands separator. Write-back values are integers (age, cycle time, size) and ISO dates; none can
  contain one. Killing it would mean asserting a value the system cannot produce.

## The behavioural evidence that matters more than either number

ADR-143's whole design rests on one claim: **both providers reject a mixed-validity batch atomically**,
so batching without a fallback would let one bad mapping destroy every field on the item. A stub cannot
establish that — it would only replay our own assumption, and the intuitive assumption ("the valid parts
apply") is the one SPIKE-03 proved wrong.

So it is asserted live, against real instances, with read-back:

| Fixture | Test | Verifies |
| --- | --- | --- |
| `JiraWriteBackTest` (17 green) | `WriteFieldsToWorkItems_OneFieldInvalidOnTheSameIssue_TheValidOnesStillLand` | Jira rejects the batch whole; the retry lands the valid fields; read-back confirms the date actually stored |
| `AzureDevOpsWriteBackTest` (19 green) | `WriteFieldsToWorkItems_OneFieldInvalidOnTheSameWorkItem_TheValidOneStillLands` | same on Azure DevOps, read-back confirmed |
| `AzureDevOpsWriteBackTest` | `WriteFieldsToWorkItems_TwoFieldsOnOneWorkItem_BothLand` | AC-05.1/05.2 on the real provider |
| `JiraWriteBackTest` | `WriteMultipleFieldTypes_DateAndNumeric_AllSucceedAndReadBackCorrectly` (pre-existing) | already wrote two fields to one issue, so it became a batched write and had to keep passing |
