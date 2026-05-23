// SCAFFOLD: true
import { describe, it } from "vitest";

describe("TeamForecastForm — Apply forecast-throughput filter toggle (RED scaffold)", () => {
	it("renders the toggle only on premium tenants where the team has a non-empty filter", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-04 AC). DELIVER wave: toggle hidden when filter absent or tenant non-premium.",
		);
	});

	it("defaults the toggle to On when visible", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-04 AC / D3). DELIVER wave: forecast surfaces default to the new opinionated behaviour (filtered).",
		);
	});

	it("submitting the form with toggle Off sends applyFilterOverride=false", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-04 AC). DELIVER wave: assert the POST body.",
		);
	});

	it("submitting the form with toggle On sends applyFilterOverride=true", () => {
		throw new Error("Not yet implemented — RED scaffold (US-04 AC).");
	});

	it("shows the FilteredThroughputChip on the result panel when the response says filterApplied=true", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-03 chip on team forecast result).",
		);
	});
});
