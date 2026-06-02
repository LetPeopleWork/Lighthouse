import { Card, CardContent, Typography, useTheme } from "@mui/material";
import { ChartsReferenceLine } from "@mui/x-charts";
import { LineChart } from "@mui/x-charts/LineChart";
import type React from "react";
import type {
	DeliveryMetricsHistory,
	DeliveryMetricsHistoryPoint,
} from "../../../models/Delivery/DeliveryMetricsHistory";

interface DeliveryBurnupChartProps {
	history: DeliveryMetricsHistory;
	title?: string;
}

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no snapshots recorded yet.";

const formatDate = (date: Date): string => date.toLocaleDateString();

const buildSeries = (
	points: DeliveryMetricsHistoryPoint[],
	backlogColor: string,
	doneColor: string,
) => [
	{
		label: "Backlog",
		data: points.map((point) => point.totalWork),
		showMark: false,
		color: backlogColor,
	},
	{
		label: "Done",
		data: points.map((point) => point.doneWork),
		area: true,
		showMark: false,
		color: doneColor,
	},
];

const DeliveryBurnupChart: React.FC<DeliveryBurnupChartProps> = ({
	history,
	title = "Delivery Burnup",
}) => {
	const theme = useTheme();

	if (history.points.length === 0) {
		return (
			<Card sx={{ p: 2, borderRadius: 2 }}>
				<CardContent>
					<Typography variant="h6">{title}</Typography>
					<Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
						{FORWARD_ONLY_EMPTY_STATE}
					</Typography>
				</CardContent>
			</Card>
		);
	}

	const dates = history.points.map((point) => point.date);
	const series = buildSeries(
		history.points,
		theme.palette.text.secondary,
		theme.palette.primary.main,
	);

	return (
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Typography variant="h6">{title}</Typography>
				<LineChart
					xAxis={[
						{
							scaleType: "time",
							data: dates,
							label: "Date",
							height: 56,
							valueFormatter: formatDate,
						},
					]}
					series={series}
					height={320}
					slotProps={{
						legend: {
							direction: "horizontal",
							position: { vertical: "top", horizontal: "end" },
						},
					}}
				>
					<ChartsReferenceLine
						x={history.deliveryDate}
						label="Delivery Date"
						lineStyle={{ stroke: theme.palette.error.main }}
					/>
				</LineChart>
			</CardContent>
		</Card>
	);
};

export default DeliveryBurnupChart;
