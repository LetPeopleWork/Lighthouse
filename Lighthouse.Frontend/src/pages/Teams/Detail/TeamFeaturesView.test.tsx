import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import TeamFeaturesView from "./TeamFeaturesView";

// Mock the useTerminology hook
vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.FEATURES]: "Features",
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

// Mock TeamFeatureList component
vi.mock("./TeamFeatureList", () => ({
	default: ({ team }: { team: Team }) => (
		<div data-testid="team-feature-list">TeamFeatureList for {team.name}</div>
	),
}));

describe("TeamFeaturesView component", () => {
	const mockTeam: Team = {
		id: 1,
		name: "Test Team",
		features: [],
		portfolios: [],
		tags: [],
		featureWip: 5,
		lastUpdated: new Date(),
		useFixedDatesForThroughput: false,
		throughputStartDate: new Date(),
		throughputEndDate: new Date(),
		workItemTypes: ["User Story", "Bug", "Task"],
		serviceLevelExpectationProbability: 85,
		serviceLevelExpectationRange: 14,
		systemWIPLimit: 3,
		remainingFeatures: 5,
	} as Team;

	const mockApiServiceContext = createMockApiServiceContext({});

	const renderWithProviders = (component: React.ReactElement) => {
		return render(
			<MemoryRouter>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					{component}
				</ApiServiceContext.Provider>
			</MemoryRouter>,
		);
	};

	it("should render TeamFeatureList component", () => {
		renderWithProviders(<TeamFeaturesView team={mockTeam} />);

		expect(screen.getByTestId("team-feature-list")).toBeInTheDocument();
		expect(
			screen.getByText("TeamFeatureList for Test Team"),
		).toBeInTheDocument();
	});

	it("should pass team prop to TeamFeatureList", () => {
		renderWithProviders(<TeamFeaturesView team={mockTeam} />);

		expect(screen.getByTestId("team-feature-list")).toBeInTheDocument();
	});
});
