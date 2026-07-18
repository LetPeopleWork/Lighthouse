import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { CycleTimeScatterPlotChart } from "../../models/metrics/CycleTimeScatterPlotChart";
import {
	CycleTimePercentilesWidget,
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const DEMO_PORTFOLIO_NAME = "Project Apollo";
const CYCLE_SCATTER_WIDGET_ID = "cycleScatter";
const DEFAULT_SCOPE = "Default";

// Backlog -> Done. Wider than the Default window (which opens at "Next", the first
// Doing state), so no percentile may come back narrower than its Default value.
// On demo data the synthesized state journey enters Backlog and Next on the same
// day, so the numbers coincide with Default — hence the separate "Analysis to Done"
// leg below, which is what proves the widget really re-queries per definition.
const WIDE_DEFINITION = "Lead Time (End to End)";
// Analysing -> Done. Narrower than Default and demonstrably different on demo data.
const NARROW_DEFINITION = "Analysis to Done";

type Percentiles = Record<string, number>;

function expectSamePercentileRows(
	actual: Percentiles,
	expected: Percentiles,
): void {
	expect(Object.keys(actual).length).toBeGreaterThan(0);
	expect(Object.keys(actual).sort()).toEqual(Object.keys(expected).sort());
}

function expectNoNarrowerThanDefault(
	actual: Percentiles,
	defaultValues: Percentiles,
): void {
	expectSamePercentileRows(actual, defaultValues);

	for (const [percentile, value] of Object.entries(actual)) {
		expect(
			value,
			`${percentile}: ${WIDE_DEFINITION} spans a wider window than ${DEFAULT_SCOPE}`,
		).toBeGreaterThanOrEqual(defaultValues[percentile]);
	}
}

async function assertNamedSelectionBehaviour(
	percentiles: CycleTimePercentilesWidget,
): Promise<void> {
	// Default scope: the SLE-driven RAG chip shows and no "named" notice does.
	await expect
		.poll(
			async () => Object.keys(await percentiles.getPercentileValues()).length,
		)
		.toBeGreaterThan(0);
	expect(await percentiles.getSelectedScope()).toBe(DEFAULT_SCOPE);
	expect(await percentiles.hasRagChip()).toBe(true);

	const defaultValues = await percentiles.getPercentileValues();

	expect(await percentiles.listScopeOptions()).toEqual(
		expect.arrayContaining([DEFAULT_SCOPE, WIDE_DEFINITION, NARROW_DEFINITION]),
	);

	// --- the wide named definition ---
	await percentiles.selectScope(WIDE_DEFINITION);
	await expect
		.poll(() => percentiles.getSelectedScope())
		.toContain(WIDE_DEFINITION);

	// The RAG goes neutral: the SLE only targets the Default cycle time, so
	// WidgetShell renders no chip. The explanation sits in the header tip
	// tooltip - the card body stays free of caption text.
	await expect.poll(() => percentiles.hasRagChip()).toBe(false);
	expectNoNarrowerThanDefault(
		await percentiles.getPercentileValues(),
		defaultValues,
	);

	// --- the narrow named definition: the values genuinely re-plot ---
	await percentiles.selectScope(NARROW_DEFINITION);
	await expect
		.poll(() => percentiles.getSelectedScope())
		.toContain(NARROW_DEFINITION);
	await expect
		.poll(() => percentiles.getPercentileValues())
		.not.toEqual(defaultValues);
	expectSamePercentileRows(
		await percentiles.getPercentileValues(),
		defaultValues,
	);
	expect(await percentiles.hasRagChip()).toBe(false);

	// --- back to Default: the SLE-driven RAG returns ---
	await percentiles.selectScope(DEFAULT_SCOPE);
	await expect
		.poll(() => percentiles.getPercentileValues())
		.toEqual(defaultValues);
	await expect.poll(() => percentiles.hasRagChip()).toBe(true);
}

test("@walking_skeleton @premium delivery lead selects a named cycle time on the team Flow Overview percentiles and the values re-plot with a neutral RAG", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetail.goToMetrics();
	const flowOverviewWidgets = await metrics.switchCategory(
		MetricsCategories.FlowOverview,
	);
	const widget = await metrics.getWidgetByName(
		MetricsWidgetNames.CycleTimePercentiles,
		flowOverviewWidgets,
	);
	await expect(widget.Widget).toBeVisible();

	const percentiles = new CycleTimePercentilesWidget(page, widget.Id);
	await assertNamedSelectionBehaviour(percentiles);

	// The percentiles selection must not leak across tabs: the scatterplot on Flow
	// Metrics keeps its own, still-Default selection.
	await percentiles.selectScope(WIDE_DEFINITION);
	await expect
		.poll(() => percentiles.getSelectedScope())
		.toContain(WIDE_DEFINITION);

	const flowMetricsWidgets = await metrics.switchCategory(
		MetricsCategories.FlowMetrics,
	);
	const scatterWidget = await metrics.getWidgetByName(
		MetricsWidgetNames.CycleTimeScatterplot,
		flowMetricsWidgets,
	);
	await expect(scatterWidget.Widget).toBeVisible();

	const scatter = new CycleTimeScatterPlotChart(page, CYCLE_SCATTER_WIDGET_ID);
	await expect.poll(() => scatter.countDots()).toBeGreaterThan(0);
	expect(await scatter.getSelectedDefinition()).toBe(DEFAULT_SCOPE);
});

test("@premium the portfolio Flow Overview percentiles honour the named cycle time selection the same way the team's do", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const portfolioDetail = await overviewPage.goToPortfolio(DEMO_PORTFOLIO_NAME);
	const metrics = await portfolioDetail.goToMetrics();
	const flowOverviewWidgets = await metrics.switchCategory(
		MetricsCategories.FlowOverview,
	);
	const widget = await metrics.getWidgetByName(
		MetricsWidgetNames.CycleTimePercentiles,
		flowOverviewWidgets,
	);
	await expect(widget.Widget).toBeVisible();

	const percentiles = new CycleTimePercentilesWidget(page, widget.Id);
	await assertNamedSelectionBehaviour(percentiles);
});
