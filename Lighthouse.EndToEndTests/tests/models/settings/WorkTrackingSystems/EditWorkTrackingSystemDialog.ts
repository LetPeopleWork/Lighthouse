import type { Locator, Page } from "@playwright/test";
import type { PortfolioEditPage } from "../../portfolios/PortfolioEditPage";
import type { TeamEditPage } from "../../teams/TeamEditPage";
import type { WorkTrackingSystemsSettingsPage } from "./WorkTrackingSystemsSettingsPage";

export class EditWorkTrackingSystemDialog<
	T extends PortfolioEditPage | TeamEditPage | WorkTrackingSystemsSettingsPage,
> {
	page: Page;
	createPageHandler: (page: Page) => T;
	modifyExisting: boolean;

	constructor(
		page: Page,
		createPageHandler: (page: Page) => T,
		modifyExisting = false,
	) {
		this.page = page;
		this.createPageHandler = createPageHandler;
		this.modifyExisting = modifyExisting;
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

	async cancel(): Promise<T> {
		await this.page.getByRole("button", { name: "Cancel" }).click();

		return this.createPageHandler(this.page);
	}

	async validate(): Promise<void> {
		await this.validateButton.click();
	}

	async create(): Promise<T> {
		await this.createButton.click();

		return this.createPageHandler(this.page);
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
		const buttonName = this.modifyExisting ? "Save" : "Create";
		return this.page.getByRole("button", { name: buttonName });
	}
}
