import { Box, useMediaQuery, useTheme } from "@mui/material";
import type React from "react";

/**
 * Dashboard item with standardized sizing based on 12-column grid
 */
export interface DashboardItem {
	id?: string | number;
	node: React.ReactNode | null | undefined;
	priority?: number;
	size?: "small" | "medium" | "large" | "xlarge";
	minWidth?: number;
}

interface DashboardProps {
	items: DashboardItem[];
	spacing?: number;
	baseRowHeight?: number;
	allowVerticalStacking?: boolean;
}

const Dashboard: React.FC<DashboardProps> = ({
	items,
	spacing = 2,
	baseRowHeight = 100,
	allowVerticalStacking = true,
}) => {
	const visibleItems = items
		.filter((it) => it.node != null)
		.sort((a, b) => (a.priority ?? 999) - (b.priority ?? 999));

	const theme = useTheme();
	const isXlUp = useMediaQuery(theme.breakpoints.up("xl"));
	const isLgUp = useMediaQuery(theme.breakpoints.up("lg"));
	const isMdUp = useMediaQuery(theme.breakpoints.up("md"));
	const isSmUp = useMediaQuery(theme.breakpoints.up("sm"));

	// More granular responsive column counts
	const columns = isXlUp ? 12 : isLgUp ? 10 : isMdUp ? 8 : isSmUp ? 6 : 4;
	const gapPx = spacing * 8;

	// Get responsive size configuration
	const getResponsiveSize = (
		size: string = "medium",
		availableColumns: number,
	) => {
		// Define sizes for different breakpoints
		const sizeConfigs = {
			small: {
				xl: { colSpan: 3, rowSpan: 1 },
				lg: { colSpan: 2, rowSpan: 1 },
				md: { colSpan: 2, rowSpan: 1 },
				sm: { colSpan: 3, rowSpan: 1 },
				xs: { colSpan: 4, rowSpan: 1 },
			},
			medium: {
				xl: { colSpan: 4, rowSpan: 2 },
				lg: { colSpan: 5, rowSpan: 2 },
				md: { colSpan: 4, rowSpan: 2 },
				sm: { colSpan: 6, rowSpan: 2 },
				xs: { colSpan: 4, rowSpan: 2 },
			},
			large: {
				xl: { colSpan: 6, rowSpan: 5 },
				lg: { colSpan: 5, rowSpan: 5 },
				md: { colSpan: 8, rowSpan: 4 },
				sm: { colSpan: 6, rowSpan: 4 },
				xs: { colSpan: 4, rowSpan: 3 },
			},
			xlarge: {
				xl: { colSpan: 12, rowSpan: 4 },
				lg: { colSpan: 10, rowSpan: 4 },
				md: { colSpan: 8, rowSpan: 4 },
				sm: { colSpan: 6, rowSpan: 4 },
				xs: { colSpan: 4, rowSpan: 4 },
			},
		};

		const sizeConfig =
			sizeConfigs[size as keyof typeof sizeConfigs] || sizeConfigs.medium;

		// Determine current breakpoint
		let currentBreakpoint: keyof typeof sizeConfig;
		if (isXlUp) currentBreakpoint = "xl";
		else if (isLgUp) currentBreakpoint = "lg";
		else if (isMdUp) currentBreakpoint = "md";
		else if (isSmUp) currentBreakpoint = "sm";
		else currentBreakpoint = "xs";

		const targetSize = sizeConfig[currentBreakpoint];

		// Ensure we don't exceed available columns
		return {
			colSpan: Math.min(availableColumns, targetSize.colSpan),
			rowSpan: targetSize.rowSpan,
		};
	};

	// Calculate if an item should be hidden on very small screens
	const shouldHideItem = (item: DashboardItem, index: number) => {
		if (!allowVerticalStacking && !isSmUp) {
			// On very small screens, only show high-priority items
			const priority = item.priority ?? 999;
			const visibleCount = Math.floor(columns / 2); // Show roughly half the items
			return priority > 50 || index >= visibleCount;
		}
		return false;
	};

	// Filter items based on screen size if needed
	const displayItems = allowVerticalStacking
		? visibleItems
		: visibleItems.filter((item, index) => !shouldHideItem(item, index));

	return (
		<Box
			component="section"
			sx={{
				display: "grid",
				gridTemplateColumns: `repeat(${columns}, 1fr)`,
				gap: `${gapPx}px`,
				alignItems: "start",
				gridAutoRows: `${baseRowHeight}px`,
				gridAutoFlow: "row",
				width: "100%",
				// Add some padding on very small screens
				px: isSmUp ? 0 : 1,
			}}
		>
			{displayItems.map((item, index) => {
				const key = item.id ?? index;

				// Get responsive size
				const { colSpan, rowSpan } = getResponsiveSize(item.size, columns);

				return (
					<Box
						key={String(key)}
						data-testid={`dashboard-item-${key}`}
						data-size={item.size || "medium"}
						data-colspan={colSpan}
						data-rowspan={rowSpan}
						sx={{
							gridColumn: `span ${colSpan}`,
							gridRow: `span ${rowSpan}`,
							width: "100%",
							height: "100%",
							display: "flex",
							flexDirection: "column",
							overflow: "hidden",
							transition: "all 0.2s ease-in-out",
							minHeight: `${baseRowHeight * rowSpan + gapPx * (rowSpan - 1)}px`,
						}}
					>
						<Box
							sx={{
								width: "100%",
								height: "100%",
								overflow: "auto",
								display: "flex",
								flexDirection: "column",
								// Add some internal padding for better spacing
								p: 1,
							}}
						>
							{item.node}
						</Box>
					</Box>
				);
			})}
		</Box>
	);
};

export default Dashboard;
