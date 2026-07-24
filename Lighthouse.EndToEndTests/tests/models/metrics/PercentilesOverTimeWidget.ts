import type { Locator, Page } from "@playwright/test";

export type PercentilesHorizon = 30 | 60 | 90;

/** The four percentiles plotted as red→green ramped lines (50/70/85/95). */
export const PERCENTILE_LINES = [50, 70, 85, 95] as const;

/**
 * Drives the Percentiles Over Time widget (Predictability category, team +
 * portfolio). The widget renders a CT-30/60/90 horizon toggle (CT-30 pressed by
 * default) above a MUI-X LineChart of four dated percentile lines. Each horizon
 * is fetched once from the read-only percentiles-over-time endpoint and cached,
 * so toggling back to an already-viewed horizon re-plots from already-recorded
 * history without issuing a new request (no day-by-day recompute).
 */
export class PercentilesOverTimeWidget {
	private readonly widget: Locator;

	constructor(public readonly page: Page) {
		this.widget = page.getByTestId("percentiles-over-time-widget");
	}

	get Widget(): Locator {
		return this.widget;
	}

	get emptyState(): Locator {
		return this.widget.getByTestId("percentiles-over-time-empty");
	}

	get legend(): Locator {
		return this.widget.getByTestId("percentiles-over-time-legend");
	}

	private horizonToggle(horizon: PercentilesHorizon): Locator {
		return this.widget.getByTestId(`percentiles-horizon-${horizon}`);
	}

	async isHorizonSelected(horizon: PercentilesHorizon): Promise<boolean> {
		return (
			(await this.horizonToggle(horizon).getAttribute("aria-pressed")) ===
			"true"
		);
	}

	async selectHorizon(horizon: PercentilesHorizon): Promise<void> {
		await this.horizonToggle(horizon).click();
	}

	private legendEntry(percentile: number): Locator {
		return this.widget.getByTestId(`percentile-line-${percentile}`);
	}

	async countLegendEntries(): Promise<number> {
		return this.legend.getByTestId(/^percentile-line-/).count();
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
	 * Reads the rendered RGB of a legend swatch, so the spec can assert the
	 * red→green ramp (50 reddest, 95 greenest) without pinning exact theme hexes.
	 */
	async legendSwatchColor(
		percentile: number,
	): Promise<{ r: number; g: number; b: number }> {
		const swatch = this.legendEntry(percentile).locator("div").first();
		const color = await swatch.evaluate(
			(node) => getComputedStyle(node).backgroundColor,
		);
		const match = color.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
		if (match === null) {
			throw new Error(`Unexpected swatch colour "${color}" for p${percentile}`);
		}
		return {
			r: Number(match[1]),
			g: Number(match[2]),
			b: Number(match[3]),
		};
	}
}
