import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockProjectService,
	createMockTeamService,
	createMockTerminologyService,
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
		canCreateProject: true,
		createProjectTooltip: "",
		canCreateTeam: true,
		createTeamTooltip: "",
	}),
}));

const renderWithProviders = (component: React.ReactElement) => {
	const mockProjectService = createMockProjectService();
	const mockTeamService = createMockTeamService();
	const mockTerminologyService = createMockTerminologyService();

	// Mock data for projects and teams
	const mockProjects = [
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
			milestones: [],
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
			milestones: [],
		},
	];

	const mockTeams = [
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
	mockProjectService.getProjects = vi.fn().mockResolvedValue(mockProjects);
	mockTeamService.getTeams = vi.fn().mockResolvedValue(mockTeams);

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	const mockApiServiceContext = createMockApiServiceContext({
		projectService: mockProjectService,
		teamService: mockTeamService,
		terminologyService: mockTerminologyService,
		licensingService: {
			getLicenseStatus: vi.fn().mockResolvedValue({
				canUsePremiumFeatures: true,
			}),
			importLicense: vi.fn(),
		},
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

	it("renders projects section after loading", async () => {
		renderWithProviders(<OverviewDashboard />);

		// Wait for loading to complete and project table to appear
		await waitFor(() => {
			expect(screen.getByText("Projects")).toBeInTheDocument();
		});
	});

	it("renders dashboard header with add buttons", async () => {
		renderWithProviders(<OverviewDashboard />);

		// Wait for loading to complete first
		await waitFor(() => {
			expect(screen.getByText("Projects")).toBeInTheDocument();
		});

		// Now check for the dashboard header and buttons
		expect(screen.getByText("Add Project")).toBeInTheDocument();
		expect(screen.getByText("Add team")).toBeInTheDocument();
	});

	it("shows main filter bar only", async () => {
		renderWithProviders(<OverviewDashboard />);

		await waitFor(() => {
			// There should only be one textbox - the main filter (individual table filters are hidden)
			const filterInputs = screen.getAllByRole("textbox");
			expect(filterInputs.length).toBe(1); // Only the main filter
		});
	});
});
