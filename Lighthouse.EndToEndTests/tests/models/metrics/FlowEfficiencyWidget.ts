import type { Locator, Page } from "@playwright/test";

// POM for the Flow Efficiency overview tile (US-03) and the cumulative-chart efficiency
// number + wait-bar highlight (US-02 / US-04). DISTILL authors the POM; DELIVER un-fixmes the
// spec and validates these locators against the running app (the data-testid values below are
// the proposed contract for the DELIVER components, locator-validated when un-fixme'd).
const WAIT_BAR_LEGEND_TEST_ID = "cumulative-state-time-wait-legend";
const CHART_EFFICIENCY_TEST_ID = "cumulative-state-time-efficiency";

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
		return this.widget.getByTestId("flow-efficiency-value");
	}

	async readEfficiencyText(): Promise<string> {
		return (await this.efficiencyValue.innerText()) ?? "";
	}

	get ragStatusIndicator(): Locator {
		return this.widget.getByTestId("rag-status");
	}

	async getRagStatus(): Promise<string> {
		return (await this.ragStatusIndicator.getAttribute("data-rag")) ?? "";
	}
}

// The efficiency number + wait-bar highlight live on the existing Cumulative Time per State chart
// (US-02 / US-04). This POM exposes just those two surfaces; the chart's bars/segments/picker stay
// in CumulativeStateTimeChart.
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
		return this.widget
			.locator('rect.MuiBarChart-element[data-wait="true"]')
			.count();
	}
}
