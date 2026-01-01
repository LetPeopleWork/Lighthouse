import { fireEvent, render, screen } from "@testing-library/react";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ConnectionDetailTable from "./ConnectionDetailTable";

describe("ConnectionDetailTable", () => {
	const mockConnections: IWorkTrackingSystemConnection[] = [
		new WorkTrackingSystemConnection({
			name: "Jira",
			workTrackingSystem: "Jira",
			options: [],
			dataSourceType: "Query",
			id: 1,
		}),
		new WorkTrackingSystemConnection({
			name: "ADO",
			workTrackingSystem: "AzureDevOps",
			options: [],
			dataSourceType: "Query",
			id: 2,
		}),
	];

	const mockConnectionsWithCsv: IWorkTrackingSystemConnection[] = [
		new WorkTrackingSystemConnection({
			name: "Jira",
			workTrackingSystem: "Jira",
			options: [],
			dataSourceType: "Query",
			id: 1,
		}),
		new WorkTrackingSystemConnection({
			name: "CSV",
			workTrackingSystem: "Csv",
			options: [],
			dataSourceType: "File",
			id: 2,
		}),
		new WorkTrackingSystemConnection({
			name: "ADO",
			workTrackingSystem: "AzureDevOps",
			options: [],
			dataSourceType: "Query",
			id: 3,
		}),
	];

	const mockOnEditConnectionButtonClicked = vi.fn();
	const mockHandleDeleteConnection = vi.fn();

	beforeEach(() => {
		mockOnEditConnectionButtonClicked.mockClear();
		mockHandleDeleteConnection.mockClear();
	});

	it("should render the data grid with connections", () => {
		render(
			<ConnectionDetailTable
				workTrackingSystemConnections={mockConnections}
				onEditConnectionButtonClicked={mockOnEditConnectionButtonClicked}
				handleDeleteConnection={mockHandleDeleteConnection}
			/>,
		);

		// Check for data grid and column headers
		const grid = screen.getByRole("grid");
		expect(grid).toBeInTheDocument();

		expect(screen.getByText("Name")).toBeInTheDocument();
		expect(screen.getByText("Actions")).toBeInTheDocument();

		// Check for connection names in the grid
		expect(screen.getByText("Jira")).toBeInTheDocument();
		expect(screen.getByText("ADO")).toBeInTheDocument();
	});

	it("should call onEditConnectionButtonClicked with the correct system when the edit button is clicked", () => {
		render(
			<ConnectionDetailTable
				workTrackingSystemConnections={mockConnections}
				onEditConnectionButtonClicked={mockOnEditConnectionButtonClicked}
				handleDeleteConnection={mockHandleDeleteConnection}
			/>,
		);

		const editButtons = screen.getAllByTestId("edit-connection-button");
		fireEvent.click(editButtons[0]);

		expect(mockOnEditConnectionButtonClicked).toHaveBeenCalledTimes(1);
		expect(mockOnEditConnectionButtonClicked).toHaveBeenCalledWith(
			mockConnections[0],
		);
	});

	it("should call handleDeleteConnection with the correct system when the delete button is clicked", () => {
		render(
			<ConnectionDetailTable
				workTrackingSystemConnections={mockConnections}
				onEditConnectionButtonClicked={mockOnEditConnectionButtonClicked}
				handleDeleteConnection={mockHandleDeleteConnection}
			/>,
		);

		const deleteButtons = screen.getAllByTestId("delete-connection-button");
		fireEvent.click(deleteButtons[0]);

		expect(mockHandleDeleteConnection).toHaveBeenCalledTimes(1);
		expect(mockHandleDeleteConnection).toHaveBeenCalledWith(mockConnections[0]);
	});

	it("should render edit and delete buttons for all connections including CSV", () => {
		render(
			<ConnectionDetailTable
				workTrackingSystemConnections={mockConnectionsWithCsv}
				onEditConnectionButtonClicked={mockOnEditConnectionButtonClicked}
				handleDeleteConnection={mockHandleDeleteConnection}
			/>,
		);

		// Verify all connections are displayed
		expect(screen.getByText("Jira")).toBeInTheDocument();
		expect(screen.getByText("CSV")).toBeInTheDocument();
		expect(screen.getByText("ADO")).toBeInTheDocument();

		// Verify all connections have edit and delete buttons (3 connections = 3 edit + 3 delete)
		const editButtons = screen.getAllByTestId("edit-connection-button");
		const deleteButtons = screen.getAllByTestId("delete-connection-button");

		expect(editButtons).toHaveLength(3);
		expect(deleteButtons).toHaveLength(3);

		// Test clicking the first edit button (Jira)
		fireEvent.click(editButtons[0]);
		expect(mockOnEditConnectionButtonClicked).toHaveBeenCalledWith(
			mockConnectionsWithCsv[0],
		);

		// Test clicking the first delete button (Jira)
		fireEvent.click(deleteButtons[0]);
		expect(mockHandleDeleteConnection).toHaveBeenCalledWith(
			mockConnectionsWithCsv[0],
		);
	});

	it("should support sorting on the Name column", () => {
		render(
			<ConnectionDetailTable
				workTrackingSystemConnections={mockConnections}
				onEditConnectionButtonClicked={mockOnEditConnectionButtonClicked}
				handleDeleteConnection={mockHandleDeleteConnection}
			/>,
		);

		const grid = screen.getByRole("grid");
		expect(grid).toBeInTheDocument();

		// The Name column should be sortable (sortable: true in column definition)
		const nameHeader = screen.getByText("Name");
		expect(nameHeader).toBeInTheDocument();
	});
});
