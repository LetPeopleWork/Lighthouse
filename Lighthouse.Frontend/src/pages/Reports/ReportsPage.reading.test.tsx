import { focusManager } from "@tanstack/react-query";
import { screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { actAndSettle } from "../../tests/actAndSettle";
import {
	APOLLO_PORTFOLIO,
	aReportOn,
	everyReport,
	FILTER,
	GRAVITY,
	MONTH,
	neverAnswered,
	OCEAN_EXPLORER,
	QUARTER,
	renderTheReportsPage,
	rowOf,
	theReportNamesListed,
	theReportsTable,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { deferred } from "../../tests/HeldMetricsService";
import { A_LATER_TYPE, aReportService } from "../../tests/ReportsTestKit";

/**
 * How the Reports page reads: busy while its list is on its way, the old list dimmed rather than dropped while it
 * is read again, a row's PDF icon exporting that row's report, the Owner column and its filter.
 */

const zenithsQuarter = aReportOn(ZENITH, QUARTER);
const gravitysMonth = aReportOn(GRAVITY, MONTH);

const theFilter = () => screen.getByRole("textbox", { name: FILTER });

describe("While the Reports page reads its list", () => {
	afterEach(() => focusManager.setFocused(undefined));

	it(`says it is busy while the list is on its way, and no longer once it is shown`, async () => {
		const answer = deferred<ReturnType<typeof everyReport>>();
		renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn(() => answer.promise),
			}),
		});

		const whileLoading = await screen.findByTestId("all-reports");
		expect(whileLoading).toHaveAttribute("aria-busy", "true");
		expect(whileLoading).toHaveAttribute("data-widget-status", "loading");

		answer.resolve(everyReport(zenithsQuarter));
		const shown = await theReportsTable();
		expect(shown).not.toHaveAttribute("aria-busy");
	});

	it(`keeps the list on screen, dimmed, while it is read again`, async () => {
		const readAgain = deferred<ReturnType<typeof everyReport>>();
		const getAllReports = vi
			.fn()
			.mockResolvedValueOnce(everyReport(zenithsQuarter))
			.mockReturnValue(readAgain.promise);
		renderTheReportsPage({
			reportService: aReportService({ getAllReports }),
		});
		await theReportsTable();

		await actAndSettle(() => {
			focusManager.setFocused(false);
			focusManager.setFocused(true);
		});

		await waitFor(() => expect(getAllReports).toHaveBeenCalledTimes(2));
		expect(screen.getByTestId("all-reports")).toHaveAttribute(
			"data-widget-status",
			"loading",
		);
		expect(rowOf(QUARTER, ZENITH)).toBeInTheDocument();
	});
});

describe("A row's PDF icon on the Reports page", () => {
	it(`starts the export of that row's report and holds every other row's PDF icon back while it is prepared`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValue(everyReport(zenithsQuarter, gravitysMonth)),
				getReport: vi.fn(() => neverAnswered()),
			}),
		});
		await theReportsTable();
		const exportOfZenith = () =>
			within(rowOf(QUARTER, ZENITH)).getByLabelText(`Export ${QUARTER} as PDF`);
		const exportOfGravity = () =>
			within(rowOf(MONTH, GRAVITY)).getByLabelText(`Export ${MONTH} as PDF`);
		const isHeldBack = (control: HTMLElement) =>
			control.hasAttribute("disabled") ||
			control.getAttribute("aria-disabled") === "true";
		expect(isHeldBack(exportOfGravity())).toBe(false);

		await user.click(exportOfZenith());

		await waitFor(() => expect(isHeldBack(exportOfGravity())).toBe(true));
	});
});

describe("The Owner column of the Reports page", () => {
	it(`has an Owner header, and sorts the rows by owner name when it is chosen`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValue(
						everyReport(
							aReportOn(ZENITH, "Zenith's"),
							aReportOn(OCEAN_EXPLORER, "Ocean Explorer's"),
							aReportOn(GRAVITY, "Gravity's"),
						),
					),
			}),
		});
		await theReportsTable();
		expect(theReportNamesListed()).toEqual([
			"Ocean Explorer's",
			"Gravity's",
			"Zenith's",
		]);

		await user.click(screen.getByRole("columnheader", { name: "Owner" }));

		await waitFor(() =>
			expect(theReportNamesListed()).toEqual([
				"Gravity's",
				"Ocean Explorer's",
				"Zenith's",
			]),
		);
	});

	it(`shows a Team's icon on a Team's row and a Portfolio's icon on a Portfolio's row`, async () => {
		renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValue(
						everyReport(zenithsQuarter, aReportOn(APOLLO_PORTFOLIO, MONTH)),
					),
			}),
		});
		await theReportsTable();

		const onTeam = within(rowOf(QUARTER, ZENITH));
		const onPortfolio = within(rowOf(MONTH, APOLLO_PORTFOLIO));
		expect(onTeam.getByTestId("GroupsIcon")).toBeInTheDocument();
		expect(onTeam.queryByTestId("FolderSpecialIcon")).toBeNull();
		expect(onPortfolio.getByTestId("FolderSpecialIcon")).toBeInTheDocument();
		expect(onPortfolio.queryByTestId("GroupsIcon")).toBeNull();
	});

	it(`filters whatever the case and the spaces around what was typed`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValue(everyReport(zenithsQuarter, gravitysMonth)),
			}),
		});
		await theReportsTable();

		await user.type(theFilter(), "  ZENITH ");

		await waitFor(() => expect(theReportNamesListed()).toEqual([QUARTER]));
	});

	it(`filters by the Type of a report, which a report of a Type this version cannot show does not have`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(
					everyReport(
						zenithsQuarter,
						aReportOn(GRAVITY, MONTH, {
							templateKey: A_LATER_TYPE,
							thenAndNow: undefined,
						}),
					),
				),
			}),
		});
		await theReportsTable();

		await user.type(theFilter(), "then & now");

		await waitFor(() => expect(theReportNamesListed()).toEqual([QUARTER]));
	});
});

describe("The order the Reports page lists its reports in", () => {
	it(`lists Portfolios first and then Teams, each alphabetically by name, whatever order the server sent`, async () => {
		renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi
					.fn()
					.mockResolvedValue(
						everyReport(
							aReportOn(ZENITH, "on Zenith"),
							aReportOn(OCEAN_EXPLORER, "on Ocean Explorer"),
							aReportOn(GRAVITY, "on Gravity"),
							aReportOn(APOLLO_PORTFOLIO, "on Apollo"),
						),
					),
			}),
		});

		await theReportsTable();

		expect(theReportNamesListed()).toEqual([
			"on Apollo",
			"on Ocean Explorer",
			"on Gravity",
			"on Zenith",
		]);
	});
});
