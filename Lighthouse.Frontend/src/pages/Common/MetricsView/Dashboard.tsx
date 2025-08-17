import AddIcon from "@mui/icons-material/Add";
import HideSourceIcon from "@mui/icons-material/HideSource";
import RefreshIcon from "@mui/icons-material/Refresh";
import RemoveIcon from "@mui/icons-material/Remove";
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
	dashboardId: string;
}

const Dashboard: React.FC<DashboardProps> = ({
	items,
	spacing = 2,
	baseRowHeight = 100,
	dashboardId,
}) => {
	const theme = useTheme();
	const isXlUp = useMediaQuery(theme.breakpoints.up("xl"));
	const isLgUp = useMediaQuery(theme.breakpoints.up("lg"));
	const isMdUp = useMediaQuery(theme.breakpoints.up("md"));
	const isSmUp = useMediaQuery(theme.breakpoints.up("sm"));

	// More granular responsive column counts
	let columns = 4;
	if (isXlUp) columns = 12;
	else if (isLgUp) columns = 10;
	else if (isMdUp) columns = 8;
	else if (isSmUp) columns = 6;
	const gapPx = spacing * 8;

	// ordering state (persisted per-dashboard)
	const [order, setOrder] = React.useState<string[]>(() => []);
	const draggingIdRef = React.useRef<string | null>(null);
	const [dragOverId, setDragOverId] = React.useState<string | null>(null);

	const rootRef = React.useRef<HTMLElement | null>(null);

	// auto-scroll state for dragging
	const autoScrollRef = React.useRef<number | null>(null);

	// Get responsive size configuration
	const getResponsiveSize = (
		availableColumns: number,
		size: string = "medium",
		key?: string,
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
		const base = {
			colSpan: Math.min(availableColumns, targetSize.colSpan),
			rowSpan: targetSize.rowSpan,
		};

		// apply user overrides if present
		if (key) {
			const override = sizesMap[String(key)];
			if (override) {
				return {
					colSpan: Math.max(1, Math.min(availableColumns, override.colSpan)),
					rowSpan: Math.max(1, Math.min(12, override.rowSpan)),
				};
			}
		}

		return base;
	};

	const persistSizes = (next: Record<string, SizeOverride>) => {
		try {
			const key = `lighthouse:dashboard:${dashboardId}:sizes`;
			localStorage.setItem(key, JSON.stringify(next));
		} catch {
			// ignore
		}
	};

	// persist order to localStorage
	const persistOrder = (next: string[]) => {
		try {
			const key = `lighthouse:dashboard:${dashboardId}:layout`;
			localStorage.setItem(key, JSON.stringify(next));
		} catch {
			// ignore
		}
	};

	React.useEffect(() => {
		const allKeys = items.map((it, idx) => String(it.id ?? idx));
		try {
			const key = `lighthouse:dashboard:${dashboardId}:layout`;
			const raw = localStorage.getItem(key);
			let stored: string[] | null = null;
			if (raw) {
				stored = JSON.parse(raw) as string[];
			}
			let initial: string[] = [];
			if (stored && Array.isArray(stored)) {
				// ensure we only keep keys that still exist
				initial = stored.filter((k) => allKeys.includes(k));
			}
			// append any missing keys (new widgets)
			allKeys.forEach((k) => {
				if (!initial.includes(k)) initial.push(k);
			});
			setOrder(initial);
		} catch {
			setOrder(allKeys);
		}
	}, [dashboardId, items]);

	// Edit mode and hidden items state (per-user, per-dashboard stored in localStorage)
	const [isEditing, setIsEditing] = React.useState<boolean>(false);
	const [hiddenIds, setHiddenIds] = React.useState<Record<string, boolean>>(
		() => ({}),
	);

	// per-widget size overrides stored per-dashboard in localStorage
	type SizeOverride = { colSpan: number; rowSpan: number };
	const [sizesMap, setSizesMap] = React.useState<Record<string, SizeOverride>>(
		() => ({}),
	);

	// FIXED: stable dependencies and proper cleanup
	React.useEffect(() => {
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

		// read sizes overrides
		try {
			const sizesKey = `lighthouse:dashboard:${dashboardId}:sizes`;
			const rawSizes = localStorage.getItem(sizesKey);
			if (rawSizes) {
				const parsed = JSON.parse(rawSizes) as Record<string, SizeOverride>;
				setSizesMap(parsed || {});
			}
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

		// listen for reset-layout event to clear stored settings and reset UI
		const resetHandler = (ev: Event) => {
			const detail = (ev as CustomEvent)?.detail as
				| { dashboardId?: string }
				| undefined;
			if (!detail) return;
			if (detail.dashboardId !== dashboardId) return;

			try {
				const keys = [
					`lighthouse:dashboard:${dashboardId}:layout`,
					`lighthouse:dashboard:${dashboardId}:hidden`,
					`lighthouse:dashboard:${dashboardId}:edit`,
					`lighthouse:dashboard:${dashboardId}:sizes`,
				];
				keys.forEach((k) => localStorage.removeItem(k));
			} catch {
				// ignore
			}

			// reset internal state to defaults - calculate from items directly
			const allKeys = items
				.filter((it) => it.node != null)
				.sort((a, b) => (a.priority ?? 999) - (b.priority ?? 999))
				.map((it, idx) => String(it.id ?? idx));
			setOrder(allKeys);
			setHiddenIds({});
			setIsEditing(false);
			setSizesMap({});
		};

		window.addEventListener(
			"lighthouse:dashboard:edit-mode-changed",
			handler as EventListener,
		);

		window.addEventListener(
			"lighthouse:dashboard:reset-layout",
			resetHandler as EventListener,
		);

		// FIXED: Cleanup both event listeners
		return () => {
			window.removeEventListener(
				"lighthouse:dashboard:edit-mode-changed",
				handler as EventListener,
			);
			window.removeEventListener(
				"lighthouse:dashboard:reset-layout",
				resetHandler as EventListener,
			);
		};
	}, [dashboardId, items]); // Use items instead of visibleItems

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

		// move to end of order so hidden items stay at the bottom
		setOrder((prev) => {
			const without = prev.filter((k) => k !== key);
			const nextOrder = [...without, key];
			persistOrder(nextOrder);
			return nextOrder;
		});
	};

	const showItem = (id: string | number) => {
		const key = String(id);
		const next: Record<string, boolean> = { ...hiddenIds };
		delete next[key];
		setHiddenIds(next);
		persistHidden(next);

		// when showing, move the item into the non-hidden area (before first hidden)
		setOrder((prev) => {
			const without = prev.filter((k) => k !== key);
			// find index of first currently hidden item
			const firstHiddenIndex = without.findIndex((k) => !!next[k]);
			let insertAt = without.length;
			if (firstHiddenIndex >= 0) insertAt = firstHiddenIndex;
			const nextOrder = [
				...without.slice(0, insertAt),
				key,
				...without.slice(insertAt),
			];
			persistOrder(nextOrder);
			return nextOrder;
		});
	};

	// derive ordered list with hidden items forced to the end for rendering and operations
	const { orderedKeys: orderedItems, keyToItem } = React.useMemo(() => {
		// map keys -> item
		const keyToItem: Record<string, DashboardItem> = {};
		items.forEach((it, idx) => {
			const k = String(it.id ?? idx);
			keyToItem[k] = it;
		});

		// start from stored order, keep only existing keys
		const storedOrder = order.filter((k) => keyToItem[k]);

		// any missing keys append
		items.forEach((it, idx) => {
			const k = String(it.id ?? idx);
			if (!storedOrder.includes(k)) storedOrder.push(k);
		});

		// split into non-hidden and hidden
		const nonHidden: string[] = [];
		const hidden: string[] = [];
		storedOrder.forEach((k) => {
			if (hiddenIds[k]) hidden.push(k);
			else nonHidden.push(k);
		});
		return { orderedKeys: [...nonHidden, ...hidden], keyToItem };
	}, [order, items, hiddenIds]);

	// drag handlers
	const onDragStart = (ev: React.DragEvent, id: string) => {
		draggingIdRef.current = id;
		ev.dataTransfer.setData("text/plain", id);
		ev.dataTransfer.effectAllowed = "move";
	};

	const onDragOverItem = (ev: React.DragEvent, id: string) => {
		ev.preventDefault();
		setDragOverId(id);

		// auto-scroll when near viewport top/bottom
		const y = ev.clientY;
		const threshold = 120;
		const scrollAmount = 24;
		if (y < threshold) {
			// scroll up
			if (!autoScrollRef.current) {
				autoScrollRef.current = window.setInterval(() => {
					window.scrollBy({ top: -scrollAmount, left: 0 });
				}, 50);
			}
		} else if (y > window.innerHeight - threshold) {
			// scroll down
			if (!autoScrollRef.current) {
				autoScrollRef.current = window.setInterval(() => {
					window.scrollBy({ top: scrollAmount, left: 0 });
				}, 50);
			}
		} else if (autoScrollRef.current) {
			clearInterval(autoScrollRef.current);
			autoScrollRef.current = null;
		}
	};

	const onDropOnItem = (ev: React.DragEvent, targetId: string) => {
		ev.preventDefault();
		const dragged =
			draggingIdRef.current ?? ev.dataTransfer.getData("text/plain");
		if (!dragged || dragged === targetId) return;
		// don't allow moving hidden widgets or dropping onto hidden widgets area
		if (hiddenIds[dragged]) return;
		if (hiddenIds[targetId]) {
			// drop before the first hidden item (i.e., at boundary)
			const firstHidden = orderedItems.findIndex((k) => hiddenIds[k]);
			const idx = firstHidden >= 0 ? firstHidden : orderedItems.length;
			setOrder((prev) => {
				const without = prev.filter((k) => k !== dragged);
				const nextOrder = [
					...without.slice(0, idx),
					dragged,
					...without.slice(idx),
				];
				persistOrder(nextOrder);
				return nextOrder;
			});
			setDragOverId(null);
			return;
		}

		setOrder((prev) => {
			const without = prev.filter((k) => k !== dragged);
			const targetIndex = without.indexOf(targetId);
			const insertAt = targetIndex >= 0 ? targetIndex : without.length;
			const nextOrder = [
				...without.slice(0, insertAt),
				dragged,
				...without.slice(insertAt),
			];
			persistOrder(nextOrder);
			return nextOrder;
		});
		setDragOverId(null);
	};

	const onDragEnd = () => {
		draggingIdRef.current = null;
		setDragOverId(null);

		if (autoScrollRef.current) {
			clearInterval(autoScrollRef.current);
			autoScrollRef.current = null;
		}
	};

	// drop into empty space / end-area
	const onDropAtEnd = (ev: React.DragEvent) => {
		ev.preventDefault();
		const dragged =
			draggingIdRef.current ?? ev.dataTransfer.getData("text/plain");
		if (!dragged) return;
		if (hiddenIds[dragged]) return;
		// insert before first hidden or at end
		const firstHidden = orderedItems.findIndex((k) => hiddenIds[k]);
		const idx = firstHidden >= 0 ? firstHidden : orderedItems.length;
		setOrder((prev) => {
			const without = prev.filter((k) => k !== dragged);
			const nextOrder = [
				...without.slice(0, idx),
				dragged,
				...without.slice(idx),
			];
			persistOrder(nextOrder);
			return nextOrder;
		});
		setDragOverId(null);
	};

	// leave edit mode when clicking outside or navigating away
	React.useEffect(() => {
		if (!rootRef.current) return;
		const onDocClick = (ev: MouseEvent) => {
			if (!isEditing) return;
			const target = ev.target as Node | null;
			if (!target) return;
			if (!rootRef.current) return;
			if (!rootRef.current.contains(target)) {
				try {
					const editKey = `lighthouse:dashboard:${dashboardId}:edit`;
					localStorage.setItem(editKey, "0");
					window.dispatchEvent(
						new CustomEvent("lighthouse:dashboard:edit-mode-changed", {
							detail: { dashboardId, isEditing: false },
						}),
					);
				} catch {
					// ignore
				}
				setIsEditing(false);
			}
		};

		const onNav = () => {
			if (!isEditing) return;
			try {
				const editKey = `lighthouse:dashboard:${dashboardId}:edit`;
				localStorage.setItem(editKey, "0");
				window.dispatchEvent(
					new CustomEvent("lighthouse:dashboard:edit-mode-changed", {
						detail: { dashboardId, isEditing: false },
					}),
				);
			} catch {
				// ignore
			}
			setIsEditing(false);
		};

		document.addEventListener("click", onDocClick, true);
		window.addEventListener("popstate", onNav);
		window.addEventListener("beforeunload", onNav);

		return () => {
			document.removeEventListener("click", onDocClick, true);
			window.removeEventListener("popstate", onNav);
			window.removeEventListener("beforeunload", onNav);
		};
	}, [isEditing, dashboardId]);

	return (
		<Box
			ref={(el: HTMLElement | null) => {
				rootRef.current = el;
			}}
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
			{orderedItems.map((k) => {
				const item = keyToItem[k];
				if (!item) return null;
				const key = k;

				// Get responsive size (allow per-widget overrides via sizesMap)
				const { colSpan, rowSpan } = getResponsiveSize(columns, item.size, key);

				// hide item when not editing and it's in hidden list
				const isHidden = hiddenIds[String(key)];
				if (!isEditing && isHidden) return null;

				const isDragTarget = dragOverId === key;

				return (
					<Box
						key={String(key)}
						data-testid={`dashboard-item-${key}`}
						data-size={item.size || "medium"}
						data-colspan={colSpan}
						data-rowspan={rowSpan}
						draggable={isEditing && !isHidden}
						onDragStart={(e) => onDragStart(e, key)}
						onDragOver={(e) => onDragOverItem(e, key)}
						onDrop={(e) => onDropOnItem(e, key)}
						onDragEnd={onDragEnd}
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
							outline: isDragTarget
								? `2px dashed ${theme.palette.primary.main}`
								: undefined,
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

									{/* combined control box in upper-right (opaque background) */}
									<Box
										sx={{
											position: "absolute",
											top: 6,
											right: 6,
											zIndex: 12,
											pointerEvents: "auto",
											backgroundColor: theme.palette.background.paper,
											border: `1px solid ${overlayBorder}`,
											borderRadius: 1,
											p: 0.5,
											display: "flex",
											flexDirection: "column",
											gap: 0.5,
											alignItems: "center",
											boxShadow: 1,
										}}
									>
										<Box
											sx={{ display: "flex", alignItems: "center", gap: 0.5 }}
										>
											<Tooltip title="Decrease width">
												<IconButton
													size="small"
													onClick={() => {
														const k = String(key);
														const prev = sizesMap[k] || { colSpan, rowSpan };
														const next = {
															...sizesMap,
															[k]: {
																...prev,
																colSpan: Math.max(1, prev.colSpan - 1),
															},
														};
														setSizesMap(next);
														persistSizes(next);
													}}
													data-testid={`dashboard-item-col-dec-${key}`}
												>
													<RemoveIcon fontSize="small" />
												</IconButton>
											</Tooltip>
											<Typography variant="caption">
												Width: {colSpan}
											</Typography>
											<Tooltip title="Increase width">
												<IconButton
													size="small"
													onClick={() => {
														const k = String(key);
														const prev = sizesMap[k] || { colSpan, rowSpan };
														const next = {
															...sizesMap,
															[k]: {
																...prev,
																colSpan: Math.min(columns, prev.colSpan + 1),
															},
														};
														setSizesMap(next);
														persistSizes(next);
													}}
													data-testid={`dashboard-item-col-inc-${key}`}
												>
													<AddIcon fontSize="small" />
												</IconButton>
											</Tooltip>
										</Box>

										<Box
											sx={{ display: "flex", alignItems: "center", gap: 0.5 }}
										>
											<Tooltip title="Decrease height">
												<IconButton
													size="small"
													onClick={() => {
														const k = String(key);
														const prev = sizesMap[k] || { colSpan, rowSpan };
														const next = {
															...sizesMap,
															[k]: {
																...prev,
																rowSpan: Math.max(1, prev.rowSpan - 1),
															},
														};
														setSizesMap(next);
														persistSizes(next);
													}}
													data-testid={`dashboard-item-row-dec-${key}`}
												>
													<RemoveIcon fontSize="small" />
												</IconButton>
											</Tooltip>
											<Typography variant="caption">
												Height: {rowSpan}
											</Typography>
											<Tooltip title="Increase height">
												<IconButton
													size="small"
													onClick={() => {
														const k = String(key);
														const prev = sizesMap[k] || { colSpan, rowSpan };
														const next = {
															...sizesMap,
															[k]: {
																...prev,
																rowSpan: Math.min(12, prev.rowSpan + 1),
															},
														};
														setSizesMap(next);
														persistSizes(next);
													}}
													data-testid={`dashboard-item-row-inc-${key}`}
												>
													<AddIcon fontSize="small" />
												</IconButton>
											</Tooltip>
										</Box>

										<Box sx={{ display: "flex", gap: 0.5 }}>
											<Tooltip title="Reset size to default">
												<IconButton
													size="small"
													onClick={() => {
														const k = String(key);
														if (!sizesMap[k]) return;
														const next = { ...sizesMap };
														delete next[k];
														setSizesMap(next);
														persistSizes(next);
													}}
													data-testid={`dashboard-item-size-reset-${key}`}
												>
													<RefreshIcon fontSize="small" />
												</IconButton>
											</Tooltip>

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
									</Box>
								</>
							)}

							{item.node}
						</Box>
					</Box>
				);
			})}

			{/* end drop target to allow dropping into empty space */}
			{isEditing && (
				<Box
					data-testid="dashboard-end-drop"
					onDragOver={(e) => {
						e.preventDefault();
						setDragOverId("END");
					}}
					onDrop={onDropAtEnd}
					sx={{
						gridColumn: `1 / span ${columns}`,
						height: 24,
						width: "100%",
						border:
							dragOverId === "END"
								? `2px dashed ${theme.palette.primary.main}`
								: undefined,
						opacity: 0.001, // almost invisible but catch events
					}}
				/>
			)}
		</Box>
	);
};

export default Dashboard;
