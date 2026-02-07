import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import PortfolioFeatureWipQuickSetting from "./PortfolioFeatureWipQuickSetting";

const getMockTeam = (overrides?: Partial<ITeamSettings>): ITeamSettings => ({
	id: 1,
	name: "Team 1",
	featureWIP: 1,
	automaticallyAdjustFeatureWIP: false,
	throughputHistory: 30,
	useFixedDatesForThroughput: false,
	throughputHistoryStartDate: new Date("2025-01-01"),
	throughputHistoryEndDate: new Date("2025-01-15"),
	serviceLevelExpectationProbability: 85,
	serviceLevelExpectationRange: 10,
	systemWIPLimit: 0,
	dataRetrievalValue: "",
	workItemTypes: [],
	workTrackingSystemConnectionId: 1,
	parentOverrideAdditionalFieldDefinitionId: null,
	toDoStates: [],
	doingStates: [],
	doneStates: [],
	tags: [],
	blockedStates: [],
	blockedTags: [],
	doneItemsCutoffDays: 180,
	processBehaviourChartBaselineStartDate: null,
	processBehaviourChartBaselineEndDate: null,
	...overrides,
});

const getMockProps = (
	overrides?: Partial<{
		teams: ITeamSettings[];
		onSave: (teamId: number, featureWip: number) => Promise<void>;
		disabled: boolean;
	}>,
) => ({
	teams: [getMockTeam(), getMockTeam({ id: 2, name: "Team 2", featureWIP: 2 })],
	onSave: vi.fn().mockResolvedValue(undefined),
	disabled: false,
	...overrides,
});

describe("PortfolioFeatureWipQuickSetting", () => {
	it("should render icon button with tooltip showing team count", () => {
		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting {...getMockProps()} />
			</MemoryRouter>,
		);

		expect(
			screen.getByRole("button", { name: /Feature WIP: 2 teams/i }),
		).toBeInTheDocument();
	});

	it("should show greyed icon when all teams have Feature WIP unset (0)", () => {
		const teams = [
			getMockTeam({ featureWIP: 0 }),
			getMockTeam({ id: 2, name: "Team 2", featureWIP: 0 }),
		];

		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting {...getMockProps({ teams })} />
			</MemoryRouter>,
		);

		const button = screen.getByRole("button", {
			name: /Feature WIP: Not set/i,
		});
		expect(button).toBeInTheDocument();
	});

	it("should open dialog showing all teams when clicked", async () => {
		const user = userEvent.setup();
		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting {...getMockProps()} />
			</MemoryRouter>,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		expect(
			screen.getByRole("heading", { name: /Feature WIP per Team/i }),
		).toBeInTheDocument();
		expect(screen.getByLabelText(/Team 1/i)).toBeInTheDocument();
		expect(screen.getByLabelText(/Team 2/i)).toBeInTheDocument();
	});

	it("should display current Feature WIP values for each team", async () => {
		const user = userEvent.setup();
		const teams = [
			getMockTeam({ id: 1, name: "Alpha", featureWIP: 3 }),
			getMockTeam({ id: 2, name: "Beta", featureWIP: 5 }),
		];

		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting {...getMockProps({ teams })} />
			</MemoryRouter>,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const alphaInput = screen.getByLabelText(/Alpha/i);
		const betaInput = screen.getByLabelText(/Beta/i);

		expect(alphaInput).toHaveValue(3);
		expect(betaInput).toHaveValue(5);
	});

	it("should call onSave for modified teams when Enter is pressed", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting
					{...getMockProps({ onSave: mockOnSave })}
				/>
			</MemoryRouter>,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const team1Input = screen.getByLabelText(/Team 1/i);
		await user.clear(team1Input);
		await user.type(team1Input, "5");

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(mockOnSave).toHaveBeenCalledWith(1, 5);
			expect(mockOnSave).toHaveBeenCalledTimes(1);
		});
	});

	it("should call onSave for all modified teams", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting
					{...getMockProps({ onSave: mockOnSave })}
				/>
			</MemoryRouter>,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const team1Input = screen.getByLabelText(/Team 1/i);
		const team2Input = screen.getByLabelText(/Team 2/i);

		await user.clear(team1Input);
		await user.type(team1Input, "3");

		await user.clear(team2Input);
		await user.type(team2Input, "4");

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(mockOnSave).toHaveBeenCalledWith(1, 3);
			expect(mockOnSave).toHaveBeenCalledWith(2, 4);
			expect(mockOnSave).toHaveBeenCalledTimes(2);
		});
	});

	it("should not call onSave when Esc is pressed", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting
					{...getMockProps({ onSave: mockOnSave })}
				/>
			</MemoryRouter>,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const team1Input = screen.getByLabelText(/Team 1/i);
		await user.clear(team1Input);
		await user.type(team1Input, "10");

		await user.keyboard("{Escape}");

		expect(mockOnSave).not.toHaveBeenCalled();
	});

	it("should not call onSave when no values changed", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting
					{...getMockProps({ onSave: mockOnSave })}
				/>
			</MemoryRouter>,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));
		await user.keyboard("{Enter}");

		expect(mockOnSave).not.toHaveBeenCalled();
	});

	it("should be disabled when disabled prop is true", () => {
		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting
					{...getMockProps({ disabled: true })}
				/>
			</MemoryRouter>,
		);

		const button = screen.getByRole("button", { name: /Feature WIP/i });
		expect(button).toBeDisabled();
	});

	it("should allow setting Feature WIP to 0", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting
					{...getMockProps({ onSave: mockOnSave })}
				/>
			</MemoryRouter>,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const team1Input = screen.getByLabelText(/Team 1/i);
		await user.clear(team1Input);
		await user.type(team1Input, "0");

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(mockOnSave).toHaveBeenCalledWith(1, 0);
		});
	});

	it("should render team names as links to team detail pages", async () => {
		const user = userEvent.setup();
		const teams = [
			getMockTeam({ id: 10, name: "Alpha Team" }),
			getMockTeam({ id: 20, name: "Beta Team" }),
		];

		render(
			<MemoryRouter>
				<PortfolioFeatureWipQuickSetting {...getMockProps({ teams })} />
			</MemoryRouter>,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const alphaLink = screen.getByRole("link", { name: /Alpha Team/i });
		const betaLink = screen.getByRole("link", { name: /Beta Team/i });

		expect(alphaLink).toHaveAttribute("href", "/teams/10");
		expect(betaLink).toHaveAttribute("href", "/teams/20");
	});
});
