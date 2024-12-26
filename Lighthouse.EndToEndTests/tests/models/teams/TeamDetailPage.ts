import { Locator, Page } from '@playwright/test';

export class TeamDetailPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    get updateTeamDataButton(): Locator {
        return this.page.getByRole('button', { name: 'Update Team Data' });
    }

    get editTeamButton(): Locator {
        return this.page.getByRole('button', { name: 'Edit' });
    }

    get teamId(): number {
        const url = new URL(this.page.url());
        const teamId = url.pathname.split('/').pop() ?? '0';
        return Number.parseInt(teamId, 10);
    }
}
