import { screen } from "@testing-library/react";
import { Suspense } from "react";
import { MemoryRouter, Routes } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../../services/TerminologyContext";
import { createMockApiServiceContext } from "../../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../../tests/renderWithQueryClient";
import { reportPrintRoutes } from "./reportPrintRoutes";

/**
 * What the print page is handed, as it is handed it at runtime. A type that names only the reads is erased when the
 * code runs, so the whole report service, with its create, edit and delete, could still reach the page; the routes
 * hand it new objects holding the three reads and the three things it needs about the instance's terms, and
 * nothing else.
 */

const { handed } = vi.hoisted(() => ({
	handed: [] as Record<string, unknown>[],
}));

vi.mock("./ReportPrintPage", () => {
	const ReportPrintPage = (props: Record<string, unknown>) => {
		handed.push(props);
		return <p>the print page</p>;
	};
	return { default: ReportPrintPage, ReportPrintPage };
});

/** The one prop the page was handed that holds the named member. */
const theInputHolding = (member: string): object | undefined =>
	handed
		.flatMap((props) => Object.values(props))
		.find(
			(value): value is object =>
				typeof value === "object" && value !== null && member in value,
		);

describe("what the print routes hand the print page", () => {
	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it("is the report read and the owner's name, and the instance's terms, each holding exactly what the page reads", async () => {
		renderWithQueryClient(
			<MemoryRouter initialEntries={["/print/teams/1/reports/42"]}>
				<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
					<TerminologyProvider>
						<Suspense fallback={null}>
							<Routes>{reportPrintRoutes()}</Routes>
						</Suspense>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>,
		);
		await screen.findByText("the print page");

		expect(Object.keys(theInputHolding("getReport") ?? {}).sort()).toEqual([
			"getPortfolio",
			"getReport",
			"getTeam",
		]);
		expect(Object.keys(theInputHolding("getTerm") ?? {}).sort()).toEqual([
			"error",
			"getTerm",
			"isLoading",
		]);
	});
});
