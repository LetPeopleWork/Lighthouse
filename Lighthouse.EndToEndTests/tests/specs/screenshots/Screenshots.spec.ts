import { TestConfig } from "../../../playwright.config";
import {
	type ModelIdentifier,
	expect,
	test,
	testWithData,
} from "../../fixutres/LighthouseFixture";

import {
	takeDialogScreenshot,
	takePageScreenshot,
} from "../../helpers/screenshots";
import type { OverviewPage } from "../../models/overview/OverviewPage";

const updateTeams = async (
	overviewPage: OverviewPage,
	teams: ModelIdentifier[],
) => {
	const teamNames = ["The A-Team", "Dawg Pound", "Team HecKING"];

	for (const team of teams) {
		const teamPage = await overviewPage.lightHousePage.goToTeams();
		const editTeam = await teamPage.editTeam(team.name);

		const newTeamName = teamNames[teams.indexOf(team)];
		await editTeam.setName(newTeamName);
		team.name = newTeamName;

		await editTeam.validate();
		await expect(editTeam.saveButton).toBeEnabled();

		const teamDetailPage = await editTeam.save();

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();
	}
};

const updateProjects = async (
	overviewPage: OverviewPage,
	projects: ModelIdentifier[],
) => {
	const projectNames = ["MadHdP", "2025.01", "Project 1886"];

	for (const project of projects) {
		const projectPage = await overviewPage.lightHousePage.goToProjects();
		const editProject = await projectPage.editProject(project);

		const newProjectName = projectNames[projects.indexOf(project)];
		await editProject.setName(newProjectName);
		project.name = newProjectName;

		await editProject.validate();
		await expect(editProject.saveButton).toBeEnabled();

		const projectDetailPage = await editProject.save();

		await expect(projectDetailPage.refreshFeatureButton).toBeEnabled();
	}
};

test("Take @screenshot of empty overview page", async ({ overviewPage }) => {
	await takePageScreenshot(overviewPage.page, "installation/landingpage.png");
});

test("Take @screenshot of setting pages", async ({ overviewPage }) => {
	const settingsPage = await overviewPage.lightHousePage.goToSettings();
	const workTrackingSystemsPage = await settingsPage.goToWorkTrackingSystems();

	// Add new Work Tracking System
	const workTrackingSystemDialog =
		await workTrackingSystemsPage.addNewWorkTrackingSystem();

	const jiraUrl = "https://letpeoplework.atlassian.net";
	const username = "benjhuser@gmail.com";
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

	const periodicRefreshSettings =
		await settingsPage.goToPeriodicRefreshSettings();
	await takePageScreenshot(
		periodicRefreshSettings.page,
		"settings/periodicrefreshsettings.png",
	);

	const dataRetentionSettings = await settingsPage.goToDataRetentionSettings();
	await takePageScreenshot(
		dataRetentionSettings.page,
		"settings/dataretention.png",
	);

	const previewFeatureSettings = await settingsPage.goToPreviewFeatures();
	await takePageScreenshot(
		previewFeatureSettings.page,
		"settings/previewfeatures.png",
	);

	const logs = await settingsPage.goToLogs();
	await takePageScreenshot(logs.page, "settings/logs.png");
});

testWithData(
	"Take @screenshot of populated overview, teams overview, team detail, projects overview, and project detail pages",
	async ({ testData, overviewPage }) => {
		await updateTeams(overviewPage, testData.teams);
		await updateProjects(overviewPage, testData.projects);

		// Overview Page
		const landingPage = await overviewPage.lightHousePage.goToOverview();
		await takePageScreenshot(landingPage.page, "features/overview.png");

		// Teams Overview Page
		const teamsPage = await overviewPage.lightHousePage.goToTeams();
		await takePageScreenshot(teamsPage.page, "features/teams.png");

		// Team Deletion Dialog
		const deleteTeamDialog = await teamsPage.deleteTeam(testData.teams[0].name);
		await takeDialogScreenshot(
			deleteTeamDialog.page.getByRole("dialog"),
			"features/teams_delete.png",
			0.5,
			1000,
		);

		await deleteTeamDialog.cancel();

		// Team Detail Page
		const teamDetailPage = await teamsPage.goToTeam(testData.teams[2].name);
		await teamDetailPage.forecast(10);
		await takePageScreenshot(teamDetailPage.page, "features/teamdetail.png", 3);

		// Go to Metrics Tab
		await teamDetailPage.goToMetrics();

		await takePageScreenshot(
			teamDetailPage.page,
			"features/teamdetail_metrics.png",
			15,
		);

		// Project Overview Page
		const projectsPage = await overviewPage.lightHousePage.goToProjects();
		await takePageScreenshot(projectsPage.page, "features/projects.png");

		// Team Deletion Dialog
		const deleteProjectDialog = await projectsPage.deleteProject(
			testData.projects[0],
		);

		await takeDialogScreenshot(
			deleteProjectDialog.page.getByRole("dialog"),
			"features/projects_delete.png",
			0.5,
			1000,
		);
		await deleteProjectDialog.cancel();

		// Project Detail Page
		const projectDetailPage = await projectsPage.goToProject(
			testData.projects[1],
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

			await takeDialogScreenshot(
				workTrackingSystemDialog.page.getByRole("dialog"),
				`concepts/worktrackingsystem_${workTrackingSystemName}.png`,
				5,
				1000,
			);
		},
	);
}
