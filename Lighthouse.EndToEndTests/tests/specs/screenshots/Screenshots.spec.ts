import {
	expect,
	test,
	testWithDemoData,
} from "../../fixutres/LighthouseFixture";
import {
	takeDialogScreenshot,
	takeDialogScreenshot as takeElementScreenshot,
	takePageScreenshot,
} from "../../helpers/screenshots";
import { CumulativeStateTimeChart } from "../../models/metrics/CumulativeStateTimeChart";
import {
	CumulativeChartFlowEfficiency,
	FlowEfficiencyOverviewTile,
} from "../../models/metrics/FlowEfficiencyWidget";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";
import { WaitStatesEditor } from "../../models/metrics/WaitStatesEditor";
import { WorkItemAgingChart } from "../../models/metrics/WorkItemAgingChart";
import { DeliveryMetricsTab } from "../../models/portfolios/Deliveries/DeliveryMetricsTab";

const DEMO_SCENARIO_ID = 0;
const testWithDemo = testWithDemoData(DEMO_SCENARIO_ID);

test("Take @screenshot of empty overview page", async ({ overviewPage }) => {
	await takePageScreenshot(overviewPage.page, "installation/landingpage.png");
});

test("Take @screenshot of licensing", async ({ overviewPage }) => {
	await overviewPage.lightHousePage.showLicenseTooltip();

	// Get the bounding box of the toolbar
	const toolbarBoundingBox = await overviewPage.toolbar.boundingBox();
	if (toolbarBoundingBox) {
		// Calculate the right third of the toolbar
		const rightThirdWidth = toolbarBoundingBox.width / 3;
		const rightThirdX =
			toolbarBoundingBox.x + (toolbarBoundingBox.width * 2) / 3;

		// Take screenshot of only the right third of the toolbar
		await overviewPage.page.screenshot({
			path: `${require("../../helpers/folderPaths").getPathToDocsAssetsFolder()}/licensing/toolbar.png`,
			clip: {
				x: rightThirdX,
				y: toolbarBoundingBox.y,
				width: rightThirdWidth,
				height: toolbarBoundingBox.height,
			},
		});
	}

	const licensingPopover =
		await overviewPage.lightHousePage.showLicensingInformation();

	await takeElementScreenshot(
		licensingPopover,
		"licensing/licenseinformation.png",
	);
});

test("Take @screenshot of setting pages", async ({ overviewPage }) => {
	const settingsPage = await overviewPage.lightHousePage.goToSettings();

	const demoDataPage = await settingsPage.goToDemoData();
	await takePageScreenshot(demoDataPage.page, "settings/demodata.png");

	const systemSettings = await settingsPage.goToSystemConfiguration();

	await takePageScreenshot(systemSettings.page, "settings/configuration.png");

	await takeElementScreenshot(
		systemSettings.terminologyConfiguration,
		"settings/terminologyConfiguration.png",
	);

	await takeElementScreenshot(
		systemSettings.teamRefreshSettings,
		"settings/teamrefreshsettings.png",
	);
	await takeElementScreenshot(
		systemSettings.featureRefreshSettings,
		"settings/featurerefreshsettings.png",
	);

	if (await systemSettings.optionalFeatures.isVisible()) {
		await takeElementScreenshot(
			systemSettings.optionalFeatures,
			"settings/optionalfeatures.png",
		);
	}

	const logs = await settingsPage.goToSystemInfo();
	await takePageScreenshot(logs.page, "settings/systeminfo.png");

	const systemInfoTable = logs.page.getByRole("table", {
		name: "system information",
	});
	await takeElementScreenshot(systemInfoTable, "settings/systeminfo_auth.png");

	const databaseManagement = await settingsPage.goToDatabaseManagement();
	await takePageScreenshot(
		databaseManagement.page,
		"settings/databasemanagement.png",
	);
});

test("Take @screenshot of blackout periods and recurring rules", async ({
	overviewPage,
}) => {
	const settingsPage = await overviewPage.lightHousePage.goToSettings();
	let systemSettings = await settingsPage.goToSystemConfiguration();

	const startDate = new Date(Date.now()).toISOString().slice(0, 10);
	const endDate = new Date(Date.now() + 24 * 60 * 60 * 1000)
		.toISOString()
		.slice(0, 10);

	const blackoutPeriodDialog = await systemSettings.addBlackoutPeriod();
	await blackoutPeriodDialog.addBlackoutPeriod(
		"GCZ Meisterfeier",
		startDate,
		endDate,
	);
	await takeDialogScreenshot(
		blackoutPeriodDialog.page.getByRole("dialog"),
		"settings/blackoutPeriodConfiguration.png",
	);
	systemSettings = await blackoutPeriodDialog.saveBlackoutPeriod();

	const recurringRuleDialog = await systemSettings.addRecurringRule();
	await recurringRuleDialog.configureRule({
		weekdays: ["Monday", "Friday"],
		intervalWeeks: 2,
		startDate,
		description: "Sprint planning & review",
	});
	await takeDialogScreenshot(
		recurringRuleDialog.page.getByRole("dialog"),
		"settings/recurringBlackoutRuleConfiguration.png",
	);
	systemSettings = await recurringRuleDialog.saveRule();

	await takeElementScreenshot(
		systemSettings.blackoutPeriodsSection,
		"settings/blackoutPeriodsSection.png",
	);
});

testWithDemo(
	"Take @screenshot of populated overview, teams overview, team detail, portfolios overview, and portfolio detail pages",
	async ({ testData, overviewPage }) => {
		const teamsPage = await overviewPage.lightHousePage.goToOverview();
		const deleteTeamDialog = await teamsPage.deleteTeam(testData.teams[0].name);
		await takeElementScreenshot(
			deleteTeamDialog.page.getByRole("dialog"),
			"features/teams_delete.png",
			0.5,
			1000,
		);

		await deleteTeamDialog.cancel();

		const teamDetailPage = await overviewPage.goToTeam(testData.teams[0].name);
		teamDetailPage.goToForecasts();

		const inTwoWeeksDate = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000);
		await teamDetailPage.forecast(10, inTwoWeeksDate);
		await takePageScreenshot(teamDetailPage.page, "features/teamdetail.png", 3);

		await teamDetailPage.forecastNewWorkItems(["Bug"]);
		await takeElementScreenshot(
			teamDetailPage.page.getByText(
				"New Work Items Creation ForecastHistorical DataHistorical data that should be",
			),
			"features/creationforecast.png",
		);

		// Backtest screenshot
		await teamDetailPage.runBacktest();
		await expect(teamDetailPage.backtestResultsSection).toBeVisible();
		await takeElementScreenshot(
			teamDetailPage.backtestForecastingSection.locator("../../.."),
			"features/backtest.png",
		);

		overviewPage.lightHousePage.goToOverview();

		// portfolio Deletion Dialog
		const deletePortfolioDialog = await overviewPage.deletePortfolio(
			testData.portfolios[0],
		);

		await takeElementScreenshot(
			deletePortfolioDialog.page.getByRole("dialog"),
			"features/portfolios_delete.png",
			0.5,
			1000,
		);
		await deletePortfolioDialog.cancel();

		// portfolio Detail Page
		const portfolioDetailPage = await overviewPage.goToPortfolio(
			testData.portfolios[0].name,
		);
		await takePageScreenshot(
			portfolioDetailPage.page,
			"features/portfoliodetail.png",
			3,
		);

		let deliveryPage = await portfolioDetailPage.goToDeliveries();
		const addDeliveryPage = await deliveryPage.addDelivery();
		await addDeliveryPage.setDeliveryName("Next Release");

		const futureDate = new Date(Date.now() + 24 * 60 * 60 * 1000)
			.toISOString()
			.slice(0, 10);

		await addDeliveryPage.setDeliveryDate(futureDate);

		await addDeliveryPage.selectFeatureByIndex(0);
		await addDeliveryPage.selectFeatureByIndex(1);

		await takeDialogScreenshot(
			addDeliveryPage.page.getByRole("dialog"),
			"features/delivery_add.png",
			0.5,
			1000,
		);

		await addDeliveryPage.switchToRuleBased();

		await addDeliveryPage.addRule();
		await addDeliveryPage.setRuleField(0, "Type");
		await addDeliveryPage.setRuleOperator(0, "Equals");
		await addDeliveryPage.setRuleValue(0, "Epic");

		await addDeliveryPage.addRule();
		await addDeliveryPage.setRuleField(1, "State");
		await addDeliveryPage.setRuleOperator(1, "Not Equals");
		await addDeliveryPage.setRuleValue(1, "Closed");

		await addDeliveryPage.validateRules();

		await takeDialogScreenshot(
			addDeliveryPage.page.getByRole("dialog"),
			"features/delivery_rule_based.png",
			0.5,
			1000,
		);

		await addDeliveryPage.switchToManual();

		deliveryPage = await addDeliveryPage.save();
		const delivery = deliveryPage.getDeliveryByName("Next Release");
		await delivery.toggleDetails();

		await takePageScreenshot(
			deliveryPage.page,
			"features/delivery_detail.png",
			5,
			1000,
		);
	},
);
testWithDemo(
	"Take @screenshot of Metrics",
	async ({ testData, overviewPage }) => {
		await overviewPage.lightHousePage.goToOverview();

		// Go to Metrics Tab
		const teamDetailPage = await overviewPage.goToTeam(testData.teams[0].name);
		const metricsPage = await teamDetailPage.goToMetrics();

		// Metrics Overview
		await takePageScreenshot(
			metricsPage.page,
			"features/metrics/metricsoverview.png",
		);

		const metricCategories = [
			MetricsCategories.FlowOverview,
			MetricsCategories.FlowMetrics,
			MetricsCategories.Predictability,
			MetricsCategories.PortfolioAndFeatures,
		];

		for (const category of metricCategories) {
			const metrics = await metricsPage.switchCategory(category);

			for (const metricWidget of metrics) {
				if (
					metricWidget.name === MetricsWidgetNames.EstimationVsCycleTime ||
					metricWidget.name === MetricsWidgetNames.FeatureSizePercentiles
				) {
					continue; // Skip Estimation vs Cycle Time chart as it is covered in a separate test and has a different setup process
				}

				await takeElementScreenshot(
					metricWidget.Widget,
					`features/metrics/${metricWidget.Id}.png`,
				);
			}
		}

		let availableWidgets = await metricsPage.switchCategory(
			MetricsCategories.FlowOverview,
		);

		const cycleTimePercentilesWidget = await metricsPage.getWidgetByName(
			MetricsWidgetNames.CycleTimePercentiles,
			availableWidgets,
		);

		const workItemsDialog = await cycleTimePercentilesWidget.openDialog();
		await takeElementScreenshot(
			workItemsDialog.page.getByRole("dialog"),
			"features/metrics/workitemsdialog.png",
		);
		await workItemsDialog.close();

		overviewPage = await overviewPage.lightHousePage.goToOverview();

		const portfolioDetailPage = await overviewPage.goToPortfolio(
			testData.portfolios[0].name,
		);

		await portfolioDetailPage.refreshFeatures();
		await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled({
			timeout: 90_000,
		});

		const portfolioMetricsPage = await portfolioDetailPage.goToMetrics();

		availableWidgets = await portfolioMetricsPage.switchCategory(
			MetricsCategories.PortfolioAndFeatures,
		);

		const featureSizeWidget = await portfolioMetricsPage.getWidgetByName(
			MetricsWidgetNames.FeatureSize,
			availableWidgets,
		);

		await takeElementScreenshot(
			featureSizeWidget.Widget,
			"features/metrics/featuresize.png",
		);

		availableWidgets = await portfolioMetricsPage.switchCategory(
			MetricsCategories.Predictability,
		);

		const featureSizeProcessBehaviourWidget =
			await portfolioMetricsPage.getWidgetByName(
				MetricsWidgetNames.FeatureSizeProcessBehaviourChart,
				availableWidgets,
			);

		await takeElementScreenshot(
			featureSizeProcessBehaviourWidget.Widget,
			"features/metrics/featureSizeProcessBehaviourChart.png",
		);

		availableWidgets = await portfolioMetricsPage.switchCategory(
			MetricsCategories.FlowOverview,
		);

		const featureSizePercentilesWidget =
			await portfolioMetricsPage.getWidgetByName(
				MetricsWidgetNames.FeatureSizePercentiles,
				availableWidgets,
			);

		await takeElementScreenshot(
			featureSizePercentilesWidget.Widget,
			"features/metrics/featureSizePercentiles.png",
		);
	},
);
const workTrackingSystemConfiguration = [
	{
		workTrackingSystemName: "AzureDevOps",
		workTrackingSystemOptions: [
			{
				field: "Organization URL",
				value: "https://dev.azure.com/letpeoplework",
			},
		],
	},
	{
		workTrackingSystemName: "Jira",
		workTrackingSystemOptions: [
			{ field: "Jira URL", value: "https://letpeoplework.atlassian.net" },
			{ field: "Username (Email)", value: "benj@letpeople.work" },
		],
	},
	{
		workTrackingSystemName: "Linear",
		workTrackingSystemOptions: [
			{ field: "API Key", value: "lin_api_demoplaceholderkey0000000000" },
		],
	},
	{
		workTrackingSystemName: "CSV",
		workTrackingSystemOptions: [
			{ field: "Delimiter", value: "," },
			{ field: "Date Time Format", value: "yyyy-MM-dd HH:mm:ss" },
			{ field: "Tag Separator", value: "|" },
			{ field: "ID Column", value: "Key" },
			{ field: "Name Column", value: "Summary" },
			{ field: "State Column", value: "State" },
			{ field: "Type Column", value: "Type" },
			{ field: "Started Date Column", value: "Started Date" },
			{ field: "Closed Date Column", value: "Closed Date" },
		],
	},
];

for (const {
	workTrackingSystemName,
	workTrackingSystemOptions,
} of workTrackingSystemConfiguration) {
	testWithDemo(
		`Take @screenshot of ${workTrackingSystemName} Work Tracking System Connection creation`,
		async ({ overviewPage }) => {
			const workTrackingSystemEditPage = await overviewPage.addConnection();

			await takePageScreenshot(
				workTrackingSystemEditPage.page,
				`concepts/worktrackingsystem_type_selection.png`,
				5,
				1000,
			);

			await workTrackingSystemEditPage.selectWorkTrackingSystemType(
				workTrackingSystemName,
			);

			for (const option of workTrackingSystemOptions) {
				await workTrackingSystemEditPage.setWorkTrackingSystemOption(
					option.field,
					option.value,
				);
			}

			await takePageScreenshot(
				workTrackingSystemEditPage.page,
				`concepts/worktrackingsystem_${workTrackingSystemName}.png`,
				5,
				1000,
			);
		},
	);
}

testWithDemo(
	"Take @screenshot of Work Item Aging chart with pace percentile bands",
	async ({ testData, page, overviewPage }) => {
		const teamDetailPage = await overviewPage.goToTeam(testData.teams[0].name);
		const metricsPage = await teamDetailPage.goToMetrics();

		const flowMetricsWidgets = await metricsPage.switchCategory(
			MetricsCategories.FlowMetrics,
		);
		const agingWidget = await metricsPage.getWidgetByName(
			MetricsWidgetNames.WorkItemAgingChart,
			flowMetricsWidgets,
		);
		await expect(agingWidget.Widget).toBeVisible();

		const agingChart = new WorkItemAgingChart(page, "aging");
		await agingChart.togglePacePercentiles();
		await expect.poll(() => agingChart.countPaceBands()).toBeGreaterThan(0);

		await takeElementScreenshot(
			agingWidget.Widget,
			"features/metrics/aging_pace_percentiles.png",
		);
	},
);

testWithDemo(
	"Take @screenshot of Wait States configuration and Flow Efficiency",
	async ({ page, testData, overviewPage }) => {
		await overviewPage.lightHousePage.goToOverview();

		const teamDetail = await overviewPage.goToTeam(testData.teams[0].name);
		const teamEdit = await teamDetail.editTeam();

		const waitStates = new WaitStatesEditor(page);
		await waitStates.enable();
		await waitStates.addWaitState("Verification");
		await takeElementScreenshot(
			waitStates.section,
			"features/metrics/waitStatesEditor.png",
		);

		const teamDetailAfterSave = await teamEdit.save();
		const metricsPage = await teamDetailAfterSave.goToMetrics();

		const overviewWidgets = await metricsPage.switchCategory(
			MetricsCategories.FlowOverview,
		);
		const tileWidget = await metricsPage.getWidgetByName(
			MetricsWidgetNames.FlowEfficiencyOverview,
			overviewWidgets,
		);
		await expect(tileWidget.Widget).toBeVisible();

		const tile = new FlowEfficiencyOverviewTile(page);
		await expect(tile.efficiencyValue).toContainText("%");
		await takeElementScreenshot(
			tileWidget.Widget,
			"features/metrics/flowEfficiencyConfigured.png",
		);

		const flowMetricsWidgets = await metricsPage.switchCategory(
			MetricsCategories.FlowMetrics,
		);
		const chartWidget = await metricsPage.getWidgetByName(
			MetricsWidgetNames.CumulativeStateTime,
			flowMetricsWidgets,
		);
		await expect(chartWidget.Widget).toBeVisible();

		const chartEfficiency = new CumulativeChartFlowEfficiency(
			page,
			"stateTimeCumulative",
		);
		await expect(chartEfficiency.efficiencyNumber).toContainText("%");
		await expect(chartEfficiency.waitColourKey).toContainText("Wait");
		await takeElementScreenshot(
			chartWidget.Widget,
			"features/metrics/stateTimeCumulativeWaitStates.png",
		);
	},
);

testWithDemo(
	"Take @screenshot of delivery over-time metrics charts",
	async ({ testData, overviewPage }) => {
		await overviewPage.lightHousePage.goToOverview();

		const portfolioDetailPage = await overviewPage.goToPortfolio(
			testData.portfolios[0].name,
		);
		const deliveryPage = await portfolioDetailPage.goToDeliveries();
		const delivery = deliveryPage.getDeliveryByName("Apollo Release");
		await delivery.toggleDetails();

		const metricsTab = new DeliveryMetricsTab(delivery);
		await metricsTab.openMetricsTab();

		await expect(metricsTab.burnupChart).toBeVisible();
		await expect
			.poll(() => metricsTab.countDrawnSeriesLines())
			.toBeGreaterThanOrEqual(1);
		await takeElementScreenshot(
			metricsTab.burnupChart,
			"features/deliveryBurnup.png",
		);

		await expect(metricsTab.predictabilityChart).toBeVisible();
		await metricsTab.showLikelihoodView();
		await expect(
			metricsTab.predictabilitySeriesLine("likelihood"),
		).toBeVisible();
		await takeElementScreenshot(
			metricsTab.predictabilityChart,
			"features/deliveryPredictabilityLikelihood.png",
		);

		await metricsTab.showWhenView();
		await expect(metricsTab.predictabilitySeriesLine("target")).toBeVisible();
		await takeElementScreenshot(
			metricsTab.predictabilityChart,
			"features/deliveryPredictabilityWhen.png",
		);

		await expect(metricsTab.feverChart).toBeVisible();
		await expect.poll(() => metricsTab.countFeverBubbles()).toBeGreaterThan(0);
		await takeElementScreenshot(
			metricsTab.feverChart,
			"features/deliveryFever.png",
		);
	},
);

testWithDemo(
	"Take @screenshot of Cumulative Time per State chart scoped to a filtered work item",
	async ({ testData, page, overviewPage }) => {
		const teamDetailPage = await overviewPage.goToTeam(testData.teams[0].name);
		const metricsPage = await teamDetailPage.goToMetrics();

		const flowMetricsWidgets = await metricsPage.switchCategory(
			MetricsCategories.FlowMetrics,
		);
		const cumulativeWidget = await metricsPage.getWidgetByName(
			MetricsWidgetNames.CumulativeStateTime,
			flowMetricsWidgets,
		);
		await expect(cumulativeWidget.Widget).toBeVisible();

		const cumulativeChart = new CumulativeStateTimeChart(
			page,
			"stateTimeCumulative",
		);
		await expect
			.poll(() => cumulativeChart.countStateBars())
			.toBeGreaterThan(0);

		await cumulativeChart.searchPicker("TZ-");
		await cumulativeChart.selectFirstPickerOption();
		await expect.poll(() => cumulativeChart.countSelectedPickerChips()).toBe(1);

		await takeElementScreenshot(
			cumulativeWidget.Widget,
			"features/metrics/stateTimeCumulative_filtered.png",
		);
	},
);
