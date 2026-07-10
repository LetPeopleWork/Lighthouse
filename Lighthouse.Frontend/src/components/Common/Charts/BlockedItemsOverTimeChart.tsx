import { Box, Card, CardContent, Typography } from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import { useState } from "react";
import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";
import type { IFeature } from "../../../models/Feature";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { errorColor } from "../../../utils/theme/colors";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";

interface BlockedItemsOverTimeChartProps {
	snapshots: BlockedCountSnapshot[] | null;
	metricsService: IMetricsService<IWorkItem | IFeature>;
	ownerId: number;
	title?: string;
}

const EMPTY_MESSAGE =
	"blocked trend builds forward from today — no snapshots yet";

/**
 * Renders the blocked-count-over-time trend in the Flow Metrics chart area.
 * Composes with the existing team/portfolio/date-range filter through the
 * parent component (BaseMetricsView + useMetricsData).
 * Clicking a bar drills into the items blocked at that date (08-03/08-04
 * endpoint) and lists them in the shared WorkItemsDialog.
 * Per-type filtering is NOT wired (UC-2 deferred — ADR-069).
 */
const BlockedItemsOverTimeChart: React.FC<BlockedItemsOverTimeChartProps> = ({
	snapshots,
	metricsService,
	ownerId,
	title = "Blocked Items Over Time",
}) => {
	const [dialogOpen, setDialogOpen] = useState(false);
	const [dialogItems, setDialogItems] = useState<IWorkItem[]>([]);
	const [dialogDate, setDialogDate] = useState("");
	const [dialogNote, setDialogNote] = useState("");

	if (!snapshots || snapshots.length === 0) {
		return (
			<Typography
				variant="body2"
				color="text.secondary"
				sx={{ py: 4, textAlign: "center" }}
			>
				{EMPTY_MESSAGE}
			</Typography>
		);
	}

	const dataset = snapshots.map((s) => ({
		label: s.recordedAt,
		value: s.blockedCount,
	}));

	// Historical membership is reconstructed from blocked transitions, which only reach back to when
	// transition capture began. When fewer items come back than the bar's recorded count, that gap is a
	// capture limitation — surface it honestly instead of showing a bare empty dialog.
	const buildCaptureGapNote = (
		shown: number,
		recordedCount: number,
	): string => {
		if (recordedCount <= shown) {
			return "";
		}
		return shown === 0
			? `${recordedCount} blocked on this date — per-item detail isn't recorded this far back`
			: `showing ${shown} of ${recordedCount} — earlier per-item detail isn't recorded this far back`;
	};

	const openBlockedItemsForDate = async (
		date: string,
		recordedCount: number,
	): Promise<void> => {
		const items = await metricsService.getBlockedItemsAtDate(ownerId, date);
		setDialogItems(items);
		setDialogDate(date);
		setDialogNote(buildCaptureGapNote(items.length, recordedCount));
		setDialogOpen(true);
	};

	return (
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Typography variant="h6">{title}</Typography>
				<Box sx={{ flex: 1, minHeight: 0 }}>
					<BarChart
						style={{ height: "100%", width: "100%" }}
						dataset={dataset}
						onItemClick={(_event, params) => {
							const clicked = dataset[params.dataIndex];
							if (clicked) {
								void openBlockedItemsForDate(clicked.label, clicked.value);
							}
						}}
						yAxis={[
							{
								min: 0,
								valueFormatter: (value: number | null) =>
									value !== null && Number.isInteger(value)
										? value.toString()
										: "",
							},
						]}
						xAxis={[
							{
								scaleType: "band",
								dataKey: "label",
								tickLabelInterval: (_: unknown, index: number) =>
									index % Math.max(1, Math.ceil(dataset.length / 10)) === 0,
							},
						]}
						series={[
							{
								dataKey: "value",
								label: "Blocked",
								color: errorColor,
							},
						]}
					/>
				</Box>
			</CardContent>
			<WorkItemsDialog
				title={
					dialogNote
						? `${title} — ${dialogDate} · ${dialogNote}`
						: `${title} — ${dialogDate}`
				}
				items={dialogItems}
				open={dialogOpen}
				onClose={() => setDialogOpen(false)}
			/>
		</Card>
	);
};

export default BlockedItemsOverTimeChart;
