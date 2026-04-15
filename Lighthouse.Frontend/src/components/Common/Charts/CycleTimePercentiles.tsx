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
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { ForecastLevel } from "../Forecasts/ForecastLevel";

interface CycleTimePercentilesProps {
	percentileValues: IPercentileValue[];
}

const CycleTimePercentiles: React.FC<CycleTimePercentilesProps> = ({
	percentileValues,
}) => {
	const { getTerm } = useTerminology();
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	const formatDays = (days: number): string => {
		return days === 1 ? `${days.toFixed(0)} day` : `${days.toFixed(0)} days`;
	};

	const getForecastLevel = (percentile: number) => {
		return new ForecastLevel(percentile);
	};

	return (
		<Card
			sx={{
				m: 0,
				p: 0,
				borderRadius: 2,
				height: "100%",
				width: "100%",
				display: "flex",
				flexDirection: "column",
				boxSizing: "border-box",
				overflow: "hidden",
			}}
		>
			<CardContent
				sx={{
					height: "100%",
					display: "flex",
					flexDirection: "column",
					flex: "1 1 auto",
					p: 1,
					boxSizing: "border-box",
					overflow: "hidden",
					minHeight: 0,
				}}
			>
				<Box
					sx={{
						display: "flex",
						justifyContent: "space-between",
						alignItems: "center",
					}}
				>
					<Typography
						variant="h6"
						gutterBottom
						sx={{ minWidth: 0, overflow: "hidden" }}
						noWrap
						style={{ fontSize: "clamp(0.9rem, 1.8vw, 1rem)" }}
					>
						{`${cycleTimeTerm} Percentiles`}
					</Typography>
				</Box>
				{percentileValues.length > 0 ? (
					/* Use a flexed box for the table so it shrinks to available space instead of causing scrolling */
					<Box sx={{ overflow: "hidden", flex: "1 1 auto", minHeight: 0 }}>
						<Table size="small" sx={{ height: "100%", tableLayout: "fixed" }}>
							<TableBody>
								{percentileValues
									.slice()
									.sort((a, b) => b.percentile - a.percentile)
									.map((item) => {
										const forecastLevel = getForecastLevel(item.percentile);
										const IconComponent = forecastLevel.IconComponent;

										return (
											<TableRow key={item.percentile}>
												<TableCell sx={{ border: 0, padding: "2px 0" }}>
													<Typography
														variant="body2"
														sx={{ display: "flex", alignItems: "center" }}
													>
														<IconComponent
															fontSize="small"
															sx={{
																color: forecastLevel.color,
																mr: 1,
																fontSize: "clamp(0.8rem, 1.4vw, 1rem)",
															}}
														/>
														{item.percentile}th
													</Typography>
												</TableCell>
												<TableCell
													align="right"
													sx={{ border: 0, padding: "2px 0" }}
												>
													<Typography
														variant="body1"
														sx={{
															fontWeight: "bold",
															color: forecastLevel.color,
														}}
														style={{
															fontSize: "clamp(0.85rem, 1.8vw, 0.95rem)",
														}}
													>
														{formatDays(item.value)}
													</Typography>
												</TableCell>
											</TableRow>
										);
									})}
							</TableBody>
						</Table>
					</Box>
				) : (
					<Box
						sx={{
							display: "flex",
							alignItems: "center",
							justifyContent: "center",
							flex: "1 1 auto",
						}}
					>
						<Typography variant="body2" color="text.secondary">
							No data available
						</Typography>
					</Box>
				)}
			</CardContent>
		</Card>
	);
};

export default CycleTimePercentiles;
