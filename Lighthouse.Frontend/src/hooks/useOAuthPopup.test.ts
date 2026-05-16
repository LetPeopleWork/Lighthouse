import { act, renderHook } from "@testing-library/react";
import {
	afterEach,
	beforeEach,
	describe,
	expect,
	it,
	type MockInstance,
	vi,
} from "vitest";
import { useOAuthPopup } from "./useOAuthPopup";

const AUTH_URL = "https://auth.atlassian.com/authorize?state=xyz";
const OPENER_ORIGIN = "https://lighthouse.example.com";

type FakePopup = {
	closed: boolean;
	close: ReturnType<typeof vi.fn>;
};

const makeFakePopup = (): FakePopup => ({
	closed: false,
	close: vi.fn(),
});

const dispatchOpenerMessage = (init: {
	data: unknown;
	origin: string;
}): void => {
	const event = new MessageEvent("message", {
		data: init.data,
		origin: init.origin,
	});
	window.dispatchEvent(event);
};

describe("useOAuthPopup", () => {
	let openSpy: MockInstance<
		(url?: string | URL, target?: string, features?: string) => Window | null
	>;

	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		Object.defineProperty(window, "location", {
			value: { origin: OPENER_ORIGIN, assign: vi.fn() },
			writable: true,
		});
		openSpy = vi.spyOn(window, "open");
	});

	afterEach(() => {
		vi.useRealTimers();
		vi.restoreAllMocks();
	});

	it("resolves with popup_blocked when window.open returns null", async () => {
		openSpy.mockReturnValue(null);

		const { result } = renderHook(() => useOAuthPopup());

		const resolved = await act(() => result.current.openOAuthPopup(AUTH_URL));

		expect(resolved.status).toBe("popup_blocked");
		expect(openSpy).toHaveBeenCalledWith(
			AUTH_URL,
			expect.any(String),
			expect.stringContaining("popup"),
		);
	});

	it("resolves with success when the landing page posts an oauth.complete message from the opener's origin", async () => {
		const fakePopup = makeFakePopup();
		openSpy.mockReturnValue(fakePopup as unknown as Window);

		const { result } = renderHook(() => useOAuthPopup());

		const pending = result.current.openOAuthPopup(AUTH_URL);

		await act(async () => {
			dispatchOpenerMessage({
				data: { type: "oauth.complete", status: "success", connectionId: 88 },
				origin: OPENER_ORIGIN,
			});
		});

		const resolved = await pending;

		expect(resolved).toEqual({
			status: "success",
			connectionId: 88,
		});
	});

	it("ignores messages whose origin does not match the opener origin", async () => {
		const fakePopup = makeFakePopup();
		openSpy.mockReturnValue(fakePopup as unknown as Window);

		const { result } = renderHook(() => useOAuthPopup());

		const pending = result.current.openOAuthPopup(AUTH_URL);

		await act(async () => {
			dispatchOpenerMessage({
				data: { type: "oauth.complete", status: "success", connectionId: 99 },
				origin: "https://evil.example.com",
			});
			dispatchOpenerMessage({
				data: { type: "oauth.complete", status: "success", connectionId: 88 },
				origin: OPENER_ORIGIN,
			});
		});

		const resolved = await pending;

		expect(resolved.connectionId).toBe(88);
	});

	it("ignores same-origin messages whose event.data.type is not oauth.complete", async () => {
		const fakePopup = makeFakePopup();
		openSpy.mockReturnValue(fakePopup as unknown as Window);

		const { result } = renderHook(() => useOAuthPopup());

		const pending = result.current.openOAuthPopup(AUTH_URL);

		await act(async () => {
			dispatchOpenerMessage({
				data: { type: "unrelated.event", payload: "noise" },
				origin: OPENER_ORIGIN,
			});
			dispatchOpenerMessage({
				data: { type: "oauth.complete", status: "success", connectionId: 88 },
				origin: OPENER_ORIGIN,
			});
		});

		const resolved = await pending;

		expect(resolved).toEqual({ status: "success", connectionId: 88 });
	});

	it("resolves with cancelled when the popup is closed by the user without sending a message", async () => {
		const fakePopup = makeFakePopup();
		openSpy.mockReturnValue(fakePopup as unknown as Window);

		const { result } = renderHook(() => useOAuthPopup());

		const pending = result.current.openOAuthPopup(AUTH_URL);

		await act(async () => {
			fakePopup.closed = true;
			await vi.advanceTimersByTimeAsync(1000);
		});

		const resolved = await pending;

		expect(resolved.status).toBe("cancelled");
	});

	it("propagates error status and reason when the landing page reports an IdP error", async () => {
		const fakePopup = makeFakePopup();
		openSpy.mockReturnValue(fakePopup as unknown as Window);

		const { result } = renderHook(() => useOAuthPopup());

		const pending = result.current.openOAuthPopup(AUTH_URL);

		await act(async () => {
			dispatchOpenerMessage({
				data: {
					type: "oauth.complete",
					status: "error",
					reason: "invalid_grant",
				},
				origin: OPENER_ORIGIN,
			});
		});

		const resolved = await pending;

		expect(resolved).toEqual({ status: "error", reason: "invalid_grant" });
	});

	it("resolves with error and reason 'timeout' when the popup never reports completion within 90 seconds", async () => {
		const fakePopup = makeFakePopup();
		openSpy.mockReturnValue(fakePopup as unknown as Window);

		const { result } = renderHook(() => useOAuthPopup());

		const pending = result.current.openOAuthPopup(AUTH_URL);

		await act(async () => {
			await vi.advanceTimersByTimeAsync(90_000);
		});

		const resolved = await pending;

		expect(resolved).toEqual({ status: "error", reason: "timeout" });
	});

	it("removes the message listener and stops polling popup.closed after resolution", async () => {
		const fakePopup = makeFakePopup();
		openSpy.mockReturnValue(fakePopup as unknown as Window);

		const { result } = renderHook(() => useOAuthPopup());
		const removeListenerSpy = vi.spyOn(window, "removeEventListener");
		const clearIntervalSpy = vi.spyOn(globalThis, "clearInterval");

		const pending = result.current.openOAuthPopup(AUTH_URL);

		await act(async () => {
			dispatchOpenerMessage({
				data: { type: "oauth.complete", status: "success", connectionId: 88 },
				origin: OPENER_ORIGIN,
			});
		});

		await pending;

		expect(
			removeListenerSpy.mock.calls.some(([type]) => type === "message"),
		).toBe(true);
		expect(clearIntervalSpy).toHaveBeenCalled();
	});
});
