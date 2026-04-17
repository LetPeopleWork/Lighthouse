import { Box, useTheme } from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import { useState } from "react";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import BaseRunChart from "./BaseRunChart";
import BlackoutOverlay from "./BlackoutOverlay";

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

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	const handleBarClick = (dataIndex: number) => {
		const items = chartData.workItemsPerUnitOfTime[dataIndex] || [];
		if (items.length > 0) {
			const day = new Date(startDate);
			// Use UTC methods to avoid timezone issues
			day.setUTCDate(day.getUTCDate() + dataIndex);
			const formattedDate = day.toLocaleDateString();
			setDialogTitle(`${workItemsTerm} on ${formattedDate}`);
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
					<Box
						sx={{
							position: "relative",
							height: "100%",
							display: "flex",
							flexDirection: "column",
						}}
					>
						<Box sx={{ flex: 1, minHeight: 0 }}>
							<BarChart
								style={{ height: "100%", width: "100%" }}
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
								xAxis={[
									{
										scaleType: "band",
										dataKey: "day",
									},
								]}
								series={[
									{
										dataKey: "value",
										color: theme.palette.primary.main,
										valueFormatter: (
											_value: number | null,
											params: { dataIndex: number },
										) => {
											const index = params?.dataIndex ?? 0;
											const isBlackout = data[index]?.isBlackout ?? false;
											const numberOfClosedItems =
												chartData.workItemsPerUnitOfTime[index]?.length ?? 0;

											const suffix = isBlackout ? " (Blackout Day)" : "";

											if (numberOfClosedItems === 1) {
												const item = chartData.workItemsPerUnitOfTime[index][0];
												return `${getWorkItemName(item.name, item.referenceId)} (Click for details)${suffix}`;
											}

											if (numberOfClosedItems > 0) {
												return `${numberOfClosedItems} ${workItemsTerm} (Click for details)${suffix}`;
											}

											return `No ${workItemsTerm}${suffix}`;
										},
									},
								]}
								// height is controlled by parent card/grid; allow flexible sizing
							>
								<BlackoutOverlay
									blackoutDayLabels={data
										.filter((item) => item.isBlackout)
										.map((item) => item.day)}
								/>
							</BarChart>
						</Box>
					</Box>
				)}
			</BaseRunChart>

			<WorkItemsDialog
				title={dialogTitle}
				items={selectedItems}
				open={dialogOpen}
				onClose={handleCloseDialog}
				highlightColumn={{
					title: cycleTimeTerm,
					description: "days",
					valueGetter: (item) => item.cycleTime,
				}}
			/>
		</>
	);
};

export default BarRunChart;
