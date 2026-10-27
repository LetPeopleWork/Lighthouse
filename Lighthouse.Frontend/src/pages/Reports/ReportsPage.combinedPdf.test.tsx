import { screen, waitFor, within } from "@testing-library/react";
import {
	afterEach,
	beforeEach,
	describe,
	expect,
	it,
	type Mock,
	vi,
} from "vitest";
import type { IReportOwner } from "../../models/Reports/Report";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import type { IReportService } from "../../services/Api/ReportService";
import {
	AT_MOST_50,
	aReportOn,
	CANCEL,
	couldNotLoadTheReportOf,
	everyReport,
	exportButton,
	GRAVITY,
	LEAVE_IT_OUT,
	MONTH,
	moreReportsThanTheCeiling,
	NO_PDF_WAS_MADE,
	NOTHING_SELECTED_TO_EXPORT,
	neverAnswered,
	PART_OF_THE_PAGE_FAILED,
	PULSAR,
	pendingEpic6350,
	QUARTER,
	RETRY,
	renderTheReportsPage,
	selectTheRowOf,
	theReportNamesListed,
	theReportsTable,
	VOYAGER,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import {
	aPrintFrameBrowser,
	forgetTheBrowser,
	theFontsCannotLoad,
	theFontsHaveLoaded,
	theFramesHavePrinted,
	theFramesPage,
} from "../../tests/ReportPrintTestKit";
import {
	aReportService,
	theEngagementStartReport,
} from "../../tests/ReportsTestKit";

/**
 * One PDF of several reports, from the Reports page: the reader picks rows, presses Export PDF, and one print
 * dialog opens for all of them, each report starting on a page of its own, in the table's order. Any reader may
 * export what they can read. If a report cannot be loaded no PDF is made; the reader is told which one and may
 * leave it out, unless that would leave nothing to print.
 */

const pending = pendingEpic6350("07");

const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 901 });
const gravitysMonth = aReportOn(GRAVITY, MONTH, { id: 902 });
const voyagersQuarter = aReportOn(VOYAGER, QUARTER, { id: 903 });
const pulsarsQuarter = aReportOn(PULSAR, QUARTER, { id: 904 });
const everyListedReport = [
	zenithsQuarter,
	gravitysMonth,
	voyagersQuarter,
	pulsarsQuarter,
];

/** Each report read by its id, under its listed name; the ids in `failing` cannot be read. */
const eachReportReads = (
	failing: readonly number[] = [],
): Mock<IReportService["getReport"]> =>
	vi.fn((_owner: IReportOwner, reportId: number) =>
		failing.includes(reportId)
			? Promise.reject(new Error("the network dropped"))
			: Promise.resolve(
					theEngagementStartReport({
						id: reportId,
						name:
							everyListedReport.find((report) => report.id === reportId)
								?.name ?? QUARTER,
					}),
				),
	);

const listingAndReading = (
	getReport: IReportService["getReport"],
	...reports: IReportAcrossOwners[]
) =>
	aReportService({
		getAllReports: vi.fn().mockResolvedValue(everyReport(...reports)),
		getReport,
	});

const theReportTitlesPrinted = () =>
	within(theFramesPage())
		.getAllByRole("heading", { level: 1 })
		.map((heading) => heading.textContent);

describe("One PDF of several reports", () => {
	beforeEach(() => {
		theFontsHaveLoaded();
	});

	afterEach(() => {
		forgetTheBrowser();
	});

	// @walking_skeleton @driving_port @us-07 @slice-07 @kpi-OUT-6350-combined-pdf-used @contract-shape:bounded-change
	it.skip(`a coach exports the reports of two Teams as one PDF, each on a page of its own (${pending})`, async () => {
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads(),
				zenithsQuarter,
				gravitysMonth,
			),
		});
		await theReportsTable();

		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, GRAVITY);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await theFramesHavePrinted(frames, 1);
		expect(theReportTitlesPrinted()).toEqual([QUARTER, MONTH]);
	});

	// @us-07 @slice-07 @contract-shape:pure-function
	it.skip(`the reports print in the table's order, as the reader sorted it (${pending})`, async () => {
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads(),
				zenithsQuarter,
				gravitysMonth,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, GRAVITY);

		await user.click(screen.getByRole("columnheader", { name: /^Name/ }));
		await waitFor(() =>
			expect(theReportNamesListed()).toEqual([MONTH, QUARTER]),
		);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await theFramesHavePrinted(frames, 1);
		expect(theReportTitlesPrinted()).toEqual([MONTH, QUARTER]);
	});

	// @us-07 @slice-07 @edge @contract-shape:unbounded-preservation
	it.skip(`while the reports load, Export PDF shows it is preparing and nothing prints yet (${pending})`, async () => {
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				vi.fn(() => neverAnswered()),
				zenithsQuarter,
				gravitysMonth,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, GRAVITY);

		const exportSeveral = screen.getByRole("button", {
			name: exportButton(2),
		});
		await user.click(exportSeveral);

		await waitFor(() =>
			expect(within(exportSeveral).getByRole("progressbar")).toBeVisible(),
		);
		expect(exportSeveral).toHaveAttribute("aria-busy", "true");
		expect(frames.timesAFramePrinted()).toBe(0);
	});

	// @us-07 @slice-07 @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:pure-function
	it.skip(`a reader who is Admin of none of them may still export them (${pending})`, async () => {
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads(),
				zenithsQuarter,
				pulsarsQuarter,
			),
			permissions: { adminOf: [] },
		});
		await theReportsTable();

		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, QUARTER, PULSAR);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await theFramesHavePrinted(frames, 1);
	});

	// @us-07 @slice-07 @error @contract-shape:unbounded-preservation
	it.skip(`a report that cannot be loaded stops the PDF and is named (${pending})`, async () => {
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads([voyagersQuarter.id]),
				zenithsQuarter,
				voyagersQuarter,
			),
		});
		await theReportsTable();

		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, QUARTER, VOYAGER);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		const failure = await screen.findByRole("dialog", {
			name: NO_PDF_WAS_MADE,
		});
		expect(
			within(failure).getByText(couldNotLoadTheReportOf(VOYAGER, QUARTER), {
				exact: true,
			}),
		).toBeInTheDocument();
		expect(
			within(failure)
				.getAllByRole("button")
				.map((button) => button.textContent),
		).toEqual([CANCEL, LEAVE_IT_OUT, RETRY]);
		expect(frames.timesAFramePrinted()).toBe(0);
	});

	// @us-07 @slice-07 @error @contract-shape:bounded-change
	it.skip(`Leave it out unpicks the report that failed and prints the rest (${pending})`, async () => {
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads([voyagersQuarter.id]),
				zenithsQuarter,
				gravitysMonth,
				voyagersQuarter,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, GRAVITY);
		await selectTheRowOf(user, QUARTER, VOYAGER);
		await user.click(screen.getByRole("button", { name: exportButton(3) }));

		await user.click(
			within(
				await screen.findByRole("dialog", { name: NO_PDF_WAS_MADE }),
			).getByRole("button", { name: LEAVE_IT_OUT }),
		);

		await theFramesHavePrinted(frames, 1);
		expect(theReportTitlesPrinted()).toEqual([QUARTER, MONTH]);
		expect(
			screen.getByRole("button", { name: exportButton(2) }),
		).toBeInTheDocument();
	});

	// @us-07 @slice-07 @error @edge @contract-shape:pure-function
	it.skip(`Leave it out is not offered when leaving the failed reports out would leave nothing to print (${pending})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads([zenithsQuarter.id, voyagersQuarter.id]),
				zenithsQuarter,
				voyagersQuarter,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, QUARTER, VOYAGER);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		const failure = await screen.findByRole("dialog", {
			name: NO_PDF_WAS_MADE,
		});
		expect(
			within(failure)
				.getAllByRole("button")
				.map((button) => button.textContent),
		).toEqual([CANCEL, RETRY]);
	});

	// @us-07 @slice-07 @error @contract-shape:bounded-change
	it.skip(`Retry asks for the report again and prints once it arrives (${pending})`, async () => {
		const frames = aPrintFrameBrowser();
		const getReport = eachReportReads();
		getReport.mockRejectedValueOnce(new Error("the network dropped"));
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				getReport,
				voyagersQuarter,
				zenithsQuarter,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, VOYAGER);
		await selectTheRowOf(user, QUARTER, ZENITH);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await user.click(
			within(
				await screen.findByRole("dialog", { name: NO_PDF_WAS_MADE }),
			).getByRole("button", { name: RETRY }),
		);

		await theFramesHavePrinted(frames, 1);
	});

	// @us-07 @slice-07 @error @contract-shape:unbounded-preservation
	it.skip(`when part of the page cannot be loaded no PDF is made and it says so (${pending})`, async () => {
		theFontsCannotLoad("fail outright");
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads(),
				zenithsQuarter,
				gravitysMonth,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, GRAVITY);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		const failure = await screen.findByRole("dialog", {
			name: NO_PDF_WAS_MADE,
		});
		expect(
			within(failure).getByText(PART_OF_THE_PAGE_FAILED, { exact: true }),
		).toBeInTheDocument();
		expect(
			within(failure)
				.getAllByRole("button")
				.map((button) => button.textContent),
		).toEqual([CANCEL, RETRY]);
		expect(frames.timesAFramePrinted()).toBe(0);
	});

	// @us-07 @slice-07 @error @edge @contract-shape:pure-function
	it.skip(`above 50 picked, Export PDF is disabled and says at most 50 (${pending})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads(),
				...moreReportsThanTheCeiling(ZENITH).reports,
			),
		});
		await theReportsTable();

		await user.click(screen.getByRole("checkbox", { name: /^Select all/ }));

		const exportSeveral = screen.getByRole("button", {
			name: exportButton(51),
		});
		expect(exportSeveral).toBeDisabled();
		await user.hover(exportSeveral.parentElement ?? exportSeveral);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			new RegExp(`^${AT_MOST_50.replaceAll(".", "\\.")}$`),
		);
	});

	// @us-07 @slice-07 @edge @contract-shape:pure-function
	it.skip(`with nothing picked Export PDF is disabled and says why (${pending})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(eachReportReads(), zenithsQuarter),
		});
		await theReportsTable();

		const exportSeveral = screen.getByRole("button", {
			name: exportButton(0),
		});
		expect(exportSeveral).toBeDisabled();
		await user.hover(exportSeveral.parentElement ?? exportSeveral);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			new RegExp(`^${NOTHING_SELECTED_TO_EXPORT.replaceAll(".", "\\.")}$`),
		);
	});
});
