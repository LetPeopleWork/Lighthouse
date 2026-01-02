import type { Locator, Page } from "@playwright/test";
import { CsvUploadWizard } from "../../helpers/csv/CsvUploadWizard";
import { BaseEditPage } from "../common/BaseEditPage";
import { EditWorkTrackingSystemDialog } from "../settings/WorkTrackingSystems/EditWorkTrackingSystemDialog";
import { PortfolioDetailPage } from "./PortfolioDetailPage";

export class PortfolioEditPage extends BaseEditPage<PortfolioDetailPage> {
	override async save(): Promise<PortfolioDetailPage> {
		await this.saveButton.click();
		return new PortfolioDetailPage(this.page);
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

	async setPercentileHistoryInDays(days: number): Promise<void> {
		await this.page.getByLabel("History in Days").fill(`${days}`);
	}

	async getPercentileHistoryInDays(): Promise<number> {
		const percentileHistoryInDays =
			(await this.page.getByLabel("History in Days").inputValue()) ?? "0";

		return Number(percentileHistoryInDays);
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
		await this.page.keyboard.press("Enter");

		// Reset the input field
		await this.page.keyboard.press("Escape");
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
		EditWorkTrackingSystemDialog<PortfolioEditPage>
	> {
		await this.page
			.getByRole("button", { name: "Add New Work Tracking System" })
			.click();

		return new EditWorkTrackingSystemDialog(
			this.page,
			(page) => new PortfolioEditPage(page),
		);
	}

	async triggerCsvWizard(): Promise<CsvUploadWizard<PortfolioEditPage>> {
		await this.page.getByRole("button", { name: "Upload CSV File" }).click();
		return new CsvUploadWizard(
			this.page,
			(page: Page) => new PortfolioEditPage(page),
		);
	}
}
