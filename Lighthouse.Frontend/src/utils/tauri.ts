interface TauriGlobal {
	__TAURI_INTERNALS__?: unknown;
}

export const isTauriEnv = (): boolean =>
	!!(globalThis as unknown as TauriGlobal).__TAURI_INTERNALS__;

// WebKit on macOS ignores print() on the hidden print frame, so a PDF cannot be made there.
export const isTauriOnMac = (): boolean =>
	isTauriEnv() && /Macintosh|Mac OS X/i.test(navigator.userAgent);

export const hasTauriBackendUrl = (): boolean =>
	!!globalThis.sessionStorage.getItem("TAURI_BACKEND_URL");

export const getTauriBackendUrl = (): string | null =>
	globalThis.sessionStorage.getItem("TAURI_BACKEND_URL");

export const setTauriBackendUrl = (url: string): void => {
	globalThis.sessionStorage.setItem("TAURI_BACKEND_URL", url);
};

export const createBackendReadyPromise = (): {
	promise: Promise<void>;
	resolve: () => void;
} => {
	let resolve!: () => void;
	const promise = new Promise<void>((res) => {
		resolve = res;
	});
	return { promise, resolve };
};
