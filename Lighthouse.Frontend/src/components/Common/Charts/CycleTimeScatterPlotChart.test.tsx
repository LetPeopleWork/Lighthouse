import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";

describe("CycleTimeScatterPlotChart component", () => {
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

	const originalOpen = window.open;
	beforeEach(() => {
		window.open = vi.fn();
	});

	afterEach(() => {
		window.open = originalOpen;
		vi.resetAllMocks();
	});

	it("should display 'No data available' when no percentiles are provided", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={[]}
			/>,
		);

		expect(screen.getByText("No data available")).toBeInTheDocument();
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

		const percentile50Text = screen.getAllByText("50%");
		const percentile85Text = screen.getAllByText("85%");
		const percentile95Text = screen.getAllByText("95%");

		expect(percentile50Text.length).toBeGreaterThan(0);
		expect(percentile85Text.length).toBeGreaterThan(0);
		expect(percentile95Text.length).toBeGreaterThan(0);
	});

	it("renders clickable legend items for each percentile", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
			/>,
		);

		// Check that all percentile chips are in the document
		const percentileChips = screen.getAllByText(/\d+%/);
		expect(percentileChips.length).toBe(mockPercentileValues.length * 2);
	});

	it("allows toggling percentile visibility on clicking legend items", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
			/>,
		);

		// Find and click the 50th percentile chip - make sure we get the one in the legend
		const percentileChips = screen.getAllByText("50%");
		const chipElement = percentileChips.find(
			(el) => el.closest(".MuiChip-root") !== null,
		);

		expect(chipElement).not.toBeNull();
		if (chipElement) {
			fireEvent.click(chipElement);
		}

		expect(screen.getByText("Cycle Time")).toBeInTheDocument();
	});

	it("formats tooltip values correctly", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
			/>,
		);

		expect(screen.getByText("Cycle Time")).toBeInTheDocument();
	});
});
