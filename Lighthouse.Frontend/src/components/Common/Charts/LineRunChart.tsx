import { Box, Chip, useTheme } from "@mui/material";
import { ChartsReferenceLine, LineChart } from "@mui/x-charts";
import type React from "react";
import { useState } from "react";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";
import { hexToRgba } from "../../../utils/theme/colors";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import BaseRunChart from "./BaseRunChart";

interface LineRunChartProps {
	chartData: RunChartData;
	startDate: Date;
	title?: string;
	displayTotal?: boolean;
	wipLimit?: number;
}

const LineRunChart: React.FC<LineRunChartProps> = ({
	chartData,
	startDate,
	title = "Run Chart",
	displayTotal = false,
	wipLimit,
}) => {
	const theme = useTheme();
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
	const [dialogOpen, setDialogOpen] = useState(false);
	const [dialogTitle, setDialogTitle] = useState<string>("");
	const [wipLimitVisible, setWipLimitVisible] = useState<boolean>(
		!!wipLimit && wipLimit >= 1,
	);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const wipTerm = getTerm(TERMINOLOGY_KEYS.WIP);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	const handleLineClick = (dataIndex: number) => {
		const items = chartData.workItemsPerUnitOfTime[dataIndex] || [];
		if (items.length > 0) {
			const day = new Date(startDate);
			// Use UTC methods to avoid timezone issues
			day.setUTCDate(day.getUTCDate() + dataIndex);
			const formattedDate = day.toLocaleDateString();
			setDialogTitle(`${workItemsTerm} in Progress on ${formattedDate}`);
			setSelectedItems(items);
			setDialogOpen(true);
		}
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
	};

	const toggleWipLimitVisibility = () => {
		setWipLimitVisible((prev) => !prev);
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
						<Box
							sx={{
								position: "relative",
								display: "flex",
								flexDirection: "column",
								height: "100%",
							}}
						>
							{/* Legend below title, above chart, right-aligned */}
							{wipLimit !== undefined && wipLimit >= 1 && (
								<Box
									sx={{ display: "flex", justifyContent: "flex-start", mb: 2 }}
								>
									<Chip
										label={`System ${wipTerm} Limit`}
										onClick={toggleWipLimitVisibility}
										sx={{
											borderColor: theme.palette.secondary.main,
											borderWidth: wipLimitVisible ? 2 : 1,
											borderStyle: "dashed",
											opacity: wipLimitVisible ? 1 : 0.7,
											backgroundColor: wipLimitVisible
												? hexToRgba(
														theme.palette.secondary.main,
														theme.opacity.medium,
													)
												: "transparent",
											"&:hover": {
												borderColor: theme.palette.secondary.main,
												borderWidth: 2,
												backgroundColor: hexToRgba(
													theme.palette.secondary.main,
													theme.opacity.high,
												),
											},
											cursor: "pointer",
										}}
										variant={wipLimitVisible ? "filled" : "outlined"}
									/>
								</Box>
							)}
							<Box sx={{ position: "relative", flex: 1, minHeight: 0 }}>
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
													const item =
														chartData.workItemsPerUnitOfTime[index][0];
													return `${getWorkItemName(item)} (Click for details)`;
												}

												if (numberOfItems > 0) {
													return `${numberOfItems} ${workItemsTerm} in Progress (Click for details)`;
												}

												return `No ${workItemsTerm} in Progress`;
											},
										},
									]}
									// height controlled by parent; allow flexible sizing
								>
									{wipLimitVisible &&
										wipLimit !== undefined &&
										wipLimit >= 1 && (
											<ChartsReferenceLine
												y={wipLimit}
												labelAlign="start"
												lineStyle={{
													stroke: theme.palette.secondary.main,
													strokeWidth: 2,
													strokeDasharray: "3 3",
												}}
											/>
										)}
								</LineChart>
							</Box>
						</Box>
					);
				}}
			</BaseRunChart>

			<WorkItemsDialog
				title={dialogTitle}
				items={selectedItems}
				open={dialogOpen}
				onClose={handleCloseDialog}
				highlightColumn={{
					title: `${workItemAgeTerm}/${cycleTimeTerm}`,
					description: "days",
					valueGetter: (item) =>
						item.cycleTime > 0 ? item.cycleTime : item.workItemAge,
				}}
			/>
		</>
	);
};

export default LineRunChart;
