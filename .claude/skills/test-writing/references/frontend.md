# Frontend test rules

The frontend half of the test-writing skill. Vitest and React Testing Library, tests next to the code under test. Each rule has a title,
a measured finding in words and one example.

### 1. Logic on the function, not through a render

**Finding.** A page test that renders a large tree to prove a rule cost seconds in the slowest files of a green main run; the same kind of
rule asserted on a function or hook took a fraction of a second. Measured on a sample of page and dialog tests, a test that proved a
rule through a render took about ten times as long, on average, as one that called the function directly (about a sixth of a second against about a hundredth).

**Example.** Where a state's band sits is a rule of `computePaceBandRects`, exported from `WorkItemAgingChart.tsx`; no chart is rendered:

```ts
const rects = computePaceBandRects({ perStatePercentileValues, doingStates, xScale, yScale, axisMin, axisMax });
expect(rects[0].x).toBe(stateIndex - STATE_BAND_HALF_WIDTH);
```

### 2. One render per wiring

**Finding.** The ten test files with the most seconds in a green main run take between eight and eleven seconds each, and hold between
eighteen and over a hundred tests. In `ModifyTeamSettings.test.tsx`, one of them, each test renders the whole form again.

**Example.** One test of one behaviour renders once and checks everything that behaviour shows. Sharing means several expectations in one
test, not one render kept across several tests (rule 6: each test builds its own state):

```tsx
it("offers a Retry when saving failed", () => {
  const onRetry = vi.fn();
  render(<SaveStateIndicator saveState="error" canSave={true} onRetry={onRetry} />);
  expect(screen.getByText("Couldn't save")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Retry" }));
  expect(onRetry).toHaveBeenCalledTimes(1);
});
```

### 3. Small fixtures

**Finding.** The three entries of the allow-list `slowTests.ts` are a dialog over 243 Work Items, a refinement list of 143 Work Items and a
long list of packages, with ceilings from 2.5 to 5.0 seconds. Each one renders a list far longer than the behaviour it proves needs.

**Example.** Three Work Items show the order. Two hundred show only the cost:

```ts
const items = Array.from({ length: 3 }, (_, index) => generateWorkItem(index));
```

### 4. Fake clock, no real wait

**Finding.** A real wait costs the whole delay each time it runs. The autosave debounce is about a third of a second and the helper
`waitPastTheDebounce` waits it out on the real clock; `pastTheAutosaveDelay` moves a fake clock past it at once.

**Example.** The fake clock with `shouldAdvanceTime`, so awaits that rely on real ticks still settle, and no sleep:

```ts
beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: true });
});
afterEach(() => {
  vi.useRealTimers();
});

// ...after the edit:
await pastTheAutosaveDelay();
expect(mockSaveTeamSettings).toHaveBeenCalledTimes(1);
```

### 5. No `testTimeout` or retry as the fix

**Finding.** The per-test limit fails a test over its limit whatever timeout the test was given, so a longer timeout only moves the
failure from the runner to the reporter; and a retry hides a test that depends on load or on order, which then comes back on another
machine.

**Example.** A slow test is made smaller until it fits, not given more time:

```tsx
// Not this: the same test over 243 Work Items, with a 15-second timeout added.
// This: a page and one more item (PAGE_SIZE is exported by RowRangeFooter.tsx), which is all paging needs.
const items = Array.from({ length: PAGE_SIZE + 1 }, (_, index) => generateWorkItem(index));
```

### 6. No order dependence

**Finding.** The CI frontend run shuffles the tests of every file, seeded by the run id, and has found three kinds: a test that fails alone
or on one seed and passes on the next; a lazy route or provider that only the first test to need it pays for; a listener or timer a test
leaves behind for the next. Replay a red file with `pnpm test --sequence.shuffle.tests --sequence.seed=<run id> <the red file>`.

**Example.** Each test starts from state it builds itself and leaves nothing behind:

```ts
beforeEach(() => {
  mockGetTeamSettings.mockClear();
  mockGetTeamSettings.mockResolvedValue(teamSettings);
});
afterEach(() => {
  localStorage.clear();
  vi.restoreAllMocks();
});
```

### 7. A new view pins its loading look and its failure look

**Finding.** A view that shows its previous answer undimmed while the next one loads looks current, and a failed load that shows an
empty area looks like "no data". End-to-end page objects that waited for mere visibility read the answer of a slow request that had
not yet been replaced, and now wait for `data-widget-status="ready"`.

**Example.** From the Predictability Score details loading test, which renders a `WidgetShell`: the loading look and the failure look, each asserted:

```tsx
// loading: the previous answer stays, dimmed under a spinner
expect(shownScore()).toBeVisible();
expect(screen.getByTestId("widget-shell-body-predictabilityScoreDetails")).toHaveStyle({ opacity: "0.4" });
expect(screen.getByRole("progressbar")).toBeInTheDocument();
// failure: the message, never an empty area
expect(frame()).toHaveAttribute("data-widget-status", "error");
expect(screen.getByText(COULD_NOT_LOAD_MESSAGE)).toBeInTheDocument();
```

End of frontend.md
