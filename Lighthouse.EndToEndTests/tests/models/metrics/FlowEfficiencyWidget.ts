import type { Locator, Page } from "@playwright/test";
import {
	type ObservedRagStatus,
	RagChip,
	type RagStatus,
	ragLabelFor,
} from "./RagChip";

const WAIT_COLOUR_KEY_TEST_ID = "cumulative-state-time-wait-legend";
const WAIT_COLOUR_KEY_SWATCH_TEST_ID =
	"cumulative-state-time-wait-legend-swatch";
const CHART_EFFICIENCY_TEST_ID = "cumulative-state-time-flow-efficiency";
const TITLE_BLOCK_TEST_ID = "cumulative-state-time-title-block";
const BAR_TOOLTIP_TEST_ID = "cumulative-state-bar-tooltip";

export type { ObservedRagStatus, RagStatus } from "./RagChip";

export class FlowEfficiencyOverviewTile {
	private readonly widget: Locator;
	private readonly rag: RagChip;

	constructor(
		public readonly page: Page,
		widgetId = "flowEfficiency",
	) {
		this.widget = page.locator(`[data-testid="dashboard-item-${widgetId}"]`);
		this.rag = new RagChip(this.widget, widgetId);
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

	get notConfiguredMessage(): Locator {
		return this.widget.getByTestId("flow-efficiency-not-configured");
	}

	/** The RAG chip rendered by the shared WidgetShell header. */
	get ragChip(): Locator {
		return this.rag.chip;
	}

	get ragStatusValue(): Locator {
		return this.rag.statusValue;
	}

	async readRagStatus(): Promise<ObservedRagStatus> {
		return this.rag.readStatus();
	}

	async readRagLabel(): Promise<string> {
		return this.rag.readLabel();
	}

	static labelFor(status: RagStatus): string {
		return ragLabelFor(status);
	}

	async readRagTipText(): Promise<string> {
		return this.rag.readTipText();
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

	get waitColourKeySwatch(): Locator {
		return this.widget.getByTestId(WAIT_COLOUR_KEY_SWATCH_TEST_ID);
	}

	async readWaitColourKeySwatchBackground(): Promise<string> {
		return this.waitColourKeySwatch.evaluate(
			(node) => globalThis.getComputedStyle(node).backgroundColor,
		);
	}

	get barTooltip(): Locator {
		return this.page.getByTestId(BAR_TOOLTIP_TEST_ID);
	}

	get barTooltipRows(): Locator {
		return this.barTooltip.locator(
			'[data-testid^="cumulative-state-bar-tooltip-row-"]',
		);
	}

	async hoverFirstBar(): Promise<void> {
		const bar = this.widget
			.locator('rect.MuiBarChart-element:not([fill^="url("])')
			.first();
		await bar.waitFor({ state: "visible" });
		await bar.hover({ force: true });
	}

	completionLegendButton(label: "Completed" | "Ongoing"): Locator {
		return this.widget.getByRole("button", {
			name: `${label} visibility toggle`,
		});
	}
}
