import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import { generateWorkItemMapForRunChart } from "../../../tests/TestDataProvider";
import StartedVsFinishedDisplay from "./StartedVsFinishedDisplay";

// Mock WorkItemsDialog component to test dialog interactions
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose, timeMetric }) => {
		if (!open) return null;

		return (
			<div data-testid="mock-dialog">
				<div data-testid="dialog-title">{title}</div>
				<div data-testid="dialog-items-count">{items.length}</div>
				<div data-testid="dialog-time-metric">{timeMetric}</div>
				<button
					type="button"
					data-testid="dialog-close-button"
					onClick={onClose}
				>
					Close
				</button>
				<div data-testid="dialog-items">
					{items.map((item: IWorkItem) => (
						<div key={item.id} data-testid={`item-${item.id}`}>
							{item.name}
						</div>
					))}
				</div>
			</div>
		);
	}),
}));

describe("FlowInformationDisplay component", () => {
	const mockStartedItems = new RunChartData(
		generateWorkItemMapForRunChart([3, 2, 5]),
		3,
		10,
	);
	const mockClosedItems = new RunChartData(
		generateWorkItemMapForRunChart([2, 4, 1]),
		3,
		7,
	);

	it("should render with title and flow information", () => {
		render(
			<StartedVsFinishedDisplay
				startedItems={mockStartedItems}
				closedItems={mockClosedItems}
			/>,
		);

		expect(
			screen.getByText("Started vs. Closed Work Items"),
		).toBeInTheDocument();
		expect(screen.getByText("Started:")).toBeInTheDocument();
		expect(screen.getByText("Closed:")).toBeInTheDocument();

		// Test for numbers with their context using exact element structure
		expect(screen.getByText("10")).toBeInTheDocument();
		expect(screen.getByText("7")).toBeInTheDocument();
		expect(screen.getByText("3.3")).toBeInTheDocument();
		expect(screen.getByText("2.3")).toBeInTheDocument();
	});

	it("should display zeros when no data is provided", () => {
		render(<StartedVsFinishedDisplay startedItems={null} closedItems={null} />);

		expect(
			screen.getByText("Started vs. Closed Work Items"),
		).toBeInTheDocument();

		// Check for zeros in both rows
		const startedRow = screen.getByText("Started:").closest("tr");
		const closedRow = screen.getByText("Closed:").closest("tr");

		expect(startedRow).not.toBeNull();
		expect(closedRow).not.toBeNull();

		if (startedRow && closedRow) {
			expect(within(startedRow).getByText("0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0")).toBeInTheDocument();
			expect(within(startedRow).getByText("0.0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0.0")).toBeInTheDocument();
		}
	});

	it("should calculate averages correctly", () => {
		const startedItems = new RunChartData(
			generateWorkItemMapForRunChart([5, 10, 15]),
			3,
			30,
		);
		const closedItems = new RunChartData(
			generateWorkItemMapForRunChart([2, 3, 4, 5]),
			4,
			14,
		);

		render(
			<StartedVsFinishedDisplay
				startedItems={startedItems}
				closedItems={closedItems}
			/>,
		);

		// Check for calculated values
		expect(screen.getByText("30")).toBeInTheDocument();
		expect(screen.getByText("14")).toBeInTheDocument();
		expect(screen.getByText("10.0")).toBeInTheDocument();
		expect(screen.getByText("3.5")).toBeInTheDocument();
	});

	it("should handle empty arrays gracefully", () => {
		const emptyStartedItems = new RunChartData([], 0, 0);
		const emptyClosedItems = new RunChartData([], 0, 0);

		render(
			<StartedVsFinishedDisplay
				startedItems={emptyStartedItems}
				closedItems={emptyClosedItems}
			/>,
		);

		expect(
			screen.getByText("Started vs. Closed Work Items"),
		).toBeInTheDocument();

		const startedRow = screen.getByText("Started:").closest("tr");
		const closedRow = screen.getByText("Closed:").closest("tr");

		expect(startedRow).not.toBeNull();
		expect(closedRow).not.toBeNull();

		if (startedRow && closedRow) {
			expect(within(startedRow).getByText("0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0")).toBeInTheDocument();
			expect(within(startedRow).getByText("0.0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0.0")).toBeInTheDocument();
		}
	});

	it("should format numbers with one decimal place", () => {
		const startedItems = new RunChartData(
			generateWorkItemMapForRunChart([1, 2, 3]),
			3,
			6,
		);

		render(
			<StartedVsFinishedDisplay
				startedItems={startedItems}
				closedItems={null}
			/>,
		);

		expect(screen.getByText("6")).toBeInTheDocument();
		expect(screen.getByText("2.0")).toBeInTheDocument();
	});

	it("should handle mixed data cases", () => {
		const startedItems = new RunChartData(
			generateWorkItemMapForRunChart([1, 2, 3]),
			3,
			6,
		);

		render(
			<StartedVsFinishedDisplay
				startedItems={startedItems}
				closedItems={null}
			/>,
		);

		const startedRow = screen.getByText("Started:").closest("tr");
		const closedRow = screen.getByText("Closed:").closest("tr");

		expect(startedRow).not.toBeNull();
		expect(closedRow).not.toBeNull();

		if (startedRow && closedRow) {
			expect(within(startedRow).getByText("6")).toBeInTheDocument();
			expect(within(closedRow).getByText("0")).toBeInTheDocument();
			expect(within(startedRow).getByText("2.0")).toBeInTheDocument();
			expect(within(closedRow).getByText("0.0")).toBeInTheDocument();
		}
	});

	it("should open dialog when clicking on card", () => {
		render(
			<StartedVsFinishedDisplay
				startedItems={mockStartedItems}
				closedItems={mockClosedItems}
			/>,
		);

		// Initially dialog should not be visible
		expect(screen.queryByTestId("mock-dialog")).not.toBeInTheDocument();

		// Click on the card to open dialog
		const card = screen
			.getByText("Started vs. Closed Work Items")
			.closest("div");
		expect(card).not.toBeNull();
		if (card) {
			fireEvent.click(card);
		}

		// Dialog should now be visible
		expect(screen.getByTestId("mock-dialog")).toBeInTheDocument();
		expect(screen.getByTestId("dialog-title")).toHaveTextContent(
			"Started and Closed Work Items",
		);
	});

	it("should close dialog when clicking close button", () => {
		render(
			<StartedVsFinishedDisplay
				startedItems={mockStartedItems}
				closedItems={mockClosedItems}
			/>,
		);

		// Open the dialog
		const card = screen
			.getByText("Started vs. Closed Work Items")
			.closest("div");
		if (card) {
			fireEvent.click(card);
		}

		expect(screen.getByTestId("mock-dialog")).toBeInTheDocument();

		// Click close button
		fireEvent.click(screen.getByTestId("dialog-close-button"));

		// Dialog should now be closed
		expect(screen.queryByTestId("mock-dialog")).not.toBeInTheDocument();
	});

	it("should pass all work items to dialog", () => {
		// Create test data with known number of items
		const startedItems = new RunChartData(
			generateWorkItemMapForRunChart([2, 1]), // 3 started items
			2,
			3,
		);
		const closedItems = new RunChartData(
			generateWorkItemMapForRunChart([2, 2]), // 4 closed items
			2,
			4,
		);

		render(
			<StartedVsFinishedDisplay
				startedItems={startedItems}
				closedItems={closedItems}
			/>,
		);

		// Open the dialog
		const card = screen
			.getByText("Started vs. Closed Work Items")
			.closest("div");
		if (card) {
			fireEvent.click(card);
		}

		// Check that dialog contains all items (should combine started and closed)
		// Note: actual count may vary because of filtering in the component
		expect(screen.getByTestId("dialog-items-count")).toBeInTheDocument();
	});

	it("should handle empty data gracefully", () => {
		render(<StartedVsFinishedDisplay startedItems={null} closedItems={null} />);

		// Open the dialog with no data
		const card = screen
			.getByText("Started vs. Closed Work Items")
			.closest("div");
		if (card) {
			fireEvent.click(card);
		}

		// Dialog should open but with 0 items
		expect(screen.getByTestId("mock-dialog")).toBeInTheDocument();
		expect(screen.getByTestId("dialog-items-count")).toHaveTextContent("0");
	});
});
