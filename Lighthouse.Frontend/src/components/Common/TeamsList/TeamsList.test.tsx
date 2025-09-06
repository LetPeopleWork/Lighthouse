import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ITeam } from "../../../models/Team/Team";
import TeamsList from "./TeamsList";

describe("TeamsList", () => {
	const mockOnSelectionChange = vi.fn();
	const allTeams: ITeam[] = [
		{
			id: 1,
			name: "Team Alpha",
			projects: [],
			features: [],
			featureWip: 1,
			remainingFeatures: 1,
			lastUpdated: new Date(),
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(
				new Date().setDate(new Date().getDate() - [1].length),
			),
			throughputEndDate: new Date(),
			tags: [],
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
			workItemTypes: ["User Story", "Bug"],
		},
		{
			id: 2,
			name: "Team Beta",
			projects: [],
			features: [],
			featureWip: 1,
			remainingFeatures: 1,
			lastUpdated: new Date(),
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(
				new Date().setDate(new Date().getDate() - [1].length),
			),
			throughputEndDate: new Date(),
			tags: [],
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
			workItemTypes: ["Task", "Feature"],
		},
		{
			id: 3,
			name: "Team Gamma",
			projects: [],
			features: [],
			featureWip: 1,
			remainingFeatures: 1,
			lastUpdated: new Date(),
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(
				new Date().setDate(new Date().getDate() - [1].length),
			),
			throughputEndDate: new Date(),
			tags: [],
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
			workItemTypes: ["Epic", "User Story"],
		},
	];
	const selectedTeams = [1, 3];

	beforeEach(() => {
		mockOnSelectionChange.mockClear();
	});

	it("renders correctly with teams and search input", () => {
		render(
			<TeamsList
				allTeams={allTeams}
				selectedTeams={selectedTeams}
				onSelectionChange={mockOnSelectionChange}
			/>,
		);

		expect(screen.getByText("Involved Teams")).toBeInTheDocument();
		expect(screen.getByLabelText("Search Teams")).toBeInTheDocument();

		for (const team of allTeams) {
			expect(screen.getByText(team.name)).toBeInTheDocument();
		}

		for (const teamId of selectedTeams) {
			const team = allTeams.find((t) => t.id === teamId);
			if (team?.name) {
				expect(screen.getByLabelText(team.name)).toBeChecked();
			}
		}
	});

	it("filters teams based on search input", () => {
		render(
			<TeamsList
				allTeams={allTeams}
				selectedTeams={selectedTeams}
				onSelectionChange={mockOnSelectionChange}
			/>,
		);

		const searchInput = screen.getByLabelText("Search Teams");
		fireEvent.change(searchInput, { target: { value: "Beta" } });

		expect(screen.queryByText("Team Alpha")).not.toBeInTheDocument();
		expect(screen.getByText("Team Beta")).toBeInTheDocument();
		expect(screen.queryByText("Team Gamma")).not.toBeInTheDocument();
	});

	it("calls onSelectionChange when a team is selected", () => {
		render(
			<TeamsList
				allTeams={allTeams}
				selectedTeams={selectedTeams}
				onSelectionChange={mockOnSelectionChange}
			/>,
		);

		const checkbox = screen.getByLabelText("Team Beta");
		fireEvent.click(checkbox);

		expect(mockOnSelectionChange).toHaveBeenCalledWith([...selectedTeams, 2]);
		expect(mockOnSelectionChange).toHaveBeenCalledTimes(1);
	});

	it("calls onSelectionChange when a team is deselected", () => {
		render(
			<TeamsList
				allTeams={allTeams}
				selectedTeams={selectedTeams}
				onSelectionChange={mockOnSelectionChange}
			/>,
		);

		const checkbox = screen.getByLabelText("Team Gamma");
		fireEvent.click(checkbox);

		expect(mockOnSelectionChange).toHaveBeenCalledWith([1]);
		expect(mockOnSelectionChange).toHaveBeenCalledTimes(1);
	});

	it("handles no matching teams in search", () => {
		render(
			<TeamsList
				allTeams={allTeams}
				selectedTeams={selectedTeams}
				onSelectionChange={mockOnSelectionChange}
			/>,
		);

		const searchInput = screen.getByLabelText("Search Teams");
		fireEvent.change(searchInput, { target: { value: "Nonexistent" } });

		for (const team of allTeams) {
			expect(screen.queryByText(team.name)).not.toBeInTheDocument();
		}
	});
});
