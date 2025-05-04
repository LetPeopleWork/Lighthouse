import type { Locator } from "@playwright/test";
import { BaseEditPage } from "../common/BaseEditPage";
import { EditWorkTrackingSystemDialog } from "../settings/WorkTrackingSystems/EditWorkTrackingSystemDialog";
import { ProjectDetailPage } from "./ProjectDetailPage";

export class ProjectEditPage extends BaseEditPage<ProjectDetailPage> {
	override async save(): Promise<ProjectDetailPage> {
		await this.saveButton.click();
		return new ProjectDetailPage(this.page);
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
		await this.removeChipItem(overrideState);
	}

	async addSizeOverrideState(overrideState: string): Promise<void> {
		await this.page.getByLabel("New Size Override State").fill(overrideState);
		await this.page.getByLabel("New Size Override State").press("Enter");
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
}
