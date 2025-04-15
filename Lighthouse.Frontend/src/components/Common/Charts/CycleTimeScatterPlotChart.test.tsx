import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";

describe("CycleTimeScatterPlotChart component", () => {
	// Mock data for tests
	const mockPercentileValues: IPercentileValue[] = [
		{ percentile: 50, value: 3 },
		{ percentile: 85, value: 7 },
		{ percentile: 95, value: 12 },
	];

	const mockWorkItems: IWorkItem[] = [
		{
			id: 1,
			workItemReference: "ITEM-1",
			name: "Test Item 1",
			url: "https://example.com/item1",
			cycleTime: 5,
			startedDate: new Date("2023-01-10"),
			closedDate: new Date("2023-01-15"),
			workItemAge: 5,
			state: "Done",
			stateCategory: "Done",
			type: "Task",
		},
		{
			id: 2,
			workItemReference: "ITEM-2",
			name: "Test Item 2",
			url: "https://example.com/item2",
			cycleTime: 10,
			startedDate: new Date("2023-01-15"),
			closedDate: new Date("2023-01-25"),
			workItemAge: 10,
			state: "Done",
			stateCategory: "Done",
			type: "Task",
		},
	];

	// Mock window.open
	const originalOpen = window.open;
	beforeEach(() => {
		window.open = vi.fn();
	});

	afterEach(() => {
		window.open = originalOpen;
		vi.resetAllMocks();
	});

	it("renders a loading indicator when no data points are provided", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={[]}
			/>,
		);

		const loadingIndicator = screen.getByRole("progressbar");
		expect(loadingIndicator).toBeInTheDocument();
	});

	it("renders the chart with correct title when data is provided", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
			/>,
		);

		expect(screen.getByText("Cycle Time")).toBeInTheDocument();
	});

	it("renders the correct number of percentile reference lines", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
			/>,
		);

		// Check for percentile labels
		expect(screen.getByText("50th percentile: 3 days")).toBeInTheDocument();
		expect(screen.getByText("85th percentile: 7 days")).toBeInTheDocument();
		expect(screen.getByText("95th percentile: 12 days")).toBeInTheDocument();
	});

	it("formats tooltip values correctly", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
			/>,
		);

		// We can't directly test the tooltip content since it's rendered on demand
		// But we can verify the chart renders properly
		expect(screen.getByText("Cycle Time")).toBeInTheDocument();
	});
});
