import type { Locator, Page } from "@playwright/test";
import { EditWorkTrackingSystemDialog } from "../settings/WorkTrackingSystems/EditWorkTrackingSystemDialog";
import { ProjectDetailPage } from "./ProjectDetailPage";

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
		await this.page.getByLabel("Name", { exact: true }).fill(newName);
	}

	async getName(): Promise<string> {
		return (
			(await this.page.getByLabel("Name", { exact: true }).inputValue()) ?? ""
		);
	}

	async setWorkItemQuery(workItemQuery: string): Promise<void> {
		await this.page.getByLabel("Work Item Query").fill(workItemQuery);
	}

	async getWorkItemQuery(): Promise<string> {
		return (
			(await this.page
				.getByLabel("Work Item Query", { exact: true })
				.inputValue()) ?? ""
		);
	}

	async toggleUnparentedWorkItemConfiguration(): Promise<void> {
		await this.page
			.locator("div")
			.filter({ hasText: /^Unparented Work Items*/ })
			.getByLabel("toggle")
			.click();
	}

	async setUnparentedWorkItemQuery(workItemQuery: string): Promise<void> {
		await this.page
			.getByLabel("Unparented Work Items Query")
			.fill(workItemQuery);
	}

	async getUnparentedWorkItemQuery(): Promise<string> {
		return (
			(await this.page
				.getByLabel("Unparented Work Items Query")
				.inputValue()) ?? ""
		);
	}

	async toggleDefaultFeatureSizeConfiguration(): Promise<void> {
		await this.page
			.locator("div")
			.filter({ hasText: /^Default Feature Size*/ })
			.getByLabel("toggle")
			.click();
	}

	async useHistoricalFeatureSize(): Promise<void> {
		await this.useHistoricalFeatureSizeToggle.check();
	}

	async useDefaultNumberOFItemsForFeatureSize(): Promise<void> {
		await this.useHistoricalFeatureSizeToggle.uncheck();
	}

	get useHistoricalFeatureSizeToggle(): Locator {
		return this.page.getByLabel("Use Historical Feature Size");
	}

	async setHistoricalFeatureSizePercentile(percentile: number): Promise<void> {
		await this.page.getByLabel("Feature Size Percentile").fill(`${percentile}`);
	}

	async getHistoricalFeatureSizePercentile(): Promise<number> {
		const featureSizePercentile =
			(await this.page.getByLabel("Feature Size Percentile").inputValue()) ??
			"0";
		return Number(featureSizePercentile);
	}

	async setHistoricalFeatureSizeQuery(query: string): Promise<void> {
		await this.page.getByLabel("Historical Features Work Item").fill(query);
	}

	async getHistoricalFeatureSizeQuery(): Promise<string> {
		return (
			(await this.page
				.getByLabel("Historical Features Work Item")
				.inputValue()) ?? "0"
		);
	}

	async setSizeEstimateField(sizeEstimateField: string): Promise<void> {
		await this.page.getByLabel("Size Estimate Field").fill(sizeEstimateField);
	}

	async getSizeEstimateField(): Promise<string> {
		return (
			(await this.page.getByLabel("Size Estimate Field").inputValue()) ?? ""
		);
	}

	async toggleOwnershipSettings(): Promise<void> {
		await this.page
			.locator("div")
			.filter({ hasText: /^Ownership Settings*/ })
			.getByLabel("toggle")
			.click();
	}

	async setFeatureOwnerField(sizeEstimateField: string): Promise<void> {
		await this.page.getByLabel("Feature Owner Field").fill(sizeEstimateField);
	}

	async getFeatureOwnerField(): Promise<string> {
		return (
			(await this.page.getByLabel("Feature Owner Field").inputValue()) ?? ""
		);
	}

	async removeSizeOverrideState(overrideState: string): Promise<void> {
		await this.page
			.locator("li")
			.filter({ hasText: overrideState })
			.getByLabel("delete")
			.click();
	}

	async addSizeOverrideState(overrideState: string): Promise<void> {
		await this.page.getByLabel("New Size Override State").fill(overrideState);
		await this.page
			.getByRole("button", { name: "Add Size Override State" })
			.click();
	}

	async addWorkItemType(workItemType: string): Promise<void> {
		await this.page.getByLabel("New Work Item Type").fill(workItemType);
		await this.page.getByRole("button", { name: "Add Work Item Type" }).click();
	}

	async removeWorkItemType(workItemType: string): Promise<void> {
		await this.getWorkItemType(workItemType).getByLabel("delete").click();
	}

	getWorkItemType(workItemType: string): Locator {
		return this.page.locator("li").filter({ hasText: workItemType });
	}

	async selectOwningTeam(teamName: string): Promise<void> {
		await this.page
			.locator("div")
			.filter({ hasText: /.*Owning Team$/ })
			.getByRole("combobox")
			.click();
		await this.page.getByRole("option", { name: teamName }).click();
	}

	async getPotentialOwningTeams(): Promise<string[]> {
		await this.page
			.locator("div")
			.filter({ hasText: /.*Owning Team$/ })
			.getByRole("combobox")
			.click();
		const options = await this.page.getByRole("option").allInnerTexts();
		await this.page.keyboard.press("Escape");
		return options;
	}

	async getSelectedOwningTeam(): Promise<string> {
		const combobox = this.page
			.locator("div")
			.filter({ hasText: /.*Owning Team$/ })
			.getByRole("combobox");
		return (await combobox.textContent()) ?? "";
	}

	async deselectTeam(teamName: string): Promise<void> {
		await this.page.getByLabel(teamName).uncheck();
	}

	async selectTeam(teamName: string): Promise<void> {
		await this.page.getByLabel(teamName).check();
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
		EditWorkTrackingSystemDialog<ProjectEditPage>
	> {
		await this.page
			.getByRole("button", { name: "Add New Work Tracking System" })
			.click();

		return new EditWorkTrackingSystemDialog(
			this.page,
			(page) => new ProjectEditPage(page),
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

	get saveButton(): Locator {
		return this.page.getByRole("button", { name: "Save" });
	}

	get validateButton(): Locator {
		return this.page.getByRole("button", { name: "Validate" });
	}
}
