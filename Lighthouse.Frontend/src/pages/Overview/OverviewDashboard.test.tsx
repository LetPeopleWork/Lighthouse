import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { WorkTrackingSystemConnection } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
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
		teams: teamOverrides,
	}: {
		connections?: WorkTrackingSystemConnection[];
		teams?: { id: number; name: string }[];
	} = {},
) => {
	const mockPortfolioService = createMockPortfolioService();
	const mockTeamService = createMockTeamService();
	const mockTerminologyService = createMockTerminologyService();
	const mockUpdateSubscriptionService = createMockUpdateSubscriptionService();
	const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();

	// Mock data for portfolios and teams
	const mockPortfolios = [
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

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	const mockApiServiceContext = createMockApiServiceContext({
		portfolioService: mockPortfolioService,
		teamService: mockTeamService,
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
		expect(screen.getByText("Add Connection")).toBeInTheDocument();
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
				screen.getByText(/No connections configured yet/),
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

	it("disables Add Portfolio button when no teams exist", async () => {
		renderWithProviders(<OverviewDashboard />, {}, { teams: [] });

		await waitFor(() => {
			const addPortfolioButton = screen.getByRole("button", {
				name: "Add Portfolio",
			});
			expect(addPortfolioButton).toBeDisabled();
		});
	});
});
