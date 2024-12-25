import { Page } from '@playwright/test';

export class TeamEditPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }
}
