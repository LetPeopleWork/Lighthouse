import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IEntityReference } from "../../../models/EntityReference";
import { Feature } from "../../../models/Feature";
import FeatureProgressIndicator from "./FeatureProgressIndicator";

vi.mock("../ProgressIndicator/ProgressIndicator", () => ({
	default: ({
		title,
		progressableItem,
	}: {
		title: React.ReactNode;
		progressableItem: { remainingWork: number; totalWork: number };
	}) => (
		<div data-testid="progress-indicator">
			<span data-testid="progress-title">{title}</span>
			<span data-testid="progress-value">
				{progressableItem.remainingWork}/{progressableItem.totalWork}
			</span>
		</div>
	),
}));

vi.mock("../ProgressIndicator/ProgressTitle", () => ({
	default: ({
		title,
		onShowDetails,
	}: {
		title: string;
		isUsingDefaultFeatureSize: boolean;
		onShowDetails: () => void;
	}) => (
		<button
			type="button"
			data-testid="progress-title-button"
			onClick={onShowDetails}
		>
			{title}
		</button>
	),
}));

const makeFeature = (
	workByTeam: Record<number, { remaining: number; total: number }>,
): Feature => {
	const feature = new Feature();
	feature.id = 1;
	feature.name = "Test Feature";
	feature.referenceId = "FTR-1";
	feature.remainingWork = Object.fromEntries(
		Object.entries(workByTeam).map(([id, w]) => [id, w.remaining]),
	);
	feature.totalWork = Object.fromEntries(
		Object.entries(workByTeam).map(([id, w]) => [id, w.total]),
	);
	feature.forecasts = [];
	feature.isUsingDefaultFeatureSize = false;
	feature.stateCategory = "Doing";
	feature.lastUpdated = new Date();
	return feature;
};

const team1: IEntityReference = { id: 1, name: "Team Alpha" };
const team2: IEntityReference = { id: 2, name: "Team Beta" };

beforeEach(() => {
	localStorage.clear();
});

describe("FeatureProgressIndicator", () => {
	describe("single team, all work belongs to that team", () => {
		it("renders only one progress bar (the team bar)", () => {
			const feature = makeFeature({ 1: { remaining: 3, total: 10 } });

			render(
				<MemoryRouter>
					<FeatureProgressIndicator feature={feature} teams={[team1]} />
				</MemoryRouter>,
			);

			const bars = screen.getAllByTestId("progress-indicator");
			expect(bars).toHaveLength(1);
		});

		it("shows the team name as bar title", () => {
			const feature = makeFeature({ 1: { remaining: 3, total: 10 } });

			render(
				<MemoryRouter>
					<FeatureProgressIndicator feature={feature} teams={[team1]} />
				</MemoryRouter>,
			);

			expect(screen.getByText("Team Alpha")).toBeInTheDocument();
		});

		it("does not render an overall/aggregate bar", () => {
			const feature = makeFeature({ 1: { remaining: 3, total: 10 } });

			render(
				<MemoryRouter>
					<FeatureProgressIndicator feature={feature} teams={[team1]} />
				</MemoryRouter>,
			);

			expect(
				screen.queryByTestId("progress-title-button"),
			).not.toBeInTheDocument();
			expect(screen.queryByText("Overall Progress")).not.toBeInTheDocument();
		});

		it("attaches onShowDetails to the single team bar when provided", async () => {
			const onShowDetails = vi.fn();
			const feature = makeFeature({ 1: { remaining: 3, total: 10 } });

			render(
				<MemoryRouter>
					<FeatureProgressIndicator
						feature={feature}
						teams={[team1]}
						onShowDetails={onShowDetails}
					/>
				</MemoryRouter>,
			);

			// Still only one bar
			expect(screen.getAllByTestId("progress-indicator")).toHaveLength(1);
			// ProgressTitle rendered with team name
			const button = screen.getByTestId("progress-title-button");
			expect(button).toHaveTextContent("Team Alpha");
			await userEvent.click(button);
			expect(onShowDetails).toHaveBeenCalledTimes(1);
		});

		it("links the team bar to the correct team route", () => {
			const feature = makeFeature({ 1: { remaining: 3, total: 10 } });

			render(
				<MemoryRouter>
					<FeatureProgressIndicator feature={feature} teams={[team1]} />
				</MemoryRouter>,
			);

			const link = screen.getByRole("link", { name: "Team Alpha" });
			expect(link).toHaveAttribute("href", "/teams/1");
		});
	});

	describe("multiple teams", () => {
		it("renders an aggregate bar plus one bar per team with work", () => {
			const feature = makeFeature({
				1: { remaining: 3, total: 8 },
				2: { remaining: 2, total: 5 },
			});

			render(
				<MemoryRouter>
					<FeatureProgressIndicator feature={feature} teams={[team1, team2]} />
				</MemoryRouter>,
			);

			expect(screen.getAllByTestId("progress-indicator")).toHaveLength(3);
		});

		it("shows the default overallTitle", () => {
			const feature = makeFeature({
				1: { remaining: 3, total: 8 },
				2: { remaining: 2, total: 5 },
			});

			render(
				<MemoryRouter>
					<FeatureProgressIndicator feature={feature} teams={[team1, team2]} />
				</MemoryRouter>,
			);

			expect(screen.getByText("Overall Progress")).toBeInTheDocument();
		});

		it("uses a custom overallTitle when provided", () => {
			const feature = makeFeature({
				1: { remaining: 3, total: 8 },
				2: { remaining: 2, total: 5 },
			});

			render(
				<MemoryRouter>
					<FeatureProgressIndicator
						feature={feature}
						teams={[team1, team2]}
						overallTitle="Total"
					/>
				</MemoryRouter>,
			);

			expect(screen.getByText("Total")).toBeInTheDocument();
		});

		it("renders a clickable ProgressTitle when onShowDetails is provided", async () => {
			const onShowDetails = vi.fn();
			const feature = makeFeature({
				1: { remaining: 3, total: 8 },
				2: { remaining: 2, total: 5 },
			});

			render(
				<MemoryRouter>
					<FeatureProgressIndicator
						feature={feature}
						teams={[team1, team2]}
						onShowDetails={onShowDetails}
					/>
				</MemoryRouter>,
			);

			const button = screen.getByTestId("progress-title-button");
			await userEvent.click(button);
			expect(onShowDetails).toHaveBeenCalledTimes(1);
		});

		it("collapses to one bar when only one team has work, even if multiple teams are passed", () => {
			const feature = makeFeature({
				1: { remaining: 3, total: 8 },
				2: { remaining: 0, total: 0 },
			});

			render(
				<MemoryRouter>
					<FeatureProgressIndicator feature={feature} teams={[team1, team2]} />
				</MemoryRouter>,
			);

			// team1 is the only team with work and its total === feature total → single bar
			expect(screen.getAllByTestId("progress-indicator")).toHaveLength(1);
			expect(screen.getByText("Team Alpha")).toBeInTheDocument();
			expect(screen.queryByText("Team Beta")).not.toBeInTheDocument();
		});
	});

	describe("single team where work does not cover the full feature total", () => {
		it("shows the aggregate bar when a second team has work but is not in the teams list", () => {
			const feature = makeFeature({
				1: { remaining: 3, total: 8 },
				2: { remaining: 2, total: 5 },
			});

			render(
				<MemoryRouter>
					<FeatureProgressIndicator feature={feature} teams={[team1]} />
				</MemoryRouter>,
			);

			// team1.totalWork (8) !== feature.totalWork (13) → aggregate bar shown
			const bars = screen.getAllByTestId("progress-indicator");
			expect(bars).toHaveLength(2); // aggregate + team1
		});
	});

	describe("no teams with work", () => {
		it("renders only the aggregate bar", () => {
			const feature = makeFeature({
				1: { remaining: 0, total: 0 },
			});

			render(
				<MemoryRouter>
					<FeatureProgressIndicator feature={feature} teams={[team1]} />
				</MemoryRouter>,
			);

			const bars = screen.getAllByTestId("progress-indicator");
			expect(bars).toHaveLength(1); // just the aggregate
		});
	});
});
