import type { Page } from "@playwright/test";

export class OptionalFeaturesPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async enableFeature(featureName: string): Promise<void> {
		const featureToggle = this.page
			.getByTestId(`${featureName}-toggle`)
			.getByRole("checkbox");

		if (await featureToggle.isChecked()) {
			return;
		}

		await featureToggle.click();
	}
}
