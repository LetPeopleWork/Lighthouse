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

	/**
	 * Only the Features grid carries a position column. Every other page's grids have rows and a name
	 * column too, so an unscoped row locator is satisfied by whatever page is still on screen.
	 */
	private get grid(): Locator {
		return this.page.locator(".MuiDataGrid-root").filter({
			has: this.page.locator(
				'.MuiDataGrid-columnHeader[data-field="position"]',
			),
		});
	}

	get featureRows(): Locator {
		return this.grid.locator(".MuiDataGrid-row");
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

	/** One line per dependency the row waits on, as the Dependencies cell draws them. */
	dependencyLinesOf(featureName: string): Locator {
		return this.getDependenciesCell(featureName).locator(
			'[data-testid^="depends-on-"]',
		);
	}

	/**
	 * What the person sees of one dependency line: whether it is cut and ends with an ellipsis, whether it stays on a single
	 * line, and whether the text it shows ends inside the Dependencies cell that holds it.
	 */
	async describeDependencyLine(
		featureName: string,
		index: number,
	): Promise<{
		isCut: boolean;
		endsWithEllipsis: boolean;
		isOneLine: boolean;
		endsInsideCell: boolean;
	}> {
		const cell = this.getDependenciesCell(featureName);
		const cellBox = await cell.boundingBox();
		return this.dependencyLinesOf(featureName)
			.nth(index)
			.evaluate(
				(line, cellRight) => {
					const box = line.getBoundingClientRect();
					const text = document.createRange();
					text.selectNodeContents(line);
					// The text can run on past the line when it is cut; what the person sees ends where the line clips it.
					const visibleTextRight = Math.min(
						text.getBoundingClientRect().right,
						box.right,
					);
					const lineHeight = Number.parseFloat(
						getComputedStyle(line).lineHeight,
					);
					return {
						isCut: line.scrollWidth > line.clientWidth,
						endsWithEllipsis:
							getComputedStyle(line).textOverflow === "ellipsis" &&
							getComputedStyle(line).overflow === "hidden",
						isOneLine: box.height <= lineHeight + 1,
						endsInsideCell:
							cellRight !== null && visibleTextRight <= cellRight + 1,
					};
				},
				cellBox === null ? null : cellBox.x + cellBox.width,
			);
	}

	async hoverDependenciesOf(featureName: string): Promise<void> {
		await this.getDependenciesCell(featureName).hover();
	}

	/** A tooltip is drawn outside the grid, at the end of the page, so it cannot be found inside the cell. */
	tooltipNaming(text: string): Locator {
		return this.page.getByRole("tooltip", { name: text });
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
		const header = this.grid.locator(
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
