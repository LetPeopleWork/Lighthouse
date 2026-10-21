import { act, screen, waitFor, within } from "@testing-library/react";
import { type ComponentProps, createElement } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	aPrintDialog,
	aPrintFrameBrowser,
	aTeamServiceThatCannotReadTheName,
	aTerminologyServiceThatCannotReadTheTerms,
	EXPORT_PDF,
	everyPrintFrameIsGone,
	type FramePrintDialog,
	forgetTheBrowser,
	PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
	PDF_EXPORT_STARTED,
	renderTheExportScene,
	reportPathFor,
	THE_REPORT_COULD_NOT_BE_LOADED,
	theErrorSnackbar,
	theErrorSnackbarIsShown,
	theExportButtonOnceItCanBePressed,
	theFontsAreStillLoading,
	theFontsCannotLoad,
	theFontsHaveLoaded,
	theFramesHavePrinted,
	theLighthouseMarkCannotLoad,
	thePrintFrames,
	theSnackbarsRetry,
} from "../../../tests/ReportPrintTestKit";
import {
	aRefusal,
	aReportService,
	ENGAGEMENT_START,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	MADE_BY_A_NEWER_VERSION,
	NO_LONGER_EXISTS,
	theEngagementStartReport,
} from "../../../tests/ReportsTestKit";

/**
 * When Export PDF cannot make the PDF. Nothing is printed and the frame goes; the app's error snackbar says so
 * where the reader clicked. A failure that asking again may mend says which part could not be loaded and offers
 * Retry, which prints from a fresh frame without counting a second export; a report that no longer exists, or was
 * made by a newer version, gets the report view's own sentence and no Retry. A PDF still not ready after
 * 30 seconds is a failure too, so the spinner never turns forever.
 */

const { reportUsage, theDocument } = vi.hoisted(() => ({
	reportUsage: vi.fn().mockReturnValue(true),
	theDocument: { breaks: false },
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

// A report that breaks while it is drawn for paper, such as a value no panel expected.
vi.mock("./print/ReportPrintDocument", async (importOriginal) => {
	const actual =
		await importOriginal<typeof import("./print/ReportPrintDocument")>();
	const Drawn = (props: ComponentProps<typeof actual.default>) => {
		if (theDocument.breaks) {
			throw new Error("the report could not be drawn for paper");
		}
		return createElement(actual.default, props);
	};
	return { ...actual, default: Drawn };
});

const THIRTY_SECONDS = 30_000;

/** A style sheet the built app carries in its page's head, which the frame copies. */
const theAppCarriesAStyleSheet = () => {
	const sheet = document.createElement("link");
	sheet.rel = "stylesheet";
	sheet.href = "/assets/index-3f9a1c.css";
	sheet.dataset.carriedByTheApp = "true";
	document.head.append(sheet);
};

const forgetTheAppsStyleSheets = () => {
	for (const sheet of document.head.querySelectorAll(
		"[data-carried-by-the-app]",
	)) {
		sheet.remove();
	}
};

const atTheReport = reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID);

type Scene = Parameters<typeof renderTheExportScene>[0];

const exportingFromTheReport = async (scene: Partial<Scene> = {}) => {
	const rendered = renderTheExportScene({
		reportService: aReportService(),
		startAt: atTheReport,
		...scene,
	});
	await rendered.user.click(await theExportButtonOnceItCanBePressed());
	return rendered;
};

/** The report view's read succeeds; the PDF's fresh read gets what follows. */
const theFreshReadGets = (...answers: (() => Promise<unknown>)[]) => {
	const getReport = vi.fn().mockResolvedValueOnce(theEngagementStartReport());
	for (const answer of answers) {
		getReport.mockImplementationOnce(answer);
	}
	return getReport;
};

const exportsReported = () =>
	reportUsage.mock.calls.filter(([use]) => use.name === PDF_EXPORT_STARTED);

/** Export PDF is back to being a plain button: no spinner, nothing held back. */
const exportPdfIsIdle = async () => {
	const exportPdf = await screen.findByRole("button", { name: EXPORT_PDF });
	await waitFor(() =>
		expect(within(exportPdf).queryByRole("progressbar")).toBeNull(),
	);
	expect(exportPdf).not.toHaveAttribute("aria-busy", "true");
	expect(exportPdf).not.toHaveAttribute("aria-disabled", "true");
};

describe("When Export PDF cannot make the PDF", () => {
	let frames: ReturnType<typeof aPrintFrameBrowser>;

	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
		aPrintDialog();
		frames = aPrintFrameBrowser();
	});

	afterEach(() => {
		theDocument.breaks = false;
		forgetTheAppsStyleSheets();
		vi.useRealTimers();
		forgetTheBrowser();
	});

	// @us-01 @rev-3 @error @contract-shape:bounded-change
	// The report view shares the answer, so it shows its own could-not-load look as well; the snackbar says no PDF.
	it.each<[string, () => Promise<unknown>, string, boolean]>([
		[
			"could not be read",
			() => Promise.reject(new Error("the network dropped")),
			THE_REPORT_COULD_NOT_BE_LOADED,
			true,
		],
		[
			"no longer exists",
			() => Promise.reject(aRefusal(404, "not-found")),
			NO_LONGER_EXISTS,
			false,
		],
		[
			"was made by a newer version",
			() => Promise.reject(aRefusal(422, "template-not-supported")),
			MADE_BY_A_NEWER_VERSION,
			false,
		],
	])(
		`says in the snackbar that no PDF was made when the report %s, offers Retry only where asking again may help, and prints nothing`,
		async (_why, freshRead, sentence, retryOffered) => {
			await exportingFromTheReport({
				reportService: aReportService({
					getReport: theFreshReadGets(freshRead),
				}),
			});

			const snackbar = await theErrorSnackbar();
			expect(snackbar).toHaveTextContent(sentence);
			expect(theSnackbarsRetry(snackbar) !== null).toBe(retryOffered);
			await everyPrintFrameIsGone();
			expect(frames.timesAFramePrinted()).toBe(0);
		},
	);

	// @us-01 @rev-3 @error @contract-shape:bounded-change
	it.each<[string, () => Partial<Scene>]>([
		[
			"the owner's name",
			() => ({ teamService: aTeamServiceThatCannotReadTheName() }),
		],
		[
			"the instance's terms",
			() => ({
				terminologyService: aTerminologyServiceThatCannotReadTheTerms({
					untilTheRead: 1_000,
				}),
			}),
		],
		[
			"the fonts",
			() => {
				theFontsCannotLoad("are not found");
				return {};
			},
		],
		[
			"the Lighthouse mark",
			() => {
				theLighthouseMarkCannotLoad();
				return {};
			},
		],
		[
			"one of the app's style sheets",
			() => {
				forgetTheBrowser();
				theFontsHaveLoaded();
				frames = aPrintFrameBrowser({ styleSheets: "fail" });
				theAppCarriesAStyleSheet();
				return {};
			},
		],
	])(
		`says part of the page couldn't be loaded, with Retry, when %s cannot be loaded, prints nothing and gives Export PDF back`,
		async (_part, failing) => {
			await exportingFromTheReport(failing());

			const snackbar = await theErrorSnackbar();
			expect(snackbar).toHaveTextContent(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED);
			expect(theSnackbarsRetry(snackbar)).not.toBeNull();
			await everyPrintFrameIsGone();
			expect(frames.timesAFramePrinted()).toBe(0);
			await exportPdfIsIdle();
		},
	);

	// @us-01 @rev-3 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
	// The reader asked for a PDF once; Retry finishes that export, from a fresh frame, and counts nothing more.
	it(`prints once from a fresh frame when Retry reads the report the export could not read, counting no second export`, async () => {
		const { user } = await exportingFromTheReport({
			reportService: aReportService({
				getReport: theFreshReadGets(
					() => Promise.reject(new Error("the network dropped")),
					() => Promise.resolve(theEngagementStartReport()),
				),
			}),
		});
		const snackbar = await theErrorSnackbar();
		await everyPrintFrameIsGone();

		await user.click(theSnackbarsRetry(snackbar) as HTMLElement);

		await theFramesHavePrinted(frames);
		expect(thePrintFrames()).toEqual(frames.framesThatPrinted());
		expect(exportsReported()).toHaveLength(1);
		await waitFor(() => expect(theErrorSnackbarIsShown()).toBe(false));
	});

	// The snackbar goes when Retry is chosen, and a keyboard reader would otherwise be left at the top of the page.
	it(`gives the keyboard focus back to Export PDF when the reader chooses Retry`, async () => {
		const { user } = await exportingFromTheReport({
			terminologyService: aTerminologyServiceThatCannotReadTheTerms({
				untilTheRead: 3,
			}),
		});
		const snackbar = await theErrorSnackbar();
		await everyPrintFrameIsGone();

		await user.click(theSnackbarsRetry(snackbar) as HTMLElement);

		expect(screen.getByRole("button", { name: EXPORT_PDF })).toHaveFocus();
	});

	it(`takes the last failure's message away when the reader chooses Export PDF again`, async () => {
		const { user } = await exportingFromTheReport({
			terminologyService: aTerminologyServiceThatCannotReadTheTerms({
				untilTheRead: 3,
			}),
		});
		await theErrorSnackbar();
		await everyPrintFrameIsGone();
		await exportPdfIsIdle();

		await user.click(screen.getByRole("button", { name: EXPORT_PDF }));

		await theFramesHavePrinted(frames);
		await waitFor(() => expect(theErrorSnackbarIsShown()).toBe(false));
	});

	// @us-01 @rev-3 @error @contract-shape:bounded-change
	// The app reads its terms once and never again on its own, so only the export can ask for them a second time.
	it(`prints from a fresh frame when Retry asks again for the instance's terms the export could not read`, async () => {
		const { user } = await exportingFromTheReport({
			terminologyService: aTerminologyServiceThatCannotReadTheTerms({
				untilTheRead: 3,
			}),
		});
		const snackbar = await theErrorSnackbar();
		expect(snackbar).toHaveTextContent(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED);
		await everyPrintFrameIsGone();

		await user.click(theSnackbarsRetry(snackbar) as HTMLElement);

		await theFramesHavePrinted(frames);
		expect(exportsReported()).toHaveLength(1);
	});

	// @us-01 @rev-3 @error @contract-shape:bounded-change
	// The frame cannot be seen, so a read, a font or a picture that never arrives would otherwise leave the spinner
	// turning with no word; the sentence follows what was still missing.
	it.each<[string, () => Partial<Scene>, string]>([
		[
			"the report never arrives",
			() => ({
				reportService: aReportService({
					getReport: theFreshReadGets(() => deferred<never>().promise),
				}),
			}),
			THE_REPORT_COULD_NOT_BE_LOADED,
		],
		[
			"the fonts never arrive",
			() => {
				theFontsAreStillLoading();
				return {};
			},
			PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
		],
		[
			"one of the app's style sheets never loads into the frame",
			() => {
				forgetTheBrowser();
				theFontsHaveLoaded();
				frames = aPrintFrameBrowser({ styleSheets: "never settle" });
				theAppCarriesAStyleSheet();
				return {};
			},
			PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
		],
	])(
		`gives up after 30 seconds when %s, says so with Retry, prints nothing and gives Export PDF back`,
		async (_what, holding, sentence) => {
			vi.useFakeTimers({ shouldAdvanceTime: true });
			await exportingFromTheReport({ ...holding(), onFakeTimers: true });
			await waitFor(() => expect(thePrintFrames()).toHaveLength(1));

			await act(async () => {
				await vi.advanceTimersByTimeAsync(THIRTY_SECONDS - 1_000);
			});
			expect(theErrorSnackbarIsShown()).toBe(false);
			await act(async () => {
				await vi.advanceTimersByTimeAsync(1_000);
			});

			const snackbar = await theErrorSnackbar();
			expect(snackbar).toHaveTextContent(sentence);
			expect(theSnackbarsRetry(snackbar)).not.toBeNull();
			await everyPrintFrameIsGone();
			expect(frames.timesAFramePrinted()).toBe(0);
			await exportPdfIsIdle();
		},
	);

	// @us-01 @rev-3 @error @contract-shape:bounded-change
	// A failure inside the printed copy is the export's alone; the report the reader is looking at stays as it was.
	it(`says part of the page couldn't be loaded, with Retry, when the report breaks while it is drawn for paper, and leaves the report view as it was`, async () => {
		theDocument.breaks = true;
		await exportingFromTheReport();

		const snackbar = await theErrorSnackbar();
		expect(snackbar).toHaveTextContent(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED);
		expect(theSnackbarsRetry(snackbar)).not.toBeNull();
		await everyPrintFrameIsGone();
		expect(frames.timesAFramePrinted()).toBe(0);
		expect(
			screen.queryByText(/this section could not be displayed/i),
		).toBeNull();
		expect(screen.getByText(ENGAGEMENT_START)).toBeVisible();
	});

	// @us-01 @rev-3 @error @contract-shape:bounded-change
	it.each<FramePrintDialog>(["throws", "is missing"])(
		`says part of the page couldn't be loaded, with Retry, when the frame's print dialog %s, and gives Export PDF back`,
		async (dialog) => {
			forgetTheBrowser();
			theFontsHaveLoaded();
			frames = aPrintFrameBrowser({ dialog });
			await exportingFromTheReport();

			const snackbar = await theErrorSnackbar();
			expect(snackbar).toHaveTextContent(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED);
			expect(theSnackbarsRetry(snackbar)).not.toBeNull();
			await everyPrintFrameIsGone();
			await exportPdfIsIdle();
		},
	);

	// @us-01 @rev-3 @error @contract-shape:bounded-change
	// Retry restarts the export on the report the reader left; the message goes with the page it belongs to.
	it(`takes its message away when the reader leaves the report it offered Retry on`, async () => {
		const { user } = await exportingFromTheReport({
			teamService: aTeamServiceThatCannotReadTheName(),
		});
		await theErrorSnackbar();

		await user.click(screen.getByRole("link", { name: "Reports" }));

		await waitFor(() => expect(theErrorSnackbarIsShown()).toBe(false));
	});
});
