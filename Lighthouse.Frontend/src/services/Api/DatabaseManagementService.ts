import type {
	IDatabaseCapabilityStatus,
	IDatabaseOperationStatus,
} from "../../models/DatabaseManagement/DatabaseManagementTypes";
import { BaseApiService } from "./BaseApiService";

export interface IDatabaseManagementService {
	getStatus(): Promise<IDatabaseCapabilityStatus>;
	createBackup(password: string): Promise<IDatabaseOperationStatus>;
	downloadBackupArtifact(operationId: string): Promise<Blob>;
	restoreBackup(
		file: File,
		password: string,
	): Promise<IDatabaseOperationStatus>;
	clearDatabase(): Promise<IDatabaseOperationStatus>;
	getOperationStatus(
		operationId: string,
	): Promise<IDatabaseOperationStatus | null>;
}

export class DatabaseManagementService
	extends BaseApiService
	implements IDatabaseManagementService
{
	public getStatus(): Promise<IDatabaseCapabilityStatus> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IDatabaseCapabilityStatus>(
				"/database-management/status",
			);
			return response.data;
		});
	}

	public createBackup(password: string): Promise<IDatabaseOperationStatus> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IDatabaseOperationStatus>(
				"/database-management/backup",
				{ password },
			);
			return response.data;
		});
	}

	public downloadBackupArtifact(operationId: string): Promise<Blob> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get(
				`/database-management/backup/${operationId}`,
				{ responseType: "blob" },
			);
			return response.data as Blob;
		});
	}

	public restoreBackup(
		file: File,
		password: string,
	): Promise<IDatabaseOperationStatus> {
		return this.withErrorHandling(async () => {
			const formData = new FormData();
			formData.append("file", file);
			formData.append("password", password);

			const response = await this.apiService.post<IDatabaseOperationStatus>(
				"/database-management/restore",
				formData,
				{
					headers: { "Content-Type": "multipart/form-data" },
				},
			);
			return response.data;
		});
	}

	public clearDatabase(): Promise<IDatabaseOperationStatus> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IDatabaseOperationStatus>(
				"/database-management/clear",
			);
			return response.data;
		});
	}

	public getOperationStatus(
		operationId: string,
	): Promise<IDatabaseOperationStatus | null> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IDatabaseOperationStatus>(
				`/database-management/operations/${operationId}`,
			);
			return response.data;
		});
	}
}
