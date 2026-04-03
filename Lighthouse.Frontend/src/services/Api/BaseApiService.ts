import axios, { type AxiosInstance } from "axios";
import { Feature, type IFeature } from "../../models/Feature";
import { type IPortfolio, Portfolio } from "../../models/Portfolio/Portfolio";
import { type ITeam, Team } from "../../models/Team/Team";
import { getBackendReadyPromise, getBackendUrl } from "../../utils/backendUrl";
import { ApiError } from "./ApiError";

export class BaseApiService {
	protected apiService: AxiosInstance;

	constructor() {
		this.apiService = axios.create({ baseURL: getBackendUrl() });

		// Once the backend URL is definitively known, update the base URL
		getBackendReadyPromise().then(() => {
			if (this.apiService?.defaults) {
				this.apiService.defaults.baseURL = getBackendUrl();
			}
		});
	}

	protected async withErrorHandling<T>(
		asyncFunction: () => Promise<T>,
	): Promise<T> {
		await getBackendReadyPromise();

		try {
			return await asyncFunction();
		} catch (error) {
			const apiError = BaseApiService.createApiErrorFromAxios(error);
			if (apiError) throw apiError;

			console.error("Error during async function execution:", error);
			throw error;
		}
	}

	private static createApiErrorFromAxios(err: unknown): ApiError | null {
		if (!axios.isAxiosError(err)) return null;

		const status = err.response?.status ?? "UNKNOWN";
		const data: unknown = err.response?.data;
		let message = "An error occurred";

		if (data && typeof data === "object" && "message" in data) {
			const { message: msg } = data as { message?: unknown };
			message = typeof msg === "string" ? msg : message;
		} else if (typeof data === "string") {
			message = data;
		} else {
			message = err.message ?? String(status);
		}

		console.error("API Error:", status, message);
		return new ApiError(status, message);
	}

	protected static deserializeTeam(item: ITeam) {
		if (item == null) return null;
		return Team.fromBackend(item);
	}

	protected static deserializePortfolio(item: IPortfolio): Portfolio {
		return Portfolio.fromBackend(item);
	}

	protected static deserializeFeatures(featureData: IFeature[]): Feature[] {
		return featureData.map((feature: IFeature) => Feature.fromBackend(feature));
	}
}
