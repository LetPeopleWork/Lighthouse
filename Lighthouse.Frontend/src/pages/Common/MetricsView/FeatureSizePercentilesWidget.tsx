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
import type React from "react";
import { ForecastLevel } from "../../../components/Common/Forecasts/ForecastLevel";
import type { IFeatureSizePercentilesInfo } from "../../../models/Metrics/InfoWidgetData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import type { TrendPayload } from "./trendTypes";

interface FeatureSizePercentilesWidgetProps {
	readonly data: IFeatureSizePercentilesInfo;
}

const FeatureSizePercentilesWidget: React.FC<FeatureSizePercentilesWidgetProps> & {
	getTrendPayload: (data: IFeatureSizePercentilesInfo) => {
		trendPayload: TrendPayload;
	};
} = ({ data }) => {
	const { getTerm } = useTerminology();
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const formatItems = (value: number): string =>
		value === 1 ? `${value} ${workItemTerm}` : `${value} ${workItemsTerm}`;

	const getForecastLevel = (percentile: number) =>
		new ForecastLevel(percentile);

	return (
		<Card sx={{ borderRadius: 2, height: "100%", width: "100%" }}>
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
						Feature Size Percentiles
					</Typography>
				</Box>

				{data.percentiles.length === 0 ? (
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
				) : (
					<Box sx={{ overflow: "hidden", flex: "1 1 auto", minHeight: 0 }}>
						<Table size="small" sx={{ height: "100%", tableLayout: "fixed" }}>
							<TableBody>
								{data.percentiles
									.slice()
									.sort((a, b) => b.percentile - a.percentile)
									.map((p) => {
										const forecastLevel = getForecastLevel(p.percentile);
										const IconComponent = forecastLevel.IconComponent;
										return (
											<TableRow key={p.percentile}>
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
														{p.percentile}th
													</Typography>
												</TableCell>
												<TableCell
													align="right"
													sx={{ border: 0, padding: "2px 0" }}
													data-testid={`percentile-row-${p.percentile}`}
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
														{formatItems(p.value)}
													</Typography>
												</TableCell>
											</TableRow>
										);
									})}
							</TableBody>
						</Table>
					</Box>
				)}
			</CardContent>
		</Card>
	);
};

FeatureSizePercentilesWidget.getTrendPayload = (
	data: IFeatureSizePercentilesInfo,
): { trendPayload: TrendPayload } => ({
	trendPayload: data.comparison,
});

export default FeatureSizePercentilesWidget;
