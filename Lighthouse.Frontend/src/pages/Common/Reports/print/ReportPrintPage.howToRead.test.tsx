import { screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import {
	aPrintDialog,
	aTerminologyServiceAnswering,
	EXPORTED_AT,
	forgetTheBrowser,
	PRODUCED_LINE,
	printPathFor,
	renderTheExportScene,
	reportPathFor,
	theFontsHaveLoaded,
	thePrintPageIsReady,
	theTermsAnswer,
} from "../../../../tests/ReportPrintTestKit";
import {
	aReportService,
	defaultTerms,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	readingOf,
	theEngagementStartReport,
} from "../../../../tests/ReportsTestKit";

/**
 * A PDF travels without Lighthouse beside it, so it says how to read itself: three short lines under "How to read
 * this", between the header and the summary. Which side is frozen and when, what Now covers, and what the marks,
 * colours and the dash mean. The numbers in it are the report's own; the screen, where Lighthouse is there to
 * explain itself, does not show it.
 */

const HOW_TO_READ_THIS = "How to read this";
const THE_THIRD_LINE =
	"Each line compares Now with Then. Improved and Got worse say which way it moved (green better, red worse); — means there is no value, and the reason is printed beside it.";

const printing = async (
	report = theEngagementStartReport(),
	terms: Record<string, string> = defaultTerms,
) => {
	renderTheExportScene({
		reportService: aReportService({ getReport: async () => report }),
		startAt: printPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
		terminologyService: aTerminologyServiceAnswering(
			Promise.resolve(theTermsAnswer(terms)),
		),
	});
	await thePrintPageIsReady();
};

/** The block under the label, label included. */
const theHowToRead = (): HTMLElement => {
	const label = screen.getByText(HOW_TO_READ_THIS);
	const block =
		label.closest<HTMLElement>("[data-how-to-read]") ?? label.parentElement;
	expect(block, "the label stands alone").not.toBeNull();
	return block as HTMLElement;
};

/** Its three lines as a reader reads them, without the bullet each one starts with. */
const theLinesOfHowToRead = (): string[] =>
	Array.from(theHowToRead().querySelectorAll<HTMLElement>("li, p"))
		.map(readingOf)
		.map((line) => line.replace(/^·\s*/, ""))
		.filter((line) => line.length > 0 && line !== HOW_TO_READ_THIS);

const follows = (earlier: Node, later: Node) =>
	(earlier.compareDocumentPosition(later) &
		Node.DOCUMENT_POSITION_FOLLOWING) !==
	0;

describe("The PDF says how to read it", () => {
	beforeEach(() => {
		theFontsHaveLoaded();
		aPrintDialog();
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(EXPORTED_AT);
	});

	afterEach(() => {
		vi.useRealTimers();
		forgetTheBrowser();
	});

	// @us-03 @slice-03 @driving_port @contract-shape:bounded-change
	it(`says which side is frozen, what Now covers and what the marks mean, with the report's own numbers`, async () => {
		await printing();

		expect(theLinesOfHowToRead()).toEqual([
			"Then is frozen: the 90 days to 31 Jul 2026, exactly as captured on that day.",
			"Now is the 90 days up to 2 Oct 2026, read when this PDF was produced.",
			THE_THIRD_LINE,
		]);
	});

	// @us-03 @slice-03 @edge @contract-shape:bounded-change
	it(`takes each window's length and last day from the report it prints`, async () => {
		const report = theEngagementStartReport();
		await printing({
			...report,
			thenAndNow: {
				...(report.thenAndNow as NonNullable<typeof report.thenAndNow>),
				thenWindow: { start: "2026-05-17", end: "2026-06-15", lengthDays: 30 },
				nowWindow: { start: "2026-08-19", end: "2026-10-02", lengthDays: 45 },
			},
		});

		expect(theLinesOfHowToRead().slice(0, 2)).toEqual([
			"Then is frozen: the 30 days to 15 Jun 2026, exactly as captured on that day.",
			"Now is the 45 days up to 2 Oct 2026, read when this PDF was produced.",
		]);
	});

	// @us-03 @slice-03 @contract-shape:bounded-change
	it(`draws the marks it explains as the report draws them, green and red`, async () => {
		await printing();

		const thirdLine = Array.from(
			theHowToRead().querySelectorAll<HTMLElement>("li, p"),
		).find((line) =>
			readingOf(line).includes("Each line compares"),
		) as HTMLElement;
		const drawn = thirdLine.querySelectorAll("svg");
		expect(drawn).toHaveLength(2);
		for (const mark of drawn) {
			expect(mark).toHaveAttribute("aria-hidden", "true");
		}
		expect(readingOf(thirdLine)).not.toMatch(/[✔✘]/);
	});

	// @us-03 @slice-03 @contract-shape:bounded-change
	it(`sits between when the PDF was produced and the summary`, async () => {
		await printing();

		const produced = screen.getByText(PRODUCED_LINE);
		const summary = document.querySelector("[data-report-summary]") as Element;
		expect(follows(produced, theHowToRead())).toBe(true);
		expect(follows(theHowToRead(), summary)).toBe(true);
	});

	// @us-03 @slice-03 @edge @contract-shape:unbounded-preservation
	it(`uses none of the seeded words when the instance has its own`, async () => {
		await printing(theEngagementStartReport(), {
			...defaultTerms,
			[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
			[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
			[TERMINOLOGY_KEYS.CYCLE_TIME]: "Lead Time",
		});

		const howToRead = readingOf(theHowToRead());
		expect(theLinesOfHowToRead()).toHaveLength(3);
		expect(howToRead).not.toMatch(/Work Item|Cycle Time/);
	});
});

describe("The screen does not say how to read it", () => {
	beforeEach(() => {
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-03 @slice-03 @edge @contract-shape:unbounded-preservation
	// Guards the report view today and after the print page gains the lines.
	it("shows no how-to-read lines on the report view", async () => {
		renderTheExportScene({
			reportService: aReportService(),
			startAt: reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
			withPrintPages: false,
		});
		await waitFor(() =>
			expect(document.querySelector("[data-report-summary]")).not.toBeNull(),
		);

		expect(screen.queryByText(HOW_TO_READ_THIS)).toBeNull();
		expect(screen.queryByText(/Then is frozen/)).toBeNull();
	});
});
