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
	theFooter,
	theTermsAnswer,
	whatPrintAsksOf,
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

/** Whether the group, or something in it, asks on paper for the given height. */
const keepsRoomFor = (group: HTMLElement, height: string): boolean =>
	[group, ...Array.from(group.querySelectorAll<HTMLElement>("*"))].some(
		(element) => whatPrintAsksOf(element, "height") === height,
	);

const theElementsFixedOnPaper = (): HTMLElement[] =>
	Array.from(
		document.querySelectorAll<HTMLElement>("[data-render-ready] *"),
	).filter((element) => whatPrintAsksOf(element, "position") === "fixed");

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

	// A table's own header and footer groups repeat only on the sheets that table spans, so each report's line stays on
	// its own pages. The credit is the same for every report, so it is fixed to the page once and prints at the foot of
	// every sheet; each report keeps an empty footer group as tall as the credit, so no panel runs under it.
	it("gives each report its own running line and room for the credit, which prints once at the foot of every sheet", async () => {
		openThePrintPageOf("/print/reports?reports=portfolio-2-7,team-1-12");

		await waitFor(() =>
			expect(
				document.querySelector("[data-render-ready='true']"),
			).not.toBeNull(),
		);
		expect(screen.getAllByText(BRANDING)).toHaveLength(1);
		const credit = theFooter();
		expect(whatPrintAsksOf(credit, "position")).toBe("fixed");
		expect(whatPrintAsksOf(credit, "bottom")).toMatch(/^0(px)?$/);
		const creditHeight = whatPrintAsksOf(credit, "height");
		expect(creditHeight).toMatch(/^[1-9]\d*mm$/);

		const titles = screen.getAllByRole("heading", { level: 1 });
		const reports = elementsPrintedAs("table");
		const lines = elementsPrintedAs("table-header-group");
		const rooms = elementsPrintedAs("table-footer-group");
		expect(reports).toHaveLength(2);
		expect(lines).toHaveLength(2);
		expect(rooms).toHaveLength(2);
		for (const [index, name] of [PORTFOLIOS_REPORT, TEAMS_REPORT].entries()) {
			const [report, line, room] = [reports[index], lines[index], rooms[index]];
			expect(report.contains(titles[index])).toBe(true);
			expect(report.contains(line)).toBe(true);
			expect(report.contains(room)).toBe(true);
			expect(report.contains(credit)).toBe(false);
			expect(line.contains(titles[index])).toBe(false);
			const header = within(line).queryByText(name)?.closest("header");
			expect(
				header,
				`${name}'s running line is not in its own header group`,
			).toBeTruthy();
			expect(within(line).getByText(/^Produced /)).toBeInTheDocument();
			expect(whatPrintAsksOf(room, "break-inside")).toBe("avoid");
			expect(
				keepsRoomFor(room, creditHeight),
				`${name} keeps no room for the credit at the foot of its sheets`,
			).toBe(true);
		}
		expect(theElementsFixedOnPaper()).toEqual([credit]);
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
