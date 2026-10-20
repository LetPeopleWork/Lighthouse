import { screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import {
	aMomentLater,
	aPrintDialog,
	BRANDING,
	forgetTheBrowser,
	PART_OF_THE_PAGE_COULD_NOT_BE_LOADED,
	renderReadiness,
	renderTheExportScene,
	reportPathFor,
	theExportButtonOnceItCanBePressed,
	theFontsHaveLoaded,
	theLighthouseMarkCannotLoad,
	theLighthouseMarkIsStillLoading,
	thePrintPageIsReady,
} from "../../../../tests/ReportPrintTestKit";
import {
	aReportService,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
} from "../../../../tests/ReportsTestKit";

/**
 * The Lighthouse mark in the footer is a picture the browser fetches, and a PDF made before it arrived has an empty
 * corner where the mark belongs. So the print page waits for the mark as it waits for its fonts: not ready and no
 * print dialog until the mark is in, and a mark that cannot be loaded is part of the page that could not be loaded.
 */

/** The reader on the report view chooses Export PDF and arrives at its print page. */
const exportingFromTheReport = async () => {
	const rendered = renderTheExportScene({
		reportService: aReportService(),
		startAt: reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
	});
	await rendered.user.click(await theExportButtonOnceItCanBePressed());
	return rendered;
};

/** The address of the mark the footer shows. */
const theMarksAddress = (): string | null => {
	const footer = screen.getByText(BRANDING).closest("footer");
	expect(footer, "the credit is not in a footer").not.toBeNull();
	return within(footer as HTMLElement)
		.getByRole("img", { name: /Lighthouse/ })
		.getAttribute("src");
};

describe("The Lighthouse mark is in the PDF", () => {
	let printDialog: ReturnType<typeof aPrintDialog>;

	beforeEach(() => {
		theFontsHaveLoaded();
		printDialog = aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-02 @slice-02 @driving_port @contract-shape:bounded-change
	// Fails today: the page never asks for the mark, so it is ready and prints with the mark still on its way.
	it("waits for the Lighthouse mark before it is ready, then opens the print dialog once", async () => {
		const mark = theLighthouseMarkIsStillLoading();
		await exportingFromTheReport();
		await screen.findByRole("heading", { level: 1 });
		await aMomentLater();

		expect(renderReadiness()).toBe("false");
		expect(printDialog.timesOpened()).toBe(0);
		expect(mark.pictures()).toEqual([theMarksAddress()]);

		await mark.arrive();
		await thePrintPageIsReady();
		expect(printDialog.timesOpened()).toBe(1);
	});

	// @us-02 @slice-02 @error @contract-shape:bounded-change
	// Fails today: the page ignores the mark and prints without it.
	it("says part of the page couldn't be loaded, offers Retry and never prints, when the Lighthouse mark cannot be loaded", async () => {
		theLighthouseMarkCannotLoad();
		await exportingFromTheReport();

		expect(
			await screen.findByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED),
		).toBeVisible();
		expect(screen.getByRole("button", { name: "Retry" })).toBeVisible();
		await aMomentLater();
		expect(renderReadiness()).not.toBe("true");
		expect(printDialog.timesOpened()).toBe(0);
	});

	// @us-02 @slice-02 @error @contract-shape:bounded-change
	// Fails today: the page never asks for the mark, so Retry has nothing to ask again.
	it("asks for the Lighthouse mark again on Retry and prints once it is in", async () => {
		const mark = theLighthouseMarkCannotLoad();
		const { user } = await exportingFromTheReport();
		await screen.findByText(PART_OF_THE_PAGE_COULD_NOT_BE_LOADED);

		mark.canBeReachedAgain();
		await user.click(screen.getByRole("button", { name: "Retry" }));

		await thePrintPageIsReady();
		expect(mark.pictures()).toHaveLength(2);
		expect(printDialog.timesOpened()).toBe(1);
	});
});
