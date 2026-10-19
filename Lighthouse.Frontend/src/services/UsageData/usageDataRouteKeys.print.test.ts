import { describe, expect, it } from "vitest";
import { usageDataPageOpeningFor } from "./usageDataRouteKeys";

/**
 * A report's print page is drawn for paper, not opened by anyone, so its address names no page: the export
 * was already counted when it was chosen, and counting the page as well would count it twice.
 */
describe("a report's print page is no page anyone opened", () => {
	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:pure-function
	it.each(["/print/teams/7/reports/42", "/print/portfolios/7/reports/42"])(
		"names no page for %s",
		(address) => {
			expect(usageDataPageOpeningFor("/teams/7/reports")).toBeDefined();
			expect(usageDataPageOpeningFor(address)).toBeUndefined();
		},
	);
});

/**
 * The way back from a print page started on the Reports list lands on that list again, and that arrival is
 * counted as the Reports tab opening like any other: one extra count per export from the list is accepted, so
 * the return carries nothing that would tell the counting to look away.
 */
describe("returning to the Reports list after printing", () => {
	// @us-01 @slice-01 @edge @contract-shape:pure-function
	it.each(["/teams/7/reports", "/portfolios/7/reports"])(
		"names %s as the Reports tab, as any arrival there does",
		(address) => {
			expect(usageDataPageOpeningFor(address)).toBeDefined();
			expect(usageDataPageOpeningFor(address)).toEqual(
				usageDataPageOpeningFor(address.replace("/7/", "/8/")),
			);
		},
	);
});
