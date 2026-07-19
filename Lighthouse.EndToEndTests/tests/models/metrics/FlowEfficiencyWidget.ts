import type { Locator, Page } from "@playwright/test";

const WAIT_COLOUR_KEY_TEST_ID = "cumulative-state-time-wait-legend";
const WAIT_COLOUR_KEY_SWATCH_TEST_ID =
	"cumulative-state-time-wait-legend-swatch";
const CHART_EFFICIENCY_TEST_ID = "cumulative-state-time-flow-efficiency";
const TITLE_BLOCK_TEST_ID = "cumulative-state-time-title-block";
const BAR_TOOLTIP_TEST_ID = "cumulative-state-bar-tooltip";

export type RagStatus = "red" | "amber" | "green";
export type ObservedRagStatus = RagStatus | "unknown";

const RAG_STATUS_LABELS: Record<RagStatus, string> = {
	red: "Act",
	amber: "Observe",
	green: "Sustain",
};

function toRagStatus(raw: string | null): ObservedRagStatus {
	if (raw === "red" || raw === "amber" || raw === "green") {
		return raw;
	}

	// Deliberately distinct: an absent or unrecognised attribute must NOT
	// collapse into a legitimate status, otherwise locator drift would read
	// as a product regression (or pass vacuously).
	return "unknown";
}

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

	get notConfiguredMessage(): Locator {
		return this.widget.getByTestId("flow-efficiency-not-configured");
	}

	/** The RAG chip rendered by the shared WidgetShell header. */
	get ragChip(): Locator {
		return this.widget.getByTestId("widget-rag-flowEfficiency");
	}

	/**
	 * `rag-status` is a shared test id across every widget, so it is scoped
	 * inside this widget's chip to avoid a strict-mode violation.
	 */
	get ragStatusValue(): Locator {
		return this.ragChip.getByTestId("rag-status");
	}

	async readRagStatus(): Promise<ObservedRagStatus> {
		return toRagStatus(await this.ragStatusValue.getAttribute("data-rag"));
	}

	async readRagLabel(): Promise<string> {
		return (await this.ragStatusValue.innerText()).trim();
	}

	static labelFor(status: RagStatus): string {
		return RAG_STATUS_LABELS[status];
	}

	/**
	 * The chip's accessible name, which MUI derives from the RAG tip text.
	 * Read from the chip itself rather than a hovered popper: a bare
	 * `getByRole("tooltip")` also matches the widget's info popover and trips
	 * strict mode intermittently.
	 */
	async readRagTipText(): Promise<string> {
		return (await this.ragChip.getAttribute("aria-label")) ?? "";
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
