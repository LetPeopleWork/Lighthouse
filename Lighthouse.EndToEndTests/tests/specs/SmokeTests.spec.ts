import { expect, test, testWithData } from '../fixutres/LighthouseFixture';

test('should open all pages from the header', async ({ overviewPage }) => {
    await overviewPage.lightHousePage.goToTeams();
    await overviewPage.lighthousePage.goToProjects();
    await overviewPage.lighthousePage.goToSettings();
});

testWithData('should show all projects on dashboard', async ({ testData, overviewPage }) => {
    const [project1, project2] = testData.projects;

    await test.step(`Search for Project ${project1.name}`, async () => {
        await overviewPage.search(project1.name);
        expect(await overviewPage.isProjectAvailable(project1)).toBeTruthy();
    });

    await test.step(`Search for Project ${project2.name}`, async () => {
        await overviewPage.search(project2.name);
        expect(await overviewPage.isProjectAvailable(project2)).toBeTruthy();
    });

    await test.step('Search for not existing Project', async () => {
        await overviewPage.search('Jambalaya');
        expect(await overviewPage.isProjectAvailable(project1)).toBeFalsy();
        expect(await overviewPage.isProjectAvailable(project2)).toBeFalsy();
    });

    await test.step('Clear Search', async () => {
        await overviewPage.search('');
        expect(await overviewPage.isProjectAvailable(project1)).toBeTruthy();
        expect(await overviewPage.isProjectAvailable(project2)).toBeTruthy();

    });
});

test('should open the contributors page', async ({ overviewPage }) => {
    const contributorsPage = await overviewPage.lightHousePage.goToContributors();

    const pageTitle = await contributorsPage.title();
    expect(pageTitle).toContain('CONTRIBUTORS.md');
});

test('should open the issues page', async ({ overviewPage }) => {
    const reportIssuePage = await overviewPage.lightHousePage.goToReportIssue();

    const pageTitle = await reportIssuePage.title();
    expect(pageTitle).toContain('Issues');
});

test('should open the youtube page', async ({ overviewPage }) => {
    const youtubePage = await overviewPage.lightHousePage.goToYoutube();

    const pageTitle = await youtubePage.title();
    expect(pageTitle).toContain('LetPeopleWork');
});

test('should open the blog posts page', async ({ overviewPage }) => {
    const blogPostPage = await overviewPage.lightHousePage.goToBlogPosts();

    const pageTitle = await blogPostPage.title();
    expect(pageTitle).toContain('Let People Work');
    expect(blogPostPage.url()).toBe('https://blog.letpeople.work/');
});

test('should open the github page', async ({ overviewPage }) => {
    const gitHubPage = await overviewPage.lightHousePage.goToGitHub();

    const pageTitle = await gitHubPage.title();
    expect(pageTitle).toBe('LetPeopleWork · GitHub');
});