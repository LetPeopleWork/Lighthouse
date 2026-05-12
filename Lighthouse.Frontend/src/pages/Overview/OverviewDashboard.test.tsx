import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { WorkTrackingSystemConnection } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../services/Api/ApiError";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockRbacService,
	createMockTeamService,
	createMockTerminologyService,
	createMockUpdateSubscriptionService,
	createMockWorkTrackingSystemService,
} from "../../tests/MockApiServiceProvider";
import OverviewDashboard from "./OverviewDashboard";

// Mock the react-router-dom's useNavigate function
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
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

const renderWithProviders = (
	component: React.ReactElement,
	overrides: Partial<IApiServiceContext> = {},
	{
		connections = mockConnections,
		portfolios: portfolioOverrides,
		teams: teamOverrides,
	}: {
		connections?: WorkTrackingSystemConnection[];
		portfolios?: { id: number; name: string }[];
		teams?: { id: number; name: string }[];
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
			queries: { retry: false },
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

	return render(
		<QueryClientProvider client={queryClient}>
			<MemoryRouter>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					<TerminologyProvider>{component}</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);
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
