# Slice 06b — What a caller who is merely signed in gets to learn

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5793 · **Estimate**: ~3h
**Origin**: not from DISCUSS. Maintainer decisions **V5** and **V6** of 2026-08-16, taken against
walkthrough finding **F-1**.

## Goal

An administrator diagnosing an instance sees which key it is on in the place they actually look, and a
caller who is only signed in learns nothing about the security posture of the installation.

## The two halves

They are one decision made twice, which is why they are one slice.

**The custody line is missing where operators look for it.** The startup banner is the design's primary
custody surface, and the entire standalone population never sees a console. The encryption panel added
in slice 02 answers half of it — but an operator working out why an instance is behaving oddly opens
system information first, and that page is silent about encryption.

**The response that page is built on is unguarded, deliberately.** `GET /api/systeminfo` answers before
anyone is authorised, because the application shell needs the version and the authentication posture to
render at all — and a viewer who opens Lighthouse inside an embedded frame satisfies "signed in". That
is why key state was kept off it. The rule stands; what changes is that the response can carry a field
only some callers are given.

**And it already leaks something of the same kind.** `emergencyAdminSubjects` is on that response
today, unguarded, and it does not name a category — it names real people who can administer the
installation. It predates this epic. It is fixed here rather than in a bug of its own because leaving
two halves of one decision in two places is how they come to disagree.

## IN scope

- A `SystemAdmin`-only encryption custody field on the system information response: **served as null to
  every other caller**, and the row drawn in the UI only when it is present.
- The same shortened custody wording the banner uses after slice 06a — custody word, then the path. No
  key id, no key material.
- `emergencyAdminSubjects` given the same treatment: present for a System Administrator, absent
  otherwise.
- One place decides who may see these, so a third field added later inherits the answer rather than
  re-deriving it.

## OUT of scope

- The encryption panel itself (slice 06a).
- Any change to what `GET /api/systeminfo` tells an unauthenticated caller — it is already nothing.
- Removing fields from the response wholesale. The version and the authentication posture stay
  unguarded; the shell cannot render without them.

## Acceptance criteria

- A System Administrator reading system information sees which custody the key is under and where the
  key store is.
- A signed-in caller who is not a System Administrator receives neither that field nor
  `emergencyAdminSubjects`, and the UI draws neither row.
- An unauthenticated caller is unaffected — the response still carries the version and the
  authentication posture, and nothing else changed.
- No key id and no key material appears on this response under any role.
- The custody wording on this page and on the startup line come from one place and cannot disagree.

## Dependencies

Slice 06a, for the shortened custody wording. Independent of slices 05 and 06.

## Verdict

**Shipped 2026-08-16.** Both halves, and they did turn out to be one decision.

- **The narrowing lives on the record**, naming the two fields only a System Administrator may be told.
  A test walks the properties by reflection and fails if that set ever changes without somebody
  deciding — which is exactly how the emergency administrators came to be on an unguarded response in
  the first place.
- **A withheld field is left off the wire, not sent empty.** What a viewer receives is byte for byte
  what it was before this field existed, and the page draws no row. An empty property announces that
  there is something here you are not being shown.
- **The custody sentence is written once and rendered twice** — the banner prints its line, the page
  reports the same string, and neither can be reworded without the other following.

Three things found while building rather than from the brief:

1. **The RBAC question can fail, and must not fail the request.** Deciding who is an administrator
   reaches the database; everything else on that response is read from configuration and the running
   process. It is what the application shell fetches before it can draw anything, so a database that
   will not answer used to leave it working and has to keep doing so. It fails closed instead.
2. **A caller who never signed in is settled without asking anybody.** Nobody is not an administrator,
   and the route deliberately admits anonymous callers where there is no authentication at all.
3. **The custody description is computed once at startup.** Correct today because custody cannot change
   without a restart — a rotation replaces the key inside a custody, it does not move the key between
   custodies. Recorded here because that is an assumption, not a guarantee.

One cost paid on the way: Sonar failed the gate on the eight-parameter constructor slice 06a left
behind, and the two ports the encryption panel used for one screen were merged to fix it. That departs
from the recorded rule in `docs/ci-learnings.md`, which says to suppress rather than aggregate, and the
departure is written down there with instructions to revert it.
