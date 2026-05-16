import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import OAuthPopupComplete from "./OAuthPopupComplete";

const OPENER_ORIGIN = "https://lighthouse.example.com";

const renderLanding = (search: string) => {
	return render(
		<MemoryRouter initialEntries={[`/oauth/popup-complete${search}`]}>
			<Routes>
				<Route path="/oauth/popup-complete" element={<OAuthPopupComplete />} />
			</Routes>
		</MemoryRouter>,
	);
};

describe("OAuthPopupComplete", () => {
	const originalOpener = window.opener;
	const originalClose = window.close;
	let postMessageSpy: ReturnType<typeof vi.fn>;
	let closeSpy: ReturnType<typeof vi.fn>;

	beforeEach(() => {
		Object.defineProperty(window, "location", {
			value: { origin: OPENER_ORIGIN, assign: vi.fn() },
			writable: true,
		});
		postMessageSpy = vi.fn();
		closeSpy = vi.fn();
		Object.defineProperty(window, "opener", {
			value: { postMessage: postMessageSpy },
			writable: true,
			configurable: true,
		});
		Object.defineProperty(window, "close", {
			value: closeSpy,
			writable: true,
			configurable: true,
		});
	});

	afterEach(() => {
		vi.restoreAllMocks();
		Object.defineProperty(window, "opener", {
			value: originalOpener,
			writable: true,
			configurable: true,
		});
		Object.defineProperty(window, "close", {
			value: originalClose,
			writable: true,
			configurable: true,
		});
	});

	it("posts an oauth.complete success message to the opener with the configured target origin and closes the popup", () => {
		renderLanding("?status=success&connectionId=88");

		expect(postMessageSpy).toHaveBeenCalledTimes(1);
		expect(postMessageSpy).toHaveBeenCalledWith(
			{ type: "oauth.complete", status: "success", connectionId: 88 },
			OPENER_ORIGIN,
		);
		expect(closeSpy).toHaveBeenCalled();
	});

	it("posts an oauth.complete error message with the reason code when the callback failed", () => {
		renderLanding("?status=error&reason=invalid_grant");

		expect(postMessageSpy).toHaveBeenCalledWith(
			{
				type: "oauth.complete",
				status: "error",
				reason: "invalid_grant",
			},
			OPENER_ORIGIN,
		);
		expect(closeSpy).toHaveBeenCalled();
	});

	it("renders the fallback message and does not attempt to post when the popup has no opener", () => {
		Object.defineProperty(window, "opener", {
			value: null,
			writable: true,
			configurable: true,
		});

		renderLanding("?status=success&connectionId=88");

		expect(screen.getByText(/you may close this window/i)).toBeInTheDocument();
		expect(postMessageSpy).not.toHaveBeenCalled();
	});
});
