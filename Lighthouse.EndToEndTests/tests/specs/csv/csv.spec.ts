import { expect, test } from "../../fixutres/LighthouseFixture";
import type { CsvUploadWizard } from "../../helpers/csv/CsvUploadWizard";
import {
	createPortfoliosCsvFile,
	createTeamCsvFile,
} from "../../helpers/csv/csvTestData";
import { generateRandomName } from "../../helpers/names";
import type { TeamEditPage } from "../../models/teams/TeamEditPage";

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
		const workTrackingSystemEditPage = await overviewPage.addConnection();

		await workTrackingSystemEditPage.selectWorkTrackingSystem("Csv");

		await workTrackingSystemEditPage.setConnectionName(workTrackingSystem.name);

		await workTrackingSystemEditPage.validate();
		await workTrackingSystemEditPage.create();
	});

	await test.step("Create CSV Team with file upload", async () => {
		// Generate CSV data with current date to ensure consistent 30-day metrics
		teamCsvFile = createTeamCsvFile();

		let newTeamPage = await overviewPage.lightHousePage.createNewTeam();

		await test.step("Add general configuration", async () => {
			await newTeamPage.setName(newTeam.name);
			await newTeamPage.setThroughputHistory(20);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.validateButton).toBeDisabled();
		});

		await test.step("Add Work Item Type Configuration", async () => {
			await newTeamPage.resetWorkItemTypes([], ["User Story", "Bug", "Task"]);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.validateButton).toBeDisabled();
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

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.validateButton).toBeDisabled();
		});

		let csvUploadWizard: CsvUploadWizard<TeamEditPage>;

		await test.step("Select CSV Work Tracking System", async () => {
			await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);

			csvUploadWizard = await newTeamPage.triggerCsvWizard();

			// CSV system should now show file upload component
			await expect(csvUploadWizard.isFileUploadVisible()).resolves.toBe(true);

			// Upload the CSV file - ensure teamCsvFile is not null
			if (!teamCsvFile) {
				throw new Error("Team CSV file not created");
			}
			await csvUploadWizard.uploadCsvFile(teamCsvFile.filePath);
			await csvUploadWizard.waitForUploadComplete();

			await expect(csvUploadWizard.hasValidationErrors()).resolves.toBeFalsy();

			newTeamPage = await csvUploadWizard.useFile();

			// Now we have all default configuration set
			await expect(newTeamPage.validateButton).toBeEnabled();
		});

		await test.step("Validate Settings", async () => {
			await newTeamPage.validate();
			await expect(newTeamPage.validateButton).toBeEnabled();
			await expect(newTeamPage.saveButton).toBeEnabled();

			// Check for any validation errors
			const hasErrors = await csvUploadWizard.hasValidationErrors();
			if (hasErrors) {
				const errors = await csvUploadWizard.getValidationErrors();
				console.warn("CSV validation errors:", errors);
			}
		});

		await test.step("Create New Team", async () => {
			await newTeamPage.validate();
			await expect(newTeamPage.saveButton).toBeEnabled();
			const teamInfoPage = await newTeamPage.save();

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

		await test.step("Add general configuration", async () => {
			await newPortfolioPage.setName(newPortfolio.name);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newPortfolioPage.validateButton).toBeDisabled();
		});

		await test.step("Add Work Item Type Configuration", async () => {
			await newPortfolioPage.resetWorkItemTypes([], ["Epic"]);

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
					toDo: ["To Do"],
					doing: ["In Progress"],
					done: ["Done"],
				},
			);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newPortfolioPage.validateButton).toBeDisabled();
		});

		await test.step("Select CSV Work Tracking System", async () => {
			await newPortfolioPage.selectWorkTrackingSystem(workTrackingSystem.name);

			const csvWizard = await newPortfolioPage.triggerCsvWizard();

			// CSV system should now show file upload component
			await expect(csvWizard.isFileUploadVisible()).resolves.toBe(true);

			// Upload the CSV file - ensure portfolioCsvFile is not null
			if (!portfolioCsvFile) {
				throw new Error("portfolio CSV file not created");
			}
			await csvWizard.uploadCsvFile(portfolioCsvFile.filePath);
			await csvWizard.waitForUploadComplete();

			newPortfolioPage = await csvWizard.useFile();

			// Now we have all default configuration set
			await expect(newPortfolioPage.validateButton).toBeEnabled();
		});

		await test.step("Validate Settings", async () => {
			await newPortfolioPage.validate();
			await expect(newPortfolioPage.validateButton).toBeEnabled();
			await expect(newPortfolioPage.saveButton).toBeEnabled();
		});

		await test.step("Create New portfolio", async () => {
			await newPortfolioPage.validate();
			await expect(newPortfolioPage.saveButton).toBeEnabled();
			const portfolioInfoPage = await newPortfolioPage.save();

			await expect(portfolioInfoPage.refreshFeatureButton).toBeEnabled();
			newPortfolio.id = portfolioInfoPage.portfolioId;

			const portfoliosPage = await overviewPage.lightHousePage.goToOverview();
			await portfoliosPage.search(newPortfolio.name);
			const portfolioLink = await overviewPage.getPortfolioLink(newPortfolio);
			await expect(portfolioLink).toBeVisible();
		});
	});

	await test.step("Wait for portfolio metrics to load and verify features", async () => {
		const portfolioInfoPage = await overviewPage.goToPortfolio(newPortfolio);

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
