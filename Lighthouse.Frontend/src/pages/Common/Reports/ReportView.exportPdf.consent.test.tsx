import { waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IUsageDataState } from "../../../models/UsageData/UsageData";
import type { IUsageDataService } from "../../../services/Api/UsageDataService";
import {
	forgetWhatWasNoticed,
	takeWhatWasNoticed,
} from "../../../services/UsageData/usageDataBuffer";
import { createMockUsageDataService } from "../../../tests/MockApiServiceProvider";
import {
	aPrintDialog,
	forgetTheBrowser,
	PDF_EXPORT_STARTED,
	renderTheExportScene,
	reportPathFor,
	THEN_AND_NOW,
	theExportButtonOnceItCanBePressed,
	theFontsHaveLoaded,
	theStandInForThePrintPage,
	thisBrowsersUsageDataAnswer,
} from "../../../tests/ReportPrintTestKit";
import {
	aReportService,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
} from "../../../tests/ReportsTestKit";

/**
 * The export count goes only where this browser agreed to send usage data. Here the page's own reporter and
 * the consent this browser was given are the real ones; what the browser would hand in next is read off what
 * it noticed.
 */

const TOKEN_STORAGE_KEY = "lighthouse:usagedata:consent";

const theAnswerAboutThisBrowser = (
	overrides: Partial<IUsageDataState>,
): IUsageDataService => ({
	...createMockUsageDataService(),
	getState: vi.fn().mockResolvedValue({
		sending: true,
		decision: "Granted",
		mayAsk: false,
		reAskAfterDays: 90,
		administratorDisabled: false,
		...overrides,
	}),
});

const exportingWith = async (
	usageDataService: IUsageDataService,
	answerFirst: "sending" | "not-sending",
) => {
	const { user } = renderTheExportScene({
		reportService: aReportService(),
		startAt: reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
		withPrintPages: false,
		usageDataService,
	});
	// The click must come after this browser's answer arrived, or a yes would read as "not yet".
	await waitFor(() => expect(thisBrowsersUsageDataAnswer()).toBe(answerFirst));

	await user.click(await theExportButtonOnceItCanBePressed());
	await theStandInForThePrintPage();
};

const exportsNoticed = () =>
	takeWhatWasNoticed().filter(
		(seen) => String(seen.name) === PDF_EXPORT_STARTED,
	);

describe("Export PDF is counted only where this browser agreed", () => {
	beforeEach(() => {
		forgetWhatWasNoticed();
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(() => {
		localStorage.clear();
		forgetWhatWasNoticed();
		forgetTheBrowser();
	});

	// @us-01 @slice-01 @driving_port @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
	it(`counts the export, with the report's Type, from a browser that agreed`, async () => {
		await exportingWith(theAnswerAboutThisBrowser({}), "sending");

		expect(exportsNoticed()).toEqual([
			expect.objectContaining({
				name: PDF_EXPORT_STARTED,
				reportTemplate: THEN_AND_NOW,
			}),
		]);
	});

	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:unbounded-preservation
	// A refusal mints a token too, so holding one says nothing about agreeing.
	it(`counts nothing from a browser that said no, token or not`, async () => {
		await exportingWith(
			theAnswerAboutThisBrowser({ sending: false, decision: "Declined" }),
			"not-sending",
		);

		expect(exportsNoticed()).toEqual([]);
	});
});
