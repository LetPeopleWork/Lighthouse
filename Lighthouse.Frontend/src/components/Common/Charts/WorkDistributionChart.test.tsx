import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IWorkItem } from "../../../models/WorkItem";
import WorkDistributionChart from "./WorkDistributionChart";

// Mock the WorkItemsDialog component
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose }) => {
		if (!open) return null;
		return (
			<dialog open={open} aria-label={title} data-testid="work-items-dialog">
				<h2>{title}</h2>
				<table>
					<thead>
						<tr>
							<th>Name</th>
							<th>Type</th>
							<th>State</th>
							<th>Cycle Time</th>
						</tr>
					</thead>
					<tbody>
						{items.map((item: IWorkItem) => (
							<tr key={item.id}>
								<td>{item.name}</td>
								<td>{item.type}</td>
								<td>{item.state}</td>
								<td>{item.cycleTime} days</td>
							</tr>
						))}
					</tbody>
				</table>
				<button type="button" onClick={onClose}>
					Close
				</button>
			</dialog>
		);
	}),
}));

// Mock the MUI-X PieChart component
vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		PieChart: vi.fn(({ series, onItemClick }) => {
			const seriesData = series?.[0]?.data || [];
			return (
				<div data-testid="mock-pie-chart">
					{seriesData.map(
						(
							item: { id: string; value: number; label: string },
							index: number,
						) => (
							<button
								type="button"
								key={item.id}
								data-testid={`pie-slice-${index}`}
								onClick={(e) => onItemClick?.(e, { dataIndex: index })}
							>
								{item.label}: {item.value}
							</button>
						),
					)}
				</div>
			);
		}),
	};
});

// Function to generate mock work items
function generateMockWorkItem(
	id: number,
	parentRef: string,
	cycleTime = 5,
): IWorkItem {
	return {
		id,
		name: `Work Item ${id}`,
		referenceId: `WI-${id}`,
		url: `https://example.com/work-item/${id}`,
		state: "Done",
		stateCategory: "Done",
		type: "Task",
		workItemAge: 5,
		startedDate: new Date(2025, 0, 1),
		closedDate: new Date(2025, 0, cycleTime + 1),
		cycleTime,
		parentWorkItemReference: parentRef,
		isBlocked: false,
	};
}

describe("WorkDistributionChart component", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Empty State", () => {
		it("should display 'No work items to display' when no work items are provided", () => {
			render(<WorkDistributionChart workItems={[]} />);

			expect(
				screen.getByText("Work Distribution by Parent"),
			).toBeInTheDocument();
			expect(screen.getByText("No work items to display")).toBeInTheDocument();
			expect(screen.queryByTestId("mock-pie-chart")).not.toBeInTheDocument();
		});
	});

	describe("Rendering", () => {
		it("should render the pie chart with default title", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, "PARENT-1"),
				generateMockWorkItem(3, "PARENT-2"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			expect(
				screen.getByText("Work Distribution by Parent"),
			).toBeInTheDocument();
			expect(screen.getByTestId("mock-pie-chart")).toBeInTheDocument();
		});

		it("should render the pie chart with custom title", () => {
			const workItems = [generateMockWorkItem(1, "PARENT-1")];
			const customTitle = "Custom Distribution Title";

			render(
				<WorkDistributionChart workItems={workItems} title={customTitle} />,
			);

			expect(screen.getByText(customTitle)).toBeInTheDocument();
		});

		it("should render all parent groups as pie slices", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, "PARENT-1"),
				generateMockWorkItem(3, "PARENT-2"),
				generateMockWorkItem(4, "PARENT-3"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			// Check that all parent references are displayed
			expect(screen.getByText("PARENT-1: 2")).toBeInTheDocument();
			expect(screen.getByText("PARENT-2: 1")).toBeInTheDocument();
			expect(screen.getByText("PARENT-3: 1")).toBeInTheDocument();
		});
	});

	describe("Grouping by Parent Reference", () => {
		it("should group work items by parentWorkItemReference", () => {
			const workItems = [
				generateMockWorkItem(1, "EPIC-100"),
				generateMockWorkItem(2, "EPIC-100"),
				generateMockWorkItem(3, "EPIC-100"),
				generateMockWorkItem(4, "EPIC-200"),
				generateMockWorkItem(5, "EPIC-200"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			// Verify grouped counts
			expect(screen.getByText("EPIC-100: 3")).toBeInTheDocument();
			expect(screen.getByText("EPIC-200: 2")).toBeInTheDocument();
		});

		it("should handle work items without parent reference as 'No Parent'", () => {
			const workItems = [
				generateMockWorkItem(1, ""),
				generateMockWorkItem(2, ""),
				generateMockWorkItem(3, "PARENT-1"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			expect(screen.getByText("No Parent: 2")).toBeInTheDocument();
			expect(screen.getByText("PARENT-1: 1")).toBeInTheDocument();
		});

		it("should correctly sort groups by size (descending)", () => {
			const workItems = [
				generateMockWorkItem(1, "SMALL"),
				generateMockWorkItem(2, "LARGE"),
				generateMockWorkItem(3, "LARGE"),
				generateMockWorkItem(4, "LARGE"),
				generateMockWorkItem(5, "MEDIUM"),
				generateMockWorkItem(6, "MEDIUM"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			const slices = screen.getAllByRole("button");
			// First slice should be LARGE (3 items), then MEDIUM (2 items), then SMALL (1 item)
			expect(slices[0]).toHaveTextContent("LARGE: 3");
			expect(slices[1]).toHaveTextContent("MEDIUM: 2");
			expect(slices[2]).toHaveTextContent("SMALL: 1");
		});
	});

	describe("User Interactions", () => {
		it("should open dialog when clicking on a pie slice", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, "PARENT-1"),
				generateMockWorkItem(3, "PARENT-2"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			// Click on the first pie slice (PARENT-1 with 2 items)
			fireEvent.click(screen.getByTestId("pie-slice-0"));

			// Verify dialog is opened
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
			expect(
				screen.getByText(/Work Items for PARENT-1 \(2 items\)/),
			).toBeInTheDocument();
		});

		it("should display correct work items in dialog when slice is clicked", () => {
			const workItems = [
				generateMockWorkItem(1, "EPIC-A"),
				generateMockWorkItem(2, "EPIC-A"),
				generateMockWorkItem(3, "EPIC-B"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			// Click on EPIC-A slice (should be first due to sorting by size)
			fireEvent.click(screen.getByTestId("pie-slice-0"));

			// Verify dialog shows the correct items
			expect(screen.getByText("Work Item 1")).toBeInTheDocument();
			expect(screen.getByText("Work Item 2")).toBeInTheDocument();
			expect(screen.queryByText("Work Item 3")).not.toBeInTheDocument();
		});

		it("should close dialog when close button is clicked", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, "PARENT-1"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			// Open dialog
			fireEvent.click(screen.getByTestId("pie-slice-0"));
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();

			// Close dialog
			const closeButton = screen.getByRole("button", { name: /close/i });
			fireEvent.click(closeButton);

			// Verify dialog is closed
			expect(screen.queryByTestId("work-items-dialog")).not.toBeInTheDocument();
		});

		it("should handle multiple slice clicks and update dialog content", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, "PARENT-1"),
				generateMockWorkItem(3, "PARENT-2"),
				generateMockWorkItem(4, "PARENT-2"),
				generateMockWorkItem(5, "PARENT-2"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			// Click first slice (PARENT-2 with 3 items, sorted descending)
			fireEvent.click(screen.getByTestId("pie-slice-0"));
			expect(
				screen.getByText(/Work Items for PARENT-2 \(3 items\)/),
			).toBeInTheDocument();
			expect(screen.getByText("Work Item 3")).toBeInTheDocument();

			// Close dialog
			fireEvent.click(screen.getByRole("button", { name: /close/i }));

			// Click second slice (PARENT-1 with 2 items)
			fireEvent.click(screen.getByTestId("pie-slice-1"));
			expect(
				screen.getByText(/Work Items for PARENT-1 \(2 items\)/),
			).toBeInTheDocument();
			expect(screen.getByText("Work Item 1")).toBeInTheDocument();
		});
	});

	describe("Edge Cases", () => {
		it("should handle single work item", () => {
			const workItems = [generateMockWorkItem(1, "SINGLE-PARENT")];

			render(<WorkDistributionChart workItems={workItems} />);

			expect(screen.getByText("SINGLE-PARENT: 1")).toBeInTheDocument();
			expect(screen.getByTestId("mock-pie-chart")).toBeInTheDocument();
		});

		it("should handle all work items having the same parent", () => {
			const workItems = [
				generateMockWorkItem(1, "SAME-PARENT"),
				generateMockWorkItem(2, "SAME-PARENT"),
				generateMockWorkItem(3, "SAME-PARENT"),
				generateMockWorkItem(4, "SAME-PARENT"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			expect(screen.getByText("SAME-PARENT: 4")).toBeInTheDocument();
			expect(screen.getAllByRole("button")).toHaveLength(1); // Only one slice
		});

		it("should handle mix of items with and without parents", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, ""),
				generateMockWorkItem(3, "PARENT-1"),
				generateMockWorkItem(4, ""),
				generateMockWorkItem(5, ""),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			// No Parent should have 3 items (sorted first)
			expect(screen.getByText("No Parent: 3")).toBeInTheDocument();
			expect(screen.getByText("PARENT-1: 2")).toBeInTheDocument();
		});

		it("should handle large number of different parents", () => {
			const workItems = Array.from({ length: 20 }, (_, i) =>
				generateMockWorkItem(i + 1, `PARENT-${i + 1}`),
			);

			render(<WorkDistributionChart workItems={workItems} />);

			// All parents should have 1 item each
			const slices = screen.getAllByRole("button");
			expect(slices).toHaveLength(20);

			// Each should show count of 1
			for (const slice of slices) {
				expect(slice.textContent).toContain(": 1");
			}
		});
	});

	describe("Dialog Integration", () => {
		it("should pass cycle time to dialog", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1", 10),
				generateMockWorkItem(2, "PARENT-1", 15),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			fireEvent.click(screen.getByTestId("pie-slice-0"));

			// Verify cycle times are displayed
			expect(screen.getByText("10 days")).toBeInTheDocument();
			expect(screen.getByText("15 days")).toBeInTheDocument();
		});

		it("should display correct dialog title format", () => {
			const workItems = [
				generateMockWorkItem(1, "FEATURE-XYZ"),
				generateMockWorkItem(2, "FEATURE-XYZ"),
				generateMockWorkItem(3, "FEATURE-XYZ"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			fireEvent.click(screen.getByTestId("pie-slice-0"));

			// Check title format: "Work Items for {parent} ({count} items)"
			expect(
				screen.getByText("Work Items for FEATURE-XYZ (3 items)"),
			).toBeInTheDocument();
		});
	});

	describe("Terminology Integration", () => {
		it("should use terminology service for work items term", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, "PARENT-1"),
			];

			render(<WorkDistributionChart workItems={workItems} />);

			fireEvent.click(screen.getByTestId("pie-slice-0"));

			// The dialog title should use the terminology from the service
			// By default, it should say "Work Items"
			expect(
				screen.getByText("Work Items for PARENT-1 (2 items)"),
			).toBeInTheDocument();
		});
	});
});
