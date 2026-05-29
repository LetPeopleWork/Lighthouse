import { TestConfig } from "../../../playwright.config";
import { expect, test } from "../../fixutres/LighthouseFixture";
import { generateRandomName } from "../../helpers/names";
import { takeDialogScreenshot } from "../../helpers/screenshots";
import { PortfolioDetailPage } from "../../models/portfolios/PortfolioDetailPage";
import { TeamDetailPage } from "../../models/teams/TeamDetailPage";

test("should be able to handle a team defined in Linear", async ({
	overviewPage,
}) => {
	const workTrackingSystem = {
		name: generateRandomName(),
	};

	await test.step("Create Linear Work Tracking System Connection", async () => {
		await overviewPage.lightHousePage.goToOverview();
		const workTrackingSystemCreationWizard = await overviewPage.addConnection();

		await workTrackingSystemCreationWizard.selectWorkTrackingSystemType(
			"Linear",
		);

		await workTrackingSystemCreationWizard.setWorkTrackingSystemOption(
			"API Key",
			TestConfig.LinearApiKey,
		);

		await workTrackingSystemCreationWizard.goToNextStep();

		await workTrackingSystemCreationWizard.setConnectionName(
			workTrackingSystem.name,
		);

		await workTrackingSystemCreationWizard.create();
	});

	const newTeam = { id: 0, name: "LighthouseDemo" };

	await test.step("Create Linear Team", async () => {
		let newTeamPage = await overviewPage.lightHousePage.createNewTeam();

		await test.step("Choose Connection", async () => {
			await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);
		});

		await test.step("Select Linear Team in Wizard", async () => {
			const linearTeamWizard = await newTeamPage.selectWizard("Linear", "Team");
			await linearTeamWizard.selectByName(newTeam.name);

			await expect(linearTeamWizard.boardInformationPanel).toBeVisible();
			expect(await linearTeamWizard.confirmButton.isEnabled()).toBeTruthy();

			await takeDialogScreenshot(
				linearTeamWizard.page.getByRole("dialog"),
				"concepts/linear_team_wizard.png",
				5,
				1000,
			);

			newTeamPage = await linearTeamWizard.confirm();
		});

		await test.step("Add Name", async () => {
			// We expect to skip the manual config here since Linear should pre-populate everything based on the selected team - just need to add a name
			await newTeamPage.setName(newTeam.name);

			await expect(newTeamPage.createButton).toBeEnabled();
		});

		await test.step("Create New Team", async () => {
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

	await test.step("Create Linear Portfolio", async () => {
		const newPortfolio = {
			id: 0,
			name: "My Demo Project",
		};

		const portfolioPage = await overviewPage.lightHousePage.goToOverview();

		const newPortfolioPage = await portfolioPage.addNewPortfolio();

		await test.step("Choose Connection", async () => {
			await newPortfolioPage.selectWorkTrackingSystem(workTrackingSystem.name);
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

			await expect(newPortfolioPage.nextButton).toBeEnabled();
			await newPortfolioPage.goToNextStep();
		});

		await test.step("Add Name", async () => {
			await newPortfolioPage.setName(newPortfolio.name);

			await expect(newPortfolioPage.createButton).toBeEnabled();
		});

		await test.step("Create New Portfolio", async () => {
			const portfolioInfoPage = await newPortfolioPage.create(
				(page) => new PortfolioDetailPage(page),
			);

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
