import { Card, CardContent, Typography, useTheme } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import type React from "react";
import type {
	DeliveryMetricsHistory,
	DeliveryMetricsHistoryPoint,
} from "../../../models/Delivery/DeliveryMetricsHistory";
import {
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

interface DeliveryPredictabilityChartProps {
	history: DeliveryMetricsHistory;
	title?: string;
}

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no snapshots recorded yet.";

const LIKELIHOOD_SERIES_ID = "likelihood";
const LIKELIHOOD_SERIES_LABEL = "Likelihood";

const RAG_THRESHOLDS = [50, 70, 85];
const RAG_BAND_COLORS = [
	riskyColor,
	realisticColor,
	confidentColor,
	certainColor,
];

const formatDate = (date: Date): string => date.toLocaleDateString();

const formatPercentage = (value: number | null): string =>
	value === null ? "" : `${value}%`;

const hasLikelihood = (point: DeliveryMetricsHistoryPoint): boolean =>
	point.likelihoodPercentage !== null;

const DeliveryPredictabilityChart: React.FC<
	DeliveryPredictabilityChartProps
> = ({ history, title = "Delivery Predictability" }) => {
	const theme = useTheme();

	const points = history.points;
	const hasAnyLikelihood = points.some(hasLikelihood);

	if (!hasAnyLikelihood) {
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

	const dates = points.map((point) => point.date);
	const likelihoodData = points.map((point) => point.likelihoodPercentage);

	return (
		<Card
			data-testid="delivery-predictability-chart"
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
					yAxis={[
						{
							min: 0,
							max: 100,
							label: "Likelihood (%)",
							colorMap: {
								type: "piecewise",
								thresholds: RAG_THRESHOLDS,
								colors: RAG_BAND_COLORS,
							},
						},
					]}
					series={[
						{
							id: LIKELIHOOD_SERIES_ID,
							label: LIKELIHOOD_SERIES_LABEL,
							data: likelihoodData,
							showMark: true,
							color: theme.palette.text.secondary,
							valueFormatter: formatPercentage,
						},
					]}
					height={320}
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

export default DeliveryPredictabilityChart;
