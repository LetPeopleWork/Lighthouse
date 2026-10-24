import { expect, type Locator, type Page } from "@playwright/test";

/**
 * The tab that previews the delivery date a work tracking system already holds. Picking an entry
 * here fills the name and the date on the surrounding dialog, which is where saving lives - so this
 * tab has no save of its own to model.
 */
export class DeliverySourceTab {
	private readonly page: Page;
	private readonly sourceName: string;

	constructor(page: Page, sourceName: string) {
		this.page = page;
		this.sourceName = sourceName;
	}

	/**
	 * The tab's body. It is on screen before the entries are, so seeing it says nothing about them;
	 * its `data-widget-status` does.
	 */
	get body(): Locator {
		return this.page.getByTestId("delivery-source-tab-body");
	}

	/**
	 * Waits until the body says it is ready or that its entries could not be loaded. On the second, it fails once the
	 * expect timeout runs out, never earlier, naming the entries that could not be loaded.
	 */
	async waitUntilLoaded(timeout = 60_000): Promise<void> {
		const body = '[data-testid="delivery-source-tab-body"]';
		await this.page
			.locator(
				`${body}[data-widget-status="ready"], ${body}[data-widget-status="error"]`,
			)
			.waitFor({ timeout });
		await expect(
			this.body,
			`the ${this.sourceName} entries could not be loaded`,
		).toHaveAttribute("data-widget-status", "ready");
	}

	get picker(): Locator {
		return this.page.getByRole("combobox", {
			name: this.sourceName,
			exact: true,
		});
	}

	async openList(): Promise<void> {
		await this.picker.click();
	}

	/**
	 * Addressed by the entry's own name rather than by the whole row: the row also names the project
	 * the entry came from, and where it cannot be picked, why, so matching anywhere in the row would
	 * find rows that merely mention the name.
	 */
	option(name: string): Locator {
		return this.page
			.getByRole("option")
			.filter({ has: this.page.getByText(name, { exact: true }) });
	}

	async isSelectable(name: string): Promise<boolean> {
		return (await this.option(name).getAttribute("aria-disabled")) !== "true";
	}

	async pick(name: string): Promise<void> {
		await this.option(name).click();
	}

	get preview(): Locator {
		return this.page.getByTestId("delivery-source-preview");
	}

	get previewSummary(): Locator {
		return this.preview.getByRole("heading");
	}

	get previewGrid(): Locator {
		return this.preview.getByRole("grid");
	}

	/**
	 * How many entries the preview holds. Taken from the grid's own count, which includes the header
	 * row and stays right whether or not a row happens to be drawn at the moment.
	 */
	async previewedCount(): Promise<number> {
		const rows = await this.previewGrid.getAttribute("aria-rowcount");
		return Number(rows) - 1;
	}

	previewed(name: string): Locator {
		return this.previewGrid.getByText(name, { exact: true });
	}
}
