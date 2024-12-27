import { Locator, Page } from '@playwright/test';

export class EditWorkTrackingSystemDialog {
    page: Page;
    createPageHandler: (page: Page) => any;

    constructor(page: Page, createPageHandler: (page: Page) => any) {
        this.page = page;
        this.createPageHandler = createPageHandler;
    }

    async setConnectionName(name: string): Promise<void>{
        await this.page.getByLabel('Connection Name').fill(name);
    }

    async selectWorkTrackingSystem(workTrackingSystemName: string): Promise<void> {
        // Default selection is ADO, so this is correctly hardcoded here
        await this.page.getByText('AzureDevOps').click();
        await this.page.getByRole('option', { name: workTrackingSystemName }).click();
    }

    async setWorkTrackingSystemOption(optionName: string, optionValue: string){
        await this.page.getByLabel(optionName).fill(optionValue);
    }

    async cancel(): Promise<any> {
        await this.page.getByRole('button', { name: 'Cancel' }).click();

        return this.createPageHandler(this.page);
    }

    async validate(): Promise<void> {
        await this.page.getByRole('button', { name: 'Validate' }).click();
    }

    async create(): Promise<any> {
        await this.createButton.click();
        
        return this.createPageHandler(this.page);
    }

    get createButton(): Locator {
        return this.page.getByRole('button', { name: 'Create' });
    }
}
