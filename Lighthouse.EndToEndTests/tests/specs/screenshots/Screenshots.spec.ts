import {
	expect,
	test,
	testWithData,
	testWithUpdatedTeams,
} from "../../fixutres/LighthouseFixture";
import { getPathToDocsAssetsFolder } from "../../helpers/folderPaths";

test("Taks @screenshot of empty overview page", async ({ overviewPage }) => {
	const screenshotLocation = `${getPathToDocsAssetsFolder()}/installation/landingpage.png`;

	await overviewPage.page.waitForTimeout(300);

	await overviewPage.page.screenshot({ path: screenshotLocation });
});

testWithData(
	"Taks @screenshot of populated overview page",
	async ({ testData, overviewPage }) => {
		const projectNames = ["MadHdP", "2025.01", "Project 1886"];
		const teamNames = ["The A-Team", "SB26", "The Neururers"];

		// Refresh Teams to take screenshot
		for (const team of testData.teams) {
			const teamPage = await overviewPage.lightHousePage.goToTeams();
			const editTeam = await teamPage.editTeam(team.name);
			await editTeam.setName(teamNames[testData.teams.indexOf(team)]);

			await editTeam.validate();
			await expect(editTeam.saveButton).toBeEnabled();

			const teamDetailPage = await editTeam.save();

			await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();
		}

		// Refresh Projects to take screenshot
		for (const project of testData.projects) {
			const projectPage = await overviewPage.lightHousePage.goToProjects();
			const editProject = await projectPage.editProject(project);
			await editProject.setName(
				projectNames[testData.projects.indexOf(project)],
			);

			await editProject.validate();
			await expect(editProject.saveButton).toBeEnabled();

			const projectDetailPage = await editProject.save();

			await expect(projectDetailPage.refreshFeatureButton).toBeEnabled();
		}

		const landingPage = await overviewPage.lightHousePage.goToOverview();
		const screenshotLocation = `${getPathToDocsAssetsFolder()}/features/overview.png`;

		await landingPage.page.waitForTimeout(300);

		await landingPage.page.screenshot({ path: screenshotLocation });
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
