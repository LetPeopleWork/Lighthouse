import { Page } from '@playwright/test';
import { WorkTrackingSystemsSettingsPage } from './WorkTrackingSystems/WorkTrackingSystemsSettingsPage';
import { TeamEditPage } from '../teams/TeamEditPage';
import { ProjectEditPage } from '../projects/ProjectEditPage';

export class SettingsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async goToWorkTrackingSystems() : Promise<WorkTrackingSystemsSettingsPage> {
        await this.page.getByTestId('work-tracking-tab').click();

        return new WorkTrackingSystemsSettingsPage(this.page);
    }

    async gotToDefaultTeamSettings() : Promise<TeamEditPage>{
        await this.page.getByTestId('default-team-settings-tab').click();

        return new TeamEditPage(this.page);
    }

    async gotToDefaultProjectSettings() : Promise<ProjectEditPage>{
        await this.page.getByTestId('default-project-settings-tab').click();

        return new ProjectEditPage(this.page);
    }
}
