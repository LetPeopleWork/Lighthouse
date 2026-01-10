import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
	Accordion,
	AccordionDetails,
	AccordionSummary,
	Box,
	Chip,
	IconButton,
	Paper,
	TableContainer,
	Typography,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../../../components/Common/DataGrid/types";
import FeatureName from "../../../../../components/Common/FeatureName/FeatureName";
import ForecastInfoList from "../../../../../components/Common/Forecasts/ForecastInfoList";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import ProgressIndicator from "../../../../../components/Common/ProgressIndicator/ProgressIndicator";
import StyledLink from "../../../../../components/Common/StyledLink/StyledLink";
import type { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../../services/TerminologyContext";
import {
	certainColor,
	realisticColor,
	riskyColor,
} from "../../../../../utils/theme/colors";

interface DeliverySectionProps {
	delivery: Delivery;
	features: IFeature[];
	isExpanded: boolean;
	isLoadingFeatures: boolean;
	onToggleExpanded: (deliveryId: number) => void;
	onDelete: (delivery: Delivery) => void;
	onEdit: (delivery: Delivery) => void;
	teams: IEntityReference[];
}

const DeliverySection: React.FC<DeliverySectionProps> = ({
	delivery,
	features,
	isExpanded,
	isLoadingFeatures,
	onToggleExpanded,
	onDelete,
	onEdit,
	teams,
}) => {
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);

	// Helper function to get forecast by probability (percentile)
	const getForecast = (probability: number) => {
		return delivery.completionDates.find((f) => f.probability === probability);
	};

	// Helper function to compare dates (date portion only)
	const compareDates = (
		forecastDate: Date,
		targetDateStr: string,
	): "before" | "equal" | "after" => {
		const forecast = new Date(forecastDate);
		const target = new Date(targetDateStr);
		forecast.setHours(0, 0, 0, 0);
		target.setHours(0, 0, 0, 0);
		if (forecast < target) return "before";
		if (forecast > target) return "after";
		return "equal";
	};

	// Helper function to determine chip color based on comparison with delivery date
	const get85PercentileColor = () => {
		const forecast85 = getForecast(85);
		if (!forecast85) return realisticColor;
		const comparison = compareDates(forecast85.expectedDate, delivery.date);
		if (comparison === "before") return certainColor;
		if (comparison === "equal") return realisticColor;
		return riskyColor;
	};

	const getRangeColor = () => {
		const forecast70 = getForecast(70);
		const forecast95 = getForecast(95);
		if (!forecast70 || !forecast95) return realisticColor;
		const comparison70 = compareDates(forecast70.expectedDate, delivery.date);
		const comparison95 = compareDates(forecast95.expectedDate, delivery.date);

		// Certain if 95% is before the delivery date
		if (comparison95 === "before") return certainColor;
		// Realistic if 70% is before, but 95% is after the delivery date
		if (comparison70 === "before" && comparison95 === "after")
			return realisticColor;
		// Risky if 70% is after the delivery date
		return riskyColor;
	};

	// Helper function to format date for display
	const formatDate = (date: Date): string => {
		return new Date(date).toLocaleDateString("en-US", {
			month: "short",
			day: "numeric",
			year: "numeric",
		});
	};

	// Define feature grid columns (adapted from PortfolioFeatureList)
	const columns: DataGridColumn<IFeature & GridValidRowModel>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: `${featureTerm} Name`,
				hideable: false,
				minWidth: 120,
				flex: 1,
				renderCell: ({ row }) => (
					<FeatureName
						name={row.name}
						url={row.url ?? ""}
						stateCategory={row.stateCategory}
						isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
						teamsWorkIngOnFeature={[]}
					/>
				),
			},
			{
				field: "owningTeam",
				headerName: "Team",
				minWidth: 100,
				flex: 0.5,
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
				minWidth: 200,
				flex: 1,
				sortable: false,
				renderCell: ({ row }) => (
					<Box sx={{ width: "100%" }}>
						<ProgressIndicator
							title="Overall Progress"
							progressableItem={{
								remainingWork: row.getRemainingWorkForFeature(),
								totalWork: row.getTotalWorkForFeature(),
							}}
							showDetails={true}
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
										showDetails={true}
									/>
								</Box>
							))}
					</Box>
				),
			},
			{
				field: "forecast",
				headerName: "Forecast",
				minWidth: 100,
				flex: 0.5,
				sortable: false,
				renderCell: ({ row }) => (
					<ForecastInfoList title={""} forecasts={row.forecasts} />
				),
			},
			{
				field: "likelihood",
				headerName: "Likelihood",
				minWidth: 100,
				flex: 0.3,
				sortable: false,
				renderCell: ({ row }) =>
					delivery.featureLikelihoods
						.filter((fl) => fl.featureId === row.id)
						.map((fl) => (
							<Chip
								key={fl.featureId}
								label={`${Math.round(fl.likelihoodPercentage)}%`}
								size="small"
								sx={{
									bgcolor: new ForecastLevel(fl.likelihoodPercentage).color,
									color: "#fff",
									fontWeight: "bold",
								}}
							/>
						)),
			},
		],
		[featureTerm, delivery, teams],
	);

	const forecastLevel = new ForecastLevel(delivery.likelihoodPercentage);

	return (
		<Paper elevation={1} sx={{ mb: 2, overflow: "hidden" }}>
			<Box sx={{ position: "relative" }}>
				<Accordion
					expanded={isExpanded}
					onChange={() => onToggleExpanded(delivery.id)}
					sx={{ overflow: "hidden" }}
					slotProps={{
						transition: {
							unmountOnExit: false, // Keep content mounted for better performance
						},
					}}
				>
					<AccordionSummary
						expandIcon={<ExpandMoreIcon />}
						sx={{
							minHeight: 80,
							position: "relative",
							"&.Mui-expanded": {
								minHeight: 80,
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
						<IconButton
							size="small"
							onClick={(e) => {
								e.stopPropagation();
								onEdit(delivery);
							}}
							aria-label="edit"
							sx={{
								position: "absolute",
								top: "50%",
								transform: "translateY(-50%)",
								right: 48, // Position to the left of delete button
								zIndex: 1,
								bgcolor: "background.paper",
								"&:hover": {
									bgcolor: "primary.light",
								},
							}}
						>
							<EditIcon />
						</IconButton>
						<IconButton
							size="small"
							onClick={(e) => {
								e.stopPropagation();
								onDelete(delivery);
							}}
							aria-label="delete"
							sx={{
								position: "absolute",
								top: "50%",
								transform: "translateY(-50%)",
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
						<Box
							sx={{
								display: "flex",
								alignItems: "center",
								justifyContent: "space-between",
								width: "100%",
								gap: 4, // Add gap between left and right sections
							}}
						>
							<Box
								sx={{
									display: "flex",
									alignItems: "center",
									gap: 2,
									flexShrink: 0,
								}}
							>
								<Typography variant="h6" component="h3">
									{`${delivery.name} (${delivery.getFormattedDate()})`}
								</Typography>
								<Chip
									label={`Likelihood: ${Math.round(delivery.likelihoodPercentage)}%`}
									size="small"
									sx={{
										bgcolor: forecastLevel.color,
										color: "#fff",
										fontWeight: "bold",
									}}
								/>
								{(() => {
									const forecast85 = getForecast(85);
									if (forecast85) {
										return (
											<Chip
												label={`85%: ${formatDate(forecast85.expectedDate)}`}
												size="small"
												sx={{
													bgcolor: get85PercentileColor(),
													color: "#fff",
													fontWeight: "bold",
												}}
											/>
										);
									}
									return null;
								})()}
								{(() => {
									const forecast70 = getForecast(70);
									const forecast95 = getForecast(95);
									if (forecast70 && forecast95) {
										return (
											<Chip
												label={`∆ 70 - 95%: ${formatDate(forecast70.expectedDate)} - ${formatDate(forecast95.expectedDate)}`}
												size="small"
												sx={{
													bgcolor: getRangeColor(),
													color: "#fff",
													fontWeight: "bold",
												}}
											/>
										);
									}
									return null;
								})()}
							</Box>
							<Box
								sx={{
									display: "flex",
									alignItems: "center",
									justifyContent: "center",
									flex: 1,
									minWidth: 200,
								}}
							>
								<ProgressIndicator
									title={`${delivery.getFeatureCount()} ${delivery.getFeatureCount() === 1 ? featureTerm : featuresTerm} (${delivery.totalWork} ${workItemsTerm})`}
									progressableItem={{
										remainingWork: delivery.remainingWork,
										totalWork: delivery.totalWork,
									}}
									showDetails={true}
								/>
							</Box>
						</Box>
					</AccordionSummary>
					<AccordionDetails sx={{ p: 0 }}>
						{(() => {
							let content: React.ReactNode;
							if (isLoadingFeatures) {
								content = (
									<Typography
										variant="body2"
										color="text.secondary"
										sx={{ p: 2 }}
									>
										{`Loading ${featuresTerm}...`}
									</Typography>
								);
							} else if (features.length === 0) {
								content = (
									<Typography
										variant="body2"
										color="text.secondary"
										sx={{ p: 2 }}
									>
										No {featuresTerm} in this {deliveryTerm}.
									</Typography>
								);
							} else {
								content = (
									<TableContainer
										component={Paper}
										elevation={0}
										sx={{ mx: 2, mb: 2 }}
									>
										<DataGridBase
											rows={features as (IFeature & GridValidRowModel)[]}
											columns={columns}
											storageKey={`delivery-features-${delivery.id}`}
											loading={false}
											emptyStateMessage={`No ${featuresTerm} found`}
										/>
									</TableContainer>
								);
							}
							return content;
						})()}
					</AccordionDetails>
				</Accordion>
			</Box>
		</Paper>
	);
};

export default DeliverySection;
