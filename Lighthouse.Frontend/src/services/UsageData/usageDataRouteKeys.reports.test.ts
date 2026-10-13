import { describe, expect, it } from "vitest";
import { UsageDataEventName } from "../Api/UsageDataService";
import { usageDataPageOpeningFor } from "./usageDataRouteKeys";

/**
 * Opening the Reports tab of a Team or a Portfolio is the existing tab opening, naming one more tab. One
 * report being opened is told by its own event once it has been read, so its address names no tab at all;
 * neither the owner's identifier nor the report's ever leaves.
 */
describe("the Reports tab is a tab like the others", () => {
	// @us-01 @slice-01 @kpi-OUT-5878-K1-report-created @contract-shape:pure-function
	it("answers a Team's Reports tab as a Team tab opening naming that tab", () => {
		expect(usageDataPageOpeningFor("/teams/7/reports")).toEqual({
			name: UsageDataEventName.TeamTabOpened,
			route: "TeamDetail_Reports",
		});
	});

	// @us-06 @slice-06 @contract-shape:pure-function
	it.skip("answers a Portfolio's Reports tab as a Portfolio tab opening naming that tab", () => {
		expect(usageDataPageOpeningFor("/portfolios/7/reports")).toEqual({
			name: UsageDataEventName.PortfolioTabOpened,
			route: "PortfolioDetail_Reports",
		});
	});

	// @us-01 @slice-01 @error @contract-shape:pure-function
	it("names no tab for the address of one report", () => {
		expect(usageDataPageOpeningFor("/teams/7/reports")).toBeDefined();
		expect(usageDataPageOpeningFor("/teams/7/reports/42")).toBeUndefined();
		expect(usageDataPageOpeningFor("/portfolios/7/reports/42")).toBeUndefined();
	});

	// @us-01 @slice-01 @error @contract-shape:pure-function
	it("names the same tab whichever Team it belongs to, so nothing in it can tell Teams apart", () => {
		expect(usageDataPageOpeningFor("/teams/7/reports")).toBeDefined();
		expect(usageDataPageOpeningFor("/teams/7/reports")).toEqual(
			usageDataPageOpeningFor("/teams/1234/reports"),
		);
	});
});
