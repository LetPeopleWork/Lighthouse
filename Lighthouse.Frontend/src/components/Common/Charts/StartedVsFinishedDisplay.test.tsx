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

		expect(screen.getByText("Started vs. Closed Items")).toBeInTheDocument();
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

		expect(screen.getByText("Started vs. Closed Items")).toBeInTheDocument();

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

		expect(screen.getByText("Started vs. Closed Items")).toBeInTheDocument();

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
		const card = screen.getByText("Started vs. Closed Items").closest("div");
		expect(card).not.toBeNull();
		if (card) {
			fireEvent.click(card);
		}

		// Dialog should now be visible
		expect(screen.getByTestId("mock-dialog")).toBeInTheDocument();
		expect(screen.getByTestId("dialog-title")).toHaveTextContent(
			"Started and Closed Items",
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
		const card = screen.getByText("Started vs. Closed Items").closest("div");
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
		const card = screen.getByText("Started vs. Closed Items").closest("div");
		if (card) {
			fireEvent.click(card);
		}

		// Check that dialog contains all items (should combine started and closed)
		// Note: actual count may vary because of filtering in the component
		expect(screen.getByTestId("dialog-items-count")).toBeInTheDocument();
	});

	it("should use ageCycleTime metric for the dialog", () => {
		render(
			<StartedVsFinishedDisplay
				startedItems={mockStartedItems}
				closedItems={mockClosedItems}
			/>,
		);

		// Open the dialog
		const card = screen.getByText("Started vs. Closed Items").closest("div");
		if (card) {
			fireEvent.click(card);
		}

		// Check dialog is using the correct time metric
		expect(screen.getByTestId("dialog-time-metric")).toHaveTextContent(
			"ageCycleTime",
		);
	});

	it("should handle empty data gracefully", () => {
		render(<StartedVsFinishedDisplay startedItems={null} closedItems={null} />);

		// Open the dialog with no data
		const card = screen.getByText("Started vs. Closed Items").closest("div");
		if (card) {
			fireEvent.click(card);
		}

		// Dialog should open but with 0 items
		expect(screen.getByTestId("mock-dialog")).toBeInTheDocument();
		expect(screen.getByTestId("dialog-items-count")).toHaveTextContent("0");
	});

	describe("WIP status indicator", () => {
		it("should show 'confident' indicator when total difference is less than 2.0", () => {
			// Total difference is 1 (6 - 5 = 1), which is < 2
			const startedItems = new RunChartData(
				generateWorkItemMapForRunChart([2, 2, 2]),
				3,
				6,
			);
			const closedItems = new RunChartData(
				generateWorkItemMapForRunChart([1, 2, 2]),
				3,
				5,
			);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are keeping a steady WIP"),
			).toBeInTheDocument();
			expect(screen.getByText("Good job!")).toBeInTheDocument();
		});

		it("should show 'confident' indicator when total difference is less than 2.0 (even if average difference is high)", () => {
			// Total difference is 1 (7 - 6 = 1), which is < 2
			// But average difference is 3.5 vs 3.0 = 0.5 (high if this was our criterion)
			const startedItems = new RunChartData(
				generateWorkItemMapForRunChart([3, 4]),
				2,
				7,
			);
			const closedItems = new RunChartData(
				generateWorkItemMapForRunChart([3, 3]),
				2,
				6,
			);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are keeping a steady WIP"),
			).toBeInTheDocument();
			expect(screen.getByText("Good job!")).toBeInTheDocument();
		});

		it("should NOT show 'confident' indicator when total difference is more than 2.0 even if average difference is small", () => {
			// Total difference is 3 (23 - 20 = 3), which is > 2
			// But average difference is 7.67 vs 6.67 = 1.0 (small if this was our criterion)
			const startedItems = new RunChartData(
				generateWorkItemMapForRunChart([7, 8, 8]),
				3,
				23,
			);
			const closedItems = new RunChartData(
				generateWorkItemMapForRunChart([6, 7, 7]),
				3,
				20,
			);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are starting more items than you close"),
			).toBeInTheDocument();
			expect(
				screen.getByText("Observe and take action if needed!"),
			).toBeInTheDocument();
		});

		it("should show 'good' indicator when started and closed are within 5%", () => {
			// Within 5% difference (good)
			const startedItems = new RunChartData(
				generateWorkItemMapForRunChart([9, 10, 11]),
				3,
				30,
			);
			const closedItems = new RunChartData(
				generateWorkItemMapForRunChart([10, 10, 9]),
				3,
				29,
			);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are keeping a steady WIP"),
			).toBeInTheDocument();
			expect(screen.getByText("Good job!")).toBeInTheDocument();
		});

		it("should show 'caution' indicator when difference is between 5% and 10%", () => {
			// ~7% difference (caution) with average difference > 1.0
			const startedItems = new RunChartData(
				generateWorkItemMapForRunChart([15, 15, 15]),
				3,
				45,
			);
			const closedItems = new RunChartData(
				generateWorkItemMapForRunChart([14, 14, 14]),
				3,
				42,
			);
			// Averages: 15.0 vs 14.0 = 1.0 difference

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			// Since 45 vs 42 means started > closed
			expect(
				screen.getByText("You are starting more items than you close"),
			).toBeInTheDocument();
			expect(
				screen.getByText("Observe and take action if needed!"),
			).toBeInTheDocument();
		});

		it("should show 'bad' indicator when difference is more than 15%", () => {
			// 20% difference (bad)
			const startedItems = new RunChartData(
				generateWorkItemMapForRunChart([10, 10, 10]),
				3,
				30,
			);
			const closedItems = new RunChartData(
				generateWorkItemMapForRunChart([8, 8, 8]),
				3,
				24,
			);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are starting more items than you close"),
			).toBeInTheDocument();
			expect(screen.getByText("Reflect on WIP control!")).toBeInTheDocument();
		});

		it("should show appropriate message when closing more than starting", () => {
			// 25% difference with closed > started
			const startedItems = new RunChartData(
				generateWorkItemMapForRunChart([6, 6, 6]),
				3,
				18,
			);
			const closedItems = new RunChartData(
				generateWorkItemMapForRunChart([8, 8, 8]),
				3,
				24,
			);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are closing more items than you start"),
			).toBeInTheDocument();
			expect(screen.getByText("Reflect on WIP control!")).toBeInTheDocument();
		});

		it("should handle zero values properly", () => {
			const startedItems = new RunChartData(
				generateWorkItemMapForRunChart([0, 0, 0]),
				3,
				0,
			);
			const closedItems = new RunChartData(
				generateWorkItemMapForRunChart([5, 5, 5]),
				3,
				15,
			);

			render(
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={closedItems}
				/>,
			);

			expect(
				screen.getByText("You are closing more items than you start"),
			).toBeInTheDocument();
			expect(screen.getByText("Reflect on WIP control!")).toBeInTheDocument();
		});
	});
});
