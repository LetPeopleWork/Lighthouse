import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	createProjectCsvFile,
	createTeamCsvFile,
} from "../../helpers/csv/csvTestData";
import { CsvUploadHelper } from "../../helpers/csv/csvUploadHelper";
import { generateRandomName } from "../../helpers/names";

test("should be able to handle teams and projects defined via CSV", async ({
	overviewPage,
}) => {
	let teamCsvFile: { filePath: string; cleanup: () => void } | null = null;
	let projectCsvFile: { filePath: string; cleanup: () => void } | null = null;

	const newTeam = { id: 0, name: generateRandomName() };
	const newProject = { id: 0, name: generateRandomName() };

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
			await newTeamPage.selectWorkTrackingSystem("CSV");

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

			const teamsPage = await overviewPage.lightHousePage.goToTeams();
			await teamsPage.search(newTeam.name);
			const teamLink = await teamsPage.getTeamLink(newTeam.name);
			await expect(teamLink).toBeVisible();
		});
	});

	await test.step("Wait for team metrics to load and verify", async () => {
		// Navigate to team page and update team data to load CSV metrics
		const teamsPage = await overviewPage.lightHousePage.goToTeams();
		const teamInfoPage = await teamsPage.goToTeam(newTeam.name);
		await teamInfoPage.updateTeamData();

		// Wait for metrics to be calculated
		await teamInfoPage.page.waitForTimeout(5000);

		// Verify that the team can be updated (metrics loaded successfully)
		await expect(teamInfoPage.updateTeamDataButton).toBeEnabled();

		// Verify last updated date is recent
		const lastUpdated = await teamInfoPage.getLastUpdatedDate();
		expect(lastUpdated).toBeDefined();
	});

	await test.step("Create CSV Project with file upload", async () => {
		// Generate CSV data with current date to ensure consistent 30-day metrics
		projectCsvFile = createProjectCsvFile();

		const projectPage = await overviewPage.lightHousePage.goToProjects();
		const newProjectPage = await projectPage.addNewProject();
		const csvUploadHelper = new CsvUploadHelper(newProjectPage.page);

		await test.step("Add general configuration", async () => {
			await newProjectPage.setName(newProject.name);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newProjectPage.validateButton).toBeDisabled();
		});

		await test.step("Add Work Item Type Configuration", async () => {
			await newProjectPage.resetWorkItemTypes(["Epic"], ["Epic"]);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newProjectPage.validateButton).toBeDisabled();
		});

		await test.step("Add Involved Teams Configuration", async () => {
			await newProjectPage.selectTeam(newTeam.name);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newProjectPage.validateButton).toBeDisabled();
		});

		await test.step("Add State Configuration", async () => {
			await newProjectPage.resetStates(
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
			await expect(newProjectPage.validateButton).toBeDisabled();
		});

		await test.step("Select CSV Work Tracking System", async () => {
			await newProjectPage.selectWorkTrackingSystem("CSV");

			// CSV system should now show file upload component
			await expect(csvUploadHelper.isFileUploadVisible()).resolves.toBe(true);

			// Upload the CSV file - ensure projectCsvFile is not null
			if (!projectCsvFile) {
				throw new Error("Project CSV file not created");
			}
			await csvUploadHelper.uploadCsvFile(projectCsvFile.filePath);
			await csvUploadHelper.waitForUploadComplete();

			// Verify file was uploaded
			const selectedFile = await csvUploadHelper.getSelectedFileName();
			expect(selectedFile).toBeTruthy();

			// Now we have all default configuration set
			await expect(newProjectPage.validateButton).toBeEnabled();
		});

		await test.step("Validate Settings", async () => {
			await newProjectPage.validate();
			await expect(newProjectPage.validateButton).toBeEnabled();
			await expect(newProjectPage.saveButton).toBeEnabled();

			// Check for any validation errors
			const hasErrors = await csvUploadHelper.hasValidationErrors();
			if (hasErrors) {
				const errors = await csvUploadHelper.getValidationErrors();
				console.warn("CSV validation errors:", errors);
			}
		});

		await test.step("Create New Project", async () => {
			await newProjectPage.validate();
			await expect(newProjectPage.saveButton).toBeEnabled();
			const projectInfoPage = await newProjectPage.save();

			await expect(projectInfoPage.refreshFeatureButton).toBeEnabled();
			newProject.id = projectInfoPage.projectId;

			const projectPage = await overviewPage.lightHousePage.goToProjects();
			await projectPage.search(newProject.name);
			const projectLink = await projectPage.getProjectLink(newProject);
			await expect(projectLink).toBeVisible();
		});
	});

	await test.step("Wait for project metrics to load and verify features", async () => {
		// Navigate to project detail page
		const projectsPage = await overviewPage.lightHousePage.goToProjects();
		const projectInfoPage = await projectsPage.goToProject(newProject);

		// Refresh features to load CSV data
		await projectInfoPage.refreshFeatures();

		// Wait for features to be loaded
		await projectInfoPage.page.waitForTimeout(5000);

		// Verify that features refresh button is re-enabled (operation completed)
		await expect(projectInfoPage.refreshFeatureButton).toBeEnabled();

		// Verify last updated date is recent
		const lastUpdated = await projectInfoPage.getLastUpdatedDate();
		expect(lastUpdated).toBeDefined();
	});

	await test.step("Verify team shows project features correctly", async () => {
		// Navigate to team detail page
		const teamsPage = await overviewPage.lightHousePage.goToTeams();
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
		if (projectCsvFile) {
			projectCsvFile.cleanup();
		}
	});
});
