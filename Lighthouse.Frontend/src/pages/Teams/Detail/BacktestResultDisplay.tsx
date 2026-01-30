import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import { styled } from "@mui/system";
import { BarChart } from "@mui/x-charts/BarChart";
import type React from "react";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";

const ResultHeader = styled(Typography)({
	marginBottom: "8px",
	fontWeight: "bold",
});

interface BacktestResultDisplayProps {
	backtestResult: BacktestResult;
}

const BacktestResultDisplay: React.FC<BacktestResultDisplayProps> = ({
	backtestResult,
}) => {
	// Prepare data for the chart
	// X-axis: percentile labels, plus actual throughput
	const percentileLabels = backtestResult.percentiles.map(
		(p) => `${p.probability}%`,
	);
	const percentileValues = backtestResult.percentiles.map((p) => p.value);

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
					xAxis={[{ scaleType: "band", data: [...percentileLabels, "Actual"] }]}
					series={[
						{
							data: percentileValues,
							label: "Forecast Percentiles",
							color: "#1976d2",
						},
						{
							data: [
								...percentileValues.map(() => null),
								backtestResult.actualThroughput,
							],
							label: "Actual",
							color: "#2e7d32",
						},
					]}
					height={300}
				/>
			</Grid>
			<Grid size={{ xs: 12, md: 4 }}>
				<Typography variant="body1" gutterBottom>
					<strong>Forecast Percentiles:</strong>
				</Typography>
				{backtestResult.percentiles.map((percentile) => (
					<Typography key={percentile.probability} variant="body2">
						{percentile.probability}%: {percentile.value} items
					</Typography>
				))}
				<Typography variant="body1" sx={{ mt: 2 }}>
					<strong>Actual Throughput:</strong> {backtestResult.actualThroughput}{" "}
					items
				</Typography>
			</Grid>
		</Grid>
	);
};

export default BacktestResultDisplay;
