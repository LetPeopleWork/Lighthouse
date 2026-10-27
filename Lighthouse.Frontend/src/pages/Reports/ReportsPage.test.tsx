import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import {
	aReportOn,
	DELETE,
	EMPTY_BEFORE_BULK_CREATION,
	EVERY_REPORT_COULD_NOT_LOAD,
	everyReport,
	FILTER,
	GRAVITY,
	MONTH,
	neverAnswered,
	OCEAN_EXPLORER,
	ownerCellOf,
	ownerLinkNameOf,
	PULSAR,
	QUARTER,
	REPORTS,
	RETRY,
	renderTheReportsPage,
	rowOf,
	theReportNamesListed,
	theReportsTable,
	whereTheReaderIs,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import {
	A_LATER_TYPE,
	aRefusal,
	aReportService,
	CANNOT_BE_REBUILT,
	defaultTerms,
	NO_LONGER_EXISTS,
	NOT_SUPPORTED_IN_THIS_VERSION,
	theEngagementStartReport,
} from "../../tests/ReportsTestKit";

/**
 * The Reports page lists every report on every Team and Portfolio the reader can read, in one table: its name,
 * Type, owner and the day it was created. A row opens the report where it always lived, on its owner, and carries
 * the same Edit, Delete and PDF icons as an owner's Reports tab. The server decides what the reader can read; the
 * page shows what it answers.
 */

const zenithsQuarter = aReportOn(ZENITH, QUARTER);
const gravitysQuarter = aReportOn(GRAVITY, QUARTER);
const oceanExplorersMonth = aReportOn(OCEAN_EXPLORER, MONTH);
const pulsarsQuarter = aReportOn(PULSAR, QUARTER);

const aReportServiceListing = (...reports: ReturnType<typeof aReportOn>[]) =>
	aReportService({
		getAllReports: vi.fn().mockResolvedValue(everyReport(...reports)),
	});

const theFilter = () => screen.getByRole("textbox", { name: FILTER });

describe("Every report in one place", () => {
	// @walking_skeleton @driving_port @us-01 @slice-01 @kpi-OUT-6350-every-report-found @contract-shape:pure-function
	it("a coach finds the reports of two Teams and a Portfolio on one page and opens one", async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportServiceListing(
				zenithsQuarter,
				gravitysQuarter,
				oceanExplorersMonth,
			),
		});

		await theReportsTable();
		expect(
			screen.getByRole("heading", { name: REPORTS, level: 1 }),
		).toBeInTheDocument();
		expect(
			within(rowOf(QUARTER, ZENITH)).getByText("Then & Now"),
		).toBeInTheDocument();
		expect(rowOf(QUARTER, GRAVITY)).toBeInTheDocument();
		expect(rowOf(MONTH, OCEAN_EXPLORER)).toBeInTheDocument();

		await user.click(
			within(rowOf(MONTH, OCEAN_EXPLORER)).getByRole("link", { name: MONTH }),
		);

		expect(whereTheReaderIs()).toBe(
			`/portfolios/${OCEAN_EXPLORER.id}/reports/${oceanExplorersMonth.id}`,
		);
	});

	// @driving_port @us-01 @slice-01 @contract-shape:pure-function
	it(`the owner cell shows the name alone, is announced with its kind, and opens that Team or Portfolio`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportServiceListing(zenithsQuarter, oceanExplorersMonth),
		});
		await theReportsTable();

		const zenith = within(rowOf(QUARTER, ZENITH)).getByRole("link", {
			name: ownerLinkNameOf(ZENITH),
		});
		expect(zenith).toHaveTextContent(new RegExp(`^${ownerCellOf(ZENITH)}$`));
		expect(
			within(rowOf(MONTH, OCEAN_EXPLORER)).getByRole("link", {
				name: "Ocean Explorer, Portfolio",
			}),
		).toBeInTheDocument();

		await user.click(zenith);

		expect(whereTheReaderIs()).toBe(`/teams/${ZENITH.id}`);
	});

	// @us-01 @slice-01 @edge @contract-shape:pure-function
	it(`the owner's kind is announced in the instance's own word`, async () => {
		renderTheReportsPage({
			reportService: aReportServiceListing(zenithsQuarter),
			terms: { ...defaultTerms, [TERMINOLOGY_KEYS.TEAM]: "Squad" },
		});
		await theReportsTable();

		expect(
			screen.getByRole("link", { name: "Zenith, Squad" }),
		).toBeInTheDocument();
	});

	// @driving_port @us-01 @slice-01 @contract-shape:pure-function
	it(`two reports of the same name on different owners are both listed and told apart by owner`, async () => {
		renderTheReportsPage({
			reportService: aReportServiceListing(zenithsQuarter, gravitysQuarter),
		});
		await theReportsTable();

		expect(theReportNamesListed()).toEqual([QUARTER, QUARTER]);
		expect(rowOf(QUARTER, ZENITH)).not.toBe(rowOf(QUARTER, GRAVITY));
	});

	// @us-01 @slice-01 @edge @contract-shape:pure-function
	it(`with no report anywhere it says where reports are created`, async () => {
		renderTheReportsPage({ reportService: aReportServiceListing() });

		expect(
			await screen.findByText(EMPTY_BEFORE_BULK_CREATION, { exact: true }),
		).toBeInTheDocument();
	});

	// @us-01 @slice-01 @edge @contract-shape:unbounded-preservation
	it(`while the reports are on their way it shows a loading look and no table`, async () => {
		renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn(() => neverAnswered()),
			}),
		});

		expect(await screen.findByRole("progressbar")).toBeInTheDocument();
		const table = screen.queryByTestId("all-reports");
		expect(table === null || table.dataset.widgetStatus === "loading").toBe(
			true,
		);
		expect(screen.queryAllByRole("row")).toEqual([]);
		expect(
			screen.queryByText(EMPTY_BEFORE_BULK_CREATION),
		).not.toBeInTheDocument();
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it(`when the reports cannot be loaded it says so, shows no empty table, and Retry loads them`, async () => {
		const getAllReports = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(everyReport(zenithsQuarter));
		const { user } = renderTheReportsPage({
			reportService: aReportService({ getAllReports }),
		});

		expect(
			await screen.findByText(EVERY_REPORT_COULD_NOT_LOAD, { exact: true }),
		).toBeInTheDocument();
		expect(
			screen.queryByText(EMPTY_BEFORE_BULK_CREATION),
		).not.toBeInTheDocument();

		await user.click(screen.getByRole("button", { name: RETRY }));

		await theReportsTable();
		expect(rowOf(QUARTER, ZENITH)).toBeInTheDocument();
	});

	// @us-01 @slice-01 @edge @contract-shape:pure-function
	it(`a report of a type this version cannot show is listed and says so`, async () => {
		renderTheReportsPage({
			reportService: aReportServiceListing(
				aReportOn(PULSAR, "Signals", {
					templateKey: A_LATER_TYPE,
					thenAndNow: undefined,
				}),
			),
		});
		await theReportsTable();

		expect(
			within(rowOf("Signals", PULSAR)).getByText(NOT_SUPPORTED_IN_THIS_VERSION),
		).toBeInTheDocument();
	});

	// @us-01 @slice-01 @edge @contract-shape:pure-function
	it(`a report whose Then can no longer be rebuilt is marked on its row`, async () => {
		renderTheReportsPage({
			reportService: aReportServiceListing(
				aReportOn(GRAVITY, QUARTER, {
					thenAndNow: { thenRebuildable: false },
				}),
				zenithsQuarter,
			),
		});
		await theReportsTable();

		expect(
			within(rowOf(QUARTER, GRAVITY)).getByLabelText(CANNOT_BE_REBUILT),
		).toBeInTheDocument();
		expect(
			within(rowOf(QUARTER, ZENITH)).queryByLabelText(CANNOT_BE_REBUILT),
		).not.toBeInTheDocument();
	});

	// @us-01 @slice-01 @contract-shape:pure-function
	it(`the filter narrows the rows by owner name`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportServiceListing(
				zenithsQuarter,
				gravitysQuarter,
				oceanExplorersMonth,
			),
		});
		await theReportsTable();

		await user.type(theFilter(), "Ocean");

		await waitFor(() => expect(theReportNamesListed()).toEqual([MONTH]));
	});

	// @us-01 @slice-01 @edge @contract-shape:pure-function
	it(`the filter still matches an owner's kind, though the cell shows only its name`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportServiceListing(zenithsQuarter, oceanExplorersMonth),
		});
		await theReportsTable();

		await user.type(theFilter(), "Portfolio");

		await waitFor(() => expect(theReportNamesListed()).toEqual([MONTH]));
	});
});

describe("The icons on each row", () => {
	// @driving_port @us-01 @slice-01 @error @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:pure-function
	it(`Edit and Delete show only on rows the reader is Admin of; PDF shows on every row`, async () => {
		renderTheReportsPage({
			reportService: aReportServiceListing(zenithsQuarter, pulsarsQuarter),
			permissions: { adminOf: [ZENITH] },
		});
		await theReportsTable();

		const onPulsar = within(rowOf(QUARTER, PULSAR));
		const onZenith = within(rowOf(QUARTER, ZENITH));
		expect(
			onZenith.getByRole("button", { name: `Edit ${QUARTER}` }),
		).toBeInTheDocument();
		expect(
			onZenith.getByRole("button", { name: `Delete ${QUARTER}` }),
		).toBeInTheDocument();
		expect(onPulsar.queryByRole("button", { name: /^Edit / })).toBeNull();
		expect(onPulsar.queryByRole("button", { name: /^Delete / })).toBeNull();
		expect(
			onPulsar.getByRole("button", { name: `Export ${QUARTER} as PDF` }),
		).toBeInTheDocument();
	});

	// @us-01 @slice-01 @edge @contract-shape:unbounded-preservation
	it(`while permissions are on their way no row offers Edit or Delete, and every row offers PDF`, async () => {
		renderTheReportsPage({
			reportService: aReportServiceListing(zenithsQuarter),
			permissions: "loading",
		});
		await theReportsTable();

		const onZenith = within(rowOf(QUARTER, ZENITH));
		expect(onZenith.queryByRole("button", { name: /^Edit / })).toBeNull();
		expect(onZenith.queryByRole("button", { name: /^Delete / })).toBeNull();
		expect(
			onZenith.getByRole("button", { name: `Export ${QUARTER} as PDF` }),
		).toBeInTheDocument();
	});

	// @driving_port @us-01 @slice-01 @contract-shape:bounded-change
	it(`Delete on a row asks the owner tab's confirmation, deletes that report alone and takes it off the list`, async () => {
		const getAllReports = vi
			.fn()
			.mockResolvedValueOnce(everyReport(zenithsQuarter, gravitysQuarter))
			.mockResolvedValue(everyReport(gravitysQuarter));
		const deleteReport = vi.fn().mockResolvedValue(undefined);
		const { user } = renderTheReportsPage({
			reportService: aReportService({ getAllReports, deleteReport }),
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
			expect(deleteReport).toHaveBeenCalledWith(
				{ kind: "team", id: ZENITH.id },
				zenithsQuarter.id,
			),
		);
		expect(deleteReport).toHaveBeenCalledTimes(1);
		await waitFor(() => expect(theReportNamesListed()).toEqual([QUARTER]));
		expect(rowOf(QUARTER, GRAVITY)).toBeInTheDocument();
	});
});

describe("Editing from a row", () => {
	// @driving_port @us-01 @slice-01 @contract-shape:bounded-change
	it(`a saved edit shows the report's new name on the list`, async () => {
		const renamed = "Since August";
		const read = theEngagementStartReport({
			id: zenithsQuarter.id,
			name: QUARTER,
			ownerId: ZENITH.id,
		});
		const getAllReports = vi
			.fn()
			.mockResolvedValueOnce(everyReport(zenithsQuarter))
			.mockResolvedValue(everyReport({ ...zenithsQuarter, name: renamed }));
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports,
				getReport: vi.fn().mockResolvedValue(read),
				editReport: vi.fn().mockResolvedValue({ ...read, name: renamed }),
			}),
		});
		await theReportsTable();

		await user.click(
			within(rowOf(QUARTER, ZENITH)).getByRole("button", {
				name: `Edit ${QUARTER}`,
			}),
		);
		const wizard = await screen.findByRole("dialog");
		await user.click(
			await within(wizard).findByRole("button", { name: "Next" }),
		);
		const name = await within(wizard).findByRole("textbox", { name: "Name" });
		await user.clear(name);
		await user.type(name, renamed);
		await user.click(within(wizard).getByRole("button", { name: "Save" }));

		await waitFor(() => expect(theReportNamesListed()).toEqual([renamed]));
	});

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it(`Edit on a report that no longer exists says so and takes its row off the list`, async () => {
		const getAllReports = vi
			.fn()
			.mockResolvedValueOnce(everyReport(zenithsQuarter, gravitysQuarter))
			.mockResolvedValue(everyReport(gravitysQuarter));
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports,
				getReport: vi.fn().mockRejectedValue(aRefusal(404, "not-found")),
			}),
		});
		await theReportsTable();

		await user.click(
			within(rowOf(QUARTER, ZENITH)).getByRole("button", {
				name: `Edit ${QUARTER}`,
			}),
		);

		expect(await screen.findByText(NO_LONGER_EXISTS)).toBeVisible();
		await waitFor(() => expect(theReportNamesListed()).toEqual([QUARTER]));
		expect(
			screen.queryByRole("link", { name: ownerLinkNameOf(ZENITH) }),
		).toBeNull();
	});
});
