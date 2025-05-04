import AccessTimeIcon from "@mui/icons-material/AccessTime";
import ViewKanban from "@mui/icons-material/ViewKanban";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import Divider from "@mui/material/Divider";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useTheme } from "@mui/material/styles";
import useMediaQuery from "@mui/material/useMediaQuery";
import { styled } from "@mui/system";
import type React from "react";
import { useState } from "react";
import { Link } from "react-router-dom";
import ForecastInfoList from "../../components/Common/Forecasts/ForecastInfoList";
import LocalDateTimeDisplay from "../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import ProgressIndicator from "../../components/Common/ProgressIndicator/ProgressIndicator";
import FeaturesDialog from "../../components/Common/ProjectCardDialogs/FeaturesDialog";
import MilestonesDialog from "../../components/Common/ProjectCardDialogs/MilestonesDialog";
import type { Project } from "../../models/Project/Project";
import ProjectLink from "./ProjectLink";

const ProjectCardStyle = styled(Card)(({ theme }) => ({
	width: "100%",
	height: "100%",
	display: "flex",
	flexDirection: "column",
	transition: "transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out",
	"&:hover": {
		transform: "translateY(-4px)",
		boxShadow: theme.shadows
			? theme.shadows[4 as keyof typeof theme.shadows]
			: "none",
	},
	borderRadius: 12,
}));

interface ProjectOverviewRowProps {
	project: Project;
}

const ProjectCard: React.FC<ProjectOverviewRowProps> = ({ project }) => {
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
	const [featuresDialogOpen, setFeaturesDialogOpen] = useState(false);
	const [milestonesDialogOpen, setMilestonesDialogOpen] = useState(false);

	const handleOpenFeaturesDialog = () => {
		setFeaturesDialogOpen(true);
	};

	const handleCloseFeaturesDialog = () => {
		setFeaturesDialogOpen(false);
	};

	const handleOpenMilestonesDialog = () => {
		setMilestonesDialogOpen(true);
	};

	const handleCloseMilestonesDialog = () => {
		setMilestonesDialogOpen(false);
	};

	const featureWithLatestForecast = project.features.reduce(
		(latest, feature) => {
			const latestForecastDate = latest?.forecasts.reduce(
				(latestDate, forecast) => {
					return forecast.expectedDate > latestDate
						? forecast.expectedDate
						: latestDate;
				},
				new Date(0),
			);

			const currentFeatureLatestForecastDate = feature.forecasts.reduce(
				(latestDate, forecast) => {
					return forecast.expectedDate > latestDate
						? forecast.expectedDate
						: latestDate;
				},
				new Date(0),
			);

			return currentFeatureLatestForecastDate > latestForecastDate
				? feature
				: latest;
		},
		project.features[0],
	);

	// Calculate completion percentage
	const completionPercentage =
		project.totalWork > 0
			? Math.round(
					((project.totalWork - project.remainingWork) / project.totalWork) *
						100,
				)
			: 0;

	// Filter out milestones that are in the past
	const currentOrFutureMilestones = project.milestones.filter((milestone) => {
		const today = new Date();
		today.setHours(0, 0, 0, 0);
		const milestoneDate = new Date(milestone.date);
		milestoneDate.setHours(0, 0, 0, 0);
		return milestoneDate >= today;
	});

	return (
		<>
			<ProjectCardStyle>
				<CardContent sx={{ flex: 1, display: "flex", flexDirection: "column" }}>
					<Grid container spacing={2}>
						<Grid size={{ xs: 12 }}>
							<ProjectLink project={project} />

							{project.involvedTeams.length > 0 && (
								<Stack
									direction="row"
									spacing={1}
									sx={{
										mt: 1,
										flexWrap: "wrap",
										gap: "8px",
										"& > *": { mb: 0.5 }, // Handle wrapping nicely
									}}
								>
									{project.involvedTeams.map((team) => (
										<Chip
											data-testid="team-link"
											key={team.id}
											size="small"
											label={team.name}
											color="primary"
											variant="outlined"
											component={Link}
											to={`/teams/${team.id}`}
											clickable
											sx={{
												cursor: "pointer",
												"&:hover": {
													bgcolor: `${theme.palette.primary.main}20`,
												},
											}}
										/>
									))}
								</Stack>
							)}
						</Grid>

						<Grid size={{ xs: 12 }}>
							<Stack
								direction="row"
								justifyContent="space-between"
								alignItems="center"
							>
								<Typography
									variant="subtitle2"
									color="text.secondary"
									sx={{ mt: 1, display: "flex", alignItems: "center" }}
								>
									<ViewKanban fontSize="small" sx={{ mr: 0.5 }} />
									{project.remainingWork} Work Items Remaining
								</Typography>

								<Chip
									label={`${project.features.length} Feature${project.features.length !== 1 ? "s" : ""}`}
									size="small"
									color="secondary"
									variant="outlined"
									onClick={handleOpenFeaturesDialog}
									sx={{
										cursor: "pointer",
										"&:hover": {
											backgroundColor: `${theme.palette.secondary.main}20`,
										},
									}}
								/>
							</Stack>

							<ProgressIndicator title="" progressableItem={project} />

							<Typography
								variant="body2"
								color="text.secondary"
								sx={{
									textAlign: "right",
									mt: 0.5,
									fontWeight: completionPercentage > 75 ? "bold" : "normal",
									color:
										completionPercentage > 75
											? theme.palette.success.main
											: "inherit",
								}}
							>
								{completionPercentage}% Complete
							</Typography>
						</Grid>

						<Grid size={{ xs: 12 }}>
							<Divider sx={{ my: 1.5 }} />

							<Stack
								direction="row"
								justifyContent="space-between"
								alignItems="flex-start"
							>
								{featureWithLatestForecast !== undefined ? (
									<ForecastInfoList
										title="Projected Completion"
										forecasts={featureWithLatestForecast.forecasts}
									/>
								) : (
									<Typography
										variant="body2"
										color="text.secondary"
										sx={{ fontStyle: "italic" }}
									>
										No forecast data available
									</Typography>
								)}

								{currentOrFutureMilestones.length > 0 && (
									<Chip
										label={`${currentOrFutureMilestones.length} Milestone${currentOrFutureMilestones.length !== 1 ? "s" : ""}`}
										size="small"
										color="info"
										variant="outlined"
										onClick={handleOpenMilestonesDialog}
										sx={{
											mt: 0.5,
											cursor: "pointer",
											"&:hover": {
												backgroundColor: `${theme.palette.info.main}20`,
											},
										}}
									/>
								)}
							</Stack>
						</Grid>
					</Grid>

					<Divider sx={{ my: 1.5 }} />

					<Stack
						direction={isMobile ? "column" : "row"}
						justifyContent="space-between"
						alignItems={isMobile ? "flex-start" : "center"}
						spacing={1}
						sx={{ mt: "auto", pt: 1 }}
					>
						<Typography
							variant="caption"
							sx={{
								display: "flex",
								alignItems: "center",
								color: theme.palette.text.secondary,
							}}
						>
							<AccessTimeIcon fontSize="small" sx={{ mr: 0.5, opacity: 0.7 }} />
							Last Updated:{" "}
							<LocalDateTimeDisplay
								utcDate={project.lastUpdated}
								showTime={true}
							/>
						</Typography>

						{project.tags && project.tags.length > 0 && (
							<Stack
								direction="row"
								spacing={0.5}
								sx={{
									flexWrap: "wrap",
									gap: "4px",
									justifyContent: "flex-end",
								}}
							>
								{project.tags.map((tag) => (
									<Chip
										key={tag}
										size="small"
										label={tag}
										variant="outlined"
										sx={{
											fontSize: "0.6875rem",
											height: "20px",
											color: theme.palette.text.secondary,
										}}
									/>
								))}
							</Stack>
						)}
					</Stack>
				</CardContent>
			</ProjectCardStyle>

			{/* Features Dialog */}
			<FeaturesDialog
				open={featuresDialogOpen}
				onClose={handleCloseFeaturesDialog}
				projectName={project.name}
				features={project.features}
			/>

			{/* Milestones Dialog */}
			<MilestonesDialog
				open={milestonesDialogOpen}
				onClose={handleCloseMilestonesDialog}
				projectName={project.name}
				milestones={project.milestones}
				milestoneLikelihoods={project.features[0]?.milestoneLikelihood}
			/>
		</>
	);
};

export default ProjectCard;
