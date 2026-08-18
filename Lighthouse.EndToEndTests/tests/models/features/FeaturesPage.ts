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

	/**
	 * How many other Features this row is waiting on, as the Depends On column renders it. Addressed
	 * by the column the cell belongs to rather than by what it prints: a Feature that waits on nothing
	 * gets a blank cell, so there is no text, dash or icon to look for.
	 */
	getDependsOnCell(featureName: string): Locator {
		return this.getFeatureRow(featureName).locator(
			'[data-field="dependsOnCount"]',
		);
	}

	/** Epic 5375 slice 02 — the sequence itself, which is what "nothing moved" is judged against. */
	async getListedFeatureNames(): Promise<string[]> {
		const cells = await this.featureRows
			.locator('[data-field="name"]')
			.allInnerTexts();
		return cells.map((text) => text.trim());
	}

	/** "#" while the tracker owns the order, the manual heading once this instance does. */
	async getPositionColumnHeading(): Promise<string> {
		const header = this.page.locator(
			'.MuiDataGrid-columnHeader[data-field="position"] .MuiDataGrid-columnHeaderTitle',
		);
		return (await header.innerText()).trim();
	}

	/**
	 * The row action menu holding the four move gestures. Waits on the move request itself rather than
	 * on a rendered state: the grid reorders optimistically, so a row that has already jumped says
	 * nothing about whether the instance accepted the move.
	 */
	async moveToTop(featureName: string): Promise<void> {
		await this.getFeatureRow(featureName)
			.getByRole("button", { name: /move/i })
			.click();

		const theMoveItself = this.page.waitForResponse(
			(response) =>
				response.request().method() === "PATCH" &&
				response.url().includes("/rank"),
		);

		await this.page.getByRole("menuitem", { name: "Move to Top" }).click();
		await theMoveItself;
	}

	async getListedPositions(): Promise<number[]> {
		const cells = await this.featureRows
			.locator('[data-field="position"]')
			.allInnerTexts();
		return cells.map((text) => Number.parseInt(text.trim(), 10));
	}
}
