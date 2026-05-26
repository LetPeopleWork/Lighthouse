import type { Locator, Page } from "@playwright/test";

const BAR_ELEMENT_CLASS = "MuiBarChart-element";
const ONGOING_HATCH_FILL = "url(#cumulative-state-time-ongoing-hatch)";
const EMPTY_PLACEHOLDER_TEST_ID = "cumulative-state-time-empty";
const ZERO_PLACEHOLDER_TEST_ID = "cumulative-state-time-zero";
const ITEM_PICKER_TEST_ID = "cumulative-state-time-item-picker";
const PARENT_EXPAND_TEST_ID = "cumulative-state-time-parent-expand";
const DATA_GRID_ROW_CLASS = "MuiDataGrid-row";

export class CumulativeStateTimeChart {
	private readonly widget: Locator;

	constructor(
		public readonly page: Page,
		widgetId: string,
	) {
		this.widget = page.locator(`[data-testid="dashboard-item-${widgetId}"]`);
	}

	get chart(): Locator {
		return this.widget;
	}

	get completedSegments(): Locator {
		return this.widget.locator(`rect.${BAR_ELEMENT_CLASS}:not([fill^="url("])`);
	}

	async countCompletedSegments(): Promise<number> {
		return this.completedSegments.count();
	}

	get ongoingSegments(): Locator {
		return this.widget.locator(
			`rect.${BAR_ELEMENT_CLASS}[fill="${ONGOING_HATCH_FILL}"]`,
		);
	}

	async countOngoingSegments(): Promise<number> {
		return this.ongoingSegments.count();
	}

	get stateBars(): Locator {
		return this.completedSegments;
	}

	async countStateBars(): Promise<number> {
		return this.stateBars.count();
	}

	tooltipForState(stateName: string): Locator {
		return this.widget.getByTestId(`cumulative-state-tooltip-${stateName}`);
	}

	tooltipField(
		stateName: string,
		field:
			| "total"
			| "completed"
			| "ongoing"
			| "mean"
			| "median"
			| "item-count"
			| "completed-count"
			| "ongoing-count",
	): Locator {
		return this.tooltipForState(stateName).getByTestId(`tooltip-${field}`);
	}

	get emptyPlaceholder(): Locator {
		return this.widget.getByTestId(EMPTY_PLACEHOLDER_TEST_ID);
	}

	get zeroPlaceholder(): Locator {
		return this.widget.getByTestId(ZERO_PLACEHOLDER_TEST_ID);
	}

	private async tallestBar(): Promise<Locator> {
		const bars = this.completedSegments;
		const count = await bars.count();
		let tallest = bars.first();
		let tallestHeight = 0;
		for (let index = 0; index < count; index++) {
			const bar = bars.nth(index);
			const box = await bar.boundingBox();
			const height = box?.height ?? 0;
			if (height > tallestHeight) {
				tallestHeight = height;
				tallest = bar;
			}
		}
		return tallest;
	}

	async clickConstraintBar(): Promise<CumulativeStateTimeDrillDownDialog> {
		const bar = await this.tallestBar();
		await bar.click({ force: true });
		return new CumulativeStateTimeDrillDownDialog(this.page);
	}

	get itemPicker(): Locator {
		return this.widget.getByTestId(ITEM_PICKER_TEST_ID);
	}

	async searchPicker(query: string): Promise<void> {
		await this.itemPicker.click();
		await this.itemPicker.getByRole("combobox").fill(query);
	}

	async selectPickerOption(label: string): Promise<void> {
		await this.page.getByRole("option", { name: label, exact: false }).click();
	}

	get parentExpandAction(): Locator {
		return this.page.getByTestId(PARENT_EXPAND_TEST_ID);
	}

	async expandParentChildren(): Promise<void> {
		await this.parentExpandAction.click();
	}

	async clearPicker(): Promise<void> {
		await this.itemPicker.getByLabel("Clear").click();
	}

	async countSelectedPickerChips(): Promise<number> {
		return this.itemPicker.getByRole("button").count();
	}
}

export class CumulativeStateTimeDrillDownDialog {
	private readonly dialog: Locator;

	constructor(public readonly page: Page) {
		this.dialog = page.getByRole("dialog");
	}

	get container(): Locator {
		return this.dialog;
	}

	get rows(): Locator {
		return this.dialog.locator(`.${DATA_GRID_ROW_CLASS}`);
	}

	async countRows(): Promise<number> {
		return this.rows.count();
	}

	async close(): Promise<void> {
		await this.page.keyboard.press("Escape");
	}
}
