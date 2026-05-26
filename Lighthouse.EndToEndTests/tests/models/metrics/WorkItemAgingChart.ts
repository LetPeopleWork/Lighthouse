import type { Locator, Page } from "@playwright/test";

const PACE_PERCENTILES_CHIP_LABEL = "Pace percentiles";
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

	get pacePercentilesChip(): Locator {
		return this.widget.locator(".MuiChip-root", {
			hasText: PACE_PERCENTILES_CHIP_LABEL,
		});
	}

	get paceBands(): Locator {
		return this.widget.getByTestId(PACE_BAND_TEST_ID);
	}

	async countPaceBands(): Promise<number> {
		return this.paceBands.count();
	}

	async togglePacePercentiles(): Promise<void> {
		await this.pacePercentilesChip.click();
	}

	get cycleTimePercentileChips(): Locator {
		return this.widget.getByText(/^\d+%$/);
	}

	async countCycleTimePercentileChips(): Promise<number> {
		return this.cycleTimePercentileChips.count();
	}
}
