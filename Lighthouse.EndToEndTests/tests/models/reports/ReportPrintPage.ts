import type { Locator, Page } from "@playwright/test";

const PRINTS_SEEN = "__lighthousePrints";
const TYPEFACE_AT_READY = "__lighthouseTypefaceAtReady";

/** A4's width less the page's 12 mm margin on each side, in the browser's pixels of 96 to the inch: 186 mm. */
const A4_PRINTABLE_WIDTH = 703;

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

	/**
	 * Opens the print page by its address, in a page of its own, so nothing the report view already fetched is in it.
	 * A print page opened by its address never prints.
	 */
	static async openByItsAddress(
		page: Page,
		reportAddress: string,
	): Promise<ReportPrintPage> {
		const report = new URL(reportAddress);
		await page.goto(`${report.origin}/print${report.pathname}`);
		return new ReportPrintPage(page).waitUntilReady();
	}

	/**
	 * Keeps back the part of the Lighthouse typeface that draws Central European letters (Ł, ő, ş) for a few
	 * seconds each time a page asks for it, as a slow connection would.
	 */
	static async holdBackTheTypefacesCentralEuropeanLetters(
		page: Page,
	): Promise<void> {
		await page.route("**/quicksand-latin-ext-*.woff2", async (route) => {
			await new Promise((held) => setTimeout(held, 3000));
			await route.continue();
		});
	}

	/**
	 * Notes, the moment a page this tab loads from now on says it is ready, whether the Lighthouse typeface could
	 * already draw every letter on it. Call before the page loads.
	 */
	static async watchTheTypefaceWhenThePageSaysReady(page: Page): Promise<void> {
		await page.addInitScript((seen: string) => {
			const record = globalThis as unknown as Record<string, unknown>;
			const observer = new MutationObserver(() => {
				if (document.querySelector('[data-render-ready="true"]') === null) {
					return;
				}
				record[seen] = document.fonts.check(
					'400 1em "Quicksand Variable"',
					document.body.innerText,
				);
				observer.disconnect();
			});
			observer.observe(document, {
				subtree: true,
				childList: true,
				attributes: true,
				attributeFilter: ["data-render-ready"],
			});
		}, TYPEFACE_AT_READY);
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
	 * the whole report to fit, so its text prints small and the sheet comes out part empty. Measured as wide as
	 * A4 leaves room for: on the test's wide window, a page far too wide for paper still fits.
	 */
	async reachesPastThePageEdgeInPrint(): Promise<boolean> {
		const theTestsWindow = this.page.viewportSize();
		await this.page.setViewportSize({
			width: A4_PRINTABLE_WIDTH,
			height: theTestsWindow?.height ?? 1080,
		});
		await this.page.emulateMedia({ media: "print" });
		try {
			return await this.page.evaluate(
				() =>
					document.documentElement.scrollWidth >
					document.documentElement.clientWidth,
			);
		} finally {
			await this.page.emulateMedia({ media: null });
			if (theTestsWindow !== null) {
				await this.page.setViewportSize(theTestsWindow);
			}
		}
	}

	/** The colour of the line that sets the first panel's Now off from its Then, as the page prints it. */
	async colourOfTheLineBeforeNowInPrint(): Promise<string> {
		await this.page.emulateMedia({ media: "print" });
		try {
			return await this.page
				.locator('[data-segment="now"]')
				.first()
				.evaluate((now) => getComputedStyle(now).borderLeftColor);
		} finally {
			await this.page.emulateMedia({ media: null });
		}
	}

	/** Whether the Lighthouse typeface could draw every letter on the page the moment it said it was ready. */
	async couldTheTypefaceDrawEveryLetterWhenReady(): Promise<boolean> {
		return this.page.evaluate(
			(seen) =>
				(globalThis as unknown as Record<string, unknown>)[seen] === true,
			TYPEFACE_AT_READY,
		);
	}

	/**
	 * The fonts the browser embeds when it turns this page into a PDF, read off the PDF itself. Chromium draws a
	 * variable font as a Type3 font, which has no BaseFont and is named only in its font descriptor, so both
	 * names are read.
	 */
	async fontsEmbeddedInThePdf(): Promise<string[]> {
		const pdf = (await this.page.pdf({ format: "A4" })).toString("latin1");
		const named = Array.from(
			pdf.matchAll(/\/(?:BaseFont|FontName)\s*\/([^\s/<>[\]()]+)/g),
		).map(([, font]) => font.replace(/^[A-Z]{6}\+/, ""));
		return [...new Set(named)];
	}
}
