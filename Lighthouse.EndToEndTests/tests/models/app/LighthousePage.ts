import { Locator, Page } from '@playwright/test';
import { OverviewPage } from '../overview/OverviewPage';
import { TeamsPage } from '../teams/TeamsPage';
import { ProjectsPage } from '../projects/ProjectsPage';
import { SettingsPage } from '../settings/SettingsPage';

export class LighthousePage {
    readonly page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async open(): Promise<OverviewPage> {
        await this.page.goto('/');
        return this.goToOverview();
    }

    async goToOverview(): Promise<OverviewPage> {
        await this.page.getByRole('link', { name: 'Overview' }).click();
        return new OverviewPage(this.page, this);
    }

    async goToTeams(): Promise<TeamsPage> {
        await this.page.getByRole('link', { name: 'Teams' }).click();
        return new TeamsPage(this.page);
    }

    async goToProjects(): Promise<ProjectsPage> {
        await this.page.getByRole('link', { name: 'Projects', exact: true }).click();
        return new ProjectsPage(this.page);
    }

    async goToSettings(): Promise<SettingsPage> {
        await this.page.getByRole('link', { name: 'Settings' }).click();
        return new SettingsPage(this.page);
    }

    async goToContributors(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const contributorsButton = await this.GetContributorsButton();
            await contributorsButton.click();
        });
    }

    async goToReportIssue(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const reportIssueButton = await this.GetReportIssueButton();
            await reportIssueButton.click();
        });
    }

    async goToYoutube(): Promise<Page> {
        const youtubePage = await this.OpenInNewTab(async () => {
            const youtubeButton = await this.GetYoutubeButton();
            await youtubeButton.click();
        });

        try {
            const rejectButton = youtubePage.getByRole('button', { name: 'Reject all' });
            await rejectButton.click({ timeout: 5000 });
        } catch {
            // No reject button found - skipping
        }

        await youtubePage.waitForLoadState('networkidle');
        return youtubePage;
    }

    async goToBlogPosts(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const blogPostsButton = await this.GetBlogPostsButton();
            await blogPostsButton.click();
        });
    }

    async goToGitHub(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const gitHubButton = await this.GetGitHubButton();
            await gitHubButton.click();
        });
    }

    private async GetContributorsButton(): Promise<Locator> {
        return this.page.getByTestId('https://github.com/LetPeopleWork/Lighthouse/blob/main/CONTRIBUTORS.md');
    }

    private async GetReportIssueButton(): Promise<Locator> {
        return this.page.getByLabel('Report an Issue');
    }

    private async GetYoutubeButton(): Promise<Locator> {
        return this.page.getByTestId('https://www.youtube.com/channel/UCipDDn2dpVE3rpoKNW2asZQ');
    }

    private async GetBlogPostsButton(): Promise<Locator> {
        return this.page.getByTestId('https://www.letpeople.work/blog/');
    }

    private async GetGitHubButton(): Promise<Locator> {
        return this.page.getByTestId('https://github.com/LetPeopleWork/');
    }

    private async OpenInNewTab(openInNewTabAction: () => Promise<void>): Promise<Page> {
        const popup = this.page.waitForEvent('popup');
        await openInNewTabAction();
        return await popup;
    }
}
