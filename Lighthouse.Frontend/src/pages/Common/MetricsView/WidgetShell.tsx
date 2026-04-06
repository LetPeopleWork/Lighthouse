import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
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

type RagStatus = "red" | "amber" | "green" | "none";

type WidgetFooter = {
	readonly ragStatus: RagStatus;
	readonly tipText: string;
};

type WidgetInfo = {
	readonly description: string;
	readonly learnMoreUrl: string;
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

const WidgetShell: React.FC<WidgetShellProps> = ({
	title,
	widgetKey,
	showTips = true,
	header,
	info,
	viewData,
	children,
}) => {
	const theme = useTheme();
	const [infoOpen, setInfoOpen] = useState(false);
	const [viewDataOpen, setViewDataOpen] = useState(false);
	const infoAnchorRef = useRef<HTMLButtonElement>(null);

	const hasViewData = !!viewData && viewData.items.length > 0;
	const hasHeader = !!title || (header && showTips) || !!info || hasViewData;

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
