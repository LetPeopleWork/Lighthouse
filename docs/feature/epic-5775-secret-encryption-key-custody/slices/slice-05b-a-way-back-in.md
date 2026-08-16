# Slice 05b — A way back in after the key is gone

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5790 · **Estimate**: ~5h
**Origin**: not from DISCUSS. Found by the manual verification walkthrough of 2026-08-16
(`verification/manual-walkthrough.md`, finding F-26; maintainer decision V1).

## Goal

An operator whose key is genuinely gone can start Lighthouse, see exactly which credentials are
unreadable, and re-enter them — instead of owning a database nothing can open.

## The defect

Refusing to start when nothing stored can be read is correct. Having no way past it is not.

The refusal fires during bootstrap, before the web host is built and long before a port is bound. The
process writes one FATAL line and exits; on Docker with `restart: always` it does that forever. Pointing
it at a fresh key store refuses again, because the database still holds unreadable secrets. Nothing in
the codebase can override it — no flag, no confirmation path. The only route back is manual surgery on
the secret columns, undocumented, and requiring the operator to know which three columns those are.

The blast radius is out of all proportion: the unreadable values may be two API tokens, and the refusal
takes teams, forecasts and history down with them.

## IN scope

- A configuration switch, delivered exactly as `Authorization:EmergencySystemAdminSubjects` is, that
  skips the unreadable-secrets refusal and lets the instance start.
- **Visible while in force.** That precedent surfaces the setting through `SystemInfo` and `RbacStatus`
  precisely so it cannot sit switched on unnoticed; this one needs the same, on the encryption panel
  and on the startup line.
- The panel hands the operator the list of Connections and fields to re-enter — the check pass already
  produces exactly that.
- The refusal's own wording (F-16, F-17): lead with *remove the key you just set*, name both key ids,
  and stop asserting "nothing is lost" as fact when the application cannot tell a misplaced key from a
  destroyed one.

## OUT of scope

- Removing keys from the ring. That is a later action which this slice makes safe, not part of it.
- Automatic detection of which case an operator is in. The application genuinely cannot tell.

## Constraint to settle while building

The guard that stops a save overwriting a credential it cannot read is correct and must stay — **but it
must not block an operator supplying a new value.** Otherwise the hatch opens the door and leaves the
room locked, and this slice delivers nothing.

## Acceptance criteria

- With the switch set, an instance whose every stored secret is unreadable starts and serves requests.
- While it is set, the encryption panel and the startup line both say so, unprompted.
- Re-entering a credential on a Connection whose stored value is unreadable succeeds and is written
  under the active key.
- Without the switch, behaviour is unchanged: the instance still refuses.

## Dependencies

Slices 01–04 (the check pass supplies the to-do list). Independent of slice 05.

## Verdict

**Shipped 2026-08-16.** Five changes.

- The readability probe reports the key ids the stored values name, gathered from the pass it already
  makes rather than from a second query.
- The refusal was rewritten around them: it names the key the instance started on and the key the
  credentials say wrote them, leads with *remove the key you just set* where there is something to
  remove, names the way past itself and what that costs, and stops asserting that nothing is lost — it
  now says the thing that is true in both cases, that this start changed nothing.
- `Encryption:StartEvenIfNothingStoredCanBeRead` lets an instance past that one refusal. It is applied
  at the refusal rather than around the whole resolution, so every other refusal still fires and none of
  their messages offers a setting that would not help. Where it is set the readability question is not
  asked at all.
- The startup line and the encryption panel both say so, spelled from the same constant.
- The re-entry path turned out to need **no production change** — a value an operator types arrives as
  plaintext, and plaintext has always been encrypted — so it is pinned by a test instead of assumed. The
  guard that protects a value nobody retyped is untouched.

**One deviation from the roadmap**: the switch (phase 3) was built before the refusal's wording
(phase 2), because the wording quotes the setting name and a message naming a setting that does nothing
would have been wrong for the length of one commit.

Mutation: **97.73 %** backend on the four files this slice touched, second run. The first said 73.43 %
and the distribution said more than the number did — the switch and its guard were 32 of 32 killed on
the first pass, and every survivor was a sentence, a marker or a piece of banner older than this slice.
Recorded in `mutation/results-5790.md`.

Owed before the epic closes: the **D2** walkthrough run repeated end to end — start with the switch,
read the list, type the credentials back in, remove the switch — and **A2d**, whose refusal must now
lead with removing the key that was just set and name both key ids.
