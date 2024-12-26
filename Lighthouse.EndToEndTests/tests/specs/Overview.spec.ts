import { expect, test, testWithData } from '../fixutres/LighthouseFixture';

testWithData('should show all projects on dashboard', async ({ testData, overviewPage }) => {
    const [project1, project2] = testData.projects;

    await expect(await overviewPage.getProjectLink(project1)).toBeVisible();
    await expect(await overviewPage.getProjectLink(project2)).toBeVisible();
});

testWithData('should filter projects on dashboard', async ({ testData, overviewPage }) => {
    const [project1, project2] = testData.projects;

    await test.step(`Search for Project ${project1.name}`, async () => {
        await overviewPage.search(project1.name);

        const projectLink = await overviewPage.getProjectLink(project1);

        await expect(projectLink).toBeVisible();
    });

    await test.step(`Search for Project ${project2.name}`, async () => {
        await overviewPage.search(project2.name);

        const projectLink = await overviewPage.getProjectLink(project2);
        await expect(projectLink).toBeVisible();
    });

    await test.step('Search for not existing Project', async () => {
        await overviewPage.search('Jambalaya');

        const projectLink1 = await overviewPage.getProjectLink(project1);
        const projectLink2 = await overviewPage.getProjectLink(project2);

        await expect(projectLink1).not.toBeVisible();
        await expect(projectLink2).not.toBeVisible();
    });

    await test.step('Clear Search', async () => {
        await overviewPage.search('');

        const projectLink1 = await overviewPage.getProjectLink(project1);
        const projectLink2 = await overviewPage.getProjectLink(project2);

        await expect(projectLink1).toBeVisible();
        await expect(projectLink2).toBeVisible();

    });
});

testWithData('should show involved teams for projects', async ({ testData, overviewPage }) => {
    const [project1, project2, project3] = testData.projects;
    const [team1, team2, team3] = testData.teams;

    await test.step(`Check Teams for Project ${project1.name}`, async () => {
        await overviewPage.search(project1.name);

        const involvedTeams = await overviewPage.getTeamsForProject(project1);
        expect(involvedTeams).toContain(team1.name);
        expect(involvedTeams).not.toContain(team2.name);
        expect(involvedTeams).not.toContain(team3.name);
    });

    await test.step(`Check Teams for Project ${project2.name}`, async () => {
        await overviewPage.search(project2.name);

        const involvedTeams = await overviewPage.getTeamsForProject(project2);
        expect(involvedTeams).toContain(team1.name);
        expect(involvedTeams).toContain(team2.name);
        expect(involvedTeams).not.toContain(team3.name);
    });

    await test.step(`Check Teams for Project ${project3.name}`, async () => {
        await overviewPage.search(project3.name);

        const involvedTeams = await overviewPage.getTeamsForProject(project3);
        expect(involvedTeams).not.toContain(team1.name);
        expect(involvedTeams).not.toContain(team2.name);
        expect(involvedTeams).toContain(team3.name);
    });
});

testWithData('should open Project Info when clicking on Project', async ({ testData, overviewPage }) => {
    const [project1] = testData.projects;

    await overviewPage.search(project1.name);
    const projectDetailPage = await overviewPage.goToProject(project1);

    expect(projectDetailPage.page.url()).toContain(`/projects/${project1.id}`);
});

testWithData('should open Team Info when clicking on Team', async ({ testData, overviewPage }) => {
    const [project1] = testData.projects;
    const [team1] = testData.teams;

    await overviewPage.search(project1.name);
    const teamDetailPage = await overviewPage.goToTeam(team1);

    expect(teamDetailPage.page.url()).toContain(`/teams/${team1.id}`);
});