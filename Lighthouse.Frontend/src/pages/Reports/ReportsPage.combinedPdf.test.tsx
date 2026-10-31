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
	QUARTER,
	RETRY,
	renderTheReportsPage,
	rowOf,
	selectTheRowOf,
	theReportNamesListed,
	theReportsTable,
	VOYAGER,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import {
	aMomentLater,
	aPrintFrameBrowser,
	everyPrintFrameIsGone,
	forgetTheBrowser,
	theFontsCannotLoad,
	theFontsHaveLoaded,
	theFramesHavePrinted,
	theFramesPage,
	thePrintFrames,
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

const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 901 });
const gravitysMonth = aReportOn(GRAVITY, MONTH, { id: 902 });
const voyagersQuarter = aReportOn(VOYAGER, QUARTER, { id: 903 });
const pulsarsQuarter = aReportOn(PULSAR, QUARTER, { id: 904 });
const zenithsMonth = aReportOn(ZENITH, MONTH, { id: 905 });
const everyListedReport = [
	zenithsQuarter,
	gravitysMonth,
	voyagersQuarter,
	pulsarsQuarter,
	zenithsMonth,
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

/** Every report read as eachReportReads would, but held back until `release` is called. */
const readsHeldBack = () => {
	let release: () => void = () => undefined;
	const released = new Promise<void>((resolve) => {
		release = resolve;
	});
	const answers = eachReportReads();
	const getReport = vi.fn(async (owner: IReportOwner, reportId: number) => {
		await released;
		return answers(owner, reportId);
	});
	return { getReport, release: () => release() };
};

const theRowsPdfIcon = (reportName: string, owner: typeof ZENITH) =>
	within(rowOf(reportName, owner)).getByRole("button", {
		name: `Export ${reportName} as PDF`,
	});

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

	it("a coach exports the reports of two Teams as one PDF, each on a page of its own", async () => {
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
		// The table lists Teams by name, so Gravity's report comes first, whichever was picked first.
		expect(theReportTitlesPrinted()).toEqual([MONTH, QUARTER]);
	});

	it("once the print dialog closes the frame goes, Export PDF is ready again, and nothing says a PDF failed", async () => {
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

		frames.closeTheDialog();

		await everyPrintFrameIsGone();
		expect(
			screen.getByRole("button", { name: exportButton(2) }),
		).not.toHaveAttribute("aria-busy");
		expect(
			screen.queryByRole("dialog", { name: NO_PDF_WAS_MADE }),
		).not.toBeInTheDocument();
	});

	it("the reports print in the table's order, as the reader sorted it", async () => {
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads(),
				gravitysMonth,
				zenithsQuarter,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, MONTH, GRAVITY);
		await selectTheRowOf(user, QUARTER, ZENITH);

		// Gravity's report is listed first, and the table would show it first too, so only sorting Z to A puts Zenith's first.
		const byName = screen.getByRole("columnheader", { name: /^Name/ });
		await user.click(byName);
		await user.click(byName);
		await waitFor(() =>
			expect(theReportNamesListed()).toEqual([QUARTER, MONTH]),
		);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await theFramesHavePrinted(frames, 1);
		expect(theReportTitlesPrinted()).toEqual([QUARTER, MONTH]);
	});

	it("nothing prints while one of the reports is still on its way, and the PDF holds both once it arrives", async () => {
		const frames = aPrintFrameBrowser();
		let answerGravity: () => void = () => undefined;
		const gravityAnswers = new Promise<void>((resolve) => {
			answerGravity = resolve;
		});
		const answers = eachReportReads();
		const getReport = vi.fn(async (owner: IReportOwner, reportId: number) => {
			if (reportId === gravitysMonth.id) await gravityAnswers;
			return answers(owner, reportId);
		});
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				getReport,
				zenithsQuarter,
				gravitysMonth,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, GRAVITY);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await waitFor(() => expect(theReportTitlesPrinted()).toEqual([QUARTER]));
		await aMomentLater();
		expect(frames.timesAFramePrinted()).toBe(0);

		answerGravity();

		await theFramesHavePrinted(frames, 1);
		expect(theReportTitlesPrinted()).toEqual([MONTH, QUARTER]);
		await aMomentLater();
		expect(frames.timesAFramePrinted()).toBe(1);
	});

	it("picks and sorting changed while the PDF is prepared leave it as it was when Export PDF was pressed", async () => {
		const frames = aPrintFrameBrowser();
		const reads = readsHeldBack();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				reads.getReport,
				zenithsQuarter,
				gravitysMonth,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, GRAVITY);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await selectTheRowOf(user, QUARTER, ZENITH);
		const byName = screen.getByRole("columnheader", { name: /^Name/ });
		await user.click(byName);
		await user.click(byName);
		await waitFor(() =>
			expect(theReportNamesListed()).toEqual([QUARTER, MONTH]),
		);
		expect(
			screen.getByRole("button", { name: exportButton(1) }),
		).toBeInTheDocument();
		reads.release();

		await theFramesHavePrinted(frames, 1);
		expect(theReportTitlesPrinted()).toEqual([MONTH, QUARTER]);
	});

	it("while a PDF of several is prepared, a row's PDF icon is held back and starts no second", async () => {
		const frames = aPrintFrameBrowser();
		const getReport = vi.fn(() => neverAnswered());
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				getReport,
				zenithsQuarter,
				gravitysMonth,
				voyagersQuarter,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, GRAVITY);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));
		await waitFor(() => expect(thePrintFrames()).toHaveLength(1));

		const voyagersIcon = theRowsPdfIcon(QUARTER, VOYAGER);
		await user.click(voyagersIcon);
		await aMomentLater();

		expect(thePrintFrames()).toHaveLength(1);
		expect(getReport).toHaveBeenCalledTimes(2);
		expect(frames.timesAFramePrinted()).toBe(0);
		expect(voyagersIcon).toHaveAttribute("aria-disabled", "true");
	});

	it("while a row's PDF is prepared, Export PDF is held back and starts no second", async () => {
		const frames = aPrintFrameBrowser();
		const getReport = vi.fn(() => neverAnswered());
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				getReport,
				zenithsQuarter,
				gravitysMonth,
				voyagersQuarter,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, GRAVITY);
		await user.click(theRowsPdfIcon(QUARTER, VOYAGER));
		await waitFor(() => expect(thePrintFrames()).toHaveLength(1));

		const exportSeveral = screen.getByRole("button", {
			name: exportButton(2),
		});
		expect(exportSeveral).toHaveAttribute("aria-busy", "true");
		await user.click(exportSeveral);
		await aMomentLater();

		expect(thePrintFrames()).toHaveLength(1);
		expect(getReport).toHaveBeenCalledTimes(1);
		expect(frames.timesAFramePrinted()).toBe(0);
	});

	it("while the reports load, Export PDF shows it is preparing and nothing prints yet", async () => {
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

	it("a reader who is Admin of none of them may still export them", async () => {
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

	it("a report that cannot be loaded stops the PDF and is named", async () => {
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
		expect(failure).toHaveAccessibleDescription(
			couldNotLoadTheReportOf(VOYAGER, QUARTER),
		);
		expect(
			within(failure)
				.getAllByRole("button")
				.map((button) => button.textContent),
		).toEqual([CANCEL, LEAVE_IT_OUT, RETRY]);
		expect(frames.timesAFramePrinted()).toBe(0);
	});

	it("Leave it out unpicks the report that failed and prints the rest", async () => {
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
		// The PDF follows the table, which lists Teams by name: Gravity's report before Zenith's.
		expect(theReportTitlesPrinted()).toEqual([MONTH, QUARTER]);
		expect(
			screen.getByRole("button", { name: exportButton(2) }),
		).toBeInTheDocument();
	});

	it("Leave it out unpicks every report that failed", async () => {
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads([zenithsQuarter.id, voyagersQuarter.id]),
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
		expect(
			screen.getByRole("button", { name: exportButton(1) }),
		).toBeInTheDocument();
	});

	it("Leave it out is not offered when leaving the failed reports out would leave nothing to print", async () => {
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

	it("Leave it out leaves out only the report that failed, not the other report of the same Team", async () => {
		const frames = aPrintFrameBrowser();
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads([zenithsMonth.id]),
				zenithsQuarter,
				zenithsMonth,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, MONTH, ZENITH);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await user.click(
			within(
				await screen.findByRole("dialog", { name: NO_PDF_WAS_MADE }),
			).getByRole("button", { name: LEAVE_IT_OUT }),
		);

		await theFramesHavePrinted(frames, 1);
		expect(theReportTitlesPrinted()).toEqual([QUARTER]);
	});

	it("Cancel closes the word that no PDF was made, prints nothing and keeps what was picked", async () => {
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

		await user.click(
			within(
				await screen.findByRole("dialog", { name: NO_PDF_WAS_MADE }),
			).getByRole("button", { name: CANCEL }),
		);

		await waitFor(() =>
			expect(
				screen.queryByRole("dialog", { name: NO_PDF_WAS_MADE }),
			).not.toBeInTheDocument(),
		);
		expect(frames.timesAFramePrinted()).toBe(0);
		expect(
			screen.getByRole("button", { name: exportButton(2) }),
		).toBeInTheDocument();
	});

	it("Retry asks for the report again and prints once it arrives", async () => {
		const frames = aPrintFrameBrowser();
		const getReport = eachReportReads();
		getReport.mockRejectedValueOnce(new Error("the network dropped"));
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				getReport,
				gravitysMonth,
				zenithsQuarter,
			),
		});
		await theReportsTable();
		await selectTheRowOf(user, MONTH, GRAVITY);
		await selectTheRowOf(user, QUARTER, ZENITH);
		await user.click(screen.getByRole("button", { name: exportButton(2) }));

		await user.click(
			within(
				await screen.findByRole("dialog", { name: NO_PDF_WAS_MADE }),
			).getByRole("button", { name: RETRY }),
		);

		await theFramesHavePrinted(frames, 1);
		expect(theReportTitlesPrinted()).toEqual([MONTH, QUARTER]);
	});

	it("when part of the page cannot be loaded no PDF is made and it says so", async () => {
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
		expect(failure).toHaveAccessibleDescription(PART_OF_THE_PAGE_FAILED);
		expect(
			within(failure)
				.getAllByRole("button")
				.map((button) => button.textContent),
		).toEqual([CANCEL, RETRY]);
		expect(frames.timesAFramePrinted()).toBe(0);
	});

	it("above 50 picked, Export PDF is disabled and says at most 50", async () => {
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

	it("exactly 50 picked may still be exported as one PDF", async () => {
		const { user } = renderTheReportsPage({
			reportService: listingAndReading(
				eachReportReads(),
				...moreReportsThanTheCeiling(ZENITH, 0).reports,
			),
		});
		await theReportsTable();

		await user.click(screen.getByRole("checkbox", { name: /^Select all/ }));

		expect(
			screen.getByRole("button", { name: exportButton(50) }),
		).toBeEnabled();
	});

	it("with nothing picked Export PDF is disabled and says why", async () => {
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
