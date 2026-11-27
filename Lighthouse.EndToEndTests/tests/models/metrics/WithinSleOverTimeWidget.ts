import type { Locator, Page } from "@playwright/test";
import { MetricsWidget } from "./MetricsPage";

export type WithinSleHorizon = 30 | 60 | 90;

const WIDGET_KEY = "withinSleOverTime";

/**
 * Drives the Within SLE Over Time chart (Predictability category, Teams and Portfolios): a
 * 30 / 60 / 90 day toggle above a line of the daily share with a stepped target line.
 */
export class WithinSleOverTimeWidget {
	private readonly frame: MetricsWidget;

	constructor(public readonly page: Page) {
		this.frame = new MetricsWidget(page, "Within SLE Over Time", WIDGET_KEY);
	}

	get widget(): Locator {
		return this.frame.Widget;
	}

	get title(): Locator {
		return this.widget.getByText("Within SLE Over Time", { exact: true });
	}

	horizonToggle(horizon: WithinSleHorizon): Locator {
		return this.widget.getByRole("button", { name: `${horizon} days` });
	}

	async isHorizonSelected(horizon: WithinSleHorizon): Promise<boolean> {
		return (
			(await this.horizonToggle(horizon).getAttribute("aria-pressed")) ===
			"true"
		);
	}

	async waitUntilLoaded(): Promise<void> {
		await this.frame.waitUntilLoaded();
	}

	// MUI-X LineChart renders one <path class="MuiLineChart-line"> per series: the share and the target.
	async countChartLines(): Promise<number> {
		await this.waitUntilLoaded();
		return this.widget.locator("path.MuiLineChart-line").count();
	}

	/**
	 * How many days the share line plots, read off the vertex count of its path. Past days are filled in
	 * from history in the background the first time the chart is opened, so a freshly seeded instance shows
	 * one day until the chart is opened again.
	 */
	async countPlottedDays(): Promise<number> {
		await this.waitUntilLoaded();
		const path = await this.widget
			.locator("path.MuiLineChart-line")
			.first()
			.getAttribute("d");
		return path === null ? 0 : (path.match(/[MLC]/g) ?? []).length;
	}
}
