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
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../../../components/Common/DataGrid/types";
import FeatureName from "../../../../../components/Common/FeatureName/FeatureName";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import ForecastLikelihood from "../../../../../components/Common/Forecasts/ForecastLikelihood";
import ProgressIndicator from "../../../../../components/Common/ProgressIndicator/ProgressIndicator";
import StyledLink from "../../../../../components/Common/StyledLink/StyledLink";
import type { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
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
	teams: IEntityReference[];
}

const DeliverySection: React.FC<DeliverySectionProps> = ({
	delivery,
	features,
	isExpanded,
	isLoadingFeatures,
	onToggleExpanded,
	onDelete,
	teams,
}) => {
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
				renderCell: ({ row }) => {
					// Find teams that have work for this feature
					const teamsWithWork = teams.filter(
						(team) => row.getTotalWorkForTeam(team.id) > 0,
					);

					if (teamsWithWork.length === 0) {
						return (
							<Typography variant="body2" color="text.secondary">
								Unassigned
							</Typography>
						);
					}

					return (
						<Box sx={{ display: "flex", flexDirection: "column", gap: 0.5 }}>
							{teamsWithWork.map((team) => (
								<StyledLink key={team.id} to={`/teams/${team.id}`}>
									<Typography variant="body2">{team.name}</Typography>
								</StyledLink>
							))}
						</Box>
					);
				},
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
						{teams
							.filter((team) => row.getTotalWorkForTeam(team.id) > 0)
							.map((team) => (
								<Box key={team.id}>
									<ProgressIndicator
										title={
											<StyledLink to={`/teams/${team.id}`}>
												{team.name}
											</StyledLink>
										}
										progressableItem={{
											remainingWork: row.getRemainingWorkForTeam(team.id),
											totalWork: row.getTotalWorkForTeam(team.id),
										}}
										showDetails={false}
									/>
								</Box>
							))}
					</Box>
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
		[featureTerm, delivery, teams],
	);

	const forecastLevel = new ForecastLevel(delivery.likelihoodPercentage);

	return (
		<Paper elevation={1} sx={{ mb: 2 }}>
			<Box sx={{ position: "relative" }}>
				<IconButton
					size="small"
					onClick={(e) => {
						e.stopPropagation();
						onDelete(delivery);
					}}
					aria-label="delete"
					sx={{
						position: "absolute",
						top: 8,
						right: 8,
						zIndex: 1,
						bgcolor: "background.paper",
						"&:hover": {
							bgcolor: "error.light",
						},
					}}
				>
					<DeleteIcon />
				</IconButton>
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
							pr: 8, // Add padding right for the delete button
						}}
					>
						<Box
							sx={{
								display: "flex",
								alignItems: "center",
								justifyContent: "space-between",
								width: "100%",
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
							<Chip
								label={`${Math.round(delivery.likelihoodPercentage)}%`}
								size="small"
								sx={{
									bgcolor: forecastLevel.color,
									color: "#fff",
									fontWeight: "bold",
								}}
							/>
						</Box>
					</AccordionSummary>
					<AccordionDetails sx={{ p: 0 }}>
						<Box sx={{ px: 2, pb: 2 }}>
							{/* Feature likelihood chips */}
							{delivery.featureLikelihoods.length > 0 && (
								<Box sx={{ mb: 2 }}>
									<Typography
										variant="body2"
										color="text.secondary"
										sx={{ mb: 1 }}
									>
										{featureTerm} Likelihood:
									</Typography>
									<Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}>
										{delivery.featureLikelihoods.map((fl) => {
											const feature = features.find(
												(f) => f.id === fl.featureId,
											);
											const featureForecastLevel = new ForecastLevel(
												fl.likelihoodPercentage,
											);
											return (
												<Chip
													key={fl.featureId}
													label={`${feature?.name || `Feature ${fl.featureId}`}: ${Math.round(fl.likelihoodPercentage)}%`}
													size="small"
													sx={{
														bgcolor: featureForecastLevel.color,
														color: "#fff",
														fontWeight: "bold",
														fontSize: "0.7rem",
													}}
												/>
											);
										})}
									</Box>
								</Box>
							)}

							{isLoadingFeatures ? (
								<Typography
									variant="body2"
									color="text.secondary"
									sx={{ p: 2 }}
								>
									Loading features...
								</Typography>
							) : features.length === 0 ? (
								<Typography
									variant="body2"
									color="text.secondary"
									sx={{ p: 2 }}
								>
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
			</Box>
		</Paper>
	);
};

export default DeliverySection;
