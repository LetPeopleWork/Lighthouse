import type { AxiosResponse } from "axios";
import { ApiError } from "./ApiError";

// Bug #5732: for five months, browsers running a precached pre-v26.3.13.16 client called
// API routes that had since moved to /api/latest. The SPA fallback answered each one with
// index.html and a 200, so the app saw HTML where it expected JSON and blanked with an
// unattributable TypeError. Say what actually happened instead.
export function assertNotHtmlResponse<T>(
	response: AxiosResponse<T>,
): AxiosResponse<T> {
	if (response.config?.responseType === "blob") {
		return response;
	}

	const contentType = response.headers?.["content-type"];

	if (typeof contentType === "string" && contentType.startsWith("text/html")) {
		throw new ApiError(
			"STALE_CLIENT",
			"Lighthouse received a web page where data was expected. Your browser is most likely running an outdated version — reload with Ctrl+Shift+R.",
			`${response.config?.url ?? "An API request"} returned ${contentType} instead of JSON.`,
		);
	}

	return response;
}
