import { Locator, Page } from '@playwright/test';

export class TeamEditPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }    

    async validate() : Promise<void>{
        await this.validateButton.click();
        await this.validateButton.isEnabled();
    }

    get saveButton() : Locator {
        return this.page.getByRole('button', { name: 'Save' });
    }

    get validateButton() : Locator {
        return this.page.getByRole('button', { name: 'Validate' });
    }
}
