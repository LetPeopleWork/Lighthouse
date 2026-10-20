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
	EXPORTED_AT,
	forgetTheBrowser,
	lightspeed,
	PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
	PREPARING,
	printPathFor,
	renderReadiness,
	renderTheExportScene,
	reportPathFor,
	reportsPathFor,
	THE_REPORT_COULD_NOT_BE_LOADED,
	theBackControl,
	theFontsAreStillLoading,
	theFontsCannotLoad,
	theFontsHaveLoaded,
	thePrintPageIsReady,
	theTermsAnswer,
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
 * The print page of a report, opened from its address: the report drawn for paper and nothing else, as a pasted
 * link, the end-to-end PDF check and a later server renderer open it. It reads the report afresh, says it is
 * preparing until everything it prints is in - the report, the owner's name, the instance's terms, the fonts and
 * the Lighthouse mark - and says when it is ready, but it never asks for the print dialog itself: Export PDF
 * prints from a hidden frame on the reader's own page. A report that cannot be read makes no PDF and says so, and
 * the page's one way on is "‹ Back to Reports".
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
	it("is not ready while the fonts are on their way, and ready once they arrive", async () => {
		aPrintDialog();
		const fonts = theFontsAreStillLoading();

		openingThePrintPageDirectly();
		expect(await screen.findByText(PREPARING)).toBeVisible();
		await aMomentLater();
		expect(renderReadiness()).toBe("false");

		await fonts.arrive();

		await thePrintPageIsReady();
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

			openingThePrintPageDirectly();

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

describe("The print page never prints by itself", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
	});

	afterEach(forgetTheBrowser);

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

	// @us-01 @rev-3 @error @contract-shape:unbounded-preservation
	// A history entry an older Lighthouse left behind, reached by a reload or Forward, still asks for nothing.
	it(`never prints, even when its history entry still carries an export's wish to print`, async () => {
		const dialog = aPrintDialog();

		openingThePrintPageDirectly({
			startState: { printWhenReady: true, from: "report" },
		});
		await thePrintPageIsReady();
		await aMomentLater();

		expect(dialog.timesOpened()).toBe(0);
	});

	// @us-01 @rev-3 @driving_port @contract-shape:bounded-change
	// Nothing sends a reader here any more, so the page knows no report view to return to; the list is its one way on.
	it(`offers only "‹ Back to Reports", a plain link to the list that carries nothing, whatever its history entry says`, async () => {
		aPrintDialog();
		const { user } = openingThePrintPageDirectly({
			startState: { from: "report" },
		});
		await thePrintPageIsReady();

		expect(screen.queryByText(BACK_TO_REPORT)).toBeNull();
		const back = theBackControl() as HTMLElement;
		expect(readingOf(back)).toBe(BACK_TO_REPORTS);
		expect(back.tagName).toBe("A");
		await user.click(back);

		await waitFor(() => expect(whereTheReaderIs().path).toBe(atTheList));
		expect(whatThisEntryCarries()).toBeNull();
	});
});

describe("A report the print page cannot read", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		theFontsHaveLoaded();
	});

	afterEach(forgetTheBrowser);

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("says no PDF was made, offers Retry, and never gets ready or prints, when the report could not be loaded", async () => {
		const dialog = aPrintDialog();
		openingThePrintPageDirectly({
			reportService: aReportService({
				getReport: vi.fn().mockRejectedValue(new Error("the network dropped")),
			}),
		});

		expect(
			await screen.findByText(THE_REPORT_COULD_NOT_BE_LOADED),
		).toBeVisible();
		expect(screen.queryByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED)).toBeNull();
		expect(screen.getByRole("button", { name: "Retry" })).toBeEnabled();
		await aMomentLater();
		expect(renderReadiness()).not.toBe("true");
		expect(dialog.timesOpened()).toBe(0);
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
	it("says part of the page could not be loaded, offers Retry, and never prints, when the owner's name cannot be read", async () => {
		const dialog = aPrintDialog();
		const teamService = aTeamServiceThatCannotReadTheName();
		const { user } = openingThePrintPageDirectly({ teamService });

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

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it("says part of the page could not be loaded when the instance's terms cannot be read, and Retry reads them again and gets the page ready", async () => {
		const dialog = aPrintDialog();
		const terminologyService = aTerminologyServiceThatCannotReadTheTerms({
			untilTheRead: 2,
		});
		const { user } = openingThePrintPageDirectly({ terminologyService });

		expect(
			await screen.findByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED),
		).toBeVisible();
		await aMomentLater();
		expect(renderReadiness()).not.toBe("true");

		const readsBeforeRetry = vi.mocked(terminologyService.getAllTerminology)
			.mock.calls.length;
		await user.click(screen.getByRole("button", { name: "Retry" }));

		await thePrintPageIsReady();
		expect(
			vi.mocked(terminologyService.getAllTerminology).mock.calls.length,
		).toBeGreaterThan(readsBeforeRetry);
		expect(screen.queryByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED)).toBeNull();
		await aMomentLater();
		expect(dialog.timesOpened()).toBe(0);
	});

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	// The page must not go on saying it failed, nor offer Retry a second time, while the answer to Retry is on its way.
	it(`shows that it is preparing the PDF, with no Retry, while Retry reads the report again`, async () => {
		const dialog = aPrintDialog();
		const theRetriedRead =
			deferred<ReturnType<typeof theEngagementStartReport>>();
		const getReport = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockReturnValueOnce(theRetriedRead.promise);
		const { user } = openingThePrintPageDirectly({
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
		await aMomentLater();
		expect(dialog.timesOpened()).toBe(0);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// A Team or Portfolio deleted since the report was opened answers with no one; waiting for its name would never end.
	it.each<[string, () => Promise<unknown>]>([
		[
			"Team",
			async () =>
				openingThePrintPageDirectly({
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
	it(`says the report couldn't be loaded, offers no Retry and leads back to Reports, when the report is refused for a reason with no sentence of its own`, async () => {
		const dialog = aPrintDialog();
		openingThePrintPageDirectly({
			reportService: aReportService({
				getReport: vi.fn().mockRejectedValue(aRefusal(422, "then-too-short")),
			}),
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
});
