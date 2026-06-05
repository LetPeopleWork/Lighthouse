import type { Locator, Page } from "@playwright/test";

const WAIT_BAR_LEGEND_TEST_ID = "cumulative-state-time-wait-legend";
const CHART_EFFICIENCY_TEST_ID = "cumulative-state-time-flow-efficiency";
const WAIT_PATTERN_FILL = "url(#cumulative-state-time-wait-pattern)";

export class FlowEfficiencyOverviewTile {
	private readonly widget: Locator;

	constructor(
		public readonly page: Page,
		widgetId = "flowEfficiency",
	) {
		this.widget = page.locator(`[data-testid="dashboard-item-${widgetId}"]`);
	}

	get tile(): Locator {
		return this.widget;
	}

	get efficiencyValue(): Locator {
		return this.widget.getByTestId("flow-efficiency-percent");
	}

	async readEfficiencyText(): Promise<string> {
		return (await this.efficiencyValue.innerText()) ?? "";
	}
}

export class CumulativeChartFlowEfficiency {
	private readonly widget: Locator;

	constructor(
		public readonly page: Page,
		widgetId: string,
	) {
		this.widget = page.locator(`[data-testid="dashboard-item-${widgetId}"]`);
	}

	get efficiencyNumber(): Locator {
		return this.widget.getByTestId(CHART_EFFICIENCY_TEST_ID);
	}

	async readEfficiencyText(): Promise<string> {
		return (await this.efficiencyNumber.innerText()) ?? "";
	}

	get waitBarLegendEntry(): Locator {
		return this.widget.getByTestId(WAIT_BAR_LEGEND_TEST_ID);
	}

	async countWaitBars(): Promise<number> {
		return this.widget.locator(`rect[fill='${WAIT_PATTERN_FILL}']`).count();
	}
}
