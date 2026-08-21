# Upstream Changes — epic-5698-deliveries-as-durable-records (DESIGN → DISCUSS)

One correction. **No user story and no acceptance criterion changes.** What changes is a stated
*mechanism* in the surface inventory and in one slice brief — the requirement each expresses is
unchanged and still met.

---

## UC-1 — The RBAC mechanism for `{deliveryId}`-rooted routes

### Affected artifacts

1. `feature-delta.md:50` — surface-inventory row **S10**
2. `slices/slice-02-note-a-delivery-moment.md:12`

### Original (verbatim)

`feature-delta.md:50`:

> | S10 | All Delivery writes gate on `RbacGuardRequirement.PortfolioWrite` with `ScopeIdRouteKey`; reads on `PortfolioRead`. | `DeliveriesController.cs:31,76` |

`slices/slice-02-note-a-delivery-moment.md:12`:

> with `ScopeIdRouteKey`, matching the existing Delivery endpoints.

### Replacement

S10 becomes:

> | S10 | All Delivery writes gate on `PortfolioWrite` and reads on `PortfolioRead`, but by **two different mechanisms**. The `portfolio/{portfolioId}` routes use the `[RbacGuard(..., ScopeIdRouteKey = "portfolioId")]` attribute — `DeliveriesController.cs:31,76` are the only two `[RbacGuard]` attributes in the file. The `{deliveryId}`-rooted routes (`GetMetricsHistory`, `UpdateDelivery`, `DeleteDelivery`) carry **no** attribute; they resolve the scope in-action via `IDeliveryRepository.GetPortfolioId(deliveryId)` and call `IRbacAdministrationService.CanSatisfyRequirementAsync`, returning `Forbid()` when it fails. | `DeliveriesController.cs:31,76` (attribute); the `{deliveryId}` actions (in-action) |

Slice 02's line becomes:

> gated on `PortfolioRead` / `PortfolioWrite` resolved **in-action** via
> `IDeliveryRepository.GetPortfolioId(deliveryId)`, matching how the existing `{deliveryId}`-rooted
> Delivery endpoints do it. `ScopeIdRouteKey` cannot be used here — see below.

### Why

`RbacGuardAttribute` (`Services/Implementation/Authorization/RbacGuardAttribute.cs`) exposes exactly
three knobs — `Requirement`, `ScopeIdRouteKey`, `Check` — and resolves the scope id from a **route
value**. There is no resolver hook and no body/entity lookup.

A `{deliveryId}` route carries no `portfolioId` route value, so the attribute structurally cannot
scope one. That is *why* the three existing `{deliveryId}` endpoints do it in-action instead, and why
`IDeliveryRepository.GetPortfolioId(int deliveryId)` exists on the repository at all.

The global fallback policy is `RequireAuthenticatedUser` (`Program.cs:807-808`) — authentication, not
authorisation. So an endpoint that reaches for `ScopeIdRouteKey = "portfolioId"` on a route without
that value does not fail loudly: it degrades to authenticated-only, and it passes any test that
merely checks a logged-in caller succeeds. Six of this epic's endpoints are `{deliveryId}`-rooted, so
this would have shipped six silently unscoped write endpoints.

### Impact on scope

None. The six routes, their HTTP verbs and their required permissions are unchanged from the DISCUSS
Driving Ports table. Only the implementation idiom is corrected. Recorded as **D26** in
`feature-delta.md` → *Wave: DESIGN / [REF] DDD List*, and as a binding constraint in
`docs/product/architecture/brief.md` → *Application Architecture —
epic-5698-deliveries-as-durable-records* → *Key invariants introduced*.

### Follow-on, deliberately not taken here

Extending `RbacGuardAttribute` with a delivery-id scope resolver would make all six routes
declarative and reflection-testable, and would let the three existing in-action endpoints be
simplified too. That touches an auth-critical filter for the benefit of one epic, so it is recorded as
open question 1 in *Wave: DESIGN / [REF] Open Questions* rather than folded in here.
