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
