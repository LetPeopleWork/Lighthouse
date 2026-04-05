import type { Page } from "@playwright/test";
import { LighthousePage } from "../../app/LighthousePage";
import { OverviewPage } from "../../overview/OverviewPage";

export class WorkTrackingSystemCreateWizard {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async selectWorkTrackingSystemType(type: string): Promise<void> {
		await this.page
			.getByRole("button", { name: `New ${type} Connection` })
			.click();
	}

	async setWorkTrackingSystemOption(optionName: string, optionValue: string) {
		await this.page.getByLabel(optionName, { exact: true }).fill(optionValue);
	}

	async setConnectionName(name: string): Promise<void> {
		await this.page.getByLabel("Connection Name").fill(name);
	}

	async goToNextStep(): Promise<void> {
		await this.page.getByRole("button", { name: "Next" }).click();
	}

	async create(): Promise<OverviewPage> {
		await this.page.getByRole("button", { name: "Create" }).click();
		return new OverviewPage(this.page, new LighthousePage(this.page));
	}
}
