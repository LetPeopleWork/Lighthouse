import { screen, waitFor } from "@testing-library/react";
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
	lightspeed,
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

	it("names none when the address names nothing", () => {
		expect(printedReportsOf(null)).toEqual([]);
	});
});
