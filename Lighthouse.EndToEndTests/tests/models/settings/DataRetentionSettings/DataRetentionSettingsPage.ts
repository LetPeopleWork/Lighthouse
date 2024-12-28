import { Page } from '@playwright/test';

export class DataRetentionSettingsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async setMaximumDataRetentionTime(maxDays: number): Promise<void> {
        await this.page.getByLabel('Maximum Data Retention Time (').fill(`${maxDays}`);
    }

    async getMaximumDataRetentionTime(): Promise<number> {
        const value = await this.page.getByLabel('Maximum Data Retention Time (').inputValue() ?? '0';
        return Number(value);
    }

    async updateDataRetentionSettings(): Promise<void> {
        await this.page.getByRole('button', { name: 'Update Data Retention Settings' }).click();
    }
}
