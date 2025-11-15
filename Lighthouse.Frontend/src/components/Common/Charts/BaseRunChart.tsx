import { Card, CardContent, Typography } from "@mui/material";
import type React from "react";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

export interface ChartDataItem {
	day: string;
	value: number;
}

export interface BaseRunChartProps {
	chartData: RunChartData;
	startDate: Date;
	title?: string;
	displayTotal?: boolean;
	children: (data: ChartDataItem[]) => React.ReactNode;
}

const BaseRunChart: React.FC<BaseRunChartProps> = ({
	chartData,
	startDate,
	title = "Chart",
	displayTotal = false,
	children,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const data = Array.from({ length: chartData.history }, (_, index) => {
		const targetDate = new Date(startDate);
		// Use UTC methods to avoid timezone issues with dates from backend
		targetDate.setUTCDate(targetDate.getUTCDate() + index);

		return {
			day: targetDate.toLocaleDateString(),
			value: chartData.getValueOnDay(index),
		};
	});

	return chartData?.history > 0 ? (
		// Make the card fill the available height so chart children can grow
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Typography variant="h6">{title}</Typography>
				{displayTotal && (
					<Typography variant="h6">
						Total: {chartData.total} {workItemsTerm}
					</Typography>
				)}
				{/* Ensure the chart area grows to fill the CardContent */}
				<div style={{ flex: 1, minHeight: 0 }}>{children(data)}</div>
			</CardContent>
		</Card>
	) : (
		<Typography variant="body2" color="text.secondary">
			No data available
		</Typography>
	);
};

export default BaseRunChart;
