import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
	createMockTeamService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import TeamDetail from "./TeamDetail";

/**
 * The Reports tab on a Team page: where it sits, that anybody who may read the Team may open it, and that
 * an address naming one report opens that report under the tab. What the tab and the report show is the
 * business of their own components, which stand in here.
 */

const { terms } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canUpdateTeamData: true,
		updateTeamDataTooltip: "",
		canUpdateTeamSettings: true,
		updateTeamSettingsTooltip: "",
	}),
}));

vi.mock("./TeamFeaturesView", () => ({
	default: () => <div data-testid="team-features-view" />,
}));

vi.mock("./TeamForecastView", () => ({
	default: () => <div data-testid="team-forecast-view" />,
}));

vi.mock("./TeamMetricsView", () => ({
	default: () => <div data-testid="team-metrics-view" />,
}));

vi.mock("./Refinement/RefinementView", () => ({
	default: () => <div data-testid="team-refinement-view" />,
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

let mockParams: { id: string; tab?: string; reportId?: string } = {
	id: "1",
	tab: "forecasts",
};
const mockNavigate = vi.fn();

vi.mock("react-router", async () => ({
	...(await vi.importActual("react-router")),
	useParams: () => mockParams,
	useNavigate: () => mockNavigate,
}));

const defaultTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.TEAM]: "Team",
	[TERMINOLOGY_KEYS.FEATURES]: "Features",
	refinement: "Refinement",
};

const lightspeed = {
	id: 1,
	name: "Team Lightspeed",
	features: [{ id: 1, name: "Feature 1", key: "F-1" }],
	portfolios: [],
	tags: [],
	workItemTypes: [],
	lastUpdated: new Date(),
	serviceLevelExpectationProbability: 85,
	serviceLevelExpectationRange: 7,
	systemWIPLimit: 0,
	featureWip: 1,
	useFixedDatesForThroughput: false,
	throughputStartDate: new Date(),
	throughputEndDate: new Date(),
	refinementConfigured: true,
};

const renderTheTeamPage = (viewer: "teamAdmin" | "reader") => {
	const teamService = createMockTeamService();
	teamService.getTeam = vi.fn().mockResolvedValue(lightspeed);

	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		isRbacEnabled: true,
		isSystemAdmin: false,
		adminTeamIds: viewer === "teamAdmin" ? [1] : [],
		canCreateTeam: false,
		canCreatePortfolio: false,
	});

	const updateSubscriptionService = createMockUpdateSubscriptionService();
	updateSubscriptionService.subscribeToTeamUpdates = vi.fn();
	updateSubscriptionService.unsubscribeFromTeamUpdates = vi
		.fn()
		.mockResolvedValue(undefined);
	updateSubscriptionService.getUpdateStatus = vi.fn().mockResolvedValue(null);

	render(
		<BrowserRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					teamService,
					rbacService,
					updateSubscriptionService,
				})}
			>
				<TeamDetail />
			</ApiServiceContext.Provider>
		</BrowserRouter>,
	);
};

const theReportsTab = () => screen.findByRole("tab", { name: "Reports" });

describe("The Reports tab on a Team page", () => {
	beforeEach(() => {
		mockParams = { id: "1", tab: "forecasts" };
		mockNavigate.mockClear();
		terms.current = { ...defaultTerms };
	});

	it("sits between Refinement and Settings", async () => {
		renderTheTeamPage("teamAdmin");

		await theReportsTab();

		expect(
			screen.getAllByRole("tab").map((tab) => tab.textContent?.trim()),
		).toEqual([
			"Features",
			"Forecasts",
			"Metrics",
			"Refinement",
			"Reports",
			"Settings",
			"Access",
		]);
	});

	it("is open to a reader", async () => {
		renderTheTeamPage("reader");

		expect(await theReportsTab()).toBeEnabled();
	});

	it("opens the Team's reports and puts the tab in the address", async () => {
		renderTheTeamPage("reader");

		await userEvent.click(await theReportsTab());

		expect(await screen.findByTestId("reports-tab")).toHaveTextContent(
			"team 1",
		);
		expect(mockNavigate).toHaveBeenCalledWith("/teams/1/reports", {
			replace: true,
		});
	});

	it("opens straight onto the Team's reports from an address that names the tab", async () => {
		mockParams = { id: "1", tab: "reports" };
		renderTheTeamPage("reader");

		expect(await screen.findByTestId("reports-tab")).toBeVisible();
		expect(await theReportsTab()).toHaveAttribute("aria-selected", "true");
	});

	it("opens one report under the Reports tab from an address that names it", async () => {
		mockParams = { id: "1", tab: "reports", reportId: "42" };
		renderTheTeamPage("reader");

		expect(await screen.findByTestId("report-view")).toHaveTextContent(
			"team 1 report 42",
		);
		expect(screen.queryByTestId("reports-tab")).toBeNull();
		expect(await theReportsTab()).toHaveAttribute("aria-selected", "true");
	});
});
