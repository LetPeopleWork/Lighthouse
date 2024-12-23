import { Page } from "@playwright/test";
import { OverviewPage } from "../overview/OverviewPage";
import { TeamsPage } from "../teams/TeamsPage";
import { ProjectsPage } from "../projects/ProjectsPage";
import { SettingsPage } from "../settings/SettingsPage";

export class Header {
    readonly page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async goToOverview(): Promise<OverviewPage> {
        await this.page.getByRole('link', { name: 'Overview' }).click();
        return new OverviewPage(this.page);
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
}