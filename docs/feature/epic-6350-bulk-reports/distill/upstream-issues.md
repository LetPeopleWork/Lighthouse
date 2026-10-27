# Epic 6350 — issues for upstream waves found at DISTILL (2026-10-27)

None blocks DELIVER.

| Id | Issue | Status |
|---|---|---|
| UI-1 | Demo Teams are named "Team …", so an Owner cell of kind + name read "Team Team Zenith". | **Closed by NMD-11**: the cell shows the name alone; the kind is in the link's accessible name. |
| UI-2 | NMD-1's sketch showed the Owner without its kind while AC-1.2 asked for kind + name. | **Closed by NMD-11** and the revised AC-1.2. |
| UI-3 | The backend RBAC test double answered every Team as readable. | **Closed in DISTILL**: the double now scopes readable Teams to the caller's grants; full backend suite re-run, only the 2 known worktree Licensing tests fail. |
| UI-4 | The plain-language reasons for "Not created" / "Not removed" were not worded. | **Closed by NMD-12**; pinned word for word. |
| UI-5 | No copy for more than 50 owners picked in the owner step. | **Closed by NMD-13**; "Pick at most 50 Teams and Portfolios." pinned, group Select all uncapped. |
| UI-6 | The default "Then ends on" day was not stated. | **Closed by NMD-14**; the request's `thenEndDate` is pinned for 30 / 90 / 180 days. |
| UI-7 | The reason shown on Export PDF when nothing is selected is not worded by any decision (NMD-1 asks for one). | **Open, for the maintainer at the slice 07 sketch.** Pinned as "Select at least one report." in one kit constant (DS-15). |
| UI-8 | "It could not be confirmed which reports were removed." mirrors NMD-7's creation sentence; NMD-7 says only "the could-not-confirm look" for removal. | **Open, low.** One kit constant (DS-8). |
