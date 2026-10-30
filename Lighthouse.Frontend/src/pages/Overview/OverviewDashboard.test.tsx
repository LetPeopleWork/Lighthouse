import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import type React from "react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { IReportList } from "../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { WorkTrackingSystemConnection } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../services/Api/ApiError";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../services/TerminologyContext";
import { actAndSettle } from "../../tests/actAndSettle";
import { deferred } from "../../tests/HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockRbacService,
	createMockTeamService,
	createMockTerminologyService,
	createMockUpdateSubscriptionService,
	createMockWorkTrackingSystemService,
} from "../../tests/MockApiServiceProvider";
import {
	aListedReport,
	aReportList,
	aReportService,
} from "../../tests/ReportsTestKit";
import OverviewDashboard from "./OverviewDashboard";

// The one pair of events nothing else can keep straight. Both call sites sit in the same handler,
// both take a name from the same list, and swapping them type-checks - so a portfolio deletion
// counted as a team's would be a quietly wrong number rather than a broken screen.
const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));
vi.mock(
	"../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

// Mock the react-router's useNavigate function
const mockNavigate = vi.fn();
vi.mock("react-router", async () => {
	const actual = await vi.importActual("react-router");
	return {
		...actual,
		useNavigate: () => mockNavigate,
	};
});

// Mock the useLicenseRestrictions hook
vi.mock("../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreatePortfolio: true,
		createPortfolioTooltip: "",
		status: "ready",
		canCreateTeam: true,
		createTeamTooltip: "",
	}),
}));

const mockConnections = [
	new WorkTrackingSystemConnection({
		name: "My ADO Connection",
		workTrackingSystem: "AzureDevOps",
		options: [],
		id: 1,
	}),
];

const makeReconnectingConnection = (
	id: number,
	name: string,
	requiresReconnect: boolean,
): WorkTrackingSystemConnection => {
	const connection = new WorkTrackingSystemConnection({
		id,
		name,
		workTrackingSystem: "Jira",
		options: [],
		authenticationMethodKey: "jira.oauth",
	});
	connection.requiresReconnect = requiresReconnect;
	return connection;
};

/** How long the app trusts a cached answer before it reads it again, so a test can trust it exactly as long. */
const THE_APPS_STALE_TIME = 5 * 60 * 1000;

const renderWithProviders = (
	component: React.ReactElement,
	overrides: Partial<IApiServiceContext> = {},
	{
		connections = mockConnections,
		portfolios: portfolioOverrides,
		teams: teamOverrides,
		staleTime = 0,
	}: {
		connections?: WorkTrackingSystemConnection[];
		portfolios?: { id: number; name: string }[];
		teams?: { id: number; name: string }[];
		staleTime?: number;
	} = {},
) => {
	const mockPortfolioService = createMockPortfolioService();
	const mockTeamService = createMockTeamService();
	const mockRbacService = createMockRbacService();
	const mockTerminologyService = createMockTerminologyService();
	const mockUpdateSubscriptionService = createMockUpdateSubscriptionService();
	const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();

	// Mock data for portfolios and teams
	const mockPortfolios = portfolioOverrides
		? portfolioOverrides.map((p) => ({
				id: p.id,
				name: p.name,
				tags: [],
				features: [],
				involvedTeams: [],
				lastUpdated: new Date(),
				serviceLevelExpectationProbability: 0,
				serviceLevelExpectationRange: 0,
				systemWIPLimit: 0,
				remainingFeatures: 0,
			}))
		: [
				{
					id: 1,
					name: "Test Project 1",
					tags: [],
					features: [],
					involvedTeams: [],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
					remainingFeatures: 0,
				},
				{
					id: 2,
					name: "Test Project 2",
					tags: [],
					features: [],
					involvedTeams: [],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
					remainingFeatures: 0,
				},
			];

	const mockTeams = teamOverrides
		? teamOverrides.map((t) => ({
				id: t.id,
				name: t.name,
				tags: [],
				features: [],
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
			}))
		: [
				{
					id: 1,
					name: "Test Team 1",
					tags: [],
					features: [],
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
				},
				{
					id: 2,
					name: "Test Team 2",
					tags: [],
					features: [],
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
				},
			];

	// Setup mock return values
	mockPortfolioService.getPortfolios = vi
		.fn()
		.mockResolvedValue(mockPortfolios);
	mockTeamService.getTeams = vi.fn().mockResolvedValue(mockTeams);
	mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
		{
			id: 1,
			key: "portfolio",
			defaultValue: "Portfolio",
			description: "Term used for individual portfolios",
			value: "Portfolio",
		},
		{
			id: 2,
			key: "portfolios",
			defaultValue: "Portfolios",
			description: "Term used for multiple portfolios",
			value: "Portfolios",
		},
		{
			id: 3,
			key: "team",
			defaultValue: "Team",
			description: "Term used for individual teams",
			value: "Team",
		},
		{
			id: 4,
			key: "teams",
			defaultValue: "Teams",
			description: "Term used for multiple teams",
			value: "Teams",
		},
		{
			id: 5,
			key: TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM,
			defaultValue: "Work Tracking System",
			description: "Term used for work tracking system connections",
			value: "Work Tracking System",
		},
		{
			id: 6,
			key: TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS,
			defaultValue: "Work Tracking Systems",
			description: "Term used for multiple work tracking system connections",
			value: "Work Tracking Systems",
		},
	]);
	mockUpdateSubscriptionService.getGlobalUpdateStatus = vi
		.fn()
		.mockResolvedValue({
			hasActiveUpdates: false,
			activeCount: 0,
		});
	mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems = vi
		.fn()
		.mockResolvedValue(connections);
	mockWorkTrackingSystemService.getWorkTrackingSystems = vi
		.fn()
		.mockResolvedValue([]);
	mockRbacService.getStatus = vi.fn().mockResolvedValue({
		enabled: false,
		premiumGateSatisfied: true,
		hasSystemAdmin: true,
		hasEmergencyAdminConfigured: false,
		readyForEnablement: true,
	});

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false, staleTime },
			mutations: { retry: false },
		},
	});

	const mockApiServiceContext = createMockApiServiceContext({
		portfolioService: mockPortfolioService,
		teamService: mockTeamService,
		rbacService: mockRbacService,
		terminologyService: mockTerminologyService,
		updateSubscriptionService: mockUpdateSubscriptionService,
		workTrackingSystemService: mockWorkTrackingSystemService,
		licensingService: {
			getLicenseStatus: vi.fn().mockResolvedValue({
				canUsePremiumFeatures: true,
			}),
			importLicense: vi.fn(),
			clearLicense: vi.fn(),
		},
		...overrides,
	});

	const rendered = render(
		<QueryClientProvider client={queryClient}>
			<MemoryRouter>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					<TerminologyProvider>{component}</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);
	return { ...rendered, queryClient, apiContext: mockApiServiceContext };
};

describe("OverviewDashboard", () => {
	it("renders loading state initially", () => {
		renderWithProviders(<OverviewDashboard />);

		// Should show loading animation initially - check for the progress indicator
		expect(
			screen.getByTestId("loading-animation-progress-indicator"),
		).toBeInTheDocument();
	});

	it("renders portfolios section after loading", async () => {
		renderWithProviders(<OverviewDashboard />);

		// Wait for loading to complete and portfolio table to appear
		await waitFor(() => {
			expect(screen.getByText("Portfolios")).toBeInTheDocument();
		});
	});

	it("renders dashboard header with add buttons", async () => {
		renderWithProviders(<OverviewDashboard />);

		// Wait for loading to complete first
		await waitFor(() => {
			expect(screen.getByText("Portfolios")).toBeInTheDocument();
		}); // Now check for the dashboard header and buttons
		expect(screen.getByText("Add Portfolio")).toBeInTheDocument();
		expect(screen.getByText("Add Team")).toBeInTheDocument();
		expect(screen.getByText("Add Work Tracking System")).toBeInTheDocument();
	});

	it("shows main filter bar only", async () => {
		renderWithProviders(<OverviewDashboard />);

		await waitFor(() => {
			// There should only be one textbox - the main filter (individual table filters are hidden)
			const filterInputs = screen.getAllByRole("textbox");
			expect(filterInputs.length).toBe(1); // Only the main filter
		});
	});

	it("renders connections section with connection name", async () => {
		renderWithProviders(<OverviewDashboard />);

		await waitFor(() => {
			expect(screen.getByText("My ADO Connection")).toBeInTheDocument();
		});
	});

	it("renders one ReconnectBanner per RefreshFailed connection and none for healthy connections", async () => {
		const connections = [
			makeReconnectingConnection(101, "Healthy Jira", false),
			makeReconnectingConnection(102, "Stale Jira", true),
			makeReconnectingConnection(103, "Stale ADO", true),
		];

		renderWithProviders(<OverviewDashboard />, {}, { connections });

		await waitFor(() => {
			expect(screen.getByText("Stale Jira")).toBeInTheDocument();
		});

		const reconnectButtons = screen.getAllByRole("button", {
			name: /Reconnect/i,
		});
		expect(reconnectButtons).toHaveLength(2);

		const reconnectAlerts = screen
			.getAllByRole("alert")
			.filter((node) =>
				node.textContent?.includes(
					"Reconnect required — the OAuth refresh token is no longer valid",
				),
			);
		expect(reconnectAlerts).toHaveLength(2);
	});

	it("shows empty state alert when no connections exist", async () => {
		renderWithProviders(<OverviewDashboard />, {}, { connections: [] });

		await waitFor(() => {
			expect(
				screen.getByText(/No Work Tracking System found/),
			).toBeInTheDocument();
		});
	});

	it("disables Add Team button when no connections exist", async () => {
		renderWithProviders(
			<OverviewDashboard />,
			{},
			{ connections: [], teams: [] },
		);

		await waitFor(() => {
			const addTeamButton = screen.getByRole("button", {
				name: "Add Team",
			});
			expect(addTeamButton).toBeDisabled();
		});
	});

	it("enables Add Portfolio button when no teams are visible to user but rbac.canCreatePortfolio is true", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: true,
		});

		renderWithProviders(
			<OverviewDashboard />,
			{ rbacService: mockRbacService },
			{ teams: [] },
		);

		await waitFor(() => {
			expect(screen.getByText("Portfolios")).toBeInTheDocument();
		});

		const addPortfolioButton = screen.getByRole("button", {
			name: "Add Portfolio",
		});
		expect(addPortfolioButton).toBeEnabled();
	});

	it("shows RBAC no-access guidance when enabled and no teams or portfolios are visible", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			systemAdminDisplayNames: ["Admin User"],
		});

		renderWithProviders(
			<OverviewDashboard />,
			{ rbacService: mockRbacService },
			{ teams: [], portfolios: [] },
		);

		await waitFor(() => {
			expect(screen.getByTestId("rbac-no-access-alert")).toBeInTheDocument();
			expect(screen.getByText(/Admin User/)).toBeInTheDocument();
		});
	});

	it("hides Add Connection button when user is not system admin", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithProviders(<OverviewDashboard />, {
			rbacService: mockRbacService,
		});

		await waitFor(() => {
			expect(
				screen.queryByText("Add Work Tracking System"),
			).not.toBeInTheDocument();
		});
	});

	it("does not fail overview when connection settings are forbidden for non-system admin", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();
		mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems = vi
			.fn()
			.mockRejectedValue(new ApiError(403, "Forbidden"));

		renderWithProviders(<OverviewDashboard />, {
			rbacService: mockRbacService,
			workTrackingSystemService: mockWorkTrackingSystemService,
		});

		await waitFor(() => {
			expect(screen.getByText("Portfolios")).toBeInTheDocument();
			expect(
				screen.queryByText("Error loading data. Please try again later."),
			).not.toBeInTheDocument();
		});
	});

	it("does not fail overview when team and portfolio reads are forbidden", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			systemAdminDisplayNames: ["Admin User"],
		});

		const mockPortfolioService = createMockPortfolioService();
		mockPortfolioService.getPortfolios = vi
			.fn()
			.mockRejectedValue(new ApiError(403, "Forbidden"));

		const mockTeamService = createMockTeamService();
		mockTeamService.getTeams = vi
			.fn()
			.mockRejectedValue(new ApiError(403, "Forbidden"));

		renderWithProviders(<OverviewDashboard />, {
			rbacService: mockRbacService,
			portfolioService: mockPortfolioService,
			teamService: mockTeamService,
		});

		await waitFor(() => {
			expect(screen.getByTestId("rbac-no-access-alert")).toBeInTheDocument();
			expect(screen.getByText(/Admin User/)).toBeInTheDocument();
			expect(
				screen.queryByText("Error loading data. Please try again later."),
			).not.toBeInTheDocument();
		});
	});

	it("shows Add Connection button when user is system admin", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithProviders(<OverviewDashboard />, {
			rbacService: mockRbacService,
		});

		await waitFor(() => {
			expect(screen.getByText("Add Work Tracking System")).toBeInTheDocument();
		});
	});

	it("goes to a new connection from Add Work Tracking System", async () => {
		mockNavigate.mockClear();
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});
		renderWithProviders(<OverviewDashboard />, {
			rbacService: mockRbacService,
		});

		fireEvent.click(await screen.findByText("Add Work Tracking System"));

		expect(mockNavigate).toHaveBeenCalledWith("/connections/new");
	});

	it("hides Add Team button when RBAC is enabled and canCreateTeam is false", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: true,
		});

		renderWithProviders(<OverviewDashboard />, {
			rbacService: mockRbacService,
		});

		await waitFor(() => {
			expect(screen.queryByText("Add Team")).not.toBeInTheDocument();
		});
	});

	it("hides Add Portfolio button when RBAC is enabled and canCreatePortfolio is false", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: false,
		});

		renderWithProviders(<OverviewDashboard />, {
			rbacService: mockRbacService,
		});

		await waitFor(() => {
			expect(screen.queryByText("Add Portfolio")).not.toBeInTheDocument();
		});
	});

	it("shows all action buttons when RBAC is disabled", async () => {
		const mockRbacService = createMockRbacService();
		// Default permissive summary (RBAC off) - all actions allowed
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: false,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithProviders(<OverviewDashboard />, {
			rbacService: mockRbacService,
		});

		await waitFor(() => {
			expect(screen.getByText("Add Work Tracking System")).toBeInTheDocument();
			expect(screen.getByText("Add Team")).toBeInTheDocument();
			expect(screen.getByText("Add Portfolio")).toBeInTheDocument();
		});
	});

	it("hides connections section for non-system-admin viewer", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			systemAdminDisplayNames: ["Admin User"],
		});

		renderWithProviders(
			<OverviewDashboard />,
			{ rbacService: mockRbacService },
			{ connections: [], teams: [], portfolios: [] },
		);

		await waitFor(() => {
			expect(screen.getByTestId("rbac-no-access-alert")).toBeInTheDocument();
		});

		expect(
			screen.queryByRole("heading", { name: "Work Tracking Systems" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("no-connections-alert"),
		).not.toBeInTheDocument();
	});

	it("shows connections section for system admin", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithProviders(<OverviewDashboard />, {
			rbacService: mockRbacService,
		});

		await waitFor(() => {
			expect(
				screen.getByRole("heading", { name: "Work Tracking Systems" }),
			).toBeInTheDocument();
		});
	});

	it("shows enabled Add Team for non-system-admin Team Admin even when no connections exist", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: false,
		});

		renderWithProviders(
			<OverviewDashboard />,
			{ rbacService: mockRbacService },
			{ connections: [], teams: [] },
		);

		await waitFor(() => {
			expect(screen.getByText("Portfolios")).toBeInTheDocument();
		});

		const addTeamButton = screen.getByRole("button", { name: "Add Team" });
		expect(addTeamButton).toBeInTheDocument();
		expect(addTeamButton).toBeEnabled();
	});

	it("disables Add Team for system admin with no connections", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithProviders(
			<OverviewDashboard />,
			{ rbacService: mockRbacService },
			{ connections: [], teams: [] },
		);

		await waitFor(() => {
			const addTeamButton = screen.getByRole("button", { name: "Add Team" });
			expect(addTeamButton).toBeDisabled();
		});
	});

	it("hides OnboardingStepper for pure viewer with no create permissions", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			systemAdminDisplayNames: ["Admin User"],
		});

		renderWithProviders(
			<OverviewDashboard />,
			{ rbacService: mockRbacService },
			{ connections: [], teams: [], portfolios: [] },
		);

		await waitFor(() => {
			expect(screen.getByTestId("rbac-no-access-alert")).toBeInTheDocument();
		});

		expect(screen.queryByTestId("onboarding-stepper")).not.toBeInTheDocument();
	});

	it("hides OnboardingStepper when canCreateTeam is true but user is not system admin", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: false,
		});

		renderWithProviders(
			<OverviewDashboard />,
			{ rbacService: mockRbacService },
			{ connections: [], teams: [] },
		);

		await waitFor(() => {
			expect(screen.getByText("Portfolios")).toBeInTheDocument();
		});

		expect(screen.queryByTestId("onboarding-stepper")).not.toBeInTheDocument();
	});

	it("shows OnboardingStepper when user is system admin and onboarding incomplete", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithProviders(
			<OverviewDashboard />,
			{ rbacService: mockRbacService },
			{ connections: [], teams: [] },
		);

		await waitFor(() => {
			expect(screen.getByTestId("onboarding-stepper")).toBeInTheDocument();
		});
	});

	describe("when the connection list is refused", () => {
		const connectionsRefusedCopy =
			"You don't have access to the connections list. Ask a Lighthouse administrator to check your roles.";

		const renderWithConnections = (
			isSystemAdmin: boolean,
			getConfiguredWorkTrackingSystems: ReturnType<
				typeof createMockWorkTrackingSystemService
			>["getConfiguredWorkTrackingSystems"],
		) => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin,
				canCreateTeam: isSystemAdmin,
				canCreatePortfolio: isSystemAdmin,
				systemAdminDisplayNames: ["Admin User"],
			});
			const mockWorkTrackingSystemService =
				createMockWorkTrackingSystemService();
			mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems =
				getConfiguredWorkTrackingSystems;

			renderWithProviders(
				<OverviewDashboard />,
				{
					rbacService: mockRbacService,
					workTrackingSystemService: mockWorkTrackingSystemService,
				},
				{ teams: [], portfolios: [] },
			);
		};

		it("tells a system admin that access was refused instead of showing onboarding", async () => {
			renderWithConnections(
				true,
				vi.fn().mockRejectedValue(new ApiError(403, "Forbidden")),
			);

			await waitFor(() => {
				expect(screen.getByText(connectionsRefusedCopy)).toBeInTheDocument();
			});
			expect(
				screen.queryByTestId("onboarding-stepper"),
			).not.toBeInTheDocument();
		});

		it("keeps the no-access guidance for a non-admin and says nothing about connections", async () => {
			renderWithConnections(
				false,
				vi.fn().mockRejectedValue(new ApiError(403, "Forbidden")),
			);

			await waitFor(() => {
				expect(screen.getByTestId("rbac-no-access-alert")).toBeInTheDocument();
			});
			expect(
				screen.queryByText(connectionsRefusedCopy),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("onboarding-stepper"),
			).not.toBeInTheDocument();
		});

		it("says nothing about connections while the roles are still loading", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi
				.fn()
				.mockReturnValue(new Promise(() => {}));
			const mockWorkTrackingSystemService =
				createMockWorkTrackingSystemService();
			mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems = vi
				.fn()
				.mockRejectedValue(new ApiError(403, "Forbidden"));

			renderWithProviders(
				<OverviewDashboard />,
				{
					rbacService: mockRbacService,
					workTrackingSystemService: mockWorkTrackingSystemService,
				},
				{ teams: [], portfolios: [] },
			);

			await waitFor(() => {
				expect(screen.getByText("Portfolios")).toBeInTheDocument();
			});
			expect(
				screen.queryByText(connectionsRefusedCopy),
			).not.toBeInTheDocument();
		});

		it("does not call a failed connection list a refusal", async () => {
			renderWithConnections(
				true,
				vi.fn().mockRejectedValue(new ApiError(500, "Internal Server Error")),
			);

			await waitFor(() => {
				expect(
					screen.getByText("Error loading data. Please try again later."),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByText(connectionsRefusedCopy),
			).not.toBeInTheDocument();
		});

		it("still shows onboarding to a system admin who simply has no connections yet", async () => {
			renderWithConnections(true, vi.fn().mockResolvedValue([]));

			await waitFor(() => {
				expect(screen.getByTestId("onboarding-stepper")).toBeInTheDocument();
			});
			expect(
				screen.queryByText(connectionsRefusedCopy),
			).not.toBeInTheDocument();
		});
	});

	describe("Row action gating via DataOverviewTable predicates", () => {
		it("shows only Details icon on every team and portfolio row for Team Reader (no admin scopes)", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: false,
				canCreatePortfolio: false,
				adminTeamIds: [],
				adminPortfolioIds: [],
			});

			renderWithProviders(<OverviewDashboard />, {
				rbacService: mockRbacService,
			});

			await waitFor(() => {
				expect(screen.getByText("Portfolios")).toBeInTheDocument();
			});

			expect(screen.queryAllByLabelText("Edit")).toHaveLength(0);
			expect(screen.queryAllByLabelText("Clone")).toHaveLength(0);
			expect(screen.queryAllByLabelText("Delete")).toHaveLength(0);
		});

		it("shows Edit only on Team A row for a Team Admin scoped to Team A, no Delete or Clone (v1: only sysadmin deletes/clones)", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: false,
				canCreatePortfolio: false,
				adminTeamIds: [1],
				adminPortfolioIds: [],
			});

			renderWithProviders(<OverviewDashboard />, {
				rbacService: mockRbacService,
			});

			await waitFor(() => {
				expect(screen.getByText("Test Team 1")).toBeInTheDocument();
			});

			expect(screen.queryAllByLabelText("Edit")).toHaveLength(1);
			expect(screen.queryAllByLabelText("Delete")).toHaveLength(0);
			expect(screen.queryAllByLabelText("Clone")).toHaveLength(0);
		});

		it("shows Edit only on Portfolio X row for a Portfolio Admin scoped to Portfolio X, no Delete or Clone (v1: only sysadmin deletes/clones)", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: false,
				canCreatePortfolio: false,
				adminTeamIds: [],
				adminPortfolioIds: [2],
			});

			renderWithProviders(<OverviewDashboard />, {
				rbacService: mockRbacService,
			});

			await waitFor(() => {
				expect(screen.getByText("Test Project 2")).toBeInTheDocument();
			});

			expect(screen.queryAllByLabelText("Edit")).toHaveLength(1);
			expect(screen.queryAllByLabelText("Delete")).toHaveLength(0);
			expect(screen.queryAllByLabelText("Clone")).toHaveLength(0);
		});

		it("shows Edit, Clone, and Delete on every team and portfolio row for system admin", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: true,
				canCreateTeam: true,
				canCreatePortfolio: true,
			});

			renderWithProviders(<OverviewDashboard />, {
				rbacService: mockRbacService,
			});

			await waitFor(() => {
				expect(screen.getByText("Portfolios")).toBeInTheDocument();
			});

			const dataGrids = screen.getAllByTestId("datagrid-container");
			const cloneIcons = dataGrids.flatMap((grid) =>
				Array.from(grid.querySelectorAll('[aria-label="Clone"]')),
			);
			expect(cloneIcons).toHaveLength(4);

			const deleteIcons = dataGrids.flatMap((grid) =>
				Array.from(grid.querySelectorAll('[data-testid="delete-item-button"]')),
			);
			expect(deleteIcons).toHaveLength(4);

			const editIcons = dataGrids.flatMap((grid) =>
				Array.from(grid.querySelectorAll("svg[data-testid='EditIcon']")),
			);
			expect(editIcons).toHaveLength(4);
		});
	});

	describe("what a deletion is reported as", () => {
		const asASystemAdmin = () => {
			const rbacService = createMockRbacService();
			rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: true,
				canCreateTeam: true,
				canCreatePortfolio: true,
			});
			return rbacService;
		};

		const deleteTheFirstRowOf = async (section: "portfolios" | "teams") => {
			renderWithProviders(<OverviewDashboard />, {
				rbacService: asASystemAdmin(),
			});

			await waitFor(() => {
				expect(screen.getByText("Portfolios")).toBeInTheDocument();
			});

			// Portfolios are drawn first and Teams second, so the grids arrive in that order.
			const grids = screen.getAllByTestId("datagrid-container");
			const grid = section === "portfolios" ? grids[0] : grids[1];

			fireEvent.click(
				grid.querySelectorAll('[data-testid="delete-item-button"]')[0],
			);

			// Delete is held until the reports going with it are counted.
			const confirm = await screen.findByRole("button", { name: "Delete" });
			await waitFor(() => expect(confirm).toBeEnabled());
			fireEvent.click(confirm);
		};

		it("reports a deleted Portfolio as a Portfolio", async () => {
			reportUsage.mockClear();

			await deleteTheFirstRowOf("portfolios");

			await waitFor(() =>
				expect(reportUsage).toHaveBeenCalledWith({
					name: "PortfolioDeleted",
				}),
			);
		});

		it("reports a deleted Team as a Team", async () => {
			reportUsage.mockClear();

			await deleteTheFirstRowOf("teams");

			await waitFor(() =>
				expect(reportUsage).toHaveBeenCalledWith({ name: "TeamDeleted" }),
			);
		});
	});

	// A Team or Portfolio takes its reports with it, so the confirmation says how many go too.
	describe("deleting a Team or Portfolio that has reports", () => {
		const withReports = (count: number) =>
			aReportService({
				getReports: vi
					.fn()
					.mockResolvedValue(
						aReportList(
							Array.from({ length: count }, (_, index) =>
								aListedReport({ id: index + 1, name: `Report ${index + 1}` }),
							),
						),
					),
			});

		const askToDeleteTheFirstRowOf = async (
			section: "portfolios" | "teams",
			reports: number,
			reportService = withReports(reports),
			{
				staleTime,
				beforeAsking,
			}: {
				staleTime?: number;
				beforeAsking?: (queryClient: QueryClient) => Promise<void>;
			} = {},
		) => {
			const rbacService = createMockRbacService();
			rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: true,
				canCreateTeam: true,
				canCreatePortfolio: true,
			});
			const { queryClient, apiContext } = renderWithProviders(
				<OverviewDashboard />,
				{ rbacService, reportService },
				{ staleTime },
			);
			await waitFor(() => {
				expect(screen.getByText("Portfolios")).toBeInTheDocument();
			});
			await beforeAsking?.(queryClient);
			const grids = screen.getAllByTestId("datagrid-container");
			fireEvent.click(
				(section === "portfolios" ? grids[0] : grids[1]).querySelectorAll(
					'[data-testid="delete-item-button"]',
				)[0],
			);
			return {
				dialog: await screen.findByRole("dialog"),
				reportService,
				queryClient,
				apiContext,
			};
		};

		// One report is all Community keeps; more than one is a Premium instance, or one whose licence lapsed.
		it.each([
			["teams", "Test Team 1", 1, "and its report"],
			["portfolios", "Test Project 1", 1, "and its report"],
			["teams", "Test Team 1", 3, "and its 3 reports"],
		] as const)(
			"names the reports that go with one of the %s (%s) holding %i when it is deleted",
			async (section, name, reports, clause) => {
				const { dialog, reportService } = await askToDeleteTheFirstRowOf(
					section,
					reports,
				);

				await waitFor(() =>
					expect(dialog).toHaveTextContent(
						`Do you really want to delete ${name} ${clause}?`,
					),
				);
				expect(reportService.getReports).toHaveBeenCalledWith({
					kind: section === "portfolios" ? "portfolio" : "team",
					id: 1,
				});
			},
		);

		it("says nothing about reports when there are none", async () => {
			const { dialog, reportService } = await askToDeleteTheFirstRowOf(
				"teams",
				0,
			);

			await waitFor(() =>
				expect(reportService.getReports).toHaveBeenCalledWith({
					kind: "team",
					id: 1,
				}),
			);
			expect(dialog).toHaveTextContent(
				"Do you really want to delete Test Team 1?",
			);
			expect(dialog).not.toHaveTextContent(/report/i);
		});

		// Reports going with their Team are not somebody deleting a report.
		it("reports the Team deleted and never a report deleted", async () => {
			reportUsage.mockClear();
			const { dialog } = await askToDeleteTheFirstRowOf("teams", 1);
			await waitFor(() => expect(dialog).toHaveTextContent("and its report?"));

			fireEvent.click(within(dialog).getByRole("button", { name: "Delete" }));

			await waitFor(() =>
				expect(reportUsage).toHaveBeenCalledWith({ name: "TeamDeleted" }),
			);
			expect(reportUsage).not.toHaveBeenCalledWith({ name: "ReportDeleted" });
		});

		// A deleted Team's reports are gone with it, so nothing may still answer for them from memory.
		it("forgets the deleted Team's reports", async () => {
			const { dialog, queryClient, apiContext } =
				await askToDeleteTheFirstRowOf("teams", 1);
			// Once deleted, the server stops listing the Team, so its row and its Reports cell go away
			// instead of reading the reports again straight after they were forgotten.
			const { teamService } = apiContext;
			vi.mocked(teamService.deleteTeam).mockImplementation(async (id) => {
				const remaining = await teamService.getTeams();
				vi.mocked(teamService.getTeams).mockResolvedValue(
					remaining.filter((team) => team.id !== id),
				);
			});
			queryClient.setQueryData(["report", "team", 1, 1], { name: "Report 1" });
			queryClient.setQueryData(["report", "team", 2, 1], { name: "Kept" });
			await waitFor(() => expect(dialog).toHaveTextContent("and its report?"));

			fireEvent.click(within(dialog).getByRole("button", { name: "Delete" }));

			await waitFor(() =>
				expect(reportUsage).toHaveBeenCalledWith({ name: "TeamDeleted" }),
			);
			await waitFor(() =>
				expect(
					queryClient.getQueryData(["reports", "team", 1]),
				).toBeUndefined(),
			);
			expect(
				queryClient.getQueryData(["report", "team", 1, 1]),
			).toBeUndefined();
			expect(queryClient.getQueryData(["report", "team", 2, 1])).toEqual({
				name: "Kept",
			});
		});

		it("keeps a Team's reports in memory and says something went wrong when the Team cannot be deleted", async () => {
			reportUsage.mockClear();
			const silenced = vi
				.spyOn(console, "error")
				.mockImplementation(() => undefined);
			const { dialog, queryClient, apiContext } =
				await askToDeleteTheFirstRowOf("teams", 1);
			vi.mocked(apiContext.teamService.deleteTeam).mockRejectedValue(
				new Error("the network dropped"),
			);
			queryClient.setQueryData(["report", "team", 1, 1], { name: "Report 1" });
			await waitFor(() => expect(dialog).toHaveTextContent("and its report?"));

			fireEvent.click(within(dialog).getByRole("button", { name: "Delete" }));

			expect(
				await screen.findByTestId("loading-animation-error-message"),
			).toBeVisible();
			expect(queryClient.getQueryData(["report", "team", 1, 1])).toEqual({
				name: "Report 1",
			});
			expect(reportUsage).not.toHaveBeenCalledWith({ name: "TeamDeleted" });
			silenced.mockRestore();
		});

		// Deleting before the count arrives would take reports along that the dialog never named.
		it("holds Delete until it knows how many reports go with it", async () => {
			const { dialog } = await askToDeleteTheFirstRowOf(
				"teams",
				0,
				aReportService({
					getReports: vi.fn().mockReturnValue(new Promise(() => {})),
				}),
			);

			expect(
				within(dialog).getByRole("progressbar", {
					name: "Counting its reports",
				}),
			).toBeVisible();
			expect(
				within(dialog).getByRole("button", { name: "Delete" }),
			).toBeDisabled();
		});

		// The row's Reports cell counted them a moment ago, but someone may have made or deleted one since.
		it("holds Delete until it has counted the reports itself, even when the row has just counted them", async () => {
			const recount = deferred<IReportList>();
			const getReports = vi
				.fn()
				.mockResolvedValue(
					aReportList([aListedReport({ id: 1, name: "Report 1" })]),
				);
			const { dialog } = await askToDeleteTheFirstRowOf(
				"teams",
				1,
				aReportService({ getReports }),
				{
					staleTime: THE_APPS_STALE_TIME,
					beforeAsking: async (queryClient) => {
						await waitFor(() =>
							expect(
								queryClient.getQueryState(["reports", "team", 1])?.status,
							).toBe("success"),
						);
						getReports.mockReturnValue(recount.promise);
					},
				},
			);

			expect(
				within(dialog).getByRole("button", { name: "Delete" }),
			).toBeDisabled();

			await actAndSettle(() => {
				recount.resolve(
					aReportList([
						aListedReport({ id: 1, name: "Report 1" }),
						aListedReport({ id: 2, name: "Report 2" }),
					]),
				);
			});

			await waitFor(() =>
				expect(dialog).toHaveTextContent("and its 2 reports"),
			);
			expect(
				within(dialog).getByRole("button", { name: "Delete" }),
			).toBeEnabled();
		});

		it("says any reports it has go with it when their number cannot be read", async () => {
			const { dialog } = await askToDeleteTheFirstRowOf(
				"teams",
				0,
				aReportService({
					getReports: vi
						.fn()
						.mockRejectedValue(new Error("the network dropped")),
				}),
			);

			await waitFor(() =>
				expect(dialog).toHaveTextContent(
					"Do you really want to delete Test Team 1 and any reports it has?",
				),
			);
			expect(
				within(dialog).getByRole("button", { name: "Delete" }),
			).toBeEnabled();
		});
	});

	describe("Add Team / Add Portfolio capability-based gating", () => {
		it("shows Add Team for non-system-admin Team Admin and shows Add Portfolio for non-system-admin Portfolio Admin", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: true,
				canCreatePortfolio: true,
			});

			renderWithProviders(<OverviewDashboard />, {
				rbacService: mockRbacService,
			});

			await waitFor(() => {
				expect(screen.getByText("Add Team")).toBeInTheDocument();
				expect(screen.getByText("Add Portfolio")).toBeInTheDocument();
			});
		});

		it("shows Add Team for Team Admin only and hides Add Portfolio when canCreatePortfolio is false", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: true,
				canCreatePortfolio: false,
			});

			renderWithProviders(<OverviewDashboard />, {
				rbacService: mockRbacService,
			});

			await waitFor(() => {
				expect(screen.getByText("Portfolios")).toBeInTheDocument();
			});

			expect(screen.getByText("Add Team")).toBeInTheDocument();
			expect(screen.queryByText("Add Portfolio")).not.toBeInTheDocument();
		});

		it("shows Add Portfolio for Portfolio Admin only and hides Add Team when canCreateTeam is false", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: false,
				canCreatePortfolio: true,
			});

			renderWithProviders(<OverviewDashboard />, {
				rbacService: mockRbacService,
			});

			await waitFor(() => {
				expect(screen.getByText("Portfolios")).toBeInTheDocument();
			});

			expect(screen.getByText("Add Portfolio")).toBeInTheDocument();
			expect(screen.queryByText("Add Team")).not.toBeInTheDocument();
		});

		it("hides Add Team and Add Portfolio for Viewer with no create capabilities", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: false,
				canCreateTeam: false,
				canCreatePortfolio: false,
			});

			renderWithProviders(<OverviewDashboard />, {
				rbacService: mockRbacService,
			});

			await waitFor(() => {
				expect(screen.getByText("Portfolios")).toBeInTheDocument();
			});

			expect(screen.queryByText("Add Team")).not.toBeInTheDocument();
			expect(screen.queryByText("Add Portfolio")).not.toBeInTheDocument();
		});

		it("shows Add Team and Add Portfolio for system admin when license/connection conditions met", async () => {
			const mockRbacService = createMockRbacService();
			mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
				isRbacEnabled: true,
				isSystemAdmin: true,
				canCreateTeam: true,
				canCreatePortfolio: true,
			});

			renderWithProviders(<OverviewDashboard />, {
				rbacService: mockRbacService,
			});

			await waitFor(() => {
				expect(screen.getByText("Add Team")).toBeInTheDocument();
				expect(screen.getByText("Add Portfolio")).toBeInTheDocument();
			});
		});
	});
});
