import { TestConfig } from "../../../playwright.config";
import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";
import { generateRandomName } from "../../helpers/names";

const workTrackingSystemConfiguration = [
	{
		workTrackingSystemName: "AzureDevOps",
		workTrackingSystemOptions: [
			{
				field: "Organization URL",
				value: "https://dev.azure.com/letpeoplework",
			},
			{ field: "Personal Access Token", value: TestConfig.AzureDevOpsToken },
		],
	},
	{
		workTrackingSystemName: "Jira",
		workTrackingSystemOptions: [
			{ field: "Jira URL", value: "https://letpeoplework.atlassian.net" },
			{
				field: "Username (Email)",
				value: "atlassian.pushchair@huser-berta.com",
			},
			{ field: "API Token", value: TestConfig.JiraToken },
		],
	},
];

for (const {
	workTrackingSystemName,
	workTrackingSystemOptions,
} of workTrackingSystemConfiguration) {
	testWithData(
		`Should add new ${workTrackingSystemName} Work Tracking System and make it available in Team and portfolio creation`,
		async ({ testData, overviewPage }) => {
			test.fail(
				testData.portfolios.length < 1,
				"Expected to have portfolios initiatilized to prevent tutorial page from being displayed",
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

			// We select the Work Tracking System first because it will clear the name
			const wtsName = generateRandomName();

			await test.step("Set Name of Work Tracking System", async () => {
				await workTrackingSystemDialog.setConnectionName(wtsName);

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

				await expect(workTrackingSystemDialog.validateButton).toBeEnabled();
				await expect(workTrackingSystemDialog.createButton).toBeDisabled();
			});

			await test.step("Validation allows Save", async () => {
				await workTrackingSystemDialog.validate();
				await expect(workTrackingSystemDialog.validateButton).toBeEnabled();
				await expect(workTrackingSystemDialog.createButton).toBeEnabled();
			});

			await test.step("Create makes Work Tracking System available for teams and portfolios", async () => {
				await workTrackingSystemDialog.create();

				const savedWorkTrackingSystem =
					workTrackingSystemsPage.getWorkTrackingSystem(wtsName);
				await expect(savedWorkTrackingSystem).toBeVisible();

				const teamsPage = await overviewPage.lightHousePage.goToOverview();
				const newTeamPage = await teamsPage.addNewTeam();
				await newTeamPage.selectWorkTrackingSystem(wtsName);

				const portfoliosPages =
					await overviewPage.lightHousePage.goToOverview();
				const newPortfolioPage = await portfoliosPages.addNewPortfolio();
				await newPortfolioPage.selectWorkTrackingSystem(wtsName);
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

		await test.step("Can modify without providing token and Re-Validation", async () => {
			const modifyDialog =
				await workTrackingSystemsPage.modifyWorkTryckingSystem(
					connectionToModify.name,
				);
			await expect(modifyDialog.validateButton).toBeEnabled();

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

		await test.step("Modify name will adjust name in portfolios and teams", async () => {
			const modifyDialog =
				await workTrackingSystemsPage.modifyWorkTryckingSystem(
					connectionToModify.name,
				);
			await expect(modifyDialog.validateButton).toBeEnabled();

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

			const teamsPage = await overviewPage.lightHousePage.goToOverview();
			const newTeamPage = await teamsPage.addNewTeam();
			await newTeamPage.selectWorkTrackingSystem(newName);

			const portfoliosPage = await overviewPage.lightHousePage.goToOverview();
			const newPortfolioPage = await portfoliosPage.addNewPortfolio();
			await newPortfolioPage.selectWorkTrackingSystem(newName);
		});
	},
);
