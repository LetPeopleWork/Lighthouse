import { Page } from '@playwright/test';

export class ProjectEditPage{
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }
}
