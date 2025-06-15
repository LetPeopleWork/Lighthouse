import { Box, useTheme } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import type React from "react";
import { useState } from "react";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import { getWorkItemName } from "../../../utils/featureName";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import BaseRunChart from "./BaseRunChart";

interface LineRunChartProps {
	chartData: RunChartData;
	startDate: Date;
	title?: string;
	displayTotal?: boolean;
}

const LineRunChart: React.FC<LineRunChartProps> = ({
	chartData,
	startDate,
	title = "Run Chart",
	displayTotal = false,
}) => {
	const theme = useTheme();
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
	const [dialogOpen, setDialogOpen] = useState(false);
	const [dialogTitle, setDialogTitle] = useState<string>("");

	const handleLineClick = (dataIndex: number) => {
		const items = chartData.workItemsPerUnitOfTime[dataIndex] || [];
		if (items.length > 0) {
			const day = new Date(startDate);
			day.setDate(day.getDate() + dataIndex);
			const formattedDate = day.toLocaleDateString();
			setDialogTitle(`Items in Progress on ${formattedDate}`);
			setSelectedItems(items);
			setDialogOpen(true);
		}
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
	};

	return (
		<>
			<BaseRunChart
				chartData={chartData}
				startDate={startDate}
				title={title}
				displayTotal={displayTotal}
			>
				{(data) => {
					const xLabels = data.map((item) => item.day);
					const yValues = data.map((item) => item.value);

					return (
						<Box sx={{ position: "relative" }}>
							<LineChart
								onAxisClick={(_event, params) =>
									handleLineClick(params?.dataIndex ?? -1)
								}
								onLineClick={(_event, params) =>
									handleLineClick(params?.dataIndex ?? -1)
								}
								yAxis={[
									{
										min: 0,
										valueFormatter: (value: number) => {
											return Number.isInteger(value) ? value.toString() : "";
										},
									},
								]}
								xAxis={[
									{
										data: xLabels,
										scaleType: "point",
									},
								]}
								series={[
									{
										data: yValues,
										color: theme.palette.primary.main,
										valueFormatter: (
											_value: number | null,
											params: { dataIndex: number },
										) => {
											const index = params?.dataIndex ?? 0;
											const numberOfItems =
												chartData.workItemsPerUnitOfTime[index]?.length ?? 0;

											if (numberOfItems === 1) {
												const item = chartData.workItemsPerUnitOfTime[index][0];
												return `${getWorkItemName(item)} (Click for details)`;
											}

											if (numberOfItems > 0) {
												return `${numberOfItems} Items in Progress (Click for details)`;
											}

											return "No Items in Progress";
										},
									},
								]}
								height={500}
							/>
						</Box>
					);
				}}
			</BaseRunChart>

			<WorkItemsDialog
				title={dialogTitle}
				items={selectedItems}
				open={dialogOpen}
				onClose={handleCloseDialog}
				timeMetric="ageCycleTime"
			/>
		</>
	);
};

export default LineRunChart;
