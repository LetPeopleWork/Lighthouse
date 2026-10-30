import { screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	aMomentLater,
	aPrintDialog,
	aPrintFrameBrowser,
	EXPORT_PDF,
	everyPrintFrameIsGone,
	exportAsPdfLabelOf,
	forgetTheBrowser,
	PDF_EXPORT_STARTED,
	renderTheExportScene,
	reportPathFor,
	reportsPathFor,
	THE_REPORT_COULD_NOT_BE_LOADED,
	THEN_AND_NOW,
	theBrowserShowsKeyboardFocus,
	theErrorSnackbar,
	theFontsAreStillLoading,
	theFontsHaveLoaded,
	theFramesHavePrinted,
	thePrintFrame,
	thePrintFrames,
	theReaderIsOn,
	theReadersHistory,
	theSnackbarsRetry,
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
	theEngagementStartReport,
} from "../../../tests/ReportsTestKit";
import { PDF_EXPORT_UNAVAILABLE_ON_MAC } from "./pdfExportCopy";

/**
 * Export PDF from the Reports list. A PDF icon leads every row's actions, for everyone who can read the
 * reports: an editor sees it before Edit and Delete, and a reader, who had no actions before, gets an Actions
 * column holding only it. A row this version cannot show offers no PDF, as it offers no Edit. Choosing it
 * counts the same export as the button on the report and prints the report from a hidden frame, while the
 * reader stays on the list. One export at a time: while it is prepared the row's icon is the waiting spinner
 * and every other row's PDF icon waits too, as every Edit does while one report is read for editing.
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

const Q2_BASELINE = "Q2 Baseline";

/** Two reports the list shows, each with a PDF icon of its own. */
const twoReports = () =>
	aListOf(aListedReport(), aListedReport({ id: 43, name: Q2_BASELINE }));

/** A row's PDF icon as it stands now, found afresh, since what it shows changes while a PDF is prepared. */
const thePdfIconOf = (name: string) =>
	screen.getByLabelText(exportAsPdfLabelOf(name));

/** A control that cannot be chosen now, whether it says so to the browser or to assistive technology. */
const isHeldBack = (control: HTMLElement) =>
	control.hasAttribute("disabled") ||
	control.getAttribute("aria-disabled") === "true";

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

	it(`leads an editor's row actions with the PDF icon, before Edit and Delete`, async () => {
		onTheList();

		expect(await theActionsOf(ENGAGEMENT_START)).toEqual([
			exportAsPdfLabelOf(ENGAGEMENT_START),
			`Edit ${ENGAGEMENT_START}`,
			`Delete ${ENGAGEMENT_START}`,
		]);
	});

	it(`gives a reader an Actions column holding only the PDF icon`, async () => {
		onTheList(aReportService(), "reader");

		expect(await theActionsOf(ENGAGEMENT_START)).toEqual([
			exportAsPdfLabelOf(ENGAGEMENT_START),
		]);
		expect(screen.getByRole("columnheader", { name: "Actions" })).toBeVisible();
	});

	it(`names the icon "Export PDF" when the pointer rests on it`, async () => {
		const { user } = onTheList();

		await user.hover(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);

		expect(readingOf(await screen.findByRole("tooltip"))).toBe(EXPORT_PDF);
	});

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

	it(`prints the row's report from a hidden frame, counting one export with its Type, and leaves the reader on the list with nothing added to their history`, async () => {
		const theAppsDialog = aPrintDialog();
		const frames = aPrintFrameBrowser();
		const { user } = onTheList(aReportService(), "reader");

		await user.click(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);

		await theFramesHavePrinted(frames);
		expect(frames.framesThatPrinted()).toEqual([thePrintFrame()]);
		expect(theAppsDialog.timesOpened()).toBe(0);
		expect(theReadersHistory()).toEqual({ path: atTheList, entries: 1 });
		expect(exportsReported()).toEqual([
			[{ name: PDF_EXPORT_STARTED, reportTemplate: THEN_AND_NOW }],
		]);
	});

	it(`turns the row's PDF icon into the waiting spinner and holds every other row's PDF icon back until the dialog is asked for, so one export runs at a time`, async () => {
		const frames = aPrintFrameBrowser();
		const fonts = theFontsAreStillLoading();
		const { user } = onTheList(twoReports(), "reader");

		await user.click(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);

		await waitFor(() =>
			expect(
				within(thePdfIconOf(ENGAGEMENT_START)).getByRole("progressbar"),
			).toBeVisible(),
		);
		expect(isHeldBack(thePdfIconOf(Q2_BASELINE))).toBe(true);
		await user.click(thePdfIconOf(Q2_BASELINE));
		await aMomentLater();
		expect(thePrintFrames()).toHaveLength(1);
		expect(exportsReported()).toHaveLength(1);

		await fonts.arrive();

		await theFramesHavePrinted(frames);
		await waitFor(() =>
			expect(
				within(thePdfIconOf(ENGAGEMENT_START)).queryByRole("progressbar"),
			).toBeNull(),
		);
		expect(isHeldBack(thePdfIconOf(Q2_BASELINE))).toBe(false);
	});
});

describe("The list's print frame goes when it is no longer wanted", () => {
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

	// A browser may never say its dialog closed; the next export takes that frame's place rather than adding one.
	it(`removes a frame whose dialog never said it closed when the next export starts, so there is only ever one frame`, async () => {
		const frames = aPrintFrameBrowser();
		const { user } = onTheList(twoReports(), "reader");

		await user.click(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);
		await theFramesHavePrinted(frames);
		await waitFor(() =>
			expect(isHeldBack(thePdfIconOf(Q2_BASELINE))).toBe(false),
		);
		await user.click(thePdfIconOf(Q2_BASELINE));
		await theFramesHavePrinted(frames, 2);

		const [first, second] = frames.framesThatPrinted();
		expect(first.isConnected).toBe(false);
		expect(thePrintFrames()).toEqual([second]);
	});

	// The reader went elsewhere; a dialog over the page they moved to would be a surprise.
	it(`removes the frame and prints nothing when the reader leaves the list while the PDF is prepared`, async () => {
		const frames = aPrintFrameBrowser();
		const fonts = theFontsAreStillLoading();
		const { user } = onTheList(aReportService(), "reader");

		await user.click(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);
		await waitFor(() => expect(thePrintFrames()).toHaveLength(1));
		await user.click(screen.getByRole("link", { name: ENGAGEMENT_START }));
		await waitFor(() =>
			expect(whereTheReaderIs().path).toBe(
				reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
			),
		);

		await everyPrintFrameIsGone();
		await fonts.arrive();
		await aMomentLater();
		expect(frames.timesAFramePrinted()).toBe(0);
	});
});

describe("When Export PDF from the list cannot make the PDF", () => {
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

	// The list has no report on screen to fall back on, so the snackbar is the only place the reader learns of it.
	it(`says in the snackbar that no PDF was made when the row's report could not be read, and Retry prints it from a fresh frame, counting one export and leaving the reader on the list`, async () => {
		const frames = aPrintFrameBrowser();
		const getReport = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(theEngagementStartReport());
		const { user } = onTheList(aReportService({ getReport }), "reader");

		await user.click(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);
		const snackbar = await theErrorSnackbar();
		expect(snackbar).toHaveTextContent(THE_REPORT_COULD_NOT_BE_LOADED);
		await everyPrintFrameIsGone();
		expect(frames.timesAFramePrinted()).toBe(0);

		await user.click(theSnackbarsRetry(snackbar) as HTMLElement);

		await theFramesHavePrinted(frames);
		expect(thePrintFrames()).toEqual(frames.framesThatPrinted());
		expect(exportsReported()).toHaveLength(1);
		expect(theReadersHistory()).toEqual({ path: atTheList, entries: 1 });
	});

	// The snackbar goes when Retry is chosen, and a keyboard reader would otherwise be left at the top of the page.
	it(`gives the keyboard focus back to the row's PDF icon when the reader chooses Retry`, async () => {
		aPrintFrameBrowser();
		const getReport = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(theEngagementStartReport());
		const { user } = onTheList(aReportService({ getReport }), "reader");

		await user.click(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);
		const snackbar = await theErrorSnackbar();
		await everyPrintFrameIsGone();

		await user.click(theSnackbarsRetry(snackbar) as HTMLElement);

		expect(thePdfIconOf(ENGAGEMENT_START)).toHaveFocus();
	});
});

/** Walks the keyboard through the page until the control has the focus, as a keyboard reader would. */
const tabTo = async (
	user: ReturnType<typeof onTheList>["user"],
	control: HTMLElement,
	presses = 20,
): Promise<void> => {
	if (document.activeElement === control) {
		return;
	}
	expect(presses, "the keyboard never reached the control").toBeGreaterThan(0);
	await user.tab();
	await tabTo(user, control, presses - 1);
};

describe("Export PDF on the macOS desktop app", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		localStorage.clear();
		theFontsHaveLoaded();
		aPrintDialog();
		theReaderIsOn("the macOS desktop app");
	});

	afterEach(() => {
		localStorage.clear();
		forgetTheBrowser();
	});

	it(`greys the row's PDF icon out for assistive technology yet keeps it focusable, and starts no export when it is clicked`, async () => {
		const frames = aPrintFrameBrowser();
		const { user } = onTheList();
		const icon = await screen.findByLabelText(
			exportAsPdfLabelOf(ENGAGEMENT_START),
		);

		expect(icon).toHaveAttribute("aria-disabled", "true");
		expect(icon).not.toBeDisabled();
		await user.click(icon);
		await aMomentLater();

		expect(frames.timesAFramePrinted()).toBe(0);
		expect(thePrintFrames()).toHaveLength(0);
		expect(exportsReported()).toHaveLength(0);
	});

	it(`explains why on hover and describes the icon with it, keeping the icon's name`, async () => {
		const { user } = onTheList();
		const icon = await screen.findByLabelText(
			exportAsPdfLabelOf(ENGAGEMENT_START),
		);

		await user.hover(icon);

		const tooltip = await screen.findByRole("tooltip");
		expect(readingOf(tooltip)).toBe(PDF_EXPORT_UNAVAILABLE_ON_MAC);
		expect(icon).toHaveAccessibleDescription(PDF_EXPORT_UNAVAILABLE_ON_MAC);
		expect(icon).toHaveAccessibleName(exportAsPdfLabelOf(ENGAGEMENT_START));
	});

	it(`explains why when the icon gets the keyboard focus`, async () => {
		const { user } = onTheList();
		const icon = await screen.findByLabelText(
			exportAsPdfLabelOf(ENGAGEMENT_START),
		);

		theBrowserShowsKeyboardFocus();
		await tabTo(user, icon);

		await waitFor(() =>
			expect(screen.getAllByRole("tooltip").map(readingOf)).toContain(
				PDF_EXPORT_UNAVAILABLE_ON_MAC,
			),
		);
	});
});

describe("Export PDF in the Reports list on every other host", () => {
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

	it.each([
		"a browser on a Mac",
		"the Linux desktop app",
		"the Windows desktop app",
	] as const)(
		`keeps the row's PDF icon enabled and starts the export on %s`,
		async (host) => {
			theReaderIsOn(host);
			const frames = aPrintFrameBrowser();
			const { user } = onTheList();
			const icon = await screen.findByLabelText(
				exportAsPdfLabelOf(ENGAGEMENT_START),
			);

			expect(icon).not.toHaveAttribute("aria-disabled");
			await user.click(icon);

			await theFramesHavePrinted(frames);
			expect(exportsReported()).toHaveLength(1);
		},
	);
});
