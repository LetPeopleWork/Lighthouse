import { screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	anOwnerToCreateOn,
	aReportOn,
	createCount,
	created,
	creationOptionsFor,
	DELETE,
	everyReport,
	exportButton,
	GRAVITY,
	notCreated,
	notRemoved,
	PULSAR,
	pendingEpic6350,
	pickOwner,
	QUARTER,
	removeButton,
	removeCount,
	removed,
	renderTheReportsPage,
	rowOf,
	selectTheRowOf,
	theReportsTable,
	toTheOwnerStep,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import {
	aPrintFrameBrowser,
	forgetTheBrowser,
	theFontsHaveLoaded,
	theFramesHavePrinted,
} from "../../tests/ReportPrintTestKit";
import {
	aReportService,
	theEngagementStartReport,
} from "../../tests/ReportsTestKit";

/**
 * What the Reports page tells the usage data, for a browser that agreed to send it: that the page was shown, that
 * a bulk creation, a bulk removal or one PDF of several reports was started, each by its name alone, and one
 * created or deleted report per report the run actually created or removed. Never how many, which, or why not.
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

const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 1001 });
const gravitysQuarter = aReportOn(GRAVITY, QUARTER, { id: 1002 });

const theEventsNamed = (name: string) =>
	reportUsage.mock.calls.filter(([event]) => event.name === name);

describe("What the Reports page tells the usage data", () => {
	beforeEach(() => {
		reportUsage.mockClear();
	});

	// @driving_port @us-01 @slice-01 @kpi-OUT-6350-every-report-found @contract-shape:bounded-change
	it(`showing the page is told once, by name alone`, async () => {
		renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport(zenithsQuarter)),
			}),
		});
		await theReportsTable();

		expect(theEventsNamed("AllReportsListed")).toEqual([
			[{ name: "AllReportsListed" }],
		]);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it(`a page whose reports could not be loaded tells nothing`, async () => {
		renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockRejectedValue(new Error("the network dropped")),
			}),
		});
		await screen.findByRole("button", { name: "Retry" });

		expect(theEventsNamed("AllReportsListed")).toEqual([]);
	});

	// @driving_port @us-02a @slice-02a @kpi-OUT-6350-bulk-creation-used @contract-shape:bounded-change
	it(`a bulk creation is told once, and one created report per report actually created`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(
							anOwnerToCreateOn(ZENITH),
							anOwnerToCreateOn(GRAVITY),
						),
					),
				createReportsInBulk: vi
					.fn()
					.mockResolvedValue([
						created(GRAVITY, 1),
						notCreated(ZENITH, "not-admin"),
					]),
			}),
		});

		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await pickOwner(user, GRAVITY);
		await user.click(screen.getByRole("button", { name: createCount(2) }));

		await waitFor(() =>
			expect(theEventsNamed("ReportCreated")).toEqual([
				[{ name: "ReportCreated", reportTemplate: "ThenAndNow" }],
			]),
		);
		expect(theEventsNamed("ReportBulkCreationStarted")).toEqual([
			[{ name: "ReportBulkCreationStarted" }],
		]);
	});

	it(`two reports created and one not are told as two created reports`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(
							anOwnerToCreateOn(ZENITH),
							anOwnerToCreateOn(GRAVITY),
							anOwnerToCreateOn(PULSAR),
						),
					),
				createReportsInBulk: vi
					.fn()
					.mockResolvedValue([
						created(GRAVITY, 1),
						notCreated(PULSAR, "not-admin"),
						created(ZENITH, 2),
					]),
			}),
			permissions: { adminOf: [ZENITH, GRAVITY, PULSAR] },
		});

		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await pickOwner(user, GRAVITY);
		await pickOwner(user, PULSAR);
		await user.click(screen.getByRole("button", { name: createCount(3) }));

		await waitFor(() =>
			expect(theEventsNamed("ReportCreated")).toHaveLength(2),
		);
		expect(theEventsNamed("ReportCreated")).toEqual([
			[{ name: "ReportCreated", reportTemplate: "ThenAndNow" }],
			[{ name: "ReportCreated", reportTemplate: "ThenAndNow" }],
		]);
	});

	// @driving_port @us-05 @slice-05 @kpi-OUT-6350-bulk-removal-used @contract-shape:bounded-change
	it.skip(`a bulk removal is told once, and one deleted report per report this run removed (${pendingEpic6350("05")})`, async () => {
		const zenithsTrialRun = aReportOn(ZENITH, "Then & Now — trial run", {
			id: 1003,
		});
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValue(
						everyReport(zenithsQuarter, gravitysQuarter, zenithsTrialRun),
					),
				removeReportsInBulk: vi
					.fn()
					.mockResolvedValue([
						removed(1001),
						removed(1002, "already-gone"),
						notRemoved(1003, "not-admin"),
					]),
			}),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, QUARTER, GRAVITY);
		await selectTheRowOf(user, "Then & Now — trial run", ZENITH);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		await user.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: removeCount(3),
			}),
		);

		await waitFor(() =>
			expect(theEventsNamed("ReportDeleted")).toEqual([
				[{ name: "ReportDeleted" }],
			]),
		);
		expect(theEventsNamed("ReportBulkRemovalStarted")).toEqual([
			[{ name: "ReportBulkRemovalStarted" }],
		]);
	});

	// @driving_port @us-07 @slice-07 @kpi-OUT-6350-combined-pdf-used @contract-shape:bounded-change
	it.skip(`one PDF of several reports is told once, by name alone, and not as a single export (${pendingEpic6350("07")})`, async () => {
		theFontsHaveLoaded();
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValue(everyReport(zenithsQuarter, gravitysQuarter)),
				getReport: vi.fn().mockResolvedValue(theEngagementStartReport()),
			}),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, QUARTER, GRAVITY);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));
		await theFramesHavePrinted(frames, 1);

		expect(theEventsNamed("ReportCombinedPdfExportStarted")).toEqual([
			[{ name: "ReportCombinedPdfExportStarted" }],
		]);
		expect(theEventsNamed("ReportPdfExportStarted")).toEqual([]);
		forgetTheBrowser();
	});
	// @driving_port @us-07 @slice-07 @edge @kpi-OUT-6350-combined-pdf-used @contract-shape:bounded-change
	// One report picked makes the same PDF as the report's own Export, so it counts as that.
	it.skip(`exporting exactly one picked report is told as the single export, never as one PDF of several (${pendingEpic6350("07")})`, async () => {
		theFontsHaveLoaded();
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValue(everyReport(zenithsQuarter, gravitysQuarter)),
				getReport: vi.fn().mockResolvedValue(theEngagementStartReport()),
			}),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await user.click(screen.getByRole("button", { name: exportButton(1) }));
		await theFramesHavePrinted(frames, 1);

		expect(theEventsNamed("ReportPdfExportStarted")).toEqual([
			[{ name: "ReportPdfExportStarted", reportTemplate: "ThenAndNow" }],
		]);
		expect(theEventsNamed("ReportCombinedPdfExportStarted")).toEqual([]);
		forgetTheBrowser();
	});

	// @driving_port @us-01 @slice-01 @contract-shape:bounded-change
	it(`deleting one report from its row is told as one deleted report, as on the owner's tab (${pendingEpic6350("01")})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValueOnce(everyReport(zenithsQuarter))
					.mockResolvedValue(everyReport()),
				deleteReport: vi.fn().mockResolvedValue(undefined),
			}),
		});
		await theReportsTable();

		await user.click(
			within(rowOf(QUARTER, ZENITH)).getByRole("button", {
				name: `Delete ${QUARTER}`,
			}),
		);
		await user.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: DELETE,
			}),
		);

		await waitFor(() =>
			expect(theEventsNamed("ReportDeleted")).toEqual([
				[{ name: "ReportDeleted" }],
			]),
		);
	});
});
