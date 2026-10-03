import { describe, expect, it } from "vitest";
import { UsageDataEventName } from "../Api/UsageDataService";
import { usageDataPageOpeningFor } from "./usageDataRouteKeys";

/**
 * Opening a Team's Refinement tab is the existing Team tab opening, naming one more tab. The name of the
 * tab travels as text, because the server reads it as text; the Team's identifier never leaves.
 */
describe("the Refinement tab is a Team tab like the others", () => {
	// @us-02 @slice-02 @kpi-OUT-5510-K2-refinement-tab-weekly @contract-shape:pure-function
	it("answers a Team's Refinement tab as a Team tab opening naming that tab", () => {
		expect(usageDataPageOpeningFor("/teams/7/refinement")).toEqual({
			name: UsageDataEventName.TeamTabOpened,
			route: "TeamDetail_Refinement",
		});
	});

	// @us-02 @slice-02 @kpi-OUT-5510-K2-refinement-tab-weekly @error @contract-shape:pure-function
	it("names the same tab whichever Team it belongs to, so nothing in it can tell Teams apart", () => {
		expect(usageDataPageOpeningFor("/teams/7/refinement")).toBeDefined();
		expect(usageDataPageOpeningFor("/teams/7/refinement")).toEqual(
			usageDataPageOpeningFor("/teams/1234/refinement"),
		);
		expect(usageDataPageOpeningFor("/teams/7/refinement")?.route).not.toMatch(
			/7/,
		);
	});
});
