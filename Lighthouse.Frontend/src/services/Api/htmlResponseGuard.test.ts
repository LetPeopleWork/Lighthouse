import type { AxiosResponse } from "axios";
import { describe, expect, it } from "vitest";
import { ApiError } from "./ApiError";
import { assertNotHtmlResponse } from "./htmlResponseGuard";

const buildResponse = (
	contentType: string | undefined,
	config: Record<string, unknown> = {},
): AxiosResponse =>
	({
		data: "",
		status: 200,
		statusText: "OK",
		headers: contentType ? { "content-type": contentType } : {},
		config: { url: "/teams", ...config },
	}) as unknown as AxiosResponse;

describe("assertNotHtmlResponse", () => {
	// Bug #5732: a stale client hitting removed routes got index.html with a 200 on every
	// call. Naming that explicitly is what turns a five-month mystery into a one-line fix.
	it("rejects an HTML body where JSON was expected", () => {
		expect(() => assertNotHtmlResponse(buildResponse("text/html"))).toThrow(
			ApiError,
		);
	});

	it("tells the user how to recover", () => {
		expect(() =>
			assertNotHtmlResponse(buildResponse("text/html; charset=utf-8")),
		).toThrow(/ctrl\+shift\+r/i);
	});

	it("passes JSON through untouched", () => {
		const response = buildResponse("application/json; charset=utf-8");

		expect(assertNotHtmlResponse(response)).toBe(response);
	});

	it("passes a response with no content type through", () => {
		const response = buildResponse(undefined);

		expect(assertNotHtmlResponse(response)).toBe(response);
	});

	// The database backup download is a blob and must never be intercepted.
	it("ignores blob responses", () => {
		const response = buildResponse("text/html", { responseType: "blob" });

		expect(assertNotHtmlResponse(response)).toBe(response);
	});
});
