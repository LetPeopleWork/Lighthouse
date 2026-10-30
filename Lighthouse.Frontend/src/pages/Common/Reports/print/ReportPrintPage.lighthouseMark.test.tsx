import { screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import {
	aMomentLater,
	aPrintDialog,
	aPrintFrameBrowser,
	everyPrintFrameIsGone,
	forgetTheBrowser,
	openingThePrintPageDirectly,
	PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
	PREPARING,
	renderReadiness,
	renderTheExportScene,
	reportPathFor,
	theErrorSnackbar,
	theExportButtonOnceItCanBePressed,
	theFontsHaveLoaded,
	theFooter,
	theFramesHavePrinted,
	theLighthouseMarkCannotLoad,
	theLighthouseMarkIsStillLoading,
	thePrintFrames,
	thePrintPageIsReady,
	theSnackbarsRetry,
} from "../../../../tests/ReportPrintTestKit";
import {
	aReportService,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
} from "../../../../tests/ReportsTestKit";

/**
 * The Lighthouse mark in the footer is a picture the browser fetches, and a PDF made before it arrived has an empty
 * corner where the mark belongs. So the report drawn for paper waits for the mark as it waits for its fonts: not
 * ready until the mark is in, and a mark that cannot be loaded is part of the page that could not be loaded. The
 * print page opened from its address shows this; Export PDF prints only once the mark is in its frame.
 */

/** The address of the mark the footer shows. */
const theMarksAddress = (): string | null =>
	within(theFooter())
		.getByRole("img", { name: /Lighthouse/ })
		.getAttribute("src");

describe("The Lighthouse mark is in the PDF", () => {
	let printDialog: ReturnType<typeof aPrintDialog>;

	beforeEach(() => {
		theFontsHaveLoaded();
		printDialog = aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	it("waits for the Lighthouse mark before it is ready", async () => {
		const mark = theLighthouseMarkIsStillLoading();
		openingThePrintPageDirectly();
		await screen.findByRole("heading", { level: 1 });
		await aMomentLater();

		expect(renderReadiness()).toBe("false");
		expect(mark.pictures()).toEqual([theMarksAddress()]);

		await mark.arrive();
		await thePrintPageIsReady();
		expect(printDialog.timesOpened()).toBe(0);
	});

	it("says part of the page couldn't be loaded, offers Retry and never gets ready, when the Lighthouse mark cannot be loaded", async () => {
		theLighthouseMarkCannotLoad();
		openingThePrintPageDirectly();

		expect(
			await screen.findByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED),
		).toBeVisible();
		expect(screen.getByRole("button", { name: "Retry" })).toBeVisible();
		await aMomentLater();
		expect(renderReadiness()).not.toBe("true");
		expect(printDialog.timesOpened()).toBe(0);
	});

	it("asks for the Lighthouse mark again on Retry and is ready once it is in", async () => {
		const mark = theLighthouseMarkCannotLoad();
		const { user } = openingThePrintPageDirectly();
		await screen.findByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED);

		mark.canBeReachedAgain();
		await user.click(screen.getByRole("button", { name: "Retry" }));

		await thePrintPageIsReady();
		expect(mark.pictures()).toHaveLength(2);
		expect(screen.queryByText(PREPARING)).toBeNull();
	});

	// A fresh frame is a fresh document, so the mark is asked for again there.
	it(`asks for the Lighthouse mark again in a fresh frame when Retry follows an export whose mark could not be loaded, and prints once it is in`, async () => {
		const frames = aPrintFrameBrowser();
		const mark = theLighthouseMarkCannotLoad();
		const { user } = renderTheExportScene({
			reportService: aReportService(),
			startAt: reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
		});
		await user.click(await theExportButtonOnceItCanBePressed());
		const snackbar = await theErrorSnackbar();
		expect(snackbar).toHaveTextContent(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED);
		await everyPrintFrameIsGone();

		mark.canBeReachedAgain();
		await user.click(theSnackbarsRetry(snackbar) as HTMLElement);

		await theFramesHavePrinted(frames);
		expect(mark.pictures()).toHaveLength(2);
		expect(thePrintFrames()).toEqual(frames.framesThatPrinted());
		expect(printDialog.timesOpened()).toBe(0);
	});
});
