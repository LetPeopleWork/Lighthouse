import { useTheme } from "@mui/material";
import { LineChart } from "@mui/x-charts";
import type React from "react";
import { useMemo, useState } from "react";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import BaseRunChart from "./BaseRunChart";

interface TotalWorkItemAgeRunChartProps {
	wipOverTimeData: RunChartData;
	startDate: Date;
	title?: string;
}

/**
 * Calculate the historical age of a work item on a specific date
 * Age = days between startedDate and the historical date + 1
 * (An item started today has age 1, not 0)
 *
 * This matches the backend calculation: ((end.Date - start.Date).TotalDays) + 1
 * We use UTC date-only comparison to avoid timezone issues.
 */
const calculateHistoricalAge = (
	item: IWorkItem,
	historicalDate: Date,
): number => {
	const started = new Date(item.startedDate);
	const historical = new Date(historicalDate);

	// Extract date-only components in UTC (matching backend's .Date property behavior)
	const startDateOnly = Date.UTC(
		started.getUTCFullYear(),
		started.getUTCMonth(),
		started.getUTCDate(),
	);
	const endDateOnly = Date.UTC(
		historical.getUTCFullYear(),
		historical.getUTCMonth(),
		historical.getUTCDate(),
	);

	// Calculate difference in days
	const diffMs = endDateOnly - startDateOnly;
	const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

	// Add 1 to match backend logic (item started today has age 1, not 0)
	return Math.max(1, diffDays + 1);
};

const TotalWorkItemAgeRunChart: React.FC<TotalWorkItemAgeRunChartProps> = ({
	wipOverTimeData,
	startDate,
	title = "Total Work Item Age Over Time",
}) => {
	const theme = useTheme();
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
	const [dialogOpen, setDialogOpen] = useState(false);
	const [dialogTitle, setDialogTitle] = useState<string>("");
	const [selectedDate, setSelectedDate] = useState<Date | null>(null);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);

	// Calculate total age for each day
	const chartData = useMemo(() => {
		const data: Array<{ day: string; value: number; itemCount: number }> = [];

		for (let dayIndex = 0; dayIndex < wipOverTimeData.history; dayIndex++) {
			const items = wipOverTimeData.workItemsPerUnitOfTime[dayIndex] || [];

			// Create historical date using UTC to avoid timezone issues
			// The startDate comes from the backend as UTC
			const historicalDate = new Date(startDate);
			historicalDate.setUTCDate(historicalDate.getUTCDate() + dayIndex);

			// Calculate total age for all items on this day
			const totalAge = items.reduce((sum, item) => {
				const age = calculateHistoricalAge(item, historicalDate);
				return sum + age;
			}, 0);

			// Format date for display
			const dayLabel = historicalDate.toLocaleDateString(undefined, {
				month: "short",
				day: "numeric",
			});

			data.push({
				day: dayLabel,
				value: totalAge,
				itemCount: items.length,
			});
		}

		return data;
	}, [wipOverTimeData, startDate]);

	const handleLineClick = (dataIndex: number) => {
		const items = wipOverTimeData.workItemsPerUnitOfTime[dataIndex] || [];
		if (items.length > 0) {
			// Use UTC methods to match calculation logic
			const day = new Date(startDate);
			day.setUTCDate(day.getUTCDate() + dataIndex);
			setSelectedDate(day);
			const formattedDate = day.toLocaleDateString();
			setDialogTitle(
				`${workItemsTerm} Contributing to Total Age on ${formattedDate}`,
			);
			setSelectedItems(items);
			setDialogOpen(true);
		}
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
		setSelectedDate(null);
	};

	return (
		<>
			<BaseRunChart
				chartData={wipOverTimeData}
				startDate={startDate}
				title={title}
				displayTotal={false}
			>
				{() => {
					const xLabels = chartData.map((item) => item.day);
					const yValues = chartData.map((item) => item.value);

					return (
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
									label: `Total ${workItemAgeTerm} (days)`,
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
										value: number | null,
										params: { dataIndex: number },
									) => {
										const index = params?.dataIndex ?? 0;
										const dataPoint = chartData[index];

										if (!dataPoint || value === null) {
											return "No data";
										}

										const { value: totalAge, itemCount } = dataPoint;

										if (itemCount === 0) {
											return "No items in progress";
										}

										if (itemCount === 1) {
											return `${totalAge} days total (1 item) - Click for details`;
										}

										return `${totalAge} days total (${itemCount} items) - Click for details`;
									},
								},
							]}
						/>
					);
				}}
			</BaseRunChart>

			<WorkItemsDialog
				title={dialogTitle}
				items={selectedItems}
				open={dialogOpen}
				onClose={handleCloseDialog}
				additionalColumnTitle={workItemAgeTerm}
				additionalColumnDescription="days"
				additionalColumnContent={(item) => {
					// Calculate historical age for the selected date
					if (selectedDate) {
						return calculateHistoricalAge(item, selectedDate);
					}
					// Fallback to current age if no date selected
					return item.workItemAge;
				}}
			/>
		</>
	);
};

export default TotalWorkItemAgeRunChart;
