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
	 * Addressed by the entry's own name rather than by the whole row: the row also carries the
	 * project it came from and either its date or the reason it cannot be picked, and matching
	 * anywhere in it would find rows that merely mention the name.
	 */
	option(name: string): Locator {
		return this.page
			.getByRole("option")
			.filter({ has: this.page.getByText(name, { exact: true }) });
	}

	async isSelectable(name: string): Promise<boolean> {
		return (await this.option(name).getAttribute("aria-disabled")) !== "true";
	}

	/**
	 * The date the list shows against an entry, in whatever format this browser writes dates. Read
	 * off the screen rather than constructed, so the check still holds after the date moves.
	 *
	 * Addressed by the element that holds it rather than by looking for the first thing in the row
	 * that reads as a date: the row leads with the entry's own name, and a Release called "2027 Q1"
	 * parses as one.
	 */
	async listedDateFor(name: string): Promise<string> {
		const shown = this.option(name).getByTestId("delivery-source-option-date");

		if ((await shown.count()) === 0) {
			throw new Error(`The list shows no date against "${name}"`);
		}

		return (await shown.innerText()).trim();
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
