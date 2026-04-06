import type { APIRequestContext } from "@playwright/test";
import { TestConfig } from "../../../playwright.config";
import {
	expect,
	type ModelIdentifier,
	test,
	testWithData,
} from "../../fixutres/LighthouseFixture";
import { updatePortfolio } from "../../helpers/api/portfolios";
import { updateTeam } from "../../helpers/api/teams";
import {
	takeDialogScreenshot,
	takeDialogScreenshot as takeElementScreenshot,
	takePageScreenshot,
} from "../../helpers/screenshots";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";
import type { OverviewPage } from "../../models/overview/OverviewPage";

const updateWorkTrackingSystems = async (
	overviewPage: OverviewPage,
	workTrackingSystems: ModelIdentifier[],
) => {
	// Force reload main page to ensure we have the latest data after API updates
	await overviewPage.lightHousePage.goToSettings();
	overviewPage = await overviewPage.lightHousePage.goToOverview();

	const workTrackingSystemNames = [
		{
			name: "My Azure DevOps Connection",
			optionName: "Personal Access Token",
			optionValue: TestConfig.AzureDevOpsToken,
		},
		{
			name: "My Jira Connection",
			optionName: "API Token",
			optionValue: TestConfig.JiraToken,
		},
	];

	for (const workTrackingSystem of workTrackingSystems) {
		const editWorkTrackingSystem = await overviewPage.editConnection(
			workTrackingSystem.name,
		);

		const wtsDetails =
			workTrackingSystemNames[workTrackingSystems.indexOf(workTrackingSystem)];
		await editWorkTrackingSystem.setConnectionName(wtsDetails.name);
		await editWorkTrackingSystem.setWorkTrackingSystemOption(
			wtsDetails.optionName,
			wtsDetails.optionValue,
		);

		await editWorkTrackingSystem.validate();
		await expect(editWorkTrackingSystem.createButton).toBeEnabled();
		await editWorkTrackingSystem.create();
	}
};

const updateTeams = async (
	api: APIRequestContext,
	overviewPage: OverviewPage,
	teams: ModelIdentifier[],
) => {
	const teamNames = ["Lighthouse Team", "Dawg Pound", "Team HecKING"];

	for (const team of teams) {
		const teamPage = await overviewPage.lightHousePage.goToOverview();
		const editTeam = await teamPage.editTeam(team.name);

		const newTeamName = teamNames[teams.indexOf(team)];
		await editTeam.setName(newTeamName);
		team.name = newTeamName;

		await expect(editTeam.saveButton).toBeEnabled();

		const teamDetailPage = await editTeam.save();

		updateTeam(api, team.id);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();
	}
};

const updatePortfolios = async (
	api: APIRequestContext,
	overviewPage: OverviewPage,
	portfolios: ModelIdentifier[],
) => {
	const portfolioNames = ["Lighthouse Project", "2025.01", "Project 1886"];

	for (const portfolio of portfolios) {
		const portfolioPage = await overviewPage.lightHousePage.goToOverview();
		const editPortfolioPage = await portfolioPage.editPortfolio(portfolio);

		const newPortfolioname = portfolioNames[portfolios.indexOf(portfolio)];
		await editPortfolioPage.setName(newPortfolioname);
		portfolio.name = newPortfolioname;

		await expect(editPortfolioPage.saveButton).toBeEnabled();

		const portfolioDetailPage = await editPortfolioPage.save();

		updateTeam(api, portfolio.id);

		await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled();
	}

	await updatePortfolio(api, portfolios[0].id);
};

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

	let systemSettings = await settingsPage.goToSystemConfiguration();

	await takePageScreenshot(systemSettings.page, "settings/configuration.png");

	const startDate = new Date(Date.now()).toISOString().slice(0, 10);
	const endDate = new Date(Date.now() + 24 * 60 * 60 * 1000)
		.toISOString()
		.slice(0, 10);

	const blackoutPeriodDialog = await systemSettings.addBlackoutPeriod();
	blackoutPeriodDialog.addBlackoutPeriod(
		"GCZ Meisterfeier",
		startDate,
		endDate,
	);

	await takeDialogScreenshot(
		blackoutPeriodDialog.page.getByRole("dialog"),
		"settings/blackoutPeriodConfiguration.png",
	);

	systemSettings = await blackoutPeriodDialog.saveBlackoutPeriod();
	await takeElementScreenshot(
		systemSettings.blackoutPeriodsSection,
		"settings/blackoutPeriodsSection.png",
	);

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

	await takeElementScreenshot(
		systemSettings.optionalFeatures,
		"settings/optionalfeatures.png",
	);

	const logs = await settingsPage.goToSystemInfo();
	await takePageScreenshot(logs.page, "settings/systeminfo.png");

	const databaseManagement = await settingsPage.goToDatabaseManagement();
	await takePageScreenshot(
		databaseManagement.page,
		"settings/databasemanagement.png",
	);
});

testWithData(
	"Take @screenshot of populated overview, teams overview, team detail, portfolios overview, and portfolio detail pages",
	async ({ testData, overviewPage, request }) => {
		await updateTeams(request, overviewPage, testData.teams);
		await updatePortfolios(request, overviewPage, testData.portfolios);

		// Team Deletion Dialog
		const teamsPage = await overviewPage.lightHousePage.goToOverview();
		const deleteTeamDialog = await teamsPage.deleteTeam(testData.teams[0].name);
		await takeElementScreenshot(
			deleteTeamDialog.page.getByRole("dialog"),
			"features/teams_delete.png",
			0.5,
			1000,
		);

		await deleteTeamDialog.cancel();

		// Team Detail Page
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
testWithData(
	"Take @screenshot of Metrics",
	async ({ testData, overviewPage, request }) => {
		await updateTeams(request, overviewPage, testData.teams);
		await updatePortfolios(request, overviewPage, testData.portfolios);

		await overviewPage.lightHousePage.goToOverview();

		// Go to Metrics Tab
		const teamDetailPage = await overviewPage.goToTeam(testData.teams[0].name);
		const metricsPage = await teamDetailPage.goToMetrics();

		// Metrics Overview
		await takePageScreenshot(
			metricsPage.page,
			"features/metrics/metricsoverview.png",
		);

		const metricCategoriesMap = new Map<MetricsCategories, number>([
			[MetricsCategories.FlowOverview, 7],
			[MetricsCategories.CycleTime, 5],
			[MetricsCategories.Throughput, 5],
			[MetricsCategories.WipAging, 7],
			[MetricsCategories.Predictability, 6],
			[MetricsCategories.PortfolioAndFeatures, 4],
		]);

		for (const [
			category,
			expectedWidgetCount,
		] of metricCategoriesMap.entries()) {
			const metrics = await metricsPage.switchCategory(category);
			expect(metrics.length).toBe(expectedWidgetCount);

			for (const metricWidget of metrics) {
				await takeElementScreenshot(
					metricWidget.Widget,
					`features/metrics/${metricWidget.Id}.png`,
				);
			}
		}

		await metricsPage.switchCategory(MetricsCategories.FlowOverview);

		const cycleTimePercentilesWidget = await metricsPage.getWidgetByName(
			MetricsWidgetNames.CycleTimePercentiles,
		);

		const workItemsDialog = await cycleTimePercentilesWidget.openDialog();
		await takeElementScreenshot(
			workItemsDialog.page.getByRole("dialog"),
			"features/metrics/workitemsdialog.png",
		);
		await workItemsDialog.close();

		overviewPage.lightHousePage.goToOverview();

		const portfolioDetailPage = await overviewPage.goToPortfolio(
			testData.portfolios[0].name,
		);
		const portfolioMetricsPage = await portfolioDetailPage.goToMetrics();

		await portfolioMetricsPage.switchCategory(
			MetricsCategories.PortfolioAndFeatures,
		);

		const featureSizeWidget = await portfolioMetricsPage.getWidgetByName(
			MetricsWidgetNames.FeatureSize,
		);

		await takeElementScreenshot(
			featureSizeWidget.Widget,
			"features/metrics/featuresize.png",
		);

		await portfolioMetricsPage.switchCategory(MetricsCategories.Predictability);

		const featureSizeProcessBehaviourWidget =
			await portfolioMetricsPage.getWidgetByName(
				MetricsWidgetNames.CycleTimeProcessBehaviourChart,
			);

		await takeElementScreenshot(
			featureSizeProcessBehaviourWidget.Widget,
			"features/metrics/featureSizeProcessBehaviourChart.png",
		);
	},
);
testWithData(
	"Take @screenshot of Estimation vs Cycle Time Chart",
	async ({ overviewPage, testData, request }) => {
		await updateWorkTrackingSystems(overviewPage, testData.connections);

		await overviewPage.lightHousePage.goToOverview();

		await updateTeams(request, overviewPage, testData.teams);

		await overviewPage.lightHousePage.goToOverview();

		const team = testData.teams[2];
		const teamEditPage = await overviewPage.editTeam(team.name);

		await teamEditPage.setEstimationField("Story Points");
		await expect(teamEditPage.saveButton).toBeEnabled();
		const teamDetailPage = await teamEditPage.save();

		const teamMetricsPage = await teamDetailPage.goToMetrics();

		const estimationVsCycleTimeWidget = await teamMetricsPage.getWidgetByName(
			MetricsWidgetNames.EstimationVsCycleTime,
		);

		await takeElementScreenshot(
			estimationVsCycleTimeWidget.Widget,
			"features/metrics/estimationVsCycleTime.png",
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
			{ field: "API Key", value: TestConfig.LinearApiKey },
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

const teamWizardScreenshotConfigs = [
	{
		name: "Jira",
		boardName: "Stories",
		teamIndex: 2,
	},
	{
		name: "Azure DevOps",
		boardName: "Lighthouse - Stories",
		teamIndex: 0,
	},
	{
		name: "Linear",
		boardName: "LighthouseDemo",
		teamIndex: 1,
	},
];

const portfolioWizardScreenshotConfigs = [
	{
		name: "Jira",
		displayName: "Jira",
		boardName: "Epics",
		teamIndex: 2,
	},
	{
		name: "Azure DevOps",
		displayName: "Azure DevOps",
		boardName: "Lighthouse - Epics",
		teamIndex: 0,
	},
];

for (const wizardConfig of teamWizardScreenshotConfigs) {
	testWithData(
		`Take @screenshot of ${wizardConfig.name} Team Wizard`,
		async ({ testData, overviewPage }) => {
			const team = testData.teams[wizardConfig.teamIndex];
			const teamEditPage = await overviewPage.editTeam(team.name);

			const wizard = await teamEditPage.selectWizard(wizardConfig.name);

			await wizard.selectByName(wizardConfig.boardName);

			await expect(wizard.boardInformationPanel).toBeVisible();

			await takeDialogScreenshot(
				wizard.page.getByRole("dialog"),
				`concepts/${wizardConfig.name.replace(" ", "").toLowerCase()}_team_wizard.png`,
				5,
				1000,
			);
		},
	);
}

for (const wizardConfig of portfolioWizardScreenshotConfigs) {
	testWithData(
		`Take @screenshot of ${wizardConfig.name} Portfolio Wizard`,
		async ({ testData, overviewPage }) => {
			const portfolio = testData.portfolios[0];
			const portfolioEditPage = await overviewPage.editPortfolio(portfolio);

			const boardWizard = await portfolioEditPage.selectWizard(
				wizardConfig.displayName,
			);

			await boardWizard.selectByName(wizardConfig.boardName);

			await expect(boardWizard.boardInformationPanel).toBeVisible();

			await takeDialogScreenshot(
				boardWizard.page.getByRole("dialog"),
				`concepts/${wizardConfig.name.replace(" ", "").toLowerCase()}_portfolio_wizard.png`,
				5,
				1000,
			);
		},
	);
}

for (const {
	workTrackingSystemName,
	workTrackingSystemOptions,
} of workTrackingSystemConfiguration) {
	testWithData(
		`Take @screenshot of ${workTrackingSystemName} Work Tracking System Connection creation`,
		async ({ testData, overviewPage }) => {
			test.fail(
				testData.portfolios.length < 1,
				"Expected to have portfolios initiatilized to prevent tutorial page from being displayed",
			);
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
