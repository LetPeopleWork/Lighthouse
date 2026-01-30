import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import { styled } from "@mui/system";
import { BarChart } from "@mui/x-charts/BarChart";
import type React from "react";
import { useMemo } from "react";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
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
}

const BacktestResultDisplay: React.FC<BacktestResultDisplayProps> = ({
	backtestResult,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	// Build display rows for chart: percentiles + actual, each with color
	const chartData = useMemo((): ChartDataRow[] => {
		const rows: ChartDataRow[] = backtestResult.percentiles.map((p) => ({
			label: `${p.probability}%`,
			value: p.value,
			color: getPercentileColor(p.probability),
		}));

		// Add Actual with neutral color
		rows.push({
			label: "Actual",
			value: backtestResult.actualThroughput,
			color: defaultColor,
		});

		// Sort by value descending (more items = higher on Y-axis)
		// Since BarChart renders Y-axis top-to-bottom, we reverse so largest is at top
		return rows.sort((a, b) => b.value - a.value);
	}, [backtestResult]);

	// Fixed order for right-side summary: 50%, 70%, 85%, 95%, then Actual
	const fixedOrderPercentiles = useMemo(() => {
		const order = [50, 70, 85, 95];
		return order
			.map((prob) =>
				backtestResult.percentiles.find((p) => p.probability === prob),
			)
			.filter(Boolean);
	}, [backtestResult.percentiles]);

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
				<BarChart
					dataset={chartData}
					yAxis={[
						{
							scaleType: "band",
							dataKey: "label",
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
						},
					]}
					series={[
						{
							dataKey: "value",
						},
					]}
					layout="horizontal"
					height={300}
					margin={{ left: 60 }}
				/>
			</Grid>
			<Grid size={{ xs: 12, md: 4 }}>
				<Typography variant="body1" gutterBottom>
					<strong>Forecast Percentiles:</strong>
				</Typography>
				{fixedOrderPercentiles.map((percentile) => (
					<Typography key={percentile?.probability} variant="body2">
						{percentile?.probability}%: {percentile?.value} {workItemsTerm}
					</Typography>
				))}
				<Typography variant="body1" sx={{ mt: 2 }}>
					<strong>Actual Throughput:</strong> {backtestResult.actualThroughput}{" "}
					{workItemsTerm}
				</Typography>
			</Grid>
		</Grid>
	);
};

export default BacktestResultDisplay;
