import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import InvolvedTeamsList from "./InvolvedTeamsList";

describe("InvolvedTeamsList component", () => {
	const teams: ITeamSettings[] = [
		{
			id: 1,
			name: "Team 1",
			throughputHistory: 30,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			featureWIP: 2,
			workItemQuery: "",
			workItemTypes: [],
			workTrackingSystemConnectionId: 5,
			relationCustomField: "",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			tags: [],
			automaticallyAdjustFeatureWIP: false,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
		},
		{
			id: 2,
			name: "Team 2",
			throughputHistory: 30,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			featureWIP: 1,
			workItemQuery: "",
			workItemTypes: [],
			tags: [],
			workTrackingSystemConnectionId: 5,
			relationCustomField: "",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			automaticallyAdjustFeatureWIP: false,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
		},
	];

	it("should render without errors when there are teams", () => {
		render(
			<MemoryRouter>
				<InvolvedTeamsList
					teams={teams}
					initiallyExpanded={true}
					onTeamUpdated={() => new Promise((resolve) => setTimeout(resolve, 0))}
				/>
			</MemoryRouter>,
		);

		expect(
			screen.getByText("Involved Teams (Feature WIP)"),
		).toBeInTheDocument();

		for (const team of teams) {
			const teamLink = screen.getByRole("link", { name: team.name });
			expect(teamLink).toBeInTheDocument();
			expect(teamLink).toHaveAttribute("href", `/teams/${team.id}`);

			const wipField = screen.getByLabelText(team.name);
			expect(wipField).toBeInTheDocument();
			expect(wipField).toHaveAttribute("type", "number");
			expect(wipField).toBeEnabled();
			expect(wipField).toHaveValue(team.featureWIP);
		}
	});

	it("should render nothing when there are no teams", () => {
		render(
			<MemoryRouter>
				<InvolvedTeamsList
					teams={[]}
					initiallyExpanded={true}
					onTeamUpdated={() => new Promise((resolve) => setTimeout(resolve, 0))}
				/>
			</MemoryRouter>,
		);

		expect(
			screen.queryByText("Involved Teams (Feature WIP)"),
		).not.toBeInTheDocument();
	});

	it("should update team with new settings on change", () => {
		const updateTeam = vi.fn();

		render(
			<MemoryRouter>
				<InvolvedTeamsList
					teams={teams}
					initiallyExpanded={true}
					onTeamUpdated={updateTeam}
				/>
			</MemoryRouter>,
		);

		fireEvent.change(screen.getByLabelText(/Team 1/i), {
			target: { value: "5" },
		});

		expect(updateTeam).toHaveBeenCalledWith(
			expect.objectContaining({
				id: 1,
				name: "Team 1",
				featureWIP: 5,
			}),
		);
	});
});
