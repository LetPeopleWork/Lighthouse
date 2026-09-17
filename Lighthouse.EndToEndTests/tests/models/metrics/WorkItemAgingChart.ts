import type { Locator, Page } from "@playwright/test";

const BACKGROUND_MODES_TEST_ID = "aging-background-modes";
const PACE_BAND_TEST_ID = "pace-band";
const SLE_RISK_ZONE_TEST_ID = "sle-risk-zone";
const AGE_BAND_CELL_TEST_ID = "ageBandColumnContent";
const SLE_RISK_CELL_TEST_ID = "sleRiskColumnContent";
const AGE_BAND_COLUMN_HEADER = "Work Item Age Band";

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

	/**
	 * The background is one channel, so the chart offers a choice of what to paint there rather
	 * than a switch per overlay. Only the modes the chart can actually paint are rendered: pace
	 * percentiles need per-state history, and the risk zones need a published target.
	 */
	get backgroundModes(): Locator {
		return this.widget.getByTestId(BACKGROUND_MODES_TEST_ID);
	}

	private backgroundMode(name: string | RegExp): Locator {
		return this.backgroundModes.getByRole("button", { name });
	}

	get paceBands(): Locator {
		return this.widget.getByTestId(PACE_BAND_TEST_ID);
	}

	async countPaceBands(): Promise<number> {
		return this.paceBands.count();
	}

	get sleRiskZones(): Locator {
		return this.widget.getByTestId(SLE_RISK_ZONE_TEST_ID);
	}

	async countSleRiskZones(): Promise<number> {
		return this.sleRiskZones.count();
	}

	async showPacePercentiles(): Promise<void> {
		await this.backgroundMode("Pace percentiles").click();
	}

	async showSleRisk(): Promise<void> {
		// Matched on the suffix because the term is renameable under Terminology.
		await this.backgroundMode(/Risk$/).click();
	}

	async hideBackground(): Promise<void> {
		await this.backgroundMode("Off").click();
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

	/**
	 * The band cells in the widget's View Data dialog. The dialog is rendered in a
	 * portal at the end of the document, so these are scoped to the dialog and not
	 * to the widget.
	 *
	 * Matched by test id rather than by band text. The band names are ordinary words
	 * that also appear in the chart behind the dialog and in the column's filter
	 * options, so a text match would count more than the cells and would change its
	 * answer as the chart finishes painting.
	 */
	get ageBandCells(): Locator {
		return this.page.getByRole("dialog").getByTestId(AGE_BAND_CELL_TEST_ID);
	}

	async countAgeBandCells(): Promise<number> {
		return this.ageBandCells.count();
	}

	async readAgeBands(): Promise<string[]> {
		return this.ageBandCells.allInnerTexts();
	}

	/**
	 * The risk cells in the widget's View Data dialog. Scoped to the dialog, which renders in a
	 * portal at the end of the document rather than inside the widget.
	 */
	get sleRiskCells(): Locator {
		return this.page.getByRole("dialog").getByTestId(SLE_RISK_CELL_TEST_ID);
	}

	async countSleRiskCells(): Promise<number> {
		return this.sleRiskCells.count();
	}

	/**
	 * Matched on the word alone rather than anchored: the grid folds its sort and column-menu
	 * affordances into the header's accessible name, so it reads "SLE Risk Sort SLE Risk column
	 * menu". Unanchored is also what survives the term being renamed under Terminology, and nothing
	 * else in this dialog carries the word.
	 */
	get sleRiskColumnHeader(): Locator {
		return this.page
			.getByRole("dialog")
			.getByRole("columnheader", { name: /Risk/ });
	}

	async sortBySleRisk(): Promise<void> {
		await this.sleRiskColumnHeader.click();
	}

	get ageBandColumnHeader(): Locator {
		return this.page
			.getByRole("dialog")
			.getByRole("columnheader", { name: AGE_BAND_COLUMN_HEADER });
	}

	async sortByAgeBand(): Promise<void> {
		await this.ageBandColumnHeader.click();
	}
}
