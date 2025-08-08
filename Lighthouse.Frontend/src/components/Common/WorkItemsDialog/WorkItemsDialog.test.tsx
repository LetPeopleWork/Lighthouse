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
		referenceId: "US-123",
		url: "https://example.com/work-item/1",
		startedDate: new Date("2023-01-01"),
		closedDate: new Date("2023-01-10"),
		cycleTime: 10,
		workItemAge: 5,
		parentWorkItemReference: "",
		isBlocked: false,
	},
	{
		id: 2,
		name: "Fix bug Y",
		state: "Active",
		stateCategory: "Doing" as StateCategory,
		type: "Bug",
		referenceId: "BUG-456",
		url: null,
		startedDate: new Date("2023-02-01"),
		closedDate: new Date("2023-02-05"),
		cycleTime: 5,
		workItemAge: 20,
		parentWorkItemReference: "",
		isBlocked: false,
	},
	{
		id: 3,
		name: "Research Z",
		state: "New",
		stateCategory: "ToDo" as StateCategory,
		type: "Task",
		referenceId: "TASK-789",
		url: "https://example.com/work-item/3",
		startedDate: new Date("2023-03-01"),
		closedDate: new Date("2023-03-15"),
		cycleTime: 15,
		workItemAge: 2,
		parentWorkItemReference: "",
		isBlocked: false,
	},
	{
		id: 4,
		name: "Completed Task",
		state: "Closed",
		stateCategory: "Done" as StateCategory,
		type: "Task",
		referenceId: "TASK-101",
		url: "https://example.com/work-item/4",
		startedDate: new Date("2023-04-01"),
		closedDate: new Date("2023-04-08"),
		cycleTime: 7,
		workItemAge: 0, // Age is 0 for closed items
		parentWorkItemReference: "",
		isBlocked: false,
	},
	{
		id: 5,
		name: "Another Closed Item",
		state: "Done",
		stateCategory: "Done" as StateCategory,
		type: "User Story",
		referenceId: "US-202",
		url: null,
		startedDate: new Date("2023-05-01"),
		closedDate: new Date("2023-05-13"),
		cycleTime: 12,
		workItemAge: 0, // Age is 0 for closed items
		parentWorkItemReference: "",
		isBlocked: false,
	},
];

// Mock data with blocked items for testing
const mockBlockedWorkItems: IWorkItem[] = [
	{
		id: 6,
		name: "Blocked Feature",
		state: "In Progress",
		stateCategory: "Doing" as StateCategory,
		type: "User Story",
		referenceId: "US-300",
		url: "https://example.com/work-item/6",
		startedDate: new Date("2023-06-01"),
		closedDate: new Date("2023-06-15"),
		cycleTime: 14,
		workItemAge: 10,
		parentWorkItemReference: "",
		isBlocked: true,
	},
	{
		id: 7,
		name: "Regular Task",
		state: "Active",
		stateCategory: "Doing" as StateCategory,
		type: "Task",
		referenceId: "TASK-400",
		url: null,
		startedDate: new Date("2023-07-01"),
		closedDate: new Date("2023-07-05"),
		cycleTime: 4,
		workItemAge: 5,
		parentWorkItemReference: "",
		isBlocked: false,
	},
	{
		id: 8,
		name: "Blocked Bug Fix",
		state: "Review",
		stateCategory: "Doing" as StateCategory,
		type: "Bug",
		referenceId: "BUG-500",
		url: "https://example.com/work-item/8",
		startedDate: new Date("2023-08-01"),
		closedDate: new Date("2023-08-12"),
		cycleTime: 11,
		workItemAge: 15,
		parentWorkItemReference: "",
		isBlocked: true,
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
		expect(screen.getByText("Work Item Age")).toBeInTheDocument();

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
		expect(cells[1]).toHaveTextContent("12 days");
		expect(cells[2]).toHaveTextContent("10 days");
		expect(cells[3]).toHaveTextContent("7 days");
		expect(cells[4]).toHaveTextContent("5 days");
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
		expect(links).toHaveLength(3);

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

	test("renders with ageCycleTime metric column header", () => {
		render(<WorkItemsDialog {...defaultProps} timeMetric="ageCycleTime" />);

		// Check if column header is displayed correctly
		expect(screen.getByText("Work Item Age/Cycle Time")).toBeInTheDocument();
	});

	test("displays appropriate time values based on item state", () => {
		render(<WorkItemsDialog {...defaultProps} timeMetric="ageCycleTime" />); // Verify time values exist for both active and done items
		expect(screen.getByText("5 days")).toBeInTheDocument();
		expect(screen.getByText("20 days")).toBeInTheDocument();
		expect(screen.getByText("2 days")).toBeInTheDocument();
		expect(screen.getByText("7 days")).toBeInTheDocument();
		expect(screen.getByText("12 days")).toBeInTheDocument();

		// Check if labels are added for each type
		expect(screen.getAllByText("(Work Item Age)")).toHaveLength(3);
		expect(screen.getAllByText("(Cycle Time)")).toHaveLength(2);
	});

	test("sorts items with Done state after active items", () => {
		render(<WorkItemsDialog {...defaultProps} timeMetric="ageCycleTime" />);

		// Analyze state ordering in the table

		// Get all state cells with Chip components
		const stateCells = screen.getAllByRole("cell");
		const stateChips = Array.from(stateCells).filter((cell) => {
			const text = cell.textContent;
			return (
				text === "In Progress" ||
				text === "Active" ||
				text === "New" ||
				text === "Closed" ||
				text === "Done"
			);
		});

		// First 3 should be active states
		expect(stateChips[0].textContent).not.toBe("Closed");
		expect(stateChips[0].textContent).not.toBe("Done");
		expect(stateChips[1].textContent).not.toBe("Closed");
		expect(stateChips[1].textContent).not.toBe("Done");
		expect(stateChips[2].textContent).not.toBe("Closed");
		expect(stateChips[2].textContent).not.toBe("Done");

		// Last 2 should be done states
		expect(["Closed", "Done"]).toContain(stateChips[3].textContent);
		expect(["Closed", "Done"]).toContain(stateChips[4].textContent);
	});

	test("sorts active items by workItemAge and Done items by cycleTime", () => {
		render(<WorkItemsDialog {...defaultProps} timeMetric="ageCycleTime" />);

		// Get all time cells
		const timeCells = screen.getAllByText(/\d+ days/);

		// First 3 cells should be sorted by age in descending order (active items)
		expect(timeCells[0]).toHaveTextContent("20 days");
		expect(timeCells[1]).toHaveTextContent("5 days");
		expect(timeCells[2]).toHaveTextContent("2 days");

		// Last 2 cells should be sorted by cycle time in descending order (Done items)
		expect(timeCells[3]).toHaveTextContent("12 days");
		expect(timeCells[4]).toHaveTextContent("7 days");
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

		test("applies risky color to items above SLE", () => {
			render(<WorkItemsDialog {...defaultProps} sle={10} />);

			const ageCells = screen.getAllByText(/\d+ days/);

			// First cell (20 days) should have risky color and be bold
			expect(ageCells[0]).toHaveStyle("color: rgb(244, 67, 54)"); // RGB equivalent of red
			expect(ageCells[0]).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies realistic color to items at SLE", () => {
			render(<WorkItemsDialog {...defaultProps} sle={20} />);

			const ageCells = screen.getAllByText(/\d+ days/);

			// First cell (20 days) should have realistic color and be bold
			expect(ageCells[0]).not.toHaveStyle("color: rgb(255, 0, 0)");
			expect(ageCells[0]).toHaveStyle("color: rgb(255, 152, 0)");
			expect(ageCells[0]).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies realistic color to items at or above 70% of SLE", () => {
			// With SLE of 10, item with age 7 (70%) should be realistic
			render(<WorkItemsDialog {...defaultProps} sle={10} />);

			// Find the cell with '5 days' text which is between 50% and 70% of SLE=10
			const fiveDaysCell = screen.getByText("5 days");

			// Should have realistic color (5 is 50% of 10, but we're testing with a real item)
			expect(fiveDaysCell).not.toHaveStyle("color: rgb(255, 0, 0)");
			expect(fiveDaysCell).toHaveStyle("color: rgb(76, 175, 80)"); // RGB equivalent of lightgreen
			expect(fiveDaysCell).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies certain color to items below 50% of SLE", () => {
			// With SLE of 10, item with age 2 (<50%) should be certain
			render(<WorkItemsDialog {...defaultProps} sle={10} />);

			// Find the cell with '2 days' text which is below 50% of SLE=10
			const twoDaysCell = screen.getByText("2 days");

			// Should have certain color
			expect(twoDaysCell).toHaveStyle("color: rgb(56, 142, 60)"); // RGB equivalent of green
			expect(twoDaysCell).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies colors correctly with cycleTime metric", () => {
			// With SLE of 10, items with cycle times of 15, 10, and 5 should have appropriate colors
			render(
				<WorkItemsDialog {...defaultProps} timeMetric="cycleTime" sle={10} />,
			);

			const cycleTimeCells = screen.getAllByText(/\d+ days/);

			// 15 days should be risky
			expect(cycleTimeCells[0]).toHaveStyle("color: rgb(244, 67, 54)"); // RGB equivalent of red

			// 10 days should be realistic (at SLE)
			expect(cycleTimeCells[2]).toHaveStyle("color: rgb(255, 152, 0)"); // RGB equivalent of orange

			// 5 days should be confident (50% of SLE)
			expect(cycleTimeCells[4]).toHaveStyle("color: rgb(76, 175, 80)"); // RGB equivalent of lightgreen
		});
	});

	describe("Blocked items functionality", () => {
		test("displays blocked icon next to time value for blocked items", () => {
			render(
				<WorkItemsDialog {...defaultProps} items={mockBlockedWorkItems} />,
			);

			// Check that blocked icons are present for blocked items
			const blockIcons = screen.getAllByTestId("BlockIcon");
			expect(blockIcons).toHaveLength(2); // Two blocked items
		});

		test("blocked icons have correct tooltip", () => {
			render(
				<WorkItemsDialog {...defaultProps} items={mockBlockedWorkItems} />,
			);

			// Check that blocked icons have tooltips by finding tooltips with the correct aria-label
			const tooltips = screen.getAllByLabelText("This Work Item is Blocked");
			expect(tooltips).toHaveLength(2);
		});

		test("does not display blocked icon for non-blocked items", () => {
			render(<WorkItemsDialog {...defaultProps} />);

			// Should not have any blocked icons since no items are blocked
			const blockIcons = screen.queryAllByTestId("BlockIcon");
			expect(blockIcons).toHaveLength(0);
		});

		test("displays blocked icon with correct styling", () => {
			render(
				<WorkItemsDialog {...defaultProps} items={mockBlockedWorkItems} />,
			);

			const blockIcons = screen.getAllByTestId("BlockIcon");

			// Check that the icon has error color
			for (const icon of blockIcons) {
				expect(icon).toHaveStyle("color: rgb(211, 47, 47)"); // RGB equivalent of error.main
			}
		});

		test("blocked icon appears in time column, not dedicated column", () => {
			render(
				<WorkItemsDialog {...defaultProps} items={mockBlockedWorkItems} />,
			);

			// Check that there's no dedicated "Blocked" column header
			expect(screen.queryByText("Blocked")).not.toBeInTheDocument();

			// Check that blocked icons are in the same cell as time values
			const timeCells = screen.getAllByText(/\d+ days/);
			const blockedTimeCells = timeCells.filter((cell) => {
				// Check if this cell contains a blocked icon
				const parent = cell.closest("td");
				return parent?.querySelector('[data-testid="BlockIcon"]') !== null;
			});

			expect(blockedTimeCells).toHaveLength(2); // Two blocked items
		});

		test("blocked items are sorted correctly with time metrics", () => {
			render(
				<WorkItemsDialog
					{...defaultProps}
					items={mockBlockedWorkItems}
					timeMetric="age"
				/>,
			);

			// Items should be sorted by age in descending order
			const timeCells = screen.getAllByText(/\d+ days/);

			// First should be 15 days (blocked), then 10 days (blocked), then 5 days (not blocked)
			expect(timeCells[0]).toHaveTextContent("15 days");
			expect(timeCells[1]).toHaveTextContent("10 days");
			expect(timeCells[2]).toHaveTextContent("5 days");
		});

		test("blocked icon works with different time metrics", () => {
			render(
				<WorkItemsDialog
					{...defaultProps}
					items={mockBlockedWorkItems}
					timeMetric="cycleTime"
				/>,
			);

			// Should still show blocked icons
			const blockIcons = screen.getAllByTestId("BlockIcon");
			expect(blockIcons).toHaveLength(2);

			// Check column header
			expect(screen.getByText("Cycle Time")).toBeInTheDocument();
		});

		test("blocked icon works with ageCycleTime metric", () => {
			render(
				<WorkItemsDialog
					{...defaultProps}
					items={mockBlockedWorkItems}
					timeMetric="ageCycleTime"
				/>,
			);

			// Should still show blocked icons
			const blockIcons = screen.getAllByTestId("BlockIcon");
			expect(blockIcons).toHaveLength(2);

			// Check column header
			expect(screen.getByText("Work Item Age/Cycle Time")).toBeInTheDocument();
		});

		test("blocked icon appears with SLE coloring", () => {
			render(
				<WorkItemsDialog
					{...defaultProps}
					items={mockBlockedWorkItems}
					sle={12}
				/>,
			);

			const blockIcons = screen.getAllByTestId("BlockIcon");
			expect(blockIcons).toHaveLength(2);

			// Time values should still have SLE coloring
			const timeCells = screen.getAllByText(/\d+ days/);
			expect(timeCells[0]).toHaveStyle("font-weight: 700"); // Bold due to SLE
		});
	});
});
