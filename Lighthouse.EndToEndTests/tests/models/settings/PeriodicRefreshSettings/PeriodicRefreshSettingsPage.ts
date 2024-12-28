import { Page } from '@playwright/test';

export type PeriodicRefreshSettingType = 'Throughput' | 'Feature' | 'Forecast';

export class PeriodicRefreshSettingsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async setInterval(interval: number, settings: PeriodicRefreshSettingType): Promise<void> {
        await this.page.getByTestId('periodic-refresh-settings-panel').locator('div').filter({ hasText: `${settings} RefreshInterval (` }).getByLabel('Interval (Minutes)').fill(`${interval}`);
    }

    async getInterval(settings: PeriodicRefreshSettingType): Promise<number> {
        const value = await this.page.getByTestId('periodic-refresh-settings-panel').locator('div').filter({ hasText: `${settings} RefreshInterval (` }).getByLabel('Interval (Minutes)').inputValue() ?? '0';
        return Number(value);
    }

    async setRefreshAfter(refreshAfter: number, settings: PeriodicRefreshSettingType): Promise<void> {
        await this.page.getByTestId('periodic-refresh-settings-panel').locator('div').filter({ hasText: `${settings} RefreshInterval (` }).getByLabel('Refresh After (Minutes)').fill(`${refreshAfter}`);
    }

    async getRefreshAfter(settings: PeriodicRefreshSettingType): Promise<number> {
        const value = await this.page.getByTestId('periodic-refresh-settings-panel').locator('div').filter({ hasText: `${settings} RefreshInterval (` }).getByLabel('Refresh After (Minutes)').inputValue() ?? '0';
        return Number(value);
    }

    async setStartDelay(startDelay: number, settings: PeriodicRefreshSettingType): Promise<void> {
        await this.page.getByTestId('periodic-refresh-settings-panel').locator('div').filter({ hasText: `${settings} RefreshInterval (` }).getByLabel('Start Delay (Minutes)').fill(`${startDelay}`);
    }

    async getStartDelay(settings: PeriodicRefreshSettingType): Promise<number> {
        const value = await this.page.getByTestId('periodic-refresh-settings-panel').locator('div').filter({ hasText: `${settings} RefreshInterval (` }).getByLabel('Start Delay (Minutes)').inputValue() ?? '0';
        return Number(value);
    }

    async updateSettings(settings: PeriodicRefreshSettingType): Promise<void> {
        await this.page.getByRole('button', { name: `Update ${settings} Settings` }).click();
    }
}
