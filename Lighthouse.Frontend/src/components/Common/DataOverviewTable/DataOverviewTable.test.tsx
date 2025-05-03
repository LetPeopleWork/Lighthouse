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
	},
	{
		id: 2,
		name: "Item 2",
		remainingWork: 20,
		remainingFeatures: 15,
		features: [],
		totalWork: 20,
	},
	{
		id: 3,
		name: "Another Item",
		remainingWork: 30,
		remainingFeatures: 25,
		features: [],
		totalWork: 33,
	},
];

describe("DataOverviewTable", () => {
	it("renders correctly", () => {
		renderWithRouter(
			<DataOverviewTable data={sampleData} api="api" onDelete={vi.fn()} />,
		);
		expect(screen.getByTestId("table-container")).toBeInTheDocument();
	});

	it("displays all items from the data passed in", () => {
		renderWithRouter(
			<DataOverviewTable data={sampleData} api="api" onDelete={vi.fn()} />,
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
				totalWork: 33,
			},
			{
				id: 1,
				name: "Apple Item",
				remainingWork: 10,
				remainingFeatures: 5,
				features: [],
				totalWork: 20,
			},
			{
				id: 2,
				name: "Banana Item",
				remainingWork: 20,
				remainingFeatures: 15,
				features: [],
				totalWork: 20,
			},
		];

		renderWithRouter(
			<DataOverviewTable data={unsortedData} api="api" onDelete={vi.fn()} />,
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
			<DataOverviewTable data={sampleData} api="api" onDelete={vi.fn()} />,
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
			<DataOverviewTable data={sampleData} api="api" onDelete={vi.fn()} />,
		);
		const filterInput = screen.getByRole("textbox", { name: "" });

		fireEvent.change(filterInput, { target: { value: "Non-existing Item" } });
		expect(screen.getByTestId("no-items-message")).toBeInTheDocument();
	});

	it('navigates to the new item page when "Add New" button is clicked', () => {
		const navigate = vi.fn();
		vi.mocked(useNavigate).mockReturnValue(navigate);

		renderWithRouter(
			<DataOverviewTable data={sampleData} api="api" onDelete={vi.fn()} />,
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
				onDelete={vi.fn()}
				onFilterChange={onFilterChange}
			/>,
		);

		const filterInput = screen.getByRole("textbox", { name: "" });
		fireEvent.change(filterInput, { target: { value: "Test Filter" } });

		expect(onFilterChange).toHaveBeenCalledWith("Test Filter");
	});
});
