import {
	Box,
	Card,
	CardContent,
	FormControl,
	MenuItem,
	Select,
	Table,
	TableBody,
	TableCell,
	TableRow,
	Typography,
} from "@mui/material";
import { useEffect } from "react";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { ForecastLevel } from "../Forecasts/ForecastLevel";

const DEFAULT_SCOPE = "default";

interface CycleTimePercentilesProps {
	percentileValues: IPercentileValue[];
	namedCycleTimeDefinitions?: INamedCycleTimeDefinition[];
	scopeDefinitionId?: number | null;
	onScopeChange?: (definitionId: number | null) => void;
}

const CycleTimePercentiles: React.FC<CycleTimePercentilesProps> = ({
	percentileValues,
	namedCycleTimeDefinitions = [],
	scopeDefinitionId = null,
	onScopeChange,
}) => {
	const { getTerm } = useTerminology();
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	const hasSelector = namedCycleTimeDefinitions.length > 0;

	useEffect(() => {
		if (scopeDefinitionId === null || onScopeChange === undefined) {
			return;
		}
		const selected = namedCycleTimeDefinitions.find(
			(definition) => definition.id === scopeDefinitionId,
		);
		if (!selected || selected.isValid === false) {
			onScopeChange(null);
		}
	}, [scopeDefinitionId, namedCycleTimeDefinitions, onScopeChange]);

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
						gap: 1,
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
					{hasSelector && (
						<FormControl size="small" sx={{ minWidth: 140, flexShrink: 0 }}>
							<Select
								/* The Overview widget is only a few rows tall - a default-height
								   small Select eats a whole percentile row, so trim its padding. */
								sx={{
									fontSize: "clamp(0.75rem, 1.4vw, 0.85rem)",
									"& .MuiSelect-select": { py: 0.25 },
								}}
								value={
									scopeDefinitionId === null
										? DEFAULT_SCOPE
										: String(scopeDefinitionId)
								}
								onChange={(event) => {
									const value = event.target.value;
									onScopeChange?.(
										value === DEFAULT_SCOPE ? null : Number(value),
									);
								}}
								SelectDisplayProps={{ "aria-label": "Cycle time scope" }}
							>
								<MenuItem value={DEFAULT_SCOPE}>Default</MenuItem>
								{namedCycleTimeDefinitions.map((definition) => (
									<MenuItem
										key={definition.id}
										value={String(definition.id)}
										disabled={definition.isValid === false}
									>
										{definition.isValid === false
											? `${definition.name} (invalid — fix its states)`
											: definition.name}
									</MenuItem>
								))}
							</Select>
						</FormControl>
					)}
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
