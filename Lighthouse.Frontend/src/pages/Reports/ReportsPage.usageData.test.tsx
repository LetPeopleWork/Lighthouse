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
	LEAVE_IT_OUT,
	NO_PDF_WAS_MADE,
	notCreated,
	notRemoved,
	PULSAR,
	pickOwner,
	QUARTER,
	RETRY,
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

	it(`a bulk removal is told once, and one deleted report per report this run removed`, async () => {
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

	it(`a bulk removal tells one deleted report for each it removed, and none for one already gone`, async () => {
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
						removed(1002),
						removed(1003, "already-gone"),
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
				[{ name: "ReportDeleted" }],
			]),
		);
	});

	it("one PDF of several reports is told once, by name alone, and not as a single export", async () => {
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
	// One report picked makes the same PDF as the report's own Export, so it counts as that.
	it("exporting exactly one picked report is told as the single export, never as one PDF of several", async () => {
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

	// Both finish the PDF the reader already asked for, so neither is a second export.
	it("a Retry and a Leave it out on one PDF of several reports count nothing more", async () => {
		theFontsHaveLoaded();
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValue(everyReport(zenithsQuarter, gravitysQuarter)),
				getReport: vi.fn((_owner, reportId: number) =>
					reportId === zenithsQuarter.id
						? Promise.reject(new Error("the network dropped"))
						: Promise.resolve(theEngagementStartReport({ id: reportId })),
				),
			}),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, QUARTER, GRAVITY);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await user.click(
			within(
				await screen.findByRole("dialog", { name: NO_PDF_WAS_MADE }),
			).getByRole("button", { name: RETRY }),
		);
		await waitFor(() =>
			expect(
				screen.queryByRole("dialog", { name: NO_PDF_WAS_MADE }),
			).not.toBeInTheDocument(),
		);
		await user.click(
			within(
				await screen.findByRole("dialog", { name: NO_PDF_WAS_MADE }),
			).getByRole("button", { name: LEAVE_IT_OUT }),
		);
		await theFramesHavePrinted(frames, 1);

		expect(theEventsNamed("ReportCombinedPdfExportStarted")).toEqual([
			[{ name: "ReportCombinedPdfExportStarted" }],
		]);
		expect(theEventsNamed("ReportPdfExportStarted")).toEqual([]);
		forgetTheBrowser();
	});

	it("deleting one report from its row is told as one deleted report, as on the owner's tab", async () => {
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
