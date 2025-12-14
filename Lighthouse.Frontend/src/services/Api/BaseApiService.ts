import axios, { type AxiosInstance } from "axios";
import { Feature, type IFeature } from "../../models/Feature";
import { type IPortfolio, Portfolio } from "../../models/Project/Portfolio";
import { type ITeam, Team } from "../../models/Team/Team";
import { ApiError } from "./ApiError";

export class BaseApiService {
	protected apiService: AxiosInstance;

	constructor() {
		let baseUrl = "/api";
		if (import.meta.env.VITE_API_BASE_URL !== undefined) {
			baseUrl = import.meta.env.VITE_API_BASE_URL;
		}

		this.apiService = axios.create({
			baseURL: baseUrl,
		});
	}

	protected async withErrorHandling<T>(
		asyncFunction: () => Promise<T>,
	): Promise<T> {
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
		const error = err;
		const status = error.response?.status ?? "UNKNOWN";
		let message = "An error occurred";
		if (error.response) {
			const data: unknown = error.response.data;
			if (
				data &&
				typeof data === "object" &&
				"message" in (data as Record<string, unknown>)
			) {
				interface HasMessage {
					message?: unknown;
				}
				message = String((data as HasMessage).message ?? "");
			} else if (data && typeof data === "string") {
				message = data;
			}
		}

		if (!message || message.length === 0)
			message = error.message ?? String(status);

		console.error("API Error:", status, message);

		return new ApiError(status, message);
	}

	protected static deserializeTeam(item: ITeam) {
		if (item == null) {
			return null;
		}

		return Team.fromBackend(item);
	}

	protected static deserializePortfolio(item: IPortfolio): Portfolio {
		return Portfolio.fromBackend(item);
	}

	protected static deserializeFeatures(featureData: IFeature[]): Feature[] {
		return featureData.map((feature: IFeature) => {
			return Feature.fromBackend(feature);
		});
	}
}
