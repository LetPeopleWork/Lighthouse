import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import type { APIRequestContext } from "@playwright/test";
import { TestConfig } from "../../../playwright.config";
import {
	expect,
	type ModelIdentifier,
	test,
	testWithData,
} from "../../fixutres/LighthouseFixture";
import { updateProject } from "../../helpers/api/projects";
import { updateTeam } from "../../helpers/api/teams";
import {
	takeDialogScreenshot,
	takeDialogScreenshot as takeElementScreenshot,
	takePageScreenshot,
} from "../../helpers/screenshots";
import type { OverviewPage } from "../../models/overview/OverviewPage";

const updateWorkTrackingSystems = async (
	overviewPage: OverviewPage,
	workTrackingSystems: ModelIdentifier[],
) => {
	const workTrackingSystemNames = [
		{
			name: "My Azure DevOps Connection",
			optionName: "Personal Access Token",
			optionValue: TestConfig.AzureDevOpsToken,
		},
		{
			name: "My Jira Connection",
			optionName: "Api Token",
			optionValue: TestConfig.JiraToken,
		},
	];

	const settingsPage = await overviewPage.lightHousePage.goToSettings();
	const workTrackingSystemsPage = await settingsPage.goToWorkTrackingSystems();

	for (const workTrackingSystem of workTrackingSystems) {
		const editWorkTrackingSystem =
			await workTrackingSystemsPage.modifyWorkTryckingSystem(
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

		await editTeam.validate();
		await expect(editTeam.saveButton).toBeEnabled();

		const teamDetailPage = await editTeam.save();

		updateTeam(api, team.id);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();
	}
};

const updateProjects = async (
	api: APIRequestContext,
	overviewPage: OverviewPage,
	projects: ModelIdentifier[],
) => {
	const projectNames = ["Lighthouse Project", "2025.01", "Project 1886"];

	for (const project of projects) {
		const projectPage = await overviewPage.lightHousePage.goToOverview();
		const editProject = await projectPage.editProject(project);

		const newProjectName = projectNames[projects.indexOf(project)];
		await editProject.setName(newProjectName);
		project.name = newProjectName;

		await editProject.validate();
		await expect(editProject.saveButton).toBeEnabled();

		const projectDetailPage = await editProject.save();

		updateTeam(api, project.id);

		await expect(projectDetailPage.refreshFeatureButton).toBeEnabled();
	}

	await updateProject(api, projects[0].id);
};

test("Take @screenshot of empty overview page", async ({ overviewPage }) => {
	await takePageScreenshot(overviewPage.page, "installation/landingpage.png");
});

testWithData(
	"Take @screenshot of import dialog",
	async ({ overviewPage, testData, request }) => {
		await updateWorkTrackingSystems(overviewPage, testData.connections);
		await updateTeams(request, overviewPage, testData.teams);
		await updateProjects(request, overviewPage, testData.projects);

		const settingsPage = await overviewPage.lightHousePage.goToSettings();
		const systemSettings = await settingsPage.goToSystemSettings();

		const exportedConfigFileName = await systemSettings.exportConfiguration();

		const tempDir = os.tmpdir();
		const invalidJsonPath = path.join(
			tempDir,
			`invalid-import-${Date.now()}.json`,
		);
		fs.writeFileSync(
			invalidJsonPath,
			`
			Lorem ipsum dolor sit amet, consectetur adipiscing elit.
			This is definitely not valid JSON!
			{
				"unclosed": "object"
				"missing": "comma"
			}
		`,
		);

		let importDialog = await systemSettings.importConfiguration();
		await importDialog.selectFile(invalidJsonPath);

		takeDialogScreenshot(
			importDialog.page.getByRole("dialog"),
			"settings/import/invalid_file.png",
			0.5,
			1000,
		);

		importDialog.close();

		importDialog = await systemSettings.importConfiguration();
		await importDialog.selectFile(exportedConfigFileName);

		await takeDialogScreenshot(
			importDialog.page.getByRole("dialog"),
			"settings/import/update.png",
			0.5,
			1000,
		);

		await importDialog.toggleClearConfiguration();
		await takeDialogScreenshot(
			importDialog.page.getByRole("dialog"),
			"settings/import/new.png",
			0.5,
			1000,
		);

		await importDialog.goToNextStep();
		await importDialog.addSecretParameter(
			"Personal Access Token",
			TestConfig.AzureDevOpsToken,
		);
		await importDialog.addSecretParameter("Api Token", TestConfig.JiraToken);

		await takeDialogScreenshot(
			importDialog.page.getByRole("dialog"),
			"settings/import/secret_parameters.png",
			0.5,
			1000,
		);

		await importDialog.validate();
		await expect(importDialog.nextButton).toBeEnabled();
		await importDialog.goToNextStep();

		await importDialog.import();

		await takeDialogScreenshot(
			importDialog.page.getByRole("dialog"),
			"settings/import/importing.png",
			0.5,
			1000,
		);

		await importDialog.waitForImportToFinish();

		await takeDialogScreenshot(
			importDialog.page.getByRole("dialog"),
			"settings/import/summary.png",
			0.5,
			1000,
		);
	},
);

test("Take @screenshot of licensing", async ({ overviewPage }) => {
	await overviewPage.showLicenseTooltip();

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

	const licensingPopover = await overviewPage.showLicensingInformation();

	await takeElementScreenshot(
		licensingPopover,
		"licensing/licenseinformation.png",
	);
});

test("Take @screenshot of setting pages", async ({ overviewPage }) => {
	const settingsPage = await overviewPage.lightHousePage.goToSettings();
	const workTrackingSystemsPage = await settingsPage.goToWorkTrackingSystems();

	// Add new Work Tracking System
	const workTrackingSystemDialog =
		await workTrackingSystemsPage.addNewWorkTrackingSystem();

	const jiraUrl = "https://letpeoplework.atlassian.net";
	const username = "atlassian.pushchair@huser-berta.com";
	const wtsName = "My Jira Connection";

	await workTrackingSystemDialog.selectWorkTrackingSystem("Jira");
	await workTrackingSystemDialog.setWorkTrackingSystemOption(
		"Jira Url",
		jiraUrl,
	);
	await workTrackingSystemDialog.setWorkTrackingSystemOption(
		"Username",
		username,
	);
	await workTrackingSystemDialog.setWorkTrackingSystemOption(
		"Api Token",
		TestConfig.JiraToken,
	);

	await workTrackingSystemDialog.setConnectionName(wtsName);

	await workTrackingSystemDialog.validate();
	await expect(workTrackingSystemDialog.validateButton).toBeEnabled();
	await expect(workTrackingSystemDialog.createButton).toBeEnabled();
	await workTrackingSystemDialog.create();

	const savedWorkTrackingSystem =
		workTrackingSystemsPage.getWorkTrackingSystem(wtsName);
	await expect(savedWorkTrackingSystem).toBeVisible();

	await takePageScreenshot(
		workTrackingSystemsPage.page,
		"settings/worktrackingsystems.png",
	);

	const defaultTeamSettingsPage = await settingsPage.goToDefaultTeamSettings();
	await takePageScreenshot(
		defaultTeamSettingsPage.page,
		"settings/defaultteamsettings.png",
	);

	const defaultprojectSettingsPage =
		await settingsPage.goToDefaultProjectSettings();
	await takePageScreenshot(
		defaultprojectSettingsPage.page,
		"settings/defaultprojectsettings.png",
	);

	const systemSettings = await settingsPage.goToSystemSettings();

	await takePageScreenshot(systemSettings.page, "settings/systemsettings.png");

	await takeElementScreenshot(
		systemSettings.lighthouseConfiguration,
		"settings/lighthouseConfiguration.png",
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

	const logs = await settingsPage.goToLogs();
	await takePageScreenshot(logs.page, "settings/logs.png");
});

testWithData(
	"Take @screenshot of populated overview, teams overview, team detail, projects overview, and project detail pages",
	async ({ testData, overviewPage, request }) => {
		await updateTeams(request, overviewPage, testData.teams);
		await updateProjects(request, overviewPage, testData.projects);

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
		await teamDetailPage.forecast(10);
		await takePageScreenshot(teamDetailPage.page, "features/teamdetail.png", 3);

		await teamDetailPage.forecastNewWorkItems(["Bug"]);
		await takeElementScreenshot(
			teamDetailPage.page.getByText(
				"New Work Items Creation ForecastHistorical DataHistorical data that should be",
			),
			"features/creationforecast.png",
		);

		// Go to Metrics Tab
		await teamDetailPage.goToMetrics();

		await takeElementScreenshot(
			teamDetailPage.workItemsInProgressWidget,
			"features/metrics/workitemsinprogress.png",
		);

		const workItemsInProgressDialog =
			await teamDetailPage.openWorkItemsInProgressDialog();
		await takeElementScreenshot(
			workItemsInProgressDialog.page.getByRole("dialog"),
			"features/metrics/workitemsinprogress_dialog.png",
		);

		await workItemsInProgressDialog.close();

		await takeElementScreenshot(
			teamDetailPage.cycleTimePercentileWidget,
			"features/metrics/cycletimepercentiles.png",
		);

		await teamDetailPage.openSleWidget();
		await takeElementScreenshot(
			teamDetailPage.sleWidget,
			"features/metrics/servicelevelexpectation.png",
		);

		await teamDetailPage.closeSleWidget();

		const closedItemDialog = await teamDetailPage.openClosedItemsDialog();
		await takeElementScreenshot(
			closedItemDialog.page.getByRole("dialog"),
			"features/metrics/closeditemsdialog.png",
		);
		await closedItemDialog.close();

		await takeElementScreenshot(
			teamDetailPage.startedVsClosedWidget,
			"features/metrics/startedVsClosed.png",
		);

		await takeElementScreenshot(
			teamDetailPage.throughputRunChartWidget,
			"features/metrics/throughputRunChart.png",
		);

		await teamDetailPage.openPredictabilityScoreWidget();
		await takeElementScreenshot(
			teamDetailPage.predictabilityScoreChartWidget,
			"features/metrics/predictabilityscore.png",
		);

		await takeElementScreenshot(
			teamDetailPage.cycleTimeScatterplotWidget,
			"features/metrics/cycleTimeScatterplot.png",
		);

		await takeElementScreenshot(
			teamDetailPage.wipOverTimeWidget,
			"features/metrics/wipOverTime.png",
		);

		await takeElementScreenshot(
			teamDetailPage.workItemAgingChart,
			"features/metrics/workItemAgingChart.png",
		);

		await takeElementScreenshot(
			teamDetailPage.simplifiedCfdWidget,
			"features/metrics/simplifiedCFD.png",
		);

		overviewPage.lightHousePage.goToOverview();

		// Project Deletion Dialog
		const deleteProjectDialog = await overviewPage.deleteProject(
			testData.projects[0],
		);

		await takeElementScreenshot(
			deleteProjectDialog.page.getByRole("dialog"),
			"features/projects_delete.png",
			0.5,
			1000,
		);
		await deleteProjectDialog.cancel();

		// Project Detail Page
		const projectDetailPage = await overviewPage.goToProject(
			testData.projects[0],
		);
		await takePageScreenshot(
			projectDetailPage.page,
			"features/projectdetail.png",
			3,
		);

		// Expand Milestones
		await projectDetailPage.toggleMilestoneConfiguration();
		const inTwoWeeks = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000);
		await projectDetailPage.addMilestone("SB26 Milestone", inTwoWeeks);
		await expect(projectDetailPage.refreshFeatureButton).toBeEnabled();

		await takePageScreenshot(
			projectDetailPage.page,
			"features/projectdetail_milestones.png",
			5,
			1000,
		);

		// Involved Teams
		await projectDetailPage.toggleMilestoneConfiguration();
		await projectDetailPage.toggleFeatureWIPConfiguration();

		await projectDetailPage.changeFeatureWIPForTeam(testData.teams[0].name, 3);

		await takePageScreenshot(
			projectDetailPage.page,
			"features/projectdetail_team_feature_wip.png",
			5,
			1000,
		);

		await projectDetailPage.goToMetrics();

		await takeElementScreenshot(
			projectDetailPage.featureSizeWidget,
			"features/metrics/featuresize.png",
		);
	},
);

const workTrackingSystemConfiguration = [
	{
		workTrackingSystemName: "AzureDevOps",
		workTrackingSystemOptions: [
			{
				field: "Azure DevOps Url",
				value: "https://dev.azure.com/letpeoplework",
			},
		],
	},
	{
		workTrackingSystemName: "Jira",
		workTrackingSystemOptions: [
			{ field: "Jira Url", value: "https://letpeoplework.atlassian.net" },
			{ field: "Username", value: "benj@letpeople.work" },
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
			{ field: "Created Date Column", value: "Created Date" },
			{ field: "Parent Reference Id Column", value: "Parent Key" },
			{ field: "Tags Column", value: "Labels" },
			{ field: "Url Column", value: "Url" },
			{ field: "Owning Team Column", value: "Owning Team" },
			{ field: "Estimated Size Column", value: "Estimated Size" },
		],
	},
];

for (const {
	workTrackingSystemName,
	workTrackingSystemOptions,
} of workTrackingSystemConfiguration) {
	testWithData(
		`Take @screenshot of ${workTrackingSystemName} Work Tracking System Connection creation`,
		async ({ testData, overviewPage }) => {
			test.fail(
				testData.projects.length < 1,
				"Expected to have projects initiatilized to prevent tutorial page from being displayed",
			);
			const settingsPage = await overviewPage.lighthousePage.goToSettings();

			const workTrackingSystemsPage =
				await settingsPage.goToWorkTrackingSystems();

			const workTrackingSystemDialog =
				await workTrackingSystemsPage.addNewWorkTrackingSystem();

			// Wait for the dialog to be visible
			await workTrackingSystemDialog.setConnectionName(
				`My ${workTrackingSystemName} Connection`,
			);

			await workTrackingSystemDialog.selectWorkTrackingSystem(
				workTrackingSystemName,
			);

			for (const option of workTrackingSystemOptions) {
				await workTrackingSystemDialog.setWorkTrackingSystemOption(
					option.field,
					option.value,
				);
			}

			await workTrackingSystemDialog.scrollToTop();

			await takeElementScreenshot(
				workTrackingSystemDialog.page.getByRole("dialog"),
				`concepts/worktrackingsystem_${workTrackingSystemName}.png`,
				5,
				1000,
			);
		},
	);
}
