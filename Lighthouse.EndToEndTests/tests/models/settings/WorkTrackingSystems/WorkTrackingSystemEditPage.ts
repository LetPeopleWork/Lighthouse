import type { Locator, Page } from "@playwright/test";
import { LighthousePage } from "../../app/LighthousePage";
import { OverviewPage } from "../../overview/OverviewPage";

export class WorkTrackingSystemEditPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async setConnectionName(name: string): Promise<void> {
		await this.page.getByLabel("Connection Name").fill(name);
	}

	async selectWorkTrackingSystem(
		workTrackingSystemName: string,
	): Promise<void> {
		// Default selection is ADO, so this is correctly hardcoded here
		await this.page.getByText("AzureDevOps").click();
		await this.page
			.getByRole("option", { name: workTrackingSystemName })
			.click();
	}

	async setWorkTrackingSystemOption(optionName: string, optionValue: string) {
		await this.page.getByLabel(optionName, { exact: true }).fill(optionValue);
	}

	async validate(): Promise<void> {
		await this.validateButton.click();
	}

	async create(): Promise<OverviewPage> {
		await this.createButton.click();

		return new OverviewPage(this.page, new LighthousePage(this.page));
	}

	async scrollToTop(): Promise<void> {
		await this.page.keyboard.press("PageUp");
		await this.page.evaluate(() => {
			window.scrollTo({ top: 0, behavior: "auto" });
		});
	}

	get validateButton(): Locator {
		return this.page.getByRole("button", { name: "Validate" });
	}

	get createButton(): Locator {
		return this.page.getByRole("button", { name: "Save" });
	}
}
