import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IDatabaseCapabilityStatus,
	IDatabaseOperationStatus,
} from "../../../models/DatabaseManagement/DatabaseManagementTypes";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import DatabaseManagementSettings from "./DatabaseManagementSettings";

describe("DatabaseManagementSettings", () => {
	const mockDatabaseManagementService = {
		getStatus: vi.fn(),
		createBackup: vi.fn(),
		downloadBackupArtifact: vi.fn(),
		restoreBackup: vi.fn(),
		clearDatabase: vi.fn(),
		getOperationStatus: vi.fn(),
	};

	const mockApiContext = createMockApiServiceContext({
		databaseManagementService: mockDatabaseManagementService,
	});

	const defaultStatus: IDatabaseCapabilityStatus = {
		provider: "sqlite",
		isOperationBlocked: false,
		blockedReason: null,
		isToolingAvailable: true,
		toolingGuidanceMessage: null,
		toolingGuidanceUrl: null,
		activeOperation: null,
	};

	beforeEach(() => {
		vi.clearAllMocks();
		// A restore answer queued by a test that stopped early must not reach the next test.
		mockDatabaseManagementService.restoreBackup.mockReset();
		mockDatabaseManagementService.getStatus.mockResolvedValue(defaultStatus);
	});

	const renderComponent = () =>
		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<DatabaseManagementSettings />
			</ApiServiceContext.Provider>,
		);

	it("renders Database Management heading", async () => {
		renderComponent();

		await waitFor(() => {
			expect(screen.getByText("Database Management")).toBeInTheDocument();
		});
	});

	it("renders provider info from capability status", async () => {
		renderComponent();

		await waitFor(() => {
			expect(screen.getByText(/sqlite/i)).toBeInTheDocument();
		});
	});

	it("says in Backup and Restore that a backup does not carry the encryption key and is checked before restoring", async () => {
		renderComponent();

		await waitFor(() => {
			expect(screen.getByText("Backup")).toBeInTheDocument();
		});
		expect(screen.getByText("Restore")).toBeInTheDocument();
		expect(
			screen.getByText(
				"The backup does not contain this installation's encryption key. Its stored credentials can only be read by an installation that uses the same key.",
			),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				"Before it changes anything, Lighthouse checks that this installation's encryption key can read the backup's stored credentials.",
			),
		).toBeInTheDocument();
	});

	const reasonTheRestoreStopped =
		"None of the stored credentials in this backup can be read with this installation's encryption key. Nothing has been changed.";

	const restoreThatFailed = (failureCode: string | null) => ({
		operationId: "op-restore",
		operationType: "Restore",
		state: "Failed",
		failureReason: reasonTheRestoreStopped,
		failureCode,
	});

	const restoreTheBackup = async (user: ReturnType<typeof userEvent.setup>) => {
		await waitFor(() => {
			expect(screen.getByTestId("restore-button")).toBeInTheDocument();
		});
		await user.upload(
			screen.getByTestId("restore-file-input"),
			new File(["backup"], "lighthouse-backup.zip", {
				type: "application/zip",
			}),
		);
		await user.type(screen.getByTestId("restore-password"), "backupPw1");
		await user.click(screen.getByTestId("restore-button"));
	};

	it("explains that the backup's credentials cannot be read, and Cancel closes the explanation keeping the chosen file and password", async () => {
		const user = userEvent.setup();
		mockDatabaseManagementService.restoreBackup.mockResolvedValue(
			restoreThatFailed("CredentialsUnreadable"),
		);
		renderComponent();

		await restoreTheBackup(user);

		const explanation = await screen.findByRole("dialog", {
			name: "Stored credentials in this backup cannot be read",
		});
		expect(explanation).toHaveTextContent(reasonTheRestoreStopped);
		expect(explanation).toHaveTextContent(
			"If you restore anyway, Lighthouse removes these credentials so it can start. You must then enter each token again in its Connection.",
		);

		await user.click(screen.getByRole("button", { name: "Cancel" }));

		await waitFor(() => {
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		});
		expect(screen.getByText("lighthouse-backup.zip")).toBeInTheDocument();
		expect(screen.getByTestId("restore-password")).toHaveValue("backupPw1");
	});

	it("shows a failed restore without the unreadable-credentials code as an error, not as the credentials explanation", async () => {
		const user = userEvent.setup();
		mockDatabaseManagementService.restoreBackup.mockResolvedValue(
			restoreThatFailed(null),
		);
		renderComponent();

		await restoreTheBackup(user);

		await waitFor(() => {
			expect(screen.getByText(reasonTheRestoreStopped)).toBeInTheDocument();
		});
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it("restores without credentials from the explanation, showing it is busy, then closes it and reports success", async () => {
		const user = userEvent.setup();
		let finishTheRestore: (status: IDatabaseOperationStatus) => void = () => {};
		mockDatabaseManagementService.restoreBackup
			.mockResolvedValueOnce(restoreThatFailed("CredentialsUnreadable"))
			.mockReturnValueOnce(
				new Promise<IDatabaseOperationStatus>((resolve) => {
					finishTheRestore = resolve;
				}),
			);
		renderComponent();
		await restoreTheBackup(user);
		const explanation = await screen.findByRole("dialog", {
			name: "Stored credentials in this backup cannot be read",
		});

		await user.click(
			within(explanation).getByRole("button", {
				name: "Restore without credentials",
			}),
		);

		const [chosenFile] =
			mockDatabaseManagementService.restoreBackup.mock.calls[0];
		expect(mockDatabaseManagementService.restoreBackup).toHaveBeenNthCalledWith(
			2,
			chosenFile,
			"backupPw1",
			true,
		);
		expect(
			within(explanation).getByRole("button", {
				name: /Restore without credentials/,
			}),
		).toBeDisabled();

		finishTheRestore({
			operationId: "op-restore",
			operationType: "Restore",
			state: "Completed",
			failureReason: null,
		});

		await waitFor(() => {
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		});
		expect(
			screen.getByText(
				"Restore completed successfully. Your data has been restored.",
			),
		).toBeInTheDocument();
	});

	it("shows a failed restore without credentials as an error", async () => {
		const user = userEvent.setup();
		mockDatabaseManagementService.restoreBackup
			.mockResolvedValueOnce(restoreThatFailed("CredentialsUnreadable"))
			.mockRejectedValueOnce(new Error("The backup could not be restored."));
		renderComponent();
		await restoreTheBackup(user);
		const explanation = await screen.findByRole("dialog", {
			name: "Stored credentials in this backup cannot be read",
		});

		await user.click(
			within(explanation).getByRole("button", {
				name: "Restore without credentials",
			}),
		);

		await waitFor(() => {
			expect(
				screen.getByText("The backup could not be restored."),
			).toBeInTheDocument();
		});
	});

	it("renders clear section", async () => {
		renderComponent();

		await waitFor(() => {
			expect(screen.getByTestId("clear-button")).toBeInTheDocument();
		});
	});

	it("shows blocked alert when operations are blocked", async () => {
		mockDatabaseManagementService.getStatus.mockResolvedValue({
			...defaultStatus,
			isOperationBlocked: true,
			blockedReason: "A background update is currently in progress.",
		});

		renderComponent();

		await waitFor(() => {
			expect(
				screen.getByText(/background update is currently in progress/i),
			).toBeInTheDocument();
		});
	});

	it("shows tooling warning when tooling is not available", async () => {
		mockDatabaseManagementService.getStatus.mockResolvedValue({
			...defaultStatus,
			provider: "postgresql",
			isToolingAvailable: false,
			toolingGuidanceMessage:
				"pg_dump and pg_restore are required on the Lighthouse server.",
			toolingGuidanceUrl: "https://www.postgresql.org/download/",
		});

		renderComponent();

		await waitFor(() => {
			expect(
				screen.getByText(
					/pg_dump and pg_restore are required on the Lighthouse server/i,
				),
			).toBeInTheDocument();
		});
	});

	it("disables backup button when operations are blocked", async () => {
		mockDatabaseManagementService.getStatus.mockResolvedValue({
			...defaultStatus,
			isOperationBlocked: true,
			blockedReason: "Blocked",
		});

		renderComponent();

		await waitFor(() => {
			expect(screen.getByTestId("backup-button")).toBeDisabled();
		});
	});

	it("triggers backup when password is provided and button clicked", async () => {
		const user = userEvent.setup();
		const backupResult: IDatabaseOperationStatus = {
			operationId: "op-1",
			operationType: "Backup",
			state: "Completed",
			failureReason: null,
		};
		mockDatabaseManagementService.createBackup.mockResolvedValue(backupResult);
		mockDatabaseManagementService.downloadBackupArtifact.mockResolvedValue(
			new Blob(["data"]),
		);

		renderComponent();

		await waitFor(() => {
			expect(screen.getByTestId("backup-password")).toBeInTheDocument();
		});

		await user.type(screen.getByTestId("backup-password"), "testPw123");
		await user.click(screen.getByTestId("backup-button"));

		await waitFor(() => {
			expect(mockDatabaseManagementService.createBackup).toHaveBeenCalledWith(
				"testPw123",
			);
		});
	});

	it("shows error when backup fails", async () => {
		const user = userEvent.setup();
		const failedOp: IDatabaseOperationStatus = {
			operationId: "op-1",
			operationType: "Backup",
			state: "Failed",
			failureReason: "Disk full",
		};
		mockDatabaseManagementService.createBackup.mockResolvedValue(failedOp);

		renderComponent();

		await waitFor(() => {
			expect(screen.getByTestId("backup-password")).toBeInTheDocument();
		});

		await user.type(screen.getByTestId("backup-password"), "testPw123");
		await user.click(screen.getByTestId("backup-button"));

		await waitFor(() => {
			expect(screen.getByText(/Disk full/i)).toBeInTheDocument();
		});
	});

	it("shows confirmation dialog before clearing database", async () => {
		const user = userEvent.setup();

		renderComponent();

		await waitFor(() => {
			expect(screen.getByTestId("clear-button")).toBeInTheDocument();
		});

		await user.click(screen.getByTestId("clear-button"));

		await waitFor(() => {
			expect(screen.getByText(/are you sure/i)).toBeInTheDocument();
		});
	});

	it("calls clearDatabase when confirmation is accepted", async () => {
		const user = userEvent.setup();
		const clearResult: IDatabaseOperationStatus = {
			operationId: "op-3",
			operationType: "Clear",
			state: "RestartPending",
			failureReason: null,
		};
		mockDatabaseManagementService.clearDatabase.mockResolvedValue(clearResult);

		renderComponent();

		await waitFor(() => {
			expect(screen.getByTestId("clear-button")).toBeInTheDocument();
		});

		await user.click(screen.getByTestId("clear-button"));

		await waitFor(() => {
			expect(screen.getByTestId("confirm-clear-button")).toBeInTheDocument();
		});

		await user.click(screen.getByTestId("confirm-clear-button"));

		await waitFor(() => {
			expect(mockDatabaseManagementService.clearDatabase).toHaveBeenCalled();
		});
	});

	it("does not call clearDatabase when confirmation is cancelled", async () => {
		const user = userEvent.setup();

		renderComponent();

		await waitFor(() => {
			expect(screen.getByTestId("clear-button")).toBeInTheDocument();
		});

		await user.click(screen.getByTestId("clear-button"));

		await waitFor(() => {
			expect(screen.getByTestId("cancel-clear-button")).toBeInTheDocument();
		});

		await user.click(screen.getByTestId("cancel-clear-button"));

		expect(mockDatabaseManagementService.clearDatabase).not.toHaveBeenCalled();
	});

	it("disables clear button when operations are blocked", async () => {
		mockDatabaseManagementService.getStatus.mockResolvedValue({
			...defaultStatus,
			isOperationBlocked: true,
			blockedReason: "Blocked",
		});

		renderComponent();

		await waitFor(() => {
			expect(screen.getByTestId("clear-button")).toBeDisabled();
		});
	});

	it("disables restore button when operations are blocked", async () => {
		mockDatabaseManagementService.getStatus.mockResolvedValue({
			...defaultStatus,
			isOperationBlocked: true,
			blockedReason: "Blocked",
		});

		renderComponent();

		await waitFor(() => {
			expect(screen.getByTestId("restore-button")).toBeDisabled();
		});
	});

	it("shows active operation status", async () => {
		const activeOp: IDatabaseOperationStatus = {
			operationId: "op-active",
			operationType: "Backup",
			state: "Executing",
			failureReason: null,
		};
		mockDatabaseManagementService.getStatus.mockResolvedValue({
			...defaultStatus,
			isOperationBlocked: true,
			blockedReason: "A database Backup operation is currently active.",
			activeOperation: activeOp,
		});

		renderComponent();

		await waitFor(() => {
			expect(screen.getByText(/Executing/i)).toBeInTheDocument();
		});
	});
});
