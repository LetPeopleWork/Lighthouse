import { useTheme } from "@mui/material";
import {
	BarPlot,
	ChartContainer,
	ChartsXAxis,
	ChartsYAxis,
	LinePlot,
	MarkPlot,
} from "@mui/x-charts";
import type React from "react";
import type { RefreshLog } from "../../../models/SystemInfo/RefreshLog";
import { appColors } from "../../../utils/theme/colors";

interface RefreshHistoryChartProps {
	data: RefreshLog[];
}

const RefreshHistoryChart: React.FC<RefreshHistoryChartProps> = ({ data }) => {
	const theme = useTheme();
	const sorted = [...data].sort(
		(a, b) =>
			new Date(a.executedAt).getTime() - new Date(b.executedAt).getTime(),
	);

	const dataset = sorted.map((entry) => ({
		label: new Date(entry.executedAt).toLocaleString(),
		itemCount: entry.itemCount,
		durationS: entry.durationMs / 1000,
	}));

	return (
		<ChartContainer
			dataset={dataset}
			xAxis={[{ scaleType: "band", dataKey: "label" }]}
			yAxis={[
				{ id: "items", position: "left" },
				{ id: "duration", position: "right" },
			]}
			series={[
				{
					type: "bar",
					dataKey: "itemCount",
					label: "Items",
					yAxisId: "items",
					color: theme.palette.primary.main,
				},
				{
					type: "line",
					dataKey: "durationS",
					label: "Duration (s)",
					yAxisId: "duration",
					color: appColors.status.warning,
				},
			]}
			height={300}
			margin={{ left: 60, right: 80, top: 20, bottom: 80 }}
		>
			<BarPlot />
			<LinePlot />
			<MarkPlot />
			<ChartsXAxis />
			<ChartsYAxis axisId="items" label="Items" />
			<ChartsYAxis axisId="duration" label="Duration (s)" />
		</ChartContainer>
	);
};

export default RefreshHistoryChart;
