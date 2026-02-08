import {
	Box,
	Card,
	CardContent,
	Stack,
	Typography,
	useTheme,
} from "@mui/material";
import {
	ChartContainer,
	ChartsReferenceLine,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	LinePlot,
	MarkPlot,
} from "@mui/x-charts";
import type React from "react";
import { useMemo, useState } from "react";
import type {
	ProcessBehaviourChartData,
	SpecialCauseType,
} from "../../../models/Metrics/ProcessBehaviourChartData";
import LegendChip from "./LegendChip";

const specialCauseColors: Record<SpecialCauseType, string> = {
	None: "",
	LargeChange: "#f44336",
	ModerateChange: "#ff9800",
	ModerateShift: "#ff9800",
	SmallShift: "#2196f3",
};

interface ProcessBehaviourChartProps {
	data: ProcessBehaviourChartData;
	title: string;
}

const ProcessBehaviourChart: React.FC<ProcessBehaviourChartProps> = ({
	data,
	title,
}) => {
	const theme = useTheme();

	const [averageVisible, setAverageVisible] = useState(true);
	const [unplVisible, setUnplVisible] = useState(true);
	const [lnplVisible, setLnplVisible] = useState(true);

	const chartData = useMemo(() => {
		if (data.status !== "Ready" || data.dataPoints.length === 0) {
			return {
				xValues: [] as number[],
				yValues: [] as number[],
				colors: [] as string[],
			};
		}

		const xValues = data.dataPoints.map((p) => new Date(p.xValue).getTime());
		const yValues = data.dataPoints.map((p) => p.yValue);
		const colors = data.dataPoints.map((p) =>
			p.specialCause === "None"
				? theme.palette.primary.main
				: specialCauseColors[p.specialCause],
		);

		return { xValues, yValues, colors };
	}, [data, theme.palette.primary.main]);

	const yAxisMax = useMemo(() => {
		if (chartData.yValues.length === 0) {
			return 10;
		}

		const maxValue = Math.max(
			...chartData.yValues,
			data.upperNaturalProcessLimit,
			data.average,
		);

		return maxValue * 1.1;
	}, [chartData.yValues, data.upperNaturalProcessLimit, data.average]);

	if (data.status !== "Ready") {
		return (
			<Card sx={{ p: 2, borderRadius: 2 }}>
				<CardContent>
					<Typography variant="h6">{title}</Typography>
					<Typography variant="body2" color="text.secondary">
						{data.statusReason}
					</Typography>
				</CardContent>
			</Card>
		);
	}

	if (data.dataPoints.length === 0) {
		return (
			<Card sx={{ p: 2, borderRadius: 2 }}>
				<CardContent>
					<Typography variant="h6">{title}</Typography>
					<Typography variant="body2" color="text.secondary">
						No data available
					</Typography>
				</CardContent>
			</Card>
		);
	}

	return (
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Typography variant="h6">{title}</Typography>

				<Stack direction="row" spacing={1} sx={{ mb: 1, flexWrap: "wrap" }}>
					<LegendChip
						label="Average"
						color={theme.palette.success.main}
						visible={averageVisible}
						onToggle={() => setAverageVisible((prev) => !prev)}
					/>
					<LegendChip
						label="UNPL"
						color={theme.palette.error.main}
						visible={unplVisible}
						onToggle={() => setUnplVisible((prev) => !prev)}
					/>
					<LegendChip
						label="LNPL"
						color={theme.palette.warning.main}
						visible={lnplVisible}
						onToggle={() => setLnplVisible((prev) => !prev)}
					/>
				</Stack>

				<Box sx={{ flex: 1, minHeight: 0 }}>
					<ChartContainer
						xAxis={[
							{
								id: "xAxis",
								data: chartData.xValues,
								scaleType: "time",
								valueFormatter: (value: number) =>
									data.xAxisKind === "DateTime"
										? new Date(value).toLocaleString()
										: new Date(value).toLocaleDateString(),
							},
						]}
						yAxis={[
							{
								id: "yAxis",
								min: 0,
								max: yAxisMax,
							},
						]}
						series={[
							{
								type: "line",
								data: chartData.yValues,
								color: theme.palette.primary.main,
							},
						]}
						height={350}
					>
						<ChartsXAxis axisId="xAxis" />
						<ChartsYAxis axisId="yAxis" />
						<LinePlot />
						<MarkPlot />
						<ChartsTooltip />

						{averageVisible && (
							<ChartsReferenceLine
								y={data.average}
								label="Average"
								lineStyle={{
									stroke: theme.palette.success.main,
									strokeWidth: 2,
									strokeDasharray: "5 5",
								}}
								labelStyle={{
									fill: theme.palette.success.main,
								}}
							/>
						)}

						{unplVisible && (
							<ChartsReferenceLine
								y={data.upperNaturalProcessLimit}
								label="UNPL"
								lineStyle={{
									stroke: theme.palette.error.main,
									strokeWidth: 2,
									strokeDasharray: "3 3",
								}}
								labelStyle={{
									fill: theme.palette.error.main,
								}}
							/>
						)}

						{lnplVisible && (
							<ChartsReferenceLine
								y={data.lowerNaturalProcessLimit}
								label="LNPL"
								lineStyle={{
									stroke: theme.palette.warning.main,
									strokeWidth: 2,
									strokeDasharray: "3 3",
								}}
								labelStyle={{
									fill: theme.palette.warning.main,
								}}
							/>
						)}
					</ChartContainer>
				</Box>
			</CardContent>
		</Card>
	);
};

export default ProcessBehaviourChart;
