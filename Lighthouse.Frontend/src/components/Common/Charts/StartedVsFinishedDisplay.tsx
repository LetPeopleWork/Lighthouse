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
import type { RunChartData } from "../../../models/Metrics/RunChartData";

interface StartedVsFinishedDisplayProps {
	startedItems: RunChartData | null;
	closedItems: RunChartData | null;
}

const StartedVsFinishedDisplay: React.FC<StartedVsFinishedDisplayProps> = ({
	startedItems,
	closedItems,
}) => {
	const calculateAverage = (data: RunChartData | null): number => {
		if (!data?.valuePerUnitOfTime?.length) return 0;
		return data.total / data.valuePerUnitOfTime.length;
	};

	const formatNumber = (value: number): string => {
		return value.toFixed(1);
	};

	const startedTotal = startedItems?.total ?? 0;
	const startedAverage = calculateAverage(startedItems);
	const closedTotal = closedItems?.total ?? 0;
	const closedAverage = calculateAverage(closedItems);

	return (
		<Card sx={{ m: 2, p: 1, borderRadius: 2, cursor: "pointer" }}>
			<CardContent>
				<Typography variant="h6" gutterBottom>
					Started vs. Closed Items
				</Typography>
				<Table size="small">
					<TableBody>
						<TableRow>
							<TableCell sx={{ border: 0, padding: "4px 0", width: "20%" }}>
								<Typography variant="body2">Started:</Typography>
							</TableCell>
							<TableCell sx={{ border: 0, padding: "4px 0" }}>
								<Box sx={{ display: "flex", justifyContent: "space-between" }}>
									<Typography variant="body1">
										<strong>{startedTotal}</strong> items (total)
									</Typography>
									<Typography variant="body1">
										<strong>{formatNumber(startedAverage)}</strong> items (per
										day)
									</Typography>
								</Box>
							</TableCell>
						</TableRow>
						<TableRow>
							<TableCell sx={{ border: 0, padding: "4px 0", width: "20%" }}>
								<Typography variant="body2">Closed:</Typography>
							</TableCell>
							<TableCell sx={{ border: 0, padding: "4px 0" }}>
								<Box sx={{ display: "flex", justifyContent: "space-between" }}>
									<Typography variant="body1">
										<strong>{closedTotal}</strong> items (total)
									</Typography>
									<Typography variant="body1">
										<strong>{formatNumber(closedAverage)}</strong> items (per
										day)
									</Typography>
								</Box>
							</TableCell>
						</TableRow>
					</TableBody>
				</Table>
			</CardContent>
		</Card>
	);
};

export default StartedVsFinishedDisplay;
