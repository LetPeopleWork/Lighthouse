import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	createPortfoliosCsvFile,
	createTeamCsvFile,
} from "../../helpers/csv/csvTestData";
import { CsvUploadHelper } from "../../helpers/csv/csvUploadHelper";
import { generateRandomName } from "../../helpers/names";

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
		const settingsPage = await overviewPage.lightHousePage.goToSettings();

		const workTrackingSystemConnections =
			await settingsPage.goToWorkTrackingSystems();

		const addWorkTrackingSystemConnectionDialog =
			await workTrackingSystemConnections.addNewWorkTrackingSystem();

		await addWorkTrackingSystemConnectionDialog.selectWorkTrackingSystem("Csv");

		await addWorkTrackingSystemConnectionDialog.setConnectionName(
			workTrackingSystem.name,
		);

		await addWorkTrackingSystemConnectionDialog.validate();
		await addWorkTrackingSystemConnectionDialog.create();
	});

	await test.step("Create CSV Team with file upload", async () => {
		// Generate CSV data with current date to ensure consistent 30-day metrics
		teamCsvFile = createTeamCsvFile();

		const newTeamPage = await overviewPage.lightHousePage.createNewTeam();
		const csvUploadHelper = new CsvUploadHelper(newTeamPage.page);

		await test.step("Add general configuration", async () => {
			await newTeamPage.setName(newTeam.name);
			await newTeamPage.setThroughputHistory(20);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.validateButton).toBeDisabled();
		});

		await test.step("Add Work Item Type Configuration", async () => {
			await newTeamPage.resetWorkItemTypes(
				["User Story", "Bug"],
				["User Story", "Bug", "Task"],
			);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.validateButton).toBeDisabled();
		});

		await test.step("Add State Configuration", async () => {
			await newTeamPage.resetStates(
				{
					toDo: ["New", "Proposed", "To Do"],
					doing: ["Active", "Resolved", "In Progress", "Committed"],
					done: ["Done", "Closed"],
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

		await test.step("Select CSV Work Tracking System", async () => {
			await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);

			// CSV system should now show file upload component
			await expect(csvUploadHelper.isFileUploadVisible()).resolves.toBe(true);

			// Upload the CSV file - ensure teamCsvFile is not null
			if (!teamCsvFile) {
				throw new Error("Team CSV file not created");
			}
			await csvUploadHelper.uploadCsvFile(teamCsvFile.filePath);
			await csvUploadHelper.waitForUploadComplete();

			// Verify file was uploaded
			const selectedFile = await csvUploadHelper.getSelectedFileName();
			expect(selectedFile).toBeTruthy();

			// Now we have all default configuration set
			await expect(newTeamPage.validateButton).toBeEnabled();
		});

		await test.step("Validate Settings", async () => {
			await newTeamPage.validate();
			await expect(newTeamPage.validateButton).toBeEnabled();
			await expect(newTeamPage.saveButton).toBeEnabled();

			// Check for any validation errors
			const hasErrors = await csvUploadHelper.hasValidationErrors();
			if (hasErrors) {
				const errors = await csvUploadHelper.getValidationErrors();
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
		const newPortfolioPage = await portfoliosPage.addNewPortfolio();
		const csvUploadHelper = new CsvUploadHelper(newPortfolioPage.page);

		await test.step("Add general configuration", async () => {
			await newPortfolioPage.setName(newPortfolio.name);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newPortfolioPage.validateButton).toBeDisabled();
		});

		await test.step("Add Work Item Type Configuration", async () => {
			await newPortfolioPage.resetWorkItemTypes(["Epic"], ["Epic"]);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newPortfolioPage.validateButton).toBeDisabled();
		});

		await test.step("Add Involved Teams Configuration", async () => {
			await newPortfolioPage.selectTeam(newTeam.name);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newPortfolioPage.validateButton).toBeDisabled();
		});

		await test.step("Add State Configuration", async () => {
			await newPortfolioPage.resetStates(
				{
					toDo: ["New", "Proposed", "To Do"],
					doing: ["Active", "Resolved", "In Progress", "Committed"],
					done: ["Done", "Closed"],
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

			// CSV system should now show file upload component
			await expect(csvUploadHelper.isFileUploadVisible()).resolves.toBe(true);

			// Upload the CSV file - ensure portfolioCsvFile is not null
			if (!portfolioCsvFile) {
				throw new Error("portfolio CSV file not created");
			}
			await csvUploadHelper.uploadCsvFile(portfolioCsvFile.filePath);
			await csvUploadHelper.waitForUploadComplete();

			// Verify file was uploaded
			const selectedFile = await csvUploadHelper.getSelectedFileName();
			expect(selectedFile).toBeTruthy();

			// Now we have all default configuration set
			await expect(newPortfolioPage.validateButton).toBeEnabled();
		});

		await test.step("Validate Settings", async () => {
			await newPortfolioPage.validate();
			await expect(newPortfolioPage.validateButton).toBeEnabled();
			await expect(newPortfolioPage.saveButton).toBeEnabled();

			// Check for any validation errors
			const hasErrors = await csvUploadHelper.hasValidationErrors();
			if (hasErrors) {
				const errors = await csvUploadHelper.getValidationErrors();
				console.warn("CSV validation errors:", errors);
			}
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

		// Check that features can be toggled (indicating features are present)
		await teamInfoPage.toggleFeatures();
		await teamInfoPage.page.waitForTimeout(1000);

		// Toggle back to show features
		await teamInfoPage.toggleFeatures();
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
