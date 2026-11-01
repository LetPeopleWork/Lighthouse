import { act, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import { COULD_NOT_LOAD_MESSAGE } from "../../Common/MetricsView/WidgetShell";
import TeamMetricsView from "./TeamMetricsView";

interface BaseMetricsViewProps {
	doingStates: string[];
	doingStatesStatus?: "loading" | "error" | "ready";
}

// The aging widget's frame is the real one, so the words on screen are the words a reader sees.
vi.mock("../../Common/MetricsView/BaseMetricsView", async () => {
	const { default: WidgetShell } = await import(
		"../../Common/MetricsView/WidgetShell"
	);
	return {
		BaseMetricsView: ({
			doingStates,
			doingStatesStatus,
		}: BaseMetricsViewProps) => (
			<WidgetShell widgetKey="aging" status={doingStatesStatus}>
				<div data-testid="doing-columns">{doingStates.join("|")}</div>
			</WidgetShell>
		),
	};
});

const teamNamed = (id: number) => {
	const team = new Team();
	team.id = id;
	team.name = `Team ${id}`;
	return team;
};

const settingsWith = (doing: string[], display: string[]) => ({
	...createMockTeamSettings(),
	doingStates: doing,
	doingDisplayStates: display,
});

const statusOfTheFrame = () =>
	screen.getByTestId("widget-shell-aging").dataset.widgetStatus;

describe("TeamMetricsView aging columns", () => {
	const teamMetricsService = createMockTeamMetricsService();
	const getTeamSettings = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
		teamMetricsService.getFeaturesInProgress = vi.fn().mockResolvedValue([]);
	});

	const view = (team: Team) => {
		const teamService = createMockTeamService();
		teamService.getTeamSettings = getTeamSettings;
		return (
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ teamMetricsService, teamService })}
			>
				<TeamMetricsView team={team} />
			</ApiServiceContext.Provider>
		);
	};

	it.skip("shows a loading frame, and no columns, while the settings are on their way", () => {
		getTeamSettings.mockReturnValue(new Promise(() => {}));

		render(view(teamNamed(1)));

		expect(statusOfTheFrame()).toBe("loading");
		expect(screen.getByTestId("doing-columns")).toBeEmptyDOMElement();
	});

	it.skip("says the columns could not be loaded when the settings fetch fails", async () => {
		getTeamSettings.mockRejectedValue(new Error("offline"));

		render(view(teamNamed(1)));

		await waitFor(() => expect(statusOfTheFrame()).toBe("error"));
		expect(screen.getByText(COULD_NOT_LOAD_MESSAGE)).toBeVisible();
	});

	it.skip("shows the columns as the settings display them, groups in place of their states", async () => {
		getTeamSettings.mockResolvedValue(
			settingsWith(
				["Analysis", "Code Review", "Testing"],
				["Analysis", "Review"],
			),
		);

		render(view(teamNamed(1)));

		await waitFor(() => expect(statusOfTheFrame()).toBe("ready"));
		expect(screen.getByTestId("doing-columns")).toHaveTextContent(
			"Analysis|Review",
		);
	});

	it.skip("does not let a late answer for the team it left replace the columns of the team it shows", async () => {
		let answerForTeamOne: (settings: unknown) => void = () => {};
		getTeamSettings.mockImplementation((id: number) =>
			id === 1
				? new Promise((resolve) => {
						answerForTeamOne = resolve;
					})
				: Promise.resolve(settingsWith(["B"], ["B"])),
		);

		const { rerender } = render(view(teamNamed(1)));
		rerender(view(teamNamed(2)));
		await waitFor(() =>
			expect(screen.getByTestId("doing-columns")).toHaveTextContent("B"),
		);

		await act(async () => answerForTeamOne(settingsWith(["A"], ["A"])));

		expect(screen.getByTestId("doing-columns")).toHaveTextContent("B");
	});
});
