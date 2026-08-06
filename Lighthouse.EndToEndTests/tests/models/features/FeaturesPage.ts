import type { Locator, Page } from "@playwright/test";

/**
 * Epic 5375 slice 01 — the Features view: every Feature the visitor may see, across every Portfolio,
 * in the order Lighthouse forecasts them, each row saying where it sits.
 */
export class FeaturesPage {
	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get featureRows(): Locator {
		return this.page.locator(".MuiDataGrid-row");
	}

	get helpText(): Locator {
		return this.page.getByText(/Lighthouse forecasts .* in this order/);
	}

	getFeatureRow(featureName: string): Locator {
		return this.featureRows.filter({ hasText: featureName });
	}

	/** The place the row holds across the whole instance, as rendered in the position column. */
	async getPosition(featureName: string): Promise<number> {
		const cell = this.getFeatureRow(featureName).locator(
			'[data-field="position"]',
		);
		const text = (await cell.innerText()).trim();
		return Number.parseInt(text, 10);
	}

	async getListedPositions(): Promise<number[]> {
		const cells = await this.featureRows
			.locator('[data-field="position"]')
			.allInnerTexts();
		return cells.map((text) => Number.parseInt(text.trim(), 10));
	}
}
