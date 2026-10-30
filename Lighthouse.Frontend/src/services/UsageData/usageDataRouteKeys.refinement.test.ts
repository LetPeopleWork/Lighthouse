import { describe, expect, it } from "vitest";
import { UsageDataEventName } from "../Api/UsageDataService";
import { usageDataPageOpeningFor } from "./usageDataRouteKeys";

/**
 * Opening a Team's Refinement tab is the existing Team tab opening, naming one more tab. The name of the
 * tab travels as text, because the server reads it as text; the Team's identifier never leaves.
 */
describe("the Refinement tab is a Team tab like the others", () => {
	it("answers a Team's Refinement tab as a Team tab opening naming that tab", () => {
		expect(usageDataPageOpeningFor("/teams/7/refinement")).toEqual({
			name: UsageDataEventName.TeamTabOpened,
			route: "TeamDetail_Refinement",
		});
	});

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
