# Mutation testing — Story 5914, metrics time-horizon presets

Frontend only. The story adds no backend code, so Stryker.NET was not run.

| Run | Score | Killed | Survived | Notes |
| --- | --- | --- | --- | --- |
| 1 | 78.49 % | 135 | 36 | Below the 80 % gate. |
| 2 | 91.37 % | 127 | 12 | After collapsing three copies of one decision and covering the chip's chosen state. |
| 3 | **94.96 %** | **132** | **7** | After covering the second-move reads. |

Reproduce with `stryker-config.5914.json` and `vitest.stryker.mutation.5914.ts`, both
copied here from `Lighthouse.Frontend/` where they are gitignored as local tooling.
Run from `Lighthouse.Frontend/`:

```
TZ=Europe/Zurich npx stryker run stryker-config.5914.json
```

`TZ` matters. The suite runs at `Europe/Zurich` in CI and locally, and the window
arithmetic is deliberately local-calendar, so a UTC runner hides the whole class of
bug these tests exist to catch.

## What the run found that the review had not

**Three copies of one decision.** Both date handlers screened their date before
handing it to the write path, which screens it again. Neither guard could ever be
the one that refused, so a version where they disagreed behaved identically —
which is exactly why the mutants at all three sites survived. The handlers now
forward and the write path decides alone.

**The preset row was asked twice whether it had anything to show**, once by its
caller and once by itself. Removing the caller's copy left the row to answer for
itself.

**The second move was never checked.** Every test moved the window once, so an
emptied dependency list still looked right: there was no first move for a second
one to read wrongly. Four cases now move it twice — an unrelated address param
written by the page must survive the next window write; a second committed step
must walk on rather than repeat; and picking one end after the other must keep the
end just picked.

**The chosen chip's fill and colour were unasserted**, so the chip a reader picks
could have rendered exactly like the ones they passed over. Only `aria-pressed`
was pinned.

## The seven survivors, and why each stays

Every one was applied by hand and the suite re-run. None is an accepted gap.

| Site | Mutation | Verdict |
| --- | --- | --- |
| `DashboardHeader.tsx:121` | `ConditionalExpression → true` | **False survivor.** Applied by hand, 9 tests fail. Stryker misreports it. |
| `dateWindow.ts:71` | `overshoot > 0` → `>= 0` | Equivalent. `shiftWindow(w, -0)` returns the same window, and the re-anchor below runs either way. Confirmed green by hand. |
| `useDateRange.ts:56`, `:57` | `?? ""` → `?? "Stryker was here!"` | Equivalent. Both strings fail to parse as a date, so both fall through to the configured window. Confirmed green by hand. |
| `useDateRange.ts:131` ×2 | `revision + 1` → `revision - 1`; the updater → `() => undefined` | Equivalent **in this hook**. The debounce restarts on the committing callback's changing identity, not on the counter; the counter only has to stay non-zero, which `-1` also does. Confirmed green by hand. The counter is still the shared hook's contract, and `TeamForecastView` passes a real revision, so it stays. |
| `DateRangePresets.tsx:29` | `"default"` → `""` | Equivalent. MUI's Chip renders an unrecognised colour as its default one, so the DOM is identical. Confirmed green by hand. |

The false survivor is the known Stryker behaviour on this repo — a "Survived"
verdict on a load-bearing branch is worth a minute of checking by hand before any
test is written to chase it. Two survivors in run 1 were false; both would have
cost a pointless test.

## Deliberately not pinned

The stepper's layout (`display: contents` plus an `order` rule reaching into
`DateWindowStepper`'s child order) and the provisional-window styling carry narrow
`Stryker disable` comments. jsdom computes no layout, so a test over them could
only read back the string it just wrote. Whether the arrows really straddle the
label is a browser check, and the provisional state itself is carried by
`data-window-pending`, which is asserted.
