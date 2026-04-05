import type { Page } from "@playwright/test";
import { BaseAddWizard } from "../common/BaseAddWizard";

export class AddTeamWizard extends BaseAddWizard<AddTeamWizard> {
	constructor(readonly page: Page) {
		super(page, (page) => new AddTeamWizard(page));
	}

	async setName(name: string): Promise<void> {
		await this.page.getByRole("textbox", { name: "Team Name" }).fill(name);
	}
}
