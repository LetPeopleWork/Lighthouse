# Slice 03: The Access tab says who else reaches a unit (US-03, ADO #6580)

**Goal:** a Team or Portfolio Admin sees, read-only, that a system-wide rule also reaches their unit, and which one.

**Hypothesis:** a read-only line is enough for a Team Admin to trust who sees their Team. Disproved if, at the sketch review or the
dogfood, a removed member who still reads remains unexplained.

**In**
- An information box above Team Access and Portfolio Access, naming role and group.
- A read of the rules for a unit, for those who manage it and System Admins.
- The list built from the rule's "applies to" part.

**Out**
- Any control in the box. Any per-unit opt-out. A list of the people a rule reaches.
- Fixing the Team Admin's refused mapping form (P10).

**Acceptance criteria:** AC-3.1 to AC-3.5.

**Security negatives**
- A Viewer has no Access tab, and a direct request for the rules is refused.
- The box changes nothing; only Settings → Access does.

**Dependencies:** slice 01. Loading and failure looks from DESIGN: the box must not look like "no rules" while loading or failed.

**Effort:** 1 day. **Reference class:** `ScopedGroupMappingManager` on the Access tab.

**Dogfood moment:** as `teamadmin@user.com`, remove `teamreader@user.com` from Team Access, watch them keep reading, and read the box.

**Data:** the Keycloak realm and demo Team Zenith and Project Apollo.
