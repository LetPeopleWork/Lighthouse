import type { ILicenseStatus } from "../../models/ILicenseStatus";
import { BaseApiService } from "./BaseApiService";

export interface ILicensingService {
	getLicenseStatus(): Promise<ILicenseStatus>;
	importLicense(file: File): Promise<ILicenseStatus>;
}

export class LicensingService
	extends BaseApiService
	implements ILicensingService
{
	async getLicenseStatus(): Promise<ILicenseStatus> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.get<ILicenseStatus>("/license");

			// Convert date string to Date object if present
			if (response.data.expiryDate) {
				response.data.expiryDate = new Date(response.data.expiryDate);
			}

			return response.data;
		});
	}

	async importLicense(file: File): Promise<ILicenseStatus> {
		return await this.withErrorHandling(async () => {
			const formData = new FormData();
			formData.append("file", file);

			const response = await this.apiService.post<ILicenseStatus>(
				"/license/import",
				formData,
				{
					headers: {
						"Content-Type": "multipart/form-data",
					},
				},
			);

			// Convert date string to Date object if present
			if (response.data.expiryDate) {
				response.data.expiryDate = new Date(response.data.expiryDate);
			}

			return response.data;
		});
	}
}
