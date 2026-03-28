import { TestConfig } from "../../../playwright.config";
import { expect, test } from "../../fixutres/LighthouseFixture";
import { generateRandomName } from "../../helpers/names";

test("should be able to handle a team defined in Linear", async ({
	overviewPage,
}) => {
	const workTrackingSystem = {
		name: generateRandomName(),
	};

	await test.step("Create Linear Work Tracking System Connection", async () => {
		await overviewPage.lightHousePage.goToOverview();
		const workTrackingSystemEditPage = await overviewPage.addConnection();

		await workTrackingSystemEditPage.selectWorkTrackingSystem("Linear");

		await workTrackingSystemEditPage.setConnectionName(workTrackingSystem.name);

		await workTrackingSystemEditPage.setWorkTrackingSystemOption(
			"API Key",
			TestConfig.LinearApiKey,
		);

		await workTrackingSystemEditPage.validate();
		await workTrackingSystemEditPage.create();
	});

	const newTeam = { id: 0, name: "LighthouseDemo" };

	await test.step("Create Linear Team", async () => {
		let newTeamPage = await overviewPage.lightHousePage.createNewTeam();

		await test.step("Add general configuration", async () => {
			await newTeamPage.setName(newTeam.name);
			await newTeamPage.setThroughputHistory(20);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.validateButton).toBeDisabled();
		});

		await test.step("Select Work Tracking System and Linear Team", async () => {
			await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);

			const linearTeamWizard = await newTeamPage.openTeamWizard(
				"Linear",
				"Team",
			);
			await linearTeamWizard.selectByName(newTeam.name);

			await expect(linearTeamWizard.boardInformationPanel).toBeVisible();
			expect(await linearTeamWizard.confirmButton.isEnabled()).toBeTruthy();

			newTeamPage = await linearTeamWizard.confirm();

			// Now we have all default configuration set
			await expect(newTeamPage.validateButton).toBeEnabled();
		});

		await test.step("Validate Settings", async () => {
			await newTeamPage.validate();
			await expect(newTeamPage.validateButton).toBeEnabled();
			await expect(newTeamPage.saveButton).toBeEnabled();
		});

		await test.step("Create New Team", async () => {
			await newTeamPage.validate();
			await expect(newTeamPage.saveButton).toBeEnabled();
			const teamInfoPage = await newTeamPage.save();

			await expect(teamInfoPage.updateTeamDataButton).toBeEnabled();
			newTeam.id = teamInfoPage.teamId;

			const teamsPage = await overviewPage.lightHousePage.goToOverview();
			await teamsPage.search(newTeam.name);
			const teamLink = await teamsPage.getTeamLink(newTeam.name);
			await expect(teamLink).toBeVisible();
		});
	});

	await test.step("Create Linear Portfolio", async () => {
		const newPortfolio = {
			id: 0,
			name: "My Demo Project",
		};

		const portfolioPage = await overviewPage.lightHousePage.goToOverview();

		const newPortfolioPage = await portfolioPage.addNewPortfolio();
		await test.step("Add general configuration", async () => {
			await newPortfolioPage.setName(newPortfolio.name);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newPortfolioPage.validateButton).toBeDisabled();
		});

		await test.step("Select Work Tracking System", async () => {
			await newPortfolioPage.selectWorkTrackingSystem(workTrackingSystem.name);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newPortfolioPage.validateButton).toBeDisabled();
		});

		await test.step("Add State Configuration", async () => {
			await newPortfolioPage.resetStates(
				{
					toDo: [],
					doing: [],
					done: [],
				},
				{
					toDo: ["Backlog", "Planned"],
					doing: ["In Progress"],
					done: ["Completed"],
				},
			);

			// Now we have all default configuration set
			await expect(newPortfolioPage.validateButton).toBeEnabled();
		});

		await test.step("Validate Settings", async () => {
			await newPortfolioPage.validate();
			await expect(newPortfolioPage.validateButton).toBeEnabled();
			await expect(newPortfolioPage.saveButton).toBeEnabled();
		});

		await test.step("Create New Portfolio", async () => {
			await newPortfolioPage.validate();
			await expect(newPortfolioPage.saveButton).toBeEnabled();
			const portfolioInfoPage = await newPortfolioPage.save();

			await expect(portfolioInfoPage.refreshFeatureButton).toBeDisabled();
			newPortfolio.id = portfolioInfoPage.portfolioId;

			const portfolioPage = await overviewPage.lightHousePage.goToOverview();
			await portfolioPage.search(newPortfolio.name);
			const portfolioLink = await portfolioPage.getPortfolioLink(
				newPortfolio.name,
			);
			await expect(portfolioLink).toBeVisible();
		});
	});
});
