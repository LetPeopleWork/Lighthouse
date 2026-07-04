import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	BlockedRuleConfigEditor,
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";

// The Blocked overview counts in-progress (WIP) items only, so the saved rule
// must match a WIP item for the widget to change from 0. Every in-progress demo
// item is a "User Story", so matching on Type reliably marks the WIP population
// blocked regardless of which Doing state each item sits in on a given run day.
const DEMO_BLOCKED_FIELD = "Type";
const DEMO_BLOCKED_VALUE = "User Story";

test("@walking_skeleton a config-admin saves a blocked rule and an item matching it reads blocked in the Blocked overview widget", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const teamEdit = await teamDetail.editTeam();

	// The config admin defines the blocked rule set from scratch: clear any
	// seeded rule so the resulting blocked count is attributable to the rule
	// saved here, then add a single matching rule.
	const blockedEditor = new BlockedRuleConfigEditor(page);
	await blockedEditor.enable();
	await blockedEditor.clearExistingRules();
	await blockedEditor.addFieldEqualsRule(
		DEMO_BLOCKED_FIELD,
		DEMO_BLOCKED_VALUE,
	);
	const detailAfterSave = await teamEdit.save();

	const metrics = await detailAfterSave.goToMetrics();

	const overviewWidgets = await metrics.switchCategory(
		MetricsCategories.FlowOverview,
	);
	const blockedWidget = await metrics.getWidgetByName(
		MetricsWidgetNames.BlockedItemsOverview,
		overviewWidgets,
	);

	await expect(blockedWidget.Widget).toBeVisible();
	await expect(blockedWidget.blockedOverviewCount).toBeVisible();
	await expect
		.poll(() => blockedWidget.getBlockedOverviewCount())
		.toBeGreaterThan(0);
});
