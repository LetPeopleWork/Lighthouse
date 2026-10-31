import { screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Routes } from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { IReportOwner } from "../../../../models/Reports/Report";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../../services/TerminologyContext";
import { createMockApiServiceContext } from "../../../../tests/MockApiServiceProvider";
import {
	aPortfolioServiceAnswering,
	apollo,
	aTeamServiceAnswering,
	aTerminologyServiceAnswering,
	BRANDING,
	elementsPrintedAs,
	lightspeed,
	thePrintOnlyDeclarations,
	theTermsAnswer,
} from "../../../../tests/ReportPrintTestKit";
import {
	aReportService,
	theEngagementStartReport,
} from "../../../../tests/ReportsTestKit";
import { renderWithQueryClient } from "../../../../tests/renderWithQueryClient";
import { printedReportsOf } from "./printedReportsQuery";
import { reportPrintRoutes } from "./reportPrintRoutes";

/**
 * Several reports as one PDF prints them, opened by its address so the printed look can be checked in a real
 * browser: every report named, in the order named, and never the print dialog.
 */

const TEAMS_REPORT = "Team quarter";
const NOTHING_NAMED = "No reports were named to print.";
const PORTFOLIOS_REPORT = "Portfolio quarter";

const openThePrintPageOf = (address: string) => {
	const print = vi.fn();
	vi.stubGlobal("print", print);
	const getReport = vi.fn((owner: IReportOwner, reportId: number) =>
		Promise.resolve(
			theEngagementStartReport({
				id: reportId,
				name: owner.kind === "team" ? TEAMS_REPORT : PORTFOLIOS_REPORT,
			}),
		),
	);
	const services = createMockApiServiceContext({
		reportService: aReportService({ getReport }),
		teamService: aTeamServiceAnswering(Promise.resolve(lightspeed())),
		portfolioService: aPortfolioServiceAnswering(Promise.resolve(apollo())),
		terminologyService: aTerminologyServiceAnswering(
			Promise.resolve(theTermsAnswer()),
		),
	});
	renderWithQueryClient(
		<MemoryRouter initialEntries={[address]}>
			<ApiServiceContext.Provider value={services}>
				<TerminologyProvider>
					<Routes>
						{reportPrintRoutes({
							loadFonts: () => Promise.resolve("loaded"),
							loadPicture: () => Promise.resolve("loaded"),
						})}
					</Routes>
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);
	return { print, getReport };
};

const theTitlesShown = () =>
	screen
		.getAllByRole("heading", { level: 1 })
		.map((heading) => heading.textContent);

describe("the print page of several reports", () => {
	it("draws every named report, in the order named, under one title, and never prints", async () => {
		const { print, getReport } = openThePrintPageOf(
			"/print/reports?reports=portfolio-2-7,team-1-12",
		);

		await waitFor(() =>
			expect(
				document.querySelector("[data-render-ready='true']"),
			).not.toBeNull(),
		);
		expect(theTitlesShown()).toEqual([PORTFOLIOS_REPORT, TEAMS_REPORT]);
		expect(getReport).toHaveBeenCalledWith({ kind: "portfolio", id: 2 }, 7);
		expect(getReport).toHaveBeenCalledWith({ kind: "team", id: 1 }, 12);
		expect(document.title).toBe("Lighthouse reports");
		expect(print).not.toHaveBeenCalled();
		vi.unstubAllGlobals();
	});

	// Something fixed to the page prints on every sheet of the document, so with several reports every report's line
	// would stack on every sheet. A table's own header and footer groups repeat only on the sheets that table spans.
	it("gives each report its own running line and footer, repeated only on that report's pages", async () => {
		openThePrintPageOf("/print/reports?reports=portfolio-2-7,team-1-12");

		await waitFor(() =>
			expect(
				document.querySelector("[data-render-ready='true']"),
			).not.toBeNull(),
		);
		const titles = screen.getAllByRole("heading", { level: 1 });
		const reports = elementsPrintedAs("table");
		const lines = elementsPrintedAs("table-header-group");
		const footers = elementsPrintedAs("table-footer-group");
		expect(reports).toHaveLength(2);
		expect(lines).toHaveLength(2);
		expect(footers).toHaveLength(2);
		for (const [index, name] of [PORTFOLIOS_REPORT, TEAMS_REPORT].entries()) {
			const [report, line, footer] = [
				reports[index],
				lines[index],
				footers[index],
			];
			expect(report.contains(titles[index])).toBe(true);
			expect(report.contains(line)).toBe(true);
			expect(report.contains(footer)).toBe(true);
			expect(line.contains(titles[index])).toBe(false);
			const header = within(line).queryByText(name)?.closest("header");
			expect(
				header,
				`${name}'s running line is not in its own header group`,
			).toBeTruthy();
			expect(within(line).getByText(/^Produced /)).toBeInTheDocument();
			const credit = within(footer).queryByText(BRANDING)?.closest("footer");
			expect(
				credit,
				`${name}'s credit is not in its own footer group`,
			).toBeTruthy();
		}
		expect(thePrintOnlyDeclarations()).not.toMatch(/position:\s*fixed/);
		vi.unstubAllGlobals();
	});

	it.each([
		"/print/reports",
		"/print/reports?reports=",
		"/print/reports?reports=epic-1-2,team-x-1",
	])("says so when %s names no report it can read", async (address) => {
		const { print, getReport } = openThePrintPageOf(address);

		expect(
			await screen.findByText(NOTHING_NAMED, { exact: true }),
		).toBeVisible();
		expect(getReport).not.toHaveBeenCalled();
		expect(print).not.toHaveBeenCalled();
		vi.unstubAllGlobals();
	});

	it("draws a report named twice once", async () => {
		openThePrintPageOf(
			"/print/reports?reports=team-1-12,portfolio-2-7,team-1-12",
		);

		await waitFor(() =>
			expect(
				document.querySelector("[data-render-ready='true']"),
			).not.toBeNull(),
		);
		expect(theTitlesShown()).toEqual([TEAMS_REPORT, PORTFOLIOS_REPORT]);
		vi.unstubAllGlobals();
	});
});

describe("the reports a print address names", () => {
	it("reads each as owner kind, owner id and report id, in order, leaving out what it cannot read", () => {
		expect(
			printedReportsOf("team-3-12, portfolio-5-7,epic-1-2,team-x-1,,team-4"),
		).toEqual([
			{ owner: { kind: "team", id: 3 }, reportId: 12 },
			{ owner: { kind: "portfolio", id: 5 }, reportId: 7 },
		]);
	});

	it("names a report given twice once, where it was first named", () => {
		expect(
			printedReportsOf("team-3-12,portfolio-3-12,team-3-12,team-4-12"),
		).toEqual([
			{ owner: { kind: "team", id: 3 }, reportId: 12 },
			{ owner: { kind: "portfolio", id: 3 }, reportId: 12 },
			{ owner: { kind: "team", id: 4 }, reportId: 12 },
		]);
	});

	it("names none when the address names nothing", () => {
		expect(printedReportsOf(null)).toEqual([]);
	});
});
