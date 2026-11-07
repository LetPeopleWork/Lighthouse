# Slice 04: Everyone signed in can view, if we say so (US-04, ADO #6581)

**Goal:** a System Admin turns on one switch, and every signed-in person reads every Team and Portfolio, with no group.

**Hypothesis:** the rule's reach does not depend on groups, and opt-in holds. Disproved if an upgraded instance changes behaviour
with the switch off, or a person with no group claim still sees nothing with it on.

**In**
- The switch on Settings → Access, off by default, with a confirm dialog when turning on.
- Reads honour it for any person with a stable subject and a profile, including API keys and embed sessions (P6).
- The Access tab box names "everyone signed in".
- A session embedded in another tool speaks only for a person who still has a profile, as an API key already does.

**Out**
- Admin through the switch (P2). Separate choices for Teams and Portfolios (D2).
- Any change to sign-in, or blocking removed people (P7).

**Acceptance criteria:** AC-4.1 to AC-4.10.

**Security negatives**
- Switch off means no change for anyone.
- Turning it off ends the reach on the next request and leaves real roles alone.
- Only a System Admin may change it; it is not offered before the first System Admin exists.
- It grants reading only; a key whose owner has no profile reads nothing, and neither does an embedded session.

**Check while building:** the guard in `CurrentUserProfileService` also skips the profile update, so find who reads
`LastSeenAt` for embed viewers and keep that behaviour.

**Dependencies:** slice 01; slice 02 for keys; slice 03 for the box. P2, P6, P7 answered.

**Effort:** 1 day. **Reference class:** a stored instance setting with a dialog on Settings → Access.

**Dogfood moment:** turn it on and sign in as `test@user.com`, who has no groups. Turn it off and reload.

**Data:** the Keycloak realm; turn the switch off at the end of the E2E step, since the spec shares one instance.

**Droppable:** nothing depends on it except the box's wording.
