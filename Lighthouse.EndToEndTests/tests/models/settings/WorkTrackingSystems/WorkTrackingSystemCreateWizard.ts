import type { Locator, Page } from "@playwright/test";
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
		await this.page.getByRole("button", { name: "Next", exact: true }).click();
	}

	async create(): Promise<OverviewPage> {
		await this.page
			.getByRole("button", { name: "Create", exact: true })
			.click();
		return new OverviewPage(this.page, new LighthousePage(this.page));
	}

	async selectAuthenticationMethod(displayName: string): Promise<void> {
		await this.page.getByLabel("Authentication Method").click();
		const listbox = this.page.getByRole("listbox");
		await listbox.getByRole("option", { name: displayName }).click();
	}

	get adoHttpsWarning(): Locator {
		return this.page.getByText(
			/Azure DevOps requires HTTPS callback URLs in production/i,
		);
	}

	get connectButton(): Locator {
		return this.page.getByRole("button", { name: "Connect", exact: true });
	}
}
