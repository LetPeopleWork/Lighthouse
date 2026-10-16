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

	async setWorkTrackingSystemOption(optionName: string, optionValue: string) {
		await this.page.getByLabel(optionName, { exact: true }).fill(optionValue);
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

	get createButton(): Locator {
		return this.page.getByRole("button", { name: "Save" });
	}

	/** The section where a connection holds what its Teams and Portfolios read unless they set their own. */
	get defaultsSection(): Locator {
		return this.page.getByRole("region", {
			name: "Defaults for Teams and Portfolios",
		});
	}

	async setTheTeamsEstimationDefault(
		field: string,
		unit: string,
	): Promise<void> {
		const teams = this.defaultsSection.getByRole("group", { name: "Teams" });
		await teams.getByRole("combobox", { name: "Estimation Field" }).click();
		await this.page.getByRole("option", { name: field, exact: true }).click();
		await teams.getByRole("textbox", { name: "Estimation Unit" }).fill(unit);
	}

	/** Saves, and answers the question the page asks before saving with "Save". */
	async saveAnswering(question: string): Promise<OverviewPage> {
		await this.createButton.click();
		const dialog = this.page.getByRole("dialog", { name: question });
		await dialog.getByRole("button", { name: "Save", exact: true }).click();
		await dialog.waitFor({ state: "detached" });

		return new OverviewPage(this.page, new LighthousePage(this.page));
	}

	get reconnectBanner(): Locator {
		return this.page.getByText(
			"Reconnect required — the OAuth refresh token is no longer valid",
		);
	}

	get reconnectButton(): Locator {
		return this.page.getByRole("button", { name: "Reconnect", exact: true });
	}

	async clickReconnectAndWaitForPopup(): Promise<Page> {
		const popupPromise = this.page.context().waitForEvent("page");
		await this.reconnectButton.click();
		const popup = await popupPromise;
		await popup.waitForLoadState("domcontentloaded");
		return popup;
	}
}
