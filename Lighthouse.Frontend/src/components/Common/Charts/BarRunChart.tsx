import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import {
	Box,
	Card,
	CardContent,
	Chip,
	IconButton,
	Typography,
	useTheme,
} from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import { useState } from "react";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";
import { getPredictabilityScoreColor } from "../../../utils/theme/colors";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import BaseRunChart from "./BaseRunChart";
import PredictabilityScore from "./PredictabilityScore";

interface BarRunChartProps {
	chartData: RunChartData;
	startDate: Date;
	displayTotal?: boolean;
	title?: string;
	predictabilityData?: IForecastPredictabilityScore | null;
}

const BarRunChart: React.FC<BarRunChartProps> = ({
	chartData,
	startDate,
	displayTotal = false,
	title = "Bar Chart",
	predictabilityData = null,
}) => {
	const theme = useTheme();
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
	const [dialogOpen, setDialogOpen] = useState(false);
	const [dialogTitle, setDialogTitle] = useState<string>("");
	const [isFlipped, setIsFlipped] = useState(false);

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
			setDialogTitle(`${workItemsTerm} Closed on ${formattedDate}`);
			setSelectedItems(items);
			setDialogOpen(true);
		}
	};

	const handleFlip = (event: React.MouseEvent) => {
		// Prevent the dialog from opening when flipping the card
		event.stopPropagation();
		setIsFlipped(!isFlipped);
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
	};

	const renderPredictabilityContent = () => {
		if (!predictabilityData) return null;

		return (
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent
					sx={{ height: "100%", display: "flex", flexDirection: "column" }}
				>
					<Box
						display="flex"
						justifyContent="space-between"
						alignItems="center"
						mb={2}
					>
						<Typography variant="h6">Predictability Score</Typography>
						<IconButton onClick={handleFlip} size="small">
							<ArrowBackIcon fontSize="small" />
						</IconButton>
					</Box>
					<Box sx={{ flex: 1, width: "100%" }}>
						<PredictabilityScore data={predictabilityData} title="" />
					</Box>
				</CardContent>
			</Card>
		);
	};

	const renderBarChartContent = () => {
		return (
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
						{/* Predictability Chip */}
						{predictabilityData && (
							<Chip
								label={`Predictability Score: ${(predictabilityData.predictabilityScore * 100).toFixed(1)}%`}
								size="small"
								onClick={handleFlip}
								sx={{
									position: "absolute",
									top: -24,
									right: 8,
									zIndex: 1,
									cursor: "pointer",
									backgroundColor: getPredictabilityScoreColor(
										predictabilityData.predictabilityScore,
									),
									color: "#ffffff",
									fontWeight: "bold",
									"&:hover": { opacity: 0.9 },
									boxShadow:
										theme.customShadows?.subtle || "0 2px 4px rgba(0,0,0,0.1)",
								}}
							/>
						)}
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
											const numberOfClosedItems =
												chartData.workItemsPerUnitOfTime[index]?.length ?? 0;

											if (numberOfClosedItems === 1) {
												const item = chartData.workItemsPerUnitOfTime[index][0];
												return `${getWorkItemName(item)} (Click for details)`;
											}

											if (numberOfClosedItems > 0) {
												return `${numberOfClosedItems} Closed ${workItemsTerm} (Click for details)`;
											}

											return `No Closed ${workItemsTerm}`;
										},
									},
								]}
								// height is controlled by parent card/grid; allow flexible sizing
							/>
						</Box>
					</Box>
				)}
			</BaseRunChart>
		);
	};

	return (
		<>
			{isFlipped ? renderPredictabilityContent() : renderBarChartContent()}

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
