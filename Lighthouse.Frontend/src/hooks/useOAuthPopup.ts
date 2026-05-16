export type OAuthPopupStatus =
	| "success"
	| "error"
	| "cancelled"
	| "popup_blocked";

export interface OAuthPopupResult {
	status: OAuthPopupStatus;
	connectionId?: number;
	reason?: string;
}

export interface UseOAuthPopup {
	openOAuthPopup(authorizationUrl: string): Promise<OAuthPopupResult>;
}

// Must be < state-token TTL (15 min). If state-token TTL changes, adjust this value.
const POPUP_COMPLETION_TIMEOUT_MS = 90_000;
const POPUP_POLL_INTERVAL_MS = 500;
const POPUP_WIDTH = 600;
const POPUP_HEIGHT = 700;

interface OAuthCompletePayload {
	type: "oauth.complete";
	status: OAuthPopupStatus;
	connectionId?: number;
	reason?: string;
}

const isOAuthCompletePayload = (
	data: unknown,
): data is OAuthCompletePayload => {
	if (typeof data !== "object" || data === null) {
		return false;
	}
	const candidate = data as { type?: unknown };
	// Convention-based filter — not a security boundary; origin check is the boundary.
	return candidate.type === "oauth.complete";
};

const computePopupFeatures = (): string => {
	const width = POPUP_WIDTH;
	const height = POPUP_HEIGHT;
	const left = Math.max(
		0,
		Math.round((globalThis.screen.availWidth - width) / 2),
	);
	const top = Math.max(
		0,
		Math.round((globalThis.screen.availHeight - height) / 2),
	);
	return `popup=yes,width=${width},height=${height},left=${left},top=${top}`;
};

const openOAuthPopup = (
	authorizationUrl: string,
): Promise<OAuthPopupResult> => {
	return new Promise<OAuthPopupResult>((resolve) => {
		const popup = globalThis.open(
			authorizationUrl,
			"_blank",
			computePopupFeatures(),
		);

		if (popup === null) {
			resolve({ status: "popup_blocked" });
			return;
		}

		const expectedOrigin = globalThis.location.origin;
		let settled = false;
		let intervalId: ReturnType<typeof setInterval> | undefined;
		let timeoutId: ReturnType<typeof setTimeout> | undefined;

		const cleanup = (): void => {
			globalThis.removeEventListener("message", handleMessage);
			if (intervalId !== undefined) {
				clearInterval(intervalId);
			}
			if (timeoutId !== undefined) {
				clearTimeout(timeoutId);
			}
		};

		const settle = (result: OAuthPopupResult): void => {
			if (settled) {
				return;
			}
			settled = true;
			cleanup();
			resolve(result);
		};

		function handleMessage(event: MessageEvent): void {
			if (event.origin !== expectedOrigin) {
				return;
			}
			if (!isOAuthCompletePayload(event.data)) {
				return;
			}
			const payload = event.data;
			settle({
				status: payload.status,
				...(payload.connectionId === undefined
					? {}
					: { connectionId: payload.connectionId }),
				...(payload.reason === undefined ? {} : { reason: payload.reason }),
			});
		}

		globalThis.addEventListener("message", handleMessage);

		intervalId = setInterval(() => {
			if (popup.closed) {
				settle({ status: "cancelled" });
			}
		}, POPUP_POLL_INTERVAL_MS);

		timeoutId = setTimeout(() => {
			settle({ status: "error", reason: "timeout" });
		}, POPUP_COMPLETION_TIMEOUT_MS);
	});
};

export const useOAuthPopup = (): UseOAuthPopup => {
	return { openOAuthPopup };
};

export default useOAuthPopup;
