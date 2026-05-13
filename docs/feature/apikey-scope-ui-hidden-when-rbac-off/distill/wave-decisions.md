# DISTILL Wave Decisions — apikey-scope-ui-hidden-when-rbac-off

Date: 2026-05-13

## DWD-1: Walking Skeleton Strategy = A (Full InMemory)

Confirmed via auto-detection: feature has no driven I/O ports of its own
(it consumes existing HTTP-backed services via mocked context). All
adapters covered transitively by their own service-level test suites.

The walking-skeleton driving adapter is the rendered React component
tree under `<ApiKeysSettings />`, exercised through `render()` and
`fireEvent.click` against the real "New API Key" button — not a direct
function call into the service layer. This satisfies the "user's actual
invocation path" requirement of Mandate 6 for a frontend-only feature.

## DWD-2: Hide vs. disable

**Decision: hide.**

The "Restrict scope (optional)" Accordion is *removed from the DOM*
when RBAC is off, not merely disabled. Rationale:

- A disabled control still implies "this feature exists but is
  unavailable right now" — which would invite the user to ask their
  operator to enable scopes, when in fact scopes have no effect even
  with auth on, until RBAC is also on. Hiding communicates "the
  control is not applicable to your deployment."
- Disabling preserves the visual real-estate cost (chevron, label,
  vertical spacing) for no benefit.
- The Header / Settings / OverviewDashboard precedent already
  established by `rbac-ui-completeness` is to *omit* RBAC-only UI when
  it is irrelevant, not to grey it out.

## DWD-3: No backend change

The Explore investigation that motivated this feature confirmed that
`RbacAdministrationService` already short-circuits all permission
checks before any scope intersection runs when RBAC is disabled. Any
attempt to also gate scope persistence on RBAC enablement at the
backend would be redundant defence-in-depth that complicates the
schema (`POST /api/apikeys` would need to reject `scope` payloads when
the service is in RBAC-off mode) for no observable user benefit.

This feature is **frontend only**. The DELIVER wave must not touch
`ApiKeyController.cs` or `ApiKeyService.cs`.

## DWD-4: Hide on loading + on fetch failure

While `useRbac()` is loading the authorization summary, and on fetch
failure, `isRbacEnabled` defaults to `false` (the
`PERMISSIVE_SUMMARY`). The scope picker is hidden in both transient
states.

This is the safe default for this control: showing it requires
*positive confirmation* that RBAC is enforcing permissions; anything
else hides it. Scenarios M1.3 and M1.4 pin both transient states.

## DWD-5: Test file naming

New Vitest file: `CreateApiKeyDialogScope_RbacOff.test.tsx`, placed in
`Lighthouse.Frontend/src/pages/Settings/ApiKeys/`. Existing
`F_FE_1_CreateApiKeyDialogScope.test.tsx` retains its scenarios
verbatim as the M1.1 regression pin (its mocks already return
`isRbacEnabled: true`, so the picker is — and should remain —
rendered).
