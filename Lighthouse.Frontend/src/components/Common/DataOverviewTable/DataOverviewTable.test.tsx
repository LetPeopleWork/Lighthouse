import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import DataOverviewTable from "./DataOverviewTable";

vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
	return {
		...actual,
		useNavigate: vi.fn(),
	};
});

// Mock matchMedia before each test
beforeEach(() => {
	Object.defineProperty(globalThis, "matchMedia", {
		writable: true,
		value: vi.fn().mockImplementation((query) => ({
			matches: false,
			media: query,
			onchange: null,
			addListener: vi.fn(),
			removeListener: vi.fn(),
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			dispatchEvent: vi.fn(),
		})),
	});
});

const renderWithRouter = (ui: React.ReactNode) => {
	return render(<BrowserRouter>{ui}</BrowserRouter>);
};

const sampleData: IFeatureOwner[] = [
	{
		id: 1,
		name: "Item 1",
		remainingFeatures: 5,
		features: [],
		tags: ["critical", "frontend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
	{
		id: 2,
		name: "Item 2",
		remainingFeatures: 15,
		features: [],
		tags: ["backend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
	{
		id: 3,
		name: "Another Item",
		remainingFeatures: 25,
		features: [],
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
				filterText=""
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
				filterText=""
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
				filterText=""
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
				filterText="Item"
			/>,
		);

		expect(screen.getByText("Item 1")).toBeInTheDocument();
		expect(screen.getByText("Item 2")).toBeInTheDocument();
		expect(screen.queryByText("Another Item")).toBeInTheDocument();
	});

	it("displays the custom message when no item matches filter", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
				filterText="Non-existing Item"
			/>,
		);

		expect(screen.getByTestId("no-items-message")).toBeInTheDocument();
	});

	it("initializes with provided filter text", () => {
		const initialFilterText = "Item";
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
				filterText={initialFilterText}
			/>,
		);

		// Verify only matching items are displayed
		expect(screen.getByText("Item 1")).toBeInTheDocument();
		expect(screen.getByText("Item 2")).toBeInTheDocument();
		expect(screen.queryByText("Another Item")).toBeInTheDocument();
	});

	it("displays tags as chips for each item", () => {
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
				filterText=""
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
		renderWithRouter(
			<DataOverviewTable
				data={sampleData}
				title="api"
				api="api"
				onDelete={vi.fn()}
				filterText=""
			/>,
		);

		// Check the item with no tags
		const item3Row = screen.getByTestId("table-row-3");

		// No additional tags should be in this row
		const tagsInRow3 = item3Row.querySelectorAll(".MuiChip-root");
		expect(tagsInRow3.length).toBe(0);
	});

	it("should not display empty tags", () => {
		const dataWithEmptyTag: IFeatureOwner[] = [
			{
				id: 1,
				name: "Item with empty tag",
				remainingFeatures: 5,
				features: [],
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
				filterText=""
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
				filterText="critical"
			/>,
		);

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
				filterText="front"
			/>,
		);

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
				filterText="backend"
			/>,
		);

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
				filterText="asdfasdfasdfasdf"
			/>,
		);

		expect(screen.queryByText("Item 1")).not.toBeInTheDocument();
		expect(screen.queryByText("Item 2")).not.toBeInTheDocument();
		expect(screen.queryByText("Another Item")).not.toBeInTheDocument();
		expect(screen.getByTestId("no-items-message")).toBeInTheDocument();
	});

	it("shows demo data link when no data is available", () => {
		renderWithRouter(
			<DataOverviewTable
				data={[]}
				title="Test Items"
				api="api"
				onDelete={vi.fn()}
				filterText=""
			/>,
		);

		expect(screen.getByTestId("empty-items-message")).toBeInTheDocument();

		// Check that the demo data link is present and has correct href
		const demoDataLink = screen.getByText("Load Demo Data");
		expect(demoDataLink).toBeInTheDocument();
		expect(demoDataLink.closest("a")).toHaveAttribute(
			"href",
			"/settings?tab=demodata",
		);

		// Check that the documentation link is also present
		const docLink = screen.getByText("Check the documentation");
		expect(docLink).toBeInTheDocument();
		expect(docLink.closest("a")).toHaveAttribute(
			"href",
			"https://docs.lighthouse.letpeople.work",
		);
	});
});
