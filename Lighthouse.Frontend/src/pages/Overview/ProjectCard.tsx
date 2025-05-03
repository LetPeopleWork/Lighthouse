import AccessTimeIcon from "@mui/icons-material/AccessTime";
import CloseIcon from "@mui/icons-material/Close";
import ViewKanban from "@mui/icons-material/ViewKanban";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import Dialog from "@mui/material/Dialog";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Divider from "@mui/material/Divider";
import Grid from "@mui/material/Grid";
import IconButton from "@mui/material/IconButton";
import LinearProgress from "@mui/material/LinearProgress";
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

const FeatureItem = styled("div")(({ theme }) => ({
	padding: theme.spacing(1.5),
	borderRadius: theme.shape.borderRadius,
	marginBottom: theme.spacing(1),
	backgroundColor:
		theme.palette.mode === "light"
			? theme.palette.grey[100]
			: theme.palette.grey[800],
	"&:last-child": {
		marginBottom: 0,
	},
}));

interface ProjectOverviewRowProps {
	project: Project;
}

const ProjectCard: React.FC<ProjectOverviewRowProps> = ({ project }) => {
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
	const [dialogOpen, setDialogOpen] = useState(false);

	const handleOpenDialog = () => {
		setDialogOpen(true);
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
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

	// Extract color logic for feature state to avoid nested ternary
	const getFeatureStateColor = (stateCategory: string) => {
		if (stateCategory === "Done") return "success";
		if (stateCategory === "Doing") return "warning";
		return "default";
	};

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
									onClick={handleOpenDialog}
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
					</Stack>
				</CardContent>
			</ProjectCardStyle>

			{/* Features Dialog */}
			<Dialog
				open={dialogOpen}
				onClose={handleCloseDialog}
				maxWidth="sm"
				fullWidth
				aria-labelledby="features-dialog-title"
			>
				<DialogTitle id="features-dialog-title">
					<Stack
						direction="row"
						justifyContent="space-between"
						alignItems="center"
					>
						<Typography variant="h6">
							{project.name}: Features ({project.features.length})
						</Typography>
						<IconButton
							aria-label="close"
							onClick={handleCloseDialog}
							edge="end"
							size="small"
							sx={{
								color: theme.palette.grey[500],
							}}
						>
							<CloseIcon />
						</IconButton>
					</Stack>
				</DialogTitle>
				<DialogContent dividers>
					{project.features.length > 0 ? (
						<Stack spacing={1}>
							{project.features.map((feature) => {
								// Calculate feature completion percentage more accurately
								const totalWork = feature.getTotalWorkForFeature();
								const remainingWork = feature.getRemainingWorkForFeature();
								const featureCompletion =
									totalWork > 0
										? Math.round(
												((totalWork - remainingWork) / totalWork) * 100,
											)
										: 0;

								return (
									<FeatureItem key={feature.id}>
										<Stack
											direction="row"
											justifyContent="space-between"
											alignItems="center"
										>
											<Typography
												variant="body2"
												component={Link}
												to={feature.url ?? `/features/${feature.id}`}
												sx={{
													textDecoration: "none",
													color: "inherit",
													fontWeight: "medium",
													"&:hover": {
														textDecoration: "underline",
														color: theme.palette.primary.main,
													},
												}}
											>
												{feature.workItemReference} - {feature.name}
											</Typography>
											<Chip
												size="small"
												label={feature.state}
												color={getFeatureStateColor(feature.stateCategory)}
												variant="outlined"
											/>
										</Stack>
										<LinearProgress
											variant="determinate"
											value={featureCompletion}
											sx={{
												my: 1,
												height: 6,
												borderRadius: 3,
												bgcolor:
													theme.palette.mode === "light"
														? "rgba(0,0,0,0.1)"
														: "rgba(255,255,255,0.1)",
											}}
										/>
										<Stack
											direction="row"
											justifyContent="space-between"
											alignItems="center"
										>
											<Typography variant="caption" color="text.secondary">
												{remainingWork} of {totalWork} work items remaining
											</Typography>
											<Typography
												variant="caption"
												color={
													featureCompletion > 75
														? "success.main"
														: "text.secondary"
												}
												fontWeight={featureCompletion > 75 ? "bold" : "normal"}
											>
												{featureCompletion}% Complete
											</Typography>
										</Stack>
									</FeatureItem>
								);
							})}
						</Stack>
					) : (
						<Typography
							variant="body2"
							color="text.secondary"
							sx={{ fontStyle: "italic" }}
						>
							No features available
						</Typography>
					)}
				</DialogContent>
			</Dialog>
		</>
	);
};

export default ProjectCard;
