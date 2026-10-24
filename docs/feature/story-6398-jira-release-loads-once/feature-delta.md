<!-- markdownlint-disable MD024 -->
# Feature Delta — story-6398-jira-release-loads-once

> ADO User Story #6398 "Choosing a Jira Release for a Delivery loads once, and never shows a guess as the answer"
> (Active, no parent Epic). Feedback from a user on Jira Data Center who ties a Delivery to a Jira Release every day.
> Opening the "Add Delivery" dialog and choosing a Jira Release costs three waits of 3 to 5 s each, one after
> another, and none of them looks like loading:
>
> 1. The Jira Release tab is missing at first, and the dialog's only message says the Delivery needs a name. The
>    Portfolio's delivery sources are still loading.
> 2. The tab appears, but says a premium licence is needed. The licence is still unknown, and unknown is treated as
>    "not premium".
> 3. Only then does the Release list start loading, because it is fetched when the tab is drawn. The slowest call
>    starts last.
>
> Wanted: never show a guess as the answer; load sources, licence and Release list together so the user waits once;
> measure why listing Jira Releases takes 3 to 5 s on Data Center, and decide on caching only after measuring.

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ⊘ DISCOVER: skipped by maintainer. The evidence is the user's feedback quoted in ADO #6398.
- ⊘ DIVERGE: skipped by maintainer. The Story names the wanted outcome (one wait, no guesses, measure first).
- ✓ `docs/product/jobs.yaml`: reused `job-forecaster-bind-a-delivery-to-the-release-it-tracks` (Epic 5565) and added
  a dated reuse note to it. No new job (see JTBD).
- ✓ `docs/product/personas/delivery-forecaster.yaml`, by reference. No persona change. No journey file exists under
  `docs/product/` that covers creating a Delivery, so none is extended; the light journey lives in this file.
- ✓ Code read (read-only, 2026-10-24). Frontend, relative to `Lighthouse.Frontend/src/`:
  `pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.tsx:105-109, :283-363, :383-393, :471-592,
  :713-771, :773-780, :836-881, :895-899`, `DeliverySourceTab.tsx:51-60, :148-185, :335-415` (the failure Alert at
  `:388-395`, its text at `:391-392`), `deliverySelectionTabs.ts:171-254`, `useDeliveryManagement.ts:222-234,
  :504-508`, `DeliveryModals.tsx:90`, `hooks/useLicenseRestrictions.ts:18-54`,
  `components/Common/LicenseStatus/LicenseStatusIcon.tsx:23-32`, `App.tsx:237`, `services/Api/DeliveryService.ts:297-319`.
  Backend, relative to `Lighthouse.Backend/Lighthouse.Backend/`: `API/DeliverySourcesController.cs:29-55, :60-89,
  :182-197`, `Services/Implementation/Repositories/PortfolioRepository.cs:74-80, :114-125`,
  `Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs:2985-3062, :3074-3181,
  :3204-3265`, `.../Jira/JiraRestRoutes.cs:79-127`.
- ✓ Related feature read: `docs/feature/epic-6400-fast-with-many-features/feature-delta.md` (facts W3, W3b; DESIGN
  component decomposition, slice 02 row; DEVOPS mutation scope, slice 02 row) and `design/upstream-changes.md` (UC-1,
  DDD-11). Epic 6400 runs in parallel and owns the licence hook and `GetById` (MD-11, MD-12).
- ✓ `CLAUDE.md` (loading and failure looks, sketch before UI, terminology) and `docs/ci-learnings.md` (skimmed; to be
  pre-applied at DELIVER).

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

| Surface | Today | Gap |
|---|---|---|
| Delivery sources for the dialog's tab row | Fetched again on every dialog open (`DeliveryCreateModal.tsx:558-567`), although the Deliveries grid already fetched the same list on page load (`useDeliveryManagement.ts:226-234, :504-508`). Until it answers, `sources` is `[]`, so the row shows only Manual and Rule-Based. A failure is also turned into `[]` (`:566`) | Loading and failure both look like "this connection offers no Jira Release" |
| `GET /portfolios/{id}/delivery-sources` | Makes no call to Jira (`DeliverySourcesController.cs:29-55`, `JiraWorkTrackingConnector.cs:3023-3026`), but `portfolioRepository.GetById` loads every Portfolio with its Features, forecasts and simulation results and filters in memory (`PortfolioRepository.cs:74-80, :114-125`; Epic 6400 fact W3b) | A call that should be instant takes seconds on a large instance. Owned by Epic 6400 (MD-11) |
| Licence in the dialog | `useLicenseRestrictions()` (`DeliveryCreateModal.tsx:481`) waits for `/license`, `/teams` and `/portfolios` together (`useLicenseRestrictions.ts:28-54`). It returns `licenseStatus: null` both while loading and after a failure. The dialog reads `isPremium = licenseStatus?.canUsePremiumFeatures ?? false` (`:482`) | Unknown and failed both read as "not premium". The hook and its speed are owned by Epic 6400 slice 02 (MD-12) |
| Jira Release tab body while licence unknown | `SelectionModeContent` shows `PremiumFeatureNotice` whenever `!isPremium` (`DeliveryCreateModal.tsx:303-306`): "Taking a delivery date from a Jira Release is a premium feature…" (`deliverySelectionTabs.ts:219-222`) | A guess shown as the answer |
| Rule-Based tab while licence unknown | Disabled, with the tooltip "Please obtain a premium license…" (`DeliveryCreateModal.tsx:336-362`, `deliverySelectionTabs.ts:177-181`); the rule schema loads only when `isPremium` (`:569-592`) | Same guess, on the other premium tab |
| Message under the form | The first blocking error, e.g. "Delivery name is required" on the Manual tab (`DeliveryCreateModal.tsx:773-780, :895-899`) | What the user reads as the dialog's "tip" while the tab row is incomplete |
| Release list (`useSourceOptions`) | Plain `useEffect` + module-level `Map` cache (`DeliverySourceTab.tsx:51-60, :148-185`), started only when the tab body mounts (`:351`), cleared when the dialog closes (`DeliveryCreateModal.tsx:728`). Loading is a centred spinner (`:398-402`), failure an Alert "The list of {sourceName} entries could not be loaded. Check this {portfolioTerm}'s connection and try again." (`:388-395`) | Starts last; not TanStack Query; "try again" names no way to do it |
| `GET …/delivery-sources/{key}/options` | `LicenseGuard(RequirePremium)` + `RbacGuard(PortfolioWrite)`; also runs `GetById` (`DeliverySourcesController.cs:60-89`) | Pays the `GetById` cost again before Jira is asked (Epic 6400) |
| Jira Release sweep | One call for the project list, then one paged call per project, 8 projects at a time, 50 versions per page (`JiraWorkTrackingConnector.cs:2988-3009, :3074-3093, :3225-3265`). On Data Center the version route cannot filter by status (`JiraRestRoutes.cs:107-110`), so every archived, released and unreleased version of every visible project is paged in and filtered afterwards. The only log line is a Debug count of projects (`:3079`) | Nobody can tell how long the sweep takes, or whether its time goes to projects, pages or `GetById` |
| Backend cache of the sweep | Already exists: process-wide, 5 minutes, only for a complete sweep (`JiraWorkTrackingConnector.cs:3011-3016, :3044-3059`) | "Add a short-lived cache" is not the open question; whether the existing one is long, warm or narrow enough is |
| Edit-Delivery dialog | The same component in edit mode (`DeliveryModals.tsx:90`, `editingDelivery`). A Delivery bound to a Jira Release opens on the Manual tab with its Feature grid until the sources arrive, then switches (`DeliveryCreateModal.tsx:731-765`, `:383-393`) | The same guess, shown when editing. In scope (MD-6) |
| Other source types | Only the Jira connector offers delivery sources (`AvailableSources` exists only in `JiraWorkTrackingConnector.cs:3023`). ADO, Linear, ServiceNow and CSV offer none | Nothing else to fix today; the fix should not be Jira-specific in the frontend |

**Where the three waits come from.** Wait 1 is `GetById` reading every Portfolio, not Jira. Wait 2 is the slowest of
`/license`, `/teams` and `/portfolios`, the last two also reading every Portfolio (Epic 6400 fact W3). Wait 3 is
`GetById` again plus the Jira sweep. Epic 6400, running in parallel, makes `GetById` and the licence cheap; this Story
makes the dialog honest and stops it waiting in series, without touching anything 6400 changes, and measures the Jira
part on its own (US-03).

## Wave: DISCUSS / [REF] Persona IDs

- **delivery-forecaster** (primary). Lena Hoffmann, delivery manager on a Jira Data Center instance with about 200
  projects. Most mornings she adds a Delivery to Portfolio "Payments Platform" and points it at the next Jira
  Release, such as "PAY 2026.11" in project PAY. She has a premium licence and write rights on the Portfolio.
- **platform-operator** (secondary, US-03). Runs the instance, reads the log, gets the "picking a Release is slow"
  complaint and has to say whether Jira or Lighthouse is slow.

## Wave: DISCUSS / [REF] JTBD One-Liners

- #6398 → reused `job-forecaster-bind-a-delivery-to-the-release-it-tracks`: when the thing I forecast already exists
  as a Jira Release, I want to point a Delivery at it instead of describing it again. Its habit force says the picker
  has to be visibly the same act as typing, one step shorter; 10 to 15 s of guesses breaks that. A reuse note is
  dated in `jobs.yaml`; the wording is unchanged.

## Wave: DISCUSS / [REF] Journey (light)

Lena, on Portfolio "Payments Platform" → Deliveries → **Add Delivery** → Jira Release tab → picks "PAY 2026.11
(Payments)" → sees the preview → **Save**.

Emotional arc: *expectant* (she does this daily) → *patient, because she can see it is loading* → *confident* (the
list is there, once). Today it runs *expectant → puzzled (no tab) → alarmed (premium notice, "did our licence
lapse?") → impatient*.

### Sketch: the dialog while it loads (for the maintainer's yes at the start of DISTILL; DISTILL pins the copy)

Reuses the existing `ButtonGroup` tab row, the tab body's `CircularProgress` (`DeliverySourceTab.tsx:398-402`) and
`Alert`. The only new control is the "Try again" button of the sources failure look.

```text
State A: the tab row is not known yet (rare once the page's list is reused)
+-- Add Delivery ---------------------------------------------------+
| Delivery Name  [                                ]                 |
| Delivery Date  [ dd.mm.yyyy ]                                     |
| Selection Mode                                                    |
| [ Manual ] [ Rule-Based ] [ (o) Loading… ]   <- disabled, busy    |
|                                                                   |
| (Manual tab body as today)                                        |
+-------------------------------------------------------------------+

State B: a premium tab chosen (Jira Release or Rule-Based), licence or Release list still on its way
| [ Manual ] [ Rule-Based ] [*Jira Release*]   <- Rule-Based enabled, no tooltip
|                                                                   |
|                (o)  Getting the Jira Releases…                    |
|                     (Rule-Based body: the same spinner)           |
   no premium notice, no locked Rule-Based tab, no premium tooltip

State C: loaded (as today)
| Preview a Jira Release                                            |
| [ Type to search Jira Releases                         v ]        |

Failure looks
- Sources failed:  [ Manual ] [ Rule-Based ]
                   (!) Couldn't check what this Portfolio's connection offers. [Try again]
                   Manual and Rule-Based stay usable.
- List failed:     (!) The list of Jira Release entries could not be loaded. Close this
                   dialog and open it again to retry.          <- no button (MD-13)
- Licence failed:  no look of its own in this Story: state B stays on screen while the dialog
                   is open (known limit, follow-up F1 once Epic 6400 slice 02 has shipped)
```

DISTILL sketch point (MD-13): the two failure looks differ on purpose, one with Try again (sources) and one asking
to reopen the dialog (Release list). The maintainer confirms that difference when the sketches are walked through.

### Shared artifacts

| Artifact | Single source | Consumers | Risk |
|---|---|---|---|
| Licence status | `useLicenseRestrictions()` (unchanged here; Epic 6400 slice 02 rewrites it) | Header, Overview, the dialog's premium tabs | HIGH: the dialog must read it exactly as today, so 6400's rewrite reaches it without a merge |
| Delivery sources of a Portfolio | `GET /portfolios/{id}/delivery-sources`, fetched once by the Deliveries page | Deliveries grid (source names), the dialog's tab row | MEDIUM: grid and dialog disagreeing on which tab exists |
| Jira Release list | `GET …/delivery-sources/jira-release/options`, backend 5-minute cache | Release picker | MEDIUM: a late answer for another Portfolio landing in this dialog |

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Why |
|---|---|---|
| D1 | **Unknown is never shown as an answer.** While the sources or the licence are unknown, the dialog shows a loading look; the premium notice, the locked Rule-Based tab and its premium tooltip appear only once the licence is known not to be premium. The dialog reads `licenseStatus === null` from the unchanged hook as "not known yet". | The Story's first ask; CLAUDE.md's loading-look rule; MD-12 |
| D2 | **Failure is never shown as "no" either, where this Story can tell.** A failed sources call and a failed Release list each have their own failure look; neither falls back to "no tab" or "premium needed". A failed licence cannot be told from a loading one through today's hook, so it shows the loading look (known limit, F1). | MD-8, MD-12, MD-13 |
| D3 | **The server keeps deciding.** `LicenseGuard` and `RbacGuard` on the options and preview routes are unchanged; the frontend only stops guessing. | The UI is not the gate |
| D4 | **The dialog reuses the sources the Deliveries page already fetched** (`useDeliveryManagement`), asking again only when the page has none yet. How (props or a keyed query) is DESIGN's call, inside the Deliveries folder. | Removes wait 1 for the common case without any backend change |
| D5 | **The Release list starts while the dialog is open, as soon as the sources include the source and the licence is known premium** (MD-7, MD-15), not when the tab is drawn and not on page load. | The slowest call starts as early as it can; a page visit without the dialog costs Jira nothing; no request the server would refuse |
| D6 | **A late answer never lands in the wrong dialog.** An answer for a Portfolio or dialog no longer on screen is dropped. | CLAUDE.md: keyed queries |
| D7 | **Caching is decided from measurement** (MD-9). A 5-minute process-wide cache already exists; US-03 measures the sweep, and the gate says whether a follow-up Story changes it. | The Story's third ask |
| D8 | **The fix is not Jira-specific in the frontend.** It works for any source the server lists. | Only Jira offers a source today, but the tab row is built from the server's list |
| D9 | **Nothing Epic 6400 changes is touched** (MD-12): not `hooks/useLicenseRestrictions.ts`, no `["licenseStatus"]` query read or created, no call to `licensingService.getLicenseStatus`, not `PortfolioRepository` or the Portfolio and Team controllers. | 6400 runs in parallel; its slice 02 adds a source-tree test that only the shared query calls `getLicenseStatus` |

### Maintainer decisions — before DISCUSS

| ID | Decision |
|---|---|
| MD-1 | DISCOVER and DIVERGE skipped by the maintainer. |
| MD-2 | Feature type: user-facing. |
| MD-3 | Walking skeleton: brownfield, evaluate (answered under WS Strategy). |
| MD-4 | UX depth: lightweight (one journey, one sketch set, in this file). |
| MD-5 | JTBD: yes (reused job above). |

### Maintainer decisions — 2026-10-24, end of DISCUSS

| ID | Decision | Was |
|---|---|---|
| MD-6 | **The edit-Delivery dialog is in scope** (same component). | Q1, recommended (a) |
| MD-7 | **The Release list starts when the dialog opens**, not on page load or hover. | Q2, recommended (a) |
| MD-8 | **A failed sources call shows a notice under the tab row with Try again**; Manual and Rule-Based stay usable. | Q4, recommended (a) |
| MD-9 | **Cache gate as recommended**: if the sweep's median with a cold cache on the reporter's connection is over 2 s, a follow-up Story chooses between a longer cache lifetime and narrowing the sweep to the projects the Portfolio's query names; otherwise close with no change. | Q7, recommended (a) |
| MD-10 | **Release Notes tag: yes.** | Q9 |
| MD-11 | **`PortfolioRepository.GetById` (wait 1) belongs entirely to Epic 6400**, which runs in parallel. | New |
| MD-12 | "epic 6400 is ongoing in parallel. leave all that could interfere out of this story." This Story does not touch `hooks/useLicenseRestrictions.ts`, does not read or create a `["licenseStatus"]` query, and does not call `licensingService.getLicenseStatus`. The dialog keeps reading `licenseStatus` from `useLicenseRestrictions()` as today and treats `null` as "not known yet". Reusing the header's licence is dropped; the licence speed-up comes from 6400 slice 02. The licence failure look ("Couldn't check your licence.") is dropped and becomes follow-up F1. | Q6 (and Q3, superseded) |
| MD-13 | **No Try again button on the Release-list error.** Keep the Alert and fix only its wording so the retry path is honest: "The list of {sourceName} entries could not be loaded. Close this dialog and open it again to retry." Today's text (`DeliverySourceTab.tsx:391-392`) uses `{sourceName}` and `{portfolioTerm}`; both stay the terms in use, and DISTILL decides whether the connection hint with `{portfolioTerm}` stays in the new wording. The sources failure look keeps its Try again (MD-8), so the two looks differ; a DISTILL sketch point. | Q8, option (b) |
| MD-14 | **Rule-Based stays enabled while the licence is unknown, and its body shows a loading look.** | Q5, recommended (a) |
| MD-15 | "assume that by the time this feature is done (or certainly when released), the epic 6400 is in too". **N1 resolved as (b): the Release list waits until the licence is known premium.** By release the licence is one fast shared query, so waiting costs almost nothing and sends no refused requests. MD-12 still stands: this Story does not touch the hook; 6400 shipping changes only the timing assumption. | N1, option (b) |

**Known limit (MD-12).** Until Epic 6400 slice 02 has shipped, a licence check that fails shows the loading look for
as long as the dialog is open: the hook returns `null` for both, and this Story may not change the hook. Only the
premium tabs are affected; Manual stays usable, and the server's `LicenseGuard` still decides.

**Follow-ups recorded.**
- **F1** Once Epic 6400 slice 02 has shipped, give the dialog's premium tabs a licence failure look ("Couldn't check
  your licence.", matching 6400's DDD-11) using the hook's new loading/ready/failed status. A new Story, not part of
  #6398. Assuming 6400 slice 02 lands before 6398 is released (MD-15), F1 can follow right after; it stays out of
  this Story's slices (MD-12).
- **F2** The follow-up Story the MD-9 gate may open (cache lifetime or narrower sweep).

### Overlap check with Epic 6400 slice 02

6400 slice 02 modifies (DESIGN component decomposition, slice 02 row; DEVOPS mutation scope, slice 02 row):
`PortfolioRepository`, `IPortfolioRepository`, `PortfoliosController`, `TeamsController`, `TeamController`,
`PortfolioController`, `hooks/useLicenseRestrictions.ts`, `OverviewDashboard.tsx`; it names as unchanged `LicenseGuard`,
`useRbac`, `PortfolioDetail`, `ModifyProjectSettings`, `TeamDetail`, `EditTeam`.

This Story changes: `DeliveryCreateModal.tsx`, `DeliverySourceTab.tsx`, possibly `deliverySelectionTabs.ts`,
`DeliveryModals.tsx` and `useDeliveryManagement.ts` (slices 01, 02); `JiraWorkTrackingConnector.cs` (slice 03).

- **File overlap: none.** No file is on both lists. Slice 05 of 6400 touches `PortfolioRepository` and slice 04
  touches `PortfolioDetail`; this Story touches neither.
- **Behavioural overlap 1 (read, not edit):** the dialog calls `useLicenseRestrictions()`. When 6400 rewrites the hook
  onto shared TanStack queries, rendering the dialog in tests needs a `QueryClientProvider`. Mitigation: render the
  dialog's new Vitest scenarios through the existing `tests/renderWithQueryClient.tsx` wrapper from the start, and
  drive the licence through the mocked `licensingService` as today, so 6400's rewrite does not break them.
- **Behavioural overlap 2 (read, not edit):** `DeliverySourcesController` calls `GetById`, which 6400 slice 02 changes
  to filter in the database. Slice 03's log line is written in the connector, after `GetById`, so the two
  measurements do not mix; slice 03's reading notes which release of 6400 the instance runs.
- **Test-source rule:** 6400's source-tree test that only the shared query calls `getLicenseStatus` is not affected;
  this Story adds no such call.

## Wave: DISCUSS / [REF] Scope Assessment

PASS. 3 Stories, 2 contexts (the Deliveries frontend; the Jira connector's Release sweep). No new endpoint, no payload
change. One UI change (loading and failure looks in the add and edit dialog, the sources Try again, and the reworded
Release-list error). 3 slices: 01 about a day, 02 about ¾ day, 03 about ½ day; about 2¼ days in all.

## Wave: DISCUSS / [REF] Story Map & Slices

Backbone (delivery-forecaster, one Delivery): *open the dialog* → *see which ways to pick are offered* → *learn
whether the licence allows the Jira Release way* → *see the Releases* → *pick and preview* → *save*.

| Slice | Outcome | Brief | Est. |
|---|---|---|---|
| 01 | The dialog never shows a guess: a busy tab while sources are unknown, a loading look in both premium tabs while the licence is unknown (Rule-Based enabled), the premium notice only when the licence is known not premium, the sources failure look with Try again, the Release-list error reworded; add and edit mode | `slices/slice-01-the-dialog-never-shows-a-guess.md` | 1 day |
| 02 | No more waiting in series: the dialog reuses the sources the Deliveries page already fetched, and the Release list starts when the dialog opens; late answers dropped | `slices/slice-02-the-release-list-loads-once.md` | ¾ day |
| 03 | The operator can see why listing Jira Releases took as long as it did, and the maintainer applies the cache gate (MD-9) | `slices/slice-03-the-release-list-says-what-it-cost.md` | ½ day |

### Priority Rationale

01 first: it removes the alarming premium notice, the part of the feedback that can make a customer think their
licence lapsed, and it is right whatever the calls cost. 02 next: it removes the wait for the sources and starts the
Release list first, with no backend change. 03 last: it needs a reading on the reporter's instance and feeds a
decision. 02 does not need 01's code, but it would show today's guesses for a shorter time, so 01 goes first. 03 is
independent of both.

### Slice taste tests (re-run after MD-6..MD-15)

- **Does slice 02 still stand alone?** It shrank: the licence reuse is gone (MD-12), leaving the page's sources and the
  early Release list. It still stands alone and is kept separate from 01. It ships a different outcome (speed, not
  honesty) with its own falsifiable hypothesis. Shipped without 01, it already removes wait 1 and moves the Release
  list to the front. Merged with 01 it would be about 1¾ days, past the one-day slice cap, and one slice would test
  two hypotheses at once.
- **What 02 can promise now.** The Release list waits for the licence (MD-15, N1 (b)). That costs little only because
  Epic 6400 slice 02 is assumed in by release, making the licence one fast shared query; until then the licence wait
  and the Release list run in series. K1 is restated to say so, as an assumption rather than a dependency.
- No slice is infrastructure-only. 01 and 02 change what Lena sees. 03 adds an Information log line the operator
  reads to answer "is it Jira or Lighthouse?".
- Each slice has a hypothesis that can fail (see briefs).
- Each is checked against production-shaped data: 01 and 02 on the dev instance (`:5169`, a Jira Data Center
  connection where available) with the browser throttled to "Slow 4G"; 03 on the reporter's connection.
- Not scale variants of each other. Not cut by layer.
- None touches a file Epic 6400 changes (overlap check above).

## Wave: DISCUSS / [REF] User Stories

### US-01 — The Delivery dialog never shows a guess as the answer (slice 01, ADO #6398)

`job_id: job-forecaster-bind-a-delivery-to-the-release-it-tracks`

As a delivery-forecaster binding a Delivery to a Jira Release, I want the dialog to show that it is still loading
while it does not know yet, so I never read "no Jira Release" or "premium needed" when neither is true.

#### Elevator Pitch
Before: Lena opens **Add Delivery** on "Payments Platform". For 4 s there is no Jira Release tab and the form says
"Delivery name is required"; then the tab appears and says "Taking a delivery date from a Jira Release is a premium
feature" although her licence is premium.
After: Lena opens **Add Delivery** → sees the tab row with a busy "Loading…" tab, then the Jira Release tab with a
spinner reading "Getting the Jira Releases…", then the picker. The premium notice appears only on an instance whose
licence is known not to be premium. If the sources cannot be loaded she reads "Couldn't check what this Portfolio's
connection offers." with **Try again**; if the Release list cannot be loaded, the message tells her to close and
reopen the dialog.
Decision enabled: whether to wait for the picker or type the Delivery by hand, made from what is true rather than from
a guess.

#### Domain Examples
1. **Premium, slow sources.** Lena opens the dialog; the sources take 4 s. The row shows Manual, Rule-Based and a busy
   "Loading…" tab, then "Jira Release". No premium notice appears at any point.
2. **Community licence.** Tom Becker on a Community instance opens the dialog. Once his licence is known, the Jira
   Release tab says "Taking a delivery date from a Jira Release is a premium feature…" and Rule-Based is locked, as
   today. Before it is known, Rule-Based is enabled and either premium tab shows the loading look, not the notice.
3. **Sources fail.** The sources call answers 500. The row shows Manual and Rule-Based and, under it, "Couldn't check
   what this Portfolio's connection offers." with **Try again**; pressing it after the backend recovers brings the
   Jira Release tab.
4. **Release list fails.** Jira is down. The Jira Release tab reads "The list of Jira Release entries could not be
   loaded. Close this dialog and open it again to retry." Lena reopens the dialog a minute later and the list loads.
5. **Editing a bound Delivery.** Lena edits "PAY 2026.10", bound to that Release. The dialog opens with a loading look,
   never on Manual with a Feature grid.
6. **Licence check fails (known limit).** `/license` times out. The premium tabs keep the loading look while the
   dialog is open; Manual stays usable. No premium notice is shown.

#### UAT Scenarios (BDD)

##### Scenario: While the ways to pick are still loading, the dialog says so
Given Lena has a premium licence and "Payments Platform" offers Jira Releases
And the Portfolio's delivery sources have not answered yet
When Lena opens Add Delivery
Then the tab row shows a busy tab in place of the ones still loading
And no premium notice is shown

##### Scenario: The premium notice appears only once the licence is known not to be premium
Given the licence has not answered yet
When Lena opens the Jira Release tab, or the Rule-Based tab
Then she sees a loading look
And neither the premium notice nor a locked Rule-Based tab nor a premium tooltip is shown
When the licence answers that premium features are not available
Then the Jira Release tab shows the premium notice and Rule-Based is locked, as today

##### Scenario: A failed list of ways to pick says so and can be tried again
Given the Portfolio's delivery sources fail when Lena opens Add Delivery
Then the dialog shows "Couldn't check what this Portfolio's connection offers." with a Try again button
And Manual and Rule-Based stay usable
When the sources answer on Try again
Then the Jira Release tab appears

##### Scenario: A failed Release list says how to retry
Given the Jira Release list fails
When Lena opens the Jira Release tab
Then she reads "The list of Jira Release entries could not be loaded. Close this dialog and open it again to retry."
And when she closes and reopens the dialog, the Release list is asked for again

##### Scenario: Editing a Delivery bound to a Jira Release opens on its own tab
Given Delivery "PAY 2026.10" follows Jira Release "PAY 2026.10"
And the Portfolio's delivery sources have not answered yet
When Lena opens it for editing
Then the dialog shows a loading look and never the Manual tab's Feature list

#### Acceptance Criteria
- AC-1.1 While the delivery sources are unknown, the tab row shows a busy placeholder tab; Manual and Rule-Based are
  shown as today, and the source tabs appear when the sources answer.
- AC-1.2 While `useLicenseRestrictions()` returns no licence, Rule-Based is enabled, no premium notice, no locked
  Rule-Based tab and no premium tooltip are shown, and a premium tab chosen meanwhile shows a loading look. The rule
  schema is not requested until the licence is known premium, as today.
- AC-1.3 Once the licence is known not to be premium, today's premium notice, lock and tooltip appear unchanged.
- AC-1.4 A failed sources call shows "Couldn't check what this Portfolio's connection offers." under the tab row with
  a Try again button that repeats only that call; Manual and Rule-Based stay usable.
- AC-1.5 A failed Release list shows "The list of {sourceName} entries could not be loaded. Close this dialog and open
  it again to retry." with no button, and reopening the dialog asks for the list again (today's cache clear on close).
- AC-1.6 Editing a Delivery bound to a source never shows another tab's body while the sources load.
- AC-1.7 `hooks/useLicenseRestrictions.ts` is unchanged, and the dialog adds no licence request of its own (D9).
- AC-1.8 A refused request (`403` from the server's guards) still ends in the Release-list failure look.

#### Outcome KPIs
See K2 under Outcome KPIs.

#### Technical Notes
- Driving port: the `DeliveryCreateModal` rendered through `DeliveryModals`, with the API services mocked (Vitest +
  React Testing Library), rendered through `tests/renderWithQueryClient.tsx` so Epic 6400's hook rewrite does not
  break the scenarios. No backend change.
- The change to the licence is local to `DeliveryCreateModal`: `licenseStatus === null` reads as "not known yet"
  instead of "not premium" (`:482`, `:303-306`, `:336-362`). Known limit: a failed check looks the same (F1).
- Copy uses the configurable terms (Delivery, Portfolio). "Jira Release" is the source's display name from the server.

### US-02 — Choosing a Jira Release no longer waits in series (slice 02, ADO #6398)

`job_id: job-forecaster-bind-a-delivery-to-the-release-it-tracks`

As a delivery-forecaster, I want the dialog to use what the page already knows and to start the Release list first,
so I no longer wait for the ways to pick, then the licence, then the Releases, one after another.

#### Elevator Pitch
Before: Add Delivery → three waits of 3 to 5 s in a row (tab row, then licence, then Release list) before Lena can
type "PAY 2026.11".
After: Add Delivery → the Jira Release tab is there at once, because the Deliveries page already loaded the sources,
and the Release list request starts as soon as the licence is known premium, before Lena clicks the tab. She types
into the picker after about the Release list's own time (with Epic 6400 slice 02 assumed in by release, MD-15).
Decision enabled: binding to the Release instead of falling back to typing name and date by hand.

#### Domain Examples
1. **Daily case.** The Deliveries page of "Payments Platform" has loaded its sources. Lena opens Add Delivery: the
   Jira Release tab is there immediately, and the Release list request starts as soon as her licence is known premium,
   a moment later. She clicks the tab 1 s later and the picker is ready when the list has answered, not 12 s after
   opening.
2. **Reopening.** Lena cancels and reopens the dialog within a minute. The Release list is asked again (the dialog
   still forgets it on close, so a date just set in Jira shows), and the backend's 5-minute cache answers quickly.
3. **Community licence.** Tom Becker's licence is not premium. No Release list request is sent (MD-15); he sees the
   premium notice once his licence is known, never a list, and the server is never asked for something it would
   refuse.
4. **Lena never picks a Release.** She opens the dialog to add a Manual Delivery. One Release list request is made on
   open (MD-7); visiting the Deliveries page without opening the dialog makes none.

#### UAT Scenarios (BDD)

##### Scenario: The dialog opens with the ways to pick the page already knows
Given the Deliveries page of "Payments Platform" has loaded its delivery sources
When Lena opens Add Delivery
Then the Jira Release tab is shown without a new request for the sources

##### Scenario: The Release list starts when the dialog opens, not when the tab is drawn
Given "Payments Platform" offers Jira Releases and Lena's licence is premium
When Lena opens Add Delivery and stays on the Manual tab
Then the Jira Releases are already being fetched
And when she then opens the Jira Release tab, the picker is ready as soon as that answer has arrived

##### Scenario: Nothing is fetched that nobody asked for or the server would refuse
Given Lena visits the Deliveries page of "Payments Platform"
When she does not open Add Delivery
Then no Release list is requested
And when Tom Becker, whose licence is not premium, opens Add Delivery, no Release list is requested either

##### Scenario: A late answer for another Portfolio never lands
Given Lena opened Add Delivery on "Payments Platform" and closed it before its Release list answered
When she opens Add Delivery on "Onboarding"
Then the picker shows only the Releases answered for "Onboarding"

#### Acceptance Criteria
- AC-2.1 Opening the dialog does not request the sources again when the Deliveries page already holds them; it asks
  only if the page has no answer yet, and the sources failure look of AC-1.4 still applies to that request.
- AC-2.2 The Release list request starts while the dialog is open, as soon as the sources include the source and the
  licence is known premium, whichever tab is on screen, never later because a tab is drawn (MD-7, MD-15).
- AC-2.3 No Release list request is sent on page load, for a Portfolio that offers no source, or for a licence that is
  unknown or known not to be premium.
- AC-2.4 An answer for a Portfolio or dialog no longer on screen is dropped.
- AC-2.5 Closing the dialog still forgets the Release list, so the next open asks again (`DeliveryCreateModal.tsx:728`).
- AC-2.6 No file Epic 6400 changes is touched (D9); the licence is read through `useLicenseRestrictions()` as today.

#### Outcome KPIs
See K1, K3 and the guardrail K5 under Outcome KPIs.

#### Technical Notes
- Driving ports: the Deliveries page (`useDeliveryManagement` + `DeliveryModals`) for Vitest, and the existing
  Deliveries E2E flow, which must stay green and wait for the picker rather than for mere visibility.
- How the sources reach the dialog (props from `useDeliveryManagement` or a keyed query in the Deliveries folder) and
  where the Release list request lives once lifted out of the tab body are DESIGN's call. CLAUDE.md asks for keyed
  TanStack queries so a late answer never lands; moving `useSourceOptions` off its module-level `Map` is a likely
  consequence. No `["licenseStatus"]` query is read or created (D9).
- No backend change. The sources and options calls still pay `GetById`, and the licence still waits for `/teams` and
  `/portfolios`, until Epic 6400 slice 02 lands. This slice removes the waiting in series, not the cost of each call.

### US-03 — The Release list says what it cost, and caching is decided from it (slice 03, ADO #6398)

`job_id: job-forecaster-bind-a-delivery-to-the-release-it-tracks`

As a platform-operator who gets "picking a Release is slow", I want each Jira Release sweep to log how long it took
and how much it read, so I can tell Jira's time from Lighthouse's and the maintainer can decide on caching from a
measurement rather than a guess.

#### Elevator Pitch
Before: the log says nothing about the sweep at Information level; the operator cannot say whether 4 s went to
200 projects, to thousands of old versions, or to Lighthouse itself.
After: each sweep writes one Information line, for example `Listed 37 unreleased Jira Releases from 212 projects
(1,480 versions over 260 pages) in 3,912 ms on this Data Center connection`, and a cache hit writes nothing new. The
operator reads it in the log; the maintainer applies the MD-9 gate.
Decision enabled: whether a follow-up Story (F2) should change the existing cache or narrow the sweep, or nothing.

#### Domain Examples
1. **Data Center, cold cache.** Lena opens the dialog at 08:05. The log gains the line above: 3.9 s of a 4.6 s
   options call. The remaining 0.7 s is the request's own cost, `GetById` included.
2. **Cache hit.** Lena reopens at 08:07. No sweep line, and the options call answers in well under a second plus the
   request's own cost.
3. **One project refused.** Project HR answers 403. The line still appears and says the list is incomplete (one
   project skipped), and the incomplete list is not cached, as today.

#### UAT Scenarios (BDD)

##### Scenario: A sweep logs its duration and size
Given the Jira connection of "Payments Platform" has 212 visible projects
When the Release list is fetched with nothing cached
Then one Information line names the number of Releases offered, projects, versions read, pages, the duration and the
kind of Jira

##### Scenario: A remembered list logs no sweep
Given the Release list was fetched completely two minutes ago
When it is fetched again
Then no sweep line is written

##### Scenario: An incomplete sweep says so
Given one project refuses to list its versions
When the Release list is fetched
Then the sweep line says the list is incomplete and how many projects were skipped

#### Acceptance Criteria
- AC-3.1 Every sweep made for the picker writes exactly one Information line with Releases offered, projects,
  versions read, pages, duration in ms, the deployment kind, and whether it was complete. No URL, credential, project
  name or Release name is logged.
- AC-3.2 A cache hit writes no sweep line.
- AC-3.3 The maintainer reads the line on the reporter's instance (asked through the reporter) and on the dev
  instance, notes which Epic 6400 release the instance runs, applies the MD-9 gate and records the outcome in this
  file before the Story closes.

#### Outcome KPIs
See K4 under Outcome KPIs.

#### Technical Notes
- Driving port: `DeliverySourcesController.GetOptions` through `WebApplicationFactory`, with the Jira HTTP stubbed
  (the existing connector tests' pattern); assert through a captured logger.
- The line is written in `JiraWorkTrackingConnector.cs` only; the controller and `GetById` are not touched (D9).
- The refresh's sweep (`ResolveMany`, `JiraWorkTrackingConnector.cs:3289-3301`) shares the walk; whether it logs too
  is DESIGN's call (recommended: yes, the same line, it costs the same).
- Not part of this slice: any change to the cache or the sweep (F2).

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measure | Baseline |
|---|---|---|---|
| K1 Time from opening Add Delivery to a usable Release picker, premium, Data Center, backend cache cold | ≤ the licence call + the Release list call + 0.5 s, and ≤ 5 s on the reporter's instance. Assumption (MD-15, not a dependency): Epic 6400 slice 02 has shipped by release, so the licence call is one fast shared query | Browser DevTools network waterfall on the dev instance throttled to "Slow 4G"; the reporter's own reading after the release | 3 waits of 3–5 s in series, about 10–15 s |
| K2 Guesses shown: a missing source tab, premium notice, locked Rule-Based tab or premium tooltip while sources or licence are unknown or failed | 0 | Vitest scenarios for each state; one throttled check by hand | Shown on every slow open |
| K3 Waits in series before the picker | 1 noticeable (the sources wait removed; the licence wait, short once Epic 6400 slice 02 is in, then the Release list) | Waterfall: no sources request when the page holds them; the options request starts within 100 ms of the licence answering | 3 |
| K4 Jira sweeps whose cost is visible | 100 % of sweeps log duration and size; the MD-9 gate applied within 2 weeks of the reporter's reading | The new log line; the gate outcome in this file | 0 % (Debug project count only) |
| K5 Guardrail: Release list requests per dialog open | ≤ 1, and 0 per page visit without the dialog | Waterfall; backend sweep lines per open | 1 per Jira Release tab mount |

Usage-data event: left to DEVOPS. Expected "N/A, because opening a dialog faster is not a new action, and saving a
bound Delivery is the use, which DEVOPS can check against the existing catalog".

## Wave: DISCUSS / [REF] Definition of Done

1. ACs green: US-01 and US-02 as Vitest + React Testing Library scenarios through the dialog and the Deliveries page;
   US-03 as NUnit tests through `GetOptions`. The existing Deliveries E2E flow stays green and waits for a ready
   picker, not mere visibility.
2. `pnpm test` and `pnpm build` (Biome) clean; `dotnet build` zero warnings and the filtered `dotnet test` clean
   (slice 03 only).
3. Sonar: no new issues; `docs/ci-learnings.md` pre-applied.
4. Checked by hand on the dev instance with the browser throttled to "Slow 4G": the busy tab, both premium tabs'
   loading look, the sources failure look and the Release-list failure look each seen once; the waterfall shows no
   repeated sources request and the options request at dialog open.
5. No file Epic 6400 changes is in the diff (D9; `git diff --stat` checked against the overlap list).
6. Docs at finalization: `docs/portfolios/detail.md` (Deliveries, following a Jira Release) says the dialog shows when
   it is still loading, that Try again retries the list of ways to pick, and that a Release list that could not be
   loaded is retried by reopening the dialog; the operator-facing docs that list log lines, if any, gain the sweep
   line. Screenshots: N/A unless a Deliveries @screenshot captures the dialog mid-load (it does not today; finalize
   confirms). Demo data: N/A, because no data shape changes.
7. Copy uses the configurable terms (Delivery, Portfolio); "Jira Release" stays the source's own name.
8. ADO: Story #6398 Active (already), Resolved on push, Closed on green CI. Release Notes tag: yes (MD-10).
9. Mutation testing (Stryker for the touched frontend files, Stryker.NET for slice 03) ≥ 80 %, run last on frozen
   code.

## Wave: DISCUSS / [REF] Out of Scope

- `PortfolioRepository.GetById` (wait 1) and the cost of `GET /delivery-sources`, `/teams` and `/portfolios`: owned
  entirely by Epic 6400, which runs in parallel (MD-11).
- Anything Epic 6400 changes (MD-12): `hooks/useLicenseRestrictions.ts`, any `["licenseStatus"]` query, any call to
  `licensingService.getLicenseStatus`, reusing the header's licence. The licence speed-up comes from 6400 slice 02.
- A licence failure look in the dialog: follow-up F1, once Epic 6400 slice 02 has shipped.
- A Try again button on the Release-list error (MD-13).
- Changing the backend cache's lifetime, warming it, or narrowing the sweep: follow-up F2, if the MD-9 gate says so.
- Fetching the Release list on page load or on hover (MD-7).
- The preview's own speed after a Release is picked.
- Any other dialog's loading looks.

## Wave: DISCUSS / [REF] WS Strategy, Driving Ports, Pre-requisites

- **WS strategy**: brownfield, evaluated: no new walking skeleton. The existing Deliveries E2E flow (create a Delivery
  bound to a source, with demo data) is the thin sanity check and must wait for the picker to be ready rather than
  visible. DISTILL confirms which spec that is.
- **Driving ports**: the Deliveries page and `DeliveryCreateModal` (frontend); `DeliverySourcesController.GetOptions`
  (backend, slice 03). No HTTP contract changes.
- **Pre-requisites**: none blocking. Epic 6400 runs in parallel with no file overlap (see the overlap check). A Jira
  Data Center connection for the hand check (the demo Jira testbed is Cloud; the reporter's reading covers Data
  Center).

## Wave: DISCUSS / [REF] Standing Checklist

- **RBAC**: N/A, because no endpoint, guard or gated control changes. The options and preview routes keep
  `RbacGuard(PortfolioWrite)` and `LicenseGuard(RequirePremium)`; gating stays derived from `useRbac()` as today, and
  no component starts fetching `my-summary`.
- **Lighthouse-Clients (CLI + MCP)**: N/A, because no payload, route or meaning changes; the clients do not drive this
  dialog. The log line of slice 03 is not an API.
- **Website**: N/A, because no feature or claim the website makes changes; the Jira Release binding is advertised and
  stays as it is. Finalization confirms with a grep of the website repo for "Jira Release".
- **Usage data**: deferred to DEVOPS, as the standing rule requires. Expected "N/A" (see Outcome KPIs).
- **Terminology**: Delivery, Portfolio, Feature in all copy and docs; "Jira Release" is the server's display name of
  the source and stays literal. The reworded Release-list error keeps `{sourceName}` (MD-13).
- **UI sketch**: needed. The sketch above (states A–C, the sources and Release-list failure looks, the licence known
  limit) goes to the maintainer at the start of DISTILL, one decision at a time, before scenarios are written. Sketch
  points: the busy-tab label, the "Getting the Jira Releases…" copy, the two failure looks differing (MD-13). Reuses
  the existing tab row, `CircularProgress` and `Alert`.
- **Loading and failure looks**: this Story is about them. Loading: busy tab (sources), tab-body spinner (licence,
  Release list) in both premium tabs. Failure: sources notice with Try again; Release-list Alert asking to reopen the
  dialog; licence failure has no look of its own until F1 (known limit, MD-12). Never the previous dialog's answer:
  AC-2.4.

## Wave: DISCUSS / [REF] Open Questions for the maintainer

Q1–Q9 were answered on 2026-10-24 (MD-6 to MD-14). N1, which followed from MD-12, was answered the same day (MD-15).
No question is open.

- N1 *(answered, MD-15: option (b))* Does the Release list wait for the licence? (a) start it on dialog open without
  waiting; a Community licence's request is refused by `LicenseGuard` and dropped · **(b) wait until the licence is
  known premium**. Chosen because by release Epic 6400 slice 02 makes the licence one fast shared query, so waiting
  costs almost nothing and no refused request is ever sent.

## Wave: DISCUSS / [REF] Definition of Ready Validation (re-run after MD-6..MD-15)

| DoR item | US-01 | US-02 | US-03 |
|---|---|---|---|
| 1 Problem in domain language | PASS: missing tab, premium notice shown on a premium licence | PASS: waits in series | PASS: operator cannot tell Jira's time from Lighthouse's |
| 2 Persona with specifics | PASS: Lena, delivery manager, Jira DC, ~200 projects, premium, daily | PASS: same | PASS: platform-operator answering the complaint |
| 3 ≥ 3 domain examples, real data | PASS: 6 ("Payments Platform", "PAY 2026.10", Tom Becker on Community, the licence known limit) | PASS: 4 | PASS: 3 (212 projects, 1,480 versions, 3,912 ms) |
| 4 UAT in G/W/T, 3–7 | PASS: 5 | PASS: 4 | PASS: 3 |
| 5 AC derived from UAT | PASS: AC-1.1–1.8 | PASS: AC-2.1–2.6 | PASS: AC-3.1–3.3 |
| 6 Right-sized | PASS: 1 day | PASS: ¾ day | PASS: ½ day |
| 7 Technical notes | PASS: `null` read as unknown, local to the dialog; test wrapper for 6400's rewrite | PASS: sharing is DESIGN's; `GetById` and licence cost stay (6400) | PASS: connector only; refresh sweep |
| 8 Dependencies tracked | PASS: none blocking; no file overlap with 6400; F1 recorded | PASS: ordered after 01; N1 answered (MD-15); Epic 6400 slice 02 at release is an assumption for K1, not a dependency | PASS: reporter's reading for AC-3.3 |
| 9 Outcome KPIs, measurable | PASS: K2 = 0 | PASS: K1, K3, K5 | PASS: K4 |

DoR status: PASSED for all three. No open question (N1 answered by MD-15).

## Wave: DISCUSS / [REF] Wave Decisions Summary

- Prior waves: DISCOVER and DIVERGE ⊘ skipped by maintainer (MD-1).
- Job reused: `job-forecaster-bind-a-delivery-to-the-release-it-tracks`, dated note added.
- Scope: PASS, 3 Stories / 3 slices, about 2¼ days; edit dialog included (MD-6).
- Key finding: waits 1 and 2 are not Jira but `GetById` and the licence hook's `/teams` + `/portfolios` (Epic 6400's
  W3/W3b), both left to Epic 6400 (MD-11, MD-12). Wait 3 is `GetById` plus a Data Center sweep that pages every
  version of every project. A 5-minute backend cache already exists.
- Maintainer decisions: MD-6 to MD-15 (2026-10-24). Known limit: a failed licence check shows the loading look while
  the dialog is open, until F1. Follow-ups: F1 (licence failure look, right after 6400 slice 02), F2 (cache, if the
  gate says so).
- Assumption (MD-15, not a dependency): Epic 6400 slice 02 is in by the time this Story is released, so the licence
  is one fast shared query; the Release list waits for it (N1 answered (b)).
- Overlap with Epic 6400 slice 02: no file overlap; two read-only behavioural overlaps, both mitigated.
- Locked: D1–D9. Open: none.
- Handoff to DESIGN: how the page's sources reach the dialog and where the Release list request lives (keyed queries,
  inside the Deliveries folder), the dialog's reading of `null` as unknown, where the sweep line is written and
  whether the refresh's sweep logs too.
