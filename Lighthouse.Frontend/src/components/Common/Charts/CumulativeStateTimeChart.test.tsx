import { render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ICumulativeStateTimeStateRow } from "../../../models/Metrics/CumulativeStateTime";
import { testTheme } from "../../../tests/testTheme";
import CumulativeStateTimeChart from "./CumulativeStateTimeChart";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		BarChart: vi.fn(({ dataset, series, xAxis, children, onItemClick }) => (
			<div
				data-testid="mock-bar-chart"
				data-dataset={dataset ? JSON.stringify(dataset) : undefined}
				data-series={series ? JSON.stringify(series) : undefined}
				data-x-axis={xAxis ? JSON.stringify(xAxis) : undefined}
			>
				<button
					type="button"
					data-testid="bar-click-proxy"
					onClick={() => onItemClick?.(null, { dataIndex: 0 })}
				>
					bar
				</button>
				{children}
			</div>
		)),
	};
});

const getMockStateRow = (
	overrides?: Partial<ICumulativeStateTimeStateRow>,
): ICumulativeStateTimeStateRow => ({
	state: "Doing",
	workflowOrder: 0,
	totalDays: 12,
	completedContributionDays: 8,
	ongoingContributionDays: 4,
	itemCount: 5,
	completedItemCount: 3,
	ongoingItemCount: 2,
	meanDays: 2.4,
	medianDays: 2,
	...overrides,
});

const threeStatesInOrder: ICumulativeStateTimeStateRow[] = [
	getMockStateRow({ state: "Doing", workflowOrder: 2, totalDays: 30 }),
	getMockStateRow({ state: "Backlog", workflowOrder: 0, totalDays: 5 }),
	getMockStateRow({ state: "Review", workflowOrder: 1, totalDays: 14 }),
];

describe("CumulativeStateTimeChart", () => {
	it("renders one bar per workflow state ordered by workflow order", () => {
		render(<CumulativeStateTimeChart data={{ states: threeStatesInOrder }} />);

		const chart = screen.getByTestId("mock-bar-chart");
		const dataset = JSON.parse(chart.getAttribute("data-dataset") ?? "[]");

		expect(dataset.map((row: { state: string }) => row.state)).toEqual([
			"Backlog",
			"Review",
			"Doing",
		]);
	});

	it("stacks a completed segment and an ongoing segment per bar", () => {
		render(
			<CumulativeStateTimeChart
				data={{
					states: [
						getMockStateRow({
							completedContributionDays: 8,
							ongoingContributionDays: 4,
						}),
					],
				}}
			/>,
		);

		const chart = screen.getByTestId("mock-bar-chart");
		const series = JSON.parse(chart.getAttribute("data-series") ?? "[]");
		const dataset = JSON.parse(chart.getAttribute("data-dataset") ?? "[]");

		const stackIds = new Set(series.map((s: { stack?: string }) => s.stack));
		expect(stackIds.size).toBe(1);
		expect(series).toHaveLength(2);

		const completedKey = series[0].dataKey;
		const ongoingKey = series[1].dataKey;
		expect(dataset[0][completedKey]).toBe(8);
		expect(dataset[0][ongoingKey]).toBe(4);
	});

	it("renders an SVG hatch pattern for the ongoing segment", () => {
		const { container } = render(
			<CumulativeStateTimeChart data={{ states: [getMockStateRow()] }} />,
		);

		expect(container.querySelector("pattern")).not.toBeNull();
	});

	it("shows the per-state fields and completed/ongoing counts without an included-items line", () => {
		render(
			<CumulativeStateTimeChart
				data={{
					states: [
						getMockStateRow({
							state: "Review",
							itemCount: 5,
							completedItemCount: 3,
							ongoingItemCount: 2,
							meanDays: 2.4,
							medianDays: 2,
						}),
					],
				}}
			/>,
		);

		const tooltip = screen.getByTestId("cumulative-state-tooltip-Review");
		const completed = within(tooltip).getByTestId("tooltip-completed-count");
		const ongoing = within(tooltip).getByTestId("tooltip-ongoing-count");

		expect(tooltip.textContent).toContain("Review");
		expect(completed.textContent).toContain("3");
		expect(ongoing.textContent).toContain("2");
		expect(tooltip.textContent?.toLowerCase()).not.toContain("included items");
	});

	it("shows an empty-state placeholder when there are no states", () => {
		render(<CumulativeStateTimeChart data={{ states: [] }} />);

		expect(screen.queryByTestId("mock-bar-chart")).toBeNull();
		expect(
			screen.getByTestId("cumulative-state-time-empty"),
		).toBeInTheDocument();
	});

	it("shows a zero-contributing placeholder when every state has no recorded time", () => {
		render(
			<CumulativeStateTimeChart
				data={{
					states: [
						getMockStateRow({
							totalDays: 0,
							completedContributionDays: 0,
							ongoingContributionDays: 0,
						}),
					],
				}}
			/>,
		);

		expect(screen.queryByTestId("mock-bar-chart")).toBeNull();
		expect(
			screen.getByTestId("cumulative-state-time-zero"),
		).toBeInTheDocument();
	});

	it("formats bar labels with one adaptive unit chosen from the largest bar", () => {
		render(<CumulativeStateTimeChart data={{ states: threeStatesInOrder }} />);

		const chart = screen.getByTestId("mock-bar-chart");
		const xAxis = JSON.parse(chart.getAttribute("data-x-axis") ?? "[]");
		const series = JSON.parse(chart.getAttribute("data-series") ?? "[]");

		expect(JSON.stringify(xAxis)).toContain("w");
		expect(series[0].unit ?? series[0].label).toBeDefined();
	});

	it("invokes onBarClick with the state name when a bar is clicked", () => {
		const onBarClick = vi.fn();
		render(
			<CumulativeStateTimeChart
				data={{ states: [getMockStateRow({ state: "Doing" })] }}
				onBarClick={onBarClick}
			/>,
		);

		screen.getByTestId("bar-click-proxy").click();

		expect(onBarClick).toHaveBeenCalledWith("Doing");
	});

	it("renders the picker slot inside the chart toolbar", () => {
		render(
			<CumulativeStateTimeChart
				data={{ states: [getMockStateRow()] }}
				pickerSlot={<div data-testid="picker-slot">picker</div>}
			/>,
		);

		expect(screen.getByTestId("picker-slot")).toBeInTheDocument();
	});
});
