import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen } from "@testing-library/react";
import type React from "react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { IReportListItem } from "../../../models/Reports/Report";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import ReportList from "./ReportList";

/** The list a Reports tab or the Reports page shows, seen without any of the icons that change or print a report. */

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const aReport: IReportListItem = {
	id: 7,
	name: "Since August",
	templateKey: "then-and-now",
	ownerKind: "team",
	ownerId: 1,
	createdOn: "2026-10-02T09:00:00Z",
	thenAndNow: { thenRebuildable: true },
};

const renderTheList = (props: React.ComponentProps<typeof ReportList>) =>
	render(
		<QueryClientProvider client={new QueryClient()}>
			<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
				<MemoryRouter>
					<ReportList {...props} />
				</MemoryRouter>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);

describe("A report list nobody picks rows on", () => {
	it("still narrows to the reports the filter matches", () => {
		const zenithsReport = { ...aReport, ownerName: "Zenith" };
		const gravitysReport = {
			...aReport,
			id: 8,
			name: "Since July",
			ownerId: 2,
			ownerName: "Gravity",
		};
		renderTheList({
			reports: [zenithsReport, gravitysReport],
			showOwner: true,
		});

		fireEvent.change(
			screen.getByRole("textbox", { name: "Filter: name / owner / type" }),
			{ target: { value: "Gravity" } },
		);

		expect(
			screen.getByRole("link", { name: "Since July" }),
		).toBeInTheDocument();
		expect(screen.queryByRole("link", { name: "Since August" })).toBeNull();
	});
});

describe("A report list given no actions", () => {
	it("lists the reports with their Type and day, and offers no Actions column and no icon", () => {
		render(
			<QueryClientProvider client={new QueryClient()}>
				<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
					<MemoryRouter>
						<ReportList owner={{ kind: "team", id: 1 }} reports={[aReport]} />
					</MemoryRouter>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);

		expect(
			screen.getByRole("link", { name: "Since August" }),
		).toBeInTheDocument();
		expect(screen.getByRole("columnheader", { name: "Type" })).toBeVisible();
		expect(screen.queryByRole("columnheader", { name: "Actions" })).toBeNull();
		expect(
			screen.queryByRole("button", { name: /PDF|Edit|Delete/ }),
		).toBeNull();
	});
});
