import { TestConfig } from "../../../playwright.config";
import { expect, test } from "../../fixutres/LighthouseFixture";
import { generateRandomName } from "../../helpers/names";

test("should be able to handle a team defined in Linear", async ({
	overviewPage,
}) => {
	const settingsPage = await overviewPage.lightHousePage.goToSettings();

	await test.step("Enable Linear Integration", async () => {
		const featuresTab = await settingsPage.goToOptionalFeatures();

		await featuresTab.enableFeature("LinearIntegration");
	});

	const workTrackingSystem = {
		name: generateRandomName(),
	};

	await test.step("Create Linear Work Tracking System Connection", async () => {
		const workTrackingSystemConnections =
			await settingsPage.goToWorkTrackingSystems();
		const addWorkTrackingSystemConnectionDialog =
			await workTrackingSystemConnections.addNewWorkTrackingSystem();

		await addWorkTrackingSystemConnectionDialog.selectWorkTrackingSystem(
			"Linear",
		);

		await addWorkTrackingSystemConnectionDialog.setConnectionName(
			workTrackingSystem.name,
		);

		await addWorkTrackingSystemConnectionDialog.setWorkTrackingSystemOption(
			"ApiKey",
			TestConfig.LinearApiKey,
		);

		await addWorkTrackingSystemConnectionDialog.validate();
		await addWorkTrackingSystemConnectionDialog.create();
	});

	const newTeam = { id: 0, name: "LighthouseDemo" };

	await test.step("Create Linear Team", async () => {
		const newTeamPage = await overviewPage.lightHousePage.createNewTeam();

		await test.step("Add general configuration", async () => {
			await newTeamPage.setName(newTeam.name);
			await newTeamPage.setThroughputHistory(20);
			await newTeamPage.setWorkItemQuery(newTeam.name);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.validateButton).toBeDisabled();
		});

		await test.step("Add Work Item Type Configuration", async () => {
			await newTeamPage.resetWorkItemTypes(
				["User Story", "Bug"],
				["Default", "Bug"],
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
					toDo: ["Backlog", "Planned"],
					doing: ["Development", "Resolved"],
					done: ["Closed"],
				},
			);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newTeamPage.validateButton).toBeDisabled();
		});

		await test.step("Select Work Tracking System", async () => {
			await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);

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

			const teamsPage = await overviewPage.lightHousePage.goToTeams();
			await teamsPage.search(newTeam.name);
			const teamLink = await teamsPage.getTeamLink(newTeam.name);
			await expect(teamLink).toBeVisible();
		});
	});

	await test.step("Create Linear Project", async () => {
		const newProject = {
			id: 0,
			name: "My Demo Project",
		};

		const projectPage = await overviewPage.lightHousePage.goToProjects();

		const newProjectPage = await projectPage.addNewProject();
		await test.step("Add general configuration", async () => {
			await newProjectPage.setName(newProject.name);
			await newProjectPage.setWorkItemQuery(newProject.name);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newProjectPage.validateButton).toBeDisabled();
		});

		await test.step("Add Work Item Type Configuration", async () => {
			await newProjectPage.resetWorkItemTypes(["Epic"], ["Default"]);

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
					toDo: ["Backlog", "Planned"],
					doing: ["Development", "Resolved"],
					done: ["Closed"],
				},
			);

			// Expect Validation to be disabled because mandatory config is still missing
			await expect(newProjectPage.validateButton).toBeDisabled();
		});

		await test.step("Select Work Tracking System", async () => {
			await newProjectPage.selectWorkTrackingSystem(workTrackingSystem.name);

			// Now we have all default configuration set
			await expect(newProjectPage.validateButton).toBeEnabled();
		});

		await test.step("Validate Settings", async () => {
			await newProjectPage.validate();
			await expect(newProjectPage.validateButton).toBeEnabled();
			await expect(newProjectPage.saveButton).toBeEnabled();
		});

		await test.step("Create New Project", async () => {
			await newProjectPage.validate();
			await expect(newProjectPage.saveButton).toBeEnabled();
			const projectInfoPage = await newProjectPage.save();

			await expect(projectInfoPage.refreshFeatureButton).toBeDisabled();
			newProject.id = projectInfoPage.projectId;

			const projectPage = await overviewPage.lightHousePage.goToProjects();
			await projectPage.search(newProject.name);
			const projectLink = await projectPage.getProjectLink(newProject);
			await expect(projectLink).toBeVisible();
		});
	});
});
