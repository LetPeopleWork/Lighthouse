import DeleteIcon from "@mui/icons-material/Delete";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
	Accordion,
	AccordionDetails,
	AccordionSummary,
	Box,
	Chip,
	IconButton,
	Paper,
	Typography,
	useTheme,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../../../components/Common/DataGrid/types";
import FeatureName from "../../../../../components/Common/FeatureName/FeatureName";
import ForecastLikelihood from "../../../../../components/Common/Forecasts/ForecastLikelihood";
import ProgressIndicator from "../../../../../components/Common/ProgressIndicator/ProgressIndicator";
import type { Delivery } from "../../../../../models/Delivery";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../../services/TerminologyContext";

interface DeliverySectionProps {
	delivery: Delivery;
	features: IFeature[];
	isExpanded: boolean;
	isLoadingFeatures: boolean;
	onToggleExpanded: (deliveryId: number) => void;
	onDelete: (delivery: Delivery) => void;
}

const DeliverySection: React.FC<DeliverySectionProps> = ({
	delivery,
	features,
	isExpanded,
	isLoadingFeatures,
	onToggleExpanded,
	onDelete,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);

	// Define feature grid columns (adapted from PortfolioFeatureList)
	const columns: DataGridColumn<IFeature & GridValidRowModel>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: `${featureTerm} Name`,
				hideable: false,
				width: 300,
				flex: 1,
				renderCell: ({ row }) => (
					<FeatureName
						name={row.name}
						url={row.url ?? ""}
						stateCategory={row.stateCategory}
						isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
						teamsWorkIngOnFeature={[]} // TODO: Add team data if needed
					/>
				),
			},
			{
				field: "owningTeam",
				headerName: "Team",
				width: 150,
				renderCell: ({ row }) => (
					<Typography variant="body2">
						{row.owningTeam || "Unassigned"}
					</Typography>
				),
			},
			{
				field: "progress",
				headerName: "Progress",
				width: 400,
				sortable: false,
				renderCell: ({ row }) => (
					<Box sx={{ width: "100%" }}>
						<ProgressIndicator
							title="Overall Progress"
							progressableItem={{
								remainingWork: row.getRemainingWorkForFeature(),
								totalWork: row.getTotalWorkForFeature(),
							}}
							showDetails={false}
						/>
					</Box>
				),
			},
			{
				field: "deadline",
				headerName: "Deadline",
				width: 120,
				renderCell: () => (
					<Typography variant="body2">{delivery.getFormattedDate()}</Typography>
				),
			},
			{
				field: "forecast",
				headerName: "Forecast",
				width: 150,
				sortable: false,
				renderCell: ({ row }) => {
					// Find forecast for delivery date
					const deliveryDate = new Date(delivery.date);
					const relevantForecast = row.forecasts?.find(
						(f) =>
							Math.abs(
								new Date(f.expectedDate).getTime() - deliveryDate.getTime(),
							) <
							24 * 60 * 60 * 1000, // Within 1 day
					);

					if (!relevantForecast) {
						return (
							<Typography variant="body2" color="text.secondary">
								No forecast
							</Typography>
						);
					}

					return (
						<ForecastLikelihood
							remainingItems={0}
							targetDate={new Date(delivery.date)}
							likelihood={relevantForecast.probability ?? 0}
						/>
					);
				},
			},
		],
		[featureTerm, delivery],
	);

	return (
		<Paper elevation={1} sx={{ mb: 2 }}>
			<Accordion
				expanded={isExpanded}
				onChange={() => onToggleExpanded(delivery.id)}
				slotProps={{
					transition: {
						unmountOnExit: false, // Keep content mounted for better performance
					},
				}}
			>
				<AccordionSummary
					expandIcon={<ExpandMoreIcon />}
					sx={{
						minHeight: 64,
						"&.Mui-expanded": {
							minHeight: 64,
						},
						"& .MuiAccordionSummary-content": {
							alignItems: "center",
							margin: "12px 0",
							"&.Mui-expanded": {
								margin: "12px 0",
							},
						},
					}}
				>
					<Box
						sx={{
							display: "flex",
							alignItems: "center",
							justifyContent: "space-between",
							width: "100%",
							pr: 2,
						}}
					>
						<Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
							<Typography variant="h6" component="h3">
								{delivery.name}
							</Typography>
							<Chip
								label={delivery.getFormattedDate()}
								size="small"
								variant="outlined"
							/>
							<Chip
								label={`${delivery.getFeatureCount()} ${featureTerm}${delivery.getFeatureCount() !== 1 ? "s" : ""}`}
								size="small"
							/>
						</Box>
						<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
							<Box
								sx={{
									minWidth: 80,
									px: 1.5,
									py: 0.5,
									borderRadius: 2,
									bgColor: (() => {
										const level = delivery.getLikelihoodLevel();
										switch (level) {
											case "risky":
												return theme.palette.error.main;
											case "realistic":
												return theme.palette.warning.main;
											case "likely":
												return theme.palette.info.main;
											case "certain":
												return theme.palette.success.main;
											default:
												return theme.palette.grey[500];
										}
									})(),
									color: theme.palette.getContrastText(
										theme.palette.success.main,
									),
									fontWeight: 600,
									textAlign: "center",
								}}
							>
								<Typography variant="body2">
									{delivery.likelihoodPercentage}%
								</Typography>
							</Box>
							<IconButton
								size="small"
								onClick={(e) => {
									e.stopPropagation();
									onDelete(delivery);
								}}
								aria-label="delete"
							>
								<DeleteIcon />
							</IconButton>
						</Box>
					</Box>
				</AccordionSummary>
				<AccordionDetails sx={{ p: 0 }}>
					<Box sx={{ px: 2, pb: 2 }}>
						{isLoadingFeatures ? (
							<Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
								Loading features...
							</Typography>
						) : features.length === 0 ? (
							<Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
								No {featureTerm}s in this {deliveryTerm.toLowerCase()}.
							</Typography>
						) : (
							<DataGridBase
								rows={features as (IFeature & GridValidRowModel)[]}
								columns={columns}
								storageKey={`delivery-features-${delivery.id}`}
								loading={false}
								emptyStateMessage={`No ${featureTerm}s found`}
							/>
						)}
					</Box>
				</AccordionDetails>
			</Accordion>
		</Paper>
	);
};

export default DeliverySection;
