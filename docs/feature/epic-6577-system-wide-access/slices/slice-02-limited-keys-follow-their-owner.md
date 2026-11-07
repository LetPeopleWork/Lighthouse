# Slice 02: Limited API keys follow their owner (US-02, ADO #6579)

**Goal:** a key limited to one unit works when its owner reaches that unit through a rule, and reaches nothing else.

**Hypothesis:** a key can follow a rule and stay no wider than its limit. Disproved if a limited key either reads nothing or reads
beyond its limit.

**In**
- A limited key's coverage check accepts an owner's system-wide reach, at the key's exact scope.
- The refusal for asking more than the owner has, unchanged.

**Out**
- A key limited to "Viewer on all" (P13). Refreshing a stale group snapshot (P11).
- Admin keys; slice 05 extends the same check.

**Acceptance criteria:** AC-2.1 to AC-2.6.

**Security negatives**
- A key never exceeds the lower of its limit and its owner's role.
- Removing the rule ends the key's reach on its next request.
- A limited key does not widen to all units.

**Dependencies:** slice 01. P11 answered.

**Effort:** 0.5 day. **Reference class:** the per-key scoping of API keys.

**Dogfood moment:** as `teamreader@user.com`, create a key limited to Team Zenith and call `GET /api/latest/teams` from a shell.

**Data:** a real key on the dev instance with the Keycloak user; the owner's snapshot comes from a real sign-in.
