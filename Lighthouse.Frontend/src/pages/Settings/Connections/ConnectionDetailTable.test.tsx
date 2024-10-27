import { render, screen, fireEvent } from "@testing-library/react";
import ConnectionDetailTable from "./ConnectionDetailTable";
import { IWorkTrackingSystemConnection, WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

describe("ConnectionDetailTable", () => {
    const mockConnections: IWorkTrackingSystemConnection[] = [
        new WorkTrackingSystemConnection("Jira", "Jira", [], 1),
        new WorkTrackingSystemConnection("ADO", "AzureDevOps", [], 1),
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
            />
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
            />
        );

        const editButtons = screen.getAllByTestId("EditIcon");
        fireEvent.click(editButtons[0]);

        expect(mockOnEditConnectionButtonClicked).toHaveBeenCalledTimes(1);
        expect(mockOnEditConnectionButtonClicked).toHaveBeenCalledWith(mockConnections[0]);
    });

    it("should call handleDeleteConnection with the correct system when the delete button is clicked", () => {
        render(
            <ConnectionDetailTable
                workTrackingSystemConnections={mockConnections}
                onEditConnectionButtonClicked={mockOnEditConnectionButtonClicked}
                handleDeleteConnection={mockHandleDeleteConnection}
            />
        );

        const deleteButtons = screen.getAllByTestId("DeleteIcon");
        fireEvent.click(deleteButtons[0]);

        expect(mockHandleDeleteConnection).toHaveBeenCalledTimes(1);
        expect(mockHandleDeleteConnection).toHaveBeenCalledWith(mockConnections[0]);
    });
});
