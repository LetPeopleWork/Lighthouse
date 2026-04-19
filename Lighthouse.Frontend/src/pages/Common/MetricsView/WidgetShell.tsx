import EastIcon from "@mui/icons-material/East";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import NorthEastIcon from "@mui/icons-material/NorthEast";
import SouthEastIcon from "@mui/icons-material/SouthEast";
import TableChartOutlinedIcon from "@mui/icons-material/TableChartOutlined";
import {
	Box,
	Chip,
	IconButton,
	Link,
	Popover,
	Tooltip,
	Typography,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useRef, useState } from "react";
import WorkItemsDialog, {
	type HighlightColumnDefinition,
} from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import type { IWorkItem } from "../../../models/WorkItem";
import type { TrendPayload } from "./trendTypes";
import type { WidgetStatusGuidance } from "./widgetInfoMetadata";

type RagStatus = "red" | "amber" | "green" | "none";

type WidgetFooter = {
	readonly ragStatus: RagStatus;
	readonly tipText: string;
};

type WidgetInfo = {
	readonly description: string;
	readonly learnMoreUrl: string;
	readonly statusGuidance: WidgetStatusGuidance;
};

export type ViewDataPayload = {
	readonly title: string;
	readonly items: IWorkItem[];
	readonly highlightColumn?: HighlightColumnDefinition;
	readonly sle?: number;
};

export interface WidgetShellProps {
	readonly title?: string;
	readonly widgetKey: string;
	readonly showTips?: boolean;
	readonly header?: WidgetFooter;
	readonly info?: WidgetInfo;
	readonly viewData?: ViewDataPayload;
	readonly trend?: TrendPayload;
	readonly children: React.ReactNode;
}

const ragColorMap: Record<Exclude<RagStatus, "none">, string> = {
	red: "#d32f2f",
	amber: "#ed6c02",
	green: "#2e7d32",
};

const ragLabelMap: Record<Exclude<RagStatus, "none">, string> = {
	red: "Act",
	amber: "Observe",
	green: "Sustain",
};

const infoGuidanceOrder: ReadonlyArray<{
	readonly guidanceKey: keyof WidgetStatusGuidance;
	readonly ragStatus: Exclude<RagStatus, "none">;
}> = [
	{ guidanceKey: "sustain", ragStatus: "green" },
	{ guidanceKey: "observe", ragStatus: "amber" },
	{ guidanceKey: "act", ragStatus: "red" },
];

const trendArrowMap: Record<
	Exclude<TrendPayload["direction"], "none">,
	React.ReactNode
> = {
	up: <NorthEastIcon fontSize="small" />,
	down: <SouthEastIcon fontSize="small" />,
	flat: <EastIcon fontSize="small" />,
};

function buildTrendTooltipContent(trend: TrendPayload): React.ReactNode {
	return (
		<Box sx={{ p: 0.5, minWidth: 160 }}>
			<Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
				{trend.metricLabel}
			</Typography>
			{trend.currentLabel && (
				<Box sx={{ display: "flex", justifyContent: "space-between", gap: 2 }}>
					<Typography
						variant="caption"
						color="text.secondary"
						sx={{ fontWeight: 600 }}
					>
						{trend.currentLabel}
					</Typography>
					<Typography variant="caption" sx={{ fontWeight: 600 }}>
						{trend.currentValue}
					</Typography>
				</Box>
			)}
			{trend.previousLabel && (
				<Box sx={{ display: "flex", justifyContent: "space-between", gap: 2 }}>
					<Typography variant="caption" color="text.secondary">
						{trend.previousLabel}
					</Typography>
					<Typography variant="caption" color="text.secondary">
						{trend.previousValue}
					</Typography>
				</Box>
			)}
			{trend.percentageDelta && (
				<Typography
					variant="caption"
					color="text.secondary"
					sx={{ display: "block", mt: 0.25 }}
				>
					{trend.percentageDelta}
				</Typography>
			)}
			{trend.detailRows && trend.detailRows.length > 0 && (
				<Box sx={{ mt: 0.5 }}>
					{trend.detailRows.map((row) => (
						<Box
							key={row.label}
							sx={{
								display: "flex",
								justifyContent: "space-between",
								gap: 2,
							}}
						>
							<Typography variant="caption" color="text.secondary">
								{row.label}
							</Typography>
							<Typography variant="caption">
								{row.previousValue} → <strong>{row.currentValue}</strong>
							</Typography>
						</Box>
					))}
				</Box>
			)}
		</Box>
	);
}

const TrendChrome: React.FC<{
	widgetKey: string;
	trend: TrendPayload;
}> = ({ widgetKey, trend }) => {
	return (
		<Tooltip title={buildTrendTooltipContent(trend)} arrow>
			<Box
				data-testid={`widget-trend-${widgetKey}`}
				sx={{
					display: "inline-flex",
					alignItems: "center",
					cursor: "default",
				}}
			>
				<Box
					data-testid={`widget-trend-arrow-${widgetKey}`}
					sx={{
						display: "inline-flex",
						alignItems: "center",
						color: "text.secondary",
					}}
				>
					{
						trendArrowMap[
							trend.direction as Exclude<TrendPayload["direction"], "none">
						]
					}
				</Box>
			</Box>
		</Tooltip>
	);
};

const WidgetShell: React.FC<WidgetShellProps> = ({
	title,
	widgetKey,
	showTips = true,
	header,
	info,
	viewData,
	trend,
	children,
}) => {
	const theme = useTheme();
	const [infoOpen, setInfoOpen] = useState(false);
	const [viewDataOpen, setViewDataOpen] = useState(false);
	const infoAnchorRef = useRef<HTMLButtonElement>(null);

	const hasViewData = !!viewData && viewData.items.length > 0;
	const hasTrend = !!trend && trend.direction !== "none";
	const hasHeader =
		!!title || (header && showTips) || !!info || hasViewData || hasTrend;
	const showInfoGuidance = showTips && !!info?.statusGuidance;

	return (
		<>
			<Box
				data-testid={`widget-shell-${widgetKey}`}
				sx={{
					width: "100%",
					height: "100%",
					display: "flex",
					flexDirection: "column",
				}}
			>
				{hasHeader && (
					<Box
						data-testid={`widget-shell-header-${widgetKey}`}
						sx={{
							display: "flex",
							alignItems: "center",
							gap: 1,
							pb: 0.5,
						}}
					>
						{info && (
							<>
								<IconButton
									size="small"
									data-testid={`widget-info-${widgetKey}`}
									ref={infoAnchorRef}
									onClick={() => setInfoOpen((prev) => !prev)}
									sx={{ color: theme.palette.text.secondary }}
								>
									<InfoOutlinedIcon fontSize="small" />
								</IconButton>
								<Popover
									open={infoOpen}
									anchorEl={infoAnchorRef.current}
									onClose={() => setInfoOpen(false)}
									anchorOrigin={{
										vertical: "bottom",
										horizontal: "right",
									}}
									transformOrigin={{
										vertical: "top",
										horizontal: "right",
									}}
								>
									<Box sx={{ p: 2, maxWidth: 300 }}>
										<Typography variant="body2" sx={{ mb: 1 }}>
											{info.description}
										</Typography>
										{showInfoGuidance && (
											<Box
												sx={{
													display: "flex",
													flexDirection: "column",
													gap: 0.75,
													mb: 1,
												}}
											>
												{infoGuidanceOrder.map(({ guidanceKey, ragStatus }) => (
													<Box
														key={guidanceKey}
														sx={{
															display: "flex",
															alignItems: "flex-start",
															gap: 0.75,
														}}
													>
														<Chip
															label={ragLabelMap[ragStatus]}
															size="small"
															sx={{
																backgroundColor: ragColorMap[ragStatus],
																color: "#fff",
																fontWeight: 600,
																fontSize: "0.65rem",
																height: 20,
																minWidth: 68,
															}}
														/>
														<Typography
															variant="caption"
															color="text.secondary"
															sx={{ lineHeight: 1.45 }}
														>
															{info.statusGuidance[guidanceKey]}
														</Typography>
													</Box>
												))}
											</Box>
										)}
										<Link
											href={info.learnMoreUrl}
											target="_blank"
											rel="noopener noreferrer"
											variant="body2"
										>
											Learn More
										</Link>
									</Box>
								</Popover>
							</>
						)}
						{hasViewData && (
							<Tooltip title="View Data" arrow>
								<IconButton
									size="small"
									data-testid={`widget-view-data-${widgetKey}`}
									onClick={() => setViewDataOpen(true)}
									sx={{ color: theme.palette.text.secondary }}
								>
									<TableChartOutlinedIcon fontSize="small" />
								</IconButton>
							</Tooltip>
						)}
						{hasTrend && <TrendChrome widgetKey={widgetKey} trend={trend} />}
						{header && showTips && header.ragStatus !== "none" && (
							<Tooltip title={header.tipText} arrow>
								<Chip
									label={ragLabelMap[header.ragStatus]}
									size="small"
									data-testid={`widget-rag-${widgetKey}`}
									sx={{
										backgroundColor: ragColorMap[header.ragStatus],
										color: "#fff",
										fontWeight: 600,
										fontSize: "0.65rem",
										height: 20,
									}}
								/>
							</Tooltip>
						)}

						{!title && <Box sx={{ flex: 1 }} />}
						{title && (
							<Typography
								variant="subtitle2"
								color="text.primary"
								sx={{ flex: 1 }}
							>
								{title}
							</Typography>
						)}
					</Box>
				)}

				<Box sx={{ flex: 1, minHeight: 0 }}>{children}</Box>
			</Box>

			{hasViewData && (
				<WorkItemsDialog
					title={viewData.title}
					items={viewData.items}
					open={viewDataOpen}
					onClose={() => setViewDataOpen(false)}
					highlightColumn={viewData.highlightColumn}
					sle={viewData.sle}
				/>
			)}
		</>
	);
};

export default WidgetShell;
