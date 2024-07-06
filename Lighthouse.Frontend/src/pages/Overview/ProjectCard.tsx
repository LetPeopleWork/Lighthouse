import React from "react";
import { Card, CardContent, Typography, Grid, Tooltip } from '@mui/material';
import { styled } from '@mui/system';
import { Project } from "../../models/Project";
import TeamLink from "./TeamLink";
import LocalDateTimeDisplay from "../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import ProjectLink from "./ProjectLink";
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import StyledCardTypography from "./StyledCardTypography";

// Custom icons representing different certainty levels with colors
import RiskyIcon from '@mui/icons-material/ErrorOutline';
import RealisticIcon from '@mui/icons-material/QueryBuilder';
import ConfidentIcon from '@mui/icons-material/CheckCircleOutline';
import CertainIcon from '@mui/icons-material/CheckCircle';
import { ViewKanban } from "@mui/icons-material";

const ProjectCardStyle = styled(Card)({
  marginBottom: 'inherit',
  width: 'fit-content', // Limit card width to content size
  alignSelf: 'flex-start', // Align card to start of flex container
});

const ForecastsHeader = styled(Typography)({
  marginBottom: 'inherit',
  fontWeight: 'bold',
});

const TooltipText: React.FC<{ level: string; percentage: number }> = ({ level, percentage }) => (
  <Typography variant="body1">
    {level} ({percentage}% Chance)
  </Typography>
);

interface ProjectOverviewRowProps {
  project: Project;
}

const ProjectCard: React.FC<ProjectOverviewRowProps> = ({ project }) => {
  const involvedTeamsList = project.involvedTeams.map((team) => (
    <TeamLink key={team.id} team={team} />
  ));

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
            <ForecastsHeader variant="body1">Project Completion</ForecastsHeader>
            <Tooltip title={<TooltipText level="Risky" percentage={50} />} arrow>
              <Typography variant="body1" sx={{ display: 'flex', alignItems: 'center' }}>
                <RiskyIcon style={{ color: 'red', marginRight: 8 }} />
                <LocalDateTimeDisplay utcDate={project.forecasts[0].expectedDate} />
              </Typography>
            </Tooltip>
            <Tooltip title={<TooltipText level="Realistic" percentage={70} />} arrow>
              <Typography variant="body1" sx={{ display: 'flex', alignItems: 'center' }}>
                <RealisticIcon style={{ color: 'orange', marginRight: 8 }} />
                <LocalDateTimeDisplay utcDate={project.forecasts[1].expectedDate} />
              </Typography>
            </Tooltip>
            <Tooltip title={<TooltipText level="Confident" percentage={85} />} arrow>
              <Typography variant="body1" sx={{ display: 'flex', alignItems: 'center' }}>
                <ConfidentIcon style={{ color: 'lightgreen', marginRight: 8 }} />
                <LocalDateTimeDisplay utcDate={project.forecasts[2].expectedDate} />
              </Typography>
            </Tooltip>
            <Tooltip title={<TooltipText level="Certain" percentage={95} />} arrow>
              <Typography variant="body1" sx={{ display: 'flex', alignItems: 'center' }}>
                <CertainIcon style={{ color: 'green', marginRight: 8 }} />
                <LocalDateTimeDisplay utcDate={project.forecasts[3].expectedDate} />
              </Typography>
            </Tooltip>
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
