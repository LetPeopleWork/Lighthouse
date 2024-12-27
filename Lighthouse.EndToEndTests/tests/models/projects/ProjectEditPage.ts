import { Locator, Page } from '@playwright/test';
import { ProjectDetailPage } from './ProjectDetailPage';
import { EditWorkTrackingSystemDialog } from '../settings/WorkTrackingSystems/EditWorkTrackingSystemDialog';

export class ProjectEditPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async validate(): Promise<void> {
        await this.validateButton.click();
        await this.validateButton.isEnabled();
    }

    async save(): Promise<ProjectDetailPage> {
        await this.saveButton.click();
        return new ProjectDetailPage(this.page);
    }

    async setName(newName: string): Promise<void> {
        await this.page.getByLabel('Name', { exact: true }).fill(newName);
    }

    async setWorkItemQuery(workItemQuery: string): Promise<void> {
        await this.page.getByLabel('Work Item Query').fill(workItemQuery);
    }

    async toggleUnparentedWorkItemConfiguration(): Promise<void> {
        await this.page.locator('div').filter({ hasText: /^Unparented Work ItemsUnparented Work Items QueryUnparented Work Items Query$/ }).getByLabel('toggle').click();
    }

    async setUnparentedWorkItemQuery(workItemQuery: string): Promise<void> {
        await this.page.getByLabel('Unparented Work Items Query').fill(workItemQuery);
    }

    async toggleDefaultFeatureSizeConfiguration(): Promise<void> {
        await this.page.locator('div:nth-child(9) > .MuiCardHeader-root > .MuiCardHeader-action > .MuiButtonBase-root').click();
    }

    async setSizeEstimateField(sizeEstimateField: string): Promise<void> {
        await this.page.getByLabel('Size Estimate Field').fill(sizeEstimateField);
    }

    async toggleOwnershipSettings(): Promise<void> {
        await this.page.locator('div:nth-child(10) > .MuiCardHeader-root > .MuiCardHeader-action > .MuiButtonBase-root').click();
    }

    async setFeatureOwnerField(sizeEstimateField: string): Promise<void> {
        await this.page.getByLabel('Feature Owner Field').fill(sizeEstimateField);
    }

    async removeSizeOverrideState(overrideState: string): Promise<void> {
        await this.page.locator('li').filter({ hasText: overrideState }).getByLabel('delete').click();
    }

    async addSizeOverrideState(overrideState: string): Promise<void> {
        await this.page.getByLabel('New Size Override State').fill(overrideState);
        await this.page.getByRole('button', { name: 'Add Size Override State' }).click();
    }

    async removeWorkItemType(workItemType: string): Promise<void> {
        await this.page.locator('li').filter({ hasText: workItemType }).getByLabel('delete').click();
    }

    async selectOwningTeam(teamName: string) : Promise<void> {
        await this.page.locator('div').filter({ hasText: /.*Owning Team$/ }).getByRole('combobox').click();
        await this.page.getByRole('option', { name: teamName }).click();
    }

    async getPotentialOwningTeams(): Promise<string[]> {
        await this.page.locator('div').filter({ hasText: /.*Owning Team$/ }).getByRole('combobox').click();
        const options = await this.page.getByRole('option').allInnerTexts();
        await this.page.keyboard.press('Escape');
        return options;
    }
    
    async getSelectedOwningTeam(): Promise<string> {
        const combobox = this.page.locator('div').filter({ hasText: /.*Owning Team$/ }).getByRole('combobox');
        return await combobox.textContent() ?? '';
    }

    async addWorkItemType(workItemType: string): Promise<void> {
        await this.page.getByLabel('New Work Item Type').fill(workItemType);
        await this.page.getByRole('button', { name: 'Add Work Item Type' }).click();
    }

    async deselectTeam(teamName: string): Promise<void> {
        await this.page.getByLabel(teamName).uncheck();
    }

    async selectTeam(teamName: string): Promise<void> {
        await this.page.getByLabel(teamName).check();
    }

    async addState(state: string, stateCategory: 'To Do' | 'Doing' | 'Done'): Promise<void> {
        await this.page.getByLabel(`New ${stateCategory} States`).fill(state);
        await this.page.getByRole('button', { name: `Add ${stateCategory} States` }).click();
    }

    async removeState(state: string): Promise<void> {
        await this.page.locator('li').filter({ hasText: state }).getByLabel('delete').click();
    }

    async resetWorkItemTypes(existingTypes: string[], newTypes: string[]) {
        for (const existingType of existingTypes) {
            await this.removeWorkItemType(existingType);
        }

        for (const itemType of newTypes) {
            await this.addWorkItemType(itemType);
        }
    }

    async resetStates(existingStates: { toDo: string[], doing: string[], done: string[] }, newStates: { toDo: string[], doing: string[], done: string[] }) {
        for (const state of existingStates.toDo.concat(existingStates.doing).concat(existingStates.done)) {
            await this.removeWorkItemType(state);
        }

        for (const state of newStates.toDo) {
            await this.addState(state, 'To Do');
        }

        for (const state of newStates.doing) {
            await this.addState(state, 'Doing');
        }

        for (const state of newStates.done) {
            await this.addState(state, 'Done');
        }
    }

    async addNewWorkTrackingSystem() : Promise<EditWorkTrackingSystemDialog> {
        await this.page.getByRole('button', { name: 'Add New Work Tracking System' }).click();

        return new EditWorkTrackingSystemDialog(this.page, (page) => new ProjectEditPage(page));
    }

    async selectWorkTrackingSystem(workTrackingSystemName: string): Promise<void> {
        await this.page.getByRole('combobox').click();
        await this.page.getByRole('option', { name: workTrackingSystemName }).click();
    }

    get saveButton(): Locator {
        return this.page.getByRole('button', { name: 'Save' });
    }

    get validateButton(): Locator {
        return this.page.getByRole('button', { name: 'Validate' });
    }
}
