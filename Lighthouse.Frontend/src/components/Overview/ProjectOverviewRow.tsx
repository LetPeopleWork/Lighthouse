import React from "react";
import { Project } from "../../models/Project";
import TeamLink from "./TeamLink";
import LocalDateTimeDisplay from "../Common/LocalDateTimeDisplay";

interface ProjectOverviewOverviewProps {
  project: Project;
}

const ProjectOverviewRow: React.FC<ProjectOverviewOverviewProps> = ({ project }) => {
  const involvedTeamsRows: JSX.Element[] = [];

  project.involvedTeams.forEach((team) => {
    involvedTeamsRows.push(
      <div key={team.id}>
        <TeamLink team={team} />
      </div>
    )
  })
      
  return (
    <tr>
      <td>{project.name}</td>
      <td>{project.remainingWork}</td>
      <td>{involvedTeamsRows}</td>
      <td><LocalDateTimeDisplay utcDate={project.forecasts[0].expectedDate} /> </td>
      <td><LocalDateTimeDisplay utcDate={project.forecasts[1].expectedDate} /> </td>
      <td><LocalDateTimeDisplay utcDate={project.forecasts[2].expectedDate} /> </td>
      <td><LocalDateTimeDisplay utcDate={project.forecasts[3].expectedDate} /> </td>
      <td><LocalDateTimeDisplay utcDate={project.lastUpdated} showTime={true} /> </td>
    </tr>
  );
}

export default ProjectOverviewRow