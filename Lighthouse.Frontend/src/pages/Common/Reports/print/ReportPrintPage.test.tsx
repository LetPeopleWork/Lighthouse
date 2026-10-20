import { hexToRgb } from "@mui/material/styles";
import { act, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { deferred } from "../../../../tests/HeldMetricsService";
import {
	aMomentLater,
	aPortfolioServiceAnswering,
	aPrintDialog,
	aQueryClient,
	aTeamServiceAnswering,
	aTeamServiceThatCannotReadTheName,
	aTerminologyServiceAnswering,
	aTerminologyServiceThatCannotReadTheTerms,
	BACK_TO_REPORT,
	BACK_TO_REPORTS,
	BRANDING,
	EXPORT_PDF,
	EXPORTED_AT,
	exportAsPdfLabelOf,
	forgetTheBrowser,
	isHiddenInPrint,
	lightspeed,
	PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
	PREPARING,
	PRODUCED_LINE,
	pressTheBrowsersBack,
	printPathFor,
	renderReadiness,
	renderTheExportScene,
	reportPathFor,
	reportsPathFor,
	THE_REPORT_COULD_NOT_BE_LOADED,
	theBackControl,
	theExportButtonOnceItCanBePressed,
	theFontsAreStillLoading,
	theFontsCannotLoad,
	theFontsHaveLoaded,
	thePrintPageIsReady,
	theTermsAnswer,
	WINDOWS_LINE,
	whatThisEntryCarries,
	whereTheReaderIs,
} from "../../../../tests/ReportPrintTestKit";
import {
	APOLLO,
	aRefusal,
	aReportService,
	defaultTerms,
	ENGAGEMENT_START,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	MADE_BY_A_NEWER_VERSION,
	NO_LONGER_EXISTS,
	readingOf,
	theEngagementStartReport,
} from "../../../../tests/ReportsTestKit";
import { lightTheme } from "../../../../utils/theme/appThemes";
import { reportQueryKeyOf } from "../reportOwner";

/**
 * The print page of a report: the page Export PDF leads to, drawn for paper and nothing else. It reads the
 * report afresh, says it is preparing until everything it prints is in - the report, the owner's name, the
 * instance's terms and the fonts - and only then, and only when the reader came by Export PDF, asks the
 * browser once for its print dialog. The wish to print is used up by printing, so nothing prints it twice. When
 * the dialog closes the reader is back where Export started. A report that cannot be read makes no PDF and says
 * so; a link pasted into the address bar shows the page, never prints, and leads back to the Reports list.
 */

const { reportUsage } = vi.hoisted(() => ({
	reportUsage: vi.fn().mockReturnValue(true),
}));

vi.mock(
	"../../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

const atTheReport = reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID);
const atThePrintPage = printPathFor(LIGHTSPEED, ENGAGEMENT_START_ID);
const atTheList = reportsPathFor(LIGHTSPEED);

const pendingReviewFix = "Pending: slice 01 review fix (epic 6298)";

const LIGHT_INK = hexToRgb(lightTheme.palette.text.primary);
const WHITE = "rgb(255, 255, 255)";

type Scene = Parameters<typeof renderTheExportScene>[0];

/** A print page opened straight from its address, the way a pasted link opens it. */
const openingThePrintPageDirectly = (scene: Partial<Scene> = {}) =>
	renderTheExportScene({
		reportService: aReportService(),
		startAt: atThePrintPage,
		...scene,
	});

/** The reader on the report view chooses Export PDF and arrives at its print page. */
const exportingFromTheReport = async (scene: Partial<Scene> = {}) => {
	const rendered = renderTheExportScene({
		reportService: aReportService(),
		startAt: atTheReport,
		...scene,
	});
	await rendered.user.click(await theExportButtonOnceItCanBePressed());
	return rendered;
};

/** The reader opens the report from the Reports list, so the list is the entry before the report in their history. */
const openingTheReportFromTheList = async () => {
	const rendered = renderTheExportScene({
		reportService: aReportService(),
		startAt: atTheList,
	});
	await rendered.user.click(
		await screen.findByRole("link", { name: ENGAGEMENT_START }),
	);
	return rendered;
};

/** The reader on the Reports list chooses the PDF icon of the engagement-start report. */
const exportingFromTheList = async (scene: Partial<Scene> = {}) => {
	const rendered = renderTheExportScene({
		reportService: aReportService(),
		startAt: atTheList,
		...scene,
	});
	await rendered.user.click(
		await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
	);
	return rendered;
};

const thePrintPage = (): HTMLElement => {
	const page = document.querySelector<HTMLElement>("[data-render-ready]");
	expect(page, "there is no print page").not.toBeNull();
	return page as HTMLElement;
};

/** The page's own words, read in the order a reader meets them. */
const thePrintedLines = (): string[] =>
	Array.from(
		thePrintPage().querySelectorAll<HTMLElement>(
			"h1, h2, h3, p, [data-print-line]",
		),
	)
		.map(readingOf)
		.filter((line) => line.length > 0);

const indexOfLine = (lines: string[], line: string) => {
	const at = lines.indexOf(line);
	expect(at, `"${line}" is not printed`).toBeGreaterThanOrEqual(0);
	return at;
};

const theStylesOfThePage = () =>
	Array.from(document.querySelectorAll("style"))
		.map((style) => style.textContent ?? "")
		.join("\n");

/** Each of the four things the page waits for, held back while the other three arrive. */
type Held = "the report" | "the owner's name" | "the terms" | "the fonts";

const withHeld = (held: Held): Partial<Scene> => {
	const never = deferred<never>().promise;
	switch (held) {
		case "the report":
			return {
				reportService: aReportService({
					getReport: vi.fn().mockReturnValue(never),
				}),
			};
		case "the owner's name":
			return { teamService: aTeamServiceAnswering(never) };
		case "the terms":
			return { terminologyService: aTerminologyServiceAnswering(never) };
		case "the fonts":
			theFontsAreStillLoading();
			return {};
	}
};

describe("The print page gets ready", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
	});

	afterEach(forgetTheBrowser);

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("says it is preparing the PDF, under a spinner, and is not ready, while the report is on its way", async () => {
		aPrintDialog();
		openingThePrintPageDirectly(withHeld("the report"));

		expect(await screen.findByText(PREPARING)).toBeVisible();
		expect(screen.getByRole("progressbar")).toBeVisible();
		expect(renderReadiness()).toBe("false");
	});

	// @us-01 @slice-01 @edge @contract-shape:unbounded-preservation
	it.each<Held>(["the report", "the owner's name", "the terms", "the fonts"])(
		"is still preparing, and not ready, while %s has not arrived",
		async (held) => {
			aPrintDialog();
			openingThePrintPageDirectly(withHeld(held));

			await aMomentLater();

			expect(screen.getByText(PREPARING)).toBeVisible();
			expect(renderReadiness()).toBe("false");
		},
	);

	// @us-01 @walking_skeleton @slice-01 @driving_port @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
	it("opens the print dialog exactly once, when everything it prints is in, after Export PDF", async () => {
		const dialog = aPrintDialog();

		await exportingFromTheReport();
		await thePrintPageIsReady();
		await aMomentLater();

		expect(dialog.timesOpened()).toBe(1);
		expect(screen.queryByText(PREPARING)).toBeNull();
		expect(theBackControl()).toBeDefined();
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	// React's strict mode runs every effect twice on mount; the reader still gets one dialog.
	it("opens the print dialog once even when the page's effects run twice", async () => {
		const dialog = aPrintDialog();

		await exportingFromTheReport({ strict: true });
		await thePrintPageIsReady();
		await aMomentLater();

		expect(dialog.timesOpened()).toBe(1);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// The wish to print travels with Export PDF, never in the address, so a pasted link only shows the page.
	it("shows a print page opened from its address but never opens the print dialog", async () => {
		const dialog = aPrintDialog();

		openingThePrintPageDirectly();
		await thePrintPageIsReady();
		await aMomentLater();

		expect(dialog.timesOpened()).toBe(0);
		expect(
			screen.getByRole("heading", { name: ENGAGEMENT_START }),
		).toBeVisible();
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	it("waits for the fonts before opening the print dialog", async () => {
		const dialog = aPrintDialog();
		const fonts = theFontsAreStillLoading();

		await exportingFromTheReport();
		await aMomentLater();
		expect(dialog.timesOpened()).toBe(0);
		expect(renderReadiness()).toBe("false");

		await fonts.arrive();

		await thePrintPageIsReady();
		await waitFor(() => expect(dialog.timesOpened()).toBe(1));
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	// Asked before the document is drawn, the browser would only fetch the faces it already needs for the spinner.
	it("asks for the Lighthouse typeface, its headings' weight included, only once the report is drawn", async () => {
		aPrintDialog();
		const fonts = theFontsHaveLoaded();

		openingThePrintPageDirectly();
		await thePrintPageIsReady();

		expect(fonts.fonts().length).toBeGreaterThan(0);
		for (const font of fonts.fonts()) {
			expect(font).toMatch(/Quicksand/);
		}
		expect(fonts.fonts().some((font) => /\b700\b/.test(font))).toBe(true);
		expect(fonts.askedBeforeTheReportWasDrawn()).toBe(false);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it.each<"are not found" | "fail outright">([
		"are not found",
		"fail outright",
	])(
		"says part of the page could not be loaded, offers Retry and never prints, when the fonts %s",
		async (how) => {
			const dialog = aPrintDialog();
			theFontsCannotLoad(how);

			await exportingFromTheReport();

			expect(
				await screen.findByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED),
			).toBeVisible();
			expect(screen.queryByText(THE_REPORT_COULD_NOT_BE_LOADED)).toBeNull();
			expect(screen.getByRole("button", { name: "Retry" })).toBeEnabled();
			await aMomentLater();
			expect(renderReadiness()).not.toBe("true");
			expect(dialog.timesOpened()).toBe(0);
		},
	);
});

describe("The wish to print is used up by printing", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
	});

	afterEach(forgetTheBrowser);

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("leaves the page's entry without the wish to print once it printed, still knowing where the export started", async () => {
		const dialog = aPrintDialog();

		await exportingFromTheReport();
		await thePrintPageIsReady();
		await waitFor(() => expect(dialog.timesOpened()).toBe(1));

		expect(whatThisEntryCarries()).toEqual({ from: "report" });
		expect(whereTheReaderIs()).toEqual({ path: atThePrintPage, search: "" });
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// A reload, or Forward back onto the page, finds the entry as printing left it.
	it("never prints again when the page comes back after it printed", async () => {
		const dialog = aPrintDialog();

		openingThePrintPageDirectly({ startState: { from: "report" } });
		await thePrintPageIsReady();
		await aMomentLater();

		expect(dialog.timesOpened()).toBe(0);
		expect(readingOf(theBackControl() as HTMLElement)).toBe(BACK_TO_REPORT);
	});
});

describe("The way back from the print page", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
	});

	afterEach(forgetTheBrowser);

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it('reads "‹ Back to report" when the reader came from the report, and is not printed', async () => {
		aPrintDialog();
		await exportingFromTheReport();
		await thePrintPageIsReady();

		const back = theBackControl() as HTMLElement;
		expect(readingOf(back)).toBe(BACK_TO_REPORT);
		expect(isHiddenInPrint(back)).toBe(true);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("takes the reader back to the report once the print dialog closes", async () => {
		const dialog = aPrintDialog();
		await exportingFromTheReport();
		await thePrintPageIsReady();

		dialog.close();

		await waitFor(() =>
			expect(whereTheReaderIs()).toEqual({ path: atTheReport, search: "" }),
		);
		expect(
			await screen.findByRole("button", { name: EXPORT_PDF }),
		).toBeEnabled();
		expect(dialog.timesOpened()).toBe(1);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it(`reads "‹ Back to Reports" when the reader came from the list, and goes back to it`, async () => {
		aPrintDialog();
		const { user } = await exportingFromTheList();
		await thePrintPageIsReady();

		const back = theBackControl() as HTMLElement;
		expect(readingOf(back)).toBe(BACK_TO_REPORTS);
		await user.click(back);

		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheList));
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it(`takes the reader from the list back to the list once the print dialog closes`, async () => {
		const dialog = aPrintDialog();
		await exportingFromTheList();
		await thePrintPageIsReady();

		dialog.close();

		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheList));
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	// A pasted link says nothing about where the reader came from; the owner's Reports list is the one safe place.
	it("leads a page opened from its address back to the Reports list", async () => {
		aPrintDialog();
		const { user } = openingThePrintPageDirectly();
		await thePrintPageIsReady();

		const back = theBackControl() as HTMLElement;
		expect(readingOf(back)).toBe(BACK_TO_REPORTS);
		await user.click(back);

		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheList));
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	// The return steps back to the entry the export started from; a copy of the report on top of it would make the
	// browser's Back show the same report a second time.
	it.skip(`takes the reader back to the report once the print dialog closes, leaving the report in their history once (${pendingReviewFix})`, async () => {
		const dialog = aPrintDialog();
		const { user } = await openingTheReportFromTheList();
		await user.click(await theExportButtonOnceItCanBePressed());
		await thePrintPageIsReady();
		await waitFor(() => expect(dialog.timesOpened()).toBe(1));

		dialog.close();
		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheReport));

		await pressTheBrowsersBack();

		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheList));
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it.skip(`takes the reader back to the report on "‹ Back to report", leaving the report in their history once (${pendingReviewFix})`, async () => {
		aPrintDialog();
		const { user } = await openingTheReportFromTheList();
		await user.click(await theExportButtonOnceItCanBePressed());
		await thePrintPageIsReady();

		await user.click(theBackControl() as HTMLElement);
		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheReport));

		await pressTheBrowsersBack();

		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheList));
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it.skip(`takes the reader from the list back to the list once the print dialog closes, leaving the list in their history once (${pendingReviewFix})`, async () => {
		const dialog = aPrintDialog();
		const { user } = renderTheExportScene({
			reportService: aReportService(),
			startAt: atTheReport,
		});
		await user.click(await screen.findByRole("link", { name: "Reports" }));
		await user.click(
			await screen.findByLabelText(exportAsPdfLabelOf(ENGAGEMENT_START)),
		);
		await thePrintPageIsReady();
		await waitFor(() => expect(dialog.timesOpened()).toBe(1));

		dialog.close();
		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheList));

		await pressTheBrowsersBack();

		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheReport));
	});
});

describe("A report the print page cannot read", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
	});

	afterEach(forgetTheBrowser);

	/** The report view's read succeeds; the print page's fresh read gets what follows. */
	const theFreshReadGets = (...answers: (() => Promise<unknown>)[]) => {
		const getReport = vi.fn().mockResolvedValueOnce(theEngagementStartReport());
		for (const answer of answers) {
			getReport.mockImplementationOnce(answer);
		}
		return getReport;
	};

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("says no PDF was made, offers Retry, and never gets ready or prints, when the report could not be loaded", async () => {
		const dialog = aPrintDialog();
		const getReport = theFreshReadGets(() =>
			Promise.reject(new Error("the network dropped")),
		);
		await exportingFromTheReport({
			reportService: aReportService({ getReport }),
		});

		expect(
			await screen.findByText(THE_REPORT_COULD_NOT_BE_LOADED),
		).toBeVisible();
		expect(screen.queryByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED)).toBeNull();
		expect(screen.getByRole("button", { name: "Retry" })).toBeEnabled();
		expect(readingOf(theBackControl() as HTMLElement)).toBe(BACK_TO_REPORT);
		await aMomentLater();
		expect(renderReadiness()).not.toBe("true");
		expect(dialog.timesOpened()).toBe(0);
	});

	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
	// The reader asked for a PDF; a read that succeeds on Retry finishes what Export started.
	it("opens the print dialog once when Retry reads the report after the export's read failed", async () => {
		const dialog = aPrintDialog();
		const getReport = theFreshReadGets(
			() => Promise.reject(new Error("the network dropped")),
			() => Promise.resolve(theEngagementStartReport()),
		);
		const { user } = await exportingFromTheReport({
			reportService: aReportService({ getReport }),
		});
		await user.click(await screen.findByRole("button", { name: "Retry" }));

		await thePrintPageIsReady();
		await aMomentLater();

		expect(dialog.timesOpened()).toBe(1);
		expect(screen.queryByText(THE_REPORT_COULD_NOT_BE_LOADED)).toBeNull();
	});

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it("reads the report again on Retry on a page opened from its address, shows it, and still never prints", async () => {
		const dialog = aPrintDialog();
		const getReport = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(theEngagementStartReport());
		const { user } = openingThePrintPageDirectly({
			reportService: aReportService({ getReport }),
		});
		await user.click(await screen.findByRole("button", { name: "Retry" }));

		await thePrintPageIsReady();
		await aMomentLater();
		expect(getReport).toHaveBeenCalledTimes(2);
		expect(screen.queryByText(THE_REPORT_COULD_NOT_BE_LOADED)).toBeNull();
		expect(dialog.timesOpened()).toBe(0);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// Nothing comes of trying again, so there is no Retry, and the only way on is the list.
	it.each([
		["no longer exists", aRefusal(404, "not-found"), NO_LONGER_EXISTS],
		[
			"was made by a newer version",
			aRefusal(422, "template-not-supported"),
			MADE_BY_A_NEWER_VERSION,
		],
	])(
		"says so, offers no Retry and leads back to Reports, for a report that %s",
		async (_why, refusal, sentence) => {
			const dialog = aPrintDialog();
			const { user } = openingThePrintPageDirectly({
				reportService: aReportService({
					getReport: vi.fn().mockRejectedValue(refusal),
				}),
			});

			expect(await screen.findByText(sentence)).toBeVisible();
			expect(screen.queryByRole("button", { name: "Retry" })).toBeNull();
			const back = theBackControl() as HTMLElement;
			expect(readingOf(back)).toBe(BACK_TO_REPORTS);
			await aMomentLater();
			expect(renderReadiness()).not.toBe("true");
			expect(dialog.timesOpened()).toBe(0);
			await user.click(back);
			await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheList));
		},
	);

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("leads back to Reports from a report that no longer exists, even when the reader came from the report", async () => {
		const dialog = aPrintDialog();
		const getReport = theFreshReadGets(() =>
			Promise.reject(aRefusal(404, "not-found")),
		);
		await exportingFromTheReport({
			reportService: aReportService({ getReport }),
		});

		expect(await screen.findByText(NO_LONGER_EXISTS)).toBeVisible();
		expect(readingOf(theBackControl() as HTMLElement)).toBe(BACK_TO_REPORTS);
		expect(dialog.timesOpened()).toBe(0);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("says part of the page could not be loaded, offers Retry, and never prints, when the owner's name cannot be read", async () => {
		const dialog = aPrintDialog();
		const teamService = aTeamServiceThatCannotReadTheName();
		const { user } = await exportingFromTheReport({ teamService });

		expect(
			await screen.findByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED),
		).toBeVisible();
		expect(screen.queryByText(THE_REPORT_COULD_NOT_BE_LOADED)).toBeNull();
		expect(screen.getByRole("button", { name: "Retry" })).toBeEnabled();
		await aMomentLater();
		expect(renderReadiness()).not.toBe("true");
		expect(dialog.timesOpened()).toBe(0);

		const readsBeforeRetry = vi.mocked(teamService.getTeam).mock.calls.length;
		await user.click(screen.getByRole("button", { name: "Retry" }));

		await waitFor(() =>
			expect(vi.mocked(teamService.getTeam).mock.calls.length).toBeGreaterThan(
				readsBeforeRetry,
			),
		);
		await aMomentLater();
		expect(dialog.timesOpened()).toBe(0);
	});

	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
	// The reader asked for a PDF; terms that arrive on Retry finish what Export started.
	it("says part of the page could not be loaded when the instance's terms cannot be read, and Retry reads them again and prints once", async () => {
		const dialog = aPrintDialog();
		const terminologyService = aTerminologyServiceThatCannotReadTheTerms({
			untilTheRead: 2,
		});
		const { user } = await exportingFromTheReport({ terminologyService });

		expect(
			await screen.findByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED),
		).toBeVisible();
		await aMomentLater();
		expect(dialog.timesOpened()).toBe(0);

		const readsBeforeRetry = vi.mocked(terminologyService.getAllTerminology)
			.mock.calls.length;
		await user.click(screen.getByRole("button", { name: "Retry" }));

		await thePrintPageIsReady();
		await waitFor(() => expect(dialog.timesOpened()).toBe(1));
		expect(
			vi.mocked(terminologyService.getAllTerminology).mock.calls.length,
		).toBeGreaterThan(readsBeforeRetry);
		expect(screen.queryByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED)).toBeNull();
	});

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	// The report view's answer stays in hand while Retry asks again, and so does the failure; the page must not go
	// on saying it failed, nor offer Retry a second time, while the answer to the first Retry is on its way.
	it(`shows that it is preparing the PDF, with no Retry, while Retry reads the report again`, async () => {
		const dialog = aPrintDialog();
		const theRetriedRead =
			deferred<ReturnType<typeof theEngagementStartReport>>();
		const getReport = theFreshReadGets(
			() => Promise.reject(new Error("the network dropped")),
			() => theRetriedRead.promise,
		);
		const { user } = await exportingFromTheReport({
			reportService: aReportService({ getReport }),
		});
		await user.click(await screen.findByRole("button", { name: "Retry" }));

		expect(await screen.findByText(PREPARING)).toBeVisible();
		expect(screen.getByRole("progressbar")).toBeVisible();
		expect(screen.queryByText(THE_REPORT_COULD_NOT_BE_LOADED)).toBeNull();
		expect(screen.queryByRole("button", { name: "Retry" })).toBeNull();
		expect(renderReadiness()).toBe("false");

		await act(async () => {
			theRetriedRead.resolve(theEngagementStartReport());
		});
		await thePrintPageIsReady();
		await waitFor(() => expect(dialog.timesOpened()).toBe(1));
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// A Team or Portfolio deleted since the report was opened answers with no one; waiting for its name would never end.
	it.each<[string, () => Promise<unknown>]>([
		[
			"Team",
			() =>
				exportingFromTheReport({
					teamService: aTeamServiceAnswering(Promise.resolve(null)),
				}),
		],
		[
			"Portfolio",
			async () =>
				openingThePrintPageDirectly({
					startAt: printPathFor(APOLLO, ENGAGEMENT_START_ID),
					reportService: aReportService({
						getReport: vi.fn().mockResolvedValue(
							theEngagementStartReport({
								ownerKind: "portfolio",
								ownerId: APOLLO.id,
							}),
						),
					}),
					portfolioService: aPortfolioServiceAnswering(Promise.resolve(null)),
				}),
		],
	])(
		`says part of the page could not be loaded, offers Retry and never prints, when the %s the report belongs to is gone`,
		async (_owner, opening) => {
			const dialog = aPrintDialog();
			await opening();

			expect(
				await screen.findByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED),
			).toBeVisible();
			expect(screen.queryByText(PREPARING)).toBeNull();
			expect(screen.getByRole("button", { name: "Retry" })).toBeEnabled();
			await aMomentLater();
			expect(renderReadiness()).not.toBe("true");
			expect(dialog.timesOpened()).toBe(0);
		},
	);

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// A refusal that asking again will not change, but which has no sentence of its own, still speaks of the report.
	it.skip(`says the report couldn't be loaded, offers no Retry and leads back to Reports, when the report is refused for a reason with no sentence of its own (${pendingReviewFix})`, async () => {
		const dialog = aPrintDialog();
		const getReport = theFreshReadGets(() =>
			Promise.reject(aRefusal(422, "then-too-short")),
		);
		await exportingFromTheReport({
			reportService: aReportService({ getReport }),
		});

		expect(
			await screen.findByText(THE_REPORT_COULD_NOT_BE_LOADED),
		).toBeVisible();
		expect(screen.queryByText(/This chart couldn't be loaded/)).toBeNull();
		expect(screen.queryByRole("button", { name: "Retry" })).toBeNull();
		expect(readingOf(theBackControl() as HTMLElement)).toBe(BACK_TO_REPORTS);
		await aMomentLater();
		expect(renderReadiness()).not.toBe("true");
		expect(dialog.timesOpened()).toBe(0);
	});
});

describe("What the print page prints", () => {
	beforeEach(() => {
		reportUsage.mockClear();
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

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("heads the page with the owner, the report's name, its two windows and when it was produced, in that order", async () => {
		openingThePrintPageDirectly();
		await thePrintPageIsReady();

		const lines = thePrintedLines();
		const owner = indexOfLine(lines, "Team Lightspeed");
		const title = indexOfLine(lines, ENGAGEMENT_START);
		const windows = indexOfLine(lines, WINDOWS_LINE);
		const produced = indexOfLine(lines, PRODUCED_LINE);
		expect([owner, title, windows, produced]).toEqual(
			[owner, title, windows, produced].sort((a, b) => a - b),
		);
		expect(
			screen.getByRole("heading", { level: 1, name: ENGAGEMENT_START }),
		).toBeVisible();
		expect(screen.getByRole("separator")).toBeInTheDocument();
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("shows nothing of the app around the report: no Edit report, no Delete, no tabs, no navigation", async () => {
		openingThePrintPageDirectly();
		await thePrintPageIsReady();

		expect(screen.getAllByRole("region").length).toBeGreaterThan(0);
		expect(screen.queryByRole("button", { name: "Edit report" })).toBeNull();
		expect(screen.queryByRole("button", { name: "Delete" })).toBeNull();
		expect(screen.queryByRole("tablist")).toBeNull();
		expect(screen.queryByRole("navigation", { name: "breadcrumb" })).toBeNull();
	});

	// @us-01 @slice-01 @contract-shape:bounded-change
	it('ends with the Lighthouse mark and "Made with Lighthouse by LetPeopleWork", after the last panel', async () => {
		openingThePrintPageDirectly();
		await thePrintPageIsReady();

		const branding = screen.getByText(BRANDING);
		const panels = screen.getAllByRole("region");
		const lastPanel = panels[panels.length - 1];
		expect(
			lastPanel.compareDocumentPosition(branding) &
				Node.DOCUMENT_POSITION_FOLLOWING,
		).toBeTruthy();
		const footer = branding.parentElement as HTMLElement;
		expect(
			within(footer).getByRole("img", { name: /Lighthouse/ }),
		).toBeVisible();
	});

	// @us-01 @slice-01 @contract-shape:bounded-change
	// The browser offers the page's title as the PDF's file name.
	it("names the page after the report and its owner, so the PDF is saved under that name", async () => {
		openingThePrintPageDirectly();
		await thePrintPageIsReady();

		expect(document.title).toBe(`${ENGAGEMENT_START} — Team Lightspeed`);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("names a Portfolio as the owner of a Portfolio's report", async () => {
		openingThePrintPageDirectly({
			startAt: printPathFor(APOLLO, ENGAGEMENT_START_ID),
			reportService: aReportService({
				getReport: vi.fn().mockResolvedValue(
					theEngagementStartReport({
						ownerKind: "portfolio",
						ownerId: APOLLO.id,
					}),
				),
			}),
		});
		await thePrintPageIsReady();

		expect(thePrintedLines()).toContain("Portfolio Apollo");
		expect(document.title).toBe(`${ENGAGEMENT_START} — Portfolio Apollo`);
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	// A panel's title is the instance's term alone, as on screen.
	it("prints the instance's own words for the owner and the metrics", async () => {
		openingThePrintPageDirectly({
			terminologyService: aTerminologyServiceAnswering(
				Promise.resolve(
					theTermsAnswer({
						...defaultTerms,
						[TERMINOLOGY_KEYS.TEAM]: "Squad",
						[TERMINOLOGY_KEYS.CYCLE_TIME]: "Lead Time",
					}),
				),
			),
		});
		await thePrintPageIsReady();

		const lines = thePrintedLines();
		expect(lines).toContain("Squad Lightspeed");
		expect(lines).not.toContain("Team Lightspeed");
		expect(screen.getByRole("region", { name: "Lead Time" })).toBeVisible();
		expect(screen.queryByRole("region", { name: "Cycle Time" })).toBeNull();
		expect(document.title).toBe(`${ENGAGEMENT_START} — Squad Lightspeed`);
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	// Paper is white whatever the reader's screen is, so the page paints its own light ground and ink.
	it("prints dark ink on its own white ground, the whole page high, even when the reader's screen is dark", async () => {
		openingThePrintPageDirectly({ readersTheme: "dark" });
		await thePrintPageIsReady();

		const page = thePrintPage();
		expect(getComputedStyle(page).backgroundColor).toBe(WHITE);
		expect(getComputedStyle(page).color).toBe(LIGHT_INK);
		expect(
			getComputedStyle(screen.getByRole("heading", { level: 1 })).color,
		).toBe(LIGHT_INK);
		// jsdom resolves a viewport height to pixels, so the whole window's height is what a full-height page reads.
		expect(getComputedStyle(page).minHeight).toBe(
			`${globalThis.innerHeight}px`,
		);
		expect(theStylesOfThePage()).toMatch(/color-scheme:\s*light/);
	});
});

describe("The print page prints the report as it is now", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
	});

	afterEach(forgetTheBrowser);

	// @us-01 @slice-01 @error @kpi-OUT-6298-G-pdf-matches-screen @contract-shape:unbounded-preservation
	// An answer read a minute ago still counts as fresh for the rest of the app, which keeps answers five minutes.
	it("never prints an answer it was holding from a minute ago, and reads the report afresh", async () => {
		aPrintDialog();
		const queryClient = aQueryClient();
		queryClient.setQueryData(
			reportQueryKeyOf(LIGHTSPEED, ENGAGEMENT_START_ID),
			theEngagementStartReport({ name: "An older name of the report" }),
			{ updatedAt: Date.now() - 60 * 1000 },
		);
		const theFreshRead =
			deferred<ReturnType<typeof theEngagementStartReport>>();
		const getReport = vi.fn().mockReturnValue(theFreshRead.promise);
		const teamService = aTeamServiceAnswering(Promise.resolve(lightspeed()));

		openingThePrintPageDirectly({
			queryClient,
			reportService: aReportService({ getReport }),
			teamService,
		});
		expect(renderReadiness()).not.toBe("true");
		expect(screen.queryByText("An older name of the report")).toBeNull();

		// Everything but the fresh read is in, so only the held answer could be drawn now.
		await waitFor(() => expect(teamService.getTeam).toHaveBeenCalled());
		await act(() => new Promise((settle) => setTimeout(settle, 50)));
		expect(screen.queryByText("An older name of the report")).toBeNull();
		expect(renderReadiness()).not.toBe("true");

		await act(async () => {
			theFreshRead.resolve(theEngagementStartReport());
		});
		await thePrintPageIsReady();
		expect(getReport).toHaveBeenCalledTimes(1);
		expect(screen.queryByText("An older name of the report")).toBeNull();
		expect(
			screen.getByRole("heading", { name: ENGAGEMENT_START }),
		).toBeVisible();
	});

	// @us-01 @slice-01 @edge @contract-shape:bounded-change
	it("reads the report again even when the report view has only just read it", async () => {
		aPrintDialog();
		const getReport = vi.fn().mockResolvedValue(theEngagementStartReport());
		const { user } = renderTheExportScene({
			reportService: aReportService({ getReport }),
			startAt: atTheReport,
		});
		const exportButton = await screen.findByRole("button", {
			name: EXPORT_PDF,
		});
		await waitFor(() => expect(exportButton).toBeEnabled());
		const readsBeforeExport = getReport.mock.calls.length;

		await user.click(exportButton);
		await thePrintPageIsReady();

		expect(getReport.mock.calls.length).toBeGreaterThan(readsBeforeExport);
	});

	// @us-01 @slice-01 @driving_port @kpi-OUT-6298-G-pdf-matches-screen @contract-shape:bounded-change
	// The screen and the PDF agree: back on the report, the reader sees the values that were just printed.
	it("shows the report view the values it just printed once the dialog closes", async () => {
		const dialog = aPrintDialog();
		const renamed = "Then & Now — engagement start, as read for the PDF";
		const getReport = vi
			.fn()
			.mockResolvedValueOnce(theEngagementStartReport())
			.mockResolvedValue(theEngagementStartReport({ name: renamed }));
		await exportingFromTheReport({
			reportService: aReportService({ getReport }),
		});
		await thePrintPageIsReady();
		expect(
			screen.getByRole("heading", { level: 1, name: renamed }),
		).toBeVisible();

		dialog.close();

		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheReport));
		expect(await screen.findByText(renamed)).toBeVisible();
		expect(screen.queryByText(ENGAGEMENT_START)).toBeNull();
	});

	// @us-01 @slice-01 @driving_port @kpi-OUT-6298-G-pdf-matches-screen @contract-shape:bounded-change
	it("prints the same summary and panels the reader sees on the report", async () => {
		aPrintDialog();
		const onScreen = renderTheExportScene({
			reportService: aReportService(),
			startAt: atTheReport,
			withPrintPages: false,
		});
		await waitFor(() =>
			expect(document.querySelector("[data-report-summary]")).not.toBeNull(),
		);
		const theReportAsRead = () => ({
			summary: readingOf(
				document.querySelector<HTMLElement>(
					"[data-report-summary]",
				) as HTMLElement,
			),
			panels: screen.getAllByRole("region").map(readingOf),
		});
		const seenOnScreen = theReportAsRead();
		onScreen.unmount();

		openingThePrintPageDirectly();
		await thePrintPageIsReady();

		expect(theReportAsRead()).toEqual(seenOnScreen);
	});
});

describe("What the print page reports about its use", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
		vi.useFakeTimers({ shouldAdvanceTime: true });
	});

	afterEach(() => {
		vi.useRealTimers();
		forgetTheBrowser();
	});

	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:unbounded-preservation
	// The export was counted when it was chosen; the page drawn for paper is not a page anyone opened.
	it("reports nothing of its own, however long it stays open", async () => {
		aPrintDialog();
		openingThePrintPageDirectly({ onFakeTimers: true });
		await thePrintPageIsReady();

		await act(async () => {
			await vi.advanceTimersByTimeAsync(10_000);
		});

		expect(reportUsage).not.toHaveBeenCalled();
	});

	/** Every event is taken, as it is once this browser has said yes to sending usage data. */
	const timesTheReportWasOpened = () =>
		reportUsage.mock.calls.filter(([use]) => use.name === "ReportOpened")
			.length;

	const stayingFor = (milliseconds: number) =>
		act(async () => {
			await vi.advanceTimersByTimeAsync(milliseconds);
		});

	/** The reader opens the report, stays on it for a while, exports, and comes back from the print dialog. */
	const exportingAfterStayingFor = async (milliseconds: number) => {
		const dialog = aPrintDialog();
		const rendered = renderTheExportScene({
			reportService: aReportService(),
			startAt: atTheReport,
			onFakeTimers: true,
		});
		const exportButton = await screen.findByRole("button", {
			name: EXPORT_PDF,
		});
		await stayingFor(milliseconds);
		await rendered.user.click(exportButton);
		await thePrintPageIsReady();
		await waitFor(() => expect(dialog.timesOpened()).toBe(1));
		dialog.close();
		await screen.findByRole("button", { name: EXPORT_PDF });
		return rendered;
	};

	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
	// One opening is one visit to the report; a reader who exported before it counted has not been counted yet, and
	// coming back starts the wait again rather than carrying on from where it stopped.
	it.skip(`counts the report as opened once the reader has stayed on it after printing, when they exported before it counted (${pendingReviewFix})`, async () => {
		await exportingAfterStayingFor(3_000);

		await stayingFor(4_000);
		expect(timesTheReportWasOpened()).toBe(0);

		await stayingFor(1_000);
		expect(timesTheReportWasOpened()).toBe(1);
	});

	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:unbounded-preservation
	// Coming back from the dialog is the same reading of the report, not a second opening of it.
	it("counts the report as opened once, not again when the reader comes back from printing", async () => {
		await exportingAfterStayingFor(6_000);
		expect(timesTheReportWasOpened()).toBe(1);

		await stayingFor(10_000);

		expect(timesTheReportWasOpened()).toBe(1);
	});

	// @us-01 @slice-01 @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
	// Opening the report again from the list is a new visit, printed in between or not.
	it("counts the report as opened again when the reader later opens it anew from the list", async () => {
		const { user } = await exportingAfterStayingFor(6_000);
		await stayingFor(10_000);

		await user.click(screen.getByRole("link", { name: "Reports" }));
		await user.click(
			await screen.findByRole("link", { name: ENGAGEMENT_START }),
		);
		await screen.findByRole("button", { name: EXPORT_PDF });
		await stayingFor(6_000);

		expect(timesTheReportWasOpened()).toBe(2);
	});
});
