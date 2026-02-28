import type { Locator, Page } from "@playwright/test";
import { CsvUploadWizard } from "../../helpers/csv/CsvUploadWizard";
import { BaseEditPage } from "../common/BaseEditPage";
import { BoardWizard } from "../common/BoardWizard";
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

	async getSelectedSizeEstimateField(): Promise<string> {
		const combobox = this.sizeEstimateFieldCombobox;
		return (await combobox.textContent()) ?? "";
	}

	async selectSizeEstimateField(additionalField: string): Promise<void> {
		await this.sizeEstimateFieldCombobox.click();
		await this.page.getByRole("option", { name: additionalField }).click();
	}

	async getPotentialSizeEstimateFields(): Promise<string[]> {
		await this.sizeEstimateFieldCombobox.click();
		const options = await this.page.getByRole("option").allInnerTexts();
		await this.page.keyboard.press("Escape");
		return options;
	}

	get sizeEstimateFieldCombobox(): Locator {
		return this.page
			.locator("div")
			.filter({ hasText: /.*Size Estimate Field$/ })
			.getByRole("combobox");
	}

	async toggleOwnershipSettings(): Promise<void> {
		await this.page
			.locator("div")
			.filter({ hasText: /^Ownership Settings*/ })
			.getByLabel("toggle")
			.click();
	}

	async getSelectedFeatureOwnerField(): Promise<string> {
		const combobox = this.featureOwnerFieldCombobox;
		return (await combobox.textContent()) ?? "";
	}

	async selectFeatureOwnerField(additionalField: string): Promise<void> {
		await this.featureOwnerFieldCombobox.click();
		await this.page.getByRole("option", { name: additionalField }).click();
	}

	async getPotentialFeatureOwnerFields(): Promise<string[]> {
		await this.featureOwnerFieldCombobox.click();
		const options = await this.page.getByRole("option").allInnerTexts();
		await this.page.keyboard.press("Escape");
		return options;
	}

	get featureOwnerFieldCombobox(): Locator {
		return this.page
			.locator("div")
			.filter({ hasText: /.*Feature Owner Field$/ })
			.getByRole("combobox")
			.nth(1);
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

	async openBoardWizard(
		workTrackingSystemType: string,
	): Promise<BoardWizard<PortfolioEditPage>> {
		await this.page
			.getByRole("button", { name: workTrackingSystemType })
			.click();
		return new BoardWizard(this.page, (page) => new PortfolioEditPage(page));
	}

	async triggerCsvWizard(): Promise<CsvUploadWizard<PortfolioEditPage>> {
		await this.page.getByRole("button", { name: "Upload CSV File" }).click();
		return new CsvUploadWizard(
			this.page,
			(page: Page) => new PortfolioEditPage(page),
		);
	}
}
