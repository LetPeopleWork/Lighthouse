# Frontend test rules

The frontend half of the test-writing skill. Vitest and React Testing Library, tests next to the code under test. Each rule has a title,
a measured finding in words and one example.

### 1. Logic on the function, not through a render

**Finding.** A page test that renders a large tree to prove a rule cost seconds in the slowest files of a green main run; the same kind of
rule asserted on a function or hook took a fraction of a second. In the audit of a sample of the `.test.tsx` files, a test that proved a
rule through a render took several times as long, on average, as one that called the function directly.

**Example.** Where a state's band sits is a rule of `computePaceBandRects`, exported from `WorkItemAgingChart.tsx`; no chart is rendered:

```ts
const rects = computePaceBandRects({ perStatePercentileValues, doingStates, xScale, yScale, axisMin, axisMax });
expect(rects[0].x).toBe(stateIndex - STATE_BAND_HALF_WIDTH);
```

### 2. One render per wiring

**Finding.** The test files with the most seconds in a green main run each hold dozens of tests that render the same large form or page
again and again; the heaviest take about ten seconds in all.

**Example.** One render of `SaveStateIndicator`, every check of that wiring after it:

```tsx
it("says it could not save and offers a Retry that calls the handler", () => {
  const onRetry = vi.fn();
  render(<SaveStateIndicator saveState="error" canSave={true} onRetry={onRetry} />);
  expect(screen.getByText("Couldn't save")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Retry" }));
  expect(onRetry).toHaveBeenCalledTimes(1);
});
```

### 3. Small fixtures

**Finding.** The tests the allow-list `slowTests.ts` names are paging and long-list tests over a couple of hundred Work Items, each
allowed several seconds; the behaviour they prove needs a page's worth of items, not the whole list.

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
// Not this: it("pages to the last item", async () => { /* 243 items */ }, 15_000);
// This: the same test over a page and one more item, which fits the limit.
const items = Array.from({ length: 11 }, (_, index) => generateWorkItem(index));
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
