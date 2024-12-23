import { Page } from '@playwright/test';

export class ProjectsPage{
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }
}
