import { expect, test, testWithData } from '../../fixutres/LighthouseFixture';

testWithData('should show all teams on Teams Overview', async ({ testData, overviewPage }) => {
    const [team1, team2, team3] = testData.teams;

    const teamsPage = await overviewPage.lightHousePage.goToTeams();

    const teamLink1 = await teamsPage.getTeamLink(team1.name);
    const teamLink2 = await teamsPage.getTeamLink(team2.name);
    const teamLink3 = await teamsPage.getTeamLink(team3.name);

    await expect(teamLink1).toBeVisible();
    await expect(teamLink2).toBeVisible();
    await expect(teamLink3).toBeVisible();
});


testWithData('should filter teams on Teams Overview', async ({ testData, overviewPage }) => {
    const [team1, team2] = testData.teams;

    const teamsPage = await overviewPage.lightHousePage.goToTeams();

    await test.step(`Search for Team ${team1.name}`, async () => {
            await teamsPage.search(team1.name);

            const teamLink1 = await teamsPage.getTeamLink(team1.name);
            const teamLink2 = await teamsPage.getTeamLink(team2.name);

            await expect(teamLink1).toBeVisible();
            await expect(teamLink2).not.toBeVisible();
        });

        await test.step(`Search for Team ${team2.name}`, async () => {
            await teamsPage.search(team2.name);

            const teamLink1 = await teamsPage.getTeamLink(team1.name);
            const teamLink2 = await teamsPage.getTeamLink(team2.name);

            await expect(teamLink1).not.toBeVisible();
            await expect(teamLink2).toBeVisible();
        });

        await test.step('Search for not existing Team', async () => {
            await teamsPage.search('Jambalaya');

            const teamLink1 = await teamsPage.getTeamLink(team1.name);
            const teamLink2 = await teamsPage.getTeamLink(team2.name);

            await expect(teamLink1).not.toBeVisible();
            await expect(teamLink2).not.toBeVisible();
        });
});

testWithData('should open Team Info when clicking on Team', async ({ testData, overviewPage }) => {
    const [team1] = testData.teams;

    const teamsPage = await overviewPage.lightHousePage.goToTeams();

    const teamDetailPage = await teamsPage.goToTeam(team1.name);
    expect(teamDetailPage.page.url()).toContain(`/teams/${team1.id}`);
});

testWithData('should open Team Edit Page when clicking on Edit Icon', async ({ testData, overviewPage }) => {
    const [team1] = testData.teams;

    const teamsPage = await overviewPage.lightHousePage.goToTeams();

    const teamDetailPage = await teamsPage.editTeam(team1.name);
    expect(teamDetailPage.page.url()).toContain(`/teams/edit/${team1.id}`);
});

testWithData('should delete Team when clicking on Delete Icon and confirming', async ({ testData, overviewPage }) => {
    const [team1] = testData.teams;

    const teamsPage = await overviewPage.lightHousePage.goToTeams();
    
    await test.step(`Delete Team ${team1.name}`, async () => {
        const teamDeletionDialog = await teamsPage.deleteTeam(team1.name);
        await teamDeletionDialog.delete();
    });

    await test.step(`Search for Team ${team1.name}`, async () => {
        await teamsPage.search(team1.name);
        const teamLink = await teamsPage.getTeamLink(team1.name);

        await expect(teamLink).not.toBeVisible();
    });
});

testWithData('should not delete Team when clicking on Delete Icon and cancelling', async ({ testData, overviewPage }) => {
    const [team1] = testData.teams;

    const teamsPage = await overviewPage.lightHousePage.goToTeams();
    
    await test.step(`Delete Team ${team1.name}`, async () => {
        const teamDeletionDialog = await teamsPage.deleteTeam(team1.name);
        await teamDeletionDialog.cancel();
    });

    await test.step(`Search for Team ${team1.name}`, async () => {
        await teamsPage.search(team1.name);
        
        const teamLink = await teamsPage.getTeamLink(team1.name);
        await expect(teamLink).toBeVisible();
    });
});