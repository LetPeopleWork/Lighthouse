import { Card, CardContent, Typography, useTheme } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import type React from "react";
import type {
	DeliveryMetricsHistory,
	DeliveryMetricsHistoryPoint,
} from "../../../models/Delivery/DeliveryMetricsHistory";

interface DeliveryBurnupChartProps {
	history: DeliveryMetricsHistory;
	title?: string;
	height?: number;
}

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no snapshots recorded yet.";

const ESTIMATED_SERIES_ID = "estimated";
const ESTIMATED_SERIES_LABEL = "Estimated (not broken down)";
const ESTIMATED_DASH_PATTERN = "2 4";
const ESTIMATED_LINE_SX = {
	[`& .MuiLineChart-line[data-series="${ESTIMATED_SERIES_ID}"]`]: {
		strokeDasharray: ESTIMATED_DASH_PATTERN,
		strokeWidth: 2,
	},
};

const formatDate = (date: Date): string => date.toLocaleDateString();

const estimatedValue = (point: DeliveryMetricsHistoryPoint): number | null =>
	point.estimatedItemCount && point.estimatedItemCount > 0
		? point.estimatedItemCount
		: null;

const latestEstimatedPoint = (
	points: DeliveryMetricsHistoryPoint[],
): DeliveryMetricsHistoryPoint | undefined => {
	const estimated = points.filter((point) => estimatedValue(point) !== null);
	return estimated[estimated.length - 1];
};

interface BurnupColors {
	backlog: string;
	done: string;
	estimated: string;
}

const buildSeries = (
	points: DeliveryMetricsHistoryPoint[],
	colors: BurnupColors,
) => {
	const series: Array<{
		id?: string;
		label: string;
		data: Array<number | null>;
		showMark: boolean;
		color: string;
		area?: boolean;
	}> = [
		{
			label: "Backlog",
			data: points.map((point) => point.totalWork),
			showMark: false,
			color: colors.backlog,
		},
		{
			label: "Done",
			data: points.map((point) => point.doneWork),
			area: true,
			showMark: false,
			color: colors.done,
		},
	];

	if (latestEstimatedPoint(points) !== undefined) {
		series.push({
			id: ESTIMATED_SERIES_ID,
			label: ESTIMATED_SERIES_LABEL,
			data: points.map(estimatedValue),
			showMark: false,
			color: colors.estimated,
		});
	}

	return series;
};

const DeliveryBurnupChart: React.FC<DeliveryBurnupChartProps> = ({
	history,
	title = "Delivery Burnup",
	height = 320,
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
	const series = buildSeries(history.points, {
		backlog: theme.palette.text.secondary,
		done: theme.palette.primary.main,
		estimated: theme.palette.warning.main,
	});

	return (
		<Card
			data-testid="delivery-burnup-chart"
			sx={{ p: 2, borderRadius: 2, height: "100%" }}
		>
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
					height={height}
					sx={ESTIMATED_LINE_SX}
					slotProps={{
						legend: {
							direction: "horizontal",
							position: { vertical: "top", horizontal: "end" },
						},
					}}
				/>
			</CardContent>
		</Card>
	);
};

export default DeliveryBurnupChart;
