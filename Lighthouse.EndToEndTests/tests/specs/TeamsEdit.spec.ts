import { expect, testWithData } from '../fixutres/LighthouseFixture';

testWithData('should allow save after validate when editing existing team', async ({ testData, overviewPage }) => {
    const [team] = testData.teams;

    const teamsPage = await overviewPage.lightHousePage.goToTeams();
    const teamDetailPage = await teamsPage.editTeam(team);

    await expect(teamDetailPage.validateButton).toBeEnabled();
    await expect(teamDetailPage.saveButton).toBeDisabled();

    await teamDetailPage.validate();

    await expect(teamDetailPage.validateButton).toBeEnabled();
    await expect(teamDetailPage.saveButton).toBeEnabled();
});

// Validation valid/invalid scenarios

// Create new Team
// --> check creation after save