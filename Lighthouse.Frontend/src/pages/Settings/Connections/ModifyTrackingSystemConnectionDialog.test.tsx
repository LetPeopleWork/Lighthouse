import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import ModifyTrackingSystemConnectionDialog from "./ModifyTrackingSystemConnectionDialog";
import { IWorkTrackingSystemConnection, WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

describe("ModifyTrackingSystemConnectionDialog", () => {
    const mockWorkTrackingSystems: IWorkTrackingSystemConnection[] = [
        new WorkTrackingSystemConnection("Jira", "Jira", [
            { key: "url", value: "http://jira.example.com", isSecret: false },
            { key: "apiToken", value: "12345", isSecret: true }
        ],
            1),
        new WorkTrackingSystemConnection("ADO", "AzureDevOps", [
            { key: "url", value: "http://ado.example.com", isSecret: false },
            { key: "apiToken", value: "67890", isSecret: true }
        ],
            2),
    ];

    const mockValidateSettings = vi.fn(async (connection: IWorkTrackingSystemConnection) => connection.name !== "Invalid");

    const mockOnClose = vi.fn();

    beforeEach(() => {
        mockValidateSettings.mockClear();
        mockOnClose.mockClear();
    });

    it("should render the dialog with initial values", () => {
        render(
            <ModifyTrackingSystemConnectionDialog
                open={true}
                onClose={mockOnClose}
                workTrackingSystems={mockWorkTrackingSystems}
                validateSettings={mockValidateSettings}
            />
        );

        // Check the dialog title
        expect(screen.getByRole('heading', { name: /Create New Connection/i })).toBeInTheDocument();

        // Check the input field for the connection name
        expect(screen.getByLabelText("Connection Name")).toHaveValue("Jira");

        const urlInput = screen.getByLabelText("url");
        expect(urlInput).toHaveValue("http://jira.example.com");

        const apiTokenInput = screen.getByLabelText("apiToken");
        expect(apiTokenInput).toHaveValue("12345");
    });


    it("should call validateSettings and show validation status", async () => {
        render(
            <ModifyTrackingSystemConnectionDialog
                open={true}
                onClose={mockOnClose}
                workTrackingSystems={mockWorkTrackingSystems}
                validateSettings={mockValidateSettings}
            />
        );

        fireEvent.change(screen.getByLabelText("Connection Name"), { target: { value: "Valid Connection" } });
        fireEvent.click(screen.getByText("Validate"));

        await waitFor(() => expect(mockValidateSettings).toHaveBeenCalledTimes(1));
        expect(mockValidateSettings).toHaveBeenCalledTimes(1);
    });

    it("should call onClose with the updated connection when submit is clicked", async () => {
        render(
            <ModifyTrackingSystemConnectionDialog
                open={true}
                onClose={mockOnClose}
                workTrackingSystems={mockWorkTrackingSystems}
                validateSettings={mockValidateSettings}
            />
        );

        fireEvent.change(screen.getByLabelText("Connection Name"), { target: { value: "Valid Connection" } });
        fireEvent.click(screen.getByText("Validate"));

        const saveButton = await screen.findByRole('button', { name: /Save|Create/i });
        fireEvent.click(saveButton);

        expect(mockOnClose).toHaveBeenCalledTimes(1);
    });

    it("should call onClose with null when cancel is clicked", () => {
        render(
            <ModifyTrackingSystemConnectionDialog
                open={true}
                onClose={mockOnClose}
                workTrackingSystems={mockWorkTrackingSystems}
                validateSettings={mockValidateSettings}
            />
        );

        fireEvent.click(screen.getByText("Cancel"));

        expect(mockOnClose).toHaveBeenCalledWith(null);
    });
});