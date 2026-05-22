// SCAFFOLD: true
import { describe, it } from "vitest";

describe("ThroughputChartFilterToggle (RED scaffold)", () => {
	it("renders the Show: Raw or Filtered toggle only when the team has a non-empty filter on a premium tenant", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-05 AC). DELIVER wave: toggle hidden when filter absent or tenant non-premium.",
		);
	});

	it("defaults the toggle to Raw on every render to preserve today's chart behaviour", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-05 AC / invariant #1 / D1). DELIVER wave: default Raw preserves existing UX for unsuspecting users.",
		);
	});

	it("flipping the toggle to Filtered re-renders the chart client-side without a network round-trip on the Run Chart", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-05 / DDD-5 Run Chart half). DELIVER wave: hand-port the minimal evaluator over RunChartData.WorkItemsPerUnitOfTime in TS; assert no extra fetch is triggered.",
		);
	});

	it("flipping the toggle to Filtered on PBC issues a request with ?view=filtered", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-05 / DDD-5 PBC half). DELIVER wave: PBC payload lacks per-item granularity so server-side filter via ?view= query param.",
		);
	});

	it("shows the FilteredThroughputChip next to the chart title when the toggle is Filtered", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-03 chip on charts).",
		);
	});

	it("shows the empty-state message when the filter excludes every item in the window (Filtered view)", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-05 / D5 chart half). DELIVER wave: copy 'No items match the throughput filter in this window. Switch to Raw to see total throughput.'",
		);
	});

	it("operator parity with the C# evaluator on equals / notEquals / contains over a representative work item set", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (DESIGN open question #2). DELIVER wave: 'operator parity' test gating the TS-side evaluator port against the canary corpus.",
		);
	});
});
