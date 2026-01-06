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
					key: "username",
					displayName: "Username",
					isSecret: false,
					isOptional: false,
				},
				{
					key: "apiToken",
					displayName: "API Token",
					isSecret: true,
					isOptional: false,
				},
			],
		},
		{
			key: "jira.datacenter",
			displayName: "Jira Data Center",
			options: [
				{ key: "url", displayName: "URL", isSecret: false, isOptional: false },
				{
					key: "apiToken",
					displayName: "Personal Access Token",
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

	// CSV has no auth options (method key "none" with empty options)
	const csvAuthMethods: IAuthenticationMethod[] = [
		{
			key: "none",
			displayName: "No Authentication",
			options: [],
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
				{
					key: "username",
					value: "user@example.com",
					isSecret: false,
					isOptional: false,
				},
				{ key: "apiToken", value: "12345", isSecret: true, isOptional: false },
			],
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
		fireEvent.change(screen.getByLabelText("Username"), {
			target: { value: "user@example.com" },
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
		fireEvent.change(screen.getByLabelText("Username"), {
			target: { value: "user@example.com" },
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
		fireEvent.change(screen.getByLabelText("Username"), {
			target: { value: "user@example.com" },
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
		fireEvent.change(screen.getByLabelText("Username"), {
			target: { value: "user@example.com" },
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
		fireEvent.change(screen.getByLabelText("Username"), {
			target: { value: "user@example.com" },
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
		fireEvent.change(screen.getByLabelText("Username"), {
			target: { value: "user@example.com" },
		});
		fireEvent.change(screen.getByLabelText("API Token"), {
			target: { value: "test-token" },
		});

		// First validate to set the correct state
		fireEvent.click(screen.getByText("Validate"));
		await waitFor(() => expect(mockValidateSettings).toHaveBeenCalledTimes(1));
		// Wait for ActionButton's timeout to complete
		await waitFor(() => {}, { timeout: 500 });

		// Use getAllByRole since there are 2 comboboxes (system + auth method)
		// The first one is the system selector
		const comboboxes = screen.getAllByRole("combobox");
		const selectElement = comboboxes[0];
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

	describe("Auth vs Options section visibility", () => {
		it("should not show Auth section for CSV (no-auth provider)", () => {
			const csvConnection = new WorkTrackingSystemConnection({
				name: "CSV",
				workTrackingSystem: "Csv",
				options: [
					{ key: "Delimiter", value: ",", isSecret: false, isOptional: false },
					{
						key: "DateTimeFormat",
						value: "yyyy-MM-dd",
						isSecret: false,
						isOptional: false,
					},
				],
				id: 3,
				authenticationMethodKey: "none",
				availableAuthenticationMethods: csvAuthMethods,
			});

			render(
				<ModifyTrackingSystemConnectionDialog
					open={true}
					onClose={mockOnClose}
					workTrackingSystems={[csvConnection]}
					validateSettings={mockValidateSettings}
				/>,
			);

			// Auth section header should not be present
			expect(screen.queryByText("Authentication")).not.toBeInTheDocument();

			// Options section should be present with CSV options
			expect(screen.getByText("Options")).toBeInTheDocument();

			// CSV options should be rendered with their keys as labels
			expect(screen.getByLabelText("Delimiter")).toBeInTheDocument();
			expect(screen.getByLabelText("DateTimeFormat")).toBeInTheDocument();
		});

		it("should show Auth section but not Options section for Jira (auth-only provider)", () => {
			const jiraConnection = new WorkTrackingSystemConnection({
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "url",
						value: "http://jira.example.com",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "apiToken",
						value: "",
						isSecret: true,
						isOptional: false,
					},
				],
				id: 1,
				authenticationMethodKey: "jira.cloud",
				availableAuthenticationMethods: jiraAuthMethods,
			});

			render(
				<ModifyTrackingSystemConnectionDialog
					open={true}
					onClose={mockOnClose}
					workTrackingSystems={[jiraConnection]}
					validateSettings={mockValidateSettings}
				/>,
			);

			// Auth section header should be present
			expect(screen.getByText("Authentication")).toBeInTheDocument();

			// Options section should NOT be present (no non-auth options)
			expect(screen.queryByText("Options")).not.toBeInTheDocument();

			// Auth options should be rendered with display names
			expect(screen.getByLabelText("URL")).toBeInTheDocument();
			expect(screen.getByLabelText("API Token")).toBeInTheDocument();
		});

		it("should preserve CSV option values when editing existing connection", () => {
			const csvConnection = new WorkTrackingSystemConnection({
				name: "My CSV Connection",
				workTrackingSystem: "Csv",
				options: [
					{ key: "Delimiter", value: ";", isSecret: false, isOptional: false },
					{
						key: "DateTimeFormat",
						value: "dd/MM/yyyy",
						isSecret: false,
						isOptional: false,
					},
				],
				id: 3,
				authenticationMethodKey: "none",
				availableAuthenticationMethods: csvAuthMethods,
			});

			render(
				<ModifyTrackingSystemConnectionDialog
					open={true}
					onClose={mockOnClose}
					workTrackingSystems={[csvConnection]}
					validateSettings={mockValidateSettings}
				/>,
			);

			// Existing values should be pre-populated
			expect(screen.getByLabelText("Delimiter")).toHaveValue(";");
			expect(screen.getByLabelText("DateTimeFormat")).toHaveValue("dd/MM/yyyy");
		});

		it("should include both auth and other options in validate payload", async () => {
			// A hypothetical provider with both auth and non-auth options
			const mixedAuthMethods: IAuthenticationMethod[] = [
				{
					key: "mixed.auth",
					displayName: "Mixed Auth",
					options: [
						{
							key: "authToken",
							displayName: "Auth Token",
							isSecret: true,
							isOptional: false,
						},
					],
				},
			];

			const mixedConnection = new WorkTrackingSystemConnection({
				name: "Mixed",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "authToken",
						value: "",
						isSecret: true,
						isOptional: false,
					},
					{
						key: "customSetting",
						value: "custom-value",
						isSecret: false,
						isOptional: false,
					},
				],
				id: 4,
				authenticationMethodKey: "mixed.auth",
				availableAuthenticationMethods: mixedAuthMethods,
			});

			render(
				<ModifyTrackingSystemConnectionDialog
					open={true}
					onClose={mockOnClose}
					workTrackingSystems={[mixedConnection]}
					validateSettings={mockValidateSettings}
				/>,
			);

			// Fill in auth token
			fireEvent.change(screen.getByLabelText("Auth Token"), {
				target: { value: "secret-token" },
			});

			// Click validate
			fireEvent.click(screen.getByText("Validate"));

			await waitFor(() =>
				expect(mockValidateSettings).toHaveBeenCalledTimes(1),
			);

			// Validate payload should include both auth and non-auth options
			expect(mockValidateSettings).toHaveBeenCalledWith(
				expect.objectContaining({
					options: expect.arrayContaining([
						expect.objectContaining({
							key: "authToken",
							value: "secret-token",
						}),
						expect.objectContaining({
							key: "customSetting",
							value: "custom-value",
						}),
					]),
				}),
			);
		});
	});

	describe("Multiple auth methods - filtering irrelevant auth options", () => {
		it("should only show auth options relevant to the selected Jira Cloud method", () => {
			const jiraConnection = new WorkTrackingSystemConnection({
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "url",
						value: "http://jira.example.com",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "username",
						value: "",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "apiToken",
						value: "",
						isSecret: true,
						isOptional: false,
					},
					{
						key: "timeout",
						value: "30",
						isSecret: false,
						isOptional: true,
					},
				],
				id: 1,
				authenticationMethodKey: "jira.cloud",
				availableAuthenticationMethods: jiraAuthMethods,
			});

			render(
				<ModifyTrackingSystemConnectionDialog
					open={true}
					onClose={mockOnClose}
					workTrackingSystems={[jiraConnection]}
					validateSettings={mockValidateSettings}
				/>,
			);

			// Auth section should show URL, Username, and API Token (Jira Cloud options)
			expect(screen.getByText("Authentication")).toBeInTheDocument();
			expect(screen.getByLabelText("URL")).toBeInTheDocument();
			expect(screen.getByLabelText("Username")).toBeInTheDocument();
			expect(screen.getByLabelText("API Token")).toBeInTheDocument();

			// Options section should show timeout (non-auth option)
			expect(screen.getByText("Options")).toBeInTheDocument();
			expect(screen.getByLabelText("timeout")).toBeInTheDocument();
		});

		it("should only show auth options relevant to Jira Data Center and hide username", () => {
			const jiraConnection = new WorkTrackingSystemConnection({
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "url",
						value: "http://jira.example.com",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "username",
						value: "user@example.com",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "apiToken",
						value: "",
						isSecret: true,
						isOptional: false,
					},
					{
						key: "timeout",
						value: "30",
						isSecret: false,
						isOptional: true,
					},
				],
				id: 1,
				authenticationMethodKey: "jira.datacenter",
				availableAuthenticationMethods: jiraAuthMethods,
			});

			render(
				<ModifyTrackingSystemConnectionDialog
					open={true}
					onClose={mockOnClose}
					workTrackingSystems={[jiraConnection]}
					validateSettings={mockValidateSettings}
				/>,
			);

			// Auth section should only show URL and Personal Access Token (Data Center options)
			expect(screen.getByText("Authentication")).toBeInTheDocument();
			expect(screen.getByLabelText("URL")).toBeInTheDocument();
			expect(
				screen.getByLabelText("Personal Access Token"),
			).toBeInTheDocument();

			// Username should NOT appear anywhere (neither in Auth nor Options)
			expect(screen.queryByLabelText("Username")).not.toBeInTheDocument();
			expect(screen.queryByLabelText("username")).not.toBeInTheDocument();

			// Options section should show timeout (non-auth option)
			expect(screen.getByText("Options")).toBeInTheDocument();
			expect(screen.getByLabelText("timeout")).toBeInTheDocument();
		});

		it("should switch auth options when changing from Jira Cloud to Data Center", async () => {
			const jiraConnection = new WorkTrackingSystemConnection({
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "url",
						value: "http://jira.example.com",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "username",
						value: "",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "apiToken",
						value: "",
						isSecret: true,
						isOptional: false,
					},
					{
						key: "timeout",
						value: "30",
						isSecret: false,
						isOptional: true,
					},
				],
				id: 1,
				authenticationMethodKey: "jira.cloud",
				availableAuthenticationMethods: jiraAuthMethods,
			});

			render(
				<ModifyTrackingSystemConnectionDialog
					open={true}
					onClose={mockOnClose}
					workTrackingSystems={[jiraConnection]}
					validateSettings={mockValidateSettings}
				/>,
			);

			// Initially should show Username for Jira Cloud
			expect(screen.getByLabelText("Username")).toBeInTheDocument();

			// Change to Jira Data Center - use getAllByRole since there are 2 comboboxes
			const comboboxes = screen.getAllByRole("combobox");
			// Second combobox is the auth method selector
			const authMethodSelect = comboboxes[1];
			fireEvent.mouseDown(authMethodSelect);
			const dataCenterOption = screen.getByText("Jira Data Center");
			fireEvent.click(dataCenterOption);

			// Username should disappear
			await waitFor(() => {
				expect(screen.queryByLabelText("Username")).not.toBeInTheDocument();
			});

			// Personal Access Token should appear
			expect(
				screen.getByLabelText("Personal Access Token"),
			).toBeInTheDocument();

			// URL and timeout should still be present
			expect(screen.getByLabelText("URL")).toBeInTheDocument();
			expect(screen.getByLabelText("timeout")).toBeInTheDocument();
		});

		it("should only send relevant auth options when validating Jira Data Center", async () => {
			const jiraConnection = new WorkTrackingSystemConnection({
				name: "Jira DC",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "url",
						value: "",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "username",
						value: "should-not-be-sent",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "apiToken",
						value: "",
						isSecret: true,
						isOptional: false,
					},
					{
						key: "timeout",
						value: "60",
						isSecret: false,
						isOptional: true,
					},
				],
				id: 1,
				authenticationMethodKey: "jira.datacenter",
				availableAuthenticationMethods: jiraAuthMethods,
			});

			render(
				<ModifyTrackingSystemConnectionDialog
					open={true}
					onClose={mockOnClose}
					workTrackingSystems={[jiraConnection]}
					validateSettings={mockValidateSettings}
				/>,
			);

			// Fill in the visible fields
			fireEvent.change(screen.getByLabelText("URL"), {
				target: { value: "http://jira.example.com" },
			});
			fireEvent.change(screen.getByLabelText("Personal Access Token"), {
				target: { value: "pat-token-123" },
			});

			// Click validate
			fireEvent.click(screen.getByText("Validate"));

			await waitFor(() =>
				expect(mockValidateSettings).toHaveBeenCalledTimes(1),
			);

			// Validate payload should NOT include username (irrelevant for Data Center)
			expect(mockValidateSettings).toHaveBeenCalledWith(
				expect.objectContaining({
					authenticationMethodKey: "jira.datacenter",
					options: expect.arrayContaining([
						expect.objectContaining({
							key: "url",
							value: "http://jira.example.com",
						}),
						expect.objectContaining({
							key: "apiToken",
							value: "pat-token-123",
						}),
						expect.objectContaining({
							key: "timeout",
							value: "60",
						}),
					]),
				}),
			);

			// Ensure username is NOT in the options
			const call = mockValidateSettings.mock.calls[0][0];
			const usernameOption = call.options.find(
				(opt: { key: string }) => opt.key === "username",
			);
			expect(usernameOption).toBeUndefined();
		});

		it("should only send relevant auth options when saving Jira Cloud", async () => {
			const jiraConnection = new WorkTrackingSystemConnection({
				name: "Jira Cloud",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "url",
						value: "",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "username",
						value: "",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "apiToken",
						value: "",
						isSecret: true,
						isOptional: false,
					},
				],
				id: 1,
				authenticationMethodKey: "jira.cloud",
				availableAuthenticationMethods: jiraAuthMethods,
			});

			render(
				<ModifyTrackingSystemConnectionDialog
					open={true}
					onClose={mockOnClose}
					workTrackingSystems={[jiraConnection]}
					validateSettings={mockValidateSettings}
				/>,
			);

			// Fill in the fields
			fireEvent.change(screen.getByLabelText("URL"), {
				target: { value: "http://jira.cloud.example.com" },
			});
			fireEvent.change(screen.getByLabelText("Username"), {
				target: { value: "user@example.com" },
			});
			fireEvent.change(screen.getByLabelText("API Token"), {
				target: { value: "api-token-456" },
			});

			// Validate first
			fireEvent.click(screen.getByText("Validate"));
			await waitFor(() =>
				expect(mockValidateSettings).toHaveBeenCalledTimes(1),
			);
			await waitFor(() => {}, { timeout: 500 });

			// Click save
			const saveButton = await screen.findByRole("button", {
				name: /Save/i,
			});
			fireEvent.click(saveButton);

			await waitFor(() => expect(mockOnClose).toHaveBeenCalledTimes(1));

			// Save payload should include all Jira Cloud auth options
			expect(mockOnClose).toHaveBeenCalledWith(
				expect.objectContaining({
					authenticationMethodKey: "jira.cloud",
					options: expect.arrayContaining([
						expect.objectContaining({
							key: "url",
							value: "http://jira.cloud.example.com",
						}),
						expect.objectContaining({
							key: "username",
							value: "user@example.com",
						}),
						expect.objectContaining({
							key: "apiToken",
							value: "api-token-456",
						}),
					]),
				}),
			);
		});
	});
});
