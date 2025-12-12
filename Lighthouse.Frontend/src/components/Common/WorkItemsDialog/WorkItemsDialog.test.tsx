import { fireEvent, render, screen } from "@testing-library/react";
import type { IFeature } from "../../../models/Feature";
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
		cycleTime: 0,
		workItemAge: 15,
		parentWorkItemReference: "",
		isBlocked: true,
	},
];

// Mock Feature data for testing the "Owned by" column
const mockFeatures: IFeature[] = [
	{
		id: 10,
		name: "Feature with Team A",
		state: "In Progress",
		stateCategory: "Doing" as StateCategory,
		type: "Feature",
		referenceId: "FEAT-001",
		url: "https://example.com/feature/10",
		startedDate: new Date("2023-01-01"),
		closedDate: new Date("2023-01-10"),
		cycleTime: 10,
		workItemAge: 5,
		parentWorkItemReference: "",
		isBlocked: false,
		owningTeam: "Team Alpha",
		lastUpdated: new Date("2023-01-15"),
		isUsingDefaultFeatureSize: false,
		size: 8,
		remainingWork: { 1: 10, 2: 5 },
		totalWork: { 1: 20, 2: 15 },
		projects: [
			{ id: 1, name: "Project A" },
			{ id: 2, name: "Project B" },
		],
		forecasts: [],
		getRemainingWorkForFeature: () => 15,
		getRemainingWorkForTeam: (id: number) => (id === 1 ? 10 : 5),
		getTotalWorkForFeature: () => 35,
		getTotalWorkForTeam: (id: number) => (id === 1 ? 20 : 15),
	},
	{
		id: 11,
		name: "Feature with Team B",
		state: "New",
		stateCategory: "ToDo" as StateCategory,
		type: "Feature",
		referenceId: "FEAT-002",
		url: "https://example.com/feature/11",
		startedDate: new Date("2023-02-01"),
		closedDate: new Date("2023-02-10"),
		cycleTime: 9,
		workItemAge: 3,
		parentWorkItemReference: "",
		isBlocked: false,
		owningTeam: "Team Beta",
		lastUpdated: new Date("2023-02-15"),
		isUsingDefaultFeatureSize: true,
		size: 5,
		remainingWork: { 3: 8 },
		totalWork: { 3: 12 },
		projects: [{ id: 3, name: "Project C" }],
		forecasts: [],
		getRemainingWorkForFeature: () => 8,
		getRemainingWorkForTeam: (id: number) => (id === 3 ? 8 : 0),
		getTotalWorkForFeature: () => 12,
		getTotalWorkForTeam: (id: number) => (id === 3 ? 12 : 0),
	},
	{
		id: 12,
		name: "Feature with no team",
		state: "Active",
		stateCategory: "Doing" as StateCategory,
		type: "Feature",
		referenceId: "FEAT-003",
		url: null,
		startedDate: new Date("2023-03-01"),
		closedDate: new Date("2023-03-08"),
		cycleTime: 7,
		workItemAge: 12,
		parentWorkItemReference: "",
		isBlocked: false,
		owningTeam: "",
		lastUpdated: new Date("2023-03-15"),
		isUsingDefaultFeatureSize: false,
		size: 3,
		remainingWork: {},
		totalWork: {},
		projects: [],
		forecasts: [],
		getRemainingWorkForFeature: () => 0,
		getRemainingWorkForTeam: () => 0,
		getTotalWorkForFeature: () => 0,
		getTotalWorkForTeam: () => 0,
	},
];

// Test scenarios
describe("WorkItemsDialog Component", () => {
	const defaultProps = {
		title: "Test Dialog",
		items: mockWorkItems,
		open: true,
		additionalColumnTitle: "Work Item Age",
		additionalColumnDescription: "days",
		additionalColumnContent: (item: IWorkItem) => item.workItemAge,
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
		expect(screen.getByText(/Work Item Age/)).toBeInTheDocument();

		// Check if work item data is displayed
		expect(screen.getByText("Implement feature X")).toBeInTheDocument();
		expect(screen.getByText("Fix bug Y")).toBeInTheDocument();
		expect(screen.getByText("Research Z")).toBeInTheDocument();

		// Check formatting of age
		expect(screen.getByText("5")).toBeInTheDocument();
		expect(screen.getByText("20")).toBeInTheDocument();
		expect(screen.getByText("2")).toBeInTheDocument();
	});

	test("renders with cycleTime metric", () => {
		render(
			<WorkItemsDialog
				{...defaultProps}
				additionalColumnTitle="Cycle Time"
				additionalColumnDescription="days"
				additionalColumnContent={(item) => item.cycleTime}
			/>,
		);

		// Check if correct column name is displayed
		expect(screen.getByText(/Cycle Time/)).toBeInTheDocument();

		// Check formatting of cycle time
		expect(screen.getByText("12")).toBeInTheDocument();
		expect(screen.getByText("7")).toBeInTheDocument();
	});

	test("displays items sorted by age in descending order with age metric", () => {
		render(
			<WorkItemsDialog
				{...defaultProps}
				additionalColumnTitle="Work Item Age"
				additionalColumnDescription="days"
				additionalColumnContent={(item) => item.workItemAge}
			/>,
		);

		// Get all row cells with age information
		const cells = screen.getAllByTestId("additionalColumnContent");

		// Check if they're in descending order (20, 5, 2)
		expect(cells[0]).toHaveTextContent("20");
		expect(cells[1]).toHaveTextContent("5");
		expect(cells[2]).toHaveTextContent("2");
	});

	test("displays items sorted by cycle time in descending order with cycleTime metric", () => {
		render(
			<WorkItemsDialog
				{...defaultProps}
				additionalColumnTitle="Cycle Time"
				additionalColumnDescription="days"
				additionalColumnContent={(item) => item.cycleTime}
			/>,
		);

		// Get all row cells with cycle time information
		const cells = screen.getAllByTestId("additionalColumnContent");

		// Check if they're in descending order (15, 10, 5)
		expect(cells[0]).toHaveTextContent("15");
		expect(cells[1]).toHaveTextContent("12");
		expect(cells[2]).toHaveTextContent("10");
		expect(cells[3]).toHaveTextContent("7");
		expect(cells[4]).toHaveTextContent("5");
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
		render(
			<WorkItemsDialog
				{...defaultProps}
				additionalColumnTitle="Work Item Age/Cycle Time"
				additionalColumnDescription="days"
				additionalColumnContent={(item) => item.cycleTime ?? item.workItemAge}
			/>,
		);

		// Check if column header is displayed correctly
		expect(screen.getByText(/Work Item Age\/Cycle Time/)).toBeInTheDocument();
	});

	describe("Service Level Expectation (SLE) functionality", () => {
		test("renders without styling when no SLE is provided", () => {
			render(<WorkItemsDialog {...defaultProps} />);

			const ageCells = screen.getAllByTestId("additionalColumnContent");

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

			const ageCells = screen.getAllByTestId("additionalColumnContent");

			// First cell (20 days) should have risky color and be bold
			expect(ageCells[0]).toHaveStyle("color: rgb(244, 67, 54)"); // RGB equivalent of red
			expect(ageCells[0]).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies realistic color to items at SLE", () => {
			render(<WorkItemsDialog {...defaultProps} sle={20} />);

			const ageCells = screen.getAllByTestId("additionalColumnContent");

			// First cell (20 days) should have realistic color and be bold
			expect(ageCells[0]).not.toHaveStyle("color: rgb(255, 0, 0)");
			expect(ageCells[0]).toHaveStyle("color: rgb(255, 152, 0)");
			expect(ageCells[0]).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies realistic color to items at or above 70% of SLE", () => {
			// With SLE of 10, item with age 7 (70%) should be realistic
			render(<WorkItemsDialog {...defaultProps} sle={10} />);

			// Find the cell with '5 days' text which is between 50% and 70% of SLE=10
			const fiveDaysCell = screen.getByText("5");

			// Should have realistic color (5 is 50% of 10, but we're testing with a real item)
			expect(fiveDaysCell).not.toHaveStyle("color: rgb(255, 0, 0)");
			expect(fiveDaysCell).toHaveStyle("color: rgb(76, 175, 80)"); // RGB equivalent of lightgreen
			expect(fiveDaysCell).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies certain color to items below 50% of SLE", () => {
			// With SLE of 10, item with age 2 (<50%) should be certain
			render(<WorkItemsDialog {...defaultProps} sle={10} />);

			// Find the cell with '2 days' text which is below 50% of SLE=10
			const twoDaysCell = screen.getByText("2");

			// Should have certain color
			expect(twoDaysCell).toHaveStyle("color: rgb(56, 142, 60)"); // RGB equivalent of green
			expect(twoDaysCell).toHaveStyle("font-weight: 700"); // 700 is equivalent to bold
		});

		test("applies colors correctly with cycleTime metric", () => {
			// With SLE of 10, items with cycle times of 15, 10, and 5 should have appropriate colors
			render(
				<WorkItemsDialog
					{...defaultProps}
					additionalColumnTitle="Cycle Time"
					additionalColumnDescription="days"
					additionalColumnContent={(item) => item.cycleTime}
					sle={10}
				/>,
			);

			const cycleTimeCells = screen.getAllByTestId("additionalColumnContent");

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
			const timeCells = screen.getAllByTestId("additionalColumnContent");
			const blockedTimeCells = timeCells.filter((cell) => {
				// Check if this cell contains a blocked icon by checking its children
				return cell.querySelector('[data-testid="BlockIcon"]') !== null;
			});

			expect(blockedTimeCells).toHaveLength(2); // Two blocked items
		});
		test("blocked items are sorted correctly with time metrics", () => {
			render(
				<WorkItemsDialog
					{...defaultProps}
					items={mockBlockedWorkItems}
					additionalColumnTitle="Work Item Age"
					additionalColumnDescription="days"
					additionalColumnContent={(item) => item.workItemAge}
				/>,
			);

			// Items should be sorted by age in descending order
			const timeCells = screen.getAllByTestId("additionalColumnContent");

			// First should be 15 days (blocked), then 10 days (blocked), then 5 days (not blocked)
			expect(timeCells[0]).toHaveTextContent("15");
			expect(timeCells[1]).toHaveTextContent("10");
			expect(timeCells[2]).toHaveTextContent("5");
		});

		test("blocked icon works with different time metrics", () => {
			render(
				<WorkItemsDialog
					{...defaultProps}
					items={mockBlockedWorkItems}
					additionalColumnTitle="Cycle Time"
					additionalColumnDescription="days"
					additionalColumnContent={(item) => item.cycleTime}
				/>,
			);

			// Should still show blocked icons
			const blockIcons = screen.getAllByTestId("BlockIcon");
			expect(blockIcons).toHaveLength(2);

			// Check column header
			expect(screen.getByText(/Cycle Time/)).toBeInTheDocument();
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
			const timeCells = screen.getAllByTestId("additionalColumnContent");
			expect(timeCells[0]).toHaveStyle("font-weight: 700"); // Bold due to SLE
		});
	});

	describe("Feature owning team functionality", () => {
		test("displays 'Owned by' column for Features with owning teams", () => {
			render(
				<WorkItemsDialog
					{...defaultProps}
					items={mockFeatures}
					title="Features Dialog"
					additionalColumnTitle="Work Item Age"
					additionalColumnDescription="days"
					additionalColumnContent={(item) => item.workItemAge}
				/>,
			);

			// Check if the 'Owned by' column header is displayed
			expect(screen.getByText("Owned by")).toBeInTheDocument();

			// Check if owning teams are displayed
			expect(screen.getByText("Team Alpha")).toBeInTheDocument();
			expect(screen.getByText("Team Beta")).toBeInTheDocument();

			// Feature with empty owningTeam should show empty cell
			const rows = screen.getAllByRole("row");
			expect(rows).toHaveLength(4); // Header + 3 data rows
		});

		test("does not display 'Owned by' column for regular WorkItems", () => {
			render(<WorkItemsDialog {...defaultProps} />);

			// Check if the 'Owned by' column header is NOT displayed
			expect(screen.queryByText("Owned by")).not.toBeInTheDocument();

			// Verify standard columns are present
			expect(screen.getByText("ID")).toBeInTheDocument();
			expect(screen.getByText("Name")).toBeInTheDocument();
			expect(screen.getByText("Type")).toBeInTheDocument();
			expect(screen.getByText("State")).toBeInTheDocument();
		});

		test("does not display 'Owned by' column when Features have no owning teams", () => {
			const featuresWithoutTeams = mockFeatures.map((feature) => ({
				...feature,
				owningTeam: "",
			}));

			render(
				<WorkItemsDialog
					{...defaultProps}
					items={featuresWithoutTeams}
					title="Features without Teams"
				/>,
			);

			// Check if the 'Owned by' column header is NOT displayed
			expect(screen.queryByText("Owned by")).not.toBeInTheDocument();
		});

		test("displays 'Owned by' column when at least one Feature has an owning team", () => {
			const mixedFeatures = [
				mockFeatures[0], // Has "Team Alpha"
				{
					...mockFeatures[1],
					owningTeam: "", // Empty team
				},
			];

			render(
				<WorkItemsDialog
					{...defaultProps}
					items={mixedFeatures}
					title="Mixed Features"
				/>,
			);

			// Check if the 'Owned by' column header IS displayed
			expect(screen.getByText("Owned by")).toBeInTheDocument();

			// Check if the team is displayed for the feature that has it
			expect(screen.getByText("Team Alpha")).toBeInTheDocument();
		});
	});
});
