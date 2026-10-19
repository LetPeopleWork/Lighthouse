import { screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import {
	aPrintDialog,
	BRANDING,
	forgetTheBrowser,
	printPathFor,
	renderTheExportScene,
	theFontsHaveLoaded,
	thePrintPageIsReady,
} from "../../../../tests/ReportPrintTestKit";
import {
	aReportService,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
} from "../../../../tests/ReportsTestKit";

/**
 * On paper nothing is cut in two: the summary and each panel stay whole on one page, the footer goes with the
 * last panel rather than onto a page of its own, and the page keeps a margin every printer can reach on A4 and
 * US Letter alike. Where the pages actually break is the browser's to decide, so what is pinned here is what
 * the page asks of it.
 */

const pendingEpic6298 =
	"Pending: nothing is cut in two on paper (epic 6298, slice 02)";

const thePrintedReport = async () => {
	renderTheExportScene({
		reportService: aReportService(),
		startAt: printPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
	});
	await thePrintPageIsReady();
};

const keptWhole = (element: Element) =>
	getComputedStyle(element).getPropertyValue("break-inside");

const theStylesOfThePage = () =>
	Array.from(document.querySelectorAll("style"))
		.map((style) => style.textContent ?? "")
		.join("\n");

describe("The printed report is never cut in two", () => {
	beforeEach(() => {
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-02 @slice-02 @contract-shape:unbounded-preservation
	it.skip(`keeps the summary and every panel whole on one page (${pendingEpic6298})`, async () => {
		await thePrintedReport();

		const summary = document.querySelector("[data-report-summary]");
		expect(summary).not.toBeNull();
		expect(keptWhole(summary as Element)).toBe("avoid");
		const panels = screen.getAllByRole("region");
		expect(panels).toHaveLength(4);
		for (const panel of panels) {
			expect(keptWhole(panel)).toBe("avoid");
		}
	});

	// @us-02 @slice-02 @edge @contract-shape:bounded-change
	// A footer alone on a last page is a page printed for nothing.
	it.skip(`keeps the footer on the page of the last panel (${pendingEpic6298})`, async () => {
		await thePrintedReport();

		const panels = screen.getAllByRole("region");
		let keptTogether: HTMLElement | null = screen.getByText(BRANDING);
		while (keptTogether !== null && keptWhole(keptTogether) !== "avoid") {
			keptTogether = keptTogether.parentElement;
		}

		expect(
			keptTogether,
			"nothing keeps the footer with a panel",
		).not.toBeNull();
		expect(keptTogether?.contains(panels[panels.length - 1])).toBe(true);
		expect(keptTogether?.contains(panels[0])).toBe(false);
	});

	// @us-02 @slice-02 @edge @contract-shape:bounded-change
	it.skip(`asks for a 12 mm margin on every printed page (${pendingEpic6298})`, async () => {
		await thePrintedReport();

		expect(theStylesOfThePage()).toMatch(/@page\s*\{[^}]*margin:\s*12mm/);
	});
});
