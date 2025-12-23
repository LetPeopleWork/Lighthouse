import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockTeamService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import TeamDetail from "./TeamDetail";

// Mock components
vi.mock("./TeamFeaturesView", () => ({
	default: () => <div data-testid="team-features-view">Team Features View</div>,
}));

vi.mock("./TeamForecastView", () => ({
	default: () => <div data-testid="team-forecast-view">Team Forecast View</div>,
}));

vi.mock("./TeamMetricsView", () => ({
	default: () => <div data-testid="team-metrics-view">Team Metrics View</div>,
}));

// Mock the useTerminology hook
vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.TEAM]: "Team",
				[TERMINOLOGY_KEYS.FEATURES]: "Features",
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

// Mock license restrictions hook
vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canUpdateTeamData: true,
		updateTeamDataTooltip: "",
		canUpdateTeamSettings: true,
		updateTeamSettingsTooltip: "",
	}),
}));

const mockTeam = {
	id: 1,
	name: "Test Team",
	involvedProjectIds: [],
	features: [],
	workItemTypes: [],
	lastUpdated: new Date().toISOString(),
	throughputStartDate: new Date("2024-01-01"),
	throughputEndDate: new Date("2024-01-31"),
	serviceLevelExpectationProbability: 85,
	serviceLevelExpectationRange: 10,
	systemWIPLimit: 0,
	featureWip: 1,
	useFixedDatesForThroughput: false,
};

// Mock react-router-dom params
let mockParams: { id: string; tab?: string } = { id: "1", tab: "features" };
const mockNavigate = vi.fn();

vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
	return {
		...actual,
		useParams: () => mockParams,
		useNavigate: () => mockNavigate,
	};
});

const renderTeamDetail = () => {
	const mockTeamService = createMockTeamService();
	const mockUpdateSubscriptionService = createMockUpdateSubscriptionService();

	// Configure specific mock behavior for the tests
	mockTeamService.getTeam = vi.fn().mockResolvedValue(mockTeam);
	mockTeamService.getTeamSettings = vi.fn().mockResolvedValue({
		id: 1,
		name: "Test Team",
		featureWIP: 1,
		automaticallyAdjustFeatureWIP: false,
		throughputHistory: 30,
		useFixedDatesForThroughput: false,
		throughputHistoryStartDate: new Date("2024-01-01"),
		throughputHistoryEndDate: new Date("2024-01-31"),
		serviceLevelExpectationProbability: 85,
		serviceLevelExpectationRange: 10,
		systemWIPLimit: 0,
	});
	mockTeamService.updateTeam = vi.fn().mockResolvedValue(undefined);
	mockTeamService.updateTeamData = vi.fn();
	mockUpdateSubscriptionService.subscribeToTeamUpdates = vi.fn();
	mockUpdateSubscriptionService.unsubscribeFromTeamUpdates = vi.fn();
	mockUpdateSubscriptionService.getUpdateStatus = vi
		.fn()
		.mockResolvedValue(null);

	const mockApiContext = createMockApiServiceContext({
		teamService: mockTeamService,
		updateSubscriptionService: mockUpdateSubscriptionService,
	});

	return render(
		<BrowserRouter>
			<ApiServiceContext.Provider value={mockApiContext}>
				<TeamDetail />
			</ApiServiceContext.Provider>
		</BrowserRouter>,
	);
};

describe("TeamDetail component", () => {
	beforeEach(() => {
		mockParams = { id: "1", tab: "features" };
		mockNavigate.mockClear();
	});

	it("should render four tabs: Features, Forecasts, Metrics, and Settings", async () => {
		renderTeamDetail();

		await waitFor(() => {
			expect(screen.getByTestId("team-features-view")).toBeInTheDocument();
		});

		// Check that all four tabs are present
		expect(screen.getByRole("tab", { name: "Features" })).toBeInTheDocument();
		expect(screen.getByRole("tab", { name: "Forecasts" })).toBeInTheDocument();
		expect(screen.getByRole("tab", { name: "Metrics" })).toBeInTheDocument();
		expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
	});

	it("should show Features tab as active by default when no tab specified", async () => {
		mockParams = { id: "1" }; // Reset to default without tab
		renderTeamDetail();

		await waitFor(() => {
			expect(screen.getByTestId("team-features-view")).toBeInTheDocument();
		});

		expect(screen.getByRole("tab", { name: "Features" })).toHaveAttribute(
			"aria-selected",
			"true",
		);
	});

	it("should show Features tab content when Features tab is active", async () => {
		mockParams.tab = "features";
		renderTeamDetail();

		await waitFor(() => {
			expect(screen.getByTestId("team-features-view")).toBeInTheDocument();
		});

		expect(screen.queryByTestId("team-forecast-view")).not.toBeInTheDocument();
		expect(screen.queryByTestId("team-metrics-view")).not.toBeInTheDocument();
	});

	it("should show Forecasts tab content when forecasts tab is active", async () => {
		mockParams.tab = "forecasts";
		renderTeamDetail();

		await waitFor(() => {
			expect(screen.getByTestId("team-forecast-view")).toBeInTheDocument();
		});

		expect(screen.queryByTestId("team-features-view")).not.toBeInTheDocument();
		expect(screen.queryByTestId("team-metrics-view")).not.toBeInTheDocument();
	});

	it("should show Metrics tab content when metrics tab is active", async () => {
		mockParams.tab = "metrics";
		renderTeamDetail();

		await waitFor(() => {
			expect(screen.getByTestId("team-metrics-view")).toBeInTheDocument();
		});

		expect(screen.queryByTestId("team-features-view")).not.toBeInTheDocument();
		expect(screen.queryByTestId("team-forecast-view")).not.toBeInTheDocument();
	});

	it("should handle tab navigation correctly", async () => {
		const user = userEvent.setup();
		mockParams.tab = "features";
		renderTeamDetail();

		await waitFor(() => {
			expect(screen.getByTestId("team-features-view")).toBeInTheDocument();
		});

		// Click on Forecasts tab
		await user.click(screen.getByRole("tab", { name: "Forecasts" }));
		expect(mockNavigate).toHaveBeenCalledWith("/teams/1/forecasts", {
			replace: true,
		});

		// Click on Metrics tab
		await user.click(screen.getByRole("tab", { name: "Metrics" }));
		expect(mockNavigate).toHaveBeenCalledWith("/teams/1/metrics", {
			replace: true,
		});
	});
});
