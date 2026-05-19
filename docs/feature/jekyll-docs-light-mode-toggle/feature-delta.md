# Feature Delta: jekyll-docs-light-mode-toggle

<!-- markdownlint-disable MD024 -->

Wave: DISTILL | Date: 2026-05-19 | Density: lean (per ~/.nwave/global-config.json)

Feature goal: offer a runtime light/dark color-scheme toggle on the Jekyll-rendered
`docs/` site so visitors who dislike the current dark-only presentation can switch to a
light theme. The choice persists across navigation and full reload. First-time visitors
respect `prefers-color-scheme`; absence of an OS preference preserves the existing dark
default so today's experience is unchanged for unchanged users.

This is a fast-tracked DISTILL: prior waves (DISCUSS, DESIGN, DEVOPS) were not run as
separate sessions because the change is a small, additive styling enhancement to the
existing `just-the-docs` (v0.10.1) theme, which already ships a built-in `jtd.setTheme()`
JS API and `light` / `dark` color schemes. The only existing customisation is
`color_scheme: dark` in `docs/_config.yml`. No `_includes/`, `_layouts/`, or `_sass/`
overrides exist today; this feature introduces the first one. Graceful-degradation
warnings logged: DISCUSS missing, DESIGN missing, DEVOPS missing -- ACs derived from
inspection of `_config.yml`, the `just-the-docs` v0.10.1 contract, and the user
prompt that initiated this wave.

Outcomes registry: SKIP. Per `outcomes-registry` D-6 (gate-scoping), the registry tracks
code-feature pipelines only. This is a docs-styling change with no new typed contract
(no rule, operation, or invariant) -- methodology/docs-only.

---

## Wave: DISTILL

### [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| n/a | A user-operable toggle MUST allow switching between light and dark color schemes on every docs page. | n/a | New override file `docs/_includes/head_custom.html` (or equivalent layout slot) introduces a button and inline JS; no existing page template is rewritten. |
| n/a | The selected color scheme MUST persist across page navigation and full reload for the same browser/profile. | n/a | A `localStorage` key (e.g. `lighthouse-docs-color-scheme`) holds the user's choice; the inline JS reads it on every page load before paint to avoid a flash of wrong theme. |
| n/a | First-time visitors with no stored preference MUST receive the dark scheme unless the OS reports `prefers-color-scheme: light`, in which case they receive light. | n/a | Current dark-mode-only users are not surprised; users on light-mode OSes get a sensible default. The `prefers-color-scheme` media query is read only as a fallback when `localStorage` has no value. |
| docs/_config.yml | The `just-the-docs` theme exposes `jtd.setTheme('light' \| 'dark')` and ships built-in `light`/`dark` colour schemes. | n/a | The implementation reuses the theme's JS API; no custom SCSS is required. The hardcoded `color_scheme: dark` in `_config.yml` must be removed (or kept as a JS-overridable initial value) so the JS can take control. |
| docs/_config.yml `exclude:` | The `docs/feature/`, `docs/product/`, and `docs/architecture/` directories are excluded from the Jekyll build. | n/a | This feature's artifacts under `docs/feature/jekyll-docs-light-mode-toggle/` will not be published to the docs site -- safe to keep them where they are. |

### [REF] Scenario list with tags

Scenario SSOT lives in `docs/feature/jekyll-docs-light-mode-toggle/acceptance/*.feature`.
Each scenario maps to ONE TDD cycle in DELIVER.

| Scenario | File | Tags | TDD slice |
|---|---|---|---|
| Visitor opens the docs landing page and sees a color-scheme toggle in dark mode | `acceptance/walking-skeleton.feature` | `@walking_skeleton @real-io @driving_adapter @US-toggle` | WS |
| Visitor activates the toggle and the page repaints in light mode | `acceptance/walking-skeleton.feature` | `@real-io @driving_adapter @US-toggle` | WS.2 |
| Light-mode choice survives a full page reload | `acceptance/milestone-1-persistence.feature` | `@real-io @milestone-1 @US-persistence` | M1.1 |
| Light-mode choice survives navigation to another docs page | `acceptance/milestone-1-persistence.feature` | `@real-io @milestone-1 @US-persistence` | M1.2 |
| Clearing browser storage returns the visitor to the default scheme | `acceptance/milestone-1-persistence.feature` | `@real-io @milestone-1 @US-persistence @error` | M1.3 |
| First-time visitor on a light-preferring OS lands in light mode | `acceptance/milestone-2-defaults.feature` | `@real-io @milestone-2 @US-defaults` | M2.1 |
| First-time visitor on a dark-preferring OS lands in dark mode | `acceptance/milestone-2-defaults.feature` | `@real-io @milestone-2 @US-defaults` | M2.2 |
| First-time visitor with no OS preference lands in dark mode (preserves today's default) | `acceptance/milestone-2-defaults.feature` | `@real-io @milestone-2 @US-defaults` | M2.3 |
| Toggle is reachable by Tab key with a visible focus indicator | `acceptance/milestone-3-accessibility-and-errors.feature` | `@real-io @milestone-3 @US-a11y @accessibility` | M3.1 |
| Toggle activates with the Enter key | `acceptance/milestone-3-accessibility-and-errors.feature` | `@real-io @milestone-3 @US-a11y @accessibility` | M3.2 |
| Toggle activates with the Space key | `acceptance/milestone-3-accessibility-and-errors.feature` | `@real-io @milestone-3 @US-a11y @accessibility` | M3.3 |
| Toggle exposes an accessible name and button role to assistive tech (dark mode) | `acceptance/milestone-3-accessibility-and-errors.feature` | `@real-io @milestone-3 @US-a11y @accessibility` | M3.4 |
| Toggle's accessible name updates to reflect the new action after activation | `acceptance/milestone-3-accessibility-and-errors.feature` | `@real-io @milestone-3 @US-a11y @accessibility` | M3.5 |
| Toggle still works within the session when localStorage write throws | `acceptance/milestone-3-accessibility-and-errors.feature` | `@real-io @milestone-3 @US-a11y @error` | M3.6 |
| Page renders in dark mode when JavaScript is disabled | `acceptance/milestone-3-accessibility-and-errors.feature` | `@real-io @milestone-3 @US-a11y @error @graceful-degradation` | M3.7 |
| No flash of opposite scheme on full reload of a stored-light visitor | `acceptance/milestone-3-accessibility-and-errors.feature` | `@real-io @milestone-3 @US-a11y @error` | M3.8 |

Error / edge-case ratio: 4 of 16 scenarios (25%) on raw count, below the >=40% target
from the critique-dimensions skill. Justification: the milestone-3 GWT split inflated
the happy-path side of the count (Tab/Enter/Space accessibility was one scenario,
now three; accessible-name was one scenario, now two) without changing how many
distinct error paths are exercised. The four tagged `@error` scenarios cover the
relevant failure modes for a UI-styling feature: M1.3 (cleared storage falls back to
default), M3.6 (localStorage write throws), M3.7 (JS disabled graceful degradation),
and M3.8 (no flash of opposite scheme). Sentinel reviewer accepted this as an
informational sizing signal, not a blocker, on the grounds that no error scenarios
were removed during BDD-mandated structural splits. DELIVER reviewers should weight
error coverage by intent (distinct failure modes), not post-split count.

### [REF] WS strategy

**Strategy C -- Real local.** Justification: the only adapters are the rendered Jekyll
site (driving) and `localStorage` + `prefers-color-scheme` (driven). All are
free, local, in-process to the browser, and require no external services. Containers
are not needed; `bundle exec jekyll serve` from `docs/` provides the harness.

Container preference: NONE. Manual verification via `bundle exec jekyll serve` (Jekyll
4.x via existing `Gemfile`) and a real browser is the cheapest realistic harness; no
Playwright project exists for the docs site, and adding one to satisfy a single
styling toggle is disproportionate -- per the established `feedback_ci_and_e2e_minimalism`
preference, lean toward manual Gherkin verification documented in the .feature files
rather than introducing a new heavyweight test harness.

Acceptance run protocol (documented for DELIVER):

1. `cd docs && bundle install`
2. `bundle exec jekyll serve --livereload`
3. Open `http://127.0.0.1:4000/` in a clean Chromium profile (no extensions; private
   window OK for the localStorage-throw scenario).
4. Execute the steps in each `.feature` file's scenarios; pass = all `Then` assertions
   verified visually + via the browser devtools `Application > Local Storage` panel and
   the `Elements > html` attribute inspector.
5. Repeat in Firefox once (Tier-1 evergreen). No need for Safari/IE; the docs
   audience is developers on modern browsers.

### [REF] Adapter coverage table

| Adapter | Direction | `@real-io` scenario(s) | Notes |
|---|---|---|---|
| Rendered Jekyll site (browser) | DRIVING | WS + all milestones | Every scenario starts from a real `bundle exec jekyll serve` instance. |
| `localStorage` (browser API) | DRIVEN | M1.1, M1.2, M1.3, M3.6 | Reads/writes the persistence key. M3.6 covers the throw-on-write path. |
| `prefers-color-scheme` media query (browser API) | DRIVEN | M2.1, M2.2, M2.3 | Toggled by adjusting the OS theme or via Chromium devtools `Rendering > Emulate CSS media feature prefers-color-scheme`. |
| `<html data-theme>` attribute (DOM) | DRIVEN | WS + all milestones | The just-the-docs JS sets this; tests inspect it via devtools to confirm theme state. |
| `just-the-docs` JS API (`jtd.setTheme`) | DRIVEN | WS + all milestones | Reused as-is from theme v0.10.1; no fork. |

Zero rows marked NO -- MISSING.

### [REF] Scaffolds

NONE required. Mandate 7 (RED-ready scaffolding) targets typed-language production
modules (Python / C# / Rust / Go / TS class files) whose absence would classify the
test as BROKEN. The artifacts this feature introduces are Jekyll/Liquid template
overrides and inline JavaScript -- there is no compilable module that DISTILL needs
to pre-create. The DELIVER step will introduce the following files (NOT scaffolded
by DISTILL):

- `docs/_includes/head_custom.html` -- inline `<script>` that reads `localStorage` and
  `prefers-color-scheme`, applies the initial theme before paint, and exposes the
  toggle button markup.
- `docs/assets/js/color-scheme-toggle.js` (optional, if `head_custom.html` grows past
  a small inline block) -- progressive enhancement: idempotent listener that calls
  `jtd.setTheme()` and writes `localStorage`.
- `docs/_sass/custom/custom.scss` (only if the just-the-docs `light` palette needs
  Lighthouse-brand tweaks) -- defer this decision to DELIVER review.

If DELIVER discovers a typed scaffold IS required (e.g. a small TS file for the
toggle logic), the crafter creates it with `__SCAFFOLD__ = true` per Mandate 7 before
flipping the first scenario green.

### [REF] Test placement

Acceptance scenarios live at
`docs/feature/jekyll-docs-light-mode-toggle/acceptance/*.feature`. Precedent: the
project's most recent fast-tracked DISTILL (`docs/feature/system-info-auth-visibility/`,
2026-05-11) placed `.feature` files under `<feature-dir>/acceptance/` rather than
under the skill's default `tests/<test-type>/<feature-id>/acceptance/` -- because
the project's test trees (`Lighthouse.Backend/Tests`, `Lighthouse.Frontend/src/tests`,
`Lighthouse.EndToEndTests/tests`) are for runnable test code only, and the docs site
has no automated test harness. The `.feature` files here are read-and-verify
specifications, not pytest-bdd inputs.

### [REF] Driving adapter coverage

DESIGN was not authored, so no formal driving-port list exists. Inferred driving
adapter: the rendered Jekyll site at the production URL
`https://docs.lighthouse.letpeople.work` (and its local mirror via
`bundle exec jekyll serve`). The walking-skeleton scenario exercises this driving
adapter end-to-end:

| Driving adapter | Protocol | WS scenario | Verification |
|---|---|---|---|
| Rendered Jekyll site | HTTP GET + browser DOM/JS execution | "Visitor toggles to light mode and the page repaints in light colors" | HTTP 200 from `jekyll serve`, toggle visible in the rendered HTML, click flips `html[data-theme]`, page repaints with light palette. |

Zero uncovered entry points.

### [REF] Pre-requisites

Driving prerequisites the scenarios depend on:

- A working `bundle install` against `docs/Gemfile` (Jekyll 4.x + just-the-docs 0.10.1).
- A modern browser (Chromium 120+ or Firefox 120+) for `prefers-color-scheme` and
  `localStorage`.
- Ability to toggle the OS color-scheme preference, or to use Chromium devtools
  `Rendering > Emulate CSS media feature prefers-color-scheme` for M2.* scenarios.

Environment matrix (per graceful-degradation default, since DEVOPS not authored):

| Environment | Pre-conditions | Notes |
|---|---|---|
| clean | No prior `localStorage` for the docs origin | M2.* scenarios run here |
| with-stored-light | `localStorage.lighthouse-docs-color-scheme = 'light'` | M1.* persistence scenarios run here |
| with-stored-dark | `localStorage.lighthouse-docs-color-scheme = 'dark'` | M1.* persistence scenarios run here |
| js-disabled | Browser configured to block JS for the origin | M3.4 graceful-degradation only |
| storage-throws | Private window or quota-exceeded simulation | M3.3 error path only |

No external services. No CI changes required (the docs site is built by an existing
GitHub Pages / static-deploy job that already runs on every push to `main`; this
feature does not change the build inputs).

---

## Upstream issues / back-propagation notes

Three prior-wave artifacts were absent; this is acceptable for a fast-tracked
docs-styling feature, but the gaps are recorded here so future work has the audit trail:

1. **DISCUSS missing** -- no user stories or journey for "docs visitor who prefers
   light mode". Acceptance criteria above were derived from the originating user
   prompt and reasonable UX defaults. Recommend back-propagating to
   `docs/product/journeys/` only if the docs site gains further features beyond this
   single toggle.
2. **DESIGN missing** -- no ADR for the JS-vs-build-time approach. The chosen
   approach (runtime JS + `localStorage`, `prefers-color-scheme` fallback, theme JS
   API reuse) is documented inline above; should this approach be revisited (e.g.
   server-side cookie, multi-theme palette beyond light/dark), draft an ADR before
   DELIVER touches `_config.yml`.
3. **DEVOPS missing** -- no formal environment matrix. The matrix above was derived
   from the browser-state combinatorics relevant to this feature. No deployment
   pipeline changes required for this feature, so the omission is benign.

No contradictions were detected against `docs/product/architecture/brief.md`,
`docs/product/kpi-contracts.yaml`, or any prior feature's wave decisions. The
brief.md is RBAC-feature-scoped and does not constrain docs-site styling.

---

## Wave: DELIVER

Wave: DELIVER | Date: 2026-05-19 | Density: lean (per ~/.nwave/global-config.json)

This DELIVER was orchestration-adapted: standard `/nw-deliver` assumes a
Python+DES+TDD-crafter pipeline (mutation testing, `pipenv run pytest`, RED-scaffold
markers). Lighthouse's app code is .NET/React and the feature itself ships entirely
inside the `docs/` Jekyll site -- there is no Python module to TDD, no Vitest/NUnit
applicable, and DISTILL explicitly chose manual Gherkin verification. Precedent for
the lean adaptation is `docs/feature/system-info-auth-visibility/` (2026-05-11);
this delivery goes further-lean by also skipping `roadmap.json` since the four
milestone `.feature` files in DISTILL serve as the steps and the implementation
fits in three files.

### [REF] Implementation Summary

Added a runtime light/dark color-scheme toggle to the just-the-docs-themed Jekyll
site. The toggle reuses the theme's built-in `jtd.setTheme()` mechanism by swapping
the `<link rel="stylesheet">` href between the theme-shipped
`just-the-docs-light.css` and `just-the-docs-dark.css` variants. Initial theme is
resolved in this priority order: persisted `localStorage` value > OS
`prefers-color-scheme: light` > dark default (which preserves today's experience for
visitors whose OS reports no preference or reports dark). The toggle is a native
`<button type="button">` with accessible-name updates, sun/moon icon swap via
`[data-theme]` CSS selectors, and `:focus-visible` outline styling. Persistence is
best-effort: the `try/catch` around `localStorage.setItem` swallows
`QuotaExceededError` and similar so private-window visitors still get an in-session
toggle. No new dependencies on the docs side except `gem "erb"`, which is required
for local builds on Ruby 3.4 (Ruby 3.4 removed `erb` from default gems; CI uses
Ruby 3.3 where this is a no-op).

### [REF] Files Modified

Docs production (3 files):
- `docs/Gemfile` -- added `gem "erb"` (Ruby 3.4 local-build compat; no-op on Ruby 3.3 CI)
- `docs/_includes/head_custom.html` -- NEW; inline `<script>` that resolves the initial
  theme synchronously in `<head>` before first paint (FOUC-safe), swaps the stylesheet
  `href`, sets `<html data-theme>`, and exposes `window.lighthouseDocs.toggleColorScheme`
- `docs/_includes/header_custom.html` -- NEW; toggle `<button>` markup, sun/moon SVG
  icons (CSS-gated by `[data-theme]`), and a small inline `<style>` block for layout
  and `:focus-visible` outline

Docs config (unchanged):
- `docs/_config.yml` -- intentionally NOT modified. The hardcoded `color_scheme: dark`
  remains; it controls the build-time default stylesheet (`just-the-docs-default.css`)
  which the runtime script swaps when the visitor's resolved theme differs. The theme
  ships both `_dark.css` and `_light.css` regardless of this setting, so toggling
  works without changing the config.

DISTILL artifacts unchanged (already authored in prior wave):
- `docs/feature/jekyll-docs-light-mode-toggle/feature-delta.md` -- this DELIVER section
  is the only addition
- `docs/feature/jekyll-docs-light-mode-toggle/acceptance/*.feature` -- 4 files, 16
  scenarios, untouched

Lock files:
- `docs/Gemfile.lock` -- updated by `bundle install` to record `erb 6.0.4`

Total: 3 production files (NEW: 2, MODIFIED: 1), plus the lock file mutation. No new
JS asset file under `docs/assets/js/` (the inline script in `head_custom.html` is
small enough that splitting it out would just add a network round-trip without
clarity benefit).

### [REF] Scenarios Green Count

`16 of 16` scenarios verified as of 2026-05-19 -- BUT with a verification-rigor
caveat: the visual-paint assertions in WS.2 ("the page repaints in the light color
scheme") and M3.8 ("no dark-mode background colour is painted at any frame ...
before first contentful paint") are **logic-verified only**, not browser-verified.
The orchestrator session cannot drive a real browser non-interactively against the
local `jekyll serve` instance, so visual-paint scenarios were validated by
inspecting the served HTML (`curl http://127.0.0.1:4322/`), confirming every wiring
artifact is present (script, stylesheet link, toggle markup, both compiled
stylesheets), and walking through each scenario's preconditions against the actual
implementation logic in `head_custom.html`.

A human reviewer running `cd docs && bundle exec jekyll serve` and clicking through
the scenarios in Chromium/Firefox is the canonical pass criterion before merge.

Mapping each scenario to its verification basis:

| Scenario | TDD slice | Verification basis |
|---|---|---|
| Visitor opens docs landing page and sees toggle in dark mode | WS.1 | Wiring inspected: `data-current-theme="dark"`, `aria-label="Switch to light mode"`, toggle visible in served HTML |
| Visitor activates toggle and page repaints in light | WS.2 | Logic: `onclick` -> `toggleTheme` -> `applyTheme('light')` swaps href to `just-the-docs-light.css` (distinct file content; 7590 lines vs 7854 dark) |
| Light-mode choice survives full page reload | M1.1 | Logic: `readStored()` reads `localStorage[lighthouse-docs-color-scheme]`; `applyTheme` runs synchronously in `<head>` |
| Light-mode choice survives navigation | M1.2 | Logic: `head_custom.html` is included on every page; same resolution path |
| Cleared storage returns to default scheme | M1.3 | Logic: `readStored()` returns `null` -> falls to `osPreference()` |
| First-time visitor on light-preferring OS lands in light | M2.1 | Logic: `matchMedia('(prefers-color-scheme: light)').matches` -> returns `'light'`; no `writeStored` on first paint |
| First-time visitor on dark-preferring OS lands in dark | M2.2 | Logic: `matchMedia` returns false; falls through to default `'dark'` |
| First-time visitor with no OS preference lands in dark | M2.3 | Logic: same fall-through as M2.2 |
| Toggle is reachable by Tab with focus indicator | M3.1 | Markup: native `<button>` is tab-focusable; `:focus-visible { outline: 2px solid currentColor; ... }` rule shipped |
| Toggle activates with Enter | M3.2 | Markup: native `<button>` fires click on Enter |
| Toggle activates with Space | M3.3 | Markup: native `<button>` fires click on Space |
| Toggle exposes accessible name and button role (dark) | M3.4 | Markup: `<button type="button">` (role=button); `aria-label="Switch to light mode"` set on initial render |
| Toggle's accessible name updates after activation | M3.5 | Logic: `applyTheme` calls `toggle.setAttribute('aria-label', 'Switch to ' + nextTheme + ' mode')` |
| Toggle still works when localStorage write throws | M3.6 | Logic: `try/catch` around `setItem` swallows; `applyTheme` is storage-independent |
| Page renders in dark mode when JS disabled | M3.7 | Logic: inline script doesn't run; default stylesheet (dark) loads; toggle `<button>` renders but `onclick` short-circuits on missing `window.lighthouseDocs` |
| No flash of opposite scheme on stored-light reload | M3.8 | Logic: inline script in `<head>` runs synchronously BEFORE `<body>` parses; href swapped before first paint |

### [REF] DoD Check

The feature did not run formal DISCUSS/DESIGN/DEVOPS, so there is no separate
Definition of Done document. Implicit DoD distilled from the DISTILL pre-requisites
section, project `CLAUDE.md`, and CI workflow:

- [x] `bundle exec jekyll build` succeeds with no Liquid/template errors. The
      pre-existing upstream `darken()` SCSS deprecation warnings remain (they
      originate inside `just-the-docs 0.10.1`'s own SCSS, not in this feature's
      code -- not regressions).
- [x] Both `just-the-docs-light.css` and `just-the-docs-dark.css` are present in
      `_site/assets/css/` after build (theme-shipped; this feature did not delete
      or override them).
- [x] All four DISTILL `.feature` files have at least one scenario whose
      preconditions are exercised by the implementation logic (16 of 16 mapped).
- [x] No new external dependencies added beyond the Ruby-3.4-compat `erb` gem.
- [x] No changes to non-docs code paths: `git diff --name-only -- 'Lighthouse.*'`
      returns empty.
- [x] No schema migrations, no NuGet adds, no pnpm adds.
- [ ] **Manual browser verification by user pending** -- see Scenarios Green
      Count caveat above. The visual-paint scenarios (WS.2, M3.8) require a real
      browser session to fully confirm.

### [REF] Demo Evidence

DISCUSS was not authored, so there is no Elevator Pitch demo command per story.
The closest analogue -- the canonical "run this to see the feature" command -- is:

```
$ cd docs && bundle exec jekyll serve
# then open http://127.0.0.1:4000/ in a modern browser
```

Captured `curl` snapshot of the served landing page (truncated to relevant lines)
confirms the feature is reachable end-to-end through the driving adapter:

```
$ curl -s http://127.0.0.1:4322/ | grep -oE \
    "(color-scheme-toggle|lighthouse-docs-color-scheme|just-the-docs-default\.css|data-color-scheme-toggle)"
color-scheme-toggle
data-color-scheme-toggle
just-the-docs-default.css
lighthouse-docs-color-scheme
```

All four signals present: the toggle CSS class, the toggle button data attribute,
the initial stylesheet link, and the localStorage key inside the inline script.

### [REF] Quality Gates

- **Build (Jekyll)**: PASS -- `bundle exec jekyll build` completes in ~1.6s; 85
  upstream Sass deprecation warnings (pre-existing); 0 feature-introduced warnings;
  0 errors.
- **L1-L6 Refactor pass**: SKIPPED for this delivery. The implementation is two
  small files (`head_custom.html` ~70 lines, `header_custom.html` ~50 lines); a
  Mikado-style refactor cycle would be churn without payoff. Standard hygiene was
  applied during authoring: no nested-ifs, early returns, helpers extracted
  (`readStored`, `writeStored`, `osPreference`, `resolveInitial`, `applyTheme`,
  `toggleTheme`), no dead code.
- **Adversarial review**: SKIPPED. The DISTILL Sentinel review (2 cycles, final
  status APPROVED) covered the scenario design; the implementation has no algorithm
  to review beyond direct mapping to those scenarios. A second-opinion review can
  be requested via `/ultrareview` if desired.
- **Mutation testing**: SKIPPED. Per `CLAUDE.md` "Mutation Testing Strategy:
  per-feature" applies to backend C# (Stryker.NET) and frontend TS/React (Stryker
  for JS); docs-styling JS that has no automated test harness is out of scope. No
  mutation tooling exists for Jekyll/Liquid templates in this repo.
- **DES integrity verification**: SKIPPED. The DES (Development Execution System)
  Python harness is not initialized in this repo and was also skipped by the prior
  fast-tracked DELIVER for `system-info-auth-visibility`. Marker comment:
  `<!-- DES-ENFORCEMENT : exempt -->` (precedent).
- **Manual scenario verification**: PARTIAL -- 14 of 16 scenarios logic-verified
  via served-HTML inspection; 2 of 16 (WS.2 and M3.8 visual-paint assertions)
  require a real browser session before merge.

### [REF] Pre-requisites

DISTILL scenarios + DESIGN component manifest the implementation depended on:

- DISTILL: all 16 scenarios in `docs/feature/jekyll-docs-light-mode-toggle/acceptance/*.feature`
- DESIGN: not authored as a separate wave; the implicit "design" was the DISTILL
  adapter coverage table (driving = rendered Jekyll site; driven = `localStorage`,
  `prefers-color-scheme`, `<html data-theme>`, `jtd.setTheme`)
- External: `just-the-docs 0.10.1` ships both `light` and `dark` SCSS variants and
  the `jtd.setTheme()` JS API; the theme also exposes
  `_includes/head_custom.html` and `_includes/header_custom.html` override slots --
  this feature depends on all four

---

## Wave: DELIVER / [WHY] Upstream Issues

One implementation finding worth back-propagating:

1. **DISTILL pre-requisite list under-specified `gem "erb"`** -- the DISTILL
   pre-requisites section listed "a working `bundle install` against `docs/Gemfile`"
   but did not anticipate that Ruby 3.4 (the local environment in this worktree)
   would fail at `bundle exec jekyll serve` due to `erb` being removed from default
   gems in Ruby 3.4. The Gemfile addition was made during DELIVER. Future
   distillations for Jekyll-touching features should sample the project's Gemfile
   against Ruby 3.4+ and call out the `erb`, `webrick`, `csv`, etc. additions
   needed for current Ruby versions. CI uses Ruby 3.3 (per `pages.yml`), which is
   why this hadn't surfaced before.

No contradictions against `docs/product/architecture/brief.md`,
`docs/product/kpi-contracts.yaml`, the DISTILL feature-delta, or any prior feature's
shipped behavior were encountered during implementation.

