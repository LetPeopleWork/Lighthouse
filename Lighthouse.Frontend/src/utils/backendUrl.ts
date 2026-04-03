import {
	createBackendReadyPromise,
	getTauriBackendUrl,
	isTauriEnv,
} from "./tauri";

let backendUrl: string = import.meta.env.VITE_API_BASE_URL ?? "/api";

const { promise: backendReadyPromise, resolve: resolveBackendReady } =
	createBackendReadyPromise();

// On module load, resolve immediately if we already know the URL
if (isTauriEnv()) {
	const cached = getTauriBackendUrl();
	if (cached) {
		// Post-reload in Tauri — URL was persisted in sessionStorage
		backendUrl = cached;
		resolveBackendReady();
	}
	// Otherwise stays pending until notifyBackendReady() is called
} else {
	// Server mode — URL is known at build time
	resolveBackendReady();
}

/** Called once from the single backend-ready Tauri event listener in App.tsx */
export const notifyBackendReady = (detectedUrl: string): void => {
	const fullUrl = `${detectedUrl}/api`;
	globalThis.sessionStorage.setItem("TAURI_BACKEND_URL", fullUrl);
	backendUrl = fullUrl;
	resolveBackendReady();
};

/** Current backend base URL (may still be the default until ready promise resolves) */
export const getBackendUrl = (): string => backendUrl;

/** Resolves when the backend URL is definitively known */
export const getBackendReadyPromise = (): Promise<void> => backendReadyPromise;
