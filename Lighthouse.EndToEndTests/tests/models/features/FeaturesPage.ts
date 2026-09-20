import { expect, type Locator, type Page } from "@playwright/test";

/**
 * The Features view: every Feature the visitor may see, across every Portfolio,
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

	/**
	 * Addressed by the name cell rather than by the row's text: a row now carries the names of the
	 * Features it waits on as well as its own, so matching anywhere in the row finds both ends of a
	 * dependency and Playwright refuses to choose between them.
	 */
	getFeatureRow(featureName: string): Locator {
		return this.featureRows.filter({
			has: this.page.locator('[data-field="name"]', { hasText: featureName }),
		});
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
	 * Which Features this row is waiting on, as the Dependencies column renders them. Addressed by the
	 * column the cell belongs to rather than by what it prints: a Feature that waits on nothing gets a
	 * blank cell, so there is no text, dash or icon to look for.
	 */
	getDependenciesCell(featureName: string): Locator {
		return this.getFeatureRow(featureName).locator('[data-field="dependsOn"]');
	}

	/**
	 * When work on a Feature is expected to begin, as the Forecasted Start column renders it. Addressed
	 * by the column rather than by what it prints, for the same reason the dependencies cell is: the
	 * cell holds percentiles, an observed date or nothing at all, and only one of those has text worth
	 * looking for.
	 */
	getForecastedStartCell(featureName: string): Locator {
		return this.getFeatureRow(featureName).locator(
			'[data-field="startForecast"]',
		);
	}

	/** The column as a whole, for judging that the rows got an answer rather than a blank grid. */
	async getListedForecastedStarts(): Promise<string[]> {
		return this.readColumn("startForecast");
	}

	/** The sequence itself, which is what "nothing moved" is judged against. */
	async getListedFeatureNames(): Promise<string[]> {
		return this.readColumn("name");
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
		const cells = await this.readColumn("position");
		return cells.map((text) => Number.parseInt(text, 10));
	}

	/**
	 * The grid puts a row into the page before it fills that row's cells in, and reading every cell of
	 * a column happens in one shot that waits for nothing. Read a column the instant the rows appear
	 * and it can come back empty — which a test then reads as "the list is empty" rather than "the
	 * list is not drawn yet". So wait for the column's first cell, then read the column.
	 */
	private async readColumn(field: string): Promise<string[]> {
		const cells = this.featureRows.locator(`[data-field="${field}"]`);
		await expect(cells.first()).toBeVisible();
		const texts = await cells.allInnerTexts();
		return texts.map((text) => text.trim());
	}
}
