import {
	type ModelIdentifier,
	expect,
	test,
	testWithData,
} from "../../fixutres/LighthouseFixture";

import { getPathToDocsAssetsFolder } from "../../helpers/folderPaths";
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

test("Taks @screenshot of empty overview page", async ({ overviewPage }) => {
	const screenshotLocation = `${getPathToDocsAssetsFolder()}/installation/landingpage.png`;

	await overviewPage.page.waitForTimeout(300);

	await overviewPage.page.screenshot({ path: screenshotLocation });
});

testWithData(
	"Take @screenshots of populated overview, teams overview, team detail, projects overview, and project detail pages",
	async ({ testData, overviewPage }) => {
		await updateTeams(overviewPage, testData.teams);
		await updateProjects(overviewPage, testData.projects);

		// Overview Page
		const landingPage = await overviewPage.lightHousePage.goToOverview();
		let screenshotLocation = `${getPathToDocsAssetsFolder()}/features/overview.png`;

		await landingPage.page.waitForTimeout(300);
		await landingPage.page.screenshot({ path: screenshotLocation });

		// Teams Overview Page
		const teamsPage = await overviewPage.lightHousePage.goToTeams();
		screenshotLocation = `${getPathToDocsAssetsFolder()}/features/teams.png`;

		await teamsPage.page.waitForTimeout(300);
		await teamsPage.page.screenshot({ path: screenshotLocation });

		// Team Deletion Dialog
		const deleteTeamDialog = await teamsPage.deleteTeam(testData.teams[0].name);
		screenshotLocation = `${getPathToDocsAssetsFolder()}/features/teams_delete.png`;

		await deleteTeamDialog.page.waitForTimeout(300);

		await deleteTeamDialog.page
			.getByRole("dialog")
			.screenshot({ path: screenshotLocation });

		await deleteTeamDialog.cancel();

		// Team Detail Page
		const teamDetailPage = await teamsPage.goToTeam(testData.teams[2].name);
		await teamDetailPage.forecast(10);
		screenshotLocation = `${getPathToDocsAssetsFolder()}/features/teamdetail.png`;

		await teamDetailPage.page.waitForTimeout(300);
		await teamDetailPage.page.screenshot({ path: screenshotLocation });

		// Collapse Features and Forecast, and show Throughput
		await teamDetailPage.toggleFeatures();
		await teamDetailPage.toggleForecast();
		await teamDetailPage.toggleThroughput();

		await teamDetailPage.page.waitForTimeout(300);
		screenshotLocation = `${getPathToDocsAssetsFolder()}/features/teamdetail_throughput.png`;
		await teamDetailPage.page.screenshot({ path: screenshotLocation });

		// Project Overview Page
		const projectsPage = await overviewPage.lightHousePage.goToProjects();
		screenshotLocation = `${getPathToDocsAssetsFolder()}/features/projects.png`;

		await projectsPage.page.waitForTimeout(300);
		await projectsPage.page.screenshot({ path: screenshotLocation });

		// Project Detail Page
		const projectDetailPage = await projectsPage.goToProject(
			testData.projects[1],
		);
		screenshotLocation = `${getPathToDocsAssetsFolder()}/features/projectdetail.png`;

		await projectDetailPage.page.waitForTimeout(300);
		await projectDetailPage.page.screenshot({ path: screenshotLocation });

		// Expand Milestones
		await projectDetailPage.toggleMilestoneConfiguration();
		const inTwoWeeks = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000);
		await projectDetailPage.addMilestone("SB26 Milestone", inTwoWeeks);
		await expect(projectDetailPage.refreshFeatureButton).toBeEnabled();

		await projectDetailPage.page.waitForTimeout(1000);
		screenshotLocation = `${getPathToDocsAssetsFolder()}/features/projectdetail_milestones.png`;
		await projectDetailPage.page.screenshot({ path: screenshotLocation });

		// Involved Teams
		await projectDetailPage.toggleMilestoneConfiguration();
		await projectDetailPage.toggleFeatureWIPConfiguration();

		await projectDetailPage.changeFeatureWIPForTeam(testData.teams[0].name, 3);
		
		await projectDetailPage.page.waitForTimeout(1000);
		screenshotLocation = `${getPathToDocsAssetsFolder()}/features/projectdetail_team_feature_wip.png`;
		await projectDetailPage.page.screenshot({ path: screenshotLocation });
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

			const screenshotLocation = `${getPathToDocsAssetsFolder()}/concepts/worktrackingsystem_${workTrackingSystemName}.png`;

			await workTrackingSystemDialog.page.waitForTimeout(300);

			await workTrackingSystemDialog.page
				.getByRole("dialog")
				.screenshot({ path: screenshotLocation });
		},
	);
}
