import { Locator, Page } from '@playwright/test';
import { Header } from './Header';
import { OverviewPage } from '../overview/OverviewPage';

export class LighthousePage extends Header {
    readonly page: Page;

    constructor(page: Page) {
        super(page);
        this.page = page;
    }

    async open(): Promise<OverviewPage> {
        await this.page.goto('/');
        return this.goToOverview();
    }

    async waitForLoad() {
        await this.page.waitForSelector('main[role="main"] img[role="img"]', { state: 'hidden' });
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
