import { Locator, Page } from '@playwright/test';

export class ProjectDetailPage{
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    get refreshFeatureButton(): Locator {
        return this.page.getByRole('button', { name: 'Refresh Features' });
    }

    get editProjectButton(): Locator {
        return this.page.getByRole('button', { name: 'Edit Project' });
    }

    get projectId(): number {
        const url = new URL(this.page.url());
        const projectId = url.pathname.split('/').pop() ?? '0';
        return Number.parseInt(projectId, 10);
    }
}
