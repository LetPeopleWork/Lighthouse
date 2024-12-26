import { Locator, Page } from '@playwright/test';
import { TeamEditPage } from '../../teams/TeamEditPage';

export class EditWorkTrackingSystemDialog {
    page: Page;

    constructor(page: Page) {
        this.page = page;
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

    async cancel(): Promise<TeamEditPage> {
        await this.page.getByRole('button', { name: 'Cancel' }).click();

        return new TeamEditPage(this.page);
    }

    async validate(): Promise<void> {
        await this.page.getByRole('button', { name: 'Validate' }).click();
    }

    async create(): Promise<TeamEditPage> {
        await this.createButton.click();
        
        return new TeamEditPage(this.page);
    }

    get createButton(): Locator {
        return this.page.getByRole('button', { name: 'Create' });
    }
}
