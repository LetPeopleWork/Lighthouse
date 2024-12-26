import { expect, test, testWithData } from '../fixutres/LighthouseFixture';

[
    { teamIndex: 0, workTrackingSystem: 'Azure DevOps' },
    { teamIndex: 2, workTrackingSystem: 'Jira' },
].forEach(({ teamIndex, workTrackingSystem }) => {
    testWithData(`should allow save after validate when editing existing ${workTrackingSystem} team`, async ({ testData, overviewPage }) => {
        test.slow();
        const team = testData.teams[teamIndex];

        const teamsPage = await overviewPage.lightHousePage.goToTeams();
        const teamDetailPage = await teamsPage.editTeam(team);

        await expect(teamDetailPage.validateButton).toBeEnabled();
        await expect(teamDetailPage.saveButton).toBeDisabled();

        await teamDetailPage.validate();

        await expect(teamDetailPage.validateButton).toBeEnabled();
        await expect(teamDetailPage.saveButton).toBeEnabled();
    });
});

// Validation valid/invalid scenarios

// Create new Team
// --> check creation after save