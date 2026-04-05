import type { Locator, Page } from "@playwright/test";
import { BaseEditPage } from "../common/BaseEditPage";
import { TeamDetailPage } from "./TeamDetailPage";

export class TeamEditPage extends BaseEditPage<TeamDetailPage> {
	constructor(page: Page) {
		super(page, (page) => new TeamDetailPage(page));
	}

	override async save(): Promise<TeamDetailPage> {
		await this.saveButton.click();
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
			.locator(
				"div:nth-child(7) > .MuiCardHeader-root > .MuiCardHeader-action > .MuiButtonBase-root",
			)
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
}
