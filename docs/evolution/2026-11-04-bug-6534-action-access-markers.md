# Bug #6534: every controller action declares its access

**Pushed to main 2026-11-04, not yet released** · ADO Bug #6534 · workspace
`docs/feature/bug-6534-action-access-markers/` (RCA, roadmap, execution log, mutation record) · commits
`edb333ef3..4efa4a094` · follows Bug #6533

## What users get

- **Reading one connection needs a System Admin**, like the list, `PUT` and `DELETE`. Any signed-in user could read it
  by id before.
- **Validating Team and Portfolio settings needs an admin of some kind.** A Viewer gets a 403. Save still works for Team
  and Portfolio Admins.
- **A hand-picked Delivery only takes Features of its own Portfolio.** A Feature it already holds stays. This holds for
  every caller, because the hole was showing another Portfolio's forecasts.
- **A Feature only moves next to a Feature the caller may read**, and a Feature the caller cannot read answers as if it
  did not exist. A move next to a hidden Feature used to succeed, rewrite places and start forecast runs.
- **Licensee details are for System Admins.** Name, email, organization and license number are left out for everyone
  else. Nothing changes with auth or RBAC off.
- **A restricted API key cannot create an unrestricted one.** It could, and the new key carried all of its owner's
  rights, System Admin included.

## How it happened

1. Nothing forced an action to declare who may call it, so an action with no guard looked the same as one that needs
   none.
2. On 2026-05-12, `e0aef89bb` and `6e23f31b6` removed guards that broke Save for scoped admins. The fix for a too
   strict guard was no guard, and nothing noticed the open route.
3. `docs/ci-learnings.md` then recommended exactly that. It now says to pick the requirement that holds for every
   caller.

## What changed

- Two markers, `SignedInOnly` and `CheckedInHandler`, on the action only, with a required reason. They enforce nothing.
- `EveryActionDeclaresItsAccessArchitectureTest` fails the build for any public controller method that declares no
  access, with no allowlist. It also refuses a guard that checks nothing, a scoped guard with a bad key, and a reason too
  short to say anything. 63 actions on 21 controllers gained a marker: 46 `CheckedInHandler`, 17 `SignedInOnly`.
- `ConfirmsSystemAdminAsync` is the one rule for a route open to everybody that tells a System Admin more.
- `FeatureRepository.GetById` reads one Feature in the database instead of loading all of them.
- The unused `LicenseStatusDialog` is gone.

## Lessons

- **Writing the reason found holes the census had not.** Re-reading each check for its marker, and a review of those
  reasons, turned up the API key escalation: a reason claimed "may not carry broader rights", and one path did. A marker
  that must state a reason is a prompt to check the claim.
- **The cheapest reviewer found the most serious bug.** A Haiku review opened the handlers and reported the escalation,
  which the census and the Sonnet reviews had not. It also reported a test floor that measured the wrong scan. Do not
  skip a review because a stronger one ran.
- **A class-level marker would have rebuilt the original hole.** It covers every future action in the controller, so
  the rule would pass an unchecked `[HttpDelete]`. Markers now compile on actions only.
- **A status answer inside the `try` of a write reports a stored licence as failed.** Moving a call behind a new
  failure point changes what the surrounding `catch` means.
- **`SingleOrDefault` on `GetAll()` loads the table.** A review caught it only because a new call doubled the cost.

## Verification

- Backend: 12,529 passed, 0 failed on the rebased code.
- Frontend: 9,408 passed, `pnpm build` clean. `pnpm test` still exited non-zero locally on the time budget:
  `ModifyTeamSettings.readiness.test.tsx` took 2.19 s against 2 s. That test is unrelated and passes alone.
- Mutation: changed lines 31 of 31; whole files 68.25 %, because the HTTP acceptance suites are outside the Stryker
  filter. See `mutation/results.md`.
- The maintainer checks Save as a Team Admin and a Portfolio Admin by hand on an RBAC-on instance, after the push.
