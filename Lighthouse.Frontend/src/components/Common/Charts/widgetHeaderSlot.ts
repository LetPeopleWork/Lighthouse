import {
	createContext,
	type ReactNode,
	useContext,
	useLayoutEffect,
} from "react";

/**
 * What a widget frame offers the chart inside it: a place in its header for something the chart
 * alone can compute, and whether the frame is showing a current answer.
 */
export interface WidgetHeaderSlot {
	readonly offer: (content: ReactNode) => void;
	readonly frameIsReady: boolean;
}

export const WidgetHeaderSlotContext = createContext<WidgetHeaderSlot | null>(
	null,
);

/** Outside a frame there is no older answer to be behind, so the chart counts as ready. */
export function useFrameIsReady(): boolean {
	return useContext(WidgetHeaderSlotContext)?.frameIsReady ?? true;
}

/** Puts the content in the frame's header while the chart is mounted; null leaves the place empty. */
export function useHeaderSlotContent(content: ReactNode): void {
	const offer = useContext(WidgetHeaderSlotContext)?.offer;

	// A layout effect lands before the browser paints, so the header never shows a stale chip.
	useLayoutEffect(() => {
		if (!offer) return;
		offer(content);
		return () => offer(null);
	}, [offer, content]);
}
