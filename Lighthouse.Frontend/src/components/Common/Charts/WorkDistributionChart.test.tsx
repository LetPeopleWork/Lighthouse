import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../../tests/MockApiServiceProvider";
import { getColorMapForKeys, hexToRgb } from "../../../utils/theme/colors";
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

// Helper function to render component with API context
function renderWithContext(ui: React.ReactElement) {
	const mockFeatureService = createMockFeatureService();
	mockFeatureService.getFeaturesByReferences = vi.fn().mockResolvedValue([]);

	const mockContext = createMockApiServiceContext({
		featureService: mockFeatureService,
	});

	return render(
		<ApiServiceContext.Provider value={mockContext}>
			{ui}
		</ApiServiceContext.Provider>,
	);
}

describe("WorkDistributionChart component", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Empty State", () => {
		it("should display 'No work items to display' when no work items are provided", () => {
			renderWithContext(<WorkDistributionChart workItems={[]} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			expect(
				screen.getByText("Work Distribution by Parent"),
			).toBeInTheDocument();
			expect(screen.getByTestId("mock-pie-chart")).toBeInTheDocument();
		});

		it("should render the pie chart with custom title", () => {
			const workItems = [generateMockWorkItem(1, "PARENT-1")];
			const customTitle = "Custom Distribution Title";

			renderWithContext(
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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			// No Parent should have 3 items (sorted first)
			expect(screen.getByText("No Parent: 3")).toBeInTheDocument();
			expect(screen.getByText("PARENT-1: 2")).toBeInTheDocument();
		});

		it("should handle large number of different parents", () => {
			const workItems = Array.from({ length: 20 }, (_, i) =>
				generateMockWorkItem(i + 1, `PARENT-${i + 1}`),
			);

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

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

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			fireEvent.click(screen.getByTestId("pie-slice-0"));

			// The dialog title should use the terminology from the service
			// By default, it should say "Work Items"
			expect(
				screen.getByText("Work Items for PARENT-1 (2 items)"),
			).toBeInTheDocument();
		});
	});

	describe("Table View", () => {
		it("should render table with correct headers", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, "PARENT-2"),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			// Check table headers
			expect(screen.getByText("Name")).toBeInTheDocument();
			expect(screen.getByText("%")).toBeInTheDocument();
			expect(screen.getByText("Work Items")).toBeInTheDocument();
		});

		it("should display all parent groups in table with correct data", () => {
			const workItems = [
				generateMockWorkItem(1, "EPIC-100"),
				generateMockWorkItem(2, "EPIC-100"),
				generateMockWorkItem(3, "EPIC-100"),
				generateMockWorkItem(4, "EPIC-200"),
				generateMockWorkItem(5, "EPIC-200"),
				generateMockWorkItem(6, "EPIC-300"),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			// Check parent names in table (should be sorted by count descending)
			expect(screen.getByText("EPIC-100")).toBeInTheDocument();
			expect(screen.getByText("EPIC-200")).toBeInTheDocument();
			expect(screen.getByText("EPIC-300")).toBeInTheDocument();

			// Check percentages (3/6 = 50%, 2/6 = 33.3%, 1/6 = 16.7%)
			expect(screen.getByText("50.0%")).toBeInTheDocument();
			expect(screen.getByText("33.3%")).toBeInTheDocument();
			expect(screen.getByText("16.7%")).toBeInTheDocument();

			// Check counts in table
			const tableCells = screen.getAllByRole("cell");
			const countCells = tableCells.filter(
				(cell) =>
					cell.textContent === "3" ||
					cell.textContent === "2" ||
					cell.textContent === "1",
			);
			expect(countCells.length).toBeGreaterThanOrEqual(3);
		});

		it("should display 'No Parent' in table when items have no parent reference", () => {
			const workItems = [
				generateMockWorkItem(1, ""),
				generateMockWorkItem(2, ""),
				generateMockWorkItem(3, "EPIC-100"),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			// Check that "No Parent" is displayed in the table
			expect(screen.getByText("No Parent")).toBeInTheDocument();

			// Check percentages (2/3 = 66.7%, 1/3 = 33.3%)
			expect(screen.getByText("66.7%")).toBeInTheDocument();
			expect(screen.getByText("33.3%")).toBeInTheDocument();
		});

		it("should display color indicators next to parent names in table", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, "PARENT-2"),
			];

			const { container } = renderWithContext(
				<WorkDistributionChart workItems={workItems} />,
			);

			// Look for the parent names in the table which should be next to color indicators
			expect(screen.getByText("PARENT-1")).toBeInTheDocument();
			expect(screen.getByText("PARENT-2")).toBeInTheDocument();

			// Verify table structure exists
			const tableCells = container.querySelectorAll("td");
			expect(tableCells.length).toBeGreaterThanOrEqual(4);

			// Verify the color boxes are present and using the getColorMapForKeys mapping
			const keys = ["PARENT-1", "PARENT-2"];
			const colorMap = getColorMapForKeys(keys);
			// Check the first two color boxes
			const colorBox0 = container.querySelector(
				'[data-testid="color-box-0"]',
			) as HTMLElement;
			const colorBox1 = container.querySelector(
				'[data-testid="color-box-1"]',
			) as HTMLElement;
			expect(colorBox0).toBeTruthy();
			expect(colorBox1).toBeTruthy();
			const computed0 = globalThis.getComputedStyle(colorBox0).backgroundColor;
			const computed1 = globalThis.getComputedStyle(colorBox1).backgroundColor;
			const expected0 = `rgb(${hexToRgb(colorMap["PARENT-1"]).r}, ${hexToRgb(colorMap["PARENT-1"]).g}, ${hexToRgb(colorMap["PARENT-1"]).b})`;
			const expected1 = `rgb(${hexToRgb(colorMap["PARENT-2"]).r}, ${hexToRgb(colorMap["PARENT-2"]).g}, ${hexToRgb(colorMap["PARENT-2"]).b})`;
			expect(computed0).toBe(expected0);
			expect(computed1).toBe(expected1);
		});

		it("should order colors starting with the largest parent", () => {
			const workItems = [
				generateMockWorkItem(1, "LARGE"),
				generateMockWorkItem(2, "LARGE"),
				generateMockWorkItem(3, "LARGE"),
				generateMockWorkItem(4, "SMALL"),
			];

			const { container } = renderWithContext(
				<WorkDistributionChart workItems={workItems} />,
			);

			const colorBox0 = container.querySelector(
				'[data-testid="color-box-0"]',
			) as HTMLElement;
			const colorBox1 = container.querySelector(
				'[data-testid="color-box-1"]',
			) as HTMLElement;
			expect(colorBox0).toBeTruthy();
			expect(colorBox1).toBeTruthy();

			const colorMap = getColorMapForKeys(["LARGE", "SMALL"], true);
			const expected0 = `rgb(${hexToRgb(colorMap.LARGE).r}, ${hexToRgb(colorMap.LARGE).g}, ${hexToRgb(colorMap.LARGE).b})`;
			const expected1 = `rgb(${hexToRgb(colorMap.SMALL).r}, ${hexToRgb(colorMap.SMALL).g}, ${hexToRgb(colorMap.SMALL).b})`;

			const computed0 = globalThis.getComputedStyle(colorBox0).backgroundColor;
			const computed1 = globalThis.getComputedStyle(colorBox1).backgroundColor;

			expect(computed0).toBe(expected0);
			expect(computed1).toBe(expected1);
		});

		it("should open dialog when clicking on table row", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-1"),
				generateMockWorkItem(2, "PARENT-1"),
				generateMockWorkItem(3, "PARENT-2"),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			// Find and click on a table row (not header)
			const tableRows = screen.getAllByRole("row");
			// First row is header, second row should be PARENT-1 (2 items, sorted first)
			fireEvent.click(tableRows[1]);

			// Verify dialog is opened with correct data
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
			expect(
				screen.getByText(/Work Items for PARENT-1 \(2 items\)/),
			).toBeInTheDocument();
		});

		it("should show correct items when clicking different table rows", () => {
			const workItems = [
				generateMockWorkItem(1, "EPIC-A"),
				generateMockWorkItem(2, "EPIC-A"),
				generateMockWorkItem(3, "EPIC-A"),
				generateMockWorkItem(4, "EPIC-B"),
				generateMockWorkItem(5, "EPIC-B"),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			const tableRows = screen.getAllByRole("row");

			// Click on first data row (EPIC-A with 3 items)
			fireEvent.click(tableRows[1]);
			expect(
				screen.getByText("Work Items for EPIC-A (3 items)"),
			).toBeInTheDocument();
			expect(screen.getByText("Work Item 1")).toBeInTheDocument();

			// Close dialog
			fireEvent.click(screen.getByRole("button", { name: /close/i }));

			// Click on second data row (EPIC-B with 2 items)
			fireEvent.click(tableRows[2]);
			expect(
				screen.getByText("Work Items for EPIC-B (2 items)"),
			).toBeInTheDocument();
			expect(screen.getByText("Work Item 4")).toBeInTheDocument();
		});

		it("should calculate percentages correctly for varying distributions", () => {
			const workItems = [
				generateMockWorkItem(1, "A"),
				generateMockWorkItem(2, "B"),
				generateMockWorkItem(3, "B"),
				generateMockWorkItem(4, "C"),
				generateMockWorkItem(5, "C"),
				generateMockWorkItem(6, "C"),
				generateMockWorkItem(7, "D"),
				generateMockWorkItem(8, "D"),
				generateMockWorkItem(9, "D"),
				generateMockWorkItem(10, "D"),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			// D: 4/10 = 40.0%
			expect(screen.getByText("40.0%")).toBeInTheDocument();
			// C: 3/10 = 30.0%
			expect(screen.getByText("30.0%")).toBeInTheDocument();
			// B: 2/10 = 20.0%
			expect(screen.getByText("20.0%")).toBeInTheDocument();
			// A: 1/10 = 10.0%
			expect(screen.getByText("10.0%")).toBeInTheDocument();
		});

		it("should display 100% for single parent group", () => {
			const workItems = [
				generateMockWorkItem(1, "ONLY-PARENT"),
				generateMockWorkItem(2, "ONLY-PARENT"),
				generateMockWorkItem(3, "ONLY-PARENT"),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			expect(screen.getByText("100.0%")).toBeInTheDocument();
		});

		it("should sort table rows by count descending", () => {
			const workItems = [
				generateMockWorkItem(1, "SMALL"),
				generateMockWorkItem(2, "LARGE"),
				generateMockWorkItem(3, "LARGE"),
				generateMockWorkItem(4, "LARGE"),
				generateMockWorkItem(5, "MEDIUM"),
				generateMockWorkItem(6, "MEDIUM"),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			const tableRows = screen.getAllByRole("row");
			// Skip header row (index 0)
			const row1Text = tableRows[1].textContent || "";
			const row2Text = tableRows[2].textContent || "";
			const row3Text = tableRows[3].textContent || "";

			// Should be ordered: LARGE (3), MEDIUM (2), SMALL (1)
			expect(row1Text).toContain("LARGE");
			expect(row1Text).toContain("3");
			expect(row2Text).toContain("MEDIUM");
			expect(row2Text).toContain("2");
			expect(row3Text).toContain("SMALL");
			expect(row3Text).toContain("1");
		});

		it("should display parent names in table when fetched from API", async () => {
			const mockFeatureService = createMockFeatureService();
			mockFeatureService.getFeaturesByReferences = vi.fn().mockResolvedValue([
				{ referenceId: "EPIC-100", name: "User Management" },
				{ referenceId: "EPIC-200", name: "Reporting Features" },
			]);

			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			const workItems = [
				generateMockWorkItem(1, "EPIC-100"),
				generateMockWorkItem(2, "EPIC-100"),
				generateMockWorkItem(3, "EPIC-200"),
			];

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<WorkDistributionChart workItems={workItems} />
				</ApiServiceContext.Provider>,
			);

			// Wait for parent names to be fetched and displayed in table
			await vi.waitFor(() => {
				expect(screen.getByText("User Management")).toBeInTheDocument();
			});

			expect(screen.getByText("Reporting Features")).toBeInTheDocument();
		});

		it("should use parent names in dialog when clicking table row", async () => {
			const mockFeatureService = createMockFeatureService();
			mockFeatureService.getFeaturesByReferences = vi
				.fn()
				.mockResolvedValue([
					{ referenceId: "EPIC-100", name: "Search Functionality" },
				]);

			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			const workItems = [
				generateMockWorkItem(1, "EPIC-100"),
				generateMockWorkItem(2, "EPIC-100"),
			];

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<WorkDistributionChart workItems={workItems} />
				</ApiServiceContext.Provider>,
			);

			// Wait for parent names to load
			await vi.waitFor(() => {
				expect(screen.getByText("Search Functionality")).toBeInTheDocument();
			});

			// Click on table row
			const tableRows = screen.getAllByRole("row");
			fireEvent.click(tableRows[1]);

			// Dialog should show parent name
			expect(
				screen.getByText("Work Items for Search Functionality (2 items)"),
			).toBeInTheDocument();
		});

		it("should handle long parent names in table", () => {
			const workItems = [
				generateMockWorkItem(
					1,
					"VERY-LONG-PARENT-REFERENCE-ID-THAT-SHOULD-BE-TRUNCATED",
				),
				generateMockWorkItem(
					2,
					"VERY-LONG-PARENT-REFERENCE-ID-THAT-SHOULD-BE-TRUNCATED",
				),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			// Verify the long parent name is displayed in the table
			expect(
				screen.getByText(
					"VERY-LONG-PARENT-REFERENCE-ID-THAT-SHOULD-BE-TRUNCATED",
				),
			).toBeInTheDocument();

			// Verify count is correct
			const tableRows = screen.getAllByRole("row");
			expect(tableRows.length).toBe(2); // Header + 1 data row
		});

		it("should not render table when there are no work items", () => {
			renderWithContext(<WorkDistributionChart workItems={[]} />);

			// Table headers should not be present
			expect(screen.queryByText("Name")).not.toBeInTheDocument();
			expect(screen.queryByText("%")).not.toBeInTheDocument();
		});

		it("should maintain consistent order between pie chart and table", () => {
			const workItems = [
				generateMockWorkItem(1, "PARENT-A"),
				generateMockWorkItem(2, "PARENT-B"),
				generateMockWorkItem(3, "PARENT-B"),
				generateMockWorkItem(4, "PARENT-C"),
				generateMockWorkItem(5, "PARENT-C"),
				generateMockWorkItem(6, "PARENT-C"),
			];

			renderWithContext(<WorkDistributionChart workItems={workItems} />);

			// Both pie chart and table should be sorted: C(3), B(2), A(1)
			const pieSlices = screen.getAllByRole("button");
			expect(pieSlices[0]).toHaveTextContent("PARENT-C: 3");
			expect(pieSlices[1]).toHaveTextContent("PARENT-B: 2");
			expect(pieSlices[2]).toHaveTextContent("PARENT-A: 1");

			const tableRows = screen.getAllByRole("row");
			expect(tableRows[1].textContent).toContain("PARENT-C");
			expect(tableRows[2].textContent).toContain("PARENT-B");
			expect(tableRows[3].textContent).toContain("PARENT-A");
		});
	});

	describe("Parent Name Fetching", () => {
		it("should fetch parent names from feature service", async () => {
			const mockFeatureService = createMockFeatureService();
			mockFeatureService.getFeaturesByReferences = vi.fn().mockResolvedValue([
				{ referenceId: "EPIC-100", name: "Epic 100 Feature Name" },
				{ referenceId: "EPIC-200", name: "Epic 200 Feature Name" },
			]);

			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			const workItems = [
				generateMockWorkItem(1, "EPIC-100"),
				generateMockWorkItem(2, "EPIC-100"),
				generateMockWorkItem(3, "EPIC-200"),
			];

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<WorkDistributionChart workItems={workItems} />
				</ApiServiceContext.Provider>,
			);

			// Wait for the feature service to be called
			await vi.waitFor(() => {
				expect(mockFeatureService.getFeaturesByReferences).toHaveBeenCalledWith(
					["EPIC-100", "EPIC-200"],
				);
			});
		});

		it("should display parent names instead of reference IDs when available", async () => {
			const mockFeatureService = createMockFeatureService();
			mockFeatureService.getFeaturesByReferences = vi.fn().mockResolvedValue([
				{ referenceId: "EPIC-100", name: "User Authentication" },
				{ referenceId: "EPIC-200", name: "Dashboard Features" },
			]);

			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			const workItems = [
				generateMockWorkItem(1, "EPIC-100"),
				generateMockWorkItem(2, "EPIC-100"),
				generateMockWorkItem(3, "EPIC-200"),
			];

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<WorkDistributionChart workItems={workItems} />
				</ApiServiceContext.Provider>,
			);

			// Wait for parent names to be fetched and rendered
			await vi.waitFor(() => {
				expect(screen.getByText("User Authentication: 2")).toBeInTheDocument();
			});

			expect(screen.getByText("Dashboard Features: 1")).toBeInTheDocument();
		});

		it("should fall back to reference ID when parent name is not available", async () => {
			const mockFeatureService = createMockFeatureService();
			mockFeatureService.getFeaturesByReferences = vi.fn().mockResolvedValue([
				{ referenceId: "EPIC-100", name: "Known Feature" },
				// EPIC-200 is not returned by the service
			]);

			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			const workItems = [
				generateMockWorkItem(1, "EPIC-100"),
				generateMockWorkItem(2, "EPIC-200"),
			];

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<WorkDistributionChart workItems={workItems} />
				</ApiServiceContext.Provider>,
			);

			// Wait for rendering to complete
			await vi.waitFor(() => {
				expect(screen.getByText("Known Feature: 1")).toBeInTheDocument();
			});

			// Should fall back to reference ID for unknown parent
			expect(screen.getByText("EPIC-200: 1")).toBeInTheDocument();
		});

		it("should not fetch parent names when all items have no parent", () => {
			const mockFeatureService = createMockFeatureService();
			mockFeatureService.getFeaturesByReferences = vi
				.fn()
				.mockResolvedValue([]);

			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			const workItems = [
				generateMockWorkItem(1, ""),
				generateMockWorkItem(2, ""),
				generateMockWorkItem(3, ""),
			];

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<WorkDistributionChart workItems={workItems} />
				</ApiServiceContext.Provider>,
			);

			// Service should not be called since there are no parent references
			expect(mockFeatureService.getFeaturesByReferences).not.toHaveBeenCalled();

			expect(screen.getByText("No Parent: 3")).toBeInTheDocument();
		});

		it("should handle API errors gracefully and fall back to reference IDs", async () => {
			const mockFeatureService = createMockFeatureService();
			mockFeatureService.getFeaturesByReferences = vi
				.fn()
				.mockRejectedValue(new Error("API Error"));

			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			const workItems = [
				generateMockWorkItem(1, "EPIC-100"),
				generateMockWorkItem(2, "EPIC-100"),
			];

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<WorkDistributionChart workItems={workItems} />
				</ApiServiceContext.Provider>,
			);

			// Wait for the API call to complete (even though it fails)
			await vi.waitFor(() => {
				expect(mockFeatureService.getFeaturesByReferences).toHaveBeenCalled();
			});

			// Should fall back to reference ID on error
			expect(screen.getByText("EPIC-100: 2")).toBeInTheDocument();
		});

		it("should use parent names in dialog title when available", async () => {
			const mockFeatureService = createMockFeatureService();
			mockFeatureService.getFeaturesByReferences = vi
				.fn()
				.mockResolvedValue([
					{ referenceId: "EPIC-100", name: "Payment Processing" },
				]);

			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			const workItems = [
				generateMockWorkItem(1, "EPIC-100"),
				generateMockWorkItem(2, "EPIC-100"),
			];

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<WorkDistributionChart workItems={workItems} />
				</ApiServiceContext.Provider>,
			);

			// Wait for parent names to be loaded
			await vi.waitFor(() => {
				expect(screen.getByText("Payment Processing: 2")).toBeInTheDocument();
			});

			// Click on the slice
			fireEvent.click(screen.getByTestId("pie-slice-0"));

			// Dialog should use the parent name
			expect(
				screen.getByText("Work Items for Payment Processing (2 items)"),
			).toBeInTheDocument();
		});

		it("should handle empty parent references (whitespace only)", () => {
			const mockFeatureService = createMockFeatureService();
			mockFeatureService.getFeaturesByReferences = vi
				.fn()
				.mockResolvedValue([]);

			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			const workItems = [
				generateMockWorkItem(1, "   "), // Whitespace only
				generateMockWorkItem(2, "\t"), // Tab character
				generateMockWorkItem(3, "EPIC-100"),
			];

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<WorkDistributionChart workItems={workItems} />
				</ApiServiceContext.Provider>,
			);

			// Whitespace-only references are treated as empty strings (no parent)
			// So we expect "No Parent: 0" because empty strings are filtered out
			// But the actual items with whitespace will get their own groups
			// The component doesn't trim, so "   " and "\t" are different from ""

			// Should only fetch for EPIC-100, not whitespace references
			expect(mockFeatureService.getFeaturesByReferences).toHaveBeenCalledWith([
				"EPIC-100",
			]);

			// Verify EPIC-100 is displayed
			expect(screen.getByText("EPIC-100: 1")).toBeInTheDocument();
		});
	});
});
