import { screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	aPrintDialog,
	EXPORT_PDF,
	exportAsPdfLabelOf,
	forgetTheBrowser,
	PDF_EXPORT_STARTED,
	printPathFor,
	renderTheExportScene,
	reportsPathFor,
	THEN_AND_NOW,
	theFontsHaveLoaded,
	theStandInForThePrintPage,
	whatThisEntryCarries,
	whereTheReaderIs,
} from "../../../tests/ReportPrintTestKit";
import {
	aListedReport,
	aListedReportOfALaterType,
	aReportList,
	aReportService,
	ENGAGEMENT_START,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	readingOf,
} from "../../../tests/ReportsTestKit";

/**
 * Export PDF from the Reports list. A PDF icon leads every row's actions, for everyone who can read the
 * reports: an editor sees it before Edit and Delete, and a reader, who had no actions before, gets an Actions
 * column holding only it. A row this version cannot show offers no PDF, as it offers no Edit. Choosing it
 * counts the same export as the button on the report and goes to the same print page.
 */

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

const atTheList = reportsPathFor(LIGHTSPEED);

const aListOf = (...reports: ReturnType<typeof aListedReport>[]) =>
	aReportService({
		getReports: vi.fn().mockResolvedValue(aReportList(reports)),
	});

const onTheList = (
	reportService = aReportService(),
	viewer: "admin" | "reader" = "admin",
) =>
	renderTheExportScene({
		reportService,
		viewer,
		startAt: atTheList,
		withPrintPages: false,
	});

/** The row of one report, found by its link; the row's controls are read by their labels, never by role. */
const theRowOf = async (name: string): Promise<HTMLElement> => {
	const row = (await screen.findByRole("link", { name })).closest<HTMLElement>(
		'[role="row"]',
	);
	expect(row, `${name} has no row`).not.toBeNull();
	return row as HTMLElement;
};

const theActionsOf = async (name: string): Promise<string[]> =>
	Array.from((await theRowOf(name)).querySelectorAll<HTMLElement>("button"))
		.map((button) => button.getAttribute("aria-label") ?? "")
		.filter((label) => label.length > 0);

const exportsReported = () =>
	reportUsage.mock.calls.filter(([use]) => use.name === PDF_EXPORT_STARTED);

describe("Export PDF in the Reports list", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		localStorage.clear();
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(() => {
		localStorage.clear();
		forgetTheBrowser();
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it(`leads an editor's row actions with the PDF icon, before Edit and Delete`, async () => {
		onTheList();

		expect(await theActionsOf(ENGAGEMENT_START)).toEqual([
			exportAsPdfLabelOf(ENGAGEMENT_START),
			`Edit ${ENGAGEMENT_START}`,
			`Delete ${ENGAGEMENT_START}`,
		]);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it(`gives a reader an Actions column holding only the PDF icon`, async () => {
		onTheList(aReportService(), "reader");

		expect(await theActionsOf(ENGAGEMENT_START)).toEqual([
			exportAsPdfLabelOf(ENGAGEMENT_START),
		]);
		expect(screen.getByRole("columnheader", { name: "Actions" })).toBeVisible();
	});

	// @us-01 @slice-01 @contract-shape:bounded-change
	it(`names the icon "Export PDF" when the pointer rests on it`, async () => {
		const { user } = onTheList();

		await user.hover(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);

		expect(readingOf(await screen.findByRole("tooltip"))).toBe(EXPORT_PDF);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it(`offers no PDF on a row this version cannot show, and keeps it on the other rows`, async () => {
		const later = aListedReportOfALaterType();
		onTheList(aListOf(aListedReport(), later));

		expect(await theActionsOf(ENGAGEMENT_START)).toContain(
			exportAsPdfLabelOf(ENGAGEMENT_START),
		);
		expect(await theActionsOf(later.name)).not.toContain(
			exportAsPdfLabelOf(later.name),
		);
		expect(
			within(await theRowOf(later.name)).queryByLabelText(
				exportAsPdfLabelOf(later.name),
			),
		).toBeNull();
	});

	// @us-01 @slice-01 @driving_port @kpi-OUT-6298-K1-pdf-export-used @kpi-OUT-6298-K6-exported-report-types @contract-shape:bounded-change
	it(`counts one export with the report's Type and takes the reader to its print page, saying it came from the list`, async () => {
		const { user } = onTheList(aReportService(), "reader");

		await user.click(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);

		await theStandInForThePrintPage();
		expect(whereTheReaderIs()).toEqual({
			path: printPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
			search: "",
		});
		expect(whatThisEntryCarries()).toEqual({
			printWhenReady: true,
			from: "list",
		});
		expect(exportsReported()).toEqual([
			[{ name: PDF_EXPORT_STARTED, reportTemplate: THEN_AND_NOW }],
		]);
	});
});
