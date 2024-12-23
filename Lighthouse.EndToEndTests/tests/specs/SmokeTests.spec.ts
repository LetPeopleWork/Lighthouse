import { expect, test } from '@playwright/test';
import { LighthousePage } from '../models/app/LighthousePage';
import config from '../config';

test('should open all pages from the header', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);

    await lighthousePage.open(config.baseUrl);
    await lighthousePage.goToTeams();
    await lighthousePage.goToProjects();
    await lighthousePage.goToSettings();
});

test('should open the contributors page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open(config.baseUrl);
    const contributorsPage = await lighthousePage.goToContributors();

    const pageTitle = await contributorsPage.title();
    expect(pageTitle).toContain('CONTRIBUTORS.md');
});

test('should open the issues page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open(config.baseUrl);
    const reportIssuePage = await lighthousePage.goToReportIssue();

    const pageTitle = await reportIssuePage.title();
    expect(pageTitle).toContain('Issues');
});

test('should open the youtube page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open(config.baseUrl);
    const youtubePage = await lighthousePage.goToYoutube();

    const pageTitle = await youtubePage.title();
    expect(pageTitle).toContain('LetPeopleWork');
});

test('should open the blog posts page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open(config.baseUrl);
    const blogPostPage = await lighthousePage.goToBlogPosts();

    const pageTitle = await blogPostPage.title();
    expect(pageTitle).toContain('Let People Work');
    expect(blogPostPage.url()).toBe('https://blog.letpeople.work/');
});

test('should open the github page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open(config.baseUrl);
    const gitHubPage = await lighthousePage.goToGitHub();

    const pageTitle = await gitHubPage.title();
    expect(pageTitle).toBe('LetPeopleWork · GitHub');
});