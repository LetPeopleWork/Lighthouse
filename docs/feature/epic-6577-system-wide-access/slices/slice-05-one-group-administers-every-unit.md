# Slice 05: One group administers every unit, without running the system (US-05, ADO #6582)

**Goal:** a group's members edit settings and members on every Team and Portfolio, and do nothing a System Admin alone may do.

**Hypothesis:** wide write can stop short of System Admin rights. Disproved if any row marked "stays with System Admin" in the
capability table is reachable, through the UI or the API.

**In**
- Admin on all in the Role choice on Settings → Access.
- Team and Portfolio writes, unit member management, reports, and the settings forms' connection fields honour it.
- `useRbac()` shows Settings and Access tabs on every unit, including later ones.
- Proof that Admin on all is refused `DELETE` on a Team and a Portfolio; the guard itself comes from Bug 6575.
- Limited keys under an owner with Admin on all.
- The Access tab box says "Admin".

**Out**
- Creating, cloning, deleting units; connections; system settings; RBAC admin; licence details (P1).
- Admin through the switch (P2). The Team Admin mapping form (P10).

**Acceptance criteria:** AC-5.1 to AC-5.8.

**Security negatives**
- Admin on all cannot create, clone or delete, change a rule, manage connections, read licence details or update all Teams.
- Removing the mapping ends the reach on the next request.
- Nothing makes everyone signed in an admin.

**Dependencies:** slices 01, 02, 03. Bug 6575 has shipped. P1, P5, P14 are answered.

**Effort:** 1 day, top of the band; a watch item.
**Reference class:** Team Admin's reach, and the write twins in `RbacAdministrationService`.

**Dogfood moment:** map `team-admins` as Admin on all, sign in as `teamadmin@user.com`, edit a Team outside their usual scope, then try
`DELETE` from a shell.

**Data:** the Keycloak realm and demo data.

**Droppable:** nothing depends on it.
