import type { Locator, Page } from "@playwright/test";
import { BaseSettingsPage } from "./BaseSettingsPage";

export abstract class BaseAddWizard<T> extends BaseSettingsPage<T> {
	abstract setName(name: string): Promise<void>;

	async goToNextStep(): Promise<void> {
		await this.nextButton.click();
	}

	async create<TNew>(createPageHandler: (page: Page) => TNew): Promise<TNew> {
		await this.createButton.click();
		return createPageHandler(this.page);
	}

	async selectWorkTrackingSystem(
		workTrackingSystemName: string,
	): Promise<void> {
		await this.page
			.getByRole("button", { name: workTrackingSystemName })
			.click();
	}

	get nextButton(): Locator {
		return this.page.getByRole("button", { name: "Next" });
	}

	get createButton(): Locator {
		return this.page.getByRole("button", { name: "Create" });
	}

	get cancelButton(): Locator {
		return this.page.getByRole("button", { name: "Cancel" });
	}

	get backButton(): Locator {
		return this.page.getByRole("button", { name: "Back" });
	}
}
