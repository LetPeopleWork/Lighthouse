import {
	GridFooter,
	gridFilteredTopLevelRowCountSelector,
	useGridApiContext,
	useGridRootProps,
	useGridSelector,
} from "@mui/x-data-grid";
import type React from "react";

/** A page of any list holds this many rows, the most the free grid allows. */
export const PAGE_SIZE = 100;

/**
 * The footer of every list: nothing at all while the list loads or holds one page or less, so a
 * short list looks exactly as it always has; the grid's own range and page arrows once it runs past
 * one page. The count is the one left after filters, since a column filter never changes the rows.
 */
export const RowRangeFooter: React.FC = () => {
	const apiRef = useGridApiContext();
	const { loading } = useGridRootProps();
	const rowCount = useGridSelector(
		apiRef,
		gridFilteredTopLevelRowCountSelector,
	);

	// The arrows sit under the last row, so the page they open would start far above the fold.
	// Only a press on an arrow scrolls; a sort, filter or reload that changes the page leaves the
	// window where the reader put it.
	const bringTheTopIntoView = (event: React.MouseEvent<HTMLDivElement>) => {
		if (!(event.target instanceof Element) || !event.target.closest("button"))
			return;
		apiRef.current.rootElementRef.current?.scrollIntoView?.({ block: "start" });
	};

	if (loading || rowCount <= PAGE_SIZE) return null;
	return <GridFooter onClick={bringTheTopIntoView} />;
};
