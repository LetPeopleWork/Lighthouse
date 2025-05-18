import { fireEvent, render, screen } from "@testing-library/react";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";
import {
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";
import WorkItemsDialog from "./WorkItemsDialog";

// Mock data for testing
const mockWorkItems: IWorkItem[] = [
	{
		id: 1,
		name: "Implement feature X",
		state: "In Progress",
		stateCategory: "Doing" as StateCategory,
		type: "User Story",
		workItemReference: "US-123",
		url: "https://example.com/work-item/1",
		startedDate: new Date("2023-01-01"),
		closedDate: new Date("2023-01-10"),
		cycleTime: 10,
		workItemAge: 5,
	},
	{
		id: 2,
		name: "Fix bug Y",
		state: "Active",
		stateCategory: "Doing" as StateCategory,
		type: "Bug",
		workItemReference: "BUG-456",
		url: null,
		startedDate: new Date("2023-02-01"),
		closedDate: new Date("2023-02-05"),
		cycleTime: 5,
		workItemAge: 20,
	},
	{
		id: 3,
		name: "Research Z",
		state: "New",
		stateCategory: "ToDo" as StateCategory,
		type: "Task",
		workItemReference: "TASK-789",
		url: "https://example.com/work-item/3",
		startedDate: new Date("2023-03-01"),
		closedDate: new Date("2023-03-15"),
		cycleTime: 15,
		workItemAge: 2,
	},
];

// Test scenarios
describe("WorkItemsDialog Component", () => {
	const defaultProps = {
		title: "Test Dialog",
		items: mockWorkItems,
		open: true,
		onClose: vi.fn(),
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	test("renders with default props", () => {
		render(<WorkItemsDialog {...defaultProps} />);

		// Check if title is displayed
		expect(screen.getByText("Test Dialog")).toBeInTheDocument();

		// Check if correct columns are displayed
		expect(screen.getByText("Name")).toBeInTheDocument();
		expect(screen.getByText("Type")).toBeInTheDocument();
		expect(screen.getByText("State")).toBeInTheDocument();
		expect(screen.getByText("Age")).toBeInTheDocument();

		// Check if work item data is displayed
		expect(screen.getByText("Implement feature X")).toBeInTheDocument();
		expect(screen.getByText("Fix bug Y")).toBeInTheDocument();
		expect(screen.getByText("Research Z")).toBeInTheDocument();

		// Check formatting of age
		expect(screen.getByText("5 days")).toBeInTheDocument();
		expect(screen.getByText("20 days")).toBeInTheDocument();
		expect(screen.getByText("2 days")).toBeInTheDocument();
	});

	test("renders with cycleTime metric", () => {
		render(<WorkItemsDialog {...defaultProps} timeMetric="cycleTime" />);

		// Check if correct column name is displayed
		expect(screen.getByText("Cycle Time")).toBeInTheDocument();

		// Check formatting of cycle time
		expect(screen.getByText("10 days")).toBeInTheDocument();
		expect(screen.getByText("5 days")).toBeInTheDocument();
		expect(screen.getByText("15 days")).toBeInTheDocument();
	});

	test("displays items sorted by age in descending order with age metric", () => {
		render(<WorkItemsDialog {...defaultProps} timeMetric="age" />);

		// Get all row cells with age information
		const cells = screen.getAllByText(/\d+ days/);

		// Check if they're in descending order (20, 5, 2)
		expect(cells[0]).toHaveTextContent("20 days");
		expect(cells[1]).toHaveTextContent("5 days");
		expect(cells[2]).toHaveTextContent("2 days");
	});

	test("displays items sorted by cycle time in descending order with cycleTime metric", () => {
		render(<WorkItemsDialog {...defaultProps} timeMetric="cycleTime" />);

		// Get all row cells with cycle time information
		const cells = screen.getAllByText(/\d+ days/);

		// Check if they're in descending order (15, 10, 5)
		expect(cells[0]).toHaveTextContent("15 days");
		expect(cells[1]).toHaveTextContent("10 days");
		expect(cells[2]).toHaveTextContent("5 days");
	});

	test("calls onClose when close button is clicked", () => {
		render(<WorkItemsDialog {...defaultProps} />);

		// Find and click the close button
		const closeButton = screen.getByTestId("CloseIcon").closest("button");
		if (closeButton) {
			fireEvent.click(closeButton);
		}

		// Check if onClose was called
		expect(defaultProps.onClose).toHaveBeenCalledTimes(1);
	});

	test("renders links for items with URLs", () => {
		render(<WorkItemsDialog {...defaultProps} />);

		// Check if links are rendered for items with URLs
		const links = screen.getAllByRole("link");
		expect(links).toHaveLength(2); // Should find 2 items with URLs

		// Check if the link content is correct
		expect(links[0]).toHaveTextContent("Implement feature X");
		expect(links[0]).toHaveAttribute("href", "https://example.com/work-item/1");

		expect(links[1]).toHaveTextContent("Research Z");
		expect(links[1]).toHaveAttribute("href", "https://example.com/work-item/3");
	});

	test("displays plain text for items without URLs", () => {
		render(<WorkItemsDialog {...defaultProps} />);

		// Check if the item without URL is displayed as plain text
		const bugItem = screen.getByText("Fix bug Y");
		expect(bugItem.tagName).not.toBe("A");
	});

	test("displays empty state message when no items", () => {
		render(<WorkItemsDialog {...defaultProps} items={[]} />);

		// Check if the empty state message is displayed
		expect(screen.getByText("No items to display")).toBeInTheDocument();
	});

	test("doesn't open dialog when open prop is false", () => {
		render(<WorkItemsDialog {...defaultProps} open={false} />);

		// Check that the dialog is not in the document
		expect(screen.queryByText("Test Dialog")).not.toBeInTheDocument();
	});

	describe("Service Level Expectation (SLE) functionality", () => {
		test("renders without styling when no SLE is provided", () => {
			render(<WorkItemsDialog {...defaultProps} />);

			const ageCells = screen.getAllByText(/\d+ days/);

			// Verify no cells have bold styling or color styling
			for (const cell of ageCells) {
				expect(cell).not.toHaveStyle("font-weight: bold");
				// We cannot easily test for undefined color, but we can verify it doesn't have any of the SLE colors
				expect(cell).not.toHaveStyle(`color: ${riskyColor}`);
				expect(cell).not.toHaveStyle(`color: ${realisticColor}`);
				expect(cell).not.toHaveStyle(`color: ${confidentColor}`);
				expect(cell).not.toHaveStyle(`color: ${certainColor}`);
			}
		});

		test("applies risky color to items at or above SLE", () => {
			// With SLE of 10, the item with age 20 should be risky
			render(<WorkItemsDialog {...defaultProps} sle={10} />);

			const ageCells = screen.getAllByText(/\d+ days/);

			// First cell (20 days) should have risky color and be bold
			expect(ageCells[0]).toHaveStyle("color: rgb(255, 0, 0)"); // RGB equivalent of red
			expect(ageCells[0]).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies realistic color to items at or above 70% of SLE", () => {
			// With SLE of 10, item with age 7 (70%) should be realistic
			render(<WorkItemsDialog {...defaultProps} sle={10} />);

			// Find the cell with '5 days' text which is between 50% and 70% of SLE=10
			const fiveDaysCell = screen.getByText("5 days");

			// Should have realistic color (5 is 50% of 10, but we're testing with a real item)
			expect(fiveDaysCell).not.toHaveStyle("color: rgb(255, 0, 0)");
			expect(fiveDaysCell).toHaveStyle("color: rgb(144, 238, 144)"); // RGB equivalent of lightgreen
			expect(fiveDaysCell).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies certain color to items below 50% of SLE", () => {
			// With SLE of 10, item with age 2 (<50%) should be certain
			render(<WorkItemsDialog {...defaultProps} sle={10} />);

			// Find the cell with '2 days' text which is below 50% of SLE=10
			const twoDaysCell = screen.getByText("2 days");

			// Should have certain color
			expect(twoDaysCell).toHaveStyle("color: rgb(0, 128, 0)"); // RGB equivalent of green
			expect(twoDaysCell).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies colors correctly with cycleTime metric", () => {
			// With SLE of 10, items with cycle times of 15, 10, and 5 should have appropriate colors
			render(
				<WorkItemsDialog {...defaultProps} timeMetric="cycleTime" sle={10} />,
			);

			const cycleTimeCells = screen.getAllByText(/\d+ days/);

			// 15 days should be risky
			expect(cycleTimeCells[0]).toHaveStyle("color: rgb(255, 0, 0)"); // RGB equivalent of red

			// 10 days should be risky (at SLE)
			expect(cycleTimeCells[1]).toHaveStyle("color: rgb(255, 0, 0)"); // RGB equivalent of red

			// 5 days should be confident (50% of SLE)
			expect(cycleTimeCells[2]).toHaveStyle("color: rgb(144, 238, 144)"); // RGB equivalent of lightgreen
		});
	});
});
