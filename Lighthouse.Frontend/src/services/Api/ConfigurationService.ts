import type { ConfigurationExport } from "../../models/Configuration/ConfigurationExport";
import type { ConfigurationValidation } from "../../models/Configuration/ConfigurationValidation";
import { BaseApiService } from "./BaseApiService";

export interface IConfigurationService {
	exportConfiguration(): Promise<void>;
	clearConfiguration(): Promise<void>;
	validateConfiguration(
		configurationExport: ConfigurationExport,
	): Promise<ConfigurationValidation>;
}

export class ConfigurationService
	extends BaseApiService
	implements IConfigurationService
{
	async exportConfiguration(): Promise<void> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.get("/configuration/export", {
				responseType: "blob",
			});

			const contentDisposition =
				response.headers["content-disposition"] ??
				response.headers["Content-Disposition"];
			let filename = "Lighthouse_Configuration.json";

			if (contentDisposition) {
				const filenameMatch = contentDisposition.match(
					/filename="?([^";]+)"?/i,
				);
				if (filenameMatch && filenameMatch.length > 1) {
					filename = filenameMatch[1];
				}
			}

			const url = globalThis.URL.createObjectURL(new Blob([response.data]));
			const link = document.createElement("a");
			link.href = url;
			link.setAttribute("download", filename);
			document.body.appendChild(link);
			link.click();
			link.remove();
			globalThis.URL.revokeObjectURL(url);
		});
	}

	async clearConfiguration(): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete("/configuration/clear");
		});
	}

	async validateConfiguration(
		configurationExport: ConfigurationExport,
	): Promise<ConfigurationValidation> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<ConfigurationValidation>(
				"/configuration/validate",
				configurationExport,
			);

			return response.data;
		});
	}
}
