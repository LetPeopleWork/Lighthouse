import type { Locator } from "@playwright/test";
import type { DeliveryItem } from "./DeliveryItem";

const TIMELINE_TAB_TEST_ID = "delivery-timeline-tab";
const CHART_TEST_ID = "delivery-gantt";
const UNPLACEABLE_TEST_ID = "timeline-unplaceable";
const BAR_TEST_ID = "timeline-bar-content";
const PREMIUM_NOTICE_TEST_ID = "premium-feature-notice";

/**
 * The Delivery's Timeline tab.
 *
 * The bars are drawn by a third-party Gantt, and this page object deliberately reaches only for
 * the parts Lighthouse owns: the tab, the chart's own wrapper, and the bar contents, which are our
 * components rendered inside the library's bars. Nothing here selects on the library's own markup,
 * which would go red on their release rather than on a defect of ours.
 *
 * The marked columns are the exception that proves it: they are the library's cells, tinted by a
 * class of ours, so there is nothing here that can reach them. Whether they are drawn correctly is
 * the screenshot's job.
 */
export class DeliveryTimelineTab {
	private readonly container: Locator;

	constructor(deliveryItem: DeliveryItem) {
		this.container = deliveryItem.container;
	}

	private get deliveryTabs(): Locator {
		return this.container
			.page()
			.getByRole("tablist", { name: "delivery view tabs" });
	}

	async openTimelineTab(): Promise<void> {
		await this.deliveryTabs.getByRole("tab", { name: "Timeline" }).click();
	}

	get tab(): Locator {
		return this.container.page().getByTestId(TIMELINE_TAB_TEST_ID);
	}

	get chart(): Locator {
		return this.container.page().getByTestId(CHART_TEST_ID);
	}

	get unplaceableList(): Locator {
		return this.container.page().getByTestId(UNPLACEABLE_TEST_ID);
	}

	get premiumNotice(): Locator {
		return this.container.page().getByTestId(PREMIUM_NOTICE_TEST_ID);
	}

	get bars(): Locator {
		return this.container.page().getByTestId(BAR_TEST_ID);
	}

	async countBars(): Promise<number> {
		return this.bars.count();
	}

	/** The theme the chart is wrapped in, which the adapter records on its own element. */
	async chartThemeMode(): Promise<string | null> {
		return this.chart.getAttribute("data-theme-mode");
	}

	async chooseProbability(percentage: 70 | 85 | 95): Promise<void> {
		await this.tab
			.getByRole("button", { name: `${percentage}%`, exact: true })
			.click();
	}
}
