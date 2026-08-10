import axios, { type AxiosInstance } from "axios";
import { z } from "zod";
import { Feature, FeatureSchema } from "../../models/Feature";
import { Portfolio, PortfolioSchema } from "../../models/Portfolio/Portfolio";
import { Team, TeamSchema } from "../../models/Team/Team";
import { getBackendReadyPromise, getBackendUrl } from "../../utils/backendUrl";
import { ApiError } from "./ApiError";
import { assertNotHtmlResponse } from "./htmlResponseGuard";

export class BaseApiService {
	protected apiService: AxiosInstance;

	constructor() {
		this.apiService = axios.create({
			baseURL: `${getBackendUrl()}/latest`,
		});

		this.apiService.interceptors?.response.use(assertNotHtmlResponse);

		// Once the backend URL is definitively known, update the base URL
		getBackendReadyPromise().then(() => {
			if (this.apiService?.defaults) {
				this.apiService.defaults.baseURL = `${getBackendUrl()}/latest`;
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
		const parsed = BaseApiService.parseApiErrorPayload(
			data,
			err.message,
			status,
		);

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

	protected static parse<TSchema extends z.ZodType>(
		schema: TSchema,
		data: unknown,
	): z.infer<TSchema> {
		const result = schema.safeParse(data);
		if (!result.success) {
			throw new ApiError(
				"INVALID_RESPONSE",
				"Received an unexpected response from the server.",
				z.prettifyError(result.error),
			);
		}
		return result.data;
	}

	// Bug #5732: HTTP success is not contract success. When the SPA fallback answered an API
	// call with index.html, `response.data` was a raw HTML string and `.map` threw a bare
	// TypeError that no caller handled. Fail as an ApiError instead, on the path callers
	// already understand.
	protected static asArray<T>(data: T[], endpoint: string): T[] {
		if (!Array.isArray(data)) {
			throw new ApiError(
				"INVALID_RESPONSE",
				"Received an unexpected response from the server.",
				`Expected a list from ${endpoint} but received ${typeof data}.`,
			);
		}

		return data;
	}

	protected static deserializeTeam(item: unknown): Team | null {
		if (item == null) return null;
		return Team.fromParsed(BaseApiService.parse(TeamSchema, item));
	}

	protected static deserializePortfolio(item: unknown): Portfolio {
		return Portfolio.fromParsed(BaseApiService.parse(PortfolioSchema, item));
	}

	protected static deserializeFeatures(data: unknown): Feature[] {
		return BaseApiService.parse(z.array(FeatureSchema), data).map(
			Feature.fromParsed,
		);
	}
}
