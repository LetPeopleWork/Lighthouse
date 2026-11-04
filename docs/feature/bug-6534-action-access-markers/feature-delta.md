# Bug 6534: every controller action declares its access

Root cause and census: `rca.md`. The bugfix flow runs RCA, maintainer review, then DELIVER, so there are no
DESIGN, DEVOPS or DISTILL waves.

## Maintainer decisions (2026-11-04)

- Fix every hole in this change rather than raising separate Bugs: the single-connection GET, both validate
  endpoints, the manual delivery selection across portfolios, and the rank target.
- `GET /license`: leave out licensee name, email, organisation and licence number for anyone who is not a System
  Admin. Validity and dates stay. Admins see no change. If the admin check cannot be answered, the details stay
  hidden. The dialog already hides empty rows, so no frontend change is expected. Sketch approved.
- `POST /surveynudge` stays open to any signed-in user, with the reason on its marker.
- Two markers, both with a required reason: `SignedInOnly` and `CheckedInHandler`.
- Existing `[Authorize]` attributes stay.
- The database write in the single-connection GET stays, behind the new guard.
- `AuthorizationController` carries a reason per action.
- Rigor: `thorough` for the guards and hole fixes, `standard` for the marker sweep.
- A Team Admin may pick any connection; an unreadable owner may show as empty (decided under Bug 6533).

## AFK session

Slice wrap-up: refactor and review, mutation once at the end. Nothing is pushed.

## Maintainer decisions during the run (2026-11-04)

- Remove the unused `LicenseStatusDialog` in this change (step 02-03).
- Non-admins losing the Renew prefill and seeing "N/A" in the Contact Support mail is accepted; nothing to do now.
- Moving the write out of the single-connection GET belongs in the improvement backlog.
- No check for Features already pulled in through the old delivery hole.
- The report removal probe and the redundant `[Authorize]` attributes stay out.
- `UpdateNotificationHub` stays as it is: any signed-in user can ask for the update status of any id, which gives only the id, progress and timestamps.
- Moving the write out of the single-connection GET is Story #6540 under Epic 6508.

## Decisions during the final gates (2026-11-04)

- A restricted API key could create an unrestricted one (found in the Slice 2 review, pre-existing). Fixed in this
  change, under "fix every hole in this change". The maintainer was told before the push.
- A moved Feature the caller cannot read answers 404, like `GetFeatureWorkItems`.
- Markers sit on actions only, a change to the roadmap's class-level plan: a class-level marker would cover every future
  action in that controller.
- The rule scans every public controller method, not only those with an HTTP attribute.
- No hold: the maintainer approved rebase, push and finalize before the final gates finished, and checks Save by hand
  afterwards.

## Open, for the maintainer

- `GET authorization/status` gives every signed-in user the unassigned user count and the group claim name.
- A restricted key whose owner is an Emergency Admin is not really restricted: `CanManageRbacAsync` answers yes before it
  looks at the key. Read from the code, not tested.
- Controllers fall back to "all requested" when a readable-ids call returns null (`??` in Teams, Portfolios, Suggestions,
  Features). The services never return null today.
- `lighthouse-clients` was not checked: non-admin keys get 403 on `connection get`, delivery calls get 404 for outside
  Features, and a restricted key can no longer create a key without scopes.
