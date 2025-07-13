import { Box, Typography, useTheme } from "@mui/material";
import {
	BarPlot,
	ChartContainer,
	ChartsReferenceLine,
	ChartsXAxis,
	ChartsYAxis,
} from "@mui/x-charts";
import type React from "react";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import {
	appColors,
	getPredictabilityScoreColor,
} from "../../../utils/theme/colors";

interface PredictabilityScoreProps {
	data: IForecastPredictabilityScore;
	title?: string;
}

const PredictabilityScore: React.FC<PredictabilityScoreProps> = ({
	data,
	title = "Predictability Score",
}) => {
	const theme = useTheme();

	// Get percentile color based on percentile value
	const getPercentileColor = (percentile: number): string => {
		if (percentile === 95) return appColors.forecast.certain;
		if (percentile === 85) return appColors.forecast.confident;
		if (percentile === 70) return appColors.forecast.realistic;
		if (percentile === 50) return appColors.forecast.risky;
		return theme.palette.grey[500];
	};

	const scoreColor = getPredictabilityScoreColor(data.predictabilityScore);
	const hasResults = data.forecastResults.size > 0;

	// Convert Map to chart data
	const chartData = hasResults
		? Array.from(data.forecastResults.entries())
				.sort(([a], [b]) => a - b)
				.map(([key, value]) => ({
					x: key,
					y: value,
				}))
		: [];

	// Get percentile marks for reference lines
	const percentileMarks = hasResults
		? data.percentiles
				.filter((p) => [50, 70, 85, 95].includes(p.percentile))
				.map((p) => ({
					value: p.value,
					probability: p.percentile,
					color: getPercentileColor(p.percentile),
				}))
		: [];

	return (
		<Box
			sx={{
				...(title
					? {
							p: 2,
							border: 1,
							borderColor: theme.palette.divider,
							borderRadius: 2,
							backgroundColor: theme.palette.background.paper,
						}
					: {}),
			}}
		>
			{title && (
				<Typography variant="h6" gutterBottom>
					{title}
				</Typography>
			)}

			{/* Predictability Score Display */}
			<Box sx={{ mb: hasResults ? 2 : 0, textAlign: "center" }}>
				<Box
					sx={{
						display: "flex",
						alignItems: "baseline",
						justifyContent: "center",
						gap: 1,
					}}
				>
					<Typography
						variant="h4"
						sx={{ color: scoreColor, fontWeight: "bold" }}
					>
						{(data.predictabilityScore * 100).toFixed(1)}%
					</Typography>
					<Typography variant="body2" color="text.secondary">
						Predictability Score
					</Typography>
				</Box>
			</Box>

			{/* Bar Chart with Percentiles */}
			{hasResults && (
				<Box sx={{ position: "relative", height: title ? 200 : 400 }}>
					<ChartContainer
						height={title ? 200 : 400}
						margin={{ left: 5, right: 5, top: 35, bottom: 5 }}
						xAxis={[
							{
								id: "forecastAxis",
								scaleType: "band",
								data: chartData.map((d) => d.x.toString()),
								hideTooltip: true,
							},
						]}
						yAxis={[
							{
								id: "countAxis",
								scaleType: "linear",
								min: 0,
								max:
									chartData.length > 0
										? Math.max(...chartData.map((d) => d.y)) * 1.3
										: 10, // Add 30% padding above highest bar
								hideTooltip: true,
							},
						]}
						series={[
							{
								type: "bar",
								data: chartData.map((d) => d.y),
								xAxisId: "forecastAxis",
								yAxisId: "countAxis",
								color: theme.palette.primary.main,
							},
						]}
						sx={{
							"& .MuiChartsAxis-root": {
								display: "none",
							},
							"& .MuiChartsAxis-tickLabel": {
								display: "none",
							},
						}}
					>
						{/* Percentile Reference Lines */}
						{percentileMarks.map((mark) => (
							<ChartsReferenceLine
								key={`percentile-${mark.probability}`}
								x={mark.value.toString()}
								label={`${mark.probability}%`}
								labelAlign="start"
								labelStyle={{
									textAnchor: "middle",
									transform: "translateY(-25px)",
								}}
								lineStyle={{
									stroke: mark.color,
									strokeWidth: 2,
									opacity: 0.8,
								}}
							/>
						))}

						<BarPlot />
						<ChartsXAxis />
						<ChartsYAxis />
					</ChartContainer>
				</Box>
			)}
		</Box>
	);
};

export default PredictabilityScore;
