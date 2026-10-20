import { act, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	aMomentLater,
	aPrintDialog,
	aPrintFrameBrowser,
	BRANDING,
	EXPORT_PDF,
	everyPrintFrameIsGone,
	forgetTheBrowser,
	PDF_EXPORT_STARTED,
	renderTheExportScene,
	reportPathFor,
	THEN_AND_NOW,
	theErrorSnackbarIsShown,
	theExportButtonOnceItCanBePressed,
	theFontsAreStillLoading,
	theFontsHaveLoaded,
	theFramesHavePrinted,
	theFramesPage,
	thePrintFrame,
	thePrintFrames,
	theReadersHistory,
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
 * report's Type, and prints the report from a hidden frame on this very page: the reader never leaves the
 * report, and nothing is added to their history. While the PDF is prepared the button shows the waiting
 * spinner; once the print dialog has been asked for, it is Export PDF again.
 */

const pendingFramePrint = "Pending: Export PDF prints from a frame (epic 6298)";

const { reportUsage } = vi.hoisted(() => ({
	reportUsage: vi.fn().mockReturnValue(true),
}));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

const OUTLINED = "MuiButton-outlined";
const WHITE = "rgb(255, 255, 255)";
/** A style sheet the app is built with, and a rule of the app's own that no Emotion cache wrote. */
const THE_APPS_STYLE_SHEET = "/assets/index-3f9a1c.css";
const THE_APPS_OWN_RULES = "body { margin: 0; }";

const atTheReport = reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID);

type Scene = Parameters<typeof renderTheExportScene>[0];

const onTheReport = (scene: Partial<Scene> = {}) =>
	renderTheExportScene({
		reportService: aReportService(),
		viewer: "admin",
		startAt: atTheReport,
		...scene,
	});

/** The reader on the report, with no print pages in the scene. */
const onTheReportAlone = (
	reportService = aReportService(),
	viewer: "admin" | "reader" = "admin",
) => onTheReport({ reportService, viewer, withPrintPages: false });

const theExportButton = () => screen.findByRole("button", { name: EXPORT_PDF });

/** Export PDF as it stands now, found afresh, since what it shows changes while the PDF is prepared. */
const exportPdfNow = () => screen.getByRole("button", { name: EXPORT_PDF });

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

const ONE_EXPORT = [{ name: PDF_EXPORT_STARTED, reportTemplate: THEN_AND_NOW }];

/** The app's own style sheets, as the built app carries them in its page's head. */
const theAppCarriesItsStyleSheets = () => {
	const sheet = document.createElement("link");
	sheet.rel = "stylesheet";
	sheet.href = THE_APPS_STYLE_SHEET;
	const ownRules = document.createElement("style");
	ownRules.textContent = THE_APPS_OWN_RULES;
	document.head.append(sheet, ownRules);
	return () => {
		sheet.remove();
		ownRules.remove();
	};
};

describe("Export PDF on the report view", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it(`offers an editor Export PDF as the first outlined button, before Edit report and Delete`, async () => {
		onTheReportAlone();

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
		onTheReportAlone(aReportService(), "reader");

		expect(await theHeaderActions()).toEqual([EXPORT_PDF]);
		expect(await theExportButton()).toBeEnabled();
	});

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	// A PDF of a report still on its way would be a PDF of nothing; the button shows where it will be.
	it(`shows Export PDF but lets nobody press it while the report is on its way`, async () => {
		const held = deferred<never>();
		onTheReportAlone(
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
			onTheReportAlone(
				aReportService({ getReport: vi.fn().mockRejectedValue(refusal) }),
			);

			expect(await screen.findByText(sentence)).toBeVisible();
			expect(screen.queryByRole("button", { name: EXPORT_PDF })).toBeNull();
			expect(exportsReported()).toHaveLength(0);
		},
	);

	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:unbounded-preservation
	// A report this version cannot show has nothing to print, and its Type has no word on usage data's own list.
	// Holds today and must keep holding once Export PDF is offered on the reports it can show.
	it("offers no Export PDF for a report of a Type this version cannot show, and reports nothing", async () => {
		onTheReportAlone(
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

describe("Export PDF prints the report without leaving it", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
	});

	afterEach(() => {
		vi.useRealTimers();
		forgetTheBrowser();
		document.title = "";
	});

	// @us-01 @rev-3 @walking_skeleton @driving_port @kpi-OUT-6298-K1-pdf-export-used @kpi-OUT-6298-K6-exported-report-types @contract-shape:bounded-change
	// The print dialog of the frame's own window shows only the report; the app's window is never asked.
	it(`asks the report's own print frame for the dialog once everything is in, counts one export, and leaves the reader on the report with nothing added to their history`, async () => {
		const theAppsDialog = aPrintDialog();
		const frames = aPrintFrameBrowser();
		const { user } = onTheReport();
		const exportPdf = await theExportButtonOnceItCanBePressed();
		const before = theReadersHistory();

		await user.click(exportPdf);

		await theFramesHavePrinted(frames);
		expect(frames.framesThatPrinted()).toEqual([thePrintFrame()]);
		expect(theAppsDialog.timesOpened()).toBe(0);
		expect(theReadersHistory()).toEqual(before);
		expect(before).toEqual({ path: atTheReport, entries: 1 });
		expect(exportsReported()).toEqual([ONE_EXPORT]);
	});

	// @us-01 @rev-3 @driving_port @contract-shape:bounded-change
	// The control keeps the focus the reader gave it, so a keyboard reader is not thrown back to the page's top.
	it(`shows the waiting spinner on Export PDF while the PDF is prepared, keeps it focused and lets a second choice do nothing, until the dialog is asked for`, async () => {
		aPrintDialog();
		const frames = aPrintFrameBrowser();
		const fonts = theFontsAreStillLoading();
		const { user } = onTheReport();

		await user.click(await theExportButtonOnceItCanBePressed());

		await waitFor(() =>
			expect(within(exportPdfNow()).getByRole("progressbar")).toBeVisible(),
		);
		expect(exportPdfNow()).toHaveAttribute("aria-disabled", "true");
		expect(exportPdfNow()).toHaveAttribute("aria-busy", "true");
		expect(exportPdfNow()).toHaveFocus();
		await user.click(exportPdfNow());
		await aMomentLater();
		expect(thePrintFrames()).toHaveLength(1);
		expect(exportsReported()).toHaveLength(1);
		expect(frames.timesAFramePrinted()).toBe(0);

		await fonts.arrive();

		await theFramesHavePrinted(frames);
		await waitFor(() =>
			expect(within(exportPdfNow()).queryByRole("progressbar")).toBeNull(),
		);
		expect(exportPdfNow()).not.toHaveAttribute("aria-disabled", "true");
		expect(exportPdfNow()).not.toHaveAttribute("aria-busy", "true");
	});

	// @us-01 @rev-3 @driving_port @contract-shape:bounded-change
	// Paper is white whatever the reader's screen is, and the frame holds the app's typeface but not its dark ground.
	it(`draws a Portfolio's report inside the frame, on a light page of its own carrying the app's style sheets, and none of it on the report view`, async () => {
		aPrintDialog();
		const frames = aPrintFrameBrowser();
		const takeTheSheetsAway = theAppCarriesItsStyleSheets();
		try {
			const { user } = onTheReport({
				startAt: reportPathFor(APOLLO, ENGAGEMENT_START_ID),
				readersTheme: "dark",
				reportService: aReportService({
					getReport: vi.fn().mockResolvedValue(
						theEngagementStartReport({
							ownerKind: "portfolio",
							ownerId: APOLLO.id,
						}),
					),
				}),
			});

			await user.click(await theExportButtonOnceItCanBePressed());
			await theFramesHavePrinted(frames);

			const page = theFramesPage();
			expect(
				within(page).getByRole("heading", { level: 1, name: ENGAGEMENT_START }),
			).toBeVisible();
			expect(within(page).getByText("Portfolio Apollo")).toBeVisible();
			expect(within(page).getByText(BRANDING)).toBeVisible();
			expect(screen.queryByText(BRANDING)).toBeNull();

			const frame = thePrintFrame();
			const head = frame.contentDocument?.head as HTMLElement;
			expect(
				Array.from(
					head.querySelectorAll<HTMLLinkElement>('link[rel="stylesheet"]'),
				).map((sheet) => sheet.getAttribute("href")),
			).toContain(THE_APPS_STYLE_SHEET);
			expect(
				Array.from(head.querySelectorAll("style")).map(
					(style) => style.textContent,
				),
			).toContain(THE_APPS_OWN_RULES);
			const paper = page.querySelector<HTMLElement>("[data-render-ready]");
			expect(paper, "the frame holds no print page").not.toBeNull();
			expect(
				frame.contentWindow?.getComputedStyle(paper as HTMLElement)
					.backgroundColor,
			).toBe(WHITE);
		} finally {
			takeTheSheetsAway();
		}
	});

	// @us-01 @rev-3 @contract-shape:bounded-change
	// Browsers differ on whose title they offer as the PDF's file name, so the frame's and the tab's both carry it.
	it(`names the PDF after the report and its owner while the frame is there, and gives the tab its own name back once the frame has gone`, async () => {
		document.title = "Lighthouse";
		aPrintDialog();
		const frames = aPrintFrameBrowser();
		const { user } = onTheReport();

		await user.click(await theExportButtonOnceItCanBePressed());
		await theFramesHavePrinted(frames);

		const named = `${ENGAGEMENT_START} — Team Lightspeed`;
		expect(thePrintFrame().contentDocument?.title).toBe(named);
		expect(document.title).toBe(named);

		await frames.closeTheDialog();
		await everyPrintFrameIsGone();
		expect(document.title).toBe("Lighthouse");
	});

	// @us-01 @rev-3 @driving_port @kpi-OUT-6298-G-pdf-matches-screen @contract-shape:bounded-change
	// The screen and the PDF agree: the report behind the dialog shows the answer that was just printed.
	it(`reads the report afresh for the PDF, and the report view shows the very answer the PDF printed`, async () => {
		aPrintDialog();
		const frames = aPrintFrameBrowser();
		const renamed = "Then & Now — engagement start, as read for the PDF";
		const getReport = vi
			.fn()
			.mockResolvedValueOnce(theEngagementStartReport())
			.mockResolvedValue(theEngagementStartReport({ name: renamed }));
		const { user } = onTheReport({
			reportService: aReportService({ getReport }),
		});

		await user.click(await theExportButtonOnceItCanBePressed());
		await theFramesHavePrinted(frames);

		expect(
			within(theFramesPage()).getByRole("heading", { level: 1, name: renamed }),
		).toBeVisible();
		expect(screen.getByText(renamed)).toBeVisible();
		expect(screen.queryByText(ENGAGEMENT_START)).toBeNull();
	});

	// @us-01 @rev-3 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:unbounded-preservation
	// One opening is one stay on the report; an export moves the reader nowhere, so it neither adds one nor loses one.
	it(`counts the report as opened once, export or not`, async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		aPrintDialog();
		const frames = aPrintFrameBrowser();
		const timesTheReportWasOpened = () =>
			reportUsage.mock.calls.filter(([use]) => use.name === "ReportOpened")
				.length;
		const stayingFor = (milliseconds: number) =>
			act(async () => {
				await vi.advanceTimersByTimeAsync(milliseconds);
			});
		const { user } = onTheReport({ onFakeTimers: true });

		await user.click(await theExportButtonOnceItCanBePressed());
		await theFramesHavePrinted(frames);
		await frames.closeTheDialog();
		await stayingFor(5_000);
		expect(timesTheReportWasOpened()).toBe(1);

		await user.click(await theExportButtonOnceItCanBePressed());
		await theFramesHavePrinted(frames, 2);
		await frames.closeTheDialog();
		await stayingFor(10_000);

		expect(timesTheReportWasOpened()).toBe(1);
		expect(theReadersHistory().entries).toBe(1);
	});

	// @us-01 @rev-3 @edge @contract-shape:bounded-change
	// React's strict mode runs every effect twice on mount; the reader still gets one frame and one dialog.
	it(`asks for one dialog from one frame even when React runs every effect twice`, async () => {
		aPrintDialog();
		const frames = aPrintFrameBrowser();
		const { user } = onTheReport({ strict: true });

		await user.click(await theExportButtonOnceItCanBePressed());
		await theFramesHavePrinted(frames);
		await aMomentLater();

		expect(frames.timesAFramePrinted()).toBe(1);
		expect(thePrintFrames()).toHaveLength(1);
	});
});

describe("The print frame goes once the dialog is done with it", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-01 @rev-3 @driving_port @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
	// It counts the choice, not a saved file: nothing tells Lighthouse whether the dialog saved one.
	it.skip(`removes the frame a moment after the dialog closes, and counts one export with the report's Type every time Export PDF is chosen (${pendingFramePrint})`, async () => {
		const frames = aPrintFrameBrowser();
		const { user } = onTheReport();

		await user.click(await theExportButtonOnceItCanBePressed());
		await theFramesHavePrinted(frames);
		await frames.closeTheDialog();
		await everyPrintFrameIsGone();
		await user.click(await theExportButtonOnceItCanBePressed());
		await theFramesHavePrinted(frames, 2);

		expect(exportsReported()).toEqual([ONE_EXPORT, ONE_EXPORT]);
		expect(theReadersHistory()).toEqual({ path: atTheReport, entries: 1 });
	});

	// @us-01 @rev-3 @edge @contract-shape:bounded-change
	// Chrome says the dialog closed before print() returns; taking the frame away then would pull the page from
	// under its own dialog.
	it.skip(`keeps the frame until print() has returned when the browser says the dialog closed before that, then removes it (${pendingFramePrint})`, async () => {
		const frames = aPrintFrameBrowser({
			dialog: "closes before print returns",
		});
		const { user } = onTheReport();

		await user.click(await theExportButtonOnceItCanBePressed());
		await theFramesHavePrinted(frames);
		await everyPrintFrameIsGone();

		expect(frames.stillThereWhenPrintReturned()).toEqual([true]);
		expect(frames.timesAFramePrinted()).toBe(1);
		expect(theErrorSnackbarIsShown()).toBe(false);
		expect(await theExportButtonOnceItCanBePressed()).not.toHaveAttribute(
			"aria-busy",
			"true",
		);
	});
});
