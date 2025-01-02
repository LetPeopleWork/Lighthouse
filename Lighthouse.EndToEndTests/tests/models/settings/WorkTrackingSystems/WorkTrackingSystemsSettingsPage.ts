import type { Page } from "@playwright/test";
import { EditWorkTrackingSystemDialog } from "./EditWorkTrackingSystemDialog";

export class WorkTrackingSystemsSettingsPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async addNewWorkTrackingSystem(): Promise<
		EditWorkTrackingSystemDialog<WorkTrackingSystemsSettingsPage>
	> {
		await this.page.getByRole("button", { name: "Add Connection" }).click();

		return new EditWorkTrackingSystemDialog(
			this.page,
			(page) => new WorkTrackingSystemsSettingsPage(page),
		);
	}

	async modifyWorkTryckingSystem(
		name: string,
	): Promise<EditWorkTrackingSystemDialog<WorkTrackingSystemsSettingsPage>> {
		await this.page
			.getByRole("row", { name: name })
			.getByRole("button")
			.first()
			.click();

		return new EditWorkTrackingSystemDialog(
			this.page,
			(page) => new WorkTrackingSystemsSettingsPage(page),
			true,
		);
	}

	async removeWorkTrackingSystem(name: string): Promise<void> {
		await this.page
			.getByRole("row", { name: name })
			.getByRole("button")
			.nth(1)
			.click();
		await this.page.getByRole("button", { name: "Delete" }).click();
	}

	getWorkTrackingSystem(name: string) {
		return this.page.getByRole("cell", { name: name });
	}
}
