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
import {
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

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

	const calculateDifference = (): number => {
		if (startedTotal === 0 && closedTotal === 0) return 0;
		if (startedTotal === 0) return 100;
		if (closedTotal === 0) return 100;

		const larger = Math.max(startedTotal, closedTotal);
		const smaller = Math.min(startedTotal, closedTotal);
		return ((larger - smaller) / larger) * 100;
	};

	const isAverageDifferenceLessThanOne = (): boolean => {
		return Math.abs(startedAverage - closedAverage) < 1.0;
	};

	const getDifferenceColor = (difference: number): string => {
		if (isAverageDifferenceLessThanOne()) return confidentColor;
		if (difference <= 5) return certainColor;
		if (difference <= 10) return realisticColor;
		if (difference <= 15) return confidentColor;
		return riskyColor;
	};

	const getDifferenceText = (): {
		text: string;
		tip: string;
		color: string;
	} => {
		const difference = calculateDifference();
		const color = getDifferenceColor(difference);
		let tip = "";

		if (isAverageDifferenceLessThanOne() || difference <= 5) {
			tip = "Good job!";
		} else if (difference <= 15) {
			tip = "Observe and take action if needed!";
		} else {
			tip = "Reflect on WIP control!";
		}

		if (isAverageDifferenceLessThanOne() || difference <= 5) {
			return {
				text: "You are keeping a steady WIP",
				tip,
				color,
			};
		}

		if (startedTotal > closedTotal) {
			return {
				text: "You are starting more items than you close",
				tip,
				color,
			};
		}

		return {
			text: "You are closing more items than you start",
			tip,
			color,
		};
	};

	const differenceInfo = getDifferenceText();

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

				<Box
					sx={{
						mt: 1,
						p: 1,
						backgroundColor: `${differenceInfo.color}20`,
						borderLeft: `4px solid ${differenceInfo.color}`,
						borderRadius: 1,
					}}
				>
					<Typography variant="body2" sx={{ fontWeight: "medium" }}>
						{differenceInfo.text}
					</Typography>
					<Typography variant="caption" sx={{ display: "block", mt: 0.5 }}>
						{differenceInfo.tip}
					</Typography>
				</Box>
			</CardContent>
		</Card>
	);
};

export default StartedVsFinishedDisplay;
