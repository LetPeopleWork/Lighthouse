import type { Locator, Page } from "@playwright/test";
import type { PortfolioEditPage } from "../portfolios/PortfolioEditPage";
import type { TeamEditPage } from "../teams/TeamEditPage";

export class JiraWizard<T extends PortfolioEditPage | TeamEditPage> {
	page: Page;
	createPageHandler: (page: Page) => T;

	constructor(page: Page, createPageHandler: (page: Page) => T) {
		this.page = page;
		this.createPageHandler = createPageHandler;
	}

	async selectBoardByName(boardName: string): Promise<void> {
		await this.page.getByRole("combobox", { name: "Board" }).click();

		await this.page.getByRole("option", { name: boardName }).click();
	}

	async confirm(): Promise<T> {
		await this.confirmButton.click();
		return this.createPageHandler(this.page);
	}

	get boardInformationPanel(): Locator {
		return this.page.getByText("Board InformationQueryproject");
	}

	get confirmButton(): Locator {
		return this.page.getByRole("button", { name: "Confirm" });
	}
}
