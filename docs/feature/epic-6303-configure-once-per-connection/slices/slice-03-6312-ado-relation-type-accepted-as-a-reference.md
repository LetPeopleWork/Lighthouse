# Slice 03 — Story 6312 — An Azure DevOps relation type is accepted where a field name goes

**ADO**: Story #6312 (Epic #6303) · **User story**: US-03 (validation half) · **Estimate**: ~0.5 day · **Depends on**: finished #6300 (order rule, D19)
**Release point**: no — #6312 releases after slice 04. A relation-type Dependency Field reads nothing and says so
(AC-3.9), but only in the log: the interim "not read yet" finding is stored on the refresh row and written as a
warning, while the Refresh History screen has no sentence for it and shows nothing. That is safe only because
slices 03 and 04 ship together: 03's commits stay local and are pushed after 04's, so `main` never carries, and
no release ever contains, slice 03 without slice 04 (L8).
**Locked decisions**: D1 (any Additional Field; field first, then relation type), D5a, D6 (ADO is its own Story #6312)

## Goal
On an ADO connection, an Additional Field whose Reference is a relation type reference name with its end
(`System.LinkTypes.Related-Forward`, `Contoso.DependsOn-Reverse`) validates instead of being reported as a
missing field, and is not reported as "no field is called …" on every refresh. A typo still fails and lists the
relation types the organisation defines.

## Why
On ADO every Additional Field reference — including Parent Override's — resolves against the field list only
(`AzureDevOpsWorkTrackingConnector.cs:690-698`, `:714-736`; Parent Override reads a field value at
`:1241-1252`). There is no ADO link-type path to reuse; this is the state Jira was in before 6028's slice 01.
ADR-193 §1's link-type port was never built (`IWorkTrackingConnector.cs`), so the lookup is new.

## IN
- Relation-type listing (the SDK's relation-types call is the candidate; DESIGN confirms), asked only for
  references that resolved to no field — field lookup wins on collision, so no existing connection changes.
- Match on reference name + `-Forward` / `-Reverse`, case-insensitive. Display names are not matched (D1).
- Validate's refusal lists the available relation types on no match.
- At refresh, a relation-type reference maps to "a link type", not to `""` with the `:727-729` warning.
- Resolution carried as Jira carries it (field vs link type + end), never persisted (ADR-193 §5).
- **Interim, until slice 04 (AC-3.9):** a Dependency Field that resolves to a relation type reads no dependencies,
  and Refresh History shows one warning per refresh that this relation type is not yet read for dependencies.
  Refusing it at the Portfolio save was rejected: that would need a tracker round trip in the save path, which
  does not exist; the warning reuses slice 02's surface and slice 04 replaces it by reading.
- **Other settings (AC-3.10), never silent (L8):** a relation-type Additional Field named by Parent Override, Size
  Estimate, Feature Owner or Estimation reads as if nothing were named (on Parent Override the native parent
  stands), and Refresh History shows, for that owner, one warning per refresh naming the setting and the relation
  type and saying a relation type cannot be read there. Same Refresh History path as the interim warning above
  (D5a). This holds after slice 04 too: only the Dependency Field reads relation types.

## OUT
- Reading dependencies from it (slice 04). ADO Parent Override from a relation (out of scope).

## Learning hypothesis
Disproves "the relation-type list is reachable with the PAT scope customers grant (`vso.work`)" if the dev
organisation refuses it — which would send the ADO half back to the maintainer before slice 04 is built.

## Production data / dogfood
The dev instance's ADO connection (letpeoplework). Record the relation types it returns (settles R3: whether
Services offers anything beyond its system types). Record which directional relation types the organisation lists;
slice 04's test vehicle is chosen from them.

## Acceptance criteria
AC-3.1, AC-3.2, AC-3.3, AC-3.7 (Validate half), AC-3.9, AC-3.10.

## Changed assumptions

### Spike record (2026-10-17)

- `GET https://dev.azure.com/huserben/_apis/wit/workitemrelationtypes?api-version=7.1` with the ADO
  integration-test PAT answered **200** with 16 entries. The raw answer is recorded at
  `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ConfigureOncePerConnection/Fixtures/azure-devops-services-relation-types.json`,
  and the acceptance harness's organisation now answers from it.
- The organisation `lighthouse-test` answered **404** to the same PAT: the PAT belongs to `huserben`. The
  dogfood organisation named above (letpeoplework) was not queried.
- **No Azure DevOps Server 2022 answer was obtained.** The custom `Contoso.DependsOn-Forward/-Reverse` pair
  the scenarios use is a hand-built addition in the harness, labelled as a custom type an on-premises Server
  can define; Azure DevOps Services cannot define custom link types (R3: Services offers nothing beyond its
  system types, consistent with this answer).
- **The `vso.work`-only refusal hypothesis is NOT disproven.** The PAT's exact scope is unknown and may be
  broader than `vso.work`, so a 200 here says nothing about a customer PAT scoped to `vso.work` alone.
- What the organisation lists (reference name — display name, topology):
  - Directional: `System.LinkTypes.Duplicate-Forward/-Reverse` (Duplicate / Duplicate Of, tree),
    `Microsoft.VSTS.TestCase.SharedParameterReferencedBy-Forward/-Reverse` (Referenced By / References,
    dependency), `Microsoft.VSTS.Common.TestedBy-Forward/-Reverse` (Tested By / Tests, dependency),
    `Microsoft.VSTS.TestCase.SharedStepReferencedBy-Forward/-Reverse` (Test Case / Shared Steps,
    dependency), `System.LinkTypes.Dependency-Forward/-Reverse` (Successor / Predecessor, dependency),
    `System.LinkTypes.Hierarchy-Forward/-Reverse` (Child / Parent, tree).
  - Not directional: `System.LinkTypes.Related` (network).
  - Not work item links (`usage: resourceLink`): `AttachedFile`, `Hyperlink`, `ArtifactLink`. Validate
    never matches a reference against these.
- Recommendation for slice 04's vehicle (not decided here): `Microsoft.VSTS.Common.TestedBy` — directional,
  dependency topology, and a link Features rarely carry for any other meaning. `System.LinkTypes.Duplicate`
  is the fallback.

### Delivered in step 03-01

- Validate looks a reference up in the field list first; only references that missed it are checked against
  the relation types, which are listed once per Validate call and not at all when every reference is a field.
  Directional types match end by end, by reference name in any case, never by display name.
- The listing is created inside one Validate call and passed down that call only; the shared connector holds
  no field of its type, pinned by a reflection check in the slice's scenarios.
