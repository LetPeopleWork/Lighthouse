import type { Locator } from "@playwright/test";
import type { DeliveryItem } from "./DeliveryItem";

const BURNUP_CHART_TEST_ID = "delivery-burnup-chart";
const LINE_ELEMENT_SELECTOR = "path.MuiLineChart-line";

export class DeliveryMetricsTab {
	private readonly container: Locator;

	constructor(deliveryItem: DeliveryItem) {
		this.container = deliveryItem.container;
	}

	get burnupChart(): Locator {
		return this.container
			.page()
			.locator(`[data-testid="${BURNUP_CHART_TEST_ID}"]`);
	}

	private get deliveryTabs(): Locator {
		return this.container
			.page()
			.getByRole("tablist", { name: "delivery view tabs" });
	}

	async openMetricsTab(): Promise<void> {
		await this.deliveryTabs.getByRole("tab", { name: "Metrics" }).click();
	}

	async openWorkItemsTab(): Promise<void> {
		await this.deliveryTabs.getByRole("tab", { name: "Work Items" }).click();
	}

	async countSeriesLines(): Promise<number> {
		return this.burnupChart.locator(LINE_ELEMENT_SELECTOR).count();
	}
}
