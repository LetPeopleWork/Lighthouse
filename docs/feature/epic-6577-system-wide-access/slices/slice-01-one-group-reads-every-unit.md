# Slice 01: One group reads every Team and Portfolio (US-01, ADO #6578)

**Goal:** a System Admin gives one SSO group Viewer on all, and its members read every unit from first sign-in.

**Hypothesis:** one rule can reach all read paths, which are built on exact unit keys, without touching each caller.
Disproved if a read route still answers "no access" for a member. Confirms that the customer's Head of Engineering needs no
System Admin role.

**In**
- Settings → Access: heading "SSO Groups", a Role choice with System Admin and Viewer on all, the "Applies to" column.
- A rule with a distinct "applies to" part, first value "all Teams and Portfolios".
- Team and Portfolio reads honour the rule, including units created later.
- Unlimited API keys follow their owner through the stored group snapshot (no change expected; pin it).

**Out**
- Limited API keys (slice 02), the Access tab line (slice 03), the switch (slice 04), Admin on all (slice 05).
- A role for one named person. A key limited to "Viewer on all".

**Acceptance criteria:** AC-1.1 to AC-1.8.

**Security negatives**
- A person outside the group sees nothing new.
- A Viewer on all cannot write; only Refinement votes and comments stay open, per P4.
- Removing the mapping ends the reach on the next request.
- No rule means no change, and only a System Admin can create one.

**Dependencies:** P3, P4 answered. Sketch reviewed at the start of DISTILL.

**Effort:** 1 day. **Reference class:** Story 4965's scoped group mappings.

**Dogfood moment:** on the dev instance with Keycloak, map `team-readers` as Viewer on all and sign in as
`teamreader@user.com`: all demo Teams appear. Create a Team; it appears. `portfolioreader@user.com` stays outside.

**Data:** the real Keycloak realm from `examples/keycloak` and demo data, not fixtures alone.

**Release:** goes out together with slice 02, because this slice alone lets a limited key be created that reads nothing.
