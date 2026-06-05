import type { Locator, Page } from "@playwright/test";

const WAIT_COLOUR_KEY_TEST_ID = "cumulative-state-time-wait-legend";
const CHART_EFFICIENCY_TEST_ID = "cumulative-state-time-flow-efficiency";
const TITLE_BLOCK_TEST_ID = "cumulative-state-time-title-block";

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

	get titleBlock(): Locator {
		return this.widget.getByTestId(TITLE_BLOCK_TEST_ID);
	}

	get efficiencyNumber(): Locator {
		return this.titleBlock.getByTestId(CHART_EFFICIENCY_TEST_ID);
	}

	async readEfficiencyText(): Promise<string> {
		return (await this.efficiencyNumber.innerText()) ?? "";
	}

	get waitColourKey(): Locator {
		return this.widget.getByTestId(WAIT_COLOUR_KEY_TEST_ID);
	}

	completionLegendButton(label: "Completed" | "Ongoing"): Locator {
		return this.widget.getByRole("button", {
			name: `${label} visibility toggle`,
		});
	}
}
