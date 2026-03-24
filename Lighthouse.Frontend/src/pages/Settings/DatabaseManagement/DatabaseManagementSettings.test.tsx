import { render, screen, waitFor } from "@testing-library/react";
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

	it("renders backup section", async () => {
		renderComponent();

		await waitFor(() => {
			expect(screen.getByText("Backup")).toBeInTheDocument();
		});
	});

	it("renders restore section", async () => {
		renderComponent();

		await waitFor(() => {
			expect(screen.getByText("Restore")).toBeInTheDocument();
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
