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

		const featureToggleRequest = this.page.waitForResponse(response => 
			response.url().includes('/api/optionalfeatures') && 
			response.status() === 200
		);

		await featureToggle.click();
		
		// Make sure the request is completed
		await featureToggleRequest;
	}
}
