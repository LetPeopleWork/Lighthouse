import { TestConfig } from "../../../playwright.config";
import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";
import { generateRandomName } from "../../helpers/names";

testWithData(
	"should show all portfolios on dashboard",
	async ({ testData, overviewPage }) => {
		const [portfolio1, portfolio2] = testData.portfolios;

		await expect(await overviewPage.getPortfolioLink(portfolio1)).toBeVisible();
		await expect(await overviewPage.getPortfolioLink(portfolio2)).toBeVisible();
	},
);

testWithData(
	"should filter portfolios on dashboard",
	async ({ testData, overviewPage }) => {
		const [portfolio1, portfolio2] = testData.portfolios;

		await test.step(`Search for Portfolio ${portfolio1.name}`, async () => {
			await overviewPage.search(portfolio1.name);

			const portfolioLink = await overviewPage.getPortfolioLink(portfolio1);

			await expect(portfolioLink).toBeVisible();
		});

		await test.step(`Search for Portfolio ${portfolio2.name}`, async () => {
			await overviewPage.search(portfolio2.name);

			const portfolioLink = await overviewPage.getPortfolioLink(portfolio2);
			await expect(portfolioLink).toBeVisible();
		});

		await test.step("Search for not existing Portfolio", async () => {
			await overviewPage.search("Jambalaya");

			const portfolioLink1 = await overviewPage.getPortfolioLink(portfolio1);
			const portfolioLink2 = await overviewPage.getPortfolioLink(portfolio2);

			await expect(portfolioLink1).not.toBeVisible();
			await expect(portfolioLink2).not.toBeVisible();
		});

		await test.step("Clear Search", async () => {
			await overviewPage.search("");

			const portfolioLink1 = await overviewPage.getPortfolioLink(portfolio1);
			const portfolioLink2 = await overviewPage.getPortfolioLink(portfolio2);

			await expect(portfolioLink1).toBeVisible();
			await expect(portfolioLink2).toBeVisible();
		});
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

			const workTrackingSystemEditPage = await overviewPage.addConnection();

			await test.step("Select Work Tracking System", async () => {
				await workTrackingSystemEditPage.selectWorkTrackingSystem(
					workTrackingSystemName,
				);

				await expect(workTrackingSystemEditPage.validateButton).toBeDisabled();
				await expect(workTrackingSystemEditPage.createButton).toBeDisabled();
			});

			// We select the Work Tracking System first because it will clear the name
			const wtsName = generateRandomName();

			await test.step("Set Name of Work Tracking System", async () => {
				await workTrackingSystemEditPage.setConnectionName(wtsName);

				await expect(workTrackingSystemEditPage.validateButton).toBeDisabled();
				await expect(workTrackingSystemEditPage.createButton).toBeDisabled();
			});

			await test.step("Add Work Tracking System Options", async () => {
				for (const option of workTrackingSystemOptions) {
					await workTrackingSystemEditPage.setWorkTrackingSystemOption(
						option.field,
						option.value,
					);
				}

				await expect(workTrackingSystemEditPage.validateButton).toBeEnabled();
				await expect(workTrackingSystemEditPage.createButton).toBeDisabled();
			});

			await test.step("Validation allows Save", async () => {
				await workTrackingSystemEditPage.validate();
				await expect(workTrackingSystemEditPage.validateButton).toBeEnabled();
				await expect(workTrackingSystemEditPage.createButton).toBeEnabled();
			});

			await test.step("Create makes Work Tracking System available for teams and portfolios", async () => {
				overviewPage = await workTrackingSystemEditPage.create();

				const savedWorkTrackingSystem = overviewPage.getConnectionLink(wtsName);
				await expect(savedWorkTrackingSystem).toBeVisible();

				const teamsPage = await overviewPage.lightHousePage.goToOverview();
				const newTeamPage = await teamsPage.addNewTeam();
				await newTeamPage.selectWorkTrackingSystem(wtsName);

				const portfoliosPages =
					await overviewPage.lightHousePage.goToOverview();
				const newPortfolioPage = await portfoliosPages.addNewPortfolio();
				await newPortfolioPage.selectWorkTrackingSystem(wtsName);
			});
		},
	);
}

testWithData(
	"Modification of Existing Work Tracking System Works as Expected",
	async ({ testData, overviewPage }) => {
		await test.step("Lists existing work tracking systems", async () => {
			for (const system of testData.connections) {
				const existingSystem = overviewPage.getConnectionLink(system.name);
				await expect(existingSystem).toBeVisible();
			}
		});

		const connectionToModify = testData.connections[0];
		const oldName = connectionToModify.name;
		const newName = generateRandomName();

		await test.step("Can modify without providing token and Re-Validation", async () => {
			const editWorkTrackingSystemPage = await overviewPage.editConnection(
				connectionToModify.name,
			);
			await expect(editWorkTrackingSystemPage.validateButton).toBeEnabled();

			await editWorkTrackingSystemPage.setWorkTrackingSystemOption(
				"Personal Access Token",
				"Bamboleo",
			);
			await editWorkTrackingSystemPage.setConnectionName(newName);
			await expect(editWorkTrackingSystemPage.validateButton).toBeEnabled();

			await editWorkTrackingSystemPage.validate();
			await expect(editWorkTrackingSystemPage.validateButton).toBeEnabled();
			await expect(editWorkTrackingSystemPage.createButton).toBeDisabled();

			// Abort without saving
			overviewPage = await overviewPage.lightHousePage.goToOverview();

			await expect(overviewPage.getConnectionLink(oldName)).toBeVisible();
			await expect(overviewPage.getConnectionLink(newName)).not.toBeVisible();
		});

		await test.step("Modify name will adjust name in portfolios and teams", async () => {
			const editWorkTrackingSystemPage = await overviewPage.editConnection(
				connectionToModify.name,
			);
			await expect(editWorkTrackingSystemPage.validateButton).toBeEnabled();

			await editWorkTrackingSystemPage.setConnectionName(newName);
			await expect(editWorkTrackingSystemPage.validateButton).toBeEnabled();

			await editWorkTrackingSystemPage.validate();
			await expect(editWorkTrackingSystemPage.validateButton).toBeEnabled();
			await expect(editWorkTrackingSystemPage.createButton).toBeEnabled();

			overviewPage = await editWorkTrackingSystemPage.create();

			await expect(overviewPage.getConnectionLink(oldName)).not.toBeVisible();
			await expect(overviewPage.getConnectionLink(newName)).toBeVisible();

			const teamsPage = await overviewPage.lightHousePage.goToOverview();
			const newTeamPage = await teamsPage.addNewTeam();
			await newTeamPage.selectWorkTrackingSystem(newName);

			const portfoliosPage = await overviewPage.lightHousePage.goToOverview();
			const newPortfolioPage = await portfoliosPage.addNewPortfolio();
			await newPortfolioPage.selectWorkTrackingSystem(newName);
		});
	},
);
