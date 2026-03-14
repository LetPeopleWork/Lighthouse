import axios, { type AxiosInstance } from "axios";
import { Feature, type IFeature } from "../../models/Feature";
import { type IPortfolio, Portfolio } from "../../models/Portfolio/Portfolio";
import { type ITeam, Team } from "../../models/Team/Team";
import {
	createBackendReadyPromise,
	getTauriBackendUrl,
	isTauriEnv,
} from "../../utils/tauri";
import { ApiError } from "./ApiError";

// Module-level — stable across all instances, no constructor async needed
const { promise: backendReadyPromise, resolve: resolveBackendReady } =
	createBackendReadyPromise();

// Starts listening for the Tauri backend-ready event, or resolves immediately
const initTauriDynamicPort = async (
	onReady: (url: string) => void,
): Promise<void> => {
	if (!isTauriEnv()) {
		resolveBackendReady();
		return;
	}

	try {
		const { listen } = await import("@tauri-apps/api/event");
		await listen<string>("backend-ready", (event) => {
			onReady(event.payload);
			resolveBackendReady();
		});
	} catch (err) {
		console.error("Failed to initialize Tauri listener", err);
	}
};

let tauriInitialized = false;

const setupBackendUrl = (apiService: AxiosInstance): void => {
	const savedUrl = getTauriBackendUrl();

	if (savedUrl) {
		resolveBackendReady();
		return;
	}

	if (tauriInitialized) return;
	tauriInitialized = true;

	initTauriDynamicPort((detectedUrl) => {
		const fullUrl = `${detectedUrl}/api`;
		globalThis.sessionStorage.setItem("TAURI_BACKEND_URL", fullUrl);
		apiService.defaults.baseURL = fullUrl;
		console.log(`[BaseApiService] 🚀 Backend Port Synced: ${fullUrl}`);
	});
};

export class BaseApiService {
	protected apiService: AxiosInstance;

	constructor() {
		const savedUrl = getTauriBackendUrl();
		const baseUrl = savedUrl ?? import.meta.env.VITE_API_BASE_URL ?? "/api";

		this.apiService = axios.create({ baseURL: baseUrl });

		setupBackendUrl(this.apiService);
	}

	protected async withErrorHandling<T>(
		asyncFunction: () => Promise<T>,
	): Promise<T> {
		await backendReadyPromise;

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
