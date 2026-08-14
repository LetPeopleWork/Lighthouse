# ADR-152: Custody is a property of the resolved ring, and minting is offered only where the app owns a durable store

**Status**: Accepted
**Date**: 2026-08-14
**Feature**: `epic-5775-secret-encryption-key-custody` (ADO Epic #5775, slices 03 and 04 / Stories #5778, #5779)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Implements**: D6, D9 · AC-2.8, AC-3.8, AC-3.10, AC-3.11, AC-3.12, AC-4.7

---

## Context

D6 splits rotation into two jobs. *Minting* creates and persists a key; *re-encrypting* walks every
stored secret onto the active one. Re-encryption is always the application's. Minting belongs to whoever
owns custody, and the panel must never offer to mint a key the application cannot persist — because
persisting into a Kubernetes Secret would mean granting Lighthouse write access to its own Secret, a
permission that should not be granted and that an external secret store would overwrite on its next
sync anyway.

D6 groups the custody modes as *app-owned* (standalone exe, Docker) and *operator-owned* (a Kubernetes
Secret). The journey YAML's `${key_source_label}` note makes the same split and puts "supplied by
configuration" on the app-owned side.

**That grouping is wrong for the configuration-supplied case, and it is the one correction this ADR
makes to an upstream decision.** An operator who sets `Encryption__Key` owns that key. If Lighthouse
minted a new one over it, the mint would go to the generated key store — and on the next restart the
configured key would win the precedence order again, making the newly-minted key inaccessible and every
secret written under it unreadable. A rotation that un-rotates itself on restart is worse than no
rotation button at all. Configuration-supplied custody is **operator-owned**, exactly like an external
secret, and for the same structural reason: the application cannot write to where the key came from.

So the three labels the DISCUSS wave defined stay, and the *capability* derived from them is what
changes: **three custody modes, two panel shapes.**

## Decision

**Custody is set once, by the resolver that produced the active key, and carried on the ring.**

```csharp
public enum KeyCustody
{
    GeneratedForThisInstance,   // resolver case: generated file in a durable key store
    SuppliedByConfiguration,    // Encryption:Key / Encryption:Keys
    SuppliedByExternalSecret,   // Encryption:KeysFile — a mounted Secret
    NoDurableStore,             // ADR-149 case 4: running on the published default, told so
}
```

**Mintability is a derived predicate, not a fourth field**:
`CanMint => Custody == GeneratedForThisInstance`. One expression, one place, so the panel and the
endpoint cannot disagree about it.

**One guarded controller, four routes.** `EncryptionController` at
`/api/v1/encryption` and `/api/latest/encryption`, every route `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`:

| Method | Route | Purpose | Slice |
|---|---|---|---|
| GET | `/encryption` | `{ custody, canMint, activeKeyId, keyIds[], keyStorePath, legacyDefaultPresent }` — the panel's state, and the Settings → System key line | 02 |
| GET | `/encryption/secrets` | The readability report: per secret, the owning Connection, the field, the key id, and one of the four states | 04 |
| POST | `/encryption/rotate` | Mint, activate, re-encrypt, retire — one action. **409 when `canMint` is false**, so the refusal is a contract, not a hidden button | 03 |
| POST | `/encryption/reencrypt` | Re-encrypt onto the already-active key. Available in every custody mode | 03 |

**Key state is not on `GET /systeminfo`.** That endpoint is `[Authorize]` only, which after ADR-137
includes any viewer who reaches an embedded Jira frame. Which key an instance is on, and where its key
store lives, is instance security posture. The Settings → System page reads it from
`GET /encryption` — the operator sees the same thing AC-2.8 described, and an embed viewer sees nothing.
This is a correction to AC-2.8's wording, not to its intent.

**A separate route rather than widening `WorkTrackingSystemConnectionDto`** for the report, per ADR-006's
one-route-one-shape precedent and because that DTO is a Lighthouse-Clients contract. The *per-field*
`secretState` on the Connection detail payload (ADR-147) is a different thing and does widen that DTO,
because AC-1.6 requires the state to appear on the field that owns it.

**The panel is custody-aware and says which mode it is in.** It always lists the key ids the ring holds,
so an operator who has just added a key to their Secret can see it has arrived before triggering
re-encryption (AC-3.12). Where `canMint` is true it offers **Rotate key**. Where it is false it offers
**Re-encrypt onto the active key** and states, in one sentence, who owns the key and what the operator
does to mint one. It never renders a disabled Rotate button — a control that exists and cannot be used
teaches the wrong model of who owns what.

**UI gating derives from `useRbac()`.** The Encryption panel is rendered only for
`rbac.isSystemAdmin`; no component fetches the authorization summary directly. The panel's own
`canMint` comes from `GET /encryption`, which is authorization state about the *instance*, not about the
*user*, and the two are not conflated.

**Rotation is recorded.** One structured log entry — `encryption.rotation.completed` with the actor's
subject, the timestamp, the counts moved and unreadable, and the new active key id. No key material
(AC-3.8, D9). No new audit table: the existing logging path is the record, consistent with how every
other administrative action in the product is recorded.

## Alternatives Considered

**A `canRotate` boolean supplied by configuration.** The operator declares whether the app may mint.
**Rejected**: it asks a human to keep a fact in sync that the application already knows with certainty
from where its own key came from, and the failure mode of getting it wrong is a mint into a store that
does not survive a restart.

**One `POST /encryption/rotate` that quietly does re-encryption-only when it cannot mint.** One route,
one button, no fork in the UI. **Rejected**: the two actions have different preconditions and different
outcomes, and an operator who pressed "Rotate" and got a re-encryption onto the *same* key would
reasonably believe their exposure was contained when it was not. The whole value of the panel is that it
tells the truth about who owns the key.

**Put key state on `GET /systeminfo` as AC-2.8 says.** No new endpoint for slice 02. **Rejected** on the
ADR-137 finding: that endpoint's audience includes embed viewers, and telling an anonymous-ish framed
viewer which key store path an instance uses is a disclosure with no upside.

**A disabled Rotate button with a tooltip in operator-owned mode.** Discoverable — the operator learns
the feature exists. **Rejected**: it models minting as something Lighthouse *could* do and is declining
to, which is the opposite of the truth. The replacement sentence teaches the correct model in the same
space.

## Consequences

**Positive**

- The panel cannot offer an action that would corrupt the instance, because the offer is derived from
  the same fact that decides whether the action is possible.
- The configuration-supplied correction removes a rotation that would have silently un-rotated on the
  next restart — a defect that would have shipped and been very hard to attribute.
- Key state gains an audience-appropriate surface, and `GET /systeminfo` keeps the property that made it
  safe to leave unguarded.
- Re-encryption is one endpoint and one code path in all four custody modes (AC-3.13); only the mint
  step forks.

**Negative / accepted**

- A fourth custody value, `NoDurableStore`, that the DISCUSS wave did not name. It is not a new mode so
  much as an honest name for the case ADR-149 found, and it exists so the panel can say something true
  rather than mislabel a shared key as "generated".
- One new controller and four routes, against widening an existing one. Justified by the guard: every
  route here is System Admin, and `SystemInfoController` is deliberately not.
- The frontend grows a Settings → Encryption panel with two shapes. It is the smallest surface that can
  express D6, and both shapes share the key-id list and the readability table.

## Earned Trust — what is probed, not assumed

| Assumption | Probe |
|---|---|
| The mint refusal is a contract, not a UI convention | Integration test: `POST /encryption/rotate` with `Encryption:Key` configured → 409, and the ring on disk is unchanged. The refusal must hold with the UI bypassed |
| A non-admin cannot read key state | Integration test per route: viewer principal → 403; embed-session principal → 403 |
| `GET /systeminfo` discloses nothing about keys | Contract test asserting the serialised payload's property set is exactly today's |
| No key material in the rotation record | Test on the emitted structured properties: exactly `{Actor, MovedCount, UnreadableCount, NewActiveKeyId}` |
| Custody is derived, never configured | Structural test: no configuration key named `canRotate`, `canMint` or equivalent exists |
| The panel's two shapes match `canMint` | Frontend test per custody value: `GeneratedForThisInstance` renders Rotate and no Re-encrypt-only copy; the other three render Re-encrypt and **no** Rotate control at all, disabled or otherwise |

## Cross-reference

- [ADR-149](./adr-149-key-store-beside-the-database.md) — where `GeneratedForThisInstance` and
  `NoDurableStore` come from.
- [ADR-151](./adr-151-re-encryption-per-row-compare-and-swap.md) — the one component both actions call.
- [ADR-153](./adr-153-kubernetes-key-custody-is-operator-supplied.md) — the mode that makes the
  re-encrypt-only shape the common case in Kubernetes.
- [ADR-137](./adr-137-viewer-identity-embed-session.md) — the finding that makes `[Authorize]` an
  insufficient guard for instance posture.
- [ADR-006](./adr-006-connection-list-payload-shape.md) — the one-route-one-shape precedent.
