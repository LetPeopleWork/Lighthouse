import type { useGridApiRef } from "@mui/x-data-grid";
import { useEffect, useState } from "react";

export type OverflowEdges = "none" | "left" | "right" | "both";

/** Pixels the browser's rounding may leave between the content and the viewport. */
const TOLERANCE_PX = 1;

/** Which sides of the table have columns scrolled out of view. A table that fits has none. */
export function overflowEdges(
	scrollLeft: number,
	viewportWidth: number,
	contentWidth: number,
): OverflowEdges {
	if (contentWidth <= viewportWidth + TOLERANCE_PX) return "none";
	const hiddenLeft = scrollLeft > TOLERANCE_PX;
	const hiddenRight =
		contentWidth - (scrollLeft + viewportWidth) > TOLERANCE_PX;
	if (hiddenLeft && hiddenRight) return "both";
	if (hiddenLeft) return "left";
	if (hiddenRight) return "right";
	return "none";
}

type GridApiRef = ReturnType<typeof useGridApiRef>;

const EVENTS_CHANGING_THE_FIT = [
	"resize",
	"columnWidthChange",
	"columnVisibilityModelChange",
] as const;

/** The sides of the grid with hidden columns, kept current as the person scrolls and the box changes. */
export function useHorizontalOverflow(apiRef: GridApiRef): OverflowEdges {
	const [edges, setEdges] = useState<OverflowEdges>("none");

	useEffect(() => {
		const api = apiRef.current;
		if (!api) return;
		const measure = (scrollLeft: number) => {
			const { contentSize, viewportInnerSize } = api.getRootDimensions();
			setEdges(
				overflowEdges(scrollLeft, viewportInnerSize.width, contentSize.width),
			);
		};
		const measureAtScrollPosition = () => measure(api.getScrollPosition().left);
		measureAtScrollPosition();
		const unsubscribes = [
			api.subscribeEvent("scrollPositionChange", ({ left }) => measure(left)),
			...EVENTS_CHANGING_THE_FIT.map((event) =>
				api.subscribeEvent(event, measureAtScrollPosition),
			),
		];
		return () => {
			for (const unsubscribe of unsubscribes) unsubscribe();
		};
	}, [apiRef]);

	return edges;
}
