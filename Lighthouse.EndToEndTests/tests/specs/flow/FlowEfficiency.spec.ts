import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	CumulativeChartFlowEfficiency,
	FlowEfficiencyOverviewTile,
	type RagStatus,
} from "../../models/metrics/FlowEfficiencyWidget";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";
import { WaitStatesEditor } from "../../models/metrics/WaitStatesEditor";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const CUMULATIVE_STATE_TIME_WIDGET_ID = "stateTimeCumulative";

const DEMO_WAIT_STATE = "Waiting for Verification";

// The unconfigured-prompt spec used to be separate, but its second half re-did this
// spec's setup verbatim — same demo seed, same wait state, same save — to re-read
// the same tile. The unconfigured state is simply what the tile shows BEFORE the
// wait state is marked, so it is asserted here first, on the same seed.
test("@walking_skeleton @US-01 the delivery lead is prompted in red to configure wait states, then an admin marks one and flow efficiency appears on the tile and the cumulative chart", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const tile = new FlowEfficiencyOverviewTile(page);

	// Unconfigured case first: no wait states yet, so the widget explains itself in the body AND
	// flags red, pointing at the setting it needs. Red here is not a measurement — it is a prompt,
	// matching how sibling widgets treat a missing SLE.
	await test.step("unconfigured: the tile explains itself and flags red", async () => {
		const metricsBefore = await teamDetail.goToMetrics();
		await metricsBefore.switchCategory(MetricsCategories.FlowOverview);
		await expect(tile.notConfiguredMessage).toBeVisible();
		await expect(tile.ragChip).toBeVisible();
		expect(await tile.readRagStatus()).toBe("red");
		expect(await tile.readRagTipText()).toContain("wait states in settings");
	});

	await page.goto("/");
	const detailAgain = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const teamEdit = await detailAgain.editTeam();
	const waitStates = new WaitStatesEditor(page);
	await waitStates.enable();
	await waitStates.addWaitState(DEMO_WAIT_STATE);
	const detailAfterSave = await teamEdit.save();

	const metrics = await detailAfterSave.goToMetrics();

	const overviewWidgets = await metrics.switchCategory(
		MetricsCategories.FlowOverview,
	);
	const tileWidget = await metrics.getWidgetByName(
		MetricsWidgetNames.FlowEfficiencyOverview,
		overviewWidgets,
	);
	await expect(tileWidget.Widget).toBeVisible();

	await test.step("configured: the tile reads a measured efficiency and a real RAG status", async () => {
		await expect(tile.efficiencyValue).toContainText("%");

		// The chip is now present and carries a real status, not a fallback.
		await expect(tile.ragChip).toBeVisible();
		const status = await tile.readRagStatus();
		expect(["red", "amber", "green"]).toContain(status);

		// Colour is never the only signal: the chip also carries a text label...
		expect(await tile.readRagLabel()).toBe(
			FlowEfficiencyOverviewTile.labelFor(status as RagStatus),
		);

		// ...and an accessible label that spells out the reading and the action.
		const efficiencyText = await tile.readEfficiencyText();
		const ragTip = await tile.readRagTipText();
		expect(ragTip).toContain("Flow efficiency is");
		expect(ragTip).toContain(efficiencyText);
	});

	await test.step("configured: the cumulative chart carries the same reading, its wait colour key, and a two-row bar tooltip", async () => {
		const flowMetricsWidgets = await metrics.switchCategory(
			MetricsCategories.FlowMetrics,
		);
		const chartWidget = await metrics.getWidgetByName(
			MetricsWidgetNames.CumulativeStateTime,
			flowMetricsWidgets,
		);
		await expect(chartWidget.Widget).toBeVisible();

		const chartEfficiency = new CumulativeChartFlowEfficiency(
			page,
			CUMULATIVE_STATE_TIME_WIDGET_ID,
		);
		await expect(chartEfficiency.titleBlock).toContainText(
			"Cumulative Time per State",
		);
		await expect(chartEfficiency.efficiencyNumber).toContainText("%");
		await expect(chartEfficiency.waitColourKey).toBeVisible();
		await expect(chartEfficiency.waitColourKey).toContainText("Wait");

		await expect(chartEfficiency.waitColourKeySwatch).toBeVisible();
		const swatchBackground =
			await chartEfficiency.readWaitColourKeySwatchBackground();
		expect(swatchBackground).toBe("rgb(244, 67, 54)");

		await chartEfficiency.hoverFirstBar();
		await expect(chartEfficiency.barTooltip).toBeVisible();
		await expect(chartEfficiency.barTooltipRows).toHaveCount(2);
		await expect(chartEfficiency.barTooltip).toContainText("Completed");
		await expect(chartEfficiency.barTooltip).toContainText("Ongoing");

		const completedToggle = chartEfficiency.completionLegendButton("Completed");
		await expect(completedToggle).toHaveAttribute("aria-pressed", "true");
		await completedToggle.click();
		await expect(completedToggle).toHaveAttribute("aria-pressed", "false");
	});
});
