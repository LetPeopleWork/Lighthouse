import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IUpdateStatus } from "../../../services/UpdateSubscriptionService";
import {
	createMockApiServiceContext,
	createMockRbacService,
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

vi.mock("../../../components/Common/Team/ModifyTeamSettings", () => ({
	default: () => <div data-testid="team-settings-editor">Settings Editor</div>,
}));

// Mock the useTerminology hook
vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.TEAM]: "Team",
				[TERMINOLOGY_KEYS.FEATURES]: "Features",
				[TERMINOLOGY_KEYS.PORTFOLIO]: "Portfolio",
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
	features: [{ id: 1, name: "Feature 1", key: "F-1" }], // Default to having features
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

		// Check that Features view is rendered (which means it's the active tab)
		expect(screen.getByTestId("team-features-view")).toBeInTheDocument();
		expect(screen.queryByTestId("team-forecast-view")).not.toBeInTheDocument();
		expect(screen.queryByTestId("team-metrics-view")).not.toBeInTheDocument();
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

	it("should disable Features tab when team has no features", async () => {
		const mockTeamService = createMockTeamService();
		const mockUpdateSubscriptionService = createMockUpdateSubscriptionService();

		const teamWithNoFeatures = {
			...mockTeam,
			features: [], // No features
		};

		mockTeamService.getTeam = vi.fn().mockResolvedValue(teamWithNoFeatures);
		mockUpdateSubscriptionService.subscribeToTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.unsubscribeFromTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.getUpdateStatus = vi
			.fn()
			.mockResolvedValue(null);

		const mockApiContext = createMockApiServiceContext({
			teamService: mockTeamService,
			updateSubscriptionService: mockUpdateSubscriptionService,
		});

		render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TeamDetail />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);

		await waitFor(() => {
			const featuresTab = screen.getByRole("tab", { name: "Features" });
			expect(featuresTab).toBeInTheDocument();
		});

		const featuresTab = screen.getByRole("tab", { name: "Features" });
		expect(featuresTab).toBeDisabled();
	});

	it("should enable Features tab when team has features", async () => {
		const mockTeamService = createMockTeamService();
		const mockUpdateSubscriptionService = createMockUpdateSubscriptionService();

		const teamWithFeatures = {
			...mockTeam,
			features: [{ id: 1, name: "Feature 1", key: "F-1" }],
		};

		mockTeamService.getTeam = vi.fn().mockResolvedValue(teamWithFeatures);
		mockUpdateSubscriptionService.subscribeToTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.unsubscribeFromTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.getUpdateStatus = vi
			.fn()
			.mockResolvedValue(null);

		const mockApiContext = createMockApiServiceContext({
			teamService: mockTeamService,
			updateSubscriptionService: mockUpdateSubscriptionService,
		});

		render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TeamDetail />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);

		await waitFor(() => {
			const featuresTab = screen.getByRole("tab", { name: "Features" });
			expect(featuresTab).toBeInTheDocument();
		});

		const featuresTab = screen.getByRole("tab", { name: "Features" });
		expect(featuresTab).not.toBeDisabled();
	});

	it("should redirect to Forecasts tab when navigating to Features tab with no features", async () => {
		const mockTeamService = createMockTeamService();
		const mockUpdateSubscriptionService = createMockUpdateSubscriptionService();

		const teamWithNoFeatures = {
			...mockTeam,
			features: [], // No features
		};

		mockTeamService.getTeam = vi.fn().mockResolvedValue(teamWithNoFeatures);
		mockUpdateSubscriptionService.subscribeToTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.unsubscribeFromTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.getUpdateStatus = vi
			.fn()
			.mockResolvedValue(null);

		const mockApiContext = createMockApiServiceContext({
			teamService: mockTeamService,
			updateSubscriptionService: mockUpdateSubscriptionService,
		});

		mockParams = { id: "1", tab: "features" }; // Try to navigate to features

		render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TeamDetail />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);

		await waitFor(() => {
			expect(mockNavigate).toHaveBeenCalledWith("/teams/1/forecasts", {
				replace: true,
			});
		});
	});

	it("should redirect to Forecasts tab when direct URL navigation to Features with no features", async () => {
		const mockTeamService = createMockTeamService();
		const mockUpdateSubscriptionService = createMockUpdateSubscriptionService();

		const teamWithNoFeatures = {
			...mockTeam,
			features: [], // No features
		};

		mockTeamService.getTeam = vi.fn().mockResolvedValue(teamWithNoFeatures);
		mockUpdateSubscriptionService.subscribeToTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.unsubscribeFromTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.getUpdateStatus = vi
			.fn()
			.mockResolvedValue(null);

		const mockApiContext = createMockApiServiceContext({
			teamService: mockTeamService,
			updateSubscriptionService: mockUpdateSubscriptionService,
		});

		// Simulate direct URL navigation to features tab
		mockParams = { id: "1", tab: "features" };

		render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TeamDetail />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);

		// Should redirect to forecasts
		await waitFor(() => {
			expect(mockNavigate).toHaveBeenCalledWith("/teams/1/forecasts", {
				replace: true,
			});
		});

		// Should show forecasts view
		await waitFor(() => {
			expect(screen.getByTestId("team-forecast-view")).toBeInTheDocument();
		});
	});

	it("shows no-access guidance when team details are unavailable", async () => {
		const mockTeamService = createMockTeamService();
		const mockUpdateSubscriptionService = createMockUpdateSubscriptionService();

		mockTeamService.getTeam = vi.fn().mockResolvedValue(null);
		mockUpdateSubscriptionService.subscribeToTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.unsubscribeFromTeamUpdates = vi.fn();
		mockUpdateSubscriptionService.getUpdateStatus = vi
			.fn()
			.mockResolvedValue(null);

		const mockApiContext = createMockApiServiceContext({
			teamService: mockTeamService,
			updateSubscriptionService: mockUpdateSubscriptionService,
		});

		render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TeamDetail />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);

		await waitFor(() => {
			expect(screen.getByTestId("team-no-access-alert")).toBeInTheDocument();
		});
	});

	describe("settings tab update deferral", () => {
		beforeEach(() => {
			// Start every test in this group on the settings tab
			mockParams = { id: "1", tab: "settings" };
		});

		const buildSettingsTabContext = () => {
			const mockTeamService = createMockTeamService();
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

			let capturedCallback: ((update: IUpdateStatus) => void) | null = null;
			const mockUpdateService = createMockUpdateSubscriptionService();
			mockUpdateService.subscribeToTeamUpdates = vi
				.fn()
				.mockImplementation(
					async (_id: number, callback: (update: IUpdateStatus) => void) => {
						capturedCallback = callback;
					},
				);
			mockUpdateService.unsubscribeFromTeamUpdates = vi.fn();
			mockUpdateService.getUpdateStatus = vi.fn().mockResolvedValue(null);

			const mockApiContext = createMockApiServiceContext({
				teamService: mockTeamService,
				updateSubscriptionService: mockUpdateService,
			});

			return {
				mockTeamService,
				mockApiContext,
				getCallback: () => capturedCallback,
			};
		};

		it("does not reload team when SignalR fires Completed while settings tab is active", async () => {
			const { mockTeamService, mockApiContext, getCallback } =
				buildSettingsTabContext();

			render(
				<BrowserRouter>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TeamDetail />
					</ApiServiceContext.Provider>
				</BrowserRouter>,
			);

			await waitFor(() =>
				expect(screen.getByTestId("team-settings-editor")).toBeInTheDocument(),
			);

			const callsBefore = (mockTeamService.getTeam as ReturnType<typeof vi.fn>)
				.mock.calls.length;

			// Background update completes while the user is on the settings tab
			await act(async () => {
				await getCallback()?.({
					status: "Completed",
					updateType: "Team",
					id: 1,
				});
			});

			// Team data must NOT have been reloaded
			expect(mockTeamService.getTeam).toHaveBeenCalledTimes(callsBefore);
		});

		it("reloads team data when leaving settings tab after a deferred update", async () => {
			const user = userEvent.setup();
			const { mockTeamService, mockApiContext, getCallback } =
				buildSettingsTabContext();

			render(
				<BrowserRouter>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TeamDetail />
					</ApiServiceContext.Provider>
				</BrowserRouter>,
			);

			await waitFor(() =>
				expect(screen.getByTestId("team-settings-editor")).toBeInTheDocument(),
			);

			// Background update completes while on settings
			await act(async () => {
				await getCallback()?.({
					status: "Completed",
					updateType: "Team",
					id: 1,
				});
			});

			const callsAfterDeferred = (
				mockTeamService.getTeam as ReturnType<typeof vi.fn>
			).mock.calls.length;

			// User navigates away from settings
			await user.click(screen.getByRole("tab", { name: "Forecasts" }));

			// The deferred refresh must now fire
			await waitFor(() => {
				expect(mockTeamService.getTeam).toHaveBeenCalledTimes(
					callsAfterDeferred + 1,
				);
			});
		});
	});

	describe("Forecast refresh subscription contract (bug 5022)", () => {
		beforeEach(() => {
			mockParams = { id: "1", tab: "features" };
		});

		const buildTeamPageContext = () => {
			const buildFreshTeam = () => ({
				...mockTeam,
				features: [{ id: 1, name: "Feature 1", key: "F-1" }],
				portfolios: [{ id: 100, name: "Portfolio 100" }],
			});

			const mockTeamService = createMockTeamService();
			mockTeamService.getTeam = vi
				.fn()
				.mockImplementation(async () => buildFreshTeam());
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

			let teamCallback: ((update: IUpdateStatus) => void) | null = null;
			let forecastCallback: ((update: IUpdateStatus) => void) | null = null;
			const mockUpdateService = createMockUpdateSubscriptionService();
			mockUpdateService.subscribeToTeamUpdates = vi
				.fn()
				.mockImplementation(
					async (_id: number, cb: (update: IUpdateStatus) => void) => {
						teamCallback = cb;
					},
				);
			mockUpdateService.subscribeToForecastUpdates = vi
				.fn()
				.mockImplementation(
					async (_id: number, cb: (update: IUpdateStatus) => void) => {
						forecastCallback = cb;
					},
				);
			mockUpdateService.unsubscribeFromTeamUpdates = vi.fn();
			mockUpdateService.unsubscribeFromForecastUpdates = vi.fn();
			mockUpdateService.getUpdateStatus = vi.fn().mockResolvedValue(null);

			const mockApiContext = createMockApiServiceContext({
				teamService: mockTeamService,
				updateSubscriptionService: mockUpdateService,
			});

			return {
				mockTeamService,
				mockUpdateService,
				mockApiContext,
				getTeamCallback: () => teamCallback,
				getForecastCallback: () => forecastCallback,
			};
		};

		const renderWith = (
			mockApiContext: ReturnType<typeof createMockApiServiceContext>,
		) =>
			render(
				<BrowserRouter>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TeamDetail />
					</ApiServiceContext.Provider>
				</BrowserRouter>,
			);

		it("subscribes to Team updates exactly once per mount across team refetches", async () => {
			const ctx = buildTeamPageContext();
			renderWith(ctx.mockApiContext);

			await waitFor(() => expect(ctx.getTeamCallback()).not.toBeNull());

			const initialFetchCount = (
				ctx.mockTeamService.getTeam as ReturnType<typeof vi.fn>
			).mock.calls.length;

			await act(async () => {
				await ctx.getTeamCallback()?.({
					status: "Completed",
					updateType: "Team",
					id: 1,
				});
			});

			await waitFor(() =>
				expect(
					(ctx.mockTeamService.getTeam as ReturnType<typeof vi.fn>).mock.calls
						.length,
				).toBeGreaterThan(initialFetchCount),
			);
			await new Promise((resolve) => setTimeout(resolve, 50));

			expect(
				ctx.mockUpdateService.subscribeToTeamUpdates,
			).toHaveBeenCalledTimes(1);
		});

		it("subscribes to Forecasts updates so portfolio forecast completion can refresh team feature data", async () => {
			const ctx = buildTeamPageContext();
			renderWith(ctx.mockApiContext);

			await waitFor(() => expect(ctx.getTeamCallback()).not.toBeNull());
			await new Promise((resolve) => setTimeout(resolve, 50));

			expect(
				ctx.mockUpdateService.subscribeToForecastUpdates,
			).toHaveBeenCalled();
		});
	});
});

describe("TeamDetail - RBAC Settings Tab Visibility", () => {
	const mockTeamService = createMockTeamService();
	(mockTeamService.getTeam as ReturnType<typeof vi.fn>).mockResolvedValue({
		id: 1,
		name: "Test Team",
		features: [],
		tags: [],
		projects: [],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		remainingFeatures: 0,
		featureWip: 1,
		useFixedDatesForThroughput: false,
		throughputStartDate: new Date(),
		throughputEndDate: new Date(),
	});

	const renderTeamDetail = (
		rbacOverrides?: Partial<ReturnType<typeof createMockRbacService>>,
	) => {
		const mockRbacService = createMockRbacService();
		if (rbacOverrides) {
			Object.assign(mockRbacService, rbacOverrides);
		}
		const mockUpdateSubscription = createMockUpdateSubscriptionService();
		const mockApiContext = createMockApiServiceContext({
			teamService: mockTeamService,
			rbacService: mockRbacService,
			updateSubscriptionService: mockUpdateSubscription,
		});
		render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TeamDetail />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);

		return { mockRbacService };
	};

	it("should show Settings tab when RBAC is disabled", async () => {
		renderTeamDetail({
			getAuthorizationSummary: vi.fn().mockResolvedValue({
				isRbacEnabled: false,
				isSystemAdmin: false,
				canCreateTeam: false,
				canCreatePortfolio: false,
			}),
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
	});

	it("should show Settings tab when user is system admin", async () => {
		renderTeamDetail({
			getAuthorizationSummary: vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: true,
				canCreateTeam: false,
				canCreatePortfolio: false,
			}),
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
	});

	it("should show Settings tab when user is admin of this team", async () => {
		renderTeamDetail({
			getAuthorizationSummary: vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: false,
				canCreatePortfolio: false,
				adminTeamIds: [1],
			}),
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
	});

	it("should hide Settings tab when RBAC is enabled and user cannot manage teams", async () => {
		renderTeamDetail({
			getAuthorizationSummary: vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: false,
				canCreatePortfolio: false,
			}),
		});

		await waitFor(() => {
			expect(
				screen.queryByRole("tab", { name: "Settings" }),
			).not.toBeInTheDocument();
		});
	});

	it("loads and renders team membership manager on access tab", async () => {
		mockParams = { id: "1", tab: "access" };
		const getTeamMembers = vi.fn().mockResolvedValue([
			{
				userProfileId: 17,
				subject: "auth0|member",
				displayName: "Member",
				email: "member@example.com",
				role: "Viewer",
			},
		]);

		renderTeamDetail({
			getAuthorizationSummary: vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: true,
				canCreatePortfolio: false,
			}),
			getTeamMembers,
		});

		await waitFor(() => {
			expect(screen.getByText("Team Access")).toBeInTheDocument();
			expect(screen.getByTestId("scoped-member-row-17")).toBeInTheDocument();
		});

		expect(getTeamMembers).toHaveBeenCalledWith(1);
	});

	it("creates team-scoped group mappings on access tab", async () => {
		mockParams = { id: "1", tab: "access" };
		const user = userEvent.setup();
		const getTeamGroupMappings = vi.fn().mockResolvedValue([
			{
				id: 77,
				groupValue: "team-viewers",
				role: "Viewer",
				scopeType: "Team",
				scopeId: 1,
			},
		]);
		const createGroupMapping = vi.fn().mockResolvedValue(undefined);

		renderTeamDetail({
			getAuthorizationSummary: vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: true,
				canCreatePortfolio: false,
			}),
			getTeamGroupMappings,
			createGroupMapping,
		});

		await waitFor(() => {
			expect(screen.getByText("Team Group Access")).toBeInTheDocument();
			expect(screen.getByTestId("scoped-group-row-77")).toBeInTheDocument();
		});

		expect(getTeamGroupMappings).toHaveBeenCalledWith(1);

		await user.type(screen.getByLabelText("Group value"), "team-admins");
		await user.click(screen.getByTestId("scoped-group-add-button"));

		await waitFor(() => {
			expect(createGroupMapping).toHaveBeenCalledWith({
				groupValue: "team-admins",
				role: "TeamAdmin",
				scopeType: "Team",
				scopeId: 1,
			});
		});
	});
});

describe("TeamDetail - RBAC Access Tab and Write Controls Visibility", () => {
	const buildMockTeam = () => ({
		id: 1,
		name: "Test Team",
		features: [],
		tags: [],
		projects: [],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		remainingFeatures: 0,
		featureWip: 1,
		useFixedDatesForThroughput: false,
		throughputStartDate: new Date(),
		throughputEndDate: new Date(),
	});

	const renderForRbac = (summary: {
		isRbacEnabled: boolean;
		isSystemAdmin: boolean;
		adminTeamIds?: number[];
	}) => {
		const mockTeamService = createMockTeamService();
		(mockTeamService.getTeam as ReturnType<typeof vi.fn>).mockResolvedValue(
			buildMockTeam(),
		);
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			...summary,
			canCreateTeam: false,
			canCreatePortfolio: false,
		});
		const mockUpdateSubscription = createMockUpdateSubscriptionService();
		const mockApiContext = createMockApiServiceContext({
			teamService: mockTeamService,
			rbacService: mockRbacService,
			updateSubscriptionService: mockUpdateSubscription,
		});
		render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TeamDetail />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);
	};

	beforeEach(() => {
		mockParams = { id: "1", tab: "forecasts" };
		mockNavigate.mockClear();
	});

	it("shows Settings and Access tabs for Team Admin of own team", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminTeamIds: [1],
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
		expect(screen.getByRole("tab", { name: "Access" })).toBeInTheDocument();
	});

	it("hides Settings and Access tabs for Team Admin of a different team", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminTeamIds: [99],
		});

		await waitFor(() => {
			expect(
				screen.getByRole("tab", { name: "Forecasts" }),
			).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("tab", { name: "Settings" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("tab", { name: "Access" }),
		).not.toBeInTheDocument();
	});

	it("hides Settings and Access tabs for Viewer of this team", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminTeamIds: [],
		});

		await waitFor(() => {
			expect(
				screen.getByRole("tab", { name: "Forecasts" }),
			).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("tab", { name: "Settings" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("tab", { name: "Access" }),
		).not.toBeInTheDocument();
	});

	it("hides Access tab when isRbacEnabled is false but keeps Settings tab (DD-10)", async () => {
		renderForRbac({
			isRbacEnabled: false,
			isSystemAdmin: false,
			adminTeamIds: [],
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("tab", { name: "Access" }),
		).not.toBeInTheDocument();
	});

	it("does not render Access tab on initial render before team loads in non-RBAC mode (DD-07)", () => {
		const mockTeamService = createMockTeamService();
		let resolveGetTeam: ((value: unknown) => void) | undefined;
		(mockTeamService.getTeam as ReturnType<typeof vi.fn>).mockImplementation(
			() =>
				new Promise((resolve) => {
					resolveGetTeam = resolve;
				}),
		);
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: false,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			adminTeamIds: [],
		});
		const mockApiContext = createMockApiServiceContext({
			teamService: mockTeamService,
			rbacService: mockRbacService,
			updateSubscriptionService: createMockUpdateSubscriptionService(),
		});

		render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TeamDetail />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);

		expect(
			screen.queryByRole("tab", { name: "Access" }),
		).not.toBeInTheDocument();

		resolveGetTeam?.(undefined);
	});

	it("shows Update Team Data button and QuickSettingsBar for Team Admin of own team", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminTeamIds: [1],
		});

		await waitFor(() => {
			expect(
				screen.getByRole("button", { name: "Update Team Data" }),
			).toBeInTheDocument();
		});
		expect(
			screen.getByRole("button", { name: "System WIP Limit" }),
		).toBeInTheDocument();
	});

	it("hides Update Team Data button and QuickSettingsBar for Viewer", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminTeamIds: [],
		});

		await waitFor(() => {
			expect(
				screen.getByRole("tab", { name: "Forecasts" }),
			).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("button", { name: "Update Team Data" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "System WIP Limit" }),
		).not.toBeInTheDocument();
	});

	it("hides Update Team Data button and QuickSettingsBar for Team Admin of a different team", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminTeamIds: [99],
		});

		await waitFor(() => {
			expect(
				screen.getByRole("tab", { name: "Forecasts" }),
			).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("button", { name: "Update Team Data" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "System WIP Limit" }),
		).not.toBeInTheDocument();
	});

	it("shows Update Team Data button and QuickSettingsBar when RBAC is disabled (PERMISSIVE_SUMMARY)", async () => {
		renderForRbac({
			isRbacEnabled: false,
			isSystemAdmin: false,
			adminTeamIds: [],
		});

		await waitFor(() => {
			expect(
				screen.getByRole("button", { name: "Update Team Data" }),
			).toBeInTheDocument();
		});
		expect(
			screen.getByRole("button", { name: "System WIP Limit" }),
		).toBeInTheDocument();
	});
});
