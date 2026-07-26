import type { Locator, Page } from "@playwright/test";

export type PercentilesHorizon = 30 | 60 | 90;

/** The four percentiles plotted as red→green ramped lines (50/70/85/95). */
export const PERCENTILE_LINES = [50, 70, 85, 95] as const;

/** Own test id — the Work Item Age tab is horizon-less, so it is not a `percentiles-horizon-{n}`. */
const AGE_TOGGLE_TEST_ID = "percentiles-selection-age";

/**
 * The honest forward-only empty state (D6) a fresh owner reads instead of a
 * broken axis — verbatim, so a copy change here fails loudly.
 */
export const PERCENTILES_OVER_TIME_EMPTY_COPY =
	"builds forward from today — no snapshots recorded yet";

/**
 * The other honest empty state (slice-03b, D10/DDD-13): the owner may well have
 * history, just not inside the selected window. Shown when the range ends before
 * today — verbatim, so a copy change here fails loudly.
 */
export const PERCENTILES_OVER_TIME_RANGE_EMPTY_COPY =
	"no data recorded in the selected range";

/**
 * Drives the Percentiles Over Time widget (Predictability category, team +
 * portfolio). The widget renders an Age / CT-30 / CT-60 / CT-90 toggle row
 * (CT-30 pressed by default) above a MUI-X LineChart of four dated percentile
 * lines. Each selection is fetched once from the read-only
 * percentiles-over-time endpoint and cached, so toggling back to an
 * already-viewed selection re-plots from already-recorded history without
 * issuing a new request (no day-by-day recompute).
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

	horizonToggle(horizon: PercentilesHorizon): Locator {
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

	/**
	 * The Work Item Age tab. Age is measured as of today and therefore carries no
	 * horizon, so it gets its own test id rather than joining the
	 * `percentiles-horizon-{n}` family the cycle-time chips share.
	 */
	get ageToggle(): Locator {
		return this.widget.getByTestId(AGE_TOGGLE_TEST_ID);
	}

	async isAgeSelected(): Promise<boolean> {
		return (await this.ageToggle.getAttribute("aria-pressed")) === "true";
	}

	async selectAge(): Promise<void> {
		await this.ageToggle.click();
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
	 * How many days the chart actually plots, read off the rendered geometry of the
	 * first percentile line. MUI-X thins axis TICK LABELS to fit, so counting ticks
	 * would hide a narrowing that genuinely dropped days; the path's vertex count
	 * cannot. One command per plotted point: an `M` for the first, then an `L` per
	 * subsequent point when the curve is linear or a `C` when it is interpolated
	 * (MUI-X defaults to a monotone curve, so in practice these are `C`s).
	 */
	async countPlottedDays(): Promise<number> {
		if ((await this.chartLines.count()) === 0) {
			return 0;
		}
		const path = await this.chartLines.first().getAttribute("d");
		return path === null ? 0 : (path.match(/[MLC]/g) ?? []).length;
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
