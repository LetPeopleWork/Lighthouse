import {
	type ILighthouseRelease,
	LighthouseRelease,
} from "../../models/LighthouseRelease/LighthouseRelease";
import { LighthouseReleaseAsset } from "../../models/LighthouseRelease/LighthouseReleaseAsset";
import { BaseApiService } from "./BaseApiService";

export interface IVersionService {
	getCurrentVersion(): Promise<string>;
	isUpdateAvailable(): Promise<boolean>;
	getNewReleases(): Promise<LighthouseRelease[]>;
	isUpdateSupported(): Promise<boolean>;
	installUpdate(): Promise<boolean>;
}

export class VersionService extends BaseApiService {
	async getCurrentVersion(): Promise<string> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string>("/version/current");
			return response.data;
		});
	}

	async isUpdateAvailable(): Promise<boolean> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<boolean>("/version/hasupdate");
			return response.data;
		});
	}

	async getNewReleases(): Promise<LighthouseRelease[]> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<ILighthouseRelease[]>("/version/new");
			return response.data.map(this.deserializeRelease);
		});
	}

	async isUpdateSupported(): Promise<boolean> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<boolean>(
				"/version/updateSupported",
			);
			return response.data;
		});
	}

	async installUpdate(): Promise<boolean> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<boolean>(
				"/version/installUpdate",
			);
			return response.data;
		});
	}

	private deserializeRelease(release: ILighthouseRelease): LighthouseRelease {
		const releaseAssets = release.assets.map(
			(asset) => new LighthouseReleaseAsset(asset.name, asset.link),
		);
		return new LighthouseRelease(
			release.name,
			release.link,
			release.highlights,
			release.version,
			releaseAssets,
		);
	}
}
