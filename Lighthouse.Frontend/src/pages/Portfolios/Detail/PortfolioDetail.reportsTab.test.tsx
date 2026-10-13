import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockRbacService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import PortfolioDetail from "./PortfolioDetail";

/**
 * The Reports tab on a Portfolio page, after Metrics, open to anybody who may read the Portfolio, and the
 * address of one report opening that report under the tab. The tab's list and the report stand in here.
 */

vi.mock("./PortfolioFeatureList", () => ({
	default: () => <div data-testid="portfolio-feature-list" />,
}));

vi.mock("../../Common/Reports/ReportsTab", () => ({
	default: ({ owner }: { owner: { kind: string; id: number } }) => (
		<div data-testid="reports-tab">
			{owner.kind} {owner.id}
		</div>
	),
}));

vi.mock("../../Common/Reports/ReportView", () => ({
	default: ({
		owner,
		reportId,
	}: {
		owner: { kind: string; id: number };
		reportId: number;
	}) => (
		<div data-testid="report-view">
			{owner.kind} {owner.id} report {reportId}
		</div>
	),
}));

const APOLLO_ID = 3;

const renderThePortfolioPage = (
	address: string,
	viewer: "portfolioAdmin" | "reader",
) => {
	const apollo = new Portfolio();
	apollo.id = APOLLO_ID;
	apollo.name = "Project Apollo";
	apollo.features = [];

	const portfolioService = createMockPortfolioService();
	portfolioService.getPortfolio = vi.fn().mockResolvedValue(apollo);
	portfolioService.getPortfolioSettings = vi.fn().mockResolvedValue({
		id: APOLLO_ID,
		name: "Project Apollo",
		workItemTypes: [],
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Done"],
	});

	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		isRbacEnabled: true,
		isSystemAdmin: false,
		adminTeamIds: [],
		adminPortfolioIds: viewer === "portfolioAdmin" ? [APOLLO_ID] : [],
		canCreateTeam: false,
		canCreatePortfolio: false,
	});

	const updateSubscriptionService = createMockUpdateSubscriptionService();
	updateSubscriptionService.subscribeToFeatureUpdates = vi.fn();
	updateSubscriptionService.subscribeToForecastUpdates = vi.fn();
	updateSubscriptionService.getUpdateStatus = vi.fn().mockResolvedValue(null);

	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				portfolioService,
				rbacService,
				updateSubscriptionService,
			})}
		>
			<MemoryRouter initialEntries={[address]}>
				<Routes>
					<Route path="/portfolios/:id/:tab?" element={<PortfolioDetail />} />
					<Route
						path="/portfolios/:id/reports/:reportId"
						element={<PortfolioDetail />}
					/>
				</Routes>
			</MemoryRouter>
		</ApiServiceContext.Provider>,
	);
};

const theReportsTab = () => screen.findByRole("tab", { name: "Reports" });

describe("The Reports tab on a Portfolio page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	// @us-06 @slice-06 @driving_port @contract-shape:pure-function
	it.skip("sits between Metrics and Settings", async () => {
		renderThePortfolioPage(`/portfolios/${APOLLO_ID}`, "portfolioAdmin");

		await theReportsTab();
		const tabs = screen
			.getAllByRole("tab")
			.map((tab) => tab.textContent?.trim());

		expect(tabs.slice(tabs.indexOf("Metrics"))).toEqual([
			"Metrics",
			"Reports",
			"Settings",
			"Access",
		]);
	});

	// @us-06 @slice-06 @driving_port @contract-shape:pure-function
	it.skip("opens the Portfolio's reports for a reader", async () => {
		renderThePortfolioPage(`/portfolios/${APOLLO_ID}`, "reader");

		await userEvent.click(await theReportsTab());

		expect(await screen.findByTestId("reports-tab")).toHaveTextContent(
			`portfolio ${APOLLO_ID}`,
		);
	});

	// @us-06 @slice-06 @contract-shape:pure-function
	it.skip("opens one report under the Reports tab from an address that names it", async () => {
		renderThePortfolioPage(`/portfolios/${APOLLO_ID}/reports/42`, "reader");

		expect(await screen.findByTestId("report-view")).toHaveTextContent(
			`portfolio ${APOLLO_ID} report 42`,
		);
		expect(await theReportsTab()).toHaveAttribute("aria-selected", "true");
	});
});
