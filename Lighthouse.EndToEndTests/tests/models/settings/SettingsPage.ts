import { Page } from '@playwright/test';
import { WorkTrackingSystemsSettingsPage } from './WorkTrackingSystems/WorkTrackingSystemsSettingsPage';
import { TeamEditPage } from '../teams/TeamEditPage';
import { ProjectEditPage } from '../projects/ProjectEditPage';
import { PeriodicRefreshSettingsPage } from './PeriodicRefreshSettings/PeriodicRefreshSettingsPage';
import { DataRetentionSettingsPage } from './DataRetentionSettings/DataRetentionSettingsPage';

export class SettingsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async goToWorkTrackingSystems() : Promise<WorkTrackingSystemsSettingsPage> {
        await this.page.getByTestId('work-tracking-tab').click();

        return new WorkTrackingSystemsSettingsPage(this.page);
    }

    async goToDefaultTeamSettings() : Promise<TeamEditPage>{
        await this.page.getByTestId('default-team-settings-tab').click();

        return new TeamEditPage(this.page);
    }

    async goToDefaultProjectSettings() : Promise<ProjectEditPage>{
        await this.page.getByTestId('default-project-settings-tab').click();

        return new ProjectEditPage(this.page);
    }

    async goToPeriodicRefreshSettings() : Promise<PeriodicRefreshSettingsPage>{
        await this.page.getByTestId('periodic-refresh-settings-tab').click();

        return new PeriodicRefreshSettingsPage(this.page);
    }

    async goToDataRetentionSettings() : Promise<DataRetentionSettingsPage>{
        await this.page.getByTestId('data-retention-settings-tab').click();

        return new DataRetentionSettingsPage(this.page);
    }
}
