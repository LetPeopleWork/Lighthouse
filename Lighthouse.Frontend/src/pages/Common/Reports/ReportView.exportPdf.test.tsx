import { screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	aPrintDialog,
	EXPORT_PDF,
	forgetTheBrowser,
	LEAVE_THE_PRINT_PAGE,
	PDF_EXPORT_STARTED,
	printPathFor,
	renderTheExportScene,
	reportPathFor,
	THEN_AND_NOW,
	theExportButtonOnceItCanBePressed,
	theFontsHaveLoaded,
	theStandInForThePrintPage,
	whatThisEntryCarries,
	whereTheReaderIs,
} from "../../../tests/ReportPrintTestKit";
import {
	A_LATER_TYPE,
	APOLLO,
	aRefusal,
	aReportService,
	COULD_NOT_LOAD,
	ENGAGEMENT_START,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	MADE_BY_A_NEWER_VERSION,
	NO_LONGER_EXISTS,
	readingOf,
	theEngagementStartReport,
} from "../../../tests/ReportsTestKit";

/**
 * Export PDF on the report view. Anyone who can read a report sees it as the first outlined button of the
 * header's action row, before Edit report and Delete; a reader who may not change the report sees it alone.
 * While the report is on its way it is shown and cannot be pressed; a report that could not be loaded, no
 * longer exists or was made by a newer version offers no PDF at all. Choosing it counts one export, with the
 * report's Type, and takes the reader to the report's print page; the address names the page and nothing
 * more.
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

const pendingEpic6298 =
	"Pending: Export PDF on the report view (epic 6298, slice 01)";

const OUTLINED = "MuiButton-outlined";

const atTheReport = reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID);

const leavingTheReportForItsPrintPage = (
	reportService = aReportService(),
	viewer: "admin" | "reader" = "admin",
	startAt = atTheReport,
) =>
	renderTheExportScene({
		reportService,
		viewer,
		startAt,
		withPrintPages: false,
	});

const theExportButton = () => screen.findByRole("button", { name: EXPORT_PDF });

/** The buttons of the header's action row, in the order a reader meets them. */
const theHeaderActions = async (): Promise<string[]> => {
	const row = (await theExportButton()).parentElement;
	expect(row, "Export PDF sits in no row").not.toBeNull();
	return Array.from(
		(row as HTMLElement).querySelectorAll<HTMLElement>("button"),
	).map(readingOf);
};

const exportsReported = () =>
	reportUsage.mock.calls.filter(([use]) => use.name === PDF_EXPORT_STARTED);

describe("Export PDF on the report view", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it(`offers an editor Export PDF as the first outlined button, before Edit report and Delete`, async () => {
		leavingTheReportForItsPrintPage();

		expect(await theHeaderActions()).toEqual([
			EXPORT_PDF,
			"Edit report",
			"Delete",
		]);
		expect(await theExportButton()).toHaveClass(OUTLINED);
		expect(await theExportButton()).toBeEnabled();
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	// Exporting is reading what is already on screen, so anyone who can read the report may do it.
	it(`offers a reader who may not change the report Export PDF alone`, async () => {
		leavingTheReportForItsPrintPage(aReportService(), "reader");

		expect(await theHeaderActions()).toEqual([EXPORT_PDF]);
		expect(await theExportButton()).toBeEnabled();
	});

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	// A PDF of a report still on its way would be a PDF of nothing; the button shows where it will be.
	it(`shows Export PDF but lets nobody press it while the report is on its way`, async () => {
		const held = deferred<never>();
		leavingTheReportForItsPrintPage(
			aReportService({ getReport: vi.fn().mockReturnValue(held.promise) }),
		);

		expect(await theExportButton()).toBeDisabled();
		expect(whereTheReaderIs().path).toBe(atTheReport);
		expect(exportsReported()).toHaveLength(0);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// Holds today and must keep holding once Export PDF is offered on reports that load.
	it.each([
		["could not be loaded", new Error("the network dropped"), COULD_NOT_LOAD],
		["no longer exists", aRefusal(404, "not-found"), NO_LONGER_EXISTS],
		[
			"was made by a newer version",
			aRefusal(422, "template-not-supported"),
			MADE_BY_A_NEWER_VERSION,
		],
	])(
		"offers no Export PDF for a report that %s",
		async (_why, refusal, sentence) => {
			leavingTheReportForItsPrintPage(
				aReportService({ getReport: vi.fn().mockRejectedValue(refusal) }),
			);

			expect(await screen.findByText(sentence)).toBeVisible();
			expect(screen.queryByRole("button", { name: EXPORT_PDF })).toBeNull();
			expect(exportsReported()).toHaveLength(0);
		},
	);

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	// The wish to print travels with the page change, never in the address: a pasted link never prints.
	it("takes the reader to the report's print page, asking it to print and saying it came from the report, with nothing in the address but the page", async () => {
		const { user } = leavingTheReportForItsPrintPage();

		await user.click(await theExportButtonOnceItCanBePressed());

		await theStandInForThePrintPage();
		expect(whereTheReaderIs()).toEqual({
			path: printPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
			search: "",
		});
		expect(whatThisEntryCarries()).toEqual({
			printWhenReady: true,
			from: "report",
		});
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it(`takes the reader of a Portfolio's report to that Portfolio's print page`, async () => {
		const { user } = leavingTheReportForItsPrintPage(
			aReportService({
				getReport: vi.fn().mockResolvedValue(
					theEngagementStartReport({
						ownerKind: "portfolio",
						ownerId: APOLLO.id,
					}),
				),
			}),
			"reader",
			reportPathFor(APOLLO, ENGAGEMENT_START_ID),
		);

		await user.click(await theExportButtonOnceItCanBePressed());

		await theStandInForThePrintPage();
		expect(whereTheReaderIs()).toEqual({
			path: printPathFor(APOLLO, ENGAGEMENT_START_ID),
			search: "",
		});
	});
});

describe("What choosing Export PDF reports", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-01 @slice-01 @driving_port @kpi-OUT-6298-K1-pdf-export-used @kpi-OUT-6298-K6-exported-report-types @contract-shape:bounded-change
	// It counts the choice, not a saved file: nothing tells Lighthouse whether the dialog saved one.
	it.skip(`reports one PDF export with the report's Type for every time Export PDF is chosen (${pendingEpic6298})`, async () => {
		const { user } = leavingTheReportForItsPrintPage();

		await user.click(await theExportButtonOnceItCanBePressed());
		await theStandInForThePrintPage();
		expect(exportsReported()).toEqual([
			[{ name: PDF_EXPORT_STARTED, reportTemplate: THEN_AND_NOW }],
		]);

		await user.click(
			await screen.findByRole("button", { name: LEAVE_THE_PRINT_PAGE }),
		);
		await user.click(await theExportButtonOnceItCanBePressed());
		await theStandInForThePrintPage();

		expect(exportsReported()).toEqual([
			[{ name: PDF_EXPORT_STARTED, reportTemplate: THEN_AND_NOW }],
			[{ name: PDF_EXPORT_STARTED, reportTemplate: THEN_AND_NOW }],
		]);
	});

	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:unbounded-preservation
	// A report this version cannot show has nothing to print, and its Type has no word on usage data's own list.
	// Holds today and must keep holding once Export PDF is offered on the reports it can show.
	it("offers no Export PDF for a report of a Type this version cannot show, and reports nothing", async () => {
		leavingTheReportForItsPrintPage(
			aReportService({
				getReport: vi
					.fn()
					.mockResolvedValue(
						theEngagementStartReport({ templateKey: A_LATER_TYPE }),
					),
			}),
		);
		await waitFor(() =>
			expect(screen.getByText(ENGAGEMENT_START)).toBeVisible(),
		);

		expect(screen.queryByRole("button", { name: EXPORT_PDF })).toBeNull();
		expect(exportsReported()).toHaveLength(0);
		expect(
			reportUsage.mock.calls.filter(
				([use]) =>
					use.reportTemplate !== undefined && use.name !== "ReportOpened",
			),
		).toEqual([]);
	});
});
