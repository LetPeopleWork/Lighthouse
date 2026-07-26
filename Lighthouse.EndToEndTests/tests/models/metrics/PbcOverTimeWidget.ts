import type { Locator, Page } from "@playwright/test";

/**
 * The process-behaviour metric families the recorder persists a dated series
 * for. Only Throughput is recorded today; later slices append here rather than
 * restructuring the toggle row.
 */
export type PbcMetricType = "Throughput";

/**
 * The three limit lines the widget plots per recorded day, in the point-in-time
 * process-behaviour chart's vocabulary (UNPL / Average / LNPL).
 */
export const PBC_LIMIT_LINES = ["unpl", "average", "lnpl"] as const;

export type PbcLimitLine = (typeof PBC_LIMIT_LINES)[number];

/**
 * The honest forward-only empty state (D6) a fresh owner reads instead of a
 * broken axis — verbatim, so a copy change here fails loudly.
 */
export const PBC_OVER_TIME_EMPTY_COPY =
	"builds forward from today — no snapshots recorded yet";

/**
 * Drives the PBC Over Time widget (Predictability category, team + portfolio).
 * The widget renders a metric-family toggle row (Throughput pressed by default)
 * above a MUI-X LineChart of the dated UNPL / Average / LNPL triple the
 * recorder persisted, one point per recorded day. A fresh owner legitimately
 * has no history and gets the forward-only copy instead of a fabricated axis.
 */
export class PbcOverTimeWidget {
	private readonly widget: Locator;

	constructor(public readonly page: Page) {
		this.widget = page.getByTestId("pbc-over-time-widget");
	}

	get Widget(): Locator {
		return this.widget;
	}

	get emptyState(): Locator {
		return this.widget.getByTestId("pbc-over-time-empty");
	}

	get legend(): Locator {
		return this.widget.getByTestId("pbc-over-time-legend");
	}

	/** Mirrors the widget's exported `processBehaviorMetricTestId` convention. */
	metricToggle(metricType: PbcMetricType): Locator {
		return this.widget.getByTestId(`pbc-metric-${metricType.toLowerCase()}`);
	}

	async isMetricSelected(metricType: PbcMetricType): Promise<boolean> {
		return (
			(await this.metricToggle(metricType).getAttribute("aria-pressed")) ===
			"true"
		);
	}

	async selectMetric(metricType: PbcMetricType): Promise<void> {
		await this.metricToggle(metricType).click();
	}

	private legendEntry(line: PbcLimitLine): Locator {
		return this.widget.getByTestId(`pbc-line-${line}`);
	}

	async countLegendEntries(): Promise<number> {
		return this.legend.getByTestId(/^pbc-line-/).count();
	}

	// MUI-X LineChart renders one <path class="MuiLineChart-line"> per series.
	private get chartLines(): Locator {
		return this.widget.locator("path.MuiLineChart-line");
	}

	async countChartLines(): Promise<number> {
		return this.chartLines.count();
	}

	// The dated x-axis renders one tick label per recorded day. A populated,
	// truly "dated" trend has more than one so the lines actually span a window.
	private get axisTickLabels(): Locator {
		return this.widget.locator(".MuiChartsAxis-tickLabel");
	}

	async countAxisTickLabels(): Promise<number> {
		return this.axisTickLabels.count();
	}

	/**
	 * The plotted path for one limit line. MUI-X tags each rendered line with a
	 * data-series attribute carrying the series id and emits no per-series class,
	 * so an id-in-class selector would silently match nothing.
	 */
	private limitLinePath(line: PbcLimitLine): Locator {
		return this.widget.locator(`path.MuiLineChart-line[data-series="${line}"]`);
	}

	/**
	 * The rendered dash pattern of a limit line, in CSS pixels. The point-in-time
	 * process-behaviour chart draws its average with "5 5" and its natural limits
	 * with "3 3"; reading the computed value lets the spec assert the over-time
	 * limits speak the same visual language without pinning emotion class names.
	 */
	async limitLineDashPattern(line: PbcLimitLine): Promise<number[]> {
		const dashArray = await this.limitLinePath(line).evaluate(
			(node) => getComputedStyle(node).strokeDasharray,
		);
		return dashArray
			.split(",")
			.map((segment) => Number.parseFloat(segment.trim()))
			.filter((segment) => !Number.isNaN(segment));
	}

	/**
	 * The rendered stroke of a limit line. The three limits are one band drawn in
	 * a single neutral colour, so the spec asserts they agree rather than pinning
	 * a theme hex.
	 */
	async limitLineStrokeColor(line: PbcLimitLine): Promise<string> {
		return this.limitLinePath(line).evaluate(
			(node) => getComputedStyle(node).stroke,
		);
	}

	/** The dashed swatch the legend draws for a limit line. */
	async isLimitLineInLegend(line: PbcLimitLine): Promise<boolean> {
		return (await this.legendEntry(line).count()) === 1;
	}
}
