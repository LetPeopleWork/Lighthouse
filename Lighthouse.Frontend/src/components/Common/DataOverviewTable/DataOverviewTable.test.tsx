import { fireEvent, render, screen } from "@testing-library/react";
import { BrowserRouter, useNavigate } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import DataOverviewTable from "./DataOverviewTable";

vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
	return {
		...actual,
		useNavigate: vi.fn(),
	};
});

const renderWithRouter = (ui: React.ReactNode) => {
	return render(<BrowserRouter>{ui}</BrowserRouter>);
};

const sampleData: IFeatureOwner[] = [
	{
		id: 1,
		name: "Item 1",
		remainingWork: 10,
		remainingFeatures: 5,
		features: [],
		totalWork: 20,
		tags: ["critical", "frontend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
	{
		id: 2,
		name: "Item 2",
		remainingWork: 20,
		remainingFeatures: 15,
		features: [],
		totalWork: 20,
		tags: ["backend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
	{
		id: 3,
		name: "Another Item",
		remainingWork: 30,
		remainingFeatures: 25,
		features: [],
		totalWork: 33,
		tags: [],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
];

describe("DataOverviewTable", () => {
	it("renders correctly", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);
		expect(screen.getByTestId("table-container")).toBeInTheDocument();
	});

	it("displays all items from the data passed in", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);
		for (const item of sampleData) {
			expect(screen.getByTestId(`table-row-${item.id}`)).toBeInTheDocument();
			expect(screen.getByText(item.name)).toBeInTheDocument();
		}
	});

	it("displays items in alphabetical order by name", () => {
		const unsortedData = [
			{
				id: 3,
				name: "Zebra Item",
				remainingWork: 30,
				remainingFeatures: 25,
				features: [],
				tags: [],
				totalWork: 33,
				lastUpdated: new Date(),
				serviceLevelExpectationProbability: 0,
				serviceLevelExpectationRange: 0,
				systemWIPLimit: 0,
			},
			{
				id: 1,
				name: "Apple Item",
				remainingWork: 10,
				remainingFeatures: 5,
				features: [],
				tags: [],
				totalWork: 20,
				lastUpdated: new Date(),
				serviceLevelExpectationProbability: 0,
				serviceLevelExpectationRange: 0,
				systemWIPLimit: 0,
			},
			{
				id: 2,
				name: "Banana Item",
				remainingWork: 20,
				remainingFeatures: 15,
				features: [],
				tags: [],
				totalWork: 20,
				lastUpdated: new Date(),
				serviceLevelExpectationProbability: 0,
				serviceLevelExpectationRange: 0,
				systemWIPLimit: 0,
			},
		];

		renderWithRouter(
			<DataOverviewTable
				data={unsortedData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);

		// Get all row elements
		const rows = screen.getAllByTestId(/^table-row-/);

		// Check that they appear in alphabetical order in the DOM
		expect(rows[0]).toHaveTextContent("Apple Item");
		expect(rows[1]).toHaveTextContent("Banana Item");
		expect(rows[2]).toHaveTextContent("Zebra Item");
	});

	it("filters properly", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);
		const filterInput = screen.getByRole("textbox", { name: "" });

		fireEvent.change(filterInput, { target: { value: "Item" } });
		expect(screen.getByText("Item 1")).toBeInTheDocument();
		expect(screen.getByText("Item 2")).toBeInTheDocument();
		expect(screen.queryByText("Another Item")).toBeInTheDocument();

		fireEvent.change(filterInput, { target: { value: "Another" } });
		expect(screen.queryByText("Item 1")).not.toBeInTheDocument();
		expect(screen.queryByText("Item 2")).not.toBeInTheDocument();
		expect(screen.getByText("Another Item")).toBeInTheDocument();
	});

	it("displays the custom message when no item matches filter", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);
		const filterInput = screen.getByRole("textbox", { name: "" });

		fireEvent.change(filterInput, { target: { value: "Non-existing Item" } });
		expect(screen.getByTestId("no-items-message")).toBeInTheDocument();
	});

	it('navigates to the new item page when "Add New" button is clicked', () => {
		const navigate = vi.fn();
		vi.mocked(useNavigate).mockReturnValue(navigate);

		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);

		// Find the button by startsWith to handle dynamic text
		const addButton = screen.getByText((content) =>
			content.startsWith("Add New"),
		);
		fireEvent.click(addButton);

		expect(navigate).toHaveBeenCalledWith("/api/new");
	});

	it("initializes with provided filter text", () => {
		const initialFilterText = "Item";
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
				initialFilterText={initialFilterText}
			/>,
		);

		const filterInput = screen.getByRole("textbox", { name: "" });
		expect(filterInput).toHaveValue(initialFilterText);

		// Verify only matching items are displayed
		expect(screen.getByText("Item 1")).toBeInTheDocument();
		expect(screen.getByText("Item 2")).toBeInTheDocument();
		expect(screen.queryByText("Another Item")).toBeInTheDocument();
	});

	it("calls onFilterChange callback when filter changes", () => {
		const onFilterChange = vi.fn();
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				api="api"
				title="api"
				onDelete={vi.fn()}
				onFilterChange={onFilterChange}
			/>,
		);

		const filterInput = screen.getByRole("textbox", { name: "" });
		fireEvent.change(filterInput, { target: { value: "Test Filter" } });

		expect(onFilterChange).toHaveBeenCalledWith("Test Filter");
	});

	it("displays tags as chips for each item", () => {
		// Prevent mobile view to make sure tags are visible
		vi.spyOn(window, "matchMedia").mockImplementation((query) => ({
			matches: false,
			media: query,
			onchange: null,
			addListener: vi.fn(),
			removeListener: vi.fn(),
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			dispatchEvent: vi.fn(),
		}));

		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);

		// Check the first item with multiple tags
		const item1Row = screen.getByTestId("table-row-1");
		const criticalTag = screen.getByText("critical");
		const frontendTag = screen.getByText("frontend");

		expect(criticalTag).toBeInTheDocument();
		expect(frontendTag).toBeInTheDocument();
		expect(item1Row).toContainElement(criticalTag);
		expect(item1Row).toContainElement(frontendTag);

		// Check the second item with a single tag
		const item2Row = screen.getByTestId("table-row-2");
		const backendTag = screen.getByText("backend");

		expect(backendTag).toBeInTheDocument();
		expect(item2Row).toContainElement(backendTag);

		// Check that tags are displayed as chips with the expected style (outlined variant)
		const tagChips = document.querySelectorAll(".MuiChip-outlined");
		expect(tagChips.length).toBe(3); // Total 3 tags across all items
	});

	it("displays no tags for items without tags", () => {
		// Prevent mobile view to make sure tags column is visible
		vi.spyOn(window, "matchMedia").mockImplementation((query) => ({
			matches: false,
			media: query,
			onchange: null,
			addListener: vi.fn(),
			removeListener: vi.fn(),
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			dispatchEvent: vi.fn(),
		}));

		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);

		// Check the item with no tags
		const item3Row = screen.getByTestId("table-row-3");

		// No additional tags should be in this row
		const tagsInRow3 = item3Row.querySelectorAll(".MuiChip-root");
		expect(tagsInRow3.length).toBe(0);
	});

	it("should not display empty tags", () => {
		// Prevent mobile view to make sure tags column is visible
		vi.spyOn(window, "matchMedia").mockImplementation((query) => ({
			matches: false,
			media: query,
			onchange: null,
			addListener: vi.fn(),
			removeListener: vi.fn(),
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			dispatchEvent: vi.fn(),
		}));

		const dataWithEmptyTag: IFeatureOwner[] = [
			{
				id: 1,
				name: "Item with empty tag",
				remainingWork: 10,
				remainingFeatures: 5,
				features: [],
				totalWork: 20,
				tags: ["valid-tag", "", "  ", "another-valid-tag"],
				lastUpdated: new Date(),
				serviceLevelExpectationProbability: 0,
				serviceLevelExpectationRange: 0,
				systemWIPLimit: 0,
			},
		];

		renderWithRouter(
			<DataOverviewTable
				data={dataWithEmptyTag}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);

		expect(screen.getByText("valid-tag")).toBeInTheDocument();
		expect(screen.getByText("another-valid-tag")).toBeInTheDocument();

		const tagChips = document.querySelectorAll(".MuiChip-outlined");
		expect(tagChips.length).toEqual(2);
	});

	it("filters items by tag", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);
		const filterInput = screen.getByRole("textbox", { name: "" });

		// Filter by "critical" tag
		fireEvent.change(filterInput, { target: { value: "critical" } });
		expect(screen.getByText("Item 1")).toBeInTheDocument(); // Item 1 has "critical" tag
		expect(screen.queryByText("Item 2")).not.toBeInTheDocument(); // Item 2 doesn't have "critical" tag
		expect(screen.queryByText("Another Item")).not.toBeInTheDocument(); // Another Item doesn't have "critical" tag
	});

	it("filters items by partial tag match", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);
		const filterInput = screen.getByRole("textbox", { name: "" });

		// Filter by partial tag match "front"
		fireEvent.change(filterInput, { target: { value: "front" } });
		expect(screen.getByText("Item 1")).toBeInTheDocument(); // Item 1 has "frontend" tag
		expect(screen.queryByText("Item 2")).not.toBeInTheDocument(); // Item 2 doesn't have a tag containing "front"
		expect(screen.queryByText("Another Item")).not.toBeInTheDocument(); // Another Item doesn't have tags
	});

	it("filters by tag when name doesn't match", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);
		const filterInput = screen.getByRole("textbox", { name: "" });

		// Filter by "backend" tag (none of the item names contain "backend")
		fireEvent.change(filterInput, { target: { value: "backend" } });
		expect(screen.queryByText("Item 1")).not.toBeInTheDocument(); // Item 1 doesn't have "backend" tag
		expect(screen.getByText("Item 2")).toBeInTheDocument(); // Item 2 has "backend" tag
		expect(screen.queryByText("Another Item")).not.toBeInTheDocument(); // Another Item doesn't have tags
	});

	it("should not show items when neither name nor tags match", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
			/>,
		);
		const filterInput = screen.getByRole("textbox", { name: "" });

		// Filter by a string that doesn't match any name or tag
		fireEvent.change(filterInput, { target: { value: "nonexistent" } });
		expect(screen.queryByText("Item 1")).not.toBeInTheDocument();
		expect(screen.queryByText("Item 2")).not.toBeInTheDocument();
		expect(screen.queryByText("Another Item")).not.toBeInTheDocument();
		expect(screen.getByTestId("no-items-message")).toBeInTheDocument();
	});

	describe("Add button restrictions", () => {
		it("disables add button when disableAdd prop is true", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleData}
					title="api"
					api="api"
					onDelete={vi.fn()}
					disableAdd={true}
				/>,
			);

			const addButton = screen.getByText((content) =>
				content.startsWith("Add New"),
			);
			expect(addButton).toBeDisabled();
		});

		it("shows tooltip when addButtonTooltip prop is provided", () => {
			const tooltipText = "Premium license required";
			renderWithRouter(
				<DataOverviewTable
					data={sampleData}
					title="api"
					api="api"
					onDelete={vi.fn()}
					addButtonTooltip={tooltipText}
				/>,
			);

			// Check that the tooltip is set as aria-label
			expect(screen.getByLabelText(tooltipText)).toBeInTheDocument();
		});

		it("disables add button and shows tooltip when both props are provided", () => {
			const tooltipText = "License restriction";
			renderWithRouter(
				<DataOverviewTable
					data={sampleData}
					title="api"
					api="api"
					onDelete={vi.fn()}
					disableAdd={true}
					addButtonTooltip={tooltipText}
				/>,
			);

			const addButton = screen.getByText((content) =>
				content.startsWith("Add New"),
			);
			expect(addButton).toBeDisabled();
			expect(screen.getByLabelText(tooltipText)).toBeInTheDocument();
		});
	});
});
