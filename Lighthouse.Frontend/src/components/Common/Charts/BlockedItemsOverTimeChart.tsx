import { Box, Card, CardContent, Typography } from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";

interface BlockedItemsOverTimeChartProps {
	snapshots: BlockedCountSnapshot[] | null;
	title?: string;
}

const EMPTY_MESSAGE =
	"blocked trend builds forward from today — no snapshots yet";

/**
 * Renders the blocked-count-over-time trend in the Flow Metrics chart area.
 * Composes with the existing team/portfolio/date-range filter through the
 * parent component (BaseMetricsView + useMetricsData).
 * Per-type filtering is NOT wired (UC-2 deferred — ADR-069).
 */
const BlockedItemsOverTimeChart: React.FC<BlockedItemsOverTimeChartProps> = ({
	snapshots,
	title = "Blocked Items Over Time",
}) => {
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
							},
						]}
					/>
				</Box>
			</CardContent>
		</Card>
	);
};

export default BlockedItemsOverTimeChart;
