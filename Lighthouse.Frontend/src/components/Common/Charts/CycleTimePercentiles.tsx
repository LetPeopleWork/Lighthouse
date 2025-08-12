import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import {
	Box,
	Card,
	CardContent,
	Chip,
	IconButton,
	Table,
	TableBody,
	TableCell,
	TableRow,
	Typography,
	useTheme,
} from "@mui/material";
import { useState } from "react";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";

interface CycleTimePercentilesProps {
	percentileValues: IPercentileValue[];
	serviceLevelExpectation?: IPercentileValue | null;
	items: IWorkItem[];
}

const CycleTimePercentiles: React.FC<CycleTimePercentilesProps> = ({
	percentileValues,
	serviceLevelExpectation = null,
	items,
}) => {
	const theme = useTheme();
	const [isFlipped, setIsFlipped] = useState(false);
	const [dialogOpen, setDialogOpen] = useState(false);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const serviceLevelExpectationTerm = getTerm(
		TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION,
	);
	const sleTerm = getTerm(TERMINOLOGY_KEYS.SLE);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	const formatDays = (days: number): string => {
		return days === 1 ? `${days.toFixed(0)} day` : `${days.toFixed(0)} days`;
	};

	const getForecastLevel = (percentile: number) => {
		return new ForecastLevel(percentile);
	};

	const getChipTitleColor = () => {
		if (!serviceLevelExpectation || items.length === 0) return "primary";

		const itemsWithinSLE = items.filter(
			(item) => item.cycleTime <= serviceLevelExpectation.value,
		).length;

		const percentageWithinSLE = (itemsWithinSLE / items.length) * 100;

		if (percentageWithinSLE >= serviceLevelExpectation.percentile) {
			return certainColor;
		}

		const difference = serviceLevelExpectation.percentile - percentageWithinSLE;

		if (difference > 20) {
			return riskyColor;
		}
		if (difference > 10) {
			return realisticColor;
		}
		return confidentColor;
	};

	const calculateSLEStats = () => {
		if (!serviceLevelExpectation || !items || items.length === 0) {
			return {
				totalItems: 0,
				percentageWithinSLE: 0,
			};
		}

		const totalItems = items.length;
		if (totalItems === 0) {
			return {
				totalItems: 0,
				percentageWithinSLE: 0,
			};
		}

		const itemsWithinSLE = items.filter(
			(item) => item.cycleTime <= serviceLevelExpectation.value,
		).length;

		const percentageWithinSLE = (itemsWithinSLE / totalItems) * 100;

		return {
			totalItems,
			percentageWithinSLE,
		};
	};

	const handleFlip = (event: React.MouseEvent) => {
		// Prevent the dialog from opening when flipping the card
		event.stopPropagation();
		setIsFlipped(!isFlipped);
	};

	const handleOpenDialog = () => {
		setDialogOpen(true);
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
	};

	const renderSLEContent = () => {
		if (!serviceLevelExpectation) return null;

		const titleColor = getChipTitleColor();
		const stats = calculateSLEStats();

		return (
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Box
					display="flex"
					justifyContent="space-between"
					alignItems="center"
					mb={2}
				>
					<Typography variant="h6">{serviceLevelExpectationTerm}</Typography>
					<IconButton onClick={(e) => handleFlip(e)} size="small">
						<ArrowBackIcon fontSize="small" />
					</IconButton>
				</Box>

				<Box
					display="flex"
					flexDirection="column"
					alignItems="center"
					justifyContent="center"
				>
					<Typography variant="body2" color="text.secondary" mb={1}>
						Target:
					</Typography>
					<Typography
						variant="body2"
						color="text.secondary"
						align="center"
						sx={{
							fontStyle: "italic",
							fontWeight: "bold",
						}}
					>
						{`${serviceLevelExpectation.percentile}% of all ${workItemsTerm} are done
						within ${formatDays(serviceLevelExpectation.value)} or less`}
					</Typography>

					{stats.totalItems > 0 ? (
						<Box
							display="flex"
							flexDirection="column"
							alignItems="center"
							justifyContent="center"
							mt={2}
						>
							<Typography variant="body2" color="text.secondary" mb={1}>
								Actual:
							</Typography>

							<Typography
								variant="body1"
								fontWeight="bold"
								sx={{ color: titleColor }}
							>
								{`${stats.percentageWithinSLE.toFixed(1)}% of all ${workItemsTerm} completed
								within ${sleTerm} target`}
							</Typography>
						</Box>
					) : (
						<Typography
							variant="body1"
							align="center"
							color="text.secondary"
							mt={4}
						>
							{`No completed ${workItemsTerm} available to analyze`}
						</Typography>
					)}
				</Box>
			</CardContent>
		);
	};

	const renderPercentileContent = () => {
		return (
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Box display="flex" justifyContent="space-between" alignItems="center">
					<Typography variant="h6" gutterBottom>
						{`${cycleTimeTerm} Percentiles`}
					</Typography>
					{serviceLevelExpectation && (
						<Chip
							label={`${sleTerm}: ${serviceLevelExpectation.percentile}% @ ${formatDays(serviceLevelExpectation.value)}`}
							size="small"
							onClick={(e) => handleFlip(e)}
							sx={{
								cursor: "pointer",
								backgroundColor: getChipTitleColor(),
								// Always use white for SLE chip to ensure high contrast
								color: "#ffffff",
								fontWeight: "bold",
								"&:hover": { opacity: 0.9 },
								// Add a subtle elevation for better visibility in dark mode
								boxShadow: theme.customShadows.subtle,
							}}
						/>
					)}
				</Box>
				{percentileValues.length > 0 ? (
					<Table size="small">
						<TableBody>
							{percentileValues
								.slice()
								.sort((a, b) => b.percentile - a.percentile)
								.map((item) => {
									const forecastLevel = getForecastLevel(item.percentile);
									const IconComponent = forecastLevel.IconComponent;

									return (
										<TableRow key={item.percentile}>
											<TableCell sx={{ border: 0, padding: "4px 0" }}>
												<Typography
													variant="body2"
													sx={{ display: "flex", alignItems: "center" }}
												>
													<IconComponent
														fontSize="small"
														sx={{ color: forecastLevel.color, mr: 1 }}
													/>
													{item.percentile}th
												</Typography>
											</TableCell>
											<TableCell
												align="right"
												sx={{ border: 0, padding: "4px 0" }}
											>
												<Typography
													variant="body1"
													fontWeight="bold"
													sx={{ color: forecastLevel.color }}
												>
													{formatDays(item.value)}
												</Typography>
											</TableCell>
										</TableRow>
									);
								})}
						</TableBody>
					</Table>
				) : (
					<Typography variant="body2" color="text.secondary">
						No data available
					</Typography>
				)}
			</CardContent>
		);
	};

	return (
		<>
			<Card
				sx={{
					m: 2,
					p: 1,
					borderRadius: 2,
					cursor: "pointer",
					height: "230px",
					display: "flex",
					flexDirection: "column",
				}}
				onClick={handleOpenDialog}
			>
				{isFlipped ? renderSLEContent() : renderPercentileContent()}
			</Card>

			<WorkItemsDialog
				title={`Closed ${workItemsTerm}`}
				items={items}
				open={dialogOpen}
				onClose={handleCloseDialog}
				additionalColumnTitle={cycleTimeTerm}
				additionalColumnDescription="days"
				additionalColumnContent={(item) => item.cycleTime}
				sle={serviceLevelExpectation?.value}
			/>
		</>
	);
};

export default CycleTimePercentiles;
