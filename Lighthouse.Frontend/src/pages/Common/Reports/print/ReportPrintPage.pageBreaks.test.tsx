import { screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import {
	aPrintDialog,
	forgetTheBrowser,
	printPathFor,
	renderTheExportScene,
	theFontsHaveLoaded,
	thePrintOnlyDeclarations,
	thePrintPageIsReady,
	whatPrintAsksOf,
} from "../../../../tests/ReportPrintTestKit";
import {
	aReportService,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
} from "../../../../tests/ReportsTestKit";

/**
 * On paper nothing is cut in two and nothing is wasted. The report is laid out for A4 portrait and still fits US
 * Letter: the summary and each panel stay whole on one page, every panel keeps Then, Now and Change side by side
 * however narrow the paper, the page keeps a margin every printer can reach, and no empty page follows the last
 * one. Where the pages actually break is the browser's to decide and jsdom cannot paginate, so what is pinned here
 * is what the page asks of the browser when it prints.
 */

/** The values that ask for no height of the element's own, so a short report ends where its content ends. */
const NO_HEIGHT_OF_ITS_OWN = ["auto", "0", "0px", "none", "initial", "unset"];

const thePrintedReport = async () => {
	renderTheExportScene({
		reportService: aReportService(),
		startAt: printPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
	});
	await thePrintPageIsReady();
};

const thePrintPage = (): HTMLElement => {
	const page = document.querySelector<HTMLElement>("[data-render-ready]");
	expect(page, "there is no print page").not.toBeNull();
	return page as HTMLElement;
};

/** The grid that sets a panel's Then, Now and Change beside each other. */
const theSegmentsOf = (panel: HTMLElement): HTMLElement => {
	const then = panel.querySelector<HTMLElement>('[data-segment="then"]');
	expect(then, "the panel has no Then").not.toBeNull();
	return (then as HTMLElement).parentElement as HTMLElement;
};

const theStylesOfThePage = () =>
	Array.from(document.querySelectorAll("style"))
		.map((style) => style.textContent ?? "")
		.join("\n");

const thePageRule = (): string => {
	const rule = /@page\s*\{([^}]*)\}/.exec(theStylesOfThePage());
	expect(rule, "the page asks nothing of the printed page").not.toBeNull();
	return (rule as RegExpExecArray)[1];
};

describe("The printed report is never cut in two", () => {
	beforeEach(() => {
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-02 @slice-02 @contract-shape:unbounded-preservation
	it(`keeps the summary and every panel whole on one page`, async () => {
		await thePrintedReport();

		const summary = document.querySelector("[data-report-summary]");
		expect(summary).not.toBeNull();
		expect(whatPrintAsksOf(summary as Element, "break-inside")).toBe("avoid");
		const panels = screen.getAllByRole("region");
		expect(panels).toHaveLength(4);
		for (const panel of panels) {
			expect(whatPrintAsksOf(panel, "break-inside")).toBe("avoid");
		}
	});

	// @us-02 @slice-02 @edge @contract-shape:bounded-change
	// The screen stacks the three columns below its breakpoint, and A4 portrait is narrower than that breakpoint.
	it(`keeps Then, Now and Change side by side in every panel, however narrow the paper`, async () => {
		await thePrintedReport();

		const panels = screen.getAllByRole("region");
		expect(panels).toHaveLength(4);
		for (const panel of panels) {
			const segments = theSegmentsOf(panel);
			expect(
				Array.from(segments.children).map(
					(segment) => (segment as HTMLElement).dataset.segment,
				),
			).toEqual(["then", "now", "change"]);
			expect(
				whatPrintAsksOf(segments, "grid-template-columns"),
				"a panel stacks its columns on paper",
			).toMatch(/^repeat\(3,/);
		}
	});

	// @us-02 @slice-02 @edge @contract-shape:bounded-change
	// A paper size of the page's own would override the reader's A4 or Letter choice in the print dialog.
	it(`asks for a 12 mm margin on every printed page and leaves the paper size to the reader`, async () => {
		await thePrintedReport();

		const pageRule = thePageRule();
		expect(pageRule).toMatch(/margin:\s*12mm/);
		expect(pageRule).not.toMatch(/size:/);
	});

	// @us-02 @slice-02 @error @contract-shape:unbounded-preservation
	// On screen the page fills the window; on paper that height spills over onto a last page holding nothing.
	it(`prints no empty page after the report`, async () => {
		await thePrintedReport();

		expect(NO_HEIGHT_OF_ITS_OWN).toContain(
			whatPrintAsksOf(thePrintPage(), "min-height"),
		);
		expect(thePrintOnlyDeclarations()).not.toMatch(/\d+(\.\d+)?[dls]?vh\b/);
	});
});
