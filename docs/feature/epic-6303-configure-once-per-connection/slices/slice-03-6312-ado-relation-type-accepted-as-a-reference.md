# Slice 03 — Story 6312 — An Azure DevOps relation type is accepted where a field name goes

**ADO**: Story #6312 (Epic #6303) · **User story**: US-03 (validation half) · **Estimate**: ~0.5 day · **Depends on**: finished #6300 (order rule, D19)
**Release point**: no — #6312 releases after slice 04. Alone, this slice is safe: a relation-type Dependency Field
reads nothing **and says so** on Refresh History (AC-3.9), never silently (L8).
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
(filled during DELIVER)
