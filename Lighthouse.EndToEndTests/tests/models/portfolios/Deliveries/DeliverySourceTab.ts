import type { Locator, Page } from "@playwright/test";

/**
 * The tab that previews the delivery date a work tracking system already holds. It only ever looks:
 * nothing on it writes anything, so there is no save path to model here.
 */
export class DeliverySourceTab {
	private readonly page: Page;
	private readonly sourceName: string;

	constructor(page: Page, sourceName: string) {
		this.page = page;
		this.sourceName = sourceName;
	}

	get picker(): Locator {
		return this.page.getByRole("combobox", { name: this.sourceName });
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
