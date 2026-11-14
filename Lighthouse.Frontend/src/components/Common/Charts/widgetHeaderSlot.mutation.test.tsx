import { renderHook } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import {
	useFrameIsReady,
	useHeaderSlotContent,
	WidgetHeaderSlotContext,
} from "./widgetHeaderSlot";

const within =
	(offer: (c: ReactNode) => void, frameIsReady: boolean) =>
	({ children }: { children: ReactNode }) => (
		<WidgetHeaderSlotContext.Provider value={{ offer, frameIsReady }}>
			{children}
		</WidgetHeaderSlotContext.Provider>
	);

describe("useFrameIsReady", () => {
	it("is ready outside a frame", () => {
		expect(renderHook(() => useFrameIsReady()).result.current).toBe(true);
	});

	it("follows the frame inside one", () => {
		const offer = vi.fn();
		expect(
			renderHook(() => useFrameIsReady(), { wrapper: within(offer, false) })
				.result.current,
		).toBe(false);
		expect(
			renderHook(() => useFrameIsReady(), { wrapper: within(offer, true) })
				.result.current,
		).toBe(true);
	});
});

describe("useHeaderSlotContent", () => {
	it("offers the content and empties the place again on unmount", () => {
		const offer = vi.fn();
		const { unmount } = renderHook(() => useHeaderSlotContent("chip"), {
			wrapper: within(offer, true),
		});
		expect(offer).toHaveBeenLastCalledWith("chip");
		unmount();
		expect(offer).toHaveBeenLastCalledWith(null);
	});
});
