import { Page } from '@playwright/test';

export class ProjectDetailPage{
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }
}
