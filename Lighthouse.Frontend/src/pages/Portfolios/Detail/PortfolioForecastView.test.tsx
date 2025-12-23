import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { Team } from "../../../models/Team/Team";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import PortfolioForecastView from "./PortfolioForecastView";

vi.mock("./InvolvedTeamsList", () => ({
	default: ({
		teams,
		onTeamUpdated,
	}: {
		teams: ITeamSettings[];
		onTeamUpdated: (team: ITeamSettings) => void;
	}) => (
		<div data-testid="involved-teams-list">
			<div data-testid="teams-count">{teams.length}</div>
			{teams.map((team) => (
				<div key={team.id} data-testid={`team-${team.id}`}>
					<span>{team.name}</span>
					<button
						type="button"
						data-testid={`update-team-${team.id}`}
						onClick={() =>
							onTeamUpdated({ ...team, name: `${team.name}-updated` })
						}
					>
						Update Team
					</button>
				</div>
			))}
		</div>
	),
}));

vi.mock("./PortfolioFeatureList", () => ({
	default: ({ portfolio }: { portfolio: Portfolio }) => (
		<div data-testid="portfolio-feature-list">
			<div data-testid="portfolio-name">{portfolio.name}</div>
			<div data-testid="features-count">{portfolio.features.length}</div>
		</div>
	),
}));

describe("PortfolioForecastView component", () => {
	const mockTeam1: Team = (() => {
		const team = new Team();
		team.name = "Team A";
		team.id = 1;
		team.projects = [];
		team.features = [];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date();
		team.throughputEndDate = new Date();
		return team;
	})();

	const mockTeam2: Team = (() => {
		const team = new Team();
		team.name = "Team B";
		team.id = 2;
		team.projects = [];
		team.features = [];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date();
		team.throughputEndDate = new Date();
		return team;
	})();

	const mockPortfolio = (() => {
		const portfolio = new Portfolio();
		portfolio.name = "Test Project";
		portfolio.id = 1;
		portfolio.involvedTeams = [mockTeam1, mockTeam2];
		portfolio.lastUpdated = new Date();
		return portfolio;
	})();

	const mockTeamSettings1 = createMockTeamSettings();
	mockTeamSettings1.id = 1;
	mockTeamSettings1.name = "Team A Settings";

	const mockTeamSettings2 = createMockTeamSettings();
	mockTeamSettings2.id = 2;
	mockTeamSettings2.name = "Team B Settings";

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders all components correctly", () => {
		render(
			<MemoryRouter>
				<PortfolioForecastView portfolio={mockPortfolio} />
			</MemoryRouter>,
		);

		expect(screen.getByTestId("portfolio-feature-list")).toBeInTheDocument();

		// Check if data is passed correctly
		expect(screen.getByTestId("portfolio-name").textContent).toBe(
			"Test Project",
		);
	});
});
