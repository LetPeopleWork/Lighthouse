import type { Locator, Page } from "@playwright/test";
import { ReportPrintPage } from "./ReportPrintPage";
import { ReportsListPage } from "./ReportsListPage";

/**
 * One report as a reader sees it. It is ready once the page marks the report itself as read; waiting for
 * mere visibility would accept the spinner shown while the report is on its way.
 */
export class ReportPage {
	constructor(private readonly page: Page) {}

	get ready(): Locator {
		return this.page.locator('[data-report-status="ready"]');
	}

	async waitUntilReady(): Promise<ReportPage> {
		await this.ready.waitFor();
		return this;
	}

	/** The report's name, the last part of the "Reports / <name>" line at the page's top. */
	get title(): Locator {
		return this.page
			.getByRole("navigation", { name: "breadcrumb" })
			.getByRole("listitem")
			.last();
	}

	/** The report's own address, as the browser shows it. */
	address(): string {
		return this.page.url();
	}

	async readName(): Promise<string> {
		return ((await this.title.textContent()) ?? "").trim();
	}

	/** The breadcrumb's "Reports" leads back to the list; the Reports tab is already selected here. */
	async backToReports(): Promise<ReportsListPage> {
		await this.page
			.getByRole("navigation", { name: "breadcrumb" })
			.getByRole("link", { name: "Reports", exact: true })
			.click();
		await this.page.waitForURL(/\/(teams|portfolios)\/\d+\/reports$/);
		return new ReportsListPage(this.page).waitUntilReady();
	}

	panel(title: string): Locator {
		return this.page.getByRole("region", { name: title });
	}

	/** How many entries the tab's history holds, which an export must leave as it found them. */
	async historyLength(): Promise<number> {
		return this.page.evaluate(() => globalThis.history.length);
	}

	/**
	 * Export PDF prints the report from a hidden frame on this page; stand in for the print dialog before calling
	 * this. Waits until the dialog has been asked for, on whichever window asked.
	 */
	async exportPdf(): Promise<void> {
		await this.page.getByRole("button", { name: "Export PDF" }).click();
		await this.page.waitForFunction(
			async () => {
				const seen = (
					globalThis as unknown as Record<
						string,
						{ page: number; frame: number } | undefined
					>
				).__lighthousePrints;
				return (seen?.page ?? 0) + (seen?.frame ?? 0) > 0;
			},
			undefined,
			{ timeout: 10_000 },
		);
	}

	/** How often the report's own print frame, rather than the page around it, asked for the print dialog. */
	async timesThePrintFrameAskedForTheDialog(): Promise<number> {
		return (await ReportPrintPage.printsSeen(this.page)).frame;
	}

	/** How often the page itself, around the report, asked for the print dialog; it never should. */
	async timesThePageAskedForTheDialog(): Promise<number> {
		return (await ReportPrintPage.printsSeen(this.page)).page;
	}

	/** Closes the print dialog the way the browser tells the frame it has closed, and waits for the frame to go. */
	async closeThePrintDialog(): Promise<void> {
		const frame = this.page.locator('iframe[title="Report PDF"]');
		await frame.evaluate((element: HTMLIFrameElement) => {
			const frameWindow = element.contentWindow;
			frameWindow?.dispatchEvent(new Event("afterprint"));
		});
		await frame.waitFor({ state: "detached" });
	}
}
