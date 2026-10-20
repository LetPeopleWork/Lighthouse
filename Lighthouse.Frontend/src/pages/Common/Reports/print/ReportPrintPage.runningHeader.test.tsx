import { screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IReportOwner } from "../../../../models/Reports/Report";
import {
	aPrintDialog,
	BRANDING,
	EXPORTED_AT,
	elementsPrintedAs,
	forgetTheBrowser,
	PRODUCED_LINE,
	printPathFor,
	renderTheExportScene,
	theFontsHaveLoaded,
	thePrintPageIsReady,
	WINDOWS_LINE,
	whatPrintAsksOf,
} from "../../../../tests/ReportPrintTestKit";
import {
	APOLLO,
	aReportService,
	ENGAGEMENT_START,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	readingOf,
	theEngagementStartReport,
} from "../../../../tests/ReportsTestKit";

/**
 * Every printed page says what it is and who made the tool: one line at the top naming the owner, the report, its
 * two windows and when it was produced, and the Lighthouse mark with its credit at the bottom. The first page also
 * carries the report's name as its big title, under that line. jsdom cannot paginate, so what is pinned is what
 * makes the browser repeat the line and the footer on each page: both fixed to the page when printed, with room
 * kept for them at the top and bottom of every page so no panel runs underneath.
 */

const pendingEpic6298 =
	"Pending: every printed page carries the report's line and the credit (epic 6298, slice 02)";

const thePrintedReportOf = async (owner: IReportOwner) => {
	renderTheExportScene({
		startAt: printPathFor(owner, ENGAGEMENT_START_ID),
		reportService: aReportService({
			getReport: vi.fn().mockResolvedValue(
				theEngagementStartReport({
					ownerKind: owner.kind,
					ownerId: owner.id,
				}),
			),
		}),
	});
	await thePrintPageIsReady();
};

/** The line at the top of every page, found by when the PDF was produced, which it alone carries. */
const theRunningLine = (): HTMLElement => {
	const line = screen.getByText(PRODUCED_LINE).closest("header");
	expect(line, "the produced date is not in a header").not.toBeNull();
	return line as HTMLElement;
};

/** The footer, found by the credit it carries. */
const theFooter = (): HTMLElement => {
	const footer = screen.getByText(BRANDING).closest("footer");
	expect(footer, "the credit is not in a footer").not.toBeNull();
	return footer as HTMLElement;
};

/** The parts of the line as a reader meets them: the innermost elements holding its words. */
const thePartsOf = (line: HTMLElement): string[] =>
	Array.from(line.querySelectorAll<HTMLElement>("*"))
		.filter(
			(element) =>
				readingOf(element) !== "" &&
				!Array.from(element.children).some(
					(inner) => readingOf(inner as HTMLElement) !== "",
				),
		)
		.map(readingOf);

/** The groups the browser repeats on every printed page, which is where the room for the line and footer is kept. */
const theGroupsPrintedAs = (
	display: "table-header-group" | "table-footer-group",
): HTMLElement[] => elementsPrintedAs(display);

/** Whether the group, or something in it, asks on paper for the height the repeated element takes. */
const keepsRoomFor = (group: HTMLElement, height: string): boolean =>
	[group, ...Array.from(group.querySelectorAll<HTMLElement>("*"))].some(
		(element) => whatPrintAsksOf(element, "height") === height,
	);

const isAtTheEdge = (value: string) => /^0(px|mm)?$/.test(value);

const thePanelsAndSummary = (): Element[] => {
	const summary = document.querySelector("[data-report-summary]");
	expect(summary).not.toBeNull();
	return [summary as Element, ...screen.getAllByRole("region")];
};

describe("Every printed page carries the report's line and the credit", () => {
	beforeEach(() => {
		theFontsHaveLoaded();
		aPrintDialog();
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(EXPORTED_AT);
	});

	afterEach(() => {
		vi.useRealTimers();
		forgetTheBrowser();
		document.title = "";
	});

	// @us-02 @slice-02 @driving_port @contract-shape:bounded-change
	// The dates read as the report view writes them.
	// Fails today: the header holds the big title among its lines instead of one line of four parts.
	it.skip.each<[string, IReportOwner, string]>([
		["a Team's", LIGHTSPEED, "Team Lightspeed"],
		["a Portfolio's", APOLLO, "Portfolio Apollo"],
	])(
		`heads the page of %s report with one line: owner, report, windows, when it was produced (${pendingEpic6298})`,
		async (_, owner, ownerLine) => {
			await thePrintedReportOf(owner);

			const line = theRunningLine();
			expect(thePartsOf(line)).toEqual([
				ownerLine,
				ENGAGEMENT_START,
				WINDOWS_LINE,
				PRODUCED_LINE,
			]);
			expect(within(line).queryByRole("heading")).toBeNull();
			expect(within(line).getByRole("separator")).toBeInTheDocument();
		},
	);

	// @us-02 @slice-02 @contract-shape:bounded-change
	// Fails today: the title sits inside the header, among the lines that are to repeat.
	it.skip(`keeps the report's name as the big title under that line, on the first page only (${pendingEpic6298})`, async () => {
		await thePrintedReportOf(LIGHTSPEED);

		const titles = screen.getAllByRole("heading", { level: 1 });
		expect(titles).toHaveLength(1);
		const [title] = titles;
		expect(title).toHaveTextContent(ENGAGEMENT_START);
		expect(title).toBeVisible();
		const line = theRunningLine();
		expect(line.contains(title)).toBe(false);
		expect(
			line.compareDocumentPosition(title) & Node.DOCUMENT_POSITION_FOLLOWING,
		).toBeTruthy();
		expect(whatPrintAsksOf(title, "position")).not.toBe("fixed");
		for (const group of theGroupsPrintedAs("table-header-group")) {
			expect(group.contains(title)).toBe(false);
		}
	});

	// @us-02 @slice-02 @edge @contract-shape:bounded-change
	// A line fixed to the page is printed on every sheet; without room kept for it, page two's panels run under it.
	// Fails today: the line flows with the report and prints on the first page only.
	it.skip(`repeats the line at the top of every printed page, with room kept for it above the report (${pendingEpic6298})`, async () => {
		await thePrintedReportOf(LIGHTSPEED);

		const line = theRunningLine();
		expect(whatPrintAsksOf(line, "position")).toBe("fixed");
		expect(isAtTheEdge(whatPrintAsksOf(line, "top"))).toBe(true);
		const height = whatPrintAsksOf(line, "height");
		expect(height, "the line takes no fixed height on paper").toMatch(/\d/);

		const roomAbove = theGroupsPrintedAs("table-header-group");
		expect(roomAbove, "no room is kept at the top of each page").toHaveLength(
			1,
		);
		const [room] = roomAbove;
		expect(keepsRoomFor(room, height)).toBe(true);
		const report = room.parentElement as HTMLElement;
		for (const part of thePanelsAndSummary()) {
			expect(report.contains(part)).toBe(true);
			expect(room.contains(part)).toBe(false);
		}
	});

	// @us-02 @slice-02 @edge @contract-shape:bounded-change
	// Page numbers were offered and declined.
	// Fails today: the footer flows after the last panel and prints on the last page only.
	it.skip(`repeats the Lighthouse mark and its credit at the bottom of every printed page, with no page numbers (${pendingEpic6298})`, async () => {
		await thePrintedReportOf(LIGHTSPEED);

		expect(screen.getAllByText(BRANDING)).toHaveLength(1);
		const footer = theFooter();
		expect(
			within(footer).getByRole("img", { name: /Lighthouse/ }),
		).toBeVisible();
		expect(whatPrintAsksOf(footer, "position")).toBe("fixed");
		expect(isAtTheEdge(whatPrintAsksOf(footer, "bottom"))).toBe(true);
		const height = whatPrintAsksOf(footer, "height");
		expect(height, "the footer takes no fixed height on paper").toMatch(/\d/);

		const roomBelow = theGroupsPrintedAs("table-footer-group");
		expect(
			roomBelow,
			"no room is kept at the bottom of each page",
		).toHaveLength(1);
		const [room] = roomBelow;
		expect(keepsRoomFor(room, height)).toBe(true);
		const report = room.parentElement as HTMLElement;
		for (const part of thePanelsAndSummary()) {
			expect(report.contains(part)).toBe(true);
			expect(room.contains(part)).toBe(false);
		}

		const styles = Array.from(document.querySelectorAll("style"))
			.map((style) => style.textContent ?? "")
			.join("\n");
		expect(styles).not.toMatch(/counter\(\s*pages?\s*\)/);
		expect(readingOf(document.body)).not.toMatch(/\bPage \d/);
	});
});
