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
		
		await featureToggle.waitFor({ state: 'visible', timeout: 10000 });

		if (await featureToggle.isChecked()) {
			return;
		}

		await featureToggle.click();
	}
}
