import { act, renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IOAuthService } from "../services/Api/OAuthService";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { type OAuthSignInCopy, useOAuthSignIn } from "./useOAuthSignIn";

const openOAuthPopupMock = vi.fn();

vi.mock("./useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup: openOAuthPopupMock }),
}));

const COPY: OAuthSignInCopy = {
	popupBlocked: "The popup was blocked.",
	cancelled: "The sign-in was cancelled.",
	startFailed: "The sign-in could not start.",
};

const TARGET = { providerKey: "ado.oauth", connectionId: 7 };

const anOAuthService = (
	initiateConnect: IOAuthService["initiateConnect"] = vi
		.fn()
		.mockResolvedValue({ authorizationUrl: "https://login.example.test" }),
): IOAuthService => ({ initiateConnect, disconnect: vi.fn() });

const renderSignIn = (
	oauthService: IOAuthService = anOAuthService(),
	onSignedIn?: () => void,
) => {
	const context = createMockApiServiceContext({ oauthService });
	const wrapper = ({ children }: { children: ReactNode }) =>
		createElement(ApiServiceContext.Provider, { value: context }, children);
	return renderHook(() => useOAuthSignIn(COPY, onSignedIn), { wrapper });
};

describe("useOAuthSignIn", () => {
	beforeEach(() => {
		openOAuthPopupMock.mockReset();
	});

	it.each([
		[
			"a blocked popup",
			{ status: "popup_blocked" },
			{ severity: "error", text: COPY.popupBlocked },
		],
		[
			"a cancelled popup",
			{ status: "cancelled" },
			{ severity: "info", text: COPY.cancelled },
		],
		[
			"a popup that failed with a reason",
			{ status: "error", reason: "invalid_grant" },
			{ severity: "error", text: "invalid_grant" },
		],
		[
			"a popup that failed without a reason",
			{ status: "error" },
			{ severity: "error", text: "OAuth failed." },
		],
	])(
		"after %s, says so and does not report a sign-in",
		async (_label, popupResult, expected) => {
			openOAuthPopupMock.mockResolvedValue(popupResult);
			const onSignedIn = vi.fn();
			const { result } = renderSignIn(anOAuthService(), onSignedIn);

			await act(() => result.current.signIn(TARGET));

			expect(result.current.inlineMessage).toEqual(expected);
			expect(result.current.isSigningIn).toBe(false);
			expect(onSignedIn).not.toHaveBeenCalled();
		},
	);

	it("when the sign-in cannot start, says it failed to start", async () => {
		const { result } = renderSignIn(
			anOAuthService(vi.fn().mockRejectedValue(new Error("offline"))),
		);

		await act(() => result.current.signIn(TARGET));

		expect(result.current.inlineMessage).toEqual({
			severity: "error",
			text: COPY.startFailed,
		});
		expect(result.current.isSigningIn).toBe(false);
		expect(openOAuthPopupMock).not.toHaveBeenCalled();
	});

	it("a successful sign-in with nobody to tell leaves no message", async () => {
		openOAuthPopupMock.mockResolvedValue({ status: "success" });
		const { result } = renderSignIn();

		await act(() => result.current.signIn(TARGET));

		expect(result.current.inlineMessage).toBeNull();
	});

	it("a new sign-in drops the message of the one before while it runs", async () => {
		openOAuthPopupMock.mockResolvedValueOnce({ status: "cancelled" });
		openOAuthPopupMock.mockReturnValueOnce(new Promise(() => {}));
		const { result } = renderSignIn();
		await act(() => result.current.signIn(TARGET));
		expect(result.current.inlineMessage).not.toBeNull();

		let pendingSignIn: Promise<void> | undefined;
		act(() => {
			pendingSignIn = result.current.signIn(TARGET);
		});

		await waitFor(() => expect(result.current.isSigningIn).toBe(true));
		expect(result.current.inlineMessage).toBeNull();
		expect(pendingSignIn).toBeInstanceOf(Promise);
	});
});
