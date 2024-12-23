import { Page } from '@playwright/test';
import { LighthousePage } from '../app/LighthousePage';

export class SettingsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }
}
