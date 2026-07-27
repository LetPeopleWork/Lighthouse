import type { Locator, Page } from "@playwright/test";

const PACE_BANDS_TOGGLE_TEST_ID = "pace-bands-toggle";
const PACE_BAND_TEST_ID = "pace-band";

export class WorkItemAgingChart {
	private readonly widget: Locator;

	constructor(
		public readonly page: Page,
		widgetId: string,
	) {
		this.widget = page.locator(`[data-testid="dashboard-item-${widgetId}"]`);
	}

	get chart(): Locator {
		return this.widget;
	}

	get pacePercentilesToggle(): Locator {
		return this.widget.getByTestId(PACE_BANDS_TOGGLE_TEST_ID);
	}

	get paceBands(): Locator {
		return this.widget.getByTestId(PACE_BAND_TEST_ID);
	}

	async countPaceBands(): Promise<number> {
		return this.paceBands.count();
	}

	async togglePacePercentiles(): Promise<void> {
		await this.pacePercentilesToggle.click();
	}

	/**
	 * The clickable percentile chips above the chart (PercentileLegend renders each
	 * as a MUI Chip with an onClick, so they carry role=button and an accessible
	 * name like "85%").
	 *
	 * Scoped to the BUTTON role on purpose. A plain `getByText(/^\d+%$/)` also
	 * matched the chart's own cycle-time reference-line labels, which carry the same
	 * "50%/70%/85%/95%" text but are painted only once the chart has data. That made
	 * the count race the chart's paint: 4 (chips only) while it was still loading,
	 * 8 (chips + reference-line labels) once painted. A spec that snapshotted the
	 * count early and compared it later then failed with "Expected 4, Received 8" —
	 * on verifypostgres only, because sqlite painted fast enough to snapshot 8 both
	 * times (CI runs 30250405183, 30258220986, 30262655725).
	 */
	get cycleTimePercentileChips(): Locator {
		return this.widget.getByRole("button", { name: /^\d+%$/ });
	}

	async countCycleTimePercentileChips(): Promise<number> {
		return this.cycleTimePercentileChips.count();
	}
}
