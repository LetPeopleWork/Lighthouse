import type { Locator, Page } from "@playwright/test";

export class CycleTimeScatterPlotChart {
	page: Page;
	widgetId: string;

	constructor(page: Page, widgetId: string) {
		this.page = page;
		this.widgetId = widgetId;
	}

	get Widget(): Locator {
		return this.page.locator(`[data-testid="dashboard-item-${this.widgetId}"]`);
	}

	get cycleTimeSelector(): Locator {
		return this.Widget.getByRole("combobox", { name: /cycle time/i });
	}

	get dots(): Locator {
		return this.Widget.locator("circle > title");
	}

	async countDots(): Promise<number> {
		return this.dots.count();
	}

	async getSelectedDefinition(): Promise<string> {
		return (await this.cycleTimeSelector.textContent())?.trim() ?? "";
	}

	async listDefinitionOptions(): Promise<string[]> {
		await this.cycleTimeSelector.click();
		const options = this.page.getByRole("option");
		await options.first().waitFor();
		const labels = await options.allTextContents();
		await this.page.keyboard.press("Escape");
		return labels.map((label) => label.trim());
	}

	async selectDefinition(name: string): Promise<void> {
		await this.cycleTimeSelector.click();
		await this.page.getByRole("option", { name, exact: true }).click();
	}

	async getDotCycleTimes(): Promise<number[]> {
		const count = await this.dots.count();
		const values: number[] = [];
		for (let index = 0; index < count; index++) {
			const title = (await this.dots.nth(index).textContent()) ?? "";
			const match = title.match(/cycle time (\d+) days/i);
			if (match) {
				values.push(Number(match[1]));
			}
		}
		return values.sort((a, b) => a - b);
	}
}
