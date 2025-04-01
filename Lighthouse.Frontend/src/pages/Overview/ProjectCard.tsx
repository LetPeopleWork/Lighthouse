import { ViewKanban } from "@mui/icons-material";
import AccessTimeIcon from "@mui/icons-material/AccessTime";
import { Card, CardContent, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import { styled } from "@mui/system";
import type React from "react";
import ForecastInfoList from "../../components/Common/Forecasts/ForecastInfoList";
import LocalDateTimeDisplay from "../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import ProgressIndicator from "../../components/Common/ProgressIndicator/ProgressIndicator";
import type { Project } from "../../models/Project/Project";
import ProjectLink from "./ProjectLink";
import StyledCardTypography from "./StyledCardTypography";
import TeamLink from "./TeamLink";

const ProjectCardStyle = styled(Card)({
	marginBottom: "inherit",
	width: "fit-content",
	alignSelf: "flex-start",
});

interface ProjectOverviewRowProps {
	project: Project;
}

const ProjectCard: React.FC<ProjectOverviewRowProps> = ({ project }) => {
	const involvedTeamsList = project.involvedTeams.map((team) => (
		<TeamLink key={team.id} team={team} />
	));

	const featureWithLatestForecast = project.features.reduce(
		(latest, feature) => {
			const latestForecastDate = latest.forecasts.reduce(
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

	return (
		<ProjectCardStyle>
			<CardContent>
				<Grid container spacing={2}>
					<Grid size={{ xs: 12 }}>
						<ProjectLink project={project} />
					</Grid>
					<Grid size={{ xs: 12 }}>
						<ProgressIndicator
							title={
								<StyledCardTypography
									text={`${project.remainingWork} Work Items Remaining`}
									icon={ViewKanban}
								/>
							}
							progressableItem={project}
						/>

						<Typography variant="body1">{involvedTeamsList}</Typography>
					</Grid>
					<Grid size={{ xs: 12 }}>
						{featureWithLatestForecast !== undefined ? (
							<ForecastInfoList
								title="Projected Completion"
								forecasts={featureWithLatestForecast.forecasts}
							/>
						) : (
							<></>
						)}
					</Grid>
				</Grid>

				<StyledCardTypography text="Last Updated:" icon={AccessTimeIcon}>
					<LocalDateTimeDisplay utcDate={project.lastUpdated} showTime={true} />
				</StyledCardTypography>
			</CardContent>
		</ProjectCardStyle>
	);
};

export default ProjectCard;
