import type { Locator, Page } from "@playwright/test";

const PRINTS_SEEN = "__lighthousePrints";

/**
 * A report's print page, the page Export PDF leads to. It is ready once the page says everything it prints is
 * in; waiting for mere visibility would accept the "Preparing the PDF…" look. The browser's print dialog is
 * stood in for before the page loads, so a test can count how often the page asked for it and close it the
 * way the browser does.
 */
export class ReportPrintPage {
	constructor(private readonly page: Page) {}

	/**
	 * Replaces the print dialog in the page already open and in every page this tab loads from now on; call
	 * before Export PDF. Moving between pages of the app loads nothing, so the open page needs it too.
	 */
	static async standInForThePrintDialog(page: Page): Promise<void> {
		const standIn = (seen: string) => {
			const record = globalThis as unknown as Record<string, number>;
			record[seen] = 0;
			globalThis.print = () => {
				record[seen] += 1;
			};
		};
		await page.addInitScript(standIn, PRINTS_SEEN);
		await page.evaluate(standIn, PRINTS_SEEN);
	}

	get ready(): Locator {
		return this.page.locator('[data-render-ready="true"]');
	}

	async waitUntilReady(): Promise<ReportPrintPage> {
		await this.ready.waitFor();
		return this;
	}

	/** The report's name, the page's one top-level heading. */
	get title(): Locator {
		return this.page.getByRole("heading", { level: 1 });
	}

	/**
	 * The header line naming who the report is about: the owner's kind, in the instance's own word for it,
	 * then the owner's name. A demo Team called "Team Lightspeed" therefore reads "Team Team Lightspeed".
	 */
	owner(ownerName: string): Locator {
		const escaped = ownerName.replaceAll(
			/[.*+?^${}()|[\]\\]/g,
			String.raw`\$&`,
		);
		return this.page
			.getByRole("banner")
			.getByText(new RegExp(String.raw`^\S.* ${escaped}$`));
	}

	async timesThePrintDialogOpened(): Promise<number> {
		return this.page.evaluate(
			(seen) => (globalThis as unknown as Record<string, number>)[seen] ?? 0,
			PRINTS_SEEN,
		);
	}

	/** Closes the print dialog the way the browser tells a page it has closed, and waits to be back on the report. */
	async closeThePrintDialog(): Promise<void> {
		await this.page.evaluate(() => {
			globalThis.dispatchEvent(new Event("afterprint"));
		});
		// Matched on the whole path, so the print page's own address, which ends the same way, cannot satisfy it.
		await this.page.waitForURL((url) =>
			/^\/(teams|portfolios)\/\d+\/reports\/\d+$/.test(url.pathname),
		);
	}

	/**
	 * Whether anything on the page, laid out for paper, reaches past the page's right edge. The browser then shrinks
	 * the whole report to fit, so its text prints small and the sheet comes out part empty.
	 */
	async reachesPastThePageEdgeInPrint(): Promise<boolean> {
		await this.page.emulateMedia({ media: "print" });
		const wider = await this.page.evaluate(
			() =>
				document.documentElement.scrollWidth >
				document.documentElement.clientWidth,
		);
		await this.page.emulateMedia({ media: null });
		return wider;
	}

	/** The fonts the browser embeds when it turns this page into a PDF, read off the PDF itself. */
	async fontsEmbeddedInThePdf(): Promise<string[]> {
		const pdf = (await this.page.pdf({ format: "A4" })).toString("latin1");
		const named = Array.from(
			pdf.matchAll(/\/BaseFont\s*\/([^\s/<>[\]()]+)/g),
		).map(([, font]) => font.replace(/^[A-Z]{6}\+/, ""));
		return [...new Set(named)];
	}
}
