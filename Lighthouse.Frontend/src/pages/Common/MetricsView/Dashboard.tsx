import { Box, useMediaQuery, useTheme } from "@mui/material";
import type React from "react";

/**
 * Dashboard item width is provided as a single `width` number expressed in
 * logical columns (baseColumns). The dashboard scales this to a 12-column grid.
 * Example: with baseColumns=4, a width of 1 becomes 3 (out of 12).
 */
export interface DashboardItem {
	id?: string | number;
	node: React.ReactNode | null | undefined;
	/** explicit column start (1-based) in the 12-column grid. If provided, will be used instead of flow placement */
	// ... existing fields below
	colStart?: number;
	rowStart?: number;
	/** explicit column span in 12-column grid (1..12). */
	colSpan?: number;
	/** explicit row span. */
	rowSpan?: number;
}

interface DashboardProps {
	items: DashboardItem[];
	/** spacing measured in theme spacing units (default 2 -> 16px) */
	spacing?: number;
}

// callers provide spans directly in 12-grid units (optimized for desktop)

const Dashboard: React.FC<DashboardProps> = ({ items, spacing = 2 }) => {
	const visibleOriginal = items.filter((it) => it.node != null);

	// preserve original relative order
	const visible = visibleOriginal;

	// spacing: MUI spacing unit is 8px by default
	const gapPx = spacing * 8;

	// determine the active column count based on breakpoints. callers provide
	// sizes assuming the largest (12) grid; on smaller screens we scale down
	// to 8 or 4 columns so items "break" / stack appropriately.
	const theme = useTheme();
	const isMdUp = useMediaQuery(theme.breakpoints.up("md"));
	const isSmUp = useMediaQuery(theme.breakpoints.up("sm"));
	// Use a 12-column layout on desktop (md+), 8 columns for tablet (sm..md),
	// and a minimum 4-column layout on the smallest screens (<sm).
	const columns = isMdUp ? 12 : isSmUp ? 8 : 4;

	// (colspan calculation handled per-item below so tests can assert canonical
	// 12-grid values via data attributes while CSS uses a scaled span)

	return (
		<Box
			component="section"
			sx={{
				display: "grid",
				gridTemplateColumns: `repeat(${columns}, 1fr)`,
				gap: `${gapPx}px`,
				alignItems: "start",
				// make rows a consistent unit so row spans work predictably
				gridAutoRows: "minmax(80px, auto)",
				// allow dense packing so smaller items fill gaps when possible
				gridAutoFlow: "row dense",
				width: "100%",
			}}
		>
			{visible.map((item, index) => {
				const key = item.id ?? index;

				// compute canonical 12-grid colspan for tests / data attributes
				// default canonical 12-grid span is 3 columns (1 logical width)
				const colSpan12 = Math.max(
					1,
					Math.min(12, Math.round(item.colSpan ?? 3)),
				);

				// scaled colspan for current breakpoint
				let colSpan = Math.max(
					1,
					Math.min(columns, Math.round((colSpan12 * columns) / 12)),
				);

				// On the smallest layout (4 columns) prefer to collapse wide items
				// to full-width for readability. Also collapse when scaled would
				// otherwise occupy most of the row (threshold).
				const collapseThreshold = Math.ceil(columns / 1.5);
				const forceStack = columns === 4 || colSpan >= collapseThreshold;
				if (forceStack) {
					colSpan = columns;
				}
				const rowSpan = item.rowSpan ?? 1;

				// If caller provided explicit start positions (expressed against a
				// 12-column grid) scale them down to the active `columns` so the
				// placement remains approximately where expected on smaller screens.
				const gridColumn =
					// when collapsed, ignore provided colStart and let the item
					// span the full width so it flows naturally
					forceStack
						? `span ${colSpan}`
						: typeof item.colStart === "number"
							? `${Math.max(1, Math.min(columns, Math.round(((item.colStart - 1) * columns) / 12) + 1))} / span ${colSpan}`
							: `span ${colSpan}`;

				const gridRow =
					// similarly, when stacked ignore explicit rowStart
					forceStack
						? `span ${rowSpan}`
						: typeof item.rowStart === "number"
							? `${item.rowStart} / span ${rowSpan}`
							: `span ${rowSpan}`;

				return (
					<Box
						key={String(key)}
						data-colspan={colSpan12}
						data-rowspan={rowSpan}
						sx={{
							gridColumn,
							gridRow,
							width: "100%",
							display: "flex",
							flexDirection: "column",
							minHeight: 0,
							overflow: "visible",
						}}
					>
						{item.node}
					</Box>
				);
			})}
		</Box>
	);
};

export default Dashboard;
