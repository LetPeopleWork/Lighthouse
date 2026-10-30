import { describe, expect, it } from "vitest";
import { UsageDataEventName } from "../Api/UsageDataService";
import { usageDataPageOpeningFor } from "./usageDataRouteKeys";

/**
 * Opening the Reports tab of a Team or a Portfolio is the existing tab opening, naming one more tab. One
 * report being opened is told by its own event once it has been read, so its address names no tab at all;
 * neither the owner's identifier nor the report's ever leaves.
 */
describe("the Reports tab is a tab like the others", () => {
	it("answers a Team's Reports tab as a Team tab opening naming that tab", () => {
		expect(usageDataPageOpeningFor("/teams/7/reports")).toEqual({
			name: UsageDataEventName.TeamTabOpened,
			route: "TeamDetail_Reports",
		});
	});

	it("answers a Portfolio's Reports tab as a Portfolio tab opening naming that tab", () => {
		expect(usageDataPageOpeningFor("/portfolios/7/reports")).toEqual({
			name: UsageDataEventName.PortfolioTabOpened,
			route: "PortfolioDetail_Reports",
		});
	});

	it("names no tab for the address of one report", () => {
		expect(usageDataPageOpeningFor("/teams/7/reports")).toBeDefined();
		expect(usageDataPageOpeningFor("/teams/7/reports/42")).toBeUndefined();
		expect(usageDataPageOpeningFor("/portfolios/7/reports/42")).toBeUndefined();
	});

	it("names the same tab whichever Team it belongs to, so nothing in it can tell Teams apart", () => {
		expect(usageDataPageOpeningFor("/teams/7/reports")).toBeDefined();
		expect(usageDataPageOpeningFor("/teams/7/reports")).toEqual(
			usageDataPageOpeningFor("/teams/1234/reports"),
		);
	});
});
