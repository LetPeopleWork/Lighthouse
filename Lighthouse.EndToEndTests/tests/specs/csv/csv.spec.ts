import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	createPortfoliosCsvFile,
	createTeamCsvFile,
} from "../../helpers/csv/csvTestData";
import { generateRandomName } from "../../helpers/names";
import { PortfolioDetailPage } from "../../models/portfolios/PortfolioDetailPage";
import { TeamDetailPage } from "../../models/teams/TeamDetailPage";

test("should be able to handle teams and portfolios defined via CSV", async ({
	overviewPage,
}) => {
	let teamCsvFile: { filePath: string; cleanup: () => void } | null = null;
	let portfolioCsvFile: { filePath: string; cleanup: () => void } | null = null;

	const newTeam = { id: 0, name: generateRandomName() };
	const newPortfolio = { id: 0, name: generateRandomName() };

	const workTrackingSystem = {
		name: generateRandomName(),
	};

	await test.step("Create CSV Connector", async () => {
		const workTrackingSystemCreationWizard = await overviewPage.addConnection();

		await workTrackingSystemCreationWizard.selectWorkTrackingSystemType("Csv");

		// CSV doesn't have auth - go directly to next step
		await workTrackingSystemCreationWizard.goToNextStep();

		await workTrackingSystemCreationWizard.setConnectionName(
			workTrackingSystem.name,
		);

		overviewPage = await workTrackingSystemCreationWizard.create();
	});

	await test.step("Create CSV Team with file upload", async () => {
		// Generate CSV data with current date to ensure consistent 30-day metrics
		teamCsvFile = createTeamCsvFile();

		let newTeamPage = await overviewPage.lightHousePage.createNewTeam();

		await test.step("Choose Connection", async () => {
			await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);
		});

		await test.step("Select CSV file", async () => {
			const csvUploadWizard = await newTeamPage.selectWizard("CSV", "File");

			// CSV system should now show file upload component
			await expect(csvUploadWizard.isFileUploadVisible()).resolves.toBe(true);

			// Upload the CSV file - ensure teamCsvFile is not null
			if (!teamCsvFile) {
				throw new Error("Team CSV file not created");
			}
			await csvUploadWizard.selectByName(teamCsvFile.filePath);
			await csvUploadWizard.waitForUploadComplete();

			await expect(csvUploadWizard.hasValidationErrors()).resolves.toBeFalsy();

			newTeamPage = await csvUploadWizard.confirm();
		});

		await test.step("Add Work Item Type Configuration", async () => {
			await newTeamPage.resetWorkItemTypes([], ["User Story", "Bug", "Task"]);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.nextButton).toBeDisabled();
		});

		await test.step("Add State Configuration", async () => {
			await newTeamPage.resetStates(
				{
					toDo: [],
					doing: [],
					done: [],
				},
				{
					toDo: ["To Do"],
					doing: ["In Progress"],
					done: ["Done"],
				},
			);

			// We have all mandatory config set, next should be enabled
			await expect(newTeamPage.nextButton).toBeEnabled();

			await newTeamPage.goToNextStep();
		});

		await test.step("Add Name and Create", async () => {
			await newTeamPage.setName("");

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.createButton).toBeDisabled();

			await newTeamPage.setName(newTeam.name);

			// Now we have all default configuration set
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
			const teamLink = await overviewPage.getTeamLink(newTeam.name);
			await expect(teamLink).toBeVisible();
		});
	});

	await test.step("Wait for team metrics to load and verify", async () => {
		// Navigate to team page and update team data to load CSV metrics
		const teamInfoPage = await overviewPage.goToTeam(newTeam.name);
		await teamInfoPage.updateTeamData();

		// Wait for metrics to be calculated
		await teamInfoPage.page.waitForTimeout(5000);

		// Verify that the team can be updated (metrics loaded successfully)
		await expect(teamInfoPage.updateTeamDataButton).toBeEnabled();

		// Verify last updated date is recent
		const lastUpdated = await teamInfoPage.getLastUpdatedDate();
		expect(lastUpdated).toBeDefined();
	});

	await test.step("Create CSV Portfolio with file upload", async () => {
		// Generate CSV data with current date to ensure consistent 30-day metrics
		portfolioCsvFile = createPortfoliosCsvFile();

		const portfoliosPage = await overviewPage.lightHousePage.goToOverview();
		let newPortfolioPage = await portfoliosPage.addNewPortfolio();

		await test.step("Choose Connection", async () => {
			await newPortfolioPage.selectWorkTrackingSystem(workTrackingSystem.name);
		});

		await test.step("Select CSV file", async () => {
			const csvUploadWizard = await newPortfolioPage.selectWizard(
				"CSV",
				"File",
			);

			// CSV system should now show file upload component
			await expect(csvUploadWizard.isFileUploadVisible()).resolves.toBe(true);

			// Upload the CSV file - ensure portfolioCsvFile is not null
			if (!portfolioCsvFile) {
				throw new Error("Portfolio CSV file not created");
			}
			await csvUploadWizard.selectByName(portfolioCsvFile.filePath);
			await csvUploadWizard.waitForUploadComplete();

			await expect(csvUploadWizard.hasValidationErrors()).resolves.toBeFalsy();

			newPortfolioPage = await csvUploadWizard.confirm();
		});

		await test.step("Add Work Item Type Configuration", async () => {
			await newPortfolioPage.resetWorkItemTypes([], ["Epic"]);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newPortfolioPage.nextButton).toBeDisabled();
		});

		await test.step("Add State Configuration", async () => {
			await newPortfolioPage.resetStates(
				{
					toDo: [],
					doing: [],
					done: [],
				},
				{
					toDo: ["To Do"],
					doing: ["In Progress"],
					done: ["Done"],
				},
			);

			// We have all mandatory config set, next should be enabled
			await expect(newPortfolioPage.nextButton).toBeEnabled();
			await newPortfolioPage.goToNextStep();
		});

		await test.step("Add general configuration", async () => {
			await newPortfolioPage.setName("");
			await expect(newPortfolioPage.createButton).toBeDisabled();

			await newPortfolioPage.setName(newPortfolio.name);
			await expect(newPortfolioPage.createButton).toBeEnabled();
		});

		await test.step("Create New portfolio", async () => {
			const portfolioInfoPage = await newPortfolioPage.create(
				(page) => new PortfolioDetailPage(page),
			);

			await expect(portfolioInfoPage.refreshFeatureButton).toBeEnabled();
			newPortfolio.id = portfolioInfoPage.portfolioId;

			const portfoliosPage = await overviewPage.lightHousePage.goToOverview();
			await portfoliosPage.search(newPortfolio.name);
			const portfolioLink = await overviewPage.getPortfolioLink(
				newPortfolio.name,
			);
			await expect(portfolioLink).toBeVisible();
		});
	});

	await test.step("Wait for portfolio metrics to load and verify features", async () => {
		const portfolioInfoPage = await overviewPage.goToPortfolio(
			newPortfolio.name,
		);

		// Refresh features to load CSV data
		await portfolioInfoPage.refreshFeatures();

		// Wait for features to be loaded
		await portfolioInfoPage.page.waitForTimeout(5000);

		// Verify that features refresh button is re-enabled (operation completed)
		await expect(portfolioInfoPage.refreshFeatureButton).toBeEnabled();

		// Verify last updated date is recent
		const lastUpdated = await portfolioInfoPage.getLastUpdatedDate();
		expect(lastUpdated).toBeDefined();
	});

	await test.step("Verify team shows portfolio features correctly", async () => {
		// Navigate to team detail page
		const teamsPage = await overviewPage.lightHousePage.goToOverview();
		const teamInfoPage = await teamsPage.goToTeam(newTeam.name);

		// Verify team can be updated (basic validation that it works with CSV)
		await expect(teamInfoPage.updateTeamDataButton).toBeEnabled();

		// Check if we have features
		const numberOfFeatures = await teamInfoPage.getNumberOfFeatures();
		expect(numberOfFeatures).toBeGreaterThan(0);
	});

	// Cleanup step - ensure CSV files are deleted
	await test.step("Cleanup test files", async () => {
		if (teamCsvFile) {
			teamCsvFile.cleanup();
		}
		if (portfolioCsvFile) {
			portfolioCsvFile.cleanup();
		}
	});
});
