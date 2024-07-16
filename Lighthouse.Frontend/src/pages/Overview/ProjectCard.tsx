import React from "react";
import { Card, CardContent, Typography, Grid } from '@mui/material';
import { styled } from '@mui/system';
import { Project } from "../../models/Project";
import TeamLink from "./TeamLink";
import LocalDateTimeDisplay from "../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import ProjectLink from "./ProjectLink";
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import StyledCardTypography from "./StyledCardTypography";
import { ViewKanban } from "@mui/icons-material";
import ForecastInfoList from "../../components/Common/Forecasts/ForecastInfoList";

const ProjectCardStyle = styled(Card)({
  marginBottom: 'inherit',
  width: 'fit-content',
  alignSelf: 'flex-start',
});

interface ProjectOverviewRowProps {
  project: Project;
}

const ProjectCard: React.FC<ProjectOverviewRowProps> = ({ project }) => {
  const involvedTeamsList = project.involvedTeams.map((team) => (
    <TeamLink key={team.id} team={team} />
  ));

  const featureWithLatestForecast = project.features.reduce((latest, feature) => {
    const latestForecastDate = latest.forecasts.reduce((latestDate, forecast) => {
      return forecast.expectedDate > latestDate ? forecast.expectedDate : latestDate;
    }, new Date(0));

    const currentFeatureLatestForecastDate = feature.forecasts.reduce((latestDate, forecast) => {
      return forecast.expectedDate > latestDate ? forecast.expectedDate : latestDate;
    }, new Date(0));

    return currentFeatureLatestForecastDate > latestForecastDate ? feature : latest;
  }, project.features[0]);

  return (
    <ProjectCardStyle>
      <CardContent>
        <ProjectLink project={project} />
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <StyledCardTypography text={`${project.remainingWork} Work Items`} icon={ViewKanban} />

            <Typography variant="body1">
              {involvedTeamsList}
            </Typography>
          </Grid>
          <Grid item xs={12}>
            <ForecastInfoList
              title="Projected Completion"
              forecasts={featureWithLatestForecast.forecasts}
            />
          </Grid>
        </Grid>

        <StyledCardTypography text="Last Updated:" icon={AccessTimeIcon} >
          <LocalDateTimeDisplay utcDate={project.lastUpdated} showTime={true} />
        </StyledCardTypography>
      </CardContent>
    </ProjectCardStyle>
  );
}

export default ProjectCard;
