# The Save Button Took the Validation With It — Evolution

**Feature:** bug-5628-settings-autosave-validation | **ADO:** Bug #5628 | **Shipped:** 2026-08-01 | **Commits:** `bb598bb5a..da18633b1`

## What shipped

Found while dogfooding US 5612: a Work Item Type was entered on an existing team with a typo — `Change Reqest`. The page said everything was saved. No validation error appeared anywhere. On refresh the team held zero work items, with nothing saying why.

Editing an existing team or portfolio now validates the settings against the work tracking system *after* the auto-save settles, and surfaces the connector's own verdict as a warning. The save still happens — the warning reports on what was persisted rather than gating it.

| Step | Outcome |
|---|---|
| 01-01 | `useModifySettings.validation.test.ts` — 8 cases pinning the auto-save path. Four reproduce the defect, four are invariant guards (the write must not become blocked, the default-settings pages must not probe, the warning must clear on the next edit). |
| 01-02 | `maybeValidateAfterSave` in `useModifySettings.ts`: one probe per settled save, behind four gates, surfaced through the alert both settings pages already rendered. |
| 01-03 | Four more cases, written against the mutation survivors — the fingerprint's field-level content, the queued-payload guard, and the non-`ApiError` branch. |

## Root cause

**Auto-save was added as a second save path through the same hook without inheriting the validation gate the button path had; removing the Save button then made the gated path unreachable.**

`useModifySettings` ended up with two ways to save:

- `handleSave` awaited `validateSettings` and refused to save when it failed.
- the auto-save effect debounced straight into `dispatchSave` → `saveSettings`, and never referenced `validateSettings` at all.

`ModifyTeamSettings.tsx` set `autoSave: { enabled: true, … }`, passed `validateTeamSettings` into the hook, and never destructured `handleSave` — the page has no Save button, only a `SaveStateIndicator`. `ModifyProjectSettings.tsx` did exactly the same. Since those two pages are the hook's only consumers, `handleSave` — and with it the entire `validationError` path — was dead code in production, and connector validation only ever ran in the create wizards.

Two things made the failure completely silent:

- The alert that would have shown the verdict *already existed* on both pages (`ModifyTeamSettings.tsx:316-322`, `ModifyProjectSettings.tsx:348-354`), complete with a technical-details expander. It was wired to state only `handleSave` could set. The UI was ready; nothing could ever populate it.
- A work item type that matches nothing is not an error anywhere downstream. The query executes, returns zero rows, and the sync reports success. For ServiceNow this bypassed the whole ADR-124 probe ladder — built precisely to stop a mistyped type from silently selecting zero rows — on the one page where a coach is most likely to mistype.

## The design question, and why it needed a decision

The obvious fix — validate on the auto-save path — is wrong. Auto-save debounces at 300 ms, and validation is a real HTTP round-trip to Azure DevOps, Jira, Linear or ServiceNow; for ServiceNow it is one probe *per named kind of work*. Validating per debounce tick would turn typing into a request storm.

Three directions were weighed:

| | approach | why not / why |
|---|---|---|
| A | validate once *after* the save settles, warn non-blockingly | **chosen** — one probe per settled save, fixed in the hook so both pages inherit it, reuses the dead alert |
| B | validate when the work-item-types field loses focus | guards one field; a mistyped query or state stays silent |
| C | reintroduce an explicit Validate action | zero surprise cost, but nothing forces a coach to press it — the same silence for anyone who doesn't |

A keeps the auto-save contract honest: your edits *are* saved, and the warning is a report about them, not a gate in front of them. That is why the test suite carries an explicit guard asserting the write still happens when validation fails — if that case ever goes red, someone has turned the warning back into a gate.

## Cost control: the fingerprint

At most one probe per settled save, and none at all when nothing worth asking about changed. `connectorFingerprint` is `JSON.stringify` of `{workTrackingSystemConnectionId, dataRetrievalValue, workItemTypes}` — the only three inputs any connector's `ValidateTeamSettings` / `ValidatePortfolioSettings` reads. Editing a state list or renaming the team saves without probing.

It is set *before* awaiting, so a concurrent save cannot double-probe. The accepted consequence: a validation that fails with a network error will not re-probe the identical payload. Changing any connector-relevant field clears it.

## What the mutation run taught

The first StrykerJS run scored **67.86 %** — under the 80 % gate — and the survivor that mattered was `JSON.stringify({...}) → JSON.stringify({})`. A constant fingerprint still lets the *first* save probe and stops every later one, which is exactly what the suite's "one probe per settled save" and "no re-probe when nothing changed" cases assert. Every case probed at most once, so none of them could tell a working fingerprint from a broken one.

The lesson generalises: **a dedupe key needs a test that demands the second call**, not just one that counts the first. Step 01-03 added it and the score went to **85.71 %**. Details and the four accepted survivors: `docs/feature/bug-5628-settings-autosave-validation/mutation/results.md`.

## Also corrected

`docs/teams/edit.md` and `docs/portfolios/edit.md` both still said "before you can save a new or modified team, you'll have to *Validate* the changes… only after this you will be able to save." That has not been true of the edit pages since auto-save landed. Both now distinguish the create wizard (validation is a gate) from the settings pages (saved automatically, validated afterwards, warned about).

## Left alone deliberately

- `handleSave` stays in the hook, still unreferenced by both pages. It is the hook's public contract; deleting it is a refactor, not part of this fix.
- Unmapped states still produce no warning. An unmapped state legitimately means the item does not exist for Lighthouse — `Canceled` is the canonical case.
- The two `isLatest()` guards survive mutation. Killing them needs a superseded request whose validation resolves after a newer one, a race the fake-timer harness can only stage by reaching into the hook's internals.
