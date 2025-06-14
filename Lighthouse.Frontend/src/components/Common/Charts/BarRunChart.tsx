import { Box, useTheme } from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import { useState } from "react";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import BaseRunChart from "./BaseRunChart";

interface BarRunChartProps {
	chartData: RunChartData;
	startDate: Date;
	displayTotal?: boolean;
	title?: string;
}

const BarRunChart: React.FC<BarRunChartProps> = ({
	chartData,
	startDate,
	displayTotal = false,
	title = "Bar Chart",
}) => {
	const theme = useTheme();
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
	const [dialogOpen, setDialogOpen] = useState(false);
	const [dialogTitle, setDialogTitle] = useState<string>("");

	const handleBarClick = (dataIndex: number) => {
		const items = chartData.workItemsPerUnitOfTime[dataIndex] || [];
		if (items.length > 0) {
			const day = new Date(startDate);
			day.setDate(day.getDate() + dataIndex);
			const formattedDate = day.toLocaleDateString();
			setDialogTitle(`Items Closed on ${formattedDate}`);
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
				{(data) => (
					<Box sx={{ position: "relative" }}>
						<BarChart
							onAxisClick={(_event, params) =>
								handleBarClick(params?.dataIndex ?? -1)
							}
							onItemClick={(_event, params) =>
								handleBarClick(params?.dataIndex ?? -1)
							}
							dataset={data.map((item, index) => ({
								day: item.day,
								value: item.value,
								index: index,
							}))}
							yAxis={[
								{
									min: 0,
									valueFormatter: (value: number | null) => {
										return value !== null && Number.isInteger(value)
											? value.toString()
											: "";
									},
								},
							]}
							xAxis={[{ scaleType: "band", dataKey: "day" }]}
							series={[
								{
									dataKey: "value",
									color: theme.palette.primary.main,
									valueFormatter: (
										_value: number | null,
										params: { dataIndex: number },
									) => {
										const index = params?.dataIndex ?? 0;
										const numberOfClosedItems =
											chartData.workItemsPerUnitOfTime[index]?.length ?? 0;

										if (numberOfClosedItems === 1) {
											return `${chartData.workItemsPerUnitOfTime[index][0].name} (Click for details)`;
										}

										if (numberOfClosedItems > 0) {
											return `${numberOfClosedItems} Closed Items (Click for details)`;
										}

										return "No Closed Items";
									},
								},
							]}
							height={500}
						/>
					</Box>
				)}
			</BaseRunChart>

			<WorkItemsDialog
				title={dialogTitle}
				items={selectedItems}
				open={dialogOpen}
				onClose={handleCloseDialog}
				timeMetric="cycleTime"
			/>
		</>
	);
};

export default BarRunChart;
