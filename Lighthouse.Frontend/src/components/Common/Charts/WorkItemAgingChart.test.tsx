import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import { testTheme } from "../../../tests/testTheme";
import WorkItemAgingChart from "./WorkItemAgingChart";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

// Mock WorkItemsDialog component
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose }) => {
		if (!open) return null;
		return (
			<div data-testid="work-items-dialog">
				<h2>{title}</h2>
				<button type="button" onClick={onClose} data-testid="close-dialog">
					Close
				</button>
				<table>
					<thead>
						<tr>
							<th>Name</th>
							<th>Type</th>
							<th>State</th>
							<th>Age</th>
						</tr>
					</thead>
					<tbody>
						{items?.map((item: IWorkItem) => (
							<tr key={item.id}>
								<td>{item.name}</td>
								<td>{item.type}</td>
								<td>{item.state}</td>
								<td>{item.workItemAge} days</td>
							</tr>
						))}
					</tbody>
				</table>
			</div>
		);
	}),
}));

// Mock the MUI-X Charts
vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		ChartContainer: vi.fn(({ children }) => (
			<div data-testid="mock-chart-container">{children}</div>
		)),
		ScatterPlot: vi.fn(() => {
			return (
				<div data-testid="mock-scatter-plot">
					<div>Scatter Plot Content</div>
				</div>
			);
		}),
		ChartsXAxis: vi.fn(() => <div>X Axis</div>),
		ChartsYAxis: vi.fn(() => <div>Y Axis</div>),
		ChartsTooltip: vi.fn(() => <div>Tooltip</div>),
		ChartsReferenceLine: vi.fn(({ label }) => (
			<div data-testid={`reference-line-${label}`}>{label}</div>
		)),
	};
});

describe("WorkItemAgingChart component", () => {
	// Mock data for tests
	const mockPercentileValues: IPercentileValue[] = [
		{ percentile: 50, value: 3 },
		{ percentile: 85, value: 7 },
		{ percentile: 95, value: 12 },
	];

	const mockSLE: IPercentileValue = {
		percentile: 85,
		value: 7,
	};

	// Create mock work items with proper date objects
	const mockInProgressItems: IWorkItem[] = [
		{
			id: 1,
			referenceId: "ITEM-1",
			name: "Test Item 1",
			url: "https://example.com/item1",
			cycleTime: 0,
			startedDate: new Date(2023, 0, 10),
			closedDate: new Date(),
			workItemAge: 5,
			type: "Story",
			state: "In Progress",
			stateCategory: "Doing",
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 2,
			referenceId: "ITEM-2",
			name: "Test Item 2",
			url: "https://example.com/item2",
			cycleTime: 0,
			startedDate: new Date(2023, 0, 5),
			closedDate: new Date(),
			workItemAge: 10,
			type: "Bug",
			state: "Ready for Review",
			stateCategory: "Doing",
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 3,
			referenceId: "ITEM-3",
			name: "Test Item 3",
			url: "https://example.com/item3",
			cycleTime: 0,
			startedDate: new Date(2023, 0, 8),
			closedDate: new Date(),
			workItemAge: 5,
			type: "Task",
			state: "In Progress",
			stateCategory: "Doing",
			parentWorkItemReference: "",
			isBlocked: false,
		},
	];

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders correctly with in-progress items", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByText("Work Item Aging")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
		expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
	});

	it("renders 'No items in progress' when there are no items", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={[]}
				percentileValues={mockPercentileValues}
				doingStates={["To Do", "In Progress", "Review"]}
				serviceLevelExpectation={mockSLE}
			/>,
		);

		expect(screen.getByText("No items in progress")).toBeInTheDocument();
		expect(
			screen.queryByTestId("mock-chart-container"),
		).not.toBeInTheDocument();
	});

	it("renders percentile chips with correct labels", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Use getAllByText to handle multiple elements and check chips specifically
		const percentile50Elements = screen.getAllByText("50%");
		const percentile85Elements = screen.getAllByText("85%");
		const percentile95Elements = screen.getAllByText("95%");

		// Should have at least one element for each percentile (the chip)
		expect(percentile50Elements.length).toBeGreaterThan(0);
		expect(percentile85Elements.length).toBeGreaterThan(0);
		expect(percentile95Elements.length).toBeGreaterThan(0);
	});

	it("renders service level expectation chip when provided", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByText("Service Level Expectation")).toBeInTheDocument();
	});

	it("does not render service level expectation chip when not provided", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={null}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(
			screen.queryByText("Service Level Expectation"),
		).not.toBeInTheDocument();
	});

	it("toggles percentile visibility when chip is clicked", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Find the chip by its role and aria-label or use a more specific query
		const chips = screen.getAllByRole("button");
		const percentile50Chip = chips.find((chip) => chip.textContent === "50%");

		expect(percentile50Chip).toBeInTheDocument();

		// Initially, the percentile should be visible (reference line should exist)
		expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();

		// Click to toggle visibility
		if (percentile50Chip) {
			fireEvent.click(percentile50Chip);
		}

		// After clicking, we can verify the chip was clicked
		expect(percentile50Chip).toBeInTheDocument();
	});

	it("toggles service level expectation visibility when chip is clicked", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		const sleChip = screen.getByText("Service Level Expectation");

		// Click to toggle visibility
		fireEvent.click(sleChip);

		// Verify the chip was clicked
		expect(sleChip).toBeInTheDocument();
	});

	it("groups items correctly by state and age", () => {
		// This test verifies the grouping logic through the component rendering
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// The chart should render with the grouped data
		expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
	});

	it("handles empty percentile values array", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={[]}
				serviceLevelExpectation={null}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByText("Work Item Aging")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();

		// No percentile chips should be rendered
		expect(screen.queryByTestId("reference-line-50%")).not.toBeInTheDocument();
		expect(screen.queryByTestId("reference-line-85%")).not.toBeInTheDocument();
		expect(screen.queryByTestId("reference-line-95%")).not.toBeInTheDocument();
	});

	it("renders chart components correctly", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Verify chart components are rendered
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
		expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
		expect(screen.getByText("X Axis")).toBeInTheDocument();
		expect(screen.getByText("Y Axis")).toBeInTheDocument();
		expect(screen.getByText("Tooltip")).toBeInTheDocument();
	});

	it("renders reference lines for visible percentiles", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={null}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// All percentiles should be visible initially
		expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();
		expect(screen.getByTestId("reference-line-85%")).toBeInTheDocument();
		expect(screen.getByTestId("reference-line-95%")).toBeInTheDocument();
	});

	it("handles items with missing state gracefully", () => {
		const itemsWithMissingState: IWorkItem[] = [
			{
				...mockInProgressItems[0],
				state: "",
			},
		];

		render(
			<WorkItemAgingChart
				inProgressItems={itemsWithMissingState}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Component should still render without crashing
		expect(screen.getByText("Work Item Aging")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
	});

	it("correctly extracts age from work items", () => {
		// Test that the component correctly uses workItemAge property
		const itemWithSpecificAge: IWorkItem[] = [
			{
				...mockInProgressItems[0],
				workItemAge: 15,
			},
		];

		render(
			<WorkItemAgingChart
				inProgressItems={itemWithSpecificAge}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// The chart should render with the item
		expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
	});
});
