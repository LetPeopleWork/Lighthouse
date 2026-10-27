import { focusManager } from "@tanstack/react-query";
import { screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { actAndSettle } from "../../tests/actAndSettle";
import {
	aReportOn,
	everyReport,
	QUARTER,
	renderTheReportsPage,
	theReportsTable,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { deferred } from "../../tests/HeldMetricsService";
import { aReportService } from "../../tests/ReportsTestKit";

/**
 * One showing of the Reports page is one listing for the usage data: reading the list again while the page stays,
 * as it does when the window regains focus, is the same listing, and a listing the browser could not yet tell about
 * is not used up.
 */

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));

vi.mock(
	"../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

const listings = () =>
	reportUsage.mock.calls.filter(([event]) => event.name === "AllReportsListed");

/** The window regains focus; the list is read again, and is seen on its way before the answer comes. */
const readTheListAgain = async (getAllReports: ReturnType<typeof vi.fn>) => {
	const answer = deferred<ReturnType<typeof everyReport>>();
	getAllReports.mockReturnValueOnce(answer.promise);
	await actAndSettle(() => {
		focusManager.setFocused(false);
		focusManager.setFocused(true);
	});
	await waitFor(() =>
		expect(screen.getByTestId("all-reports")).toHaveAttribute(
			"data-widget-status",
			"loading",
		),
	);
	answer.resolve(everyReport(aReportOn(ZENITH, QUARTER)));
	await theReportsTable();
};

describe("Listings the Reports page tells the usage data", () => {
	beforeEach(() => reportUsage.mockReset());
	afterEach(() => focusManager.setFocused(undefined));

	it(`reading the list again while the page stays is not another listing`, async () => {
		reportUsage.mockReturnValue(true);
		const getAllReports = vi
			.fn()
			.mockResolvedValue(everyReport(aReportOn(ZENITH, QUARTER)));
		renderTheReportsPage({ reportService: aReportService({ getAllReports }) });
		await theReportsTable();

		await readTheListAgain(getAllReports);
		await readTheListAgain(getAllReports);

		expect(listings()).toHaveLength(1);
	});

	it(`a listing the browser could not yet tell about is told once it can, and only once`, async () => {
		reportUsage.mockReturnValue(false);
		const getAllReports = vi
			.fn()
			.mockResolvedValue(everyReport(aReportOn(ZENITH, QUARTER)));
		renderTheReportsPage({ reportService: aReportService({ getAllReports }) });
		await theReportsTable();
		expect(listings()).toHaveLength(1);

		reportUsage.mockReturnValue(true);
		await readTheListAgain(getAllReports);
		expect(listings()).toHaveLength(2);

		await readTheListAgain(getAllReports);
		expect(listings()).toHaveLength(2);
	});
});
