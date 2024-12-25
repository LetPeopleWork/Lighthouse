import { Page } from '@playwright/test';

export class TeamDetailPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }
}
