import type { Locator, Page } from "@playwright/test";
import { EditWorkTrackingSystemDialog } from "../settings/WorkTrackingSystems/EditWorkTrackingSystemDialog";
import { TeamDetailPage } from "./TeamDetailPage";

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
		await this.page.getByLabel("Name").fill(newName);
	}

	async getName(): Promise<string> {
		return await this.page.getByLabel("Name").inputValue();
	}

	async setThroughputHistory(throughputHistory: number): Promise<void> {
		await this.page
			.getByLabel("Throughput History")
			.fill(`${throughputHistory}`);
	}

	async getThroughputHistory(): Promise<number> {
		const throughput =
			(await this.page.getByLabel("Throughput History").inputValue()) ?? "0";
		return Number(throughput);
	}

	async setWorkItemQuery(workItemQuery: string): Promise<void> {
		await this.page.getByLabel("Work Item Query").fill(workItemQuery);
	}

	async getWorkItemQuery(): Promise<string> {
		return (await this.page.getByLabel("Work Item Query").inputValue()) ?? "";
	}

	async removeWorkItemType(workItemType: string): Promise<void> {
		await this.getWorkItemType(workItemType).getByLabel("delete").click();
	}

	getWorkItemType(workItemType: string): Locator {
		return this.page.locator("li").filter({ hasText: workItemType });
	}

	async addWorkItemType(workItemType: string): Promise<void> {
		await this.page.getByLabel("New Work Item Type").fill(workItemType);
		await this.page.getByRole("button", { name: "Add Work Item Type" }).click();
	}

	async addState(
		state: string,
		stateCategory: "To Do" | "Doing" | "Done",
	): Promise<void> {
		await this.page.getByLabel(`New ${stateCategory} States`).fill(state);
		await this.page
			.getByRole("button", { name: `Add ${stateCategory} States` })
			.click();
	}

	async removeState(state: string): Promise<void> {
		await this.getState(state).getByLabel("delete").click();
	}

	getState(state: string): Locator {
		return this.page.locator("li").filter({ hasText: state });
	}

	async toggleAdvancedConfiguration(): Promise<void> {
		await this.page.getByLabel("toggle").last().click();
	}

	async setFeatureWip(featureWIP: number): Promise<void> {
		await this.page
			.getByLabel("Feature WIP", { exact: true })
			.fill(`${featureWIP}`);
	}

	async getFeatureWip(): Promise<number> {
		const featureWIP =
			(await this.page
				.getByLabel("Feature WIP", { exact: true })
				.inputValue()) ?? "0";
		return Number(featureWIP);
	}

	get automaticallyAdjustFeatureWIPCheckBox(): Locator {
		return this.page.getByLabel("Automatically Adjust Feature");
	}

	async enableAutomaticallyAdjustFeatureWIP(): Promise<void> {
		await this.automaticallyAdjustFeatureWIPCheckBox.check();
	}

	async disableAutomaticallyAdjustFeatureWIP(): Promise<void> {
		await this.automaticallyAdjustFeatureWIPCheckBox.uncheck();
	}

	async setRelationCustomField(customField: string): Promise<void> {
		await this.page.getByLabel("Relation Custom Field").fill(customField);
	}

	async getRelationCustomField(): Promise<string> {
		return (
			(await this.page.getByLabel("Relation Custom Field").inputValue()) ?? ""
		);
	}

	async selectWorkTrackingSystem(
		workTrackingSystemName: string,
	): Promise<void> {
		await this.page.getByRole("combobox").click();
		await this.page
			.getByRole("option", { name: workTrackingSystemName })
			.click();
	}

	async resetWorkItemTypes(existingTypes: string[], newTypes: string[]) {
		for (const existingType of existingTypes) {
			await this.removeWorkItemType(existingType);
		}

		for (const itemType of newTypes) {
			await this.addWorkItemType(itemType);
		}
	}

	async resetStates(
		existingStates: { toDo: string[]; doing: string[]; done: string[] },
		newStates: { toDo: string[]; doing: string[]; done: string[] },
	) {
		for (const state of existingStates.toDo
			.concat(existingStates.doing)
			.concat(existingStates.done)) {
			await this.removeWorkItemType(state);
		}

		for (const state of newStates.toDo) {
			await this.addState(state, "To Do");
		}

		for (const state of newStates.doing) {
			await this.addState(state, "Doing");
		}

		for (const state of newStates.done) {
			await this.addState(state, "Done");
		}
	}

	async addNewWorkTrackingSystem(): Promise<
		EditWorkTrackingSystemDialog<TeamEditPage>
	> {
		await this.page
			.getByRole("button", { name: "Add New Work Tracking System" })
			.click();

		return new EditWorkTrackingSystemDialog(
			this.page,
			(page) => new TeamEditPage(page),
		);
	}

	get saveButton(): Locator {
		return this.page.getByRole("button", { name: "Save" });
	}

	get validateButton(): Locator {
		return this.page.getByRole("button", { name: "Validate" });
	}
}
