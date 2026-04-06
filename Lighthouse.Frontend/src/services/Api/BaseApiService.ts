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
			throw error;
		}
	}

	private static createApiErrorFromAxios(err: unknown): ApiError | null {
		if (!axios.isAxiosError(err)) return null;

		const status = err.response?.status ?? "UNKNOWN";
		const data: unknown = err.response?.data;
		const parsed = BaseApiService.parseApiErrorPayload(data, err.message, status);

		return new ApiError(
			status,
			parsed.message,
			parsed.technicalDetails,
			parsed.fieldName,
		);
	}

	private static parseApiErrorPayload(
		data: unknown,
		axiosMessage: string | undefined,
		status: string | number,
	): {
		message: string;
		technicalDetails?: string;
		fieldName?: string;
	} {
		const fallbackMessage = axiosMessage ?? String(status);

		if (typeof data === "string") {
			return { message: data };
		}

		if (data && typeof data === "object") {
			const payload = data as {
				message?: unknown;
				Message?: unknown;
				errors?: unknown;
				technicalDetails?: unknown;
				TechnicalDetails?: unknown;
				fieldName?: unknown;
				FieldName?: unknown;
			};

			return {
				message: BaseApiService.extractMessage(payload, fallbackMessage),
				technicalDetails: BaseApiService.extractString(
					payload.technicalDetails,
					payload.TechnicalDetails,
				),
				fieldName: BaseApiService.extractString(
					payload.fieldName,
					payload.FieldName,
				),
			};
		}

		return { message: fallbackMessage };
	}

	private static extractMessage(
		payload: {
			message?: unknown;
			Message?: unknown;
			errors?: unknown;
		},
		fallbackMessage: string,
	): string {
		const directMessage = BaseApiService.extractString(
			payload.message,
			payload.Message,
		);
		if (directMessage) {
			return directMessage;
		}

		if (Array.isArray(payload.errors)) {
			const stringErrors = payload.errors.filter(
				(error): error is string => typeof error === "string",
			);
			if (stringErrors.length > 0) {
				return stringErrors.join("\n");
			}
		}

		return fallbackMessage;
	}

	private static extractString(...values: unknown[]): string | undefined {
		for (const value of values) {
			if (typeof value === "string") {
				return value;
			}
		}

		return undefined;
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
