import CloseIcon from "@mui/icons-material/Close";
import OpenInFullIcon from "@mui/icons-material/OpenInFull";
import {
	Box,
	IconButton,
	Modal,
	Tooltip,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import React from "react";
import { getColorWithOpacity } from "../../../utils/theme/colors";

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
}

const Dashboard: React.FC<DashboardProps> = ({
	items,
	spacing = 2,
	baseRowHeight = 100,
}) => {
	const theme = useTheme();
	const isXlUp = useMediaQuery(theme.breakpoints.up("xl"));
	const isLgUp = useMediaQuery(theme.breakpoints.up("lg"));
	const isMdUp = useMediaQuery(theme.breakpoints.up("md"));
	const isSmUp = useMediaQuery(theme.breakpoints.up("sm"));

	let columns = 4;
	if (isXlUp) columns = 12;
	else if (isLgUp) columns = 10;
	else if (isMdUp) columns = 8;
	else if (isSmUp) columns = 6;
	const gapPx = spacing * 8;

	const [spotlightId, setSpotlightId] = React.useState<string | null>(null);

	const getResponsiveSize = (
		availableColumns: number,
		size: string = "medium",
	) => {
		const sizeConfigs = {
			small: {
				xl: { colSpan: 3, rowSpan: 2 },
				lg: { colSpan: 2, rowSpan: 2 },
				md: { colSpan: 2, rowSpan: 2 },
				sm: { colSpan: 3, rowSpan: 2 },
				xs: { colSpan: 4, rowSpan: 2 },
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

		let currentBreakpoint: keyof typeof sizeConfig;
		if (isXlUp) currentBreakpoint = "xl";
		else if (isLgUp) currentBreakpoint = "lg";
		else if (isMdUp) currentBreakpoint = "md";
		else if (isSmUp) currentBreakpoint = "sm";
		else currentBreakpoint = "xs";

		const targetSize = sizeConfig[currentBreakpoint];

		return {
			colSpan: Math.min(availableColumns, targetSize.colSpan),
			rowSpan: targetSize.rowSpan,
		};
	};

	// Build a key→item lookup for spotlight rendering
	const keyToItem = React.useMemo(() => {
		const map: Record<string, DashboardItem> = {};
		for (const [idx, item] of items.entries()) {
			map[String(item.id ?? idx)] = item;
		}
		return map;
	}, [items]);

	// Handle Escape key to close spotlight mode
	React.useEffect(() => {
		const handleEscape = (ev: KeyboardEvent) => {
			if (ev.key === "Escape" && spotlightId !== null) {
				setSpotlightId(null);
			}
		};
		globalThis.addEventListener("keydown", handleEscape);
		return () => {
			globalThis.removeEventListener("keydown", handleEscape);
		};
	}, [spotlightId]);

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
				px: isSmUp ? 0 : 1,
			}}
		>
			{items.map((item, idx) => {
				if (item.node == null) return null;

				const key = String(item.id ?? idx);
				const { colSpan, rowSpan } = getResponsiveSize(columns, item.size);

				return (
					<Box
						key={key}
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
								p: 1,
								position: "relative",
								"&:hover .spotlight-button": {
									opacity: 1,
								},
							}}
						>
							<Box
								className="spotlight-button"
								sx={{
									position: "absolute",
									top: 10,
									right: 10,
									zIndex: 5,
									pointerEvents: "auto",
									opacity: 0,
									transition: "opacity 0.2s ease-in-out",
								}}
							>
								<Tooltip title="Expand">
									<IconButton
										size="small"
										onClick={(e) => {
											e.stopPropagation();
											setSpotlightId(key);
										}}
										data-testid={`dashboard-item-spotlight-${key}`}
										sx={{
											padding: "3px",
											backgroundColor: getColorWithOpacity(
												theme.palette.background.paper,
												0.85,
											),
											border: "none",
											borderRadius: "4px",
											boxShadow: "none",
											color: theme.palette.text.secondary,
											"&:hover": {
												backgroundColor: theme.palette.action.hover,
												color: theme.palette.text.primary,
											},
										}}
									>
										<OpenInFullIcon sx={{ fontSize: 14 }} />
									</IconButton>
								</Tooltip>
							</Box>

							{item.node}
						</Box>
					</Box>
				);
			})}

			{/* Spotlight Modal */}
			<Modal
				open={spotlightId !== null}
				onClose={() => setSpotlightId(null)}
				slotProps={{
					backdrop: {
						sx: {
							backgroundColor: getColorWithOpacity(
								"#000",
								theme.palette.mode === "dark" ? 0.8 : 0.7,
							),
						},
					},
				}}
				data-testid="dashboard-spotlight-modal"
			>
				<Box
					sx={{
						position: "absolute",
						top: "50%",
						left: "50%",
						transform: "translate(-50%, -50%)",
						width: "95vw",
						height: "95vh",
						maxWidth: "1800px",
						maxHeight: "1200px",
						backgroundColor: theme.palette.background.paper,
						borderRadius: 2,
						boxShadow: 24,
						p: 3,
						overflow: "auto",
						display: "flex",
						flexDirection: "column",
						outline: "none",
						opacity: spotlightId === null ? 0 : 1,
						transition: "opacity 300ms cubic-bezier(0.4, 0, 0.2, 1)",
					}}
				>
					<Box
						sx={{
							position: "absolute",
							top: 12,
							right: 12,
							zIndex: 1,
						}}
					>
						<Tooltip title="Close (Esc)">
							<IconButton
								onClick={() => setSpotlightId(null)}
								data-testid="dashboard-spotlight-close"
								sx={{
									backgroundColor: theme.palette.background.paper,
									border: `1px solid ${theme.palette.divider}`,
									"&:hover": {
										backgroundColor: theme.palette.action.hover,
									},
								}}
							>
								<CloseIcon />
							</IconButton>
						</Tooltip>
					</Box>

					<Box
						sx={{
							flex: 1,
							overflow: "auto",
							display: "flex",
							flexDirection: "column",
							pt: 2,
						}}
					>
						{spotlightId !== null && keyToItem[spotlightId]?.node}
					</Box>
				</Box>
			</Modal>
		</Box>
	);
};

export default Dashboard;
