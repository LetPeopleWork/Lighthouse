import { expect, test, testWithData } from '../fixutres/LighthouseFixture';

[
    { name: "Azure DevOps", index: 0 },
    { name: "Jira", index: 2 }
]
    .forEach(({ index, name }) => {
        testWithData(`should update Team Data for ${name} team on click`, async ({ testData, overviewPage }) => {
            test.slow();

            const team = testData.teams[index];

            const teamsPage = await overviewPage.lightHousePage.goToTeams();
            const teamDetailPage = await teamsPage.goToTeam(team);

            await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

            await teamDetailPage.updateTeamData();
            await expect(teamDetailPage.updateTeamDataButton).toBeDisabled();

            // Wait for update to be done
            await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

            // Make sure the features in progress is correct
            const featuresInProgress = await teamDetailPage.getFeaturesInProgress();
            expect(featuresInProgress).toBe(1);

            const lastUpdatedDate = await teamDetailPage.getLastUpdatedDate();
            const now = new Date();

            // Make sure lastUpdated is within the last 10 seconds
            expect(Math.abs(lastUpdatedDate.getUTCMilliseconds() - now.getUTCMilliseconds())).toBeLessThanOrEqual(10000);
        });
    });

testWithData('should open Team Edit Page when clicking on Edit Button', async ({ testData, overviewPage }) => {
    const [team] = testData.teams;

    const teamsPage = await overviewPage.lightHousePage.goToTeams();
    const teamDetailPage = await teamsPage.goToTeam(team);

    const teamEditPage = await teamDetailPage.editTeam();
    expect(teamEditPage.page.url()).toContain(`/teams/edit/${team.id}`);
});

testWithData(`should show Manual When and How Many Forecast for team`, async ({ testData, overviewPage }) => {
    test.slow();
    const team = testData.teams[0];

    const teamsPage = await overviewPage.lightHousePage.goToTeams();
    const teamDetailPage = await teamsPage.goToTeam(team);

    await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

    await teamDetailPage.updateTeamData();
    await expect(teamDetailPage.updateTeamDataButton).toBeDisabled();

    // Wait for update to be done
    await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

    const howMany = 20;
    const likelihood = await teamDetailPage.forecast(howMany);

    expect(likelihood).toBeGreaterThan(0);
});