import { Locator, Page } from '@playwright/test';
import { TeamDetailPage } from './TeamDetailPage';
import { EditWorkTrackingSystemDialog } from '../settings/WorkTrackingSystems/EditWorkTrackingSystemDialog';

export class TeamEditPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async validate(): Promise<void> {
        await this.validateButton.click();
        await this.validateButton.isEnabled();
    }

    async save(): Promise<TeamDetailPage> {
        await this.saveButton.click();
        return new TeamDetailPage(this.page);
    }

    async setName(newName: string): Promise<void> {
        await this.page.getByLabel('Name').fill(newName);
    }

    async setThroughputHistory(throughputHistory: number): Promise<void> {
        await this.page.getByLabel('Throughput History').fill(`${throughputHistory}`);
    }

    async setWorkItemQuery(workItemQuery: string): Promise<void> {
        await this.page.getByLabel('Work Item Query').fill(workItemQuery);
    }

    async removeWorkItemType(workItemType: string): Promise<void> {
        await this.page.locator('li').filter({ hasText: workItemType }).getByLabel('delete').click();
    }

    async addWorkItemType(workItemType: string): Promise<void> {
        await this.page.getByLabel('New Work Item Type').fill(workItemType);
        await this.page.getByRole('button', { name: 'Add Work Item Type' }).click();
    }

    async addState(state: string, stateCategory: 'To Do' | 'Doing' | 'Done'): Promise<void> {
        await this.page.getByLabel(`New ${stateCategory} States`).fill(state);
        await this.page.getByRole('button', { name: `Add ${stateCategory} States` }).click();
    }

    async removeState(state: string): Promise<void> {
        await this.page.locator('li').filter({ hasText: state }).getByLabel('delete').click();
    }

    async toggleAdvancedConfiguration(): Promise<void> {
        await this.page.getByLabel('toggle').nth(4).click();
    }

    async setFeatureWip(featureWIP: number): Promise<void> {
        await this.page.getByLabel('Feature WIP', { exact: true }).fill(`${featureWIP}`);
    }

    async enableAutomaticallyAdjustFeatureWIP(): Promise<void> {
        await this.page.getByLabel('Automatically Adjust Feature').check();
    }

    async disableAutomaticallyAdjustFeatureWIP(): Promise<void> {
        await this.page.getByLabel('Automatically Adjust Feature').uncheck();
    }

    async setRelationCustomField(customField: string): Promise<void> {
        await this.page.getByLabel('Relation Custom Field').fill(customField);
    }

    async selectWorkTrackingSystem(workTrackingSystemName: string): Promise<void> {
        await this.page.getByRole('combobox').click();
        await this.page.getByRole('option', { name: workTrackingSystemName }).click();
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
    
    async addNewWorkTrackingSystem() : Promise<EditWorkTrackingSystemDialog<TeamEditPage>> {
        await this.page.getByRole('button', { name: 'Add New Work Tracking System' }).click();

        return new EditWorkTrackingSystemDialog(this.page, (page) => new TeamEditPage(page));
    }

    get saveButton(): Locator {
        return this.page.getByRole('button', { name: 'Save' });
    }

    get validateButton(): Locator {
        return this.page.getByRole('button', { name: 'Validate' });
    }
}
