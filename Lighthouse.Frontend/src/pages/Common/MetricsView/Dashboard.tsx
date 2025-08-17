import HideSourceIcon from "@mui/icons-material/HideSource";
import VisibilityIcon from "@mui/icons-material/Visibility";
import {
	Box,
	IconButton,
	Tooltip,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import React from "react";
import {
	appColors,
	getColorWithOpacity,
	getContrastText,
} from "../../../utils/theme/colors";

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
	dashboardId: string;
}

const Dashboard: React.FC<DashboardProps> = ({
	items,
	spacing = 2,
	baseRowHeight = 100,
	allowVerticalStacking = true,
	dashboardId,
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

	// Edit mode and hidden items state (per-user, per-dashboard stored in localStorage)
	const [isEditing, setIsEditing] = React.useState<boolean>(false);
	const [hiddenIds, setHiddenIds] = React.useState<Record<string, boolean>>(
		() => ({}),
	);

	// storage is per-browser per-dashboard (no per-user)

	React.useEffect(() => {
		// load hidden ids
		try {
			const key = `lighthouse:dashboard:${dashboardId}:hidden`;
			const raw = localStorage.getItem(key);
			if (raw) {
				const parsed = JSON.parse(raw) as string[];
				const map: Record<string, boolean> = {};
				parsed.forEach((id) => {
					map[String(id)] = true;
				});
				setHiddenIds(map);
			}
		} catch {
			// ignore
		}

		// read edit mode initial value
		try {
			const editKey = `lighthouse:dashboard:${dashboardId}:edit`;
			const v = localStorage.getItem(editKey) === "1";
			setIsEditing(v);
		} catch {
			// ignore
		}

		// subscribe to edit mode events
		const handler = (ev: Event) => {
			const detail = (ev as CustomEvent)?.detail as
				| { dashboardId?: string; userId?: string; isEditing?: boolean }
				| undefined;
			if (!detail) return;
			if (detail.dashboardId === dashboardId) {
				setIsEditing(!!detail.isEditing);
			}
		};

		window.addEventListener(
			"lighthouse:dashboard:edit-mode-changed",
			handler as EventListener,
		);
		return () =>
			window.removeEventListener(
				"lighthouse:dashboard:edit-mode-changed",
				handler as EventListener,
			);
	}, [dashboardId]);

	// overlay colors using centralized color helpers
	// stronger overlay in dark mode using brand light color for better visibility
	const overlayColorHidden = getColorWithOpacity(
		appColors.primary.light,
		theme.palette.mode === "dark" ? 0.56 : 0.14,
	);
	const overlayColorEdit = getColorWithOpacity(
		appColors.primary.main,
		theme.palette.mode === "dark" ? 0.08 : 0.03,
	);
	const overlayBorder =
		theme.palette.mode === "dark"
			? getColorWithOpacity(appColors.primary.light, 0.6)
			: theme.palette.divider;
	const hiddenLabelColor = getContrastText(appColors.primary.light);

	const persistHidden = (nextMap: Record<string, boolean>) => {
		try {
			const key = `lighthouse:dashboard:${dashboardId}:hidden`;
			const arr = Object.keys(nextMap);
			localStorage.setItem(key, JSON.stringify(arr));
		} catch {
			// ignore
		}
	};

	const hideItem = (id: string | number) => {
		const key = String(id);
		const next: Record<string, boolean> = { ...hiddenIds, [key]: true };
		setHiddenIds(next);
		persistHidden(next);
	};

	const showItem = (id: string | number) => {
		const key = String(id);
		const next: Record<string, boolean> = { ...hiddenIds };
		delete next[key];
		setHiddenIds(next);
		persistHidden(next);
	};

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

				// hide item when not editing and it's in hidden list
				const isHidden = hiddenIds[String(key)];
				if (!isEditing && isHidden) return null;

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
								position: "relative",
							}}
						>
							{/* edit controls overlay and interaction blocker */}
							{isEditing && (
								<>
									{/* full-area overlay that captures pointer events to block underlying widget interactions and gives a visual cue */}
									<Box
										aria-hidden
										sx={{
											position: "absolute",
											inset: 0,
											zIndex: 4,
											backgroundColor: isHidden
												? overlayColorHidden
												: overlayColorEdit,
											border: `1px dashed ${overlayBorder}`,
											pointerEvents: "auto",
											display: "flex",
											alignItems: "center",
											justifyContent: "center",
										}}
									>
										{isHidden ? (
											<Box sx={{ pointerEvents: "none", textAlign: "center" }}>
												<Typography
													variant="subtitle2"
													sx={{ color: hiddenLabelColor, fontWeight: 700 }}
												>
													Hidden
												</Typography>
											</Box>
										) : (
											<Box
												sx={{
													position: "absolute",
													left: 6,
													top: 6,
													pointerEvents: "none",
												}}
											/>
										)}
									</Box>

									{/* control buttons sit above the blocker */}
									<Box
										sx={{ position: "absolute", top: 6, right: 6, zIndex: 10 }}
									>
										{isHidden ? (
											<Tooltip title="Show widget">
												<IconButton
													size="small"
													onClick={() => showItem(key)}
													data-testid={`dashboard-item-show-${key}`}
												>
													<VisibilityIcon fontSize="small" />
												</IconButton>
											</Tooltip>
										) : (
											<Tooltip title="Hide widget">
												<IconButton
													size="small"
													onClick={() => hideItem(key)}
													data-testid={`dashboard-item-hide-${key}`}
												>
													<HideSourceIcon fontSize="small" />
												</IconButton>
											</Tooltip>
										)}
									</Box>
								</>
							)}

							{item.node}
						</Box>
					</Box>
				);
			})}
		</Box>
	);
};

export default Dashboard;
