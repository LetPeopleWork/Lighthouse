import {
	Box,
	Card,
	CardContent,
	Table,
	TableBody,
	TableCell,
	TableRow,
	Typography,
} from "@mui/material";
import { useState } from "react";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";

interface StartedVsFinishedDisplayProps {
	startedItems: RunChartData | null;
	closedItems: RunChartData | null;
}

const StartedVsFinishedDisplay: React.FC<StartedVsFinishedDisplayProps> = ({
	startedItems,
	closedItems,
}) => {
	const [dialogOpen, setDialogOpen] = useState(false);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	const handleOpenDialog = () => {
		setDialogOpen(true);
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
	};

	const getAllWorkItems = () => {
		const items: IWorkItem[] = [];

		if (startedItems) {
			const startedWorkItems = Object.values(
				startedItems.workItemsPerUnitOfTime,
			).flat();
			const notClosedStartedItems = startedWorkItems.filter((startedItem) => {
				return startedItem.closedDate === null;
			});

			items.push(...notClosedStartedItems);
		}

		if (closedItems) {
			const closedWorkItems = Object.values(
				closedItems.workItemsPerUnitOfTime,
			).flat();
			items.push(...closedWorkItems);
		}

		return items;
	};

	const calculateAverage = (data: RunChartData | null): number => {
		if (!data?.history) return 0;
		return data.total / data.history;
	};

	const formatNumber = (value: number): string => {
		return value.toFixed(1);
	};

	const startedTotal = startedItems?.total ?? 0;
	const startedAverage = calculateAverage(startedItems);
	const closedTotal = closedItems?.total ?? 0;
	const closedAverage = calculateAverage(closedItems);

	return (
		<>
			<Card
				sx={{
					m: 0,
					p: 0,
					borderRadius: 2,
					cursor: "pointer",
					height: "100%",
					width: "100%",
					display: "flex",
					flexDirection: "column",
					boxSizing: "border-box",
					overflow: "hidden",
				}}
				onClick={handleOpenDialog}
			>
				<CardContent
					sx={{
						display: "flex",
						flexDirection: "column",
						flex: "1 1 auto",
						justifyContent: "space-between",
						p: 2,
						boxSizing: "border-box",
						overflow: "hidden",
						minHeight: 0, // allow children to shrink inside flex container
					}}
				>
					<Box
						display="flex"
						justifyContent="space-between"
						alignItems="center"
					>
						<Typography
							variant="h6"
							gutterBottom
							style={{ fontSize: "clamp(1rem, 2.2vw, 1.15rem)" }}
						>
							{`Started vs. Closed ${workItemsTerm}`}
						</Typography>
					</Box>
					<Box
						sx={{
							flexGrow: 1,
							display: "flex",
							flexDirection: "column",
							justifyContent: "center",
						}}
					>
						<Table size="small">
							<TableBody>
								<TableRow>
									<TableCell sx={{ border: 0, padding: "4px 0", width: "20%" }}>
										<Typography
											variant="body2"
											sx={{
												cursor: "pointer",
												minWidth: 0,
												overflow: "hidden",
											}}
											noWrap
											onClick={(e) => {
												e.stopPropagation();
												handleOpenDialog();
											}}
											style={{ fontSize: "clamp(0.8rem, 1.8vw, 0.95rem)" }}
										>
											Started:
										</Typography>
									</TableCell>
									<TableCell sx={{ border: 0, padding: "4px 0" }}>
										<Box
											sx={{ display: "flex", justifyContent: "space-between" }}
										>
											<Typography
												variant="body1"
												style={{ fontSize: "clamp(0.9rem, 2.2vw, 1rem)" }}
											>
												<strong>{startedTotal}</strong> items (total)
											</Typography>
											<Typography
												variant="body1"
												style={{ fontSize: "clamp(0.9rem, 2.2vw, 1rem)" }}
											>
												<strong>{formatNumber(startedAverage)}</strong> items
												(per day)
											</Typography>
										</Box>
									</TableCell>
								</TableRow>
								<TableRow>
									<TableCell sx={{ border: 0, padding: "4px 0", width: "20%" }}>
										<Typography
											variant="body2"
											sx={{
												cursor: "pointer",
												minWidth: 0,
												overflow: "hidden",
											}}
											noWrap
											onClick={(e) => {
												e.stopPropagation();
												handleOpenDialog();
											}}
											style={{ fontSize: "clamp(0.8rem, 1.8vw, 0.95rem)" }}
										>
											Closed:
										</Typography>
									</TableCell>
									<TableCell sx={{ border: 0, padding: "4px 0" }}>
										<Box
											sx={{ display: "flex", justifyContent: "space-between" }}
										>
											<Typography
												variant="body1"
												style={{ fontSize: "clamp(0.9rem, 2.2vw, 1rem)" }}
											>
												<strong>{closedTotal}</strong> items (total)
											</Typography>
											<Typography
												variant="body1"
												style={{ fontSize: "clamp(0.9rem, 2.2vw, 1rem)" }}
											>
												<strong>{formatNumber(closedAverage)}</strong> items
												(per day)
											</Typography>
										</Box>
									</TableCell>
								</TableRow>
							</TableBody>
						</Table>
					</Box>
				</CardContent>
			</Card>

			<WorkItemsDialog
				title={`Started and Closed ${workItemsTerm}`}
				items={getAllWorkItems()}
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

export default StartedVsFinishedDisplay;
