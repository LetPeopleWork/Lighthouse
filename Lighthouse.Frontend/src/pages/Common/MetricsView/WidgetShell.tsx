import EastIcon from "@mui/icons-material/East";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import NorthEastIcon from "@mui/icons-material/NorthEast";
import RemoveIcon from "@mui/icons-material/Remove";
import SouthEastIcon from "@mui/icons-material/SouthEast";
import TableChartOutlinedIcon from "@mui/icons-material/TableChartOutlined";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import {
	Box,
	Chip,
	CircularProgress,
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
	type TimeInStateColumnDefinition,
} from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import type { IWorkItem } from "../../../models/WorkItem";
import type { AgeBandColumnDescriptor } from "../../../utils/charts/paceBands";
import type { SleRiskColumnDescriptor } from "../../../utils/charts/sleRisk";
import type { TrendPayload } from "./trendTypes";
import type { WidgetStatusGuidance } from "./widgetInfoMetadata";
import type { WidgetStatus } from "./widgetStatus";

/** What a frame says when its chart's data could not be loaded. */
export const COULD_NOT_LOAD_MESSAGE =
	"This chart couldn't be loaded. Change the dates or reload to try again.";

type RagStatus = "red" | "amber" | "green" | "none";

type WidgetFooter = {
	readonly ragStatus: RagStatus;
	readonly tipText: string;
};

type WidgetInfo = {
	readonly description: string;
	readonly learnMoreUrl: string;
	readonly statusGuidance?: WidgetStatusGuidance;
};

export type ViewDataPayload = {
	readonly title: string;
	readonly items: IWorkItem[];
	readonly highlightColumn?: HighlightColumnDefinition;
	readonly timeInStateColumn?: TimeInStateColumnDefinition;
	readonly sle?: number;
	/** Forwarded to the dialog untouched; the shell never looks inside it. */
	readonly ageBandColumn?: AgeBandColumnDescriptor;
	/** Forwarded the same way, and absent whenever the owner published no target. */
	readonly sleRiskColumn?: SleRiskColumnDescriptor;
};

export interface WidgetShellProps {
	readonly title?: string;
	readonly widgetKey: string;
	readonly showTips?: boolean;
	readonly header?: WidgetFooter;
	readonly info?: WidgetInfo;
	readonly viewData?: ViewDataPayload;
	readonly trend?: TrendPayload;
	readonly status?: WidgetStatus;
	/** Whether an older chart is on screen to dim while the selected window loads. */
	readonly hasContentToDim?: boolean;
	readonly children: React.ReactNode;
}

const LOADING_OPACITY = 0.4;
const SPINNER_SIZE = 24;

type HeaderParts = {
	readonly viewData: boolean;
	readonly trend: boolean;
	readonly rag: boolean;
};

// With no chart on screen, a rating, trend or data list would describe something the reader cannot see.
function headerPartsFor(
	hasChartOnScreen: boolean,
	hasViewData: boolean,
	hasTrend: boolean,
	hasRag: boolean,
): HeaderParts {
	return {
		viewData: hasChartOnScreen && hasViewData,
		trend: hasChartOnScreen && hasTrend,
		rag: hasChartOnScreen && hasRag,
	};
}

const centredSpinner = (
	<CircularProgress
		size={SPINNER_SIZE}
		sx={{
			position: "absolute",
			top: "50%",
			left: "50%",
			marginTop: `${-SPINNER_SIZE / 2}px`,
			marginLeft: `${-SPINNER_SIZE / 2}px`,
		}}
	/>
);

// The pointer is refused by the body box alone: `inert` would also hide the dimmed chart
// from assistive technology, where aria-busy on the frame already says it is loading.
const WidgetBody: React.FC<{
	readonly widgetKey: string;
	readonly status: WidgetStatus;
	readonly hasContentToDim: boolean;
	readonly children: React.ReactNode;
}> = ({ widgetKey, status, hasContentToDim, children }) => {
	const bodyTestId = `widget-shell-body-${widgetKey}`;

	if (status === "error") {
		// No retry: changing the dates or reloading the page asks again. A chart that fetches
		// for itself stays mounted out of sight, so a later answer can still replace this note.
		return (
			<Box
				data-testid={bodyTestId}
				sx={{
					flex: 1,
					minHeight: 0,
					display: "flex",
					alignItems: "center",
					justifyContent: "center",
					gap: 1,
					p: 2,
				}}
			>
				<Box sx={{ display: "none" }}>{children}</Box>
				<WarningAmberIcon color="warning" fontSize="small" />
				<Typography variant="body2" color="text.secondary">
					{COULD_NOT_LOAD_MESSAGE}
				</Typography>
			</Box>
		);
	}

	if (status === "ready") {
		return (
			<Box data-testid={bodyTestId} sx={{ flex: 1, minHeight: 0 }}>
				{children}
			</Box>
		);
	}

	if (hasContentToDim) {
		return (
			<Box sx={{ flex: 1, minHeight: 0, position: "relative" }}>
				<Box
					data-testid={bodyTestId}
					sx={{
						height: "100%",
						opacity: LOADING_OPACITY,
						pointerEvents: "none",
					}}
				>
					{children}
				</Box>
				{centredSpinner}
			</Box>
		);
	}

	// A chart that fetches for itself stays mounted out of sight, so its answer still arrives.
	return (
		<Box
			data-testid={bodyTestId}
			sx={{
				flex: 1,
				minHeight: 0,
				position: "relative",
				pointerEvents: "none",
			}}
		>
			<Box sx={{ display: "none" }}>{children}</Box>
			{centredSpinner}
		</Box>
	);
};

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
			{trend.hintText && (
				<Typography
					variant="caption"
					color="text.secondary"
					sx={{ display: "block", mb: 0.25 }}
				>
					{trend.hintText}
				</Typography>
			)}
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
					data-nobaseline={trend.noBaseline ? "true" : undefined}
					sx={{
						display: "inline-flex",
						alignItems: "center",
						color: "text.secondary",
					}}
				>
					{trend.noBaseline ? (
						<RemoveIcon fontSize="small" />
					) : (
						trendArrowMap[
							trend.direction as Exclude<TrendPayload["direction"], "none">
						]
					)}
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
	status = "ready",
	hasContentToDim = false,
	children,
}) => {
	const theme = useTheme();
	const [infoOpen, setInfoOpen] = useState(false);
	const [viewDataOpen, setViewDataOpen] = useState(false);
	const infoAnchorRef = useRef<HTMLButtonElement>(null);

	const isLoading = status === "loading";
	const hasChartOnScreen = status === "ready" || (isLoading && hasContentToDim);
	const shows = headerPartsFor(
		hasChartOnScreen,
		!!viewData && viewData.items.length > 0,
		!!trend && (trend.direction !== "none" || trend.noBaseline === true),
		!!header && showTips,
	);
	const hasViewData = shows.viewData;
	const hasTrend = shows.trend;
	const hasHeader = !!title || shows.rag || !!info || hasViewData || hasTrend;
	const showInfoGuidance = showTips && !!info?.statusGuidance;

	return (
		<>
			<Box
				data-testid={`widget-shell-${widgetKey}`}
				data-widget-status={status}
				aria-busy={isLoading ? "true" : undefined}
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
															{info.statusGuidance?.[guidanceKey]}
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
								<span>
									<IconButton
										size="small"
										data-testid={`widget-view-data-${widgetKey}`}
										disabled={isLoading}
										onClick={() => setViewDataOpen(true)}
										sx={{ color: theme.palette.text.secondary }}
									>
										<TableChartOutlinedIcon fontSize="small" />
									</IconButton>
								</span>
							</Tooltip>
						)}
						{hasTrend && trend && (
							<TrendChrome widgetKey={widgetKey} trend={trend} />
						)}
						{shows.rag && header && header.ragStatus !== "none" && (
							<Tooltip title={header.tipText} arrow>
								<Chip
									component="span"
									label={
										<span data-testid="rag-status" data-rag={header.ragStatus}>
											{ragLabelMap[header.ragStatus]}
										</span>
									}
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

				<WidgetBody
					widgetKey={widgetKey}
					status={status}
					hasContentToDim={hasContentToDim}
				>
					{children}
				</WidgetBody>
			</Box>

			{hasViewData && viewData && (
				<WorkItemsDialog
					title={viewData.title}
					items={viewData.items}
					open={viewDataOpen}
					onClose={() => setViewDataOpen(false)}
					highlightColumn={viewData.highlightColumn}
					timeInStateColumn={viewData.timeInStateColumn}
					ageBandColumn={viewData.ageBandColumn}
					sleRiskColumn={viewData.sleRiskColumn}
					sle={viewData.sle}
				/>
			)}
		</>
	);
};

export default WidgetShell;
