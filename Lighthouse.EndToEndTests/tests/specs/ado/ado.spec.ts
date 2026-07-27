import { TestConfig } from "../../../playwright.config";
import { expect, test } from "../../fixutres/LighthouseFixture";
import { generateRandomName } from "../../helpers/names";
import { PortfolioDetailPage } from "../../models/portfolios/PortfolioDetailPage";
import { TeamDetailPage } from "../../models/teams/TeamDetailPage";

// Both creations below block on the new owner's INITIAL data fetch from real
// Azure DevOps, so the size of the board picked here IS the runtime of this spec.
//
// The team leg used to select `Lighthouse - Stories`, the live dev backlog. Its
// initial fetch was measured at 101.1s on CI run 30258220986 — past the 90s the
// wait then allowed, which would have failed the attempt and made Playwright
// re-run the WHOLE spec (one such retry cost 231s against a 73s happy path, the
// largest single item in the E2E wall-clock). It now selects `DummyProject -
// Stories`: 3 work items, kept deliberately as a fixture, so the fetch is
// near-instant. What this leg proves — the wizard enumerates real boards, binds
// one, and the resulting team completes its first fetch — does not depend on how
// much data comes back.
//
// The portfolio leg deliberately STAYS on `Lighthouse - Epics` (127 real epics,
// ~24.5s). DummyProject has zero epics, so moving it there would still pass —
// nothing asserts a feature count — while quietly proving only that a board can
// be bound, never that features are fetched. A hollow skeleton is worse than a
// slow one.
//
// Budgets are sized so a slow-but-working fetch is absorbed rather than retried:
// retrying is the expensive path here, not waiting.
test.setTimeout(180_000);

const INITIAL_ADO_FETCH_TIMEOUT_MS = 120_000;

// Kept as named constants so the intent of each board choice stays readable at
// the call site (see the note above before changing either).
const TEAM_BOARD = "DummyProject - Stories";
const PORTFOLIO_BOARD = "Lighthouse - Epics";
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

			await wizard.selectByName(TEAM_BOARD);

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

			await expect(teamInfoPage.updateTeamDataButton).toBeEnabled({
				timeout: INITIAL_ADO_FETCH_TIMEOUT_MS,
			});
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

			await boardWizard.selectByName(PORTFOLIO_BOARD);

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

			await expect(portfolioInfoPage.refreshFeatureButton).toBeEnabled({
				timeout: INITIAL_ADO_FETCH_TIMEOUT_MS,
			});
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
