import type { Page } from "@playwright/test";
import { BaseAddWizard } from "../common/BaseAddWizard";

export class AddPortfolioWizard extends BaseAddWizard<AddPortfolioWizard> {
	constructor(readonly page: Page) {
		super(page, (page) => new AddPortfolioWizard(page));
	}

	setName(name: string): Promise<void> {
		return this.page
			.getByRole("textbox", { name: "Portfolio Name" })
			.fill(name);
	}
}
