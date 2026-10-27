import { waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IUsageDataState } from "../../models/UsageData/UsageData";
import type { IUsageDataService } from "../../services/Api/UsageDataService";
import {
	forgetWhatWasNoticed,
	takeWhatWasNoticed,
} from "../../services/UsageData/usageDataBuffer";
import {
	aReportOn,
	everyReport,
	pendingEpic6350,
	QUARTER,
	renderTheReportsPage,
	theReportsTable,
	thisBrowsersUsageDataAnswer,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { createMockUsageDataService } from "../../tests/MockApiServiceProvider";
import { aReportService } from "../../tests/ReportsTestKit";

/**
 * The Reports page is counted only where this browser agreed to send usage data. Here the page's own reporter and
 * the consent this browser was given are the real ones; what the browser would hand in next is read off what it
 * noticed, so nothing about the reports or their owners can ride along unseen.
 */

const TOKEN_STORAGE_KEY = "lighthouse:usagedata:consent";

const pending = pendingEpic6350("01");

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

const showingThePageWith = async (
	usageDataService: IUsageDataService,
	answerFirst: "sending" | "not-sending",
) => {
	renderTheReportsPage({
		reportService: aReportService({
			getAllReports: vi
				.fn()
				.mockResolvedValue(everyReport(aReportOn(ZENITH, QUARTER))),
		}),
		usageDataService,
	});
	await waitFor(() => expect(thisBrowsersUsageDataAnswer()).toBe(answerFirst));
	await theReportsTable();
};

const pagesNoticed = () =>
	takeWhatWasNoticed().filter(
		(seen) => String(seen.name) === "AllReportsListed",
	);

describe("The Reports page is counted only where this browser agreed", () => {
	beforeEach(() => {
		forgetWhatWasNoticed();
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");
	});

	afterEach(() => {
		localStorage.clear();
		forgetWhatWasNoticed();
	});

	// @driving_port @us-01 @slice-01 @kpi-OUT-6350-every-report-found @contract-shape:bounded-change
	it.skip(`counts the page, and nothing about its reports, from a browser that agreed (${pending})`, async () => {
		await showingThePageWith(theAnswerAboutThisBrowser({}), "sending");

		await waitFor(() =>
			expect(
				pagesNoticed().map((seen) =>
					Object.keys(seen).sort((a, b) => a.localeCompare(b)),
				),
			).toEqual([["name", "noticedAt"]]),
		);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it.skip(`counts nothing from a browser that said no, token or not (${pending})`, async () => {
		await showingThePageWith(
			theAnswerAboutThisBrowser({ sending: false, decision: "Declined" }),
			"not-sending",
		);

		expect(pagesNoticed()).toEqual([]);
	});
});
