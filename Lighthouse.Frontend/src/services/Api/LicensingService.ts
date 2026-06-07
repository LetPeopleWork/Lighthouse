import {
	type ILicenseStatus,
	LicenseStatusSchema,
} from "../../models/ILicenseStatus";
import { BaseApiService } from "./BaseApiService";

export interface ILicensingService {
	getLicenseStatus(): Promise<ILicenseStatus>;
	importLicense(file: File): Promise<ILicenseStatus>;
	clearLicense(): Promise<void>;
}

export class LicensingService
	extends BaseApiService
	implements ILicensingService
{
	async getLicenseStatus(): Promise<ILicenseStatus> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>("/license");
			return BaseApiService.parse(LicenseStatusSchema, response.data);
		});
	}

	async importLicense(file: File): Promise<ILicenseStatus> {
		return await this.withErrorHandling(async () => {
			const formData = new FormData();
			formData.append("file", file);

			const response = await this.apiService.post<unknown>(
				"/license/import",
				formData,
				{
					headers: {
						"Content-Type": "multipart/form-data",
					},
				},
			);

			return BaseApiService.parse(LicenseStatusSchema, response.data);
		});
	}

	async clearLicense(): Promise<void> {
		return await this.withErrorHandling(async () => {
			await this.apiService.delete("/license");
		});
	}
}
