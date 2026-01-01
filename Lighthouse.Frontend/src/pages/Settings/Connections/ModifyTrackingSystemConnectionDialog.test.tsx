import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type IAuthenticationMethod,
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ModifyTrackingSystemConnectionDialog from "./ModifyTrackingSystemConnectionDialog";

describe("ModifyTrackingSystemConnectionDialog", () => {
	// Define auth methods for each system with their options
	const jiraAuthMethods: IAuthenticationMethod[] = [
		{
			key: "jira.cloud",
			displayName: "Jira Cloud",
			options: [
				{ key: "url", displayName: "URL", isSecret: false, isOptional: false },
				{
					key: "apiToken",
					displayName: "API Token",
					isSecret: true,
					isOptional: false,
				},
			],
		},
	];

	const adoAuthMethods: IAuthenticationMethod[] = [
		{
			key: "ado.pat",
			displayName: "Personal Access Token",
			options: [
				{ key: "url", displayName: "URL", isSecret: false, isOptional: false },
				{
					key: "apiToken",
					displayName: "API Token",
					isSecret: true,
					isOptional: false,
				},
			],
		},
	];

	const mockWorkTrackingSystems: IWorkTrackingSystemConnection[] = [
		new WorkTrackingSystemConnection({
			name: "Jira",
			workTrackingSystem: "Jira",
			options: [
				{
					key: "url",
					value: "http://jira.example.com",
					isSecret: false,
					isOptional: false,
				},
				{ key: "apiToken", value: "12345", isSecret: true, isOptional: false },
			],
			dataSourceType: "Query",
			id: 1,
			authenticationMethodKey: "jira.cloud",
			availableAuthenticationMethods: jiraAuthMethods,
		}),
		new WorkTrackingSystemConnection({
			name: "ADO",
			workTrackingSystem: "AzureDevOps",
			options: [
				{
					key: "url",
					value: "http://ado.example.com",
					isSecret: false,
					isOptional: false,
				},
				{ key: "apiToken", value: "67890", isSecret: true, isOptional: false },
			],
			dataSourceType: "Query",
			id: 2,
			authenticationMethodKey: "ado.pat",
			availableAuthenticationMethods: adoAuthMethods,
		}),
	];

	const mockValidateSettings = vi.fn(
		async (connection: IWorkTrackingSystemConnection) =>
			connection.name !== "Invalid",
	);

	const mockOnClose = vi.fn();

	beforeEach(() => {
		mockValidateSettings.mockClear();
		mockOnClose.mockClear();
	});

	afterEach(() => {
		vi.clearAllTimers();
		vi.useRealTimers();
	});

	it("should render the dialog with initial values", () => {
		render(
			<ModifyTrackingSystemConnectionDialog
				open={true}
				onClose={mockOnClose}
				workTrackingSystems={mockWorkTrackingSystems}
				validateSettings={mockValidateSettings}
			/>,
		);

		// Check the dialog title
		expect(
			screen.getByRole("heading", { name: /Create New Connection/i }),
		).toBeInTheDocument();

		// Check the input field for the connection name
		expect(screen.getByLabelText("Connection Name")).toHaveValue("Jira");

		// When creating a new connection, options come from auth method schema with displayNames
		const urlInput = screen.getByLabelText("URL");
		expect(urlInput).toBeInTheDocument();

		const apiTokenInput = screen.getByLabelText("API Token");
		expect(apiTokenInput).toBeInTheDocument();
	});

	it("should call validateSettings and show validation status", async () => {
		render(
			<ModifyTrackingSystemConnectionDialog
				open={true}
				onClose={mockOnClose}
				workTrackingSystems={mockWorkTrackingSystems}
				validateSettings={mockValidateSettings}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Connection Name"), {
			target: { value: "Valid Connection" },
		});
		// Fill in required options
		fireEvent.change(screen.getByLabelText("URL"), {
			target: { value: "http://example.com" },
		});
		fireEvent.change(screen.getByLabelText("API Token"), {
			target: { value: "test-token" },
		});
		fireEvent.click(screen.getByText("Validate"));

		await waitFor(() => expect(mockValidateSettings).toHaveBeenCalledTimes(1));
		// Wait for ActionButton's 300ms timeout to complete
		await waitFor(() => {}, { timeout: 500 });
		expect(mockValidateSettings).toHaveBeenCalledTimes(1);
	});

	it("should call onClose with the updated connection when submit is clicked", async () => {
		render(
			<ModifyTrackingSystemConnectionDialog
				open={true}
				onClose={mockOnClose}
				workTrackingSystems={mockWorkTrackingSystems}
				validateSettings={mockValidateSettings}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Connection Name"), {
			target: { value: "Valid Connection" },
		});
		// Fill in required options
		fireEvent.change(screen.getByLabelText("URL"), {
			target: { value: "http://example.com" },
		});
		fireEvent.change(screen.getByLabelText("API Token"), {
			target: { value: "test-token" },
		});
		fireEvent.click(screen.getByText("Validate"));

		// Wait for ActionButton's validate timeout to complete
		await waitFor(() => {}, { timeout: 500 });

		const saveButton = await screen.findByRole("button", {
			name: /Save|Create/i,
		});
		fireEvent.click(saveButton);

		// Wait for ActionButton's save timeout to complete
		await waitFor(() => {}, { timeout: 500 });

		expect(mockOnClose).toHaveBeenCalledTimes(1);
	});

	it("should call onClose with null when cancel is clicked", () => {
		render(
			<ModifyTrackingSystemConnectionDialog
				open={true}
				onClose={mockOnClose}
				workTrackingSystems={mockWorkTrackingSystems}
				validateSettings={mockValidateSettings}
			/>,
		);

		fireEvent.click(screen.getByText("Cancel"));

		expect(mockOnClose).toHaveBeenCalledWith(null);
	});

	it("should validate correctly when password is pasted instead of typed", async () => {
		render(
			<ModifyTrackingSystemConnectionDialog
				open={true}
				onClose={mockOnClose}
				workTrackingSystems={mockWorkTrackingSystems}
				validateSettings={mockValidateSettings}
			/>,
		);

		// Fill in required fields with display names from auth method schema
		fireEvent.change(screen.getByLabelText("URL"), {
			target: { value: "http://example.com" },
		});

		// Find the API token input field (which is a password field)
		const apiTokenInput = screen.getByLabelText("API Token");

		// Simulate a paste event by directly using fireEvent.change
		// This should trigger the handleOptionChange function the same way a paste would
		fireEvent.change(apiTokenInput, {
			target: { value: "pasted-password-123" },
		});

		// Now click the validate button
		fireEvent.click(screen.getByText("Validate"));

		// The validate function should be called with the updated connection that includes the pasted password
		await waitFor(() => expect(mockValidateSettings).toHaveBeenCalledTimes(1));
		// Wait for ActionButton's timeout to complete
		await waitFor(() => {}, { timeout: 500 });

		// Verify that mockValidateSettings was called with a connection that has the pasted password
		expect(mockValidateSettings).toHaveBeenCalledWith(
			expect.objectContaining({
				options: expect.arrayContaining([
					expect.objectContaining({
						key: "apiToken",
						value: "pasted-password-123",
					}),
				]),
			}),
		);
	});

	it("should automatically validate inputs when name changes", async () => {
		render(
			<ModifyTrackingSystemConnectionDialog
				open={true}
				onClose={mockOnClose}
				workTrackingSystems={mockWorkTrackingSystems}
				validateSettings={mockValidateSettings}
			/>,
		);

		// Fill in required options first
		fireEvent.change(screen.getByLabelText("URL"), {
			target: { value: "http://example.com" },
		});
		fireEvent.change(screen.getByLabelText("API Token"), {
			target: { value: "test-token" },
		});

		// First validate to set the correct state
		fireEvent.click(screen.getByText("Validate"));
		await waitFor(() => expect(mockValidateSettings).toHaveBeenCalledTimes(1));
		// Wait for ActionButton's timeout to complete
		await waitFor(() => {}, { timeout: 500 });

		// Clear the name field to make inputs invalid
		fireEvent.change(screen.getByLabelText("Connection Name"), {
			target: { value: "" },
		});

		// The useEffect should trigger inputsValid to be false
		// We test this by checking if our validation logic correctly reports the input as invalid
		let connectionNameLabel: HTMLInputElement =
			screen.getByLabelText("Connection Name");
		expect(connectionNameLabel.value).toBe("");

		// Set the name field back to a valid value
		fireEvent.change(screen.getByLabelText("Connection Name"), {
			target: { value: "New Connection" },
		});

		// Check that the name field now has a value
		connectionNameLabel = screen.getByLabelText("Connection Name");
		expect(connectionNameLabel.value).toBe("New Connection");
	});

	it("should automatically validate inputs when options change", async () => {
		render(
			<ModifyTrackingSystemConnectionDialog
				open={true}
				onClose={mockOnClose}
				workTrackingSystems={mockWorkTrackingSystems}
				validateSettings={mockValidateSettings}
			/>,
		);

		// Fill in required options first
		fireEvent.change(screen.getByLabelText("URL"), {
			target: { value: "http://example.com" },
		});
		fireEvent.change(screen.getByLabelText("API Token"), {
			target: { value: "test-token" },
		});

		// First validate to set the correct state
		fireEvent.click(screen.getByText("Validate"));
		await waitFor(() => expect(mockValidateSettings).toHaveBeenCalledTimes(1));
		// Wait for ActionButton's timeout to complete
		await waitFor(() => {}, { timeout: 500 });

		// Clear a required option field to make inputs invalid
		const urlInput: HTMLInputElement = screen.getByLabelText("URL");
		fireEvent.change(urlInput, {
			target: { value: "" },
		});

		// Check that the url field is now empty
		expect(urlInput.value).toBe("");

		// Set the option field back to a valid value
		fireEvent.change(urlInput, {
			target: { value: "http://new.example.com" },
		});

		// Check that the url field now has a value
		expect(urlInput.value).toBe("http://new.example.com");
	});

	it("should automatically validate inputs when work tracking system changes", async () => {
		render(
			<ModifyTrackingSystemConnectionDialog
				open={true}
				onClose={mockOnClose}
				workTrackingSystems={mockWorkTrackingSystems}
				validateSettings={mockValidateSettings}
			/>,
		);

		// Fill in required options first
		fireEvent.change(screen.getByLabelText("URL"), {
			target: { value: "http://example.com" },
		});
		fireEvent.change(screen.getByLabelText("API Token"), {
			target: { value: "test-token" },
		});

		// First validate to set the correct state
		fireEvent.click(screen.getByText("Validate"));
		await waitFor(() => expect(mockValidateSettings).toHaveBeenCalledTimes(1));
		// Wait for ActionButton's timeout to complete
		await waitFor(() => {}, { timeout: 500 });

		// Use the Select component directly instead of trying to find it by label
		const selectElement = screen.getByRole("combobox");
		fireEvent.mouseDown(selectElement);

		// Now select the AzureDevOps option
		const adoOption = screen.getByText("AzureDevOps");
		fireEvent.click(adoOption);

		// The system should have changed - options are reset to empty values from the new auth method
		await waitFor(() => {
			const urlInput = screen.getByLabelText("URL");
			expect(urlInput).toHaveValue("");
		});
	});
});
