import { BaseApiService } from "./BaseApiService";

export interface IConfigurationService {
	exportConfiguration(): Promise<void>;
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

			const contentDisposition = response.headers["content-disposition"];
			let filename = "Lighthouse_Configuration.json";

			if (contentDisposition) {
				const filenameMatch = contentDisposition.match(
					/filename="?(.+?)"?(?:;|$)/,
				);
				if (filenameMatch?.[1]) {
					filename = filenameMatch[1];
				}
			}

			const url = window.URL.createObjectURL(new Blob([response.data]));
			const link = document.createElement("a");
			link.href = url;
			link.setAttribute("download", filename);
			document.body.appendChild(link);
			link.click();
			document.body.removeChild(link);
			window.URL.revokeObjectURL(url);
		});
	}
}
