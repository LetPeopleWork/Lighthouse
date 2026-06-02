import type { Locator } from "@playwright/test";
import type { DeliveryItem } from "./DeliveryItem";

const BURNUP_CHART_TEST_ID = "delivery-burnup-chart";
const LINE_ELEMENT_SELECTOR = "path.MuiLineChart-line";
const ESTIMATED_CAPTION_PATTERN =
	/are estimated \(features not yet broken down\)/;

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

	get estimatedItemsCaption(): Locator {
		return this.burnupChart.getByText(ESTIMATED_CAPTION_PATTERN);
	}

	async countDrawnSeriesLines(): Promise<number> {
		const segmentCommands = await this.burnupChart
			.locator(LINE_ELEMENT_SELECTOR)
			.evaluateAll((paths) =>
				paths.map((path) => (path.getAttribute("d")?.match(/C/g) ?? []).length),
			);
		return segmentCommands.filter((commands) => commands > 0).length;
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
