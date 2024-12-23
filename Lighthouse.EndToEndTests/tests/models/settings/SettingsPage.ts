import { Page } from '@playwright/test';

export class SettingsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }
}
