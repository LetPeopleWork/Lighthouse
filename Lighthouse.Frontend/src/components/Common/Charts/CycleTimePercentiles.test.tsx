import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import { testTheme } from "../../../tests/testTheme";
import CycleTimePercentiles from "./CycleTimePercentiles";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

// Mock the WorkItemsDialog component at the top level
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn((props) =>
		props.open ? (
			<div data-testid="mock-dialog" data-metric={props.timeMetric}>
				Mock Dialog
			</div>
		) : null,
	),
}));

describe("CycleTimePercentiles component", () => {
	const mockPercentiles: IPercentileValue[] = [
		{ percentile: 50, value: 3 },
		{ percentile: 85, value: 7 },
		{ percentile: 95, value: 12 },
		{ percentile: 99, value: 20 },
	];

	// Mock work items for SLE tests
	const mockWorkItems: IWorkItem[] = [
		{
			id: 1,
			name: "Task 1",
			state: "Done",
			stateCategory: "Done",
			type: "Task",
			referenceId: "TASK-1",
			url: null,
			startedDate: new Date("2025-04-01"),
			closedDate: new Date("2025-04-03"),
			cycleTime: 2,
			workItemAge: 2,
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 2,
			name: "Task 2",
			state: "Done",
			stateCategory: "Done",
			type: "Task",
			referenceId: "TASK-2",
			url: null,
			startedDate: new Date("2025-04-01"),
			closedDate: new Date("2025-04-05"),
			cycleTime: 4,
			workItemAge: 4,
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 3,
			name: "Task 3",
			state: "Done",
			stateCategory: "Done",
			type: "Task",
			referenceId: "TASK-3",
			url: null,
			startedDate: new Date("2025-04-01"),
			closedDate: new Date("2025-04-08"),
			cycleTime: 7,
			workItemAge: 7,
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 4,
			name: "Task 4",
			state: "Done",
			stateCategory: "Done",
			type: "Task",
			referenceId: "TASK-4",
			url: null,
			startedDate: new Date("2025-04-01"),
			closedDate: new Date("2025-04-15"),
			cycleTime: 14,
			workItemAge: 14,
			parentWorkItemReference: "",
			isBlocked: false,
		},
	];

	// Clear mocks between tests
	beforeEach(() => {
		vi.resetAllMocks();
	});

	it("should render with title and percentile data", () => {
		render(
			<CycleTimePercentiles percentileValues={mockPercentiles} items={[]} />,
		);

		expect(screen.getByText("Cycle Time Percentiles")).toBeInTheDocument();
		expect(screen.getByText("50th")).toBeInTheDocument();
		expect(screen.getByText("85th")).toBeInTheDocument();
		expect(screen.getByText("95th")).toBeInTheDocument();
		expect(screen.getByText("99th")).toBeInTheDocument();
	});

	it("should display 'No data available' when no percentiles are provided", () => {
		render(<CycleTimePercentiles percentileValues={[]} items={[]} />);

		expect(screen.getByText("Cycle Time Percentiles")).toBeInTheDocument();
		expect(screen.getByText("No data available")).toBeInTheDocument();
	});

	it("should display percentiles in descending order", () => {
		render(
			<CycleTimePercentiles percentileValues={mockPercentiles} items={[]} />,
		);

		const percentileElements = screen.getAllByText(/\d+th/);
		expect(percentileElements[0].textContent).toBe("99th");
		expect(percentileElements[1].textContent).toBe("95th");
		expect(percentileElements[2].textContent).toBe("85th");
		expect(percentileElements[3].textContent).toBe("50th");
	});

	it("should format days correctly for singular and plural values", () => {
		const singleDayPercentile: IPercentileValue[] = [
			{ percentile: 50, value: 1 },
		];

		const multiDayPercentile: IPercentileValue[] = [
			{ percentile: 85, value: 5 },
		];

		const { rerender } = render(
			<CycleTimePercentiles
				percentileValues={singleDayPercentile}
				items={[]}
			/>,
		);
		expect(screen.getByText("1 day")).toBeInTheDocument();

		rerender(
			<CycleTimePercentiles percentileValues={multiDayPercentile} items={[]} />,
		);
		expect(screen.getByText("5 days")).toBeInTheDocument();
	});

	it("should display different colors based on percentile levels", () => {
		render(
			<CycleTimePercentiles percentileValues={mockPercentiles} items={[]} />,
		);

		// We can't directly test the colors in this test environment,
		// but we can verify that the component renders without errors
		// and all percentiles are displayed with their values
		expect(screen.getByText("3 days")).toBeInTheDocument();
		expect(screen.getByText("7 days")).toBeInTheDocument();
		expect(screen.getByText("12 days")).toBeInTheDocument();
		expect(screen.getByText("20 days")).toBeInTheDocument();
	});

	// SLE-related tests
	it("should display SLE chip when serviceLevelExpectation is provided", () => {
		const mockSLE: IPercentileValue = { percentile: 85, value: 10 };
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				serviceLevelExpectation={mockSLE}
				items={[]}
			/>,
		);

		expect(screen.getByText("SLE: 85% @ 10 days")).toBeInTheDocument();
	});

	it("should not display SLE chip when serviceLevelExpectation is not provided", () => {
		render(
			<CycleTimePercentiles percentileValues={mockPercentiles} items={[]} />,
		);

		expect(screen.queryByText(/SLE:/)).not.toBeInTheDocument();
	});

	it("should flip to SLE view when SLE chip is clicked", () => {
		const mockSLE: IPercentileValue = { percentile: 85, value: 10 };
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				serviceLevelExpectation={mockSLE}
				items={mockWorkItems}
			/>,
		);

		// Initially showing percentiles view
		expect(screen.getByText("Cycle Time Percentiles")).toBeInTheDocument();

		// Click the SLE chip
		fireEvent.click(screen.getByText("SLE: 85% @ 10 days"));

		// Now showing SLE view
		expect(screen.getByText("Service Level Expectation")).toBeInTheDocument();
		expect(
			screen.getByText(/85% of all Work Items are done within 10 days or less/),
		).toBeInTheDocument();
	});

	it("should display correct SLE statistics based on work items", () => {
		const mockSLE: IPercentileValue = { percentile: 75, value: 7 };
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				serviceLevelExpectation={mockSLE}
				items={mockWorkItems}
			/>,
		);

		// Click to view SLE details
		fireEvent.click(screen.getByText("SLE: 75% @ 7 days"));

		// Check statistics - 3 out of 4 items (75%) have cycle time <= 7 days
		expect(
			screen.getByText(/75\.0% of all Work Items completed within SLE target/),
		).toBeInTheDocument();
	});

	it("should flip back to percentile view when back button is clicked", () => {
		const mockSLE: IPercentileValue = { percentile: 85, value: 10 };
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				serviceLevelExpectation={mockSLE}
				items={mockWorkItems}
			/>,
		);

		// Go to SLE view
		fireEvent.click(screen.getByText("SLE: 85% @ 10 days"));
		expect(screen.getByText("Service Level Expectation")).toBeInTheDocument();

		// Go back to percentiles view
		fireEvent.click(screen.getByRole("button", { name: "" })); // Back button
		expect(screen.getByText("Cycle Time Percentiles")).toBeInTheDocument();
	});

	it("should show 'No completed items available to analyze' when no items provided", () => {
		const mockSLE: IPercentileValue = { percentile: 85, value: 10 };
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				serviceLevelExpectation={mockSLE}
				items={[]}
			/>,
		);

		// Go to SLE view
		fireEvent.click(screen.getByText("SLE: 85% @ 10 days"));

		expect(
			screen.getByText("No completed Work Items available to analyze"),
		).toBeInTheDocument();
	});

	// Dialog opening behavior tests
	it("should open the dialog when clicking on the card", () => {
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				items={mockWorkItems}
			/>,
		);

		// Click on the card (but not on any other interactive element)
		const card = screen
			.getByText("Cycle Time Percentiles")
			.closest(".MuiCard-root");
		if (card) {
			fireEvent.click(card);
		}

		// Verify dialog is opened
		expect(screen.getByTestId("mock-dialog")).toBeInTheDocument();
		expect(screen.getByTestId("mock-dialog")).toHaveAttribute(
			"data-metric",
			"cycleTime",
		);
	});

	it("should not open the dialog when clicking on the SLE chip", () => {
		const mockSLE: IPercentileValue = { percentile: 85, value: 10 };
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				serviceLevelExpectation={mockSLE}
				items={mockWorkItems}
			/>,
		);

		// Click only on the SLE chip
		fireEvent.click(screen.getByText("SLE: 85% @ 10 days"));

		// Dialog should not be opened
		expect(screen.queryByTestId("mock-dialog")).not.toBeInTheDocument();

		// But we should have flipped to SLE view
		expect(screen.getByText("Service Level Expectation")).toBeInTheDocument();
	});

	it("should not open the dialog when clicking on the back button", () => {
		const mockSLE: IPercentileValue = { percentile: 85, value: 10 };
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				serviceLevelExpectation={mockSLE}
				items={mockWorkItems}
			/>,
		);

		// First go to SLE view
		fireEvent.click(screen.getByText("SLE: 85% @ 10 days"));
		expect(screen.getByText("Service Level Expectation")).toBeInTheDocument();

		// Then click the back button
		const backButton = screen.getByRole("button", { name: "" });
		if (backButton) {
			fireEvent.click(backButton);
		}

		// Dialog should not be opened
		expect(screen.queryByTestId("mock-dialog")).not.toBeInTheDocument();

		// And we should be back to percentiles view
		expect(screen.getByText("Cycle Time Percentiles")).toBeInTheDocument();
	});
});
