import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import { pendingEpic6350 } from "../../../tests/BulkReportsTestKit";
import {
	createMockApiServiceContext,
	createMockRbacService,
	createMockTerminologyService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import Header from "./Header";

vi.mock("../../../hooks/useUpdateAll", () => ({
	useUpdateAll: () => ({
		handleUpdateAll: vi.fn(),
		globalUpdateStatus: { hasActiveUpdates: false, activeCount: 0 },
		isLoading: false,
		hasError: false,
	}),
}));

/** The way in to every report: a Reports entry in the header, for every signed-in reader whatever their role. */

const pending = pendingEpic6350("01");

const WhereTheReaderIs = () => (
	<output aria-label="where the reader is" data-path={useLocation().pathname} />
);

const renderTheHeader = (rbacService = createMockRbacService()) => {
	const terminologyService = createMockTerminologyService();
	terminologyService.getAllTerminology = vi.fn().mockResolvedValue([]);

	return render(
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					rbacService,
					terminologyService,
					updateSubscriptionService: createMockUpdateSubscriptionService(),
				})}
			>
				<TerminologyProvider>
					<MemoryRouter initialEntries={["/"]}>
						<Header />
						<Routes>
							<Route path="*" element={<WhereTheReaderIs />} />
						</Routes>
					</MemoryRouter>
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

const theNavigationLabels = () =>
	within(screen.getByTestId("main-navigation"))
		.getAllByRole("link")
		.map((link) => link.textContent);

describe("Header — the way in to every report", () => {
	// @driving_port @us-01 @slice-01 @contract-shape:bounded-change
	it.skip(`Reports sits between Features and System Settings (${pending})`, async () => {
		renderTheHeader();

		await waitFor(() =>
			expect(theNavigationLabels()).toEqual([
				"Overview",
				"Features",
				"Reports",
				"System Settings",
			]),
		);
	});

	// @driving_port @us-01 @slice-01 @contract-shape:bounded-change
	it.skip(`Reports opens the Reports page (${pending})`, async () => {
		renderTheHeader();
		const user = userEvent.setup();

		await user.click(
			within(screen.getByTestId("main-navigation")).getByRole("link", {
				name: "Reports",
			}),
		);

		expect(screen.getByLabelText("where the reader is").dataset.path).toBe(
			"/reports",
		);
	});

	// @us-01 @slice-01 @edge @contract-shape:pure-function
	it.skip(`a reader who is Admin of nothing still finds Reports (${pending})`, async () => {
		const rbacService = createMockRbacService();
		rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			adminTeamIds: [],
			adminPortfolioIds: [],
		});
		renderTheHeader(rbacService);

		await waitFor(() => expect(theNavigationLabels()).toContain("Reports"));
	});
});
