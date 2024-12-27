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
            const contributorsButton = this.GetContributorsButton();
            await contributorsButton.click();
        });
    }

    async goToReportIssue(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const reportIssueButton = this.GetReportIssueButton();
            await reportIssueButton.click();
        });
    }

    async goToLetPeopleWork(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const lpwLogo = this.GetLpwLogoButton();
            await lpwLogo.click();
        });
    }

    async goToRelease(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const versionNumberButton = this.GetVersionNumberButton();
            await versionNumberButton.click();
        });
    }

    async getEmailContact(): Promise<string> {
        const emailButton = this.page.getByTestId('mailto:contact@letpeople.work');
        const link = await emailButton.getAttribute('href');
        return link ?? '';
    }

    async contactViaCall(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const callButton = this.page.getByTestId('https://calendly.com/letpeoplework/');
            await callButton.click();
        });
    }

    async contactViaLinkedIn(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const linkedInButton = this.page.getByTestId('https://www.linkedin.com/company/let-people-work/?viewAsMember=true');
            await linkedInButton.click();
        });        
    }

    async contactViaGitHubIssue(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const raiseGitHubIssueButton = this.page.getByLabel('Raise an Issue on GitHub');
            await raiseGitHubIssueButton.click();
        });        
    }

    async goToYoutube(): Promise<Page> {
        const youtubePage = await this.OpenInNewTab(async () => {
            const youtubeButton = this.GetYoutubeButton();
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
            const blogPostsButton = this.GetBlogPostsButton();
            await blogPostsButton.click();
        });
    }

    async goToGitHub(): Promise<Page> {
        return this.OpenInNewTab(async () => {
            const gitHubButton = this.GetGitHubButton();
            await gitHubButton.click();
        });
    }

    private GetContributorsButton(): Locator {
        return this.page.getByTestId('https://github.com/LetPeopleWork/Lighthouse/blob/main/CONTRIBUTORS.md');
    }

    private GetReportIssueButton(): Locator {
        return this.page.getByLabel('Report an Issue');
    }

    private GetYoutubeButton(): Locator {
        return this.page.getByTestId('https://www.youtube.com/channel/UCipDDn2dpVE3rpoKNW2asZQ');
    }

    private GetBlogPostsButton(): Locator {
        return this.page.getByTestId('https://www.letpeople.work/blog/');
    }

    private GetGitHubButton(): Locator {
        return this.page.getByTestId('https://github.com/LetPeopleWork/');
    }

    private GetLpwLogoButton(): Locator {
        return this.page.getByRole('link', { name: 'Let People Work Logo' });
    }

    private GetVersionNumberButton(): Locator {
        // Match version number scheme like 'v1.33.7' or 'v24.12.20.1852'
        return this.page.getByRole('link', { name: /^v\d{2,4}(\.\d{1,4}){2,3}$/ })
    }

    private async OpenInNewTab(openInNewTabAction: () => Promise<void>): Promise<Page> {
        const popup = this.page.waitForEvent('popup');
        await openInNewTabAction();
        return await popup;
    }
}
