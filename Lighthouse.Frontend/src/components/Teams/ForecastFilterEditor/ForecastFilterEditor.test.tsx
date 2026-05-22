// SCAFFOLD: true
import { describe, it } from "vitest";

describe("ForecastFilterEditor (RED scaffold)", () => {
	it("renders the existing DeliveryRuleBuilder configured for exclusion semantics", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (DDD-6, US-01). DELIVER wave: assert <DeliveryRuleBuilder> renders with title='Exclude items where…' and emptyStateMessage='Add at least one rule to exclude work items from forecast throughput.'",
		);
	});

	it("fetches the WorkItem field schema from /api/team/:teamId/forecast-filter/schema on mount", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-01 / D9). DELIVER wave: mock the schema service; assert the editor passes the resolved schema fields to DeliveryRuleBuilder.",
		);
	});

	it("renders read-only when the current user is not a team admin for the team", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-01 AC). DELIVER wave: useRbac().isTeamAdmin(teamId)==false renders DeliveryRuleBuilder in disabled mode.",
		);
	});

	it("renders read-only when the rule set is non-empty and the user is a viewer", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-01 AC). DELIVER wave: viewers see existing rules so they understand why their forecasts are filtered.",
		);
	});

	it("does not render the rule editor when the tenant is non-premium", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-07). DELIVER wave: useLicenseRestrictions().isPremium==false replaces the editor with the upgrade teaser.",
		);
	});
});
