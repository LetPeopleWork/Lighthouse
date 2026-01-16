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

	async selectBoard(): Promise<T> {
		await this.selectBoardButton.click();
		return this.createPageHandler(this.page);
	}

	get boardInformationPanel(): Locator {
		return this.page.getByText("Board InformationQueryproject");
	}

	get selectBoardButton(): Locator {
		return this.page.getByRole("button", { name: "Select Board" });
	}
}
