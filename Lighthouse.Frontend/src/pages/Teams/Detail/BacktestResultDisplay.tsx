import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import { styled } from "@mui/system";
import {
	BarPlot,
	ChartContainer,
	ChartsReferenceLine,
	ChartsXAxis,
	ChartsYAxis,
} from "@mui/x-charts";
import type React from "react";
import { useMemo } from "react";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	certainColor,
	confidentColor,
	defaultColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

const ResultHeader = styled(Typography)({
	marginBottom: "8px",
	fontWeight: "bold",
});

const ResultLabel = styled(Typography)({
	marginTop: "12px",
});

/**
 * Computes average-based forecast from historical throughput data.
 * @param historicalThroughput Historical throughput data with total items and days
 * @param backtestResult Backtest configuration with start/end dates
 * @returns Average per day and projected forecast for the backtest period
 */
export const computeAverageForecast = (
	historicalThroughput: RunChartData,
	backtestResult: BacktestResult,
): { avgPerDay: number; avgForecast: number } => {
	const historicalDays = historicalThroughput.history;
	const historicalTotal = historicalThroughput.total;

	// Calculate average throughput per day from historical data
	const avgPerDay = historicalTotal / historicalDays;

	// Calculate the number of days in the backtest period (inclusive)
	const backtestStartDate = new Date(backtestResult.startDate);
	const backtestEndDate = new Date(backtestResult.endDate);
	const backtestDays =
		Math.floor(
			(backtestEndDate.getTime() - backtestStartDate.getTime()) /
				(1000 * 60 * 60 * 24),
		) + 1; // +1 for inclusive

	// Project the average to the backtest period
	const avgForecast = avgPerDay * backtestDays;

	return { avgPerDay, avgForecast };
};

/**
 * Maps percentile probability to forecast palette color.
 * 50% = risky, 70% = realistic, 85% = confident, 95% = certain
 */
const getPercentileColor = (probability: number): string => {
	switch (probability) {
		case 50:
			return riskyColor;
		case 70:
			return realisticColor;
		case 85:
			return confidentColor;
		case 95:
			return certainColor;
		default:
			return defaultColor;
	}
};

interface ChartDataRow {
	[key: string]: string | number;
	label: string;
	value: number;
	color: string;
}

interface BacktestResultDisplayProps {
	backtestResult: BacktestResult;
	historicalThroughput?: RunChartData | null;
}

const BacktestResultDisplay: React.FC<BacktestResultDisplayProps> = ({
	backtestResult,
	historicalThroughput,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	// Compute average forecast if historical data is available
	const averageForecast = useMemo(() => {
		if (!historicalThroughput) return null;
		return computeAverageForecast(historicalThroughput, backtestResult);
	}, [historicalThroughput, backtestResult]);

	// Build display rows for chart: percentiles + average (if available)
	const chartData = useMemo((): ChartDataRow[] => {
		const rows: ChartDataRow[] = backtestResult.percentiles.map((p) => ({
			label: `${p.probability}%`,
			value: p.value,
			color: getPercentileColor(p.probability),
		}));

		// Add Average bar if historical data is available
		if (averageForecast) {
			rows.push({
				label: "Avg",
				value: averageForecast.avgForecast,
				color: defaultColor,
			});
		}

		// Sort by value descending (more items = higher on Y-axis)
		return rows.sort((a, b) => b.value - a.value);
	}, [backtestResult, averageForecast]);

	// Fixed order for right-side summary: 50%, 70%, 85%, 95%, then Actual
	const fixedOrderPercentiles = useMemo(() => {
		const order = [50, 70, 85, 95];
		return order
			.map((prob) =>
				backtestResult.percentiles.find((p) => p.probability === prob),
			)
			.filter(Boolean);
	}, [backtestResult.percentiles]);

	// Calculate max x-axis value to ensure all data (including actual) is visible
	const maxXValue = useMemo(() => {
		const values = [
			...chartData.map((row) => row.value),
			backtestResult.actualThroughput,
		];
		return Math.max(...values) * 1.1; // Add 10% padding
	}, [chartData, backtestResult.actualThroughput]);

	return (
		<Grid container spacing={2}>
			<Grid size={{ xs: 12 }}>
				<ResultHeader variant="h6">Backtest Results</ResultHeader>
				<Typography variant="body2" color="text.secondary">
					Period: {backtestResult.startDate.toLocaleDateString()} to{" "}
					{backtestResult.endDate.toLocaleDateString()} (
					{backtestResult.historicalWindowDays} days of historical data)
				</Typography>
			</Grid>
			<Grid size={{ xs: 12, md: 8 }}>
				<ChartContainer
					dataset={chartData}
					yAxis={[
						{
							scaleType: "band",
							dataKey: "label",
							id: "y-axis",
							colorMap: {
								type: "ordinal",
								values: chartData.map((row) => row.label),
								colors: chartData.map((row) => row.color),
							},
						},
					]}
					xAxis={[
						{
							label: `Number of ${workItemsTerm}`,
							min: 0,
							id: "x-axis",
							max: maxXValue,
						},
					]}
					series={[
						{
							type: "bar",
							dataKey: "value",
							id: "backtest-series",
							yAxisId: "y-axis",
							xAxisId: "x-axis",
							layout: "horizontal",
						},
					]}
					height={450}
				>
					<BarPlot />
					<ChartsXAxis />
					<ChartsYAxis />
					{/* Render Actual as a vertical reference line */}
					<ChartsReferenceLine
						x={backtestResult.actualThroughput}
						label="Actual"
						labelAlign="end"
						lineStyle={{
							stroke: "#000000",
							strokeDasharray: "5 5",
							strokeWidth: 2,
						}}
					/>
				</ChartContainer>
			</Grid>
			<Grid size={{ xs: 12, md: 4 }}>
				<Typography variant="body1" fontWeight="bold" gutterBottom>
					Forecast Percentiles:
				</Typography>
				{fixedOrderPercentiles.map((percentile) => (
					<Typography key={percentile?.probability} variant="body2">
						{percentile?.probability}%: {percentile?.value} {workItemsTerm}
					</Typography>
				))}
				{averageForecast && (
					<>
						<ResultLabel variant="body1" fontWeight="bold">
							Average:
						</ResultLabel>
						<Typography variant="body2">
							{Math.round(averageForecast.avgForecast)} {workItemsTerm} (
							{averageForecast.avgPerDay.toFixed(1)}/day)
						</Typography>
					</>
				)}
				<ResultLabel variant="body1" fontWeight="bold">
					Actual Throughput:
				</ResultLabel>
				<Typography variant="body2">
					{backtestResult.actualThroughput} {workItemsTerm}
				</Typography>
			</Grid>
		</Grid>
	);
};

export default BacktestResultDisplay;
