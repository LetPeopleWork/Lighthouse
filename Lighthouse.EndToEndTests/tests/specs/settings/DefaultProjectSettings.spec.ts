import {
	expect,
	test,
	testWithRestoredDefaultSettings,
} from "../../fixutres/LighthouseFixture";
import { generateRandomName } from "../../helpers/names";
import type { ProjectEditPage } from "../../models/projects/ProjectEditPage";

test("Modify Default Projects Settings should not allow Save if Mandatory Options are missing", async ({
	overviewPage,
}) => {
	const settingsPage = await overviewPage.lighthousePage.goToSettings();
	const projectSettingsPage = await settingsPage.goToDefaultProjectSettings();

	await test.step("Save Enabled and No Validation Possible", async () => {
		await expect(projectSettingsPage.validateButton).not.toBeVisible();
		await expect(projectSettingsPage.saveButton).toBeEnabled();
	});

	await test.step("General Configuration", async () => {
		await projectSettingsPage.setName("");
		await expect(projectSettingsPage.saveButton).toBeDisabled();

		await projectSettingsPage.setName(generateRandomName());
		await expect(projectSettingsPage.saveButton).toBeEnabled();

		await projectSettingsPage.setWorkItemQuery("Query!");
		await expect(projectSettingsPage.saveButton).toBeEnabled();

		await projectSettingsPage.setWorkItemQuery("");
		await expect(projectSettingsPage.saveButton).toBeEnabled();
	});

	await test.step("Work Item Types", async () => {
		const existingWorkItemTypes = ["Epic"];
		await projectSettingsPage.resetWorkItemTypes(existingWorkItemTypes, []);
		await expect(projectSettingsPage.saveButton).toBeDisabled();

		await projectSettingsPage.resetWorkItemTypes([], existingWorkItemTypes);
		await expect(projectSettingsPage.saveButton).toBeEnabled();
	});

	await test.step("States", async () => {
		const existingToDoStates = ["New", "Proposed", "To Do"];
		const existingDoingStates = [
			"Active",
			"Resolved",
			"In Progress",
			"Committed",
		];
		const existingDoneStates = ["Done", "Closed"];

		/* To Do States */
		for (const state of existingToDoStates) {
			await projectSettingsPage.removeState(state);
		}
		await expect(projectSettingsPage.saveButton).toBeDisabled();

		for (const state of existingToDoStates) {
			await projectSettingsPage.addState(state, "To Do");
		}
		await expect(projectSettingsPage.saveButton).toBeEnabled();

		/* Doing States */
		for (const state of existingDoingStates) {
			await projectSettingsPage.removeState(state);
		}
		await expect(projectSettingsPage.saveButton).toBeDisabled();

		for (const state of existingDoingStates) {
			await projectSettingsPage.addState(state, "Doing");
		}
		await expect(projectSettingsPage.saveButton).toBeEnabled();

		/* Done States */
		for (const state of existingDoneStates) {
			await projectSettingsPage.removeState(state);
		}
		await expect(projectSettingsPage.saveButton).toBeDisabled();

		for (const state of existingDoneStates) {
			await projectSettingsPage.addState(state, "Done");
		}
		await expect(projectSettingsPage.saveButton).toBeEnabled();
	});
});

testWithRestoredDefaultSettings(
	"Change default team settings should affect new team creation",
	async ({ overviewPage, defaultSettings }) => {
		test.fail(
			defaultSettings == null,
			"Expected to have default settings initiatilized",
		);

		let settingsPage = await overviewPage.lighthousePage.goToSettings();
		let projectSettingsPage = await settingsPage.goToDefaultProjectSettings();

		const newName = generateRandomName();
		const newWorkItemQuery = "Query!";
		const newWorkItemType = "Fancy Type";
		const newtoDoState = "TODO";
		const newDoingState = "DOING";
		const newDoneState = "DONE";
		const newUnparentedWorkItemQuery = "Can I have some more?";
		const newFeatureSizePercentile = 77;
		const newHistoricalFeatureWorkItemQuery =
			"What would the elder features think about this?";
		const newSizeEstimateField = "CUSTOMSIZEFIELD";
		const newSizeOverrideState = "OVERRIDE";
		const newFeatureOwnerField = "labels";
		const newTag = "tag";

		await test.step("Adjust Settings and Save", async () => {
			await projectSettingsPage.setName(newName);
			await projectSettingsPage.setWorkItemQuery(newWorkItemQuery);

			const existingWorkItemTypes = ["Epic"];
			await projectSettingsPage.resetWorkItemTypes(existingWorkItemTypes, [
				newWorkItemType,
			]);

			const existingToDoStates = ["New", "Proposed", "To Do"];
			const existingDoingStates = [
				"Active",
				"Resolved",
				"In Progress",
				"Committed",
			];
			const existingDoneStates = ["Done", "Closed"];
			await projectSettingsPage.resetStates(
				{
					toDo: existingToDoStates,
					doing: existingDoingStates,
					done: existingDoneStates,
				},
				{ toDo: [newtoDoState], doing: [newDoingState], done: [newDoneState] },
			);

			await projectSettingsPage.addTag(newTag);

			await projectSettingsPage.toggleUnparentedWorkItemConfiguration();
			await projectSettingsPage.setUnparentedWorkItemQuery(
				newUnparentedWorkItemQuery,
			);

			await projectSettingsPage.toggleDefaultFeatureSizeConfiguration();
			await projectSettingsPage.useHistoricalFeatureSize();
			await projectSettingsPage.setHistoricalFeatureSizePercentile(
				newFeatureSizePercentile,
			);
			await projectSettingsPage.setHistoricalFeatureSizeQuery(
				newHistoricalFeatureWorkItemQuery,
			);
			await projectSettingsPage.setSizeEstimateField(newSizeEstimateField);
			await projectSettingsPage.addSizeOverrideState(newSizeOverrideState);

			await projectSettingsPage.toggleOwnershipSettings();
			await projectSettingsPage.setFeatureOwnerField(newFeatureOwnerField);

			await expect(projectSettingsPage.saveButton).toBeEnabled();
			await projectSettingsPage.save();

			await expect(projectSettingsPage.saveButton).toBeEnabled();
		});

		async function verifyTeamSettings(projectSettings: ProjectEditPage) {
			const name = await projectSettings.getName();
			expect(name).toBe(newName);

			const query = await projectSettings.getWorkItemQuery();
			expect(query).toBe(newWorkItemQuery);

			const workItemType = projectSettings.getWorkItemType(newWorkItemType);
			await expect(workItemType).toBeVisible();

			const toDoState = projectSettings.getState(newtoDoState);
			await expect(toDoState).toBeVisible();

			const donigState = projectSettings.getState(newDoingState);
			await expect(donigState).toBeVisible();

			const doneState = projectSettings.getState(newDoneState);
			await expect(doneState).toBeVisible();

			const tag = projectSettings.getTag(newTag);
			await expect(tag).toBeVisible();

			await projectSettings.toggleUnparentedWorkItemConfiguration();
			const unparentedWorkItemQuery =
				await projectSettings.getUnparentedWorkItemQuery();
			expect(unparentedWorkItemQuery).toBe(newUnparentedWorkItemQuery);

			await projectSettings.toggleDefaultFeatureSizeConfiguration();
			await expect(
				projectSettings.useHistoricalFeatureSizeToggle,
			).toBeChecked();

			const percentile =
				await projectSettings.getHistoricalFeatureSizePercentile();
			expect(percentile).toBe(newFeatureSizePercentile);

			const historicalFeatureSizeQuery =
				await projectSettings.getHistoricalFeatureSizeQuery();
			expect(historicalFeatureSizeQuery).toBe(
				newHistoricalFeatureWorkItemQuery,
			);

			const sizeEstimateField = await projectSettings.getSizeEstimateField();
			expect(sizeEstimateField).toBe(newSizeEstimateField);

			const overrideState = projectSettings.getState(newSizeOverrideState);
			await expect(overrideState).toBeVisible();

			await projectSettings.toggleOwnershipSettings();
			const featureOwnerField = await projectSettings.getFeatureOwnerField();
			expect(featureOwnerField).toBe(newFeatureOwnerField);
		}

		await test.step("Make sure settings are updated", async () => {
			await overviewPage.lightHousePage.goToOverview();
			settingsPage = await overviewPage.lighthousePage.goToSettings();
			projectSettingsPage = await settingsPage.goToDefaultProjectSettings();

			await verifyTeamSettings(projectSettingsPage);
		});

		await test.step("Make sure new Projects have the default settings", async () => {
			projectSettingsPage =
				await overviewPage.lightHousePage.createNewProject();

			await verifyTeamSettings(projectSettingsPage);
		});
	},
);
