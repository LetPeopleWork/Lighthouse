import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IDatabaseCapabilityStatus,
	IDatabaseOperationStatus,
} from "../../models/DatabaseManagement/DatabaseManagementTypes";
import { DatabaseManagementService } from "./DatabaseManagementService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("DatabaseManagementService", () => {
	let service: DatabaseManagementService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		service = new DatabaseManagementService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get database capability status", async () => {
		const mockStatus: IDatabaseCapabilityStatus = {
			provider: "sqlite",
			isOperationBlocked: false,
			blockedReason: null,
			isToolingAvailable: true,
			toolingGuidanceMessage: null,
			toolingGuidanceUrl: null,
			activeOperation: null,
		};
		mockedAxios.get.mockResolvedValueOnce({ data: mockStatus });

		const result = await service.getStatus();

		expect(result).toEqual(mockStatus);
		expect(mockedAxios.get).toHaveBeenCalledWith("/database-management/status");
	});

	it("should create backup with password", async () => {
		const mockOp: IDatabaseOperationStatus = {
			operationId: "op-1",
			operationType: "Backup",
			state: "Executing",
			failureReason: null,
		};
		mockedAxios.post.mockResolvedValueOnce({ data: mockOp });

		const result = await service.createBackup("myPassword");

		expect(result).toEqual(mockOp);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/database-management/backup",
			{ password: "myPassword" },
		);
	});

	it("should download backup artifact", async () => {
		const mockBlob = new Blob(["test"], { type: "application/zip" });
		mockedAxios.get.mockResolvedValueOnce({ data: mockBlob });

		const result = await service.downloadBackupArtifact("op-1");

		expect(result).toBe(mockBlob);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/database-management/backup/op-1",
			{ responseType: "blob" },
		);
	});

	it("should restore backup with file and password", async () => {
		const mockOp: IDatabaseOperationStatus = {
			operationId: "op-2",
			operationType: "Restore",
			state: "Executing",
			failureReason: null,
		};
		mockedAxios.post.mockResolvedValueOnce({ data: mockOp });

		const file = new File(["backup-data"], "backup.zip", {
			type: "application/zip",
		});

		const result = await service.restoreBackup(file, "restorePw");

		expect(result).toEqual(mockOp);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/database-management/restore",
			expect.any(FormData),
			{ headers: { "Content-Type": "multipart/form-data" } },
		);
	});

	it("should clear database", async () => {
		const mockOp: IDatabaseOperationStatus = {
			operationId: "op-3",
			operationType: "Clear",
			state: "Executing",
			failureReason: null,
		};
		mockedAxios.post.mockResolvedValueOnce({ data: mockOp });

		const result = await service.clearDatabase();

		expect(result).toEqual(mockOp);
		expect(mockedAxios.post).toHaveBeenCalledWith("/database-management/clear");
	});

	it("should get operation status", async () => {
		const mockOp: IDatabaseOperationStatus = {
			operationId: "op-1",
			operationType: "Backup",
			state: "Completed",
			failureReason: null,
		};
		mockedAxios.get.mockResolvedValueOnce({ data: mockOp });

		const result = await service.getOperationStatus("op-1");

		expect(result).toEqual(mockOp);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/database-management/operations/op-1",
		);
	});
});
