import type { Page } from "@playwright/test";
import { SystemConfigurationPage } from "./SystemConfigurationPage";

export class BlackoutPeriodDialog {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async addBlackoutPeriod(
		name: string,
		startDate: string,
		endDate: string,
	): Promise<void> {
		await this.page.getByLabel("Des").fill(name);
		await this.page.getByLabel("Start Date").fill(startDate);
		await this.page.getByLabel("End Date").fill(endDate);
	}

	async saveBlackoutPeriod(): Promise<SystemConfigurationPage> {
		await this.page.getByRole("button", { name: "Save" }).click();
		return new SystemConfigurationPage(this.page);
	}
}
