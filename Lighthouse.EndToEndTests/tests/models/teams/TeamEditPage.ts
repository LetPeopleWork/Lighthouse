import { expect, type Locator, type Page } from "@playwright/test";
import { BaseEditPage } from "../common/BaseEditPage";
import { ForecastFilterEditor } from "./ForecastFilterEditor";
import { TeamDetailPage } from "./TeamDetailPage";

export class TeamEditPage extends BaseEditPage<TeamDetailPage> {
	constructor(page: Page) {
		super(page, (page) => new TeamDetailPage(page));
	}

	get forecastFilterEditor(): ForecastFilterEditor {
		return new ForecastFilterEditor(this.page);
	}

	override async save(): Promise<TeamDetailPage> {
		await this.waitForChangesSaved();
		return new TeamDetailPage(this.page);
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

	async toggleFlowMetricsConfiguration(): Promise<void> {
		await this.page
			.getByText("Flow Metrics Configuration", { exact: true })
			.click();
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

	get stalenessThresholdField(): Locator {
		return this.page.getByLabel("Staleness Threshold (days)");
	}

	async setStalenessThreshold(days: number): Promise<void> {
		await this.stalenessThresholdField.fill(`${days}`);
	}

	async getStalenessThreshold(): Promise<number> {
		const value = (await this.stalenessThresholdField.inputValue()) ?? "0";
		return Number(value);
	}

	get stalenessEnableCheckbox(): Locator {
		return this.page.getByLabel("Set Staleness Threshold");
	}

	async enableStaleness(): Promise<void> {
		await expect
			.poll(
				async () => {
					if (!(await this.stalenessEnableCheckbox.isVisible())) {
						await this.toggleFlowMetricsConfiguration().catch(() => {});
						return false;
					}
					if (!(await this.stalenessEnableCheckbox.isChecked())) {
						await this.stalenessEnableCheckbox.check().catch(() => {});
					}
					return this.stalenessEnableCheckbox.isChecked();
				},
				{ timeout: 15_000 },
			)
			.toBe(true);
		await expect(this.stalenessThresholdField).toBeVisible();
	}

	get legacyFlowSignalsGroupHeader(): Locator {
		return this.page.getByText("Flow Signals", { exact: true });
	}

	get savedIndicator(): Locator {
		return this.page.getByText("All changes saved", { exact: true });
	}

	async waitForChangesSaved(): Promise<void> {
		await expect(this.savedIndicator).toBeVisible({ timeout: 15_000 });
	}

	async hasSaveButton(): Promise<boolean> {
		return this.saveButton.isVisible();
	}
}
