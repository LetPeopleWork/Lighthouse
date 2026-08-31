# RED classification — story-5876-behaviour-settings

Written 2026-08-31, from an actual run, and re-run after the DISTILL review gate. Every scaffolded
scenario was un-ignored, executed against the code on `main`, and classified by *why* it failed. Only
`MISSING_FUNCTIONALITY` is a correct RED; an import, fixture or setup failure would be BROKEN and would
block the handoff, because a crafter who "fixes" it turns the test green without the feature ever
having been exercised.

The scaffolds were then re-ignored, so the committed suite is green. DELIVER un-ignores one at a time.

**Result: 18 failures, all `MISSING_FUNCTIONALITY`. Zero BROKEN. Handoff is not blocked.**

## Backend — `Lighthouse.Backend.Tests/API/Integration/BehaviourSettings/`

Run: `dotnet test --filter "TestCategory=story-5876-behaviour-settings"` with every `[Ignore]` removed
→ **`Failed: 14, Passed: 11, Total: 25`**. Every failure is an `Assert.That` on behaviour; not one is
an exception, an import error or a fixture fault.

| Scenario | AC | Classification | The assertion that fired |
|---|---|---|---|
| `A_toggle_the_licence_does_not_cover_is_refused_out_loud` | 02.1 | MISSING_FUNCTIONALITY | `Expected: Forbidden / But was: OK` — the premium branch returns 200 carrying the unchanged row |
| `The_refusal_reads_the_same_as_the_one_the_other_door_already_gives` | 02.1 | MISSING_FUNCTIONALITY | Body is the serialised entity, not `Access Denied: Premium Features Required` |
| `Both_doors_refuse_an_unlicensed_administrator_in_the_same_words` | 02.1 | MISSING_FUNCTIONALITY | The two doors disagree on status; there is nothing to compare until the new one refuses |
| `Turning_the_ordering_setting_on_for_the_first_time_moves_nobody` | 01.5 | MISSING_FUNCTIONALITY, **blocked on UI-1** | `No behaviour setting is stored under 'FeatureOrdering'` |
| `The_places_are_seeded_in_the_order_the_admin_was_looking_at` | 01.5 | MISSING_FUNCTIONALITY, **blocked on UI-1** | ditto |
| `Giving_the_order_back_writes_no_places` | 01.7 | MISSING_FUNCTIONALITY, **blocked on UI-1** | ditto |
| `Handing_the_order_over_and_giving_it_back_both_re_queue_the_forecasts` | 01.6 | MISSING_FUNCTIONALITY, **blocked on UI-1** | ditto |
| `Taking_the_order_over_again_restores_the_places_this_instance_already_chose` | 01.7 | MISSING_FUNCTIONALITY, **blocked on UI-1** | ditto |
| `Each_setting_in_the_list_is_switched_on_its_own` | 01.1 | MISSING_FUNCTIONALITY, **blocked on UI-1** | ditto |
| `An_instance_that_already_owned_its_order_still_owns_it_after_the_upgrade` | 01.3 | MISSING_FUNCTIONALITY | `stored.Found / Expected: True / But was: False` |
| `An_instance_that_never_took_its_order_over_does_not_acquire_it_in_the_upgrade("SourceOrder")` | 01.4 | MISSING_FUNCTIONALITY | ditto |
| `An_instance_that_never_took_its_order_over_does_not_acquire_it_in_the_upgrade(null)` | 01.4 | MISSING_FUNCTIONALITY | ditto |
| `An_instance_that_never_took_its_order_over_does_not_acquire_it_in_the_upgrade("Nonsense")` | 01.4 | MISSING_FUNCTIONALITY | ditto |
| `A_write_through_the_deprecated_door_is_visible_through_the_new_one` | 01.8 | MISSING_FUNCTIONALITY | ditto |

### The UI-1 blast radius, stated in full

Six scenarios are marked **blocked on UI-1**, not one. Every scenario that toggles the ordering row
through the behaviour-settings port is blocked, because that port cannot address either row once both
are seeded — the store keys these rows by their key and nothing generates the number the route uses.

Two consequences DELIVER must know before slice 02 opens:

1. **A currently-green guard would flip red for a harness reason.**
   `The_setting_the_licence_has_nothing_to_say_about_is_taken_either_way` resolves `DeltaSync` and
   toggles it. The moment the second row is seeded, that row's identity is ambiguous too. A committed
   green test going red mid-slice for an addressing reason is the failure that gets "fixed" in the
   test rather than in the product.
2. **The harness names the failure rather than crashing on it.** `ToggleOptionalFeature` takes the
   setting's **key**, looks the row up by it, and asserts that exactly one row carries the resulting
   identity — quoting UI-1 when more than one does. Without that guard the collision surfaces as
   `InvalidOperationException: Sequence contains more than one element` from inside `GetById`, i.e. a
   500, which reads as BROKEN and sends a crafter looking in the wrong place.

Resolving UI-1 the recommended way — addressing the write by key — also changes this harness helper's
route shape. That is one deliberate edit, made once, rather than a red guard discovered mid-slice.

### Green already, and deliberately not ignored

These assert an invariant that holds on `main` today and must keep holding through both slices. They
are committed un-ignored so the suite guards them from now on rather than from the end of DELIVER.

| Scenario | AC | Why it is already true, and why it is kept |
|---|---|---|
| `A_refused_toggle_leaves_the_setting_exactly_as_it_was` | 02.1 | The write is already dropped. It is the *response* that lies, which is the scenario above |
| `A_toggle_the_licence_covers_is_taken_and_reported_back` | 02.2 | The licensed branch already works |
| `The_setting_the_licence_has_nothing_to_say_about_is_taken_either_way(True/False)` | 02.3 | `DeltaSync` is not premium; both licence states already pass. An inverted check cannot survive it |
| `The_setting_the_licence_has_nothing_to_say_about_is_still_not_premium` | 02.3 | ditto |
| `A_setting_that_does_not_exist_is_still_reported_as_not_found` | 02.1 | 404 comes from the lookup. The request claims to be premium, so a check hoisted above the lookup fires whether it reads the request or the store — a body claiming otherwise would let one of those two hoists through unnoticed |
| `The_door_this_setting_has_today_already_refuses_an_unlicensed_administrator` | 02.1 | The shipped `[LicenseGuard]` already answers 403. This is Epic #5375 AC-2.5 — the criterion the whole 403-first slice order exists to protect, and until now nothing asserted it on the door that delivers it |
| `A_write_through_the_deprecated_door_moves_nobody_either` | 01.8 | The alias already seeds before it writes. Asserted before the move rather than after |
| `An_instance_whose_licence_lapsed_keeps_the_order_it_already_owns` | 01.3 | Nothing in the ordering read path consults the licence. Kept because the move re-backs that path onto a row marked premium, and the obvious tidy-up — a licence check in the provider — would hand every lapsed customer's list back to their tracker and reorder every Feature they had placed, on their renewal date |
| `The_setting_that_was_already_in_the_list_is_carried_across_untouched` | 01.9 | Nothing changes `DeltaSync` yet, and nothing may. The probe covers name, description, premium status, **preview badge** and stored on/off — all five clauses of the AC |
| `The_upgrade_leaves_the_setting_it_migrated_from_in_place` | 01.10 | The app setting row survives a re-seed |

## Frontend — `src/pages/Settings/System/SystemSettingsTab.behaviourSettings.test.tsx`

Run with every `.skip` removed → **`4 failed | 5 passed`**.

| Test | AC | Classification | The assertion that fired |
|---|---|---|---|
| `puts every instance-wide switch under one heading` | 01.1 | MISSING_FUNCTIONALITY | `Unable to find an element with the text: Behaviour Settings` |
| `leaves no separate section behind` | 01.1 | MISSING_FUNCTIONALITY | ditto |
| `reads the row in the instance's own word for a Feature` | 01.11 | MISSING_FUNCTIONALITY | `Unable to find an element with the text: /arrange your Deliverables yourself/` — no resolver exists |
| `switches one setting without touching the other` | 01.1 | MISSING_FUNCTIONALITY, **UI-1's frontend twin** | `expect(element).not.toBeChecked() — Received element is checked` |

That last one reproduces UI-1 in the browser: with both rows carrying the identity the seeder really
writes, clicking *Faster Updates* also flips the ordering switch, because the optimistic update matches
rows on that number. It fails today and survives any backend-only fix.

### Green already, and deliberately not skipped

Four of the nine, not two. Stated plainly, because the frontend suite is thinner than its size suggests:
three of these four are driven entirely by hand-written mock rows and exercise rendering that shipped
long ago, so they could not fail if this feature were never built. They are kept as guards, not as
evidence.

| Test | AC | Standing |
|---|---|---|
| `shows the ordering switch as unavailable on an instance without the licence` | 01.2 | Mock-driven. Asserts both clauses of the AC — the disabled switch and the tooltip naming a premium licence |
| `leaves a token nobody defined exactly where it is` | 01.11 | **Vacuous today** — nothing resolves anything. Kept because after the resolver ships it is the only thing between a typo and a silently deleted word |
| `changes nothing about the setting that was already in the list` | 01.9 | Mock-driven; the real assertion for this AC is the backend probe |
| `never lets an unlicensed administrator reach the refusal` | 02.4 | The control is disabled. Written with `userEvent`, not `fireEvent`: `fireEvent` dispatches straight at the element and a disabled control answers it, so the first draft passed against a control a real administrator can operate |
| `puts the switch back when the write is refused` | 02.4 | The page already rolls back and re-fetches on a throw. Slice 01 turns this endpoint's refusal from a 200 carrying the old value into a 403, which is what makes the rollback load-bearing rather than decorative |

## Suite state at handoff

- `dotnet build` — zero warnings.
- `dotnet test` with the connector filter from `CLAUDE.md` — **green**; 14 of the skips are this
  feature's RED scaffolds.
- `pnpm test` — green; 4 of the skips are this feature's.
- `pnpm build` — clean. `pnpm biome check ./src` — clean.

One caveat, recorded in `upstream-issues.md` as UI-4: an earlier full backend run failed once on
`TeamInProject_WithExistingForecasts_DeleteTeam_SucceedsAsync` with `no such table`. It passes alone,
the baseline without these fixtures is green, and the suite with them is green on the re-run. Two more
integration fixtures widen a known contention window; they did not introduce it.
