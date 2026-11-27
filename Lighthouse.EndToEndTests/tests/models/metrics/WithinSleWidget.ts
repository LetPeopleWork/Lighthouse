import type { Locator, Page } from "@playwright/test";
import { MetricsWidget } from "./MetricsPage";
import { WorkItemsDialog } from "./WorkItemsDialog";

const WIDGET_KEY = "withinSle";

export class WithinSleWidget {
	private readonly frame: MetricsWidget;

	constructor(public readonly page: Page) {
		this.frame = new MetricsWidget(page, "Within SLE", WIDGET_KEY);
	}

	get widget(): Locator {
		return this.frame.Widget;
	}

	get title(): Locator {
		return this.widget.getByText("Within SLE", { exact: true });
	}

	/** The large share, for example "72.7 % (8 / 11)". */
	get share(): Locator {
		return this.widget.getByText(/^\d+(\.\d)? % \(\d+ \/ \d+\)$/);
	}

	get sentence(): Locator {
		return this.widget.getByText(/^of .+ closed within \d+ days or less$/);
	}

	get target(): Locator {
		return this.widget.getByText(/^Target \d+ %$/);
	}

	async waitUntilLoaded(): Promise<void> {
		await this.frame.waitUntilLoaded();
	}

	async readShare(): Promise<string> {
		await this.waitUntilLoaded();
		return (await this.share.innerText()) ?? "";
	}

	/** The closed count of the share text, the second number in "(8 / 11)". */
	async readClosedCount(): Promise<number> {
		const text = await this.readShare();
		return Number(/\/ (\d+)\)/.exec(text)?.[1]);
	}

	async openViewData(): Promise<WorkItemsDialog> {
		return this.frame.openDialog();
	}
}
