import { TestConfig } from "../../../playwright.config";
import { expect, test } from "../../fixutres/LighthouseFixture";
import { generateRandomName } from "../../helpers/names";
import { PortfolioDetailPage } from "../../models/portfolios/PortfolioDetailPage";
import { TeamDetailPage } from "../../models/teams/TeamDetailPage";

test("should be able to handle a team and portfolio defined in Azure DevOps", async ({
	overviewPage,
}) => {
	const workTrackingSystem = {
		name: generateRandomName(),
	};

	await test.step("Create Azure DevOps Work Tracking System Connection", async () => {
		await overviewPage.lightHousePage.goToOverview();
		const workTrackingSystemCreationWizard = await overviewPage.addConnection();

		await workTrackingSystemCreationWizard.selectWorkTrackingSystemType(
			"AzureDevOps",
		);

		await workTrackingSystemCreationWizard.setWorkTrackingSystemOption(
			"Organization URL",
			"https://dev.azure.com/letpeoplework",
		);
		await workTrackingSystemCreationWizard.setWorkTrackingSystemOption(
			"Personal Access Token",
			TestConfig.AzureDevOpsToken,
		);

		await workTrackingSystemCreationWizard.goToNextStep();

		await workTrackingSystemCreationWizard.setConnectionName(
			workTrackingSystem.name,
		);

		await workTrackingSystemCreationWizard.create();
	});

	const newTeam = { id: 0, name: generateRandomName() };

	await test.step("Create Azure DevOps Team via Wizard", async () => {
		let newTeamPage = await overviewPage.lightHousePage.createNewTeam();

		await test.step("Choose Connection", async () => {
			await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);
		});

		await test.step("Select Azure DevOps Board in Wizard", async () => {
			const wizard = await newTeamPage.selectWizard("Azure DevOps");

			expect(await wizard.confirmButton.isEnabled()).toBeFalsy();

			await wizard.selectByName("Lighthouse - Stories");

			await expect(wizard.boardInformationPanel).toBeVisible();
			expect(await wizard.confirmButton.isEnabled()).toBeTruthy();

			newTeamPage = await wizard.confirm();
		});

		await test.step("Add Name and Create", async () => {
			await newTeamPage.setName(newTeam.name);
			await expect(newTeamPage.createButton).toBeEnabled();

			const teamInfoPage = await newTeamPage.create(
				(page) => new TeamDetailPage(page),
			);

			await expect(teamInfoPage.updateTeamDataButton).toBeEnabled();
			newTeam.id = teamInfoPage.teamId;

			const teamsPage = await overviewPage.lightHousePage.goToOverview();
			await teamsPage.search(newTeam.name);
			const teamLink = await teamsPage.getTeamLink(newTeam.name);
			await expect(teamLink).toBeVisible();
		});
	});

	const newPortfolio = { id: 0, name: generateRandomName() };

	await test.step("Create Azure DevOps Portfolio via Wizard", async () => {
		const portfoliosPage = await overviewPage.lightHousePage.goToOverview();
		let newPortfolioPage = await portfoliosPage.addNewPortfolio();

		await test.step("Choose Connection", async () => {
			await newPortfolioPage.selectWorkTrackingSystem(workTrackingSystem.name);
		});

		await test.step("Select Azure DevOps Board in Wizard", async () => {
			const boardWizard = await newPortfolioPage.selectWizard("Azure DevOps");

			expect(await boardWizard.confirmButton.isEnabled()).toBeFalsy();

			await boardWizard.selectByName("Lighthouse - Epics");

			await expect(boardWizard.boardInformationPanel).toBeVisible();
			expect(await boardWizard.confirmButton.isEnabled()).toBeTruthy();

			newPortfolioPage = await boardWizard.confirm();
		});

		await test.step("Add Name and Create", async () => {
			await newPortfolioPage.setName(newPortfolio.name);
			await expect(newPortfolioPage.createButton).toBeEnabled();

			const portfolioInfoPage = await newPortfolioPage.create(
				(page) => new PortfolioDetailPage(page),
			);

			await expect(portfolioInfoPage.refreshFeatureButton).toBeEnabled();
			newPortfolio.id = portfolioInfoPage.portfolioId;

			const portfoliosPage = await overviewPage.lightHousePage.goToOverview();
			await portfoliosPage.search(newPortfolio.name);
			const portfolioLink = await portfoliosPage.getPortfolioLink(
				newPortfolio.name,
			);
			await expect(portfolioLink).toBeVisible();
		});
	});
});
