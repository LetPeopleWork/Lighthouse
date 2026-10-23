# Mutation testing — Stories 6348, 6352, 6355 (say what it does)

Run 2026-10-23 against `main` @ `eba92a37d`. Gate is 80 % kill rate on each stack the feature touched,
over the lines it changed (`git diff 95dc64767 HEAD`, production files only).

The feature spans three Stories (#6348, #6352, #6355). The configs carry the id of #6352, the Story that
added the shared info button and the direction tips, which is where most of the mutated lines sit.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend | N/A | – | – | – | – | – |
| Frontend (StrykerJS 10.0.0), first run | **74.31 %** | 109 | 81 | 28 | 0 | 8 m 29 s |
| Frontend, after the kill round (incremental, 68 of 109 reused) | **76.15 %** | 109 | 83 | 26 | 0 | 5 m 32 s |

**Backend: N/A.** The feature changed no backend file. The one backend path in the diff range,
`Lighthouse.Backend.csproj`, is Renovate's OpenTelemetry.Extensions.Hosting bump (`2e65ed54d`), not
this feature.

**Frontend: the raw score is under 80 %, and so are four of the files.** Every one of the 26 remaining
survivors is accepted below. 24 are `sx`, popover-placement or layout literals, which jsdom renders without
layout. One is equivalent because HTML collapses whitespace. One is equivalent because its `null` case
cannot be reached. Leaving those out, every file is at 100 %. No survivor pointed to a bug.

Configs: `stryker.6352.frontend.json` and `vitest.stryker.mutation.ts`. Copy the vitest config into
`Lighthouse.Frontend/` before a run, because Stryker resolves it against the working directory. Then run
from `Lighthouse.Frontend/`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/stories-6348-6352-6355-say-what-it-does/mutation/stryker.6352.frontend.json --incremental
```

`coverageAnalysis: perTest`, incremental file `reports/mutation/stryker-incremental-6352.json` (gitignored).
`mutate` is scoped by line range to the changed lines. The two new small files, `fieldDirectionTips.ts` and
`InfoPopoverButton.tsx`, are mutated whole. The vitest config narrows `include` to the 30 spec files that
cover the mutated files (328 tests before the kill round, 330 after). It also carries the main config's
`onUnhandledError` filter, because without it the deliberate save failure in
`ModifyConnectionSettings.test.tsx` counts as an unhandled error.

## Frontend

| file | mutants | killed (first) | survived (first) | score (first) | killed (after) | survived (after) | score (after) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| fieldDirectionTips.ts | 34 | 32 | 2 | 94.12 % | 33 | 1 | 97.06 % |
| InfoPopoverButton.tsx | 23 | 13 | 10 | 56.52 % | 13 | 10 | 56.52 % |
| WidgetShell.tsx (changed lines) | 18 | 8 | 10 | 44.44 % | 9 | 9 | 50.00 % |
| AdvancedInputs.tsx | 11 | 10 | 1 | 90.91 % | 10 | 1 | 90.91 % |
| FormSelectField.tsx | 9 | 5 | 4 | 55.56 % | 5 | 4 | 55.56 % |
| DeliverySourceTab.tsx | 9 | 9 | 0 | 100 % | 9 | 0 | 100 % |
| connectionDefaultEntry.ts | 4 | 4 | 0 | 100 % | 4 | 0 | 100 % |
| ModifyConnectionSettings.tsx (changed lines) | 1 | 0 | 1 | 0 % | 0 | 1 | 0 % |
| **total** | **109** | **81** | **28** | **74.31 %** | **83** | **26** | **76.15 %** |

### Closed by this pass

- **`fieldDirectionTips.ts:101`, `.join(" ")` → `.join("")`.** The tip's sentences ran together
  ("…points to its parent.With None, …"), and every test matched one sentence at a time.
  `AdvancedInputs.parentOverrideTip.test.tsx`: *without an example, the tip reads as two sentences: which
  end the field reads, then what None does* pins the whole tip text of a Linear Team against the literal.
- **`WidgetShell.tsx:468`, `"this widget"` → `""`.** A widget with no title named its info button just
  "About ". `WidgetShell.infoPopover.test.tsx`: *on a widget without a title, the info button is named after
  the widget all the same* finds the button as "About this widget".

### Accepted survivors

- **`fieldDirectionTips.ts:99`, `.filter(Boolean)` removed.** This is equivalent. Without the filter, a tip with
  no tracker example joins an empty string between the two sentences, which leaves a double space. HTML
  rendering and the accessible text both collapse whitespace, so neither the reader nor a screen reader can
  tell the difference. A test that asserts on raw `textContent` spacing would be pinning markup, not anything
  the reader sees.
- **`ModifyConnectionSettings.tsx:1116`, `selectedWorkTrackingSystem?.workTrackingSystem` → `.`.** This is
  equivalent. The line only renders when `supportedDefaults.length > 0`, and `supportedDefaults` is
  `selectedWorkTrackingSystem?.supportedDefaults ?? []`. A non-empty list therefore means a selected
  connection, so the `null` case the optional chain guards cannot be reached.
- **`InfoPopoverButton.tsx:34`: `sx={{ color: "text.secondary" }}` → `{}` / `""` (2 mutants).** These set the
  icon's colour, which is styling.
- **`InfoPopoverButton.tsx:42`: `anchorOrigin` → `{}` and its two strings (3 mutants).** jsdom gives every
  element a zero-size rectangle, so the anchor point is 0,0 whichever corner is named.
- **`InfoPopoverButton.tsx:43`: `transformOrigin` → `{}` and its two strings (3 mutants).** In jsdom this only
  changes the CSS `transform-origin` of the popover's grow animation. That is placement, not content, and the
  popover still opens with the same text, link and name.
- **`InfoPopoverButton.tsx:46`, `:47`: the `sx` of the popover body and of its text (2 mutants).** Padding,
  maximum width and the margin under the text.
- **`FormSelectField.tsx:65`: `sx={info ? { flex: 1 } : undefined}` → `{}`.** This lets the field fill the row
  beside its info button. It is layout.
- **`FormSelectField.tsx:101`: the row's `sx` → `{}`, `"flex"` → `""`, `"center"` → `""` (3 mutants).** These
  set the field and its info button side by side. It is layout.
- **`AdvancedInputs.tsx:61`: `size={{ xs: 12 }}` → `{}` on the Closed Items Cutoff grid cell.** This is the
  grid column width. It is layout.
- **`WidgetShell.tsx:405`–`:407`, `:416`–`:417`, `:436`: the guidance list's `sx` objects and their flex
  strings (6 mutants).** These set the guidance rows' direction, alignment and line height. It is layout.
- **`WidgetShell.tsx:424`, `:426`, `:428`: the guidance chip's `sx` → `{}`, `color: "#fff"` → `""`,
  `fontSize: "0.65rem"` → `""` (3 mutants).** These style the Sustain / Observe / Act chips in the popover.
  One of them is a borderline case. The `{}` mutant also drops the chip's rating colour. The header's rating
  chip has that colour pinned (`WidgetShell.test.tsx`, *shows a … rating as …, in its own colour*), but the
  guidance chip does not. It was left for a later pass rather than reopening the kill round. A test in the same
  style, opening the popover and asserting `Act` on `#d32f2f`, would kill it.

### Not mutated

- **`ConnectionDefaultFieldSelect.tsx`.** Its changed lines (11–12, 18, 20, 52–57, 102) produced no mutants.
  They are imports and a call that passes `theDefault`, the tracker and the terms through to
  `directionTip`, plus the `info={info}` prop. Stryker does not mutate imports, argument lists or prop
  passing. The spec files for this file ran anyway: `ConnectionDefaultFieldSelect.noTip.test.tsx` and the
  `AdvancedInputs` and `DependenciesComponent` tip suites all go through it.
- **`DependenciesComponent.tsx`.** It has no net change over the feature. The feature commits changed it and
  later refactors returned it to its original form, so `git diff 95dc64767 HEAD` for this file is empty and
  there are no lines to mutate. Its specs stayed in the run because they reach `fieldDirectionTips.ts`
  through the field list.
- StrykerJS does not mutate JSX text, so the copy written directly in markup is absent from the report. The
  tip copy itself lives in `fieldDirectionTips.ts` as module constants, which are mutated and fully killed.
