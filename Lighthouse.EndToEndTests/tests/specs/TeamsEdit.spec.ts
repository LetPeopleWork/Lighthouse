import { TestConfig } from '../../playwright.config';
import { expect, test, testWithData } from '../fixutres/LighthouseFixture';
import { deleteTeam } from '../helpers/api/teams';
import { deleteWorkTrackingSystemConnectionByName } from '../helpers/api/workTrackingSystemConnections';

[
    { name: "Azure DevOps", index: 0 },
    { name: "Jira", index: 2 }
]
    .forEach(({ index, name }) => {
        testWithData(`should allow save after validate when editing existing ${name} team`, async ({ testData, overviewPage }) => {
            test.slow();
            const team = testData.teams[index];

            const teamsPage = await overviewPage.lightHousePage.goToTeams();
            const teamEditPage = await teamsPage.editTeam(team);

            await expect(teamEditPage.validateButton).toBeEnabled();
            await expect(teamEditPage.saveButton).toBeDisabled();

            await teamEditPage.validate();

            await expect(teamEditPage.validateButton).toBeEnabled();
            await expect(teamEditPage.saveButton).toBeEnabled();
        });
    });

testWithData("should disable validate button if not all mandatory fields are set", async ({ testData, overviewPage }) => {
    const team = testData.teams[0];

    const teamsPage = await overviewPage.lightHousePage.goToTeams();
    const teamEditPage = await teamsPage.editTeam(team);

    await expect(teamEditPage.validateButton).toBeEnabled();

    await test.step("Team Name should be mandatory", async () => {
        const oldName = team.name;
        await teamEditPage.setName('');
        await expect(teamEditPage.validateButton).toBeDisabled();

        await teamEditPage.setName(oldName);
        await expect(teamEditPage.validateButton).toBeEnabled();
    });

    await test.step("Throughput History should be mandatory and greater than 0", async () => {
        await teamEditPage.setThroughputHistory(0);
        await expect(teamEditPage.validateButton).toBeDisabled();

        await teamEditPage.setThroughputHistory(-1);
        await expect(teamEditPage.validateButton).toBeDisabled();

        await teamEditPage.setThroughputHistory(1);
        await expect(teamEditPage.validateButton).toBeEnabled();
    });

    await test.step("Work Item Query should be mandatory", async () => {
        await teamEditPage.setWorkItemQuery('');
        await expect(teamEditPage.validateButton).toBeDisabled();

        await teamEditPage.setWorkItemQuery('[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers"');
        await expect(teamEditPage.validateButton).toBeEnabled();
    });

    await test.step("Work Item Types should be mandatory and more than 1", async () => {
        await teamEditPage.removeWorkItemType('Bug');
        await expect(teamEditPage.validateButton).toBeEnabled();

        await teamEditPage.removeWorkItemType('User Story');
        await expect(teamEditPage.validateButton).toBeDisabled();

        await teamEditPage.addWorkItemType('Feature');
        await expect(teamEditPage.validateButton).toBeEnabled();
    });

    await test.step("Each state category should have at least one state", async () => {
        await teamEditPage.removeState('New');
        await expect(teamEditPage.validateButton).toBeDisabled();

        await teamEditPage.addState('Backlog', 'To Do');
        await expect(teamEditPage.validateButton).toBeEnabled();

        await teamEditPage.removeState('Active');
        await teamEditPage.removeState('Resolved');
        await expect(teamEditPage.validateButton).toBeDisabled();

        await teamEditPage.addState('In Progress', 'Doing');
        await expect(teamEditPage.validateButton).toBeEnabled();

        await teamEditPage.removeState('Closed');
        await expect(teamEditPage.validateButton).toBeDisabled();


        await teamEditPage.addState('Done', 'Done');
        await expect(teamEditPage.validateButton).toBeEnabled();
    });

    await test.step("Advanced Configuration should not be mandatory", async () => {
        await teamEditPage.toggleAdvancedConfiguration();

        await teamEditPage.setFeatureWip(0);
        await expect(teamEditPage.validateButton).toBeEnabled();

        await teamEditPage.setFeatureWip(-1);
        await expect(teamEditPage.validateButton).toBeEnabled();

        await teamEditPage.enableAutomaticallyAdjustFeatureWIP();
        await expect(teamEditPage.validateButton).toBeEnabled();

        await teamEditPage.disableAutomaticallyAdjustFeatureWIP();
        await expect(teamEditPage.validateButton).toBeEnabled();

        await teamEditPage.setRelationCustomField('CUSTOMFIELD');
        await expect(teamEditPage.validateButton).toBeEnabled();
        await teamEditPage.setRelationCustomField('');
        await expect(teamEditPage.validateButton).toBeEnabled();
    });
});

const newTeamConfigurations = [
    {
        name: "Jira",
        workTrackingSystemIndex: 1,
        teamConfiguration: {
            validWorkItemTypes: ['Story', 'Bug'],
            invalidWorkItemTypes: ['User Story', 'Bug'],
            validStates: { toDo: ['To Do'], doing: ['In Progress'], done: ['Done'] },
            invalidStates: { toDo: ['New'], doing: ['Active'], done: ['Closed'] },
            validQuery: 'project = "LGHTHSDMO" AND labels = "Lagunitas"',
            invalidQuery: 'project = "LGHTHSDMO" AND tags = "Lagunitas"'
        },
        workTrackingSystemOptions: [
            { field: "Jira Url", value: "https://letpeoplework.atlassian.net" },
            { field: "Username", value: "benjhuser@gmail.com" },
            { field: "Api Token", value: TestConfig.JiraToken },
        ]
    },
{
    name: "AzureDevOps",
        workTrackingSystemIndex: 0,
            teamConfiguration: {
        validWorkItemTypes: ['User Story', 'Bug'],
            invalidWorkItemTypes: ['Story'],
                validStates: { toDo: ['New'], doing: ['Active'], done: ['Closed'] },
        invalidStates: { toDo: ['To Do'], doing: ['In Progress'], done: ['Done'] },
        validQuery: '[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers"',
            invalidQuery: '[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Lighthouse Demo\\Binary Blazers"'
    },
    workTrackingSystemOptions: [
        { field: "Azure DevOps Url", value: "https://dev.azure.com/letpeoplework" },
        { field: "Personal Access Token", value: TestConfig.AzureDevOpsToken },
    ]
},
];

newTeamConfigurations.forEach(({ name, workTrackingSystemIndex, teamConfiguration }) => {
    testWithData(`should allow to create a new team for ${name}`, async ({ testData, overviewPage, request }) => {
        test.slow();
        let teamsPage = await overviewPage.lightHousePage.goToTeams();
        const newTeamPage = await teamsPage.addNewTeam();

        const newTeam = { id: 0, name: `My New ${name} team` };

        await test.step("Add general configuration", async () => {
            await newTeamPage.setName(newTeam.name);
            await newTeamPage.setThroughputHistory(20);
            await newTeamPage.setWorkItemQuery(teamConfiguration.validQuery);

            // Expect Validation to be disabled because mandatory config is still missing
            await expect(newTeamPage.validateButton).toBeDisabled();
        });

        await test.step("Add Work Item Type Configuration", async () => {
            await newTeamPage.resetWorkItemTypes(['User Story', 'Bug'], teamConfiguration.validWorkItemTypes);

            // Expect Validation to be disabled because mandatory config is still missing
            await expect(newTeamPage.validateButton).toBeDisabled();
        });

        await test.step("Add State Configuration", async () => {
            await newTeamPage.resetStates(
                { toDo: ['New', 'Proposed', 'To Do'], doing: ['Active', 'Resolved', 'In Progress', 'Committed'], done: ['Done', 'Closed'] },
                teamConfiguration.validStates
            );

            // Expect Validation to be disabled because mandatory config is still missing
            await expect(newTeamPage.validateButton).toBeDisabled();
        });

        await test.step("Select Work Tracking System", async () => {
            const workTrackingSystem = testData.connections[workTrackingSystemIndex];
            await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);

            // Now we have all default configuration set
            await expect(newTeamPage.validateButton).toBeEnabled();
        });

        await test.step("Validate Settings", async () => {
            await newTeamPage.validate();
            await expect(newTeamPage.validateButton).toBeEnabled();
            await expect(newTeamPage.saveButton).toBeEnabled();
        });

        await test.step("Invalidate Work Item Query", async () => {
            await newTeamPage.setWorkItemQuery(teamConfiguration.invalidQuery);
            await newTeamPage.validate();
            await expect(newTeamPage.validateButton).toBeEnabled();
            await expect(newTeamPage.saveButton).toBeDisabled();

            await newTeamPage.setWorkItemQuery(teamConfiguration.validQuery);
        });

        await test.step("Invalidate Work Item Types", async () => {
            await newTeamPage.resetWorkItemTypes(teamConfiguration.validWorkItemTypes, teamConfiguration.invalidWorkItemTypes);
            await newTeamPage.validate();
            await expect(newTeamPage.validateButton).toBeEnabled();
            await expect(newTeamPage.saveButton).toBeDisabled();

            await newTeamPage.resetWorkItemTypes(teamConfiguration.invalidWorkItemTypes, teamConfiguration.validWorkItemTypes);
        });

        await test.step("Invalidate States", async () => {
            await newTeamPage.resetStates(teamConfiguration.validStates, teamConfiguration.invalidStates);
            await newTeamPage.validate();
            await expect(newTeamPage.validateButton).toBeEnabled();
            await expect(newTeamPage.saveButton).toBeDisabled();

            await newTeamPage.resetStates(teamConfiguration.invalidStates, teamConfiguration.validStates);
        });

        await test.step("Create New Team", async () => {
            await newTeamPage.validate();
            await expect(newTeamPage.saveButton).toBeEnabled();
            const teamInfoPage = await newTeamPage.save();

            await expect(teamInfoPage.updateTeamDataButton).toBeDisabled();
            newTeam.id = teamInfoPage.teamId;

            teamsPage = await overviewPage.lightHousePage.goToTeams();
            await teamsPage.search(newTeam.name);
            const teamLink = await teamsPage.getTeamLink(newTeam);
            await expect(teamLink).toBeVisible();
        });

        await deleteTeam(request, newTeam.id);
    });
});

newTeamConfigurations.forEach(({ name: workTrackingSystemName, teamConfiguration, workTrackingSystemOptions }) => {
    testWithData(`should allow to create a new team with a new Work Tracking System ${workTrackingSystemName}`, async ({ testData, overviewPage, request }) => {
        test.slow();

        const teams = testData.teams;
        console.log(`Initiating test with ${teams.length} teams so that we do not show tutorial...`);

        const teamsPage = await overviewPage.lightHousePage.goToTeams();
        let newTeamPage = await teamsPage.addNewTeam();

        await test.step("Add Valid Configuration for new team", async () => {
            await newTeamPage.setName(`My New ${workTrackingSystemName} team`);
            await newTeamPage.setWorkItemQuery(teamConfiguration.validQuery);
            await newTeamPage.resetWorkItemTypes(['User Story', 'Bug'], teamConfiguration.validWorkItemTypes);
            await newTeamPage.resetStates(
                { toDo: ['New', 'Proposed', 'To Do'], doing: ['Active', 'Resolved', 'In Progress', 'Committed'], done: ['Done', 'Closed'] },
                teamConfiguration.validStates
            );
        });

        const newWorkTrackingSystemConnectionName = `New ${workTrackingSystemName} Work Tracking System Connection`;
        await test.step("Add new Work Tracking System", async () => {
            let newWorkTrackingSystemDialog = await newTeamPage.addNewWorkTrackingSystem();

            newTeamPage = await newWorkTrackingSystemDialog.cancel();

            // No New Work Tracking System
            await expect(newTeamPage.validateButton).toBeDisabled();

            newWorkTrackingSystemDialog = await newTeamPage.addNewWorkTrackingSystem();
            await newWorkTrackingSystemDialog.selectWorkTrackingSystem(workTrackingSystemName);

            for (const option of workTrackingSystemOptions){
                await newWorkTrackingSystemDialog.setWorkTrackingSystemOption(option.field, option.value);
            }

            await newWorkTrackingSystemDialog.setConnectionName(newWorkTrackingSystemConnectionName);

            await newWorkTrackingSystemDialog.validate();
            await expect(newWorkTrackingSystemDialog.createButton).toBeEnabled();

            newTeamPage = await newWorkTrackingSystemDialog.create();

            await expect(newTeamPage.validateButton).toBeEnabled();
            await expect(newTeamPage.saveButton).toBeDisabled();

            await newTeamPage.validate();
            await expect(newTeamPage.validateButton).toBeEnabled();
            await expect(newTeamPage.saveButton).toBeEnabled();
        });

        await deleteWorkTrackingSystemConnectionByName(request, newWorkTrackingSystemConnectionName)
    });
});