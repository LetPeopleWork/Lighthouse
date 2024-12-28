import { Page } from '@playwright/test';
import { WorkTrackingSystemsSettingsPage } from './WorkTrackingSystems/WorkTrackingSystemsSettingsPage';

export class SettingsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async goToWorkTrackingSystems() : Promise<WorkTrackingSystemsSettingsPage> {
        await this.page.getByTestId('work-tracking-tab').click();

        return new WorkTrackingSystemsSettingsPage(this.page);
    }
}
