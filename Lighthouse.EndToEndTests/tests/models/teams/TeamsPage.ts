import { Page } from '@playwright/test';

export class TeamsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }
}
