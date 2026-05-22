// SCAFFOLD: true
//
// Forecast Filter — Playwright spec (executable form)
//
// Feature: filter-forecast-throughput (ADO Epic #4896)
// Wave: DISTILL (skeleton — the walking-skeleton scenario starts as test.skip())
//
// The Gherkin documentation form lives alongside as `ForecastFilter.feature`.
// Same scenario title in both. DELIVER unskips when the slice-01 backend +
// frontend wiring lands; the walking_skeleton then drives every later slice
// (US-04 toggle, US-05 chart toggle, US-06 backtest toggle) end-to-end.
//
// Walking Skeleton Strategy: B — Real local + faked WTS.
//   - Real WebApplicationFactory backend, real Sqlite, real Vitest.
//   - Work-tracking system connector (Jira / ADO / Linear) faked via the existing
//     stub pattern. The faked connector returns a mixed closed history of User
//     Stories and Bugs so that the filter has observable effect.

import { expect } from "@playwright/test";
import { testWithUpdatedTeams } from "../../fixutres/LighthouseFixture";

const test = testWithUpdatedTeams([0]);

test.describe("Forecast filter — premium walking skeleton", () => {
	test.skip(
		"[@walking_skeleton @premium @driving_adapter @real-io @US-01 @US-02 @US-03 @US-04 @US-05 @US-06 @kpi-OUT-filter-adoption] Premium delivery-forecaster configures the filter and propagates it across every forecast surface",
		async () => {
			throw new Error(
				"Not yet implemented — RED scaffold. DELIVER wave wires this up after slice-01 backend lands.",
			);
		},
	);
});
