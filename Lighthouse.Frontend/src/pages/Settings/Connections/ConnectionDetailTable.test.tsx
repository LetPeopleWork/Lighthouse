import { fireEvent, render, screen } from "@testing-library/react";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ConnectionDetailTable from "./ConnectionDetailTable";

describe("ConnectionDetailTable", () => {
	const mockConnections: IWorkTrackingSystemConnection[] = [
		new WorkTrackingSystemConnection("Jira", "Jira", [], "Query", 1),
		new WorkTrackingSystemConnection("ADO", "AzureDevOps", [], "Query", 2),
	];

	const mockConnectionsWithCsv: IWorkTrackingSystemConnection[] = [
		new WorkTrackingSystemConnection("Jira", "Jira", [], "Query", 1),
		new WorkTrackingSystemConnection("CSV", "Csv", [], "File", 2),
		new WorkTrackingSystemConnection("ADO", "AzureDevOps", [], "Query", 3),
	];

	const mockOnEditConnectionButtonClicked = vi.fn();
	const mockHandleDeleteConnection = vi.fn();

	beforeEach(() => {
		mockOnEditConnectionButtonClicked.mockClear();
		mockHandleDeleteConnection.mockClear();
	});

	it("should render the table with connections", () => {
		render(
			<ConnectionDetailTable
				workTrackingSystemConnections={mockConnections}
				onEditConnectionButtonClicked={mockOnEditConnectionButtonClicked}
				handleDeleteConnection={mockHandleDeleteConnection}
			/>,
		);

		expect(screen.getByText("Name")).toBeInTheDocument();
		expect(screen.getByText("Actions")).toBeInTheDocument();
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

		const editButtons = screen.getAllByTestId("EditIcon");
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

		const deleteButtons = screen.getAllByTestId("DeleteIcon");
		fireEvent.click(deleteButtons[0]);

		expect(mockHandleDeleteConnection).toHaveBeenCalledTimes(1);
		expect(mockHandleDeleteConnection).toHaveBeenCalledWith(mockConnections[0]);
	});

	// Note: The following tests verify the component can handle CSV connections,
	// but in practice CSV connections are filtered out at the parent component level

	it("should render edit and delete buttons for non-CSV connections when CSV is present", () => {
		render(
			<ConnectionDetailTable
				workTrackingSystemConnections={mockConnectionsWithCsv}
				onEditConnectionButtonClicked={mockOnEditConnectionButtonClicked}
				handleDeleteConnection={mockHandleDeleteConnection}
			/>,
		);

		// Find the Jira connection row and verify it has buttons
		const jiraRow = screen.getByText("Jira").closest("tr");
		expect(jiraRow).toBeInTheDocument();

		// The Jira row should have edit and delete buttons
		const editButtons = screen.getAllByTestId("EditIcon");
		const deleteButtons = screen.getAllByTestId("DeleteIcon");

		// Click on the first edit button (should be Jira since CSV buttons are hidden)
		fireEvent.click(editButtons[0]);
		expect(mockOnEditConnectionButtonClicked).toHaveBeenCalledWith(
			mockConnectionsWithCsv[0],
		); // Jira connection

		// Click on the first delete button (should be Jira since CSV buttons are hidden)
		fireEvent.click(deleteButtons[0]);
		expect(mockHandleDeleteConnection).toHaveBeenCalledWith(
			mockConnectionsWithCsv[0],
		); // Jira connection
	});

	it("should display CSV connection name but with disabled delete button", () => {
		render(
			<ConnectionDetailTable
				workTrackingSystemConnections={mockConnectionsWithCsv}
				onEditConnectionButtonClicked={mockOnEditConnectionButtonClicked}
				handleDeleteConnection={mockHandleDeleteConnection}
			/>,
		);

		// CSV should be displayed in the table
		expect(screen.getByText("CSV")).toBeInTheDocument();

		// Find the CSV row
		const csvRow = screen.getByText("CSV").closest("tr");
		expect(csvRow).toBeInTheDocument();

		// The CSV row should contain the edit button and a disabled delete button
		const csvActionsCell = csvRow?.querySelector("td:nth-child(2)");
		expect(csvActionsCell).toBeInTheDocument();

		// The CSV actions cell should contain edit and IconButton elements
		const csvIconButtons = csvActionsCell?.querySelectorAll("button");
		expect(csvIconButtons).toHaveLength(2);

		expect(csvIconButtons?.[0]).not.toBeDisabled();
		expect(csvIconButtons?.[1]).toBeDisabled();
	});
});
