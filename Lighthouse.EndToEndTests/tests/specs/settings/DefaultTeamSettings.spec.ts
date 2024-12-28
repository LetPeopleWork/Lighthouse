import { expect, test, testWithRestoredDefaultSettings } from '../../fixutres/LighthouseFixture';
import { generateRandomName } from '../../helpers/names';
import { TeamEditPage } from '../../models/teams/TeamEditPage';

test("Modify Default Team Settings should not allow Save if Mandatory Options are missing", async ({ overviewPage }) => {
    const settingsPage = await overviewPage.lighthousePage.goToSettings();
    const teamSettingsPage = await settingsPage.gotToDefaultTeamSettings();

    await test.step("Save Enabled and No Validation Possible", async () => {
        await expect(teamSettingsPage.validateButton).not.toBeVisible();
        await expect(teamSettingsPage.saveButton).toBeEnabled();
    });

    await test.step("General Configuration", async () => {
        await teamSettingsPage.setName('');
        await expect(teamSettingsPage.saveButton).toBeDisabled();

        await teamSettingsPage.setName(generateRandomName());
        await expect(teamSettingsPage.saveButton).toBeEnabled();

        await teamSettingsPage.setThroughputHistory(0);
        await expect(teamSettingsPage.saveButton).toBeDisabled();

        await teamSettingsPage.setThroughputHistory(30);
        await expect(teamSettingsPage.saveButton).toBeEnabled();

        await teamSettingsPage.setWorkItemQuery('Query!');
        await expect(teamSettingsPage.saveButton).toBeEnabled();

        await teamSettingsPage.setWorkItemQuery('');
        await expect(teamSettingsPage.saveButton).toBeEnabled();
    });

    await test.step("Work Item Types", async () => {
        const existingWorkItemTypes = ["User Story", "Bug"];
        await teamSettingsPage.resetWorkItemTypes(existingWorkItemTypes, []);
        await expect(teamSettingsPage.saveButton).toBeDisabled();

        await teamSettingsPage.resetWorkItemTypes([], existingWorkItemTypes);
        await expect(teamSettingsPage.saveButton).toBeEnabled();
    });

    await test.step("States", async () => {
        const existingToDoStates = ["New", "Proposed", "To Do"];
        const existingDoingStates = ["Active", "Resolved", "In Progress", "Committed"];
        const existingDoneStates = ["Done", "Closed"];

        /* To Do States */
        for (const state of existingToDoStates) {
            await teamSettingsPage.removeState(state);
        }
        await expect(teamSettingsPage.saveButton).toBeDisabled();

        for (const state of existingToDoStates) {
            await teamSettingsPage.addState(state, 'To Do');
        }
        await expect(teamSettingsPage.saveButton).toBeEnabled();

        /* Doing States */
        for (const state of existingDoingStates) {
            await teamSettingsPage.removeState(state);
        }
        await expect(teamSettingsPage.saveButton).toBeDisabled();

        for (const state of existingDoingStates) {
            await teamSettingsPage.addState(state, 'Doing');
        }
        await expect(teamSettingsPage.saveButton).toBeEnabled();

        /* Done States */
        for (const state of existingDoneStates) {
            await teamSettingsPage.removeState(state);
        }
        await expect(teamSettingsPage.saveButton).toBeDisabled();

        for (const state of existingDoneStates) {
            await teamSettingsPage.addState(state, 'Done');
        }
        await expect(teamSettingsPage.saveButton).toBeEnabled();
    });
});

testWithRestoredDefaultSettings("Change default team settings should affect new team creation", async ({ overviewPage, defaultSettings }) => {
    test.fail(defaultSettings == null, "Expected to have default settings initiatilized");

    let settingsPage = await overviewPage.lighthousePage.goToSettings();
    let teamSettingsPage = await settingsPage.gotToDefaultTeamSettings();

    const newName = generateRandomName();
    const newThroughputHistory = 90;
    const newWorkItemQuery = 'Query!';
    const newWorkItemType = 'Fancy Type';
    const newtoDoState = "TODO";
    const newDoingState = "DOING";
    const newDoneState = "DONE";
    const newFeatureWIP = 3;
    const newRelationCustomField = "RELATION!";

    await test.step('Adjust Settings and Save', async () => {
        await teamSettingsPage.setName(newName);
        await teamSettingsPage.setThroughputHistory(newThroughputHistory);
        await teamSettingsPage.setWorkItemQuery(newWorkItemQuery);

        const existingWorkItemTypes = ["User Story", "Bug"];
        await teamSettingsPage.resetWorkItemTypes(existingWorkItemTypes, [newWorkItemType]);

        const existingToDoStates = ["New", "Proposed", "To Do"];
        const existingDoingStates = ["Active", "Resolved", "In Progress", "Committed"];
        const existingDoneStates = ["Done", "Closed"];
        await teamSettingsPage.resetStates({ toDo: existingToDoStates, doing: existingDoingStates, done: existingDoneStates }, { toDo: [newtoDoState], doing: [newDoingState], done: [newDoneState] });

        await teamSettingsPage.toggleAdvancedConfiguration();
        await teamSettingsPage.setFeatureWip(newFeatureWIP);
        await teamSettingsPage.enableAutomaticallyAdjustFeatureWIP();
        await teamSettingsPage.setRelationCustomField(newRelationCustomField);

        await expect(teamSettingsPage.saveButton).toBeEnabled();
        await teamSettingsPage.save();

        await expect(teamSettingsPage.saveButton).toBeEnabled();
    });


    async function verifyTeamSettings(teamSettings: TeamEditPage) {
        const name = await teamSettings.getName();
        expect(name).toBe(newName);

        const throughputHistory = await teamSettings.getThroughputHistory();
        expect(throughputHistory).toBe(newThroughputHistory);

        const query = await teamSettings.getWorkItemQuery();
        expect(query).toBe(newWorkItemQuery);

        const workItemType = teamSettings.getWorkItemType(newWorkItemType);
        await expect(workItemType).toBeVisible();

        const toDoState = teamSettings.getState(newtoDoState);
        await expect(toDoState).toBeVisible();

        const donigState = teamSettings.getState(newDoingState);
        await expect(donigState).toBeVisible();

        const doneState = teamSettings.getState(newDoneState);
        await expect(doneState).toBeVisible();

        await teamSettings.toggleAdvancedConfiguration();
        const featureWip = await teamSettings.getFeatureWip();
        expect(featureWip).toBe(newFeatureWIP);

        await expect(teamSettings.automaticallyAdjustFeatureWIPCheckBox).toBeChecked();

        const relationField = await teamSettings.getRelationCustomField();
        expect(relationField).toBe(newRelationCustomField);
    }

    await test.step("Make sure settings are updated", async () => {
        await overviewPage.lightHousePage.goToOverview();
        settingsPage = await overviewPage.lighthousePage.goToSettings();
        teamSettingsPage = await settingsPage.gotToDefaultTeamSettings();

        await verifyTeamSettings(teamSettingsPage);
    });

    await test.step("Make sure new Teams have the default settings", async () => {
        teamSettingsPage = await overviewPage.lightHousePage.createNewTeam();

        await verifyTeamSettings(teamSettingsPage);
    });
});