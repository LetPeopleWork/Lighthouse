import ViewKanban from "@mui/icons-material/ViewKanban";
import AccessTimeIcon from "@mui/icons-material/AccessTime";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import Divider from "@mui/material/Divider";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import useMediaQuery from "@mui/material/useMediaQuery";
import { useTheme } from "@mui/material/styles";
import Grid from "@mui/material/Grid";
import { styled } from "@mui/system";
import type React from "react";
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
		boxShadow:
			theme.palette.mode === "dark"
				? "0px 8px 16px rgba(0, 0, 0, 0.6)"
				: "0px 8px 16px rgba(0, 0, 0, 0.15)",
	},
	borderRadius: 12,
}));

interface ProjectOverviewRowProps {
	project: Project;
}

const ProjectCard: React.FC<ProjectOverviewRowProps> = ({ project }) => {
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("sm"));

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

	return (
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
						<Typography
							variant="subtitle2"
							color="text.secondary"
							sx={{ mt: 1, display: "flex", alignItems: "center" }}
						>
							<ViewKanban fontSize="small" sx={{ mr: 0.5 }} />
							{project.remainingWork} Work Items Remaining
						</Typography>

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

					<Chip
						label={`${project.features.length} Feature${project.features.length !== 1 ? "s" : ""}`}
						size="small"
						color="secondary"
						variant="outlined"
					/>
				</Stack>
			</CardContent>
		</ProjectCardStyle>
	);
};

export default ProjectCard;
