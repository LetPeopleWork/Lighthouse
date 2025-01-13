import { TestConfig } from "../../../playwright.config";
import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";
import { getPathToDocsAssetsFolder } from "../../helpers/folderPaths";
import { generateRandomName } from "../../helpers/names";

const workTrackingSystemConfiguration = [
	{
		workTrackingSystemName: "AzureDevOps",
		workTrackingSystemOptions: [
			{
				field: "Azure DevOps Url",
				value: "https://dev.azure.com/letpeoplework",
			},
			{ field: "Personal Access Token", value: TestConfig.AzureDevOpsToken },
		],
	},
	{
		workTrackingSystemName: "Jira",
		workTrackingSystemOptions: [
			{ field: "Jira Url", value: "https://letpeoplework.atlassian.net" },
			{ field: "Username", value: "benjhuser@gmail.com" },
			{ field: "Api Token", value: TestConfig.JiraToken },
		],
	},
];

for (const {
	workTrackingSystemName,
	workTrackingSystemOptions,
} of workTrackingSystemConfiguration) {
	testWithData(
		`Should add new ${workTrackingSystemName} Work Tracking System and make it available in Team and Project creation`,
		async ({ testData, overviewPage }) => {
			test.fail(
				testData.projects.length < 1,
				"Expected to have projects initiatilized to prevent tutorial page from being displayed",
			);

			let settingsPage = await overviewPage.lighthousePage.goToSettings();
			let workTrackingSystemsPage =
				await settingsPage.goToWorkTrackingSystems();

			const workTrackingSystemDialog =
				await workTrackingSystemsPage.addNewWorkTrackingSystem();

			await test.step("Select Work Tracking System", async () => {
				await workTrackingSystemDialog.selectWorkTrackingSystem(
					workTrackingSystemName,
				);

				await expect(workTrackingSystemDialog.validateButton).toBeDisabled();
				await expect(workTrackingSystemDialog.createButton).toBeDisabled();
			});

			await test.step("Add Work Tracking System Options", async () => {
				for (const option of workTrackingSystemOptions) {
					await workTrackingSystemDialog.setWorkTrackingSystemOption(
						option.field,
						option.value,
					);
				}

				await expect(workTrackingSystemDialog.validateButton).toBeDisabled();
				await expect(workTrackingSystemDialog.createButton).toBeDisabled();
			});

			const wtsName = generateRandomName();

			await test.step("Set Name should enable Validation", async () => {
				await workTrackingSystemDialog.setConnectionName(wtsName);

				await expect(workTrackingSystemDialog.validateButton).toBeEnabled();
				await expect(workTrackingSystemDialog.createButton).toBeDisabled();
			});

			await test.step("Validation allows Save", async () => {
				await workTrackingSystemDialog.validate();
				await expect(workTrackingSystemDialog.validateButton).toBeEnabled();
				await expect(workTrackingSystemDialog.createButton).toBeEnabled();
			});

			await test.step("Create makes Work Tracking System available for teams and projects", async () => {
				await workTrackingSystemDialog.create();

				const savedWorkTrackingSystem =
					workTrackingSystemsPage.getWorkTrackingSystem(wtsName);
				await expect(savedWorkTrackingSystem).toBeVisible();

				const newTeamPage = await (
					await overviewPage.lightHousePage.goToTeams()
				).addNewTeam();
				await newTeamPage.selectWorkTrackingSystem(wtsName);

				const newProjectPage = await (
					await overviewPage.lightHousePage.goToProjects()
				).addNewProject();
				await newProjectPage.selectWorkTrackingSystem(wtsName);
			});

			await test.step("Delete Removes Work Tracking System", async () => {
				settingsPage = await overviewPage.lighthousePage.goToSettings();
				workTrackingSystemsPage = await settingsPage.goToWorkTrackingSystems();

				await workTrackingSystemsPage.removeWorkTrackingSystem(wtsName);

				const removedWorkTrackingSystem =
					workTrackingSystemsPage.getWorkTrackingSystem(wtsName);
				await expect(removedWorkTrackingSystem).not.toBeVisible();
			});
		},
	);
}

testWithData(
	"Modification of Existing Work Tracking System Works as Expected",
	async ({ testData, overviewPage }) => {
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		let workTrackingSystemsPage = await settingsPage.goToWorkTrackingSystems();

		await test.step("Lists existing work tracking systems", async () => {
			for (const system of testData.connections) {
				const existingSystem = workTrackingSystemsPage.getWorkTrackingSystem(
					system.name,
				);
				await expect(existingSystem).toBeVisible();
			}
		});

		const connectionToModify = testData.connections[0];
		const oldName = connectionToModify.name;
		const newName = generateRandomName();

		await test.step("Can't modify without providing token and Re-Validation", async () => {
			const modifyDialog =
				await workTrackingSystemsPage.modifyWorkTryckingSystem(
					connectionToModify.name,
				);
			await expect(modifyDialog.validateButton).not.toBeEnabled();

			await modifyDialog.setWorkTrackingSystemOption(
				"Personal Access Token",
				"Bamboleo",
			);
			await modifyDialog.setConnectionName(newName);
			await expect(modifyDialog.validateButton).toBeEnabled();

			await modifyDialog.validate();
			await expect(modifyDialog.validateButton).toBeEnabled();
			await expect(modifyDialog.createButton).toBeDisabled();

			workTrackingSystemsPage = await modifyDialog.cancel();
			await expect(
				workTrackingSystemsPage.getWorkTrackingSystem(oldName),
			).toBeVisible();
			await expect(
				workTrackingSystemsPage.getWorkTrackingSystem(newName),
			).not.toBeVisible();
		});

		await test.step("Modify name will adjust name in projects and teams", async () => {
			const modifyDialog =
				await workTrackingSystemsPage.modifyWorkTryckingSystem(
					connectionToModify.name,
				);
			await expect(modifyDialog.validateButton).not.toBeEnabled();

			await modifyDialog.setWorkTrackingSystemOption(
				"Personal Access Token",
				TestConfig.AzureDevOpsToken,
			);
			await modifyDialog.setConnectionName(newName);
			await expect(modifyDialog.validateButton).toBeEnabled();

			await modifyDialog.validate();
			await expect(modifyDialog.validateButton).toBeEnabled();
			await expect(modifyDialog.createButton).toBeEnabled();

			await modifyDialog.create();

			await expect(
				workTrackingSystemsPage.getWorkTrackingSystem(oldName),
			).not.toBeVisible();
			await expect(
				workTrackingSystemsPage.getWorkTrackingSystem(newName),
			).toBeVisible();

			const newTeamPage = await (
				await overviewPage.lightHousePage.goToTeams()
			).addNewTeam();
			await newTeamPage.selectWorkTrackingSystem(newName);

			const newProjectPage = await (
				await overviewPage.lightHousePage.goToProjects()
			).addNewProject();
			await newProjectPage.selectWorkTrackingSystem(newName);
		});
	},
);

testWithData(
	"Take @screenshot create new work tracking system connection dialg",
	async ({ overviewPage }) => {
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		const workTrackingSystemsPage =
			await settingsPage.goToWorkTrackingSystems();
		const workTrackingSystemDialog =
			await workTrackingSystemsPage.addNewWorkTrackingSystem();

		// Wait for the dialog to be visible
		await workTrackingSystemDialog.selectWorkTrackingSystem("AzureDevOps");
		await workTrackingSystemDialog.setConnectionName(
			"My Work Tracking System Connection",
		);

		const screenshotLocation = `${getPathToDocsAssetsFolder()}/concepts/general/worktrackingsystem.png`;
		await workTrackingSystemDialog.page
			.getByRole("dialog")
			.screenshot({ path: screenshotLocation });
	},
);
